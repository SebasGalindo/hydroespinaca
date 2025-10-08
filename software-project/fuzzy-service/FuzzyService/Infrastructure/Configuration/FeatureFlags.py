"""
Feature flags para control de funcionalidades experimentales y migraciones.

Este módulo centraliza los feature flags del servicio fuzzy, permitiendo
activar/desactivar funcionalidades mediante variables de entorno.
"""

import os
from typing import Optional


class FeatureFlags:
    """Configuración de feature flags mediante variables de entorno."""

    @staticmethod
    def _get_bool_env(key: str, default: bool = False) -> bool:
        """
        Obtiene una variable de entorno como booleano.

        Args:
            key: Nombre de la variable de entorno
            default: Valor por defecto si no está definida

        Returns:
            True si el valor es "true", "1", "yes", "y" (case-insensitive)
        """
        value = os.getenv(key)
        if value is None:
            return default
        return value.lower() in {"true", "1", "yes", "y"}

    # -------------------------
    # Migración Mamdani
    # -------------------------
    @staticmethod
    def use_mamdani_consequents() -> bool:
        """
        Feature flag para usar consecuentes directos Mamdani.

        Cuando está habilitado (true):
        - Las reglas usan el campo 'consequents' (List[RuleConsequent])
        - Se usa defuzzify_from_consequents() para inferencia
        - Permite agregación multi-regla por variable

        Cuando está deshabilitado (false, default):
        - Las reglas usan el campo 'consequent' (FuzzyRoutineId)
        - Se usa defuzzify_routines() para inferencia (legacy)
        - Comportamiento basado en rutinas

        Variable de entorno: FUZZY_USE_MAMDANI_CONSEQUENTS
        Default: false (para compatibilidad con producción actual)
        """
        return FeatureFlags._get_bool_env("FUZZY_USE_MAMDANI_CONSEQUENTS", default=False)

    # -------------------------
    # Configuración de seeding y mantenimiento
    # -------------------------
    @staticmethod
    def ensure_indexes_on_startup() -> bool:
        """
        Controla si se crean índices MongoDB al iniciar el servicio.

        Variable de entorno: FUZZY_ENSURE_INDEXES_ON_STARTUP
        Default: true
        """
        return FeatureFlags._get_bool_env("FUZZY_ENSURE_INDEXES_ON_STARTUP", default=True)

    @staticmethod
    def seed_on_startup() -> bool:
        """
        Controla si se ejecuta el seeding de datos al iniciar.

        Variable de entorno: FUZZY_SEED_ON_STARTUP
        Default: depende del entorno (true en Development, false en Production)
        """
        environment = os.getenv("ASPNETCORE_ENVIRONMENT", "Development")
        is_development = environment.lower() == "development"

        # En desarrollo: seed por defecto, a menos que se deshabilite explícitamente
        # En producción: no seed por defecto, a menos que se habilite explícitamente
        env_value = os.getenv("FUZZY_SEED_ON_STARTUP")
        if env_value is not None:
            return FeatureFlags._get_bool_env("FUZZY_SEED_ON_STARTUP")

        return is_development

    # -------------------------
    # Validaciones y debugging
    # -------------------------
    @staticmethod
    def enable_validation_warnings() -> bool:
        """
        Habilita warnings detallados durante validaciones.

        Variable de entorno: FUZZY_ENABLE_VALIDATION_WARNINGS
        Default: true en Development, false en Production
        """
        environment = os.getenv("ASPNETCORE_ENVIRONMENT", "Development")
        is_development = environment.lower() == "development"
        return FeatureFlags._get_bool_env("FUZZY_ENABLE_VALIDATION_WARNINGS", default=is_development)

    @staticmethod
    def log_defuzzification_details() -> bool:
        """
        Habilita logging detallado de defuzzificación (performance overhead).

        Variable de entorno: FUZZY_LOG_DEFUZZIFICATION_DETAILS
        Default: false
        """
        return FeatureFlags._get_bool_env("FUZZY_LOG_DEFUZZIFICATION_DETAILS", default=False)

    # -------------------------
    # Helpers
    # -------------------------
    @staticmethod
    def get_all_flags() -> dict:
        """
        Retorna el estado actual de todos los feature flags.

        Útil para debugging y logging de configuración al inicio del servicio.
        """
        return {
            "use_mamdani_consequents": FeatureFlags.use_mamdani_consequents(),
            "ensure_indexes_on_startup": FeatureFlags.ensure_indexes_on_startup(),
            "seed_on_startup": FeatureFlags.seed_on_startup(),
            "enable_validation_warnings": FeatureFlags.enable_validation_warnings(),
            "log_defuzzification_details": FeatureFlags.log_defuzzification_details(),
        }

    @staticmethod
    def log_configuration(logger) -> None:
        """
        Registra la configuración actual de feature flags en el logger.

        Args:
            logger: Logger instance para registrar la configuración
        """
        flags = FeatureFlags.get_all_flags()
        logger.info("Feature Flags Configuration:")
        for flag_name, flag_value in flags.items():
            logger.info(f"  {flag_name}: {flag_value}")
