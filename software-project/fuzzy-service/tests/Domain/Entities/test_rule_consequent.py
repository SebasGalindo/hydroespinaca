import pytest
from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent
from FuzzyService.Domain.ValueObjects import FuzzyVariableId, FuzzyTermId


class TestRuleConsequentCreation:
    """Pruebas para la creación de RuleConsequent."""

    def test_create_rule_consequent_with_single_term(self):
        """Debe crear un consecuente con un solo término."""
        var_id = FuzzyVariableId.generate()
        term_id = FuzzyTermId.generate()
        
        consequent = RuleConsequent(
            variable_id=var_id,
            terms=[term_id]
        )

        assert consequent.variable_id == var_id
        assert len(consequent.terms) == 1
        assert consequent.terms[0] == term_id

    def test_create_rule_consequent_with_multiple_terms(self):
        """Debe crear un consecuente con múltiples términos."""
        var_id = FuzzyVariableId.generate()
        term_ids = [FuzzyTermId.generate() for _ in range(3)]
        
        consequent = RuleConsequent(
            variable_id=var_id,
            terms=term_ids
        )

        assert len(consequent.terms) == 3

    def test_default_aggregation_method_is_max(self):
        """Debe usar 'max' como método de agregación por defecto."""
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )

        assert consequent.aggregation_method == "max"

    def test_create_with_custom_aggregation_method(self):
        """Debe permitir método de agregación personalizado."""
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()],
            aggregation_method="sum"
        )

        assert consequent.aggregation_method == "sum"


class TestRuleConsequentValidation:
    """Pruebas para validaciones de RuleConsequent."""

    def test_empty_terms_list_raises_error(self):
        """Debe lanzar error si la lista de términos está vacía."""
        with pytest.raises(ValueError, match="debe tener al menos un término"):
            RuleConsequent(
                variable_id=FuzzyVariableId.generate(),
                terms=[]
            )

    def test_duplicate_terms_raises_error(self):
        """Debe lanzar error si hay términos duplicados."""
        term_id = FuzzyTermId.generate()
        
        with pytest.raises(ValueError, match="No se permiten términos duplicados"):
            RuleConsequent(
                variable_id=FuzzyVariableId.generate(),
                terms=[term_id, term_id]
            )

    def test_invalid_aggregation_method_raises_error(self):
        """Debe lanzar error para método de agregación inválido."""
        with pytest.raises(ValueError, match="aggregation_method inválido"):
            RuleConsequent(
                variable_id=FuzzyVariableId.generate(),
                terms=[FuzzyTermId.generate()],
                aggregation_method="invalid_method"
            )

    def test_valid_aggregation_methods(self):
        """Debe aceptar métodos de agregación válidos."""
        valid_methods = ["max", "sum", "probabilistic_or"]
        
        for method in valid_methods:
            consequent = RuleConsequent(
                variable_id=FuzzyVariableId.generate(),
                terms=[FuzzyTermId.generate()],
                aggregation_method=method
            )
            assert consequent.aggregation_method == method


class TestRuleConsequentTermManagement:
    """Pruebas para gestión de términos en RuleConsequent."""

    def test_add_term_to_consequent(self):
        """Debe agregar un término al consecuente."""
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )
        
        new_term = FuzzyTermId.generate()
        consequent.add_term(new_term)

        assert len(consequent.terms) == 2
        assert new_term in consequent.terms

    def test_add_duplicate_term_does_not_duplicate(self):
        """No debe agregar término duplicado."""
        term_id = FuzzyTermId.generate()
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[term_id]
        )

        consequent.add_term(term_id)

        assert len(consequent.terms) == 1

    def test_remove_term_from_consequent(self):
        """Debe remover un término del consecuente."""
        term1 = FuzzyTermId.generate()
        term2 = FuzzyTermId.generate()
        
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[term1, term2]
        )

        consequent.remove_term(term1)

        assert len(consequent.terms) == 1
        assert term1 not in consequent.terms

    def test_remove_nonexistent_term_does_nothing(self):
        """No debe hacer nada al remover término inexistente."""
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )
        
        nonexistent_term = FuzzyTermId.generate()
        consequent.remove_term(nonexistent_term)

        assert len(consequent.terms) == 1

    def test_has_term_returns_true_for_existing_term(self):
        """Debe retornar True para término existente."""
        term_id = FuzzyTermId.generate()
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[term_id]
        )

        assert consequent.has_term(term_id) is True

    def test_has_term_returns_false_for_nonexistent_term(self):
        """Debe retornar False para término inexistente."""
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=[FuzzyTermId.generate()]
        )
        
        nonexistent_term = FuzzyTermId.generate()
        assert consequent.has_term(nonexistent_term) is False

    def test_get_term_count(self):
        """Debe retornar el número de términos."""
        terms = [FuzzyTermId.generate() for _ in range(3)]
        consequent = RuleConsequent(
            variable_id=FuzzyVariableId.generate(),
            terms=terms
        )

        assert consequent.get_term_count() == 3


class TestRuleConsequentSerialization:
    """Pruebas para serialización de RuleConsequent."""

    def test_to_dict_converts_to_dictionary(self):
        """Debe convertir a diccionario correctamente."""
        var_id = FuzzyVariableId.generate()
        term1 = FuzzyTermId.generate()
        term2 = FuzzyTermId.generate()
        
        consequent = RuleConsequent(
            variable_id=var_id,
            terms=[term1, term2],
            aggregation_method="sum"
        )

        result = consequent.to_dict()

        assert result["variable_id"] == str(var_id)
        assert len(result["terms"]) == 2
        assert str(term1) in result["terms"]
        assert str(term2) in result["terms"]
        assert result["aggregation_method"] == "sum"

    def test_from_dict_creates_rule_consequent(self):
        """Debe crear RuleConsequent desde diccionario."""
        var_id = str(FuzzyVariableId.generate())
        term1 = str(FuzzyTermId.generate())
        term2 = str(FuzzyTermId.generate())
        
        data = {
            "variable_id": var_id,
            "terms": [term1, term2],
            "aggregation_method": "probabilistic_or"
        }

        consequent = RuleConsequent.from_dict(data)

        assert str(consequent.variable_id) == var_id
        assert len(consequent.terms) == 2
        assert consequent.aggregation_method == "probabilistic_or"

    def test_from_dict_uses_default_aggregation_method(self):
        """Debe usar método de agregación por defecto si no se especifica."""
        data = {
            "variable_id": str(FuzzyVariableId.generate()),
            "terms": [str(FuzzyTermId.generate())]
        }

        consequent = RuleConsequent.from_dict(data)

        assert consequent.aggregation_method == "max"

    def test_str_representation(self):
        """Debe retornar representación en string correcta."""
        var_id = FuzzyVariableId.generate()
        terms = [FuzzyTermId.generate() for _ in range(2)]
        
        consequent = RuleConsequent(
            variable_id=var_id,
            terms=terms,
            aggregation_method="sum"
        )

        result = str(consequent)

        assert "RuleConsequent" in result
        assert f"variable={var_id}" in result
        assert "terms=2" in result
        assert "method=sum" in result

    def test_roundtrip_serialization(self):
        """Debe mantener integridad en serialización ida y vuelta."""
        var_id = FuzzyVariableId.generate()
        terms = [FuzzyTermId.generate() for _ in range(3)]
        
        original = RuleConsequent(
            variable_id=var_id,
            terms=terms,
            aggregation_method="probabilistic_or"
        )

        data = original.to_dict()
        restored = RuleConsequent.from_dict(data)

        assert str(restored.variable_id) == str(original.variable_id)
        assert len(restored.terms) == len(original.terms)
        assert restored.aggregation_method == original.aggregation_method
