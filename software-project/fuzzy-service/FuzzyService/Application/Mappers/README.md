# Mappers Centralizados

Este módulo contiene mappers centralizados para eliminar la duplicación de entidades entre las capas Domain e Infrastructure del sistema fuzzy.

## Problema Resuelto

Antes de esta refactorización, existían múltiples conversiones inline dispersas por el código:

- `FuzzyEngineService` tenía métodos `_convert_to_engine_request` y `_convert_to_domain_evaluation`
- Cada repositorio tenía sus propios métodos `_to_domain_*`
- Los DTOs tenían conversiones manuales a entidades
- Duplicación de lógica de mapeo en múltiples lugares

## Solución

### DomainToInfrastructureMapper

Convierte entidades del dominio a estructuras de infraestructura:

```python
from FuzzyService.Application.Mappers import DomainToInfrastructureMapper

# Convertir una regla
infra_rule = DomainToInfrastructureMapper.map_fuzzy_rule(domain_rule)

# Convertir múltiples reglas
infra_rules = DomainToInfrastructureMapper.map_rules_batch(domain_rules)

# Convertir variables y términos
infra_variable = DomainToInfrastructureMapper.map_fuzzy_variable(domain_variable)
infra_term = DomainToInfrastructureMapper.map_fuzzy_term(domain_term)
```

### InfrastructureToDomainMapper

Convierte estructuras de infraestructura a entidades del dominio:

```python
from FuzzyService.Application.Mappers import InfrastructureToDomainMapper

# Convertir respuesta de evaluación
domain_evaluation = InfrastructureToDomainMapper.map_evaluation_response(
    engine_response, system_id, inputs
)

# Convertir entidades individuales
domain_rule = InfrastructureToDomainMapper.map_fuzzy_rule(infra_rule, system_id)
domain_variable = InfrastructureToDomainMapper.map_fuzzy_variable(infra_variable)
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
infra_rules = DomainToInfrastructureMapper.map_rules_batch(rules)
```

### Repositorios

Los repositorios pueden seguir usando sus métodos `_to_domain_*` para conversiones específicas de persistencia, pero para conversiones complejas pueden usar los mappers centralizados.

### DTOs

Los DTOs pueden usar los mappers para conversiones más complejas:

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