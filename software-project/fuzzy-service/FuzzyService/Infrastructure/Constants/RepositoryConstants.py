"""
Repository Constants Module

This module contains reusable constants for repositories and services
to avoid duplication and ensure consistency across the codebase.
"""

# HTTP Headers and Content Types
CONTENT_TYPE_JSON = "application/json"

# MongoDB Query Operators
MONGO_SIZE_OPERATOR = "$size"
MONGO_EXPR_OPERATOR = "$expr"
MONGO_ADD_OPERATOR = "$add"
MONGO_GT_OPERATOR = "$gt"
MONGO_GTE_OPERATOR = "$gte"
MONGO_LTE_OPERATOR = "$lte"
MONGO_AND_OPERATOR = "$and"

# MongoDB Field Paths
FIELD_ACTIVATED_RULES_RULE_ID = "activated_rules.ruleId"
FIELD_CONDITIONS_VARIABLE_ID = "conditions.variableId"

# Error Messages
ERROR_SYSTEM_NOT_FOUND = "Sistema no encontrado"
ERROR_VARIABLE_NOT_FOUND = "Variable no encontrada"
ERROR_RULE_NOT_FOUND = "Regla no encontrada"
ERROR_TERM_NOT_FOUND = "Término no encontrado"
ERROR_EVALUATION_NOT_FOUND = "Evaluación no encontrada"
