#!/usr/bin/env python3
"""Script para verificar que el payload generado para actuator-service es correcto."""

import json


def validate_step_payload(step: dict, step_index: int) -> list[str]:
    """Valida que un step tenga la estructura correcta."""
    errors = []

    # Verificar campos requeridos
    if "outputVariable" not in step:
        errors.append(f"Step {step_index}: falta campo 'outputVariable'")
    elif not isinstance(step["outputVariable"], str):
        errors.append(f"Step {step_index}: 'outputVariable' debe ser string")
    elif step["outputVariable"].startswith("{") or "$oid" in str(step["outputVariable"]):
        errors.append(f"Step {step_index}: 'outputVariable' no debe ser objeto {{'$oid': ...}}, debe ser string plano")

    if "duration" not in step:
        errors.append(f"Step {step_index}: falta campo 'duration'")
    elif not isinstance(step["duration"], (int, float)):
        errors.append(f"Step {step_index}: 'duration' debe ser número")
    elif step["duration"] <= 0:
        errors.append(f"Step {step_index}: 'duration' debe ser mayor a 0")

    # Verificar que tenga power O dutyCycle (no ambos, no ninguno)
    has_power = "power" in step
    has_duty_cycle = "dutyCycle" in step

    if not has_power and not has_duty_cycle:
        errors.append(f"Step {step_index}: debe tener 'power' O 'dutyCycle'")

    if has_power and has_duty_cycle:
        errors.append(f"Step {step_index}: no debe tener ambos 'power' Y 'dutyCycle'")

    # Validar power si existe
    if has_power:
        if step["power"] not in ["ON", "OFF"]:
            errors.append(f"Step {step_index}: 'power' debe ser 'ON' o 'OFF', no {step['power']}")

    # Validar dutyCycle si existe
    if has_duty_cycle:
        if not isinstance(step["dutyCycle"], (int, float)):
            errors.append(f"Step {step_index}: 'dutyCycle' debe ser número")
        elif step["dutyCycle"] < 0 or step["dutyCycle"] > 100:
            errors.append(f"Step {step_index}: 'dutyCycle' debe estar entre 0-100")

    # Verificar que NO tenga campos legacy
    legacy_fields = ["actuator", "outputVariableId", "device_id"]
    for field in legacy_fields:
        if field in step:
            errors.append(f"Step {step_index}: NO debe tener campo legacy '{field}'")

    return errors


def validate_routine_payload(routine: dict, routine_index: int) -> list[str]:
    """Valida que una rutina tenga la estructura correcta."""
    errors = []

    # Verificar campos requeridos
    if "routineId" not in routine:
        errors.append(f"Rutina {routine_index}: falta campo 'routineId'")
    elif not isinstance(routine["routineId"], str):
        errors.append(f"Rutina {routine_index}: 'routineId' debe ser string")
    elif routine["routineId"].strip() == "":
        errors.append(f"Rutina {routine_index}: 'routineId' no puede estar vacío")

    if "steps" not in routine:
        errors.append(f"Rutina {routine_index}: falta campo 'steps'")
    elif not isinstance(routine["steps"], list):
        errors.append(f"Rutina {routine_index}: 'steps' debe ser lista")
    elif len(routine["steps"]) == 0:
        errors.append(f"Rutina {routine_index}: 'steps' no puede estar vacía")
    else:
        # Validar cada step
        for step_idx, step in enumerate(routine["steps"]):
            step_errors = validate_step_payload(step, step_idx)
            errors.extend([f"Rutina '{routine.get('routineId', routine_index)}' - {err}" for err in step_errors])

    return errors


def validate_full_payload(payload: dict) -> tuple[bool, list[str]]:
    """Valida el payload completo enviado a actuator-service."""
    errors = []

    # Verificar estructura raíz
    if not isinstance(payload, dict):
        errors.append("Payload debe ser un objeto/diccionario")
        return False, errors

    if "routines" not in payload:
        errors.append("Payload debe tener campo 'routines'")
        return False, errors

    if not isinstance(payload["routines"], list):
        errors.append("'routines' debe ser una lista")
        return False, errors

    if len(payload["routines"]) == 0:
        errors.append("'routines' no puede estar vacía")
        return False, errors

    # Validar cada rutina
    for routine_idx, routine in enumerate(payload["routines"]):
        routine_errors = validate_routine_payload(routine, routine_idx)
        errors.extend(routine_errors)

    return len(errors) == 0, errors


# Ejemplo de payload correcto
EXAMPLE_CORRECT = {
    "routines": [
        {
            "routineId": "ControlTemperatura",
            "steps": [
                {
                    "outputVariable": "6883fff7b079309f3ba4f240",
                    "dutyCycle": 75,
                    "duration": 300
                }
            ]
        },
        {
            "routineId": "ControlIluminacion",
            "steps": [
                {
                    "outputVariable": "6883fff7b079309f3ba4f243",
                    "power": "ON",
                    "duration": 7200
                }
            ]
        }
    ]
}

# Ejemplo de payload INCORRECTO (con errores legacy)
EXAMPLE_INCORRECT = {
    "routines": [
        {
            "routineId": "507f1f77bcf86cd799439011",  # Debería ser nombre legible
            "steps": [
                {
                    "actuator": {"$oid": "6883fff7b079309f3ba4f240"},  # ❌ Debe ser outputVariable (string plano)
                    "power": 75,  # ❌ Debería ser dutyCycle o "ON"/"OFF"
                    "duration": 300
                }
            ]
        }
    ]
}


if __name__ == "__main__":
    print("=" * 80)
    print("🔍 VERIFICACIÓN DE PAYLOAD PARA ACTUATOR-SERVICE")
    print("=" * 80)
    print()

    print("📝 Validando payload CORRECTO:")
    is_valid, errors = validate_full_payload(EXAMPLE_CORRECT)
    if is_valid:
        print("✅ Payload correcto - SIN ERRORES")
        print(f"\n{json.dumps(EXAMPLE_CORRECT, indent=2)}")
    else:
        print("❌ Payload con errores:")
        for error in errors:
            print(f"   - {error}")

    print("\n" + "=" * 80)
    print("📝 Validando payload INCORRECTO (legacy):")
    is_valid, errors = validate_full_payload(EXAMPLE_INCORRECT)
    if is_valid:
        print("✅ Sin errores (esto no debería pasar)")
    else:
        print("❌ Errores detectados (esperado):")
        for error in errors:
            print(f"   - {error}")

    print("\n" + "=" * 80)
    print("📋 FORMATO ESPERADO:")
    print("=" * 80)
    print("""
{
  "routines": [
    {
      "routineId": "string",           // Nombre legible de la rutina
      "steps": [
        {
          "outputVariable": "string",  // ObjectId del control_output (string plano)
          "power": "ON" | "OFF",       // Solo para DIGITAL (mutuamente exclusivo con dutyCycle)
          "dutyCycle": number,         // 0-100, solo para PWM (mutuamente exclusivo con power)
          "duration": number           // Segundos (siempre requerido)
        }
      ]
    }
  ]
}
    """)

    print("🚫 CAMPOS LEGACY A EVITAR:")
    print("   - actuator (usar outputVariable)")
    print("   - outputVariableId (usar outputVariable)")
    print("   - device_id (usar reference_id en fuzzy variables)")
    print("   - Objetos {'$oid': '...'} (usar strings planos)")
