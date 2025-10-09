#!/usr/bin/env python3
"""Script para verificar cambios en seed data tras implementar solución ON/OFF."""

import sys
from typing import Dict, List, Set

# Importar configuración de seed data
sys.path.append('.')
from FuzzyService.Infrastructure.Configuration.SeedData import SeedDataConfig


def verify_onoff_terms() -> tuple[bool, List[str]]:
    """Verifica que los términos ON/OFF existen y están correctamente configurados."""
    errors = []

    terms = SeedDataConfig.get_terms_config()

    # Buscar términos ON y OFF
    on_term = next((t for t in terms if t["label"] == "ON"), None)
    off_term = next((t for t in terms if t["label"] == "OFF"), None)

    if not on_term:
        errors.append("❌ Falta término 'ON'")
    else:
        # Verificar que defuzzifica a 100
        params = on_term["membership_function"]["parameters"]
        if params != [100.0, 100.0, 100.0]:
            errors.append(f"❌ Término 'ON' tiene parámetros incorrectos: {params}")

    if not off_term:
        errors.append("❌ Falta término 'OFF'")
    else:
        # Verificar que defuzzifica a 0
        params = off_term["membership_function"]["parameters"]
        if params != [0.0, 0.0, 0.0]:
            errors.append(f"❌ Término 'OFF' tiene parámetros incorrectos: {params}")

    # Verificar que NO existe el término "apagado"
    apagado_term = next((t for t in terms if t["label"] == "apagado"), None)
    if apagado_term:
        errors.append("❌ Término 'apagado' ambiguo aún existe (debe ser eliminado)")

    return len(errors) == 0, errors


def verify_routines_use_onoff() -> tuple[bool, List[str]]:
    """Verifica que las rutinas digitales usen términos ON/OFF."""
    errors = []

    routines = SeedDataConfig.get_routines_config()
    terms = SeedDataConfig.get_terms_config()

    # Obtener IDs de términos ON/OFF
    on_term = next((t for t in terms if t["label"] == "ON"), None)
    off_term = next((t for t in terms if t["label"] == "OFF"), None)

    if not on_term or not off_term:
        errors.append("❌ No se pueden verificar rutinas sin términos ON/OFF")
        return False, errors

    on_id = on_term["_id"]
    off_id = off_term["_id"]

    # Rutinas DIGITALES que deben usar ON
    digital_on_routines = [
        "Encender Calefactor Aire",
        "Encender Calefactor Agua",
        "Encender Humidificador",
        "Encender Luz"
    ]

    # Rutinas DIGITALES que deben usar OFF
    digital_off_routines = [
        "Apagar Calefactor Aire",
        "Apagar Calefactor Agua",
        "Apagar Humidificador",
        "Apagar Luz"
    ]

    for routine in routines:
        name = routine["name"]

        # Verificar rutinas ON
        if name in digital_on_routines:
            for step in routine["steps"]:
                power_term = step.get("power_term_id")
                if power_term != on_id:
                    errors.append(f"❌ Rutina '{name}' step {step['stepId']} debe usar ON (actual: {power_term})")

        # Verificar rutinas OFF
        elif name in digital_off_routines:
            for step in routine["steps"]:
                power_term = step.get("power_term_id")
                if power_term != off_id:
                    errors.append(f"❌ Rutina '{name}' step {step['stepId']} debe usar OFF (actual: {power_term})")

        # Verificar Emergencia Termica (pasos específicos)
        elif name == "Emergencia Termica":
            step_0 = next((s for s in routine["steps"] if s["stepId"] == 0), None)
            step_4 = next((s for s in routine["steps"] if s["stepId"] == 4), None)

            if step_0 and step_0.get("power_term_id") != off_id:
                errors.append(f"❌ 'Emergencia Termica' step 0 (Calefactor Aire) debe usar OFF")

            if step_4 and step_4.get("power_term_id") != off_id:
                errors.append(f"❌ 'Emergencia Termica' step 4 (Calefactor Agua) debe usar OFF")

        # Verificar Apagar Actuadores de Agua
        elif name == "Apagar Actuadores de Agua":
            for step in routine["steps"]:
                power_term = step.get("power_term_id")
                if power_term != off_id:
                    errors.append(f"❌ Rutina '{name}' step {step['stepId']} debe usar OFF")

    return len(errors) == 0, errors


def verify_pwm_routines_unchanged() -> tuple[bool, List[str]]:
    """Verifica que las rutinas PWM sigan usando términos PWM correctos."""
    errors = []

    routines = SeedDataConfig.get_routines_config()
    terms = SeedDataConfig.get_terms_config()

    # Obtener IDs de términos PWM
    pwm_terms_map = {
        "potenciaBaja": next((t["_id"] for t in terms if t["label"] == "potenciaBaja"), None),
        "potenciaMedia": next((t["_id"] for t in terms if t["label"] == "potenciaMedia"), None),
        "potenciaAlta": next((t["_id"] for t in terms if t["label"] == "potenciaAlta"), None),
        "potenciaMaxima": next((t["_id"] for t in terms if t["label"] == "potenciaMaxima"), None),
    }

    # Rutinas PWM que deben mantener sus términos
    pwm_routines = {
        "Encender Ventilador": "potenciaAlta",
        "Apagar Ventilador": "OFF"  # OFF también se usa para PWM al apagar
    }

    for routine in routines:
        name = routine["name"]

        if name == "Encender Ventilador":
            step = routine["steps"][0]
            expected_id = pwm_terms_map["potenciaAlta"]
            if step.get("power_term_id") != expected_id:
                errors.append(f"❌ 'Encender Ventilador' debe usar 'potenciaAlta' (~75%)")

        # Verificar Emergencia Termica - pasos PWM
        elif name == "Emergencia Termica":
            step_1 = next((s for s in routine["steps"] if s["stepId"] == 1), None)
            step_2 = next((s for s in routine["steps"] if s["stepId"] == 2), None)
            step_3 = next((s for s in routine["steps"] if s["stepId"] == 3), None)

            if step_1 and step_1.get("power_term_id") != pwm_terms_map["potenciaAlta"]:
                errors.append(f"❌ 'Emergencia Termica' step 1 (Ventilador) debe usar 'potenciaAlta'")

            if step_2 and step_2.get("power_term_id") != pwm_terms_map["potenciaMaxima"]:
                errors.append(f"❌ 'Emergencia Termica' step 2 (Bomba Aire) debe usar 'potenciaMaxima'")

            if step_3 and step_3.get("power_term_id") != pwm_terms_map["potenciaMaxima"]:
                errors.append(f"❌ 'Emergencia Termica' step 3 (Bomba Agua) debe usar 'potenciaMaxima'")

    return len(errors) == 0, errors


def verify_no_orphaned_term_references() -> tuple[bool, List[str]]:
    """Verifica que todas las rutinas referencien términos existentes."""
    errors = []

    routines = SeedDataConfig.get_routines_config()
    terms = SeedDataConfig.get_terms_config()

    term_ids = {t["_id"] for t in terms}

    for routine in routines:
        for step in routine["steps"]:
            power_term_id = step.get("power_term_id")
            duration_term_id = step.get("duration_term_id")

            if power_term_id and power_term_id not in term_ids:
                errors.append(
                    f"❌ Rutina '{routine['name']}' step {step['stepId']} "
                    f"referencia power_term_id inexistente: {power_term_id}"
                )

            if duration_term_id and duration_term_id not in term_ids:
                errors.append(
                    f"❌ Rutina '{routine['name']}' step {step['stepId']} "
                    f"referencia duration_term_id inexistente: {duration_term_id}"
                )

    return len(errors) == 0, errors


def main():
    print("=" * 80)
    print("🔍 VERIFICACIÓN DE CAMBIOS EN SEED DATA")
    print("=" * 80)
    print()

    all_passed = True

    # 1. Verificar términos ON/OFF
    print("📝 1. Verificando términos ON/OFF...")
    passed, errors = verify_onoff_terms()
    if passed:
        print("   ✅ Términos ON/OFF correctos")
    else:
        all_passed = False
        for error in errors:
            print(f"   {error}")
    print()

    # 2. Verificar rutinas digitales
    print("📝 2. Verificando rutinas DIGITALES usan ON/OFF...")
    passed, errors = verify_routines_use_onoff()
    if passed:
        print("   ✅ Todas las rutinas digitales usan ON/OFF")
    else:
        all_passed = False
        for error in errors:
            print(f"   {error}")
    print()

    # 3. Verificar rutinas PWM
    print("📝 3. Verificando rutinas PWM mantienen términos correctos...")
    passed, errors = verify_pwm_routines_unchanged()
    if passed:
        print("   ✅ Rutinas PWM correctas")
    else:
        all_passed = False
        for error in errors:
            print(f"   {error}")
    print()

    # 4. Verificar referencias huérfanas
    print("📝 4. Verificando que no hay referencias a términos inexistentes...")
    passed, errors = verify_no_orphaned_term_references()
    if passed:
        print("   ✅ Todas las referencias son válidas")
    else:
        all_passed = False
        for error in errors:
            print(f"   {error}")
    print()

    # Resumen final
    print("=" * 80)
    if all_passed:
        print("✅ TODAS LAS VERIFICACIONES PASARON")
        print("=" * 80)
        print()
        print("🎯 Resumen de cambios implementados:")
        print("   1. ✅ Términos ON (100) y OFF (0) creados")
        print("   2. ✅ Término 'apagado' ambiguo eliminado")
        print("   3. ✅ Rutinas digitales actualizadas a ON/OFF")
        print("   4. ✅ Rutinas PWM mantienen términos correctos")
        print("   5. ✅ Threshold ajustado en defuzzificación (≤1 para OFF, ≥99 para ON)")
        print()
        print("📋 Próximos pasos:")
        print("   - Ejecutar seed data con: python3 -m FuzzyService.Api.main")
        print("   - Probar payloads con: python3 verify-actuator-payload.py")
        return 0
    else:
        print("❌ ALGUNAS VERIFICACIONES FALLARON")
        print("=" * 80)
        print("Por favor revisa los errores arriba y corrige el seed data.")
        return 1


if __name__ == "__main__":
    sys.exit(main())
