#!/usr/bin/env python3
"""
Script de migración: Rutinas → Consecuentes Directos Mamdani

Este script convierte el modelo legacy basado en fuzzy_routines a consecuentes
directos para habilitar agregación Mamdani completa.

Flujo de migración:
1. Para cada regla fuzzy con 'consequent' (FuzzyRoutineId):
   a. Obtener la rutina asociada
   b. Extraer los términos (power_term_id, duration_term_id) de cada step
   c. Crear RuleConsequent por cada variable de salida única
   d. Guardar _legacy_routine_id para rollback
2. Actualizar las reglas en MongoDB
3. Generar reporte de migración

Uso:
    python scripts/migrate_rules_to_consequents.py [--dry-run] [--system-id <id>]

Opciones:
    --dry-run       Simula la migración sin modificar la base de datos
    --system-id     Migra solo las reglas del sistema especificado
    --verbose       Muestra información detallada de cada migración

Variables de entorno requeridas:
    MONGODB_CONNECTION_STRING: Conexión a MongoDB
"""

import os
import sys
import asyncio
import argparse
from typing import List, Dict, Any, Optional
from datetime import datetime

# Agregar el directorio raíz al path para imports
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from motor.motor_asyncio import AsyncIOMotorClient
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent
from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine
from FuzzyService.Domain.ValueObjects import FuzzyVariableId, FuzzyTermId, FuzzyRuleId, FuzzyRoutineId
from FuzzyService.Domain.Utils import extract_oid


class MigrationStats:
    """Estadísticas de la migración."""

    def __init__(self):
        self.rules_total = 0
        self.rules_migrated = 0
        self.rules_skipped = 0
        self.rules_failed = 0
        self.consequents_created = 0
        self.errors: List[str] = []

    def add_error(self, rule_id: str, error: str):
        self.errors.append(f"Rule {rule_id}: {error}")
        self.rules_failed += 1

    def print_summary(self):
        print("\n" + "=" * 80)
        print("RESUMEN DE MIGRACIÓN")
        print("=" * 80)
        print(f"Reglas analizadas:      {self.rules_total}")
        print(f"Reglas migradas:        {self.rules_migrated}")
        print(f"Reglas omitidas:        {self.rules_skipped}")
        print(f"Reglas con error:       {self.rules_failed}")
        print(f"Consecuentes creados:   {self.consequents_created}")

        if self.errors:
            print("\n" + "-" * 80)
            print("ERRORES:")
            for error in self.errors:
                print(f"  - {error}")
        print("=" * 80)


class RuleConsequentMigrator:
    """Migrador de reglas de rutinas a consecuentes directos."""

    def __init__(self, db, dry_run: bool = False, verbose: bool = False):
        self.db = db
        self.dry_run = dry_run
        self.verbose = verbose
        self.stats = MigrationStats()

    async def get_routine_by_id(self, routine_id: str) -> Optional[FuzzyRoutine]:
        """Obtiene una rutina por ID desde MongoDB."""
        try:
            routine_doc = await self.db.fuzzy_routines.find_one({"_id": routine_id})
            if not routine_doc:
                return None
            return FuzzyRoutine.from_dict(routine_doc)
        except Exception as e:
            print(f"Error obteniendo rutina {routine_id}: {e}")
            return None

    async def get_variable_id_for_term(self, term_id: str) -> Optional[str]:
        """Obtiene el variable_id de un término."""
        try:
            term_doc = await self.db.fuzzy_terms.find_one({"_id": term_id})
            if not term_doc:
                return None
            variable_id = term_doc.get("variableId")
            if isinstance(variable_id, dict):
                return extract_oid(variable_id)
            return variable_id
        except Exception as e:
            print(f"Error obteniendo término {term_id}: {e}")
            return None

    async def create_consequents_from_routine(
        self, routine: FuzzyRoutine
    ) -> List[RuleConsequent]:
        """
        Crea consecuentes directos desde una rutina.

        Para cada step de la rutina:
        - Extrae power_term_id y duration_term_id
        - Obtiene el variable_id de cada término
        - Agrupa términos por variable_id
        - Crea un RuleConsequent por cada variable única

        Returns:
            Lista de RuleConsequent creados
        """
        # Diccionario para agrupar términos por variable
        terms_by_variable: Dict[str, List[str]] = {}

        for step in routine.steps:
            # Procesar power_term_id
            power_term_id = str(step.power_term_id)
            power_var_id = await self.get_variable_id_for_term(power_term_id)

            if power_var_id:
                if power_var_id not in terms_by_variable:
                    terms_by_variable[power_var_id] = []
                if power_term_id not in terms_by_variable[power_var_id]:
                    terms_by_variable[power_var_id].append(power_term_id)

            # Procesar duration_term_id
            duration_term_id = str(step.duration_term_id)
            duration_var_id = await self.get_variable_id_for_term(duration_term_id)

            if duration_var_id:
                if duration_var_id not in terms_by_variable:
                    terms_by_variable[duration_var_id] = []
                if duration_term_id not in terms_by_variable[duration_var_id]:
                    terms_by_variable[duration_var_id].append(duration_term_id)

        # Crear consecuentes
        consequents = []
        for variable_id, term_ids in terms_by_variable.items():
            consequent = RuleConsequent(
                variable_id=FuzzyVariableId(variable_id),
                terms=[FuzzyTermId(tid) for tid in term_ids],
                aggregation_method="max"  # Mamdani estándar
            )
            consequents.append(consequent)

        return consequents

    async def migrate_rule(self, rule_doc: Dict[str, Any]) -> bool:
        """
        Migra una regla individual.

        Returns:
            True si se migró exitosamente, False en caso contrario
        """
        rule_id = str(rule_doc.get("_id", "unknown"))

        try:
            # Parsear regla
            rule = FuzzyRule.from_dict(rule_doc)

            # Verificar si ya está migrada
            if rule.has_mamdani_consequents():
                if self.verbose:
                    print(f"  ⏭️  Regla {rule.name} ({rule_id}) ya tiene consecuentes Mamdani")
                self.stats.rules_skipped += 1
                return False

            # Verificar si tiene consequent legacy
            if not rule.has_legacy_consequent():
                if self.verbose:
                    print(f"  ⚠️  Regla {rule.name} ({rule_id}) no tiene consequent legacy")
                self.stats.rules_skipped += 1
                return False

            # Obtener rutina
            routine_id = str(rule.consequent)
            routine = await self.get_routine_by_id(routine_id)

            if not routine:
                self.stats.add_error(rule_id, f"Rutina {routine_id} no encontrada")
                return False

            # Crear consecuentes desde rutina
            consequents = await self.create_consequents_from_routine(routine)

            if not consequents:
                self.stats.add_error(rule_id, f"No se pudieron crear consecuentes desde rutina {routine_id}")
                return False

            if self.verbose:
                print(f"  ✅ Regla {rule.name} ({rule_id}):")
                print(f"     Rutina: {routine.routine_name} → {len(consequents)} consecuente(s)")
                for cons in consequents:
                    print(f"       - Variable {cons.variable_id}: {len(cons.terms)} término(s)")

            # Actualizar regla en MongoDB
            if not self.dry_run:
                update_doc = {
                    "$set": {
                        "consequents": [c.to_dict() for c in consequents],
                        "_legacy_routine_id": {"$oid": routine_id}
                    }
                }

                result = await self.db.fuzzy_rules.update_one(
                    {"_id": rule_id},
                    update_doc
                )

                if result.modified_count != 1:
                    self.stats.add_error(rule_id, "No se pudo actualizar en MongoDB")
                    return False

            self.stats.rules_migrated += 1
            self.stats.consequents_created += len(consequents)
            return True

        except Exception as e:
            self.stats.add_error(rule_id, str(e))
            return False

    async def migrate_system(self, system_id: Optional[str] = None):
        """
        Migra todas las reglas de un sistema (o todos los sistemas).

        Args:
            system_id: ID del sistema a migrar, o None para migrar todos
        """
        # Construir query
        query = {}
        if system_id:
            query["systemId"] = {"$oid": system_id}

        # Obtener reglas
        cursor = self.db.fuzzy_rules.find(query)
        rules = await cursor.to_list(length=None)

        self.stats.rules_total = len(rules)

        print(f"\n📊 Iniciando migración de {len(rules)} regla(s)...")
        if self.dry_run:
            print("⚠️  Modo DRY-RUN activado (sin cambios en BD)\n")

        # Migrar cada regla
        for rule_doc in rules:
            await self.migrate_rule(rule_doc)

        # Mostrar resumen
        self.stats.print_summary()


async def main():
    """Punto de entrada principal del script."""
    parser = argparse.ArgumentParser(
        description="Migra reglas fuzzy de rutinas a consecuentes directos Mamdani"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Simula la migración sin modificar la base de datos"
    )
    parser.add_argument(
        "--system-id",
        type=str,
        help="Migra solo las reglas del sistema especificado"
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Muestra información detallada de cada migración"
    )

    args = parser.parse_args()

    # Obtener conexión MongoDB
    connection_string = os.getenv("MONGODB_CONNECTION_STRING")
    if not connection_string:
        print("❌ Error: MONGODB_CONNECTION_STRING no está definida")
        sys.exit(1)

    # Conectar a MongoDB
    client = AsyncIOMotorClient(connection_string)
    db = client.fuzzy_db

    try:
        # Verificar conexión
        await client.admin.command('ping')
        print("✅ Conectado a MongoDB")

        # Crear migrador
        migrator = RuleConsequentMigrator(
            db=db,
            dry_run=args.dry_run,
            verbose=args.verbose
        )

        # Ejecutar migración
        await migrator.migrate_system(system_id=args.system_id)

        # Mensaje final
        if args.dry_run:
            print("\n💡 Para aplicar los cambios, ejecuta sin --dry-run")
        else:
            print("\n✅ Migración completada")
            print("💡 Para activar el nuevo modelo, configura: FUZZY_USE_MAMDANI_CONSEQUENTS=true")

    except Exception as e:
        print(f"❌ Error durante la migración: {e}")
        sys.exit(1)
    finally:
        client.close()


if __name__ == "__main__":
    asyncio.run(main())
