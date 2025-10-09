#!/usr/bin/env python3
"""
Script de verificación para la actualización del sensor BH1750 en el fuzzy-service.
Valida que los términos lingüísticos y las reglas estén correctamente configurados.
"""

import sys
from typing import Dict, List, Any

# Agregar el directorio raíz al path
sys.path.insert(0, '/home/moredev/dev/hydroespinaca/software-project/fuzzy-service')

from FuzzyService.Infrastructure.Configuration.SeedData import SeedDataConfig


def validate_luminosity_variable(variables: List[Dict[str, Any]]) -> Dict[str, Any]:
    """Valida la configuración de la variable Luminosity."""
    luminosity_var = None
    for var in variables:
        if var["_id"] == "68e05364d86d6edc39982870":
            luminosity_var = var
            break

    if not luminosity_var:
        return {"valid": False, "error": "Variable Luminosity no encontrada"}

    # Verificar campos obligatorios
    checks = {
        "name": luminosity_var.get("name") == "Luminosity",
        "reference_id": luminosity_var.get("reference_id") == "688970837f02137645d58395",
        "type": luminosity_var.get("type") == "input",
        "defuzzification_threshold": luminosity_var.get("defuzzification_threshold") == 50,
    }

    return {
        "valid": all(checks.values()),
        "checks": checks,
        "variable": luminosity_var
    }


def validate_luminosity_terms(terms: List[Dict[str, Any]]) -> Dict[str, Any]:
    """Valida los términos lingüísticos de Luminosity."""
    luminosity_terms = [t for t in terms if t["variable_id"] == "68e05364d86d6edc39982870"]

    if len(luminosity_terms) != 3:
        return {
            "valid": False,
            "error": f"Se esperaban 3 términos, se encontraron {len(luminosity_terms)}"
        }

    expected_terms = {
        "68e05364d86d6edc39982880": {
            "label": "bajaLuminosidad",
            "parameters": [0.0, 5000.0, 10000.0],
            "universe_min": 0.0,
            "universe_max": 65535.0
        },
        "68e05364d86d6edc39982881": {
            "label": "luminosidadNormal",
            "parameters": [9000.0, 11000.0, 13000.0],
            "universe_min": 0.0,
            "universe_max": 65535.0
        },
        "68e05364d86d6edc39982882": {
            "label": "altaLuminosidad",
            "parameters": [12000.0, 38767.5, 65535.0],
            "universe_min": 0.0,
            "universe_max": 65535.0
        }
    }

    term_checks = {}
    for term in luminosity_terms:
        term_id = term["_id"]
        expected = expected_terms.get(term_id)

        if not expected:
            term_checks[term_id] = {"valid": False, "error": "Término inesperado"}
            continue

        mf = term["membership_function"]
        checks = {
            "label": term["label"] == expected["label"],
            "parameters": mf["parameters"] == expected["parameters"],
            "universe_min": mf["universe_min"] == expected["universe_min"],
            "universe_max": mf["universe_max"] == expected["universe_max"],
            "function_type": mf["function_type"] == "triangular"
        }

        term_checks[term_id] = {
            "valid": all(checks.values()),
            "checks": checks,
            "label": term["label"]
        }

    return {
        "valid": all(t["valid"] for t in term_checks.values()),
        "term_checks": term_checks,
        "terms": luminosity_terms
    }


def validate_obsolete_removed(variables: List[Dict[str, Any]], terms: List[Dict[str, Any]]) -> Dict[str, Any]:
    """Verifica que Luminosity Clear haya sido eliminada."""
    obsolete_var = any(v["_id"] == "68e05364d86d6edc39982874" for v in variables)
    obsolete_terms = [t for t in terms if t["variable_id"] == "68e05364d86d6edc39982874"]

    return {
        "valid": not obsolete_var and len(obsolete_terms) == 0,
        "obsolete_variable_found": obsolete_var,
        "obsolete_terms_found": len(obsolete_terms)
    }


def validate_rules(rules: List[Dict[str, Any]]) -> Dict[str, Any]:
    """Valida las reglas de luminosidad (R9 y R10)."""
    rule_9 = None
    rule_10 = None

    for rule in rules:
        if rule["_id"] == "68e05365d86d6edc39982918":
            rule_9 = rule
        elif rule["_id"] == "68e05365d86d6edc39982919":
            rule_10 = rule

    if not rule_9 or not rule_10:
        return {"valid": False, "error": "No se encontraron las reglas 9 y 10"}

    # Validar Regla 9: solo debe tener condición de Luminosity
    r9_checks = {
        "conditions_count": len(rule_9["conditions"]) == 1,
        "uses_luminosity": rule_9["conditions"][0]["variable_id"] == "68e05364d86d6edc39982870",
        "no_obsolete_clear": all(c["variable_id"] != "68e05364d86d6edc39982874" for c in rule_9["conditions"]),
        "connectors_empty": len(rule_9["connectors"]) == 0,
    }

    # Validar Regla 10: solo debe tener condición de Luminosity
    r10_checks = {
        "conditions_count": len(rule_10["conditions"]) == 1,
        "uses_luminosity": rule_10["conditions"][0]["variable_id"] == "68e05364d86d6edc39982870",
        "no_obsolete_clear": all(c["variable_id"] != "68e05364d86d6edc39982874" for c in rule_10["conditions"]),
        "connectors_empty": len(rule_10["connectors"]) == 0,
    }

    return {
        "valid": all(r9_checks.values()) and all(r10_checks.values()),
        "rule_9": {"valid": all(r9_checks.values()), "checks": r9_checks, "name": rule_9["name"]},
        "rule_10": {"valid": all(r10_checks.values()), "checks": r10_checks, "name": rule_10["name"]}
    }


def main():
    """Ejecuta todas las validaciones."""
    print("=" * 80)
    print("VERIFICACIÓN DE ACTUALIZACIÓN DEL SENSOR BH1750")
    print("=" * 80)
    print()

    # Cargar configuración
    try:
        variables = SeedDataConfig.get_variables_config()
        terms = SeedDataConfig.get_terms_config()
        rules = SeedDataConfig.get_rules_config()
    except Exception as e:
        print(f"❌ ERROR al cargar configuración: {e}")
        return 1

    all_valid = True

    # 1. Validar variable Luminosity
    print("1️⃣  Validando variable Luminosity...")
    var_result = validate_luminosity_variable(variables)
    if var_result["valid"]:
        print("   ✅ Variable Luminosity correctamente configurada")
        print(f"      - Nombre: {var_result['variable']['name']}")
        print(f"      - Reference ID: {var_result['variable']['reference_id']} (BH1750)")
        print(f"      - Defuzzification Threshold: {var_result['variable']['defuzzification_threshold']}")
    else:
        print(f"   ❌ {var_result.get('error', 'Error en validación de variable')}")
        for check, status in var_result.get("checks", {}).items():
            print(f"      - {check}: {'✅' if status else '❌'}")
        all_valid = False
    print()

    # 2. Validar términos lingüísticos
    print("2️⃣  Validando términos lingüísticos...")
    terms_result = validate_luminosity_terms(terms)
    if terms_result["valid"]:
        print("   ✅ Términos lingüísticos correctamente configurados")
        for term_id, term_check in terms_result["term_checks"].items():
            print(f"      - {term_check['label']}: ✅")
        print(f"      - Rango universe: 0-65535 lux")
        print(f"      - bajaLuminosidad: 0-10000 lux (activar luz)")
        print(f"      - luminosidadNormal: 9000-13000 lux (zona óptima)")
        print(f"      - altaLuminosidad: 12000-65535 lux (apagar luz)")
    else:
        print(f"   ❌ {terms_result.get('error', 'Error en validación de términos')}")
        for term_id, term_check in terms_result.get("term_checks", {}).items():
            status = '✅' if term_check["valid"] else '❌'
            print(f"      - {term_check.get('label', term_id)}: {status}")
        all_valid = False
    print()

    # 3. Validar eliminación de Luminosity Clear
    print("3️⃣  Validando eliminación de variable obsoleta...")
    obsolete_result = validate_obsolete_removed(variables, terms)
    if obsolete_result["valid"]:
        print("   ✅ Variable 'Luminosity Clear' eliminada correctamente")
        print("   ✅ Términos obsoletos eliminados")
    else:
        print("   ❌ Se encontraron elementos obsoletos:")
        if obsolete_result["obsolete_variable_found"]:
            print("      - Variable 'Luminosity Clear' aún existe")
        if obsolete_result["obsolete_terms_found"] > 0:
            print(f"      - {obsolete_result['obsolete_terms_found']} términos obsoletos encontrados")
        all_valid = False
    print()

    # 4. Validar reglas
    print("4️⃣  Validando reglas difusas...")
    rules_result = validate_rules(rules)
    if rules_result["valid"]:
        print("   ✅ Reglas correctamente actualizadas")
        print(f"      - {rules_result['rule_9']['name']}: ✅")
        print(f"      - {rules_result['rule_10']['name']}: ✅")
    else:
        print("   ❌ Error en validación de reglas:")
        for rule_key in ["rule_9", "rule_10"]:
            rule = rules_result.get(rule_key, {})
            status = '✅' if rule.get("valid") else '❌'
            print(f"      - {rule.get('name', rule_key)}: {status}")
            if not rule.get("valid"):
                for check, status in rule.get("checks", {}).items():
                    print(f"         * {check}: {'✅' if status else '❌'}")
        all_valid = False
    print()

    # Resumen final
    print("=" * 80)
    if all_valid:
        print("✅ TODAS LAS VALIDACIONES PASARON")
        print()
        print("📊 Resumen de cambios:")
        print("   • Variable renombrada: 'Luminosity Index' → 'Luminosity'")
        print("   • Reference ID: 688970837f02137645d58395 (BH1750)")
        print("   • Rango actualizado: 0-100 → 0-65535 lux")
        print("   • Términos actualizados para valores reales de lux")
        print("   • Variable obsoleta eliminada: 'Luminosity Clear'")
        print("   • Reglas simplificadas: R9 y R10 usan solo BH1750")
        print()
        print("🚀 El servicio está listo para usar el sensor BH1750")
        return 0
    else:
        print("❌ ALGUNAS VALIDACIONES FALLARON")
        print("   Por favor revisa los errores reportados arriba")
        return 1


if __name__ == "__main__":
    sys.exit(main())
