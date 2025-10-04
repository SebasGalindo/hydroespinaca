#!/usr/bin/env python3
"""Script para verificar el seed data del fuzzy-service."""

import asyncio
import os
import sys
from motor.motor_asyncio import AsyncIOMotorClient


async def verify_seed_data():
    """Verifica que el seed data se haya creado correctamente."""

    # Conectar a MongoDB
    mongo_uri = os.getenv("MONGODB_CONNECTION_STRING", "mongodb://localhost:27017")
    client = AsyncIOMotorClient(mongo_uri)
    db = client["fuzzy_db"]

    print("=" * 80)
    print("🔍 VERIFICACIÓN DE SEED DATA - FUZZY SERVICE")
    print("=" * 80)
    print()

    # Verificar fuzzy_systems
    print("📊 1. Verificando fuzzy_systems...")
    systems_count = await db.fuzzy_systems.count_documents({})
    print(f"   Total sistemas: {systems_count}")

    if systems_count > 0:
        system = await db.fuzzy_systems.find_one({})
        print(f"   ✅ Sistema: {system['name']}")
        print(f"   - Status: {system['status']}")
        print(f"   - Input variables: {len(system.get('input_variable_ids', []))}")
        print(f"   - Output variables: {len(system.get('output_variable_ids', []))}")
        print(f"   - Rules: {len(system.get('rule_ids', []))}")
    else:
        print("   ❌ No se encontraron sistemas")

    print()

    # Verificar fuzzy_variables
    print("📊 2. Verificando fuzzy_variables...")
    variables_count = await db.fuzzy_variables.count_documents({})
    print(f"   Total variables: {variables_count}")

    if variables_count > 0:
        input_vars = await db.fuzzy_variables.count_documents({"variable_type": "input"})
        output_vars = await db.fuzzy_variables.count_documents({"variable_type": "output"})
        print(f"   - Variables INPUT: {input_vars}")
        print(f"   - Variables OUTPUT: {output_vars}")

        # Verificar que todas tienen reference_id
        vars_without_ref = await db.fuzzy_variables.count_documents({
            "$or": [
                {"reference_id": {"$exists": False}},
                {"reference_id": ""}
            ]
        })
        if vars_without_ref > 0:
            print(f"   ⚠️  Variables sin reference_id: {vars_without_ref}")
        else:
            print(f"   ✅ Todas las variables tienen reference_id")

        # Verificar que todas tienen términos
        vars_without_terms = await db.fuzzy_variables.count_documents({
            "$or": [
                {"terms": {"$exists": False}},
                {"terms": {"$size": 0}}
            ]
        })
        if vars_without_terms > 0:
            print(f"   ⚠️  Variables sin términos: {vars_without_terms}")
        else:
            print(f"   ✅ Todas las variables tienen términos")

        # Mostrar algunas variables
        print("\n   📋 Muestra de variables:")
        async for var in db.fuzzy_variables.find({}).limit(5):
            terms_count = len(var.get("terms", []))
            print(f"      - {var['name']} ({var['variable_type']}): {terms_count} términos")
    else:
        print("   ❌ No se encontraron variables")

    print()

    # Verificar fuzzy_terms
    print("📊 3. Verificando fuzzy_terms...")
    terms_count = await db.fuzzy_terms.count_documents({})
    print(f"   Total términos: {terms_count}")

    if terms_count > 0:
        # Verificar que todos tienen variable_id válido
        terms_with_var = await db.fuzzy_terms.count_documents({
            "variable_id": {"$exists": True, "$ne": None}
        })
        print(f"   - Términos con variable_id: {terms_with_var}/{terms_count}")

        # Verificar que todos tienen membership_function
        terms_with_mf = await db.fuzzy_terms.count_documents({
            "membership_function": {"$exists": True}
        })
        print(f"   - Términos con membership_function: {terms_with_mf}/{terms_count}")

        if terms_with_var == terms_count and terms_with_mf == terms_count:
            print(f"   ✅ Todos los términos están completos")

        # Mostrar algunos términos
        print("\n   📋 Muestra de términos:")
        async for term in db.fuzzy_terms.find({}).limit(5):
            mf_type = term.get("membership_function", {}).get("function_type", "N/A")
            print(f"      - {term['label']} ({mf_type})")
    else:
        print("   ❌ No se encontraron términos")

    print()

    # Verificar fuzzy_rules
    print("📊 4. Verificando fuzzy_rules...")
    rules_count = await db.fuzzy_rules.count_documents({})
    print(f"   Total reglas: {rules_count}")

    expected_rules = 10  # 10 reglas según el seed data
    if rules_count >= expected_rules:
        print(f"   ✅ Se encontraron {rules_count} reglas (esperado: {expected_rules})")
        async for rule in db.fuzzy_rules.find({}).limit(3):
            conditions_count = len(rule.get("conditions", []))
            print(f"      - {rule['name']}: {conditions_count} condiciones")
    else:
        print(f"   ⚠️  Solo se encontraron {rules_count} reglas (esperado: {expected_rules})")

    print()

    # Verificar fuzzy_routines
    print("📊 5. Verificando fuzzy_routines...")
    routines_count = await db.fuzzy_routines.count_documents({})
    print(f"   Total rutinas: {routines_count}")

    expected_routines = 12  # 12 rutinas según el seed data
    if routines_count >= expected_routines:
        print(f"   ✅ Se encontraron {routines_count} rutinas (esperado: {expected_routines})")

        # Verificar rutina de emergencia térmica (debe tener 5 pasos)
        emergency_routine = await db.fuzzy_routines.find_one({"routineName": "Emergencia Termica"})
        if emergency_routine:
            emergency_steps = len(emergency_routine.get("steps", []))
            if emergency_steps == 5:
                print(f"   ✅ Rutina 'Emergencia Termica' tiene {emergency_steps} pasos (correcto)")
            else:
                print(f"   ⚠️  Rutina 'Emergencia Termica' tiene {emergency_steps} pasos (esperado: 5)")

        print("\n   📋 Muestra de rutinas:")
        async for routine in db.fuzzy_routines.find({}).limit(5):
            steps_count = len(routine.get("steps", []))
            print(f"      - {routine['routineName']}: {steps_count} pasos")
    else:
        print(f"   ⚠️  Solo se encontraron {routines_count} rutinas (esperado: {expected_routines})")

    print()

    # Verificar integridad referencial
    print("🔗 6. Verificando integridad referencial...")

    # Verificar que los términos apuntan a variables existentes
    all_term_var_ids = set()
    async for term in db.fuzzy_terms.find({}, {"variable_id": 1}):
        var_id = term.get("variable_id")
        if var_id:
            all_term_var_ids.add(str(var_id))

    all_variable_ids = set()
    async for var in db.fuzzy_variables.find({}, {"_id": 1}):
        all_variable_ids.add(str(var["_id"]))

    orphan_terms = all_term_var_ids - all_variable_ids
    if orphan_terms:
        print(f"   ❌ Términos huérfanos (sin variable): {len(orphan_terms)}")
        for var_id in list(orphan_terms)[:5]:
            print(f"      - {var_id}")
    else:
        print(f"   ✅ Todos los términos apuntan a variables válidas")

    # Verificar que las variables tienen los términos que declaran
    print("\n   Verificando términos declarados en variables...")
    invalid_refs = 0
    async for var in db.fuzzy_variables.find({}):
        declared_term_ids = set(str(tid) for tid in var.get("terms", []))
        actual_term_ids = set()

        async for term in db.fuzzy_terms.find({"variable_id": var["_id"]}):
            actual_term_ids.add(str(term["_id"]))

        if declared_term_ids != actual_term_ids:
            invalid_refs += 1
            print(f"   ⚠️  Variable '{var['name']}': declarados={len(declared_term_ids)}, reales={len(actual_term_ids)}")

    if invalid_refs == 0:
        print(f"   ✅ Todas las variables tienen referencias correctas a términos")

    print()

    # Resumen final
    print("=" * 80)
    print("📊 RESUMEN")
    print("=" * 80)

    total_checks = 0
    passed_checks = 0

    checks = [
        ("Sistema creado", systems_count > 0),
        ("Variables creadas (esperado: 12)", variables_count >= 12),
        ("Términos creados (esperado: ~23)", terms_count >= 20),
        ("Rutinas creadas (esperado: 12)", routines_count >= expected_routines),
        ("Reglas creadas (esperado: 10)", rules_count >= expected_rules),
        ("Variables con reference_id", vars_without_ref == 0 if variables_count > 0 else False),
        ("Variables con términos", vars_without_terms == 0 if variables_count > 0 else False),
        ("Términos sin huérfanos", len(orphan_terms) == 0),
        ("Referencias válidas", invalid_refs == 0)
    ]

    for check_name, passed in checks:
        total_checks += 1
        if passed:
            passed_checks += 1
            print(f"✅ {check_name}")
        else:
            print(f"❌ {check_name}")

    print()
    print(f"Resultado: {passed_checks}/{total_checks} verificaciones exitosas")

    if passed_checks == total_checks:
        print("\n🎉 ¡Seed data verificado correctamente!")
        return 0
    else:
        print("\n⚠️  Algunos problemas detectados")
        return 1


if __name__ == "__main__":
    exit_code = asyncio.run(verify_seed_data())
    sys.exit(exit_code)
