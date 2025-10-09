#!/usr/bin/env python3
"""
Validador exhaustivo de lógica de rutinas del fuzzy-service.
Detecta inconsistencias entre tipos de actuadores y comandos usados en steps.
"""

import sys
from typing import Dict, List, Any, Set, Tuple
from collections import defaultdict

sys.path.insert(0, '/home/moredev/dev/hydroespinaca/software-project/fuzzy-service')
from FuzzyService.Infrastructure.Configuration.SeedData import SeedDataConfig


class RoutineLogicValidator:
    """Validador de lógica de rutinas fuzzy."""

    def __init__(self):
        self.variables = SeedDataConfig.get_variables_config()
        self.terms = SeedDataConfig.get_terms_config()
        self.routines = SeedDataConfig.get_routines_config()

        # Mapeo de variables
        self.var_by_id: Dict[str, Dict] = {v['_id']: v for v in self.variables}
        self.term_by_id: Dict[str, Dict] = {t['_id']: t for t in self.terms}

        # Mapeo de términos a variables
        self.term_to_var: Dict[str, str] = {}
        for term in self.terms:
            self.term_to_var[term['_id']] = term['variable_id']

        # Errores detectados
        self.errors: List[Dict[str, Any]] = []
        self.warnings: List[Dict[str, Any]] = []

    def get_actuator_type(self, variable_id: str) -> str:
        """Obtiene el tipo de actuador (PWM o DIGITAL) de una variable."""
        var = self.var_by_id.get(variable_id)
        if not var:
            return "UNKNOWN"
        return var.get('actuator_type', 'NOT_DEFINED')

    def get_term_value_type(self, term_id: str) -> str:
        """Determina si un término genera power (ON/OFF) o dutyCycle (numérico)."""
        term = self.term_by_id.get(term_id)
        if not term:
            return "UNKNOWN"

        label = term.get('label', '').upper()
        params = term.get('parameters', [])

        # Términos ON/OFF son para power
        if label in ['ON', 'OFF']:
            return "POWER"

        # Términos con parámetros numéricos son para dutyCycle
        if params and isinstance(params, list) and len(params) == 3:
            # Si el término está cerca de 0 o 100, puede ser power o dutyCycle
            # Pero si la variable es PWM, definitivamente es dutyCycle
            var_id = self.term_to_var.get(term_id)
            if var_id:
                actuator_type = self.get_actuator_type(var_id)
                if actuator_type == "PWM":
                    return "DUTYCYCLE"
                elif actuator_type == "DIGITAL":
                    # Para DIGITAL, si no es ON/OFF explícito, verificar parámetros
                    if params[1] >= 99:  # Centro cerca de 100
                        return "POWER"  # Probablemente ON
                    elif params[1] <= 1:  # Centro cerca de 0
                        return "POWER"  # Probablemente OFF
                    else:
                        return "DURATION"  # Es una variable de duración

        return "UNKNOWN"

    def get_variable_from_term(self, term_id: str) -> str:
        """Obtiene el ID de la variable asociada a un término."""
        return self.term_to_var.get(term_id, "UNKNOWN")

    def validate_routine(self, routine: Dict[str, Any]) -> None:
        """Valida una rutina completa."""
        routine_name = routine.get('name', 'UNKNOWN')
        routine_id = routine.get('_id', 'UNKNOWN')
        steps = routine.get('steps', [])

        # Track variables usadas en esta rutina
        power_vars_used: Dict[str, List[int]] = defaultdict(list)  # var_id -> [step_indices]
        duration_vars_used: Dict[str, List[int]] = defaultdict(list)

        for idx, step in enumerate(steps):
            power_term = step.get('power_term_id')
            duration_term = step.get('duration_term_id')

            # 1️⃣ VALIDAR CORRESPONDENCIA TIPO ACTUADOR vs COMANDO
            if power_term:
                var_id = self.get_variable_from_term(power_term)
                actuator_type = self.get_actuator_type(var_id)
                term_type = self.get_term_value_type(power_term)
                var_name = self.var_by_id.get(var_id, {}).get('name', 'UNKNOWN')
                term_label = self.term_by_id.get(power_term, {}).get('label', 'UNKNOWN')

                # Registrar uso
                power_vars_used[var_id].append(idx)

                # Validar coherencia
                if actuator_type == "PWM" and term_type == "POWER":
                    self.errors.append({
                        'type': 'TYPE_MISMATCH',
                        'severity': 'ERROR',
                        'routine': routine_name,
                        'routine_id': routine_id,
                        'step': idx,
                        'variable': var_name,
                        'variable_id': var_id,
                        'issue': f'Actuador PWM usando término ON/OFF ({term_label})',
                        'expected': 'Debe usar dutyCycle (término numérico)',
                        'term_id': power_term
                    })
                elif actuator_type == "DIGITAL" and term_type == "DUTYCYCLE":
                    self.errors.append({
                        'type': 'TYPE_MISMATCH',
                        'severity': 'ERROR',
                        'routine': routine_name,
                        'routine_id': routine_id,
                        'step': idx,
                        'variable': var_name,
                        'variable_id': var_id,
                        'issue': f'Actuador DIGITAL usando dutyCycle ({term_label})',
                        'expected': 'Debe usar power (ON/OFF)',
                        'term_id': power_term
                    })

            if duration_term:
                var_id = self.get_variable_from_term(duration_term)
                duration_vars_used[var_id].append(idx)

        # 2️⃣ VALIDAR REDUNDANCIA Y LÓGICA INCORRECTA
        is_emergency = 'emergencia' in routine_name.lower()

        for var_id, step_indices in power_vars_used.items():
            if len(step_indices) > 1:
                var_name = self.var_by_id.get(var_id, {}).get('name', 'UNKNOWN')

                # Obtener términos usados
                terms_used = []
                for step_idx in step_indices:
                    term_id = steps[step_idx].get('power_term_id')
                    term_label = self.term_by_id.get(term_id, {}).get('label', 'UNKNOWN')
                    terms_used.append((step_idx, term_id, term_label))

                # Verificar si hay pares ON/OFF consecutivos
                has_on_off_pair = False
                for i in range(len(terms_used) - 1):
                    label1 = terms_used[i][2].upper()
                    label2 = terms_used[i+1][2].upper()
                    if (label1 == 'ON' and label2 == 'OFF') or (label1 == 'OFF' and label2 == 'ON'):
                        has_on_off_pair = True
                        break

                if has_on_off_pair and not is_emergency:
                    self.errors.append({
                        'type': 'REDUNDANCY',
                        'severity': 'ERROR',
                        'routine': routine_name,
                        'routine_id': routine_id,
                        'variable': var_name,
                        'variable_id': var_id,
                        'issue': f'Rutina NO emergencia contiene par ON/OFF del mismo actuador',
                        'steps': step_indices,
                        'details': f'Steps {step_indices} usan {var_name} con términos {[t[2] for t in terms_used]}'
                    })

                # Verificar duplicados idénticos
                unique_terms = set(t[1] for t in terms_used)
                if len(unique_terms) < len(terms_used):
                    self.warnings.append({
                        'type': 'REDUNDANCY',
                        'severity': 'WARNING',
                        'routine': routine_name,
                        'routine_id': routine_id,
                        'variable': var_name,
                        'variable_id': var_id,
                        'issue': f'Steps duplicados con mismo término en {var_name}',
                        'steps': step_indices,
                        'recommendation': 'Consolidar en un solo step'
                    })

        # 3️⃣ VALIDAR UNICIDAD DE outputVariable
        # Ya validado en el punto anterior

    def validate_all(self) -> None:
        """Ejecuta todas las validaciones."""
        print("="*80)
        print("VALIDACIÓN EXHAUSTIVA DE LÓGICA DE RUTINAS - FUZZY SERVICE")
        print("="*80)
        print()

        for routine in self.routines:
            self.validate_routine(routine)

        # Generar reporte
        self.print_report()

    def print_report(self) -> None:
        """Imprime el reporte de validación."""
        print("\n" + "="*80)
        print("1️⃣ ERRORES DE CORRESPONDENCIA TIPO ACTUADOR vs COMANDO")
        print("="*80)

        type_errors = [e for e in self.errors if e['type'] == 'TYPE_MISMATCH']
        if type_errors:
            for err in type_errors:
                print(f"\n❌ ERROR en rutina '{err['routine']}' (ID: {err['routine_id']})")
                print(f"   Step {err['step']}: Variable '{err['variable']}' (ID: {err['variable_id']})")
                print(f"   Problema: {err['issue']}")
                print(f"   Esperado: {err['expected']}")
                print(f"   Term ID: {err['term_id']}")
        else:
            print("\n✅ No se detectaron errores de correspondencia tipo-comando")

        print("\n" + "="*80)
        print("2️⃣ ERRORES DE REDUNDANCIA Y LÓGICA INCORRECTA")
        print("="*80)

        redundancy_errors = [e for e in self.errors if e['type'] == 'REDUNDANCY']
        if redundancy_errors:
            for err in redundancy_errors:
                print(f"\n❌ ERROR en rutina '{err['routine']}' (ID: {err['routine_id']})")
                print(f"   Variable: '{err['variable']}' (ID: {err['variable_id']})")
                print(f"   Problema: {err['issue']}")
                print(f"   Detalles: {err['details']}")
        else:
            print("\n✅ No se detectaron errores de redundancia o lógica incorrecta")

        print("\n" + "="*80)
        print("3️⃣ ADVERTENCIAS DE UNICIDAD Y CONSISTENCIA")
        print("="*80)

        if self.warnings:
            for warn in self.warnings:
                print(f"\n⚠️  ADVERTENCIA en rutina '{warn['routine']}' (ID: {warn['routine_id']})")
                print(f"   Variable: '{warn['variable']}' (ID: {warn['variable_id']})")
                print(f"   Problema: {warn['issue']}")
                print(f"   Recomendación: {warn['recommendation']}")
        else:
            print("\n✅ No se detectaron advertencias de unicidad")

        print("\n" + "="*80)
        print("RESUMEN DE VALIDACIÓN")
        print("="*80)
        print(f"\nTotal de rutinas validadas: {len(self.routines)}")
        print(f"Errores críticos detectados: {len(self.errors)}")
        print(f"Advertencias detectadas: {len(self.warnings)}")

        if not self.errors and not self.warnings:
            print("\n🎉 ¡TODAS LAS VALIDACIONES PASARON EXITOSAMENTE!")
        elif self.errors:
            print(f"\n⚠️  SE DETECTARON {len(self.errors)} ERRORES CRÍTICOS QUE DEBEN CORREGIRSE")

        print("\n" + "="*80)


if __name__ == "__main__":
    validator = RoutineLogicValidator()
    validator.validate_all()
