# Mappers Específicos

Este módulo contiene mappers específicos para cada entidad, siguiendo el patrón de clases con métodos estáticos `to_infra` y `to_domain` para eliminar la duplicación entre las capas Domain e Infrastructure del sistema fuzzy.

## Problema Resuelto

Antes de esta refactorización, existían múltiples conversiones inline dispersas por el código:

- `FuzzyEngineService` tenía métodos `_convert_to_engine_request` y `_convert_to_domain_evaluation`
- Cada repositorio tenía sus propios métodos `_to_domain_*`
- Los DTOs tenían conversiones manuales a entidades
- Duplicación de lógica de mapeo en múltiples lugares

## Solución

### Mappers Específicos por Entidad

Cada entidad tiene su propio mapper con métodos estáticos:

```python
from FuzzyService.Application.Mappers import (
    FuzzyRuleMapper,
    FuzzyVariableMapper,
    FuzzyTermMapper,
    FuzzySystemMapper,
    FuzzyEvaluationMapper,
    FuzzyRoutineMapper
)

# Convertir reglas
infra_rule = FuzzyRuleMapper.to_infra(domain_rule)
domain_rule = FuzzyRuleMapper.to_domain(infra_rule)

# Convertir variables
infra_variable = FuzzyVariableMapper.to_infra(domain_variable)
domain_variable = FuzzyVariableMapper.to_domain(infra_variable, system_id)

# Convertir términos
infra_term = FuzzyTermMapper.to_infra(domain_term)
domain_term = FuzzyTermMapper.to_domain(infra_term, variable_id)

# Convertir evaluaciones
infra_evaluation = FuzzyEvaluationMapper.to_infra_request(domain_evaluation)
domain_evaluation = FuzzyEvaluationMapper.from_rule_evaluation_result(
    engine_response, system_id, inputs
)
```

## Beneficios

1. **Single Source of Truth**: Toda la lógica de conversión está centralizada
2. **Mantenibilidad**: Cambios en el mapeo solo requieren modificar un lugar
3. **Consistencia**: Todas las conversiones siguen las mismas reglas
4. **Testabilidad**: Los mappers pueden ser probados de forma aislada
5. **Reutilización**: Los mappers pueden ser usados en cualquier capa de aplicación

## Uso en el Código

### FuzzyEngineService (Refactorizado)

```python
# Antes
infra_rules = []
for rule in rules:
    # ... lógica de conversión inline ...
    infra_rule = InfraFuzzyRule(...)
    infra_rules.append(infra_rule)

# Después
infra_rules = [FuzzyRuleMapper.to_infra(rule) for rule in rules]
```

### Repositorios

Los repositorios pueden seguir usando sus métodos `_to_domain_*` para conversiones específicas de persistencia, pero para conversiones complejas pueden usar los mappers específicos.

### DTOs

Los DTOs pueden usar los mappers específicos para conversiones más complejas:

```python
def to_entity(self) -> FuzzyRule:
    # Para conversiones simples, mantener lógica inline
    # Para conversiones complejas, usar mappers centralizados
    pass
```

## Extensibilidad

Para agregar nuevos tipos de conversión:

1. Agregar método estático al mapper apropiado
2. Seguir el patrón de nomenclatura `map_*`
3. Incluir validaciones y manejo de errores
4. Documentar el nuevo método
5. Agregar tests unitarios

## Consideraciones de Performance

- Los mappers son stateless y thread-safe
- Métodos estáticos para evitar overhead de instanciación
- Conversiones batch para operaciones en lote
- Validaciones mínimas necesarias (las entidades ya están validadas)

## Testing

Cada mapper debe tener tests unitarios que cubran:

- Conversiones exitosas
- Manejo de valores nulos/opcionales
- Validaciones de entrada
- Conversiones batch
- Casos edge