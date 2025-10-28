import pytest
import uuid
from bson import ObjectId
from FuzzyService.Domain.ValueObjects.DomainId import (
    DomainId,
    FuzzySystemId,
    FuzzyVariableId,
    FuzzyTermId,
    FuzzyRuleId,
    FuzzyEvaluationId
)


class TestDomainId:
    """Tests for DomainId value object"""

    def test_create_domain_id_without_value_should_generate_uuid(self):
        # Arrange & Act
        domain_id = DomainId()

        # Assert
        assert domain_id.root is not None
        # Should be a valid UUID
        uuid.UUID(domain_id.root)

    def test_create_domain_id_with_valid_uuid_should_succeed(self):
        # Arrange
        test_uuid = str(uuid.uuid4())

        # Act
        domain_id = DomainId(test_uuid)

        # Assert
        assert str(domain_id) == test_uuid

    def test_create_domain_id_with_valid_objectid_should_succeed(self):
        # Arrange
        test_oid = str(ObjectId())

        # Act
        domain_id = DomainId(test_oid)

        # Assert
        assert str(domain_id) == test_oid

    def test_create_domain_id_with_mongo_oid_dict_should_extract_value(self):
        # Arrange
        test_oid = str(ObjectId())
        mongo_dict = {"$oid": test_oid}

        # Act
        domain_id = DomainId(mongo_dict)

        # Assert
        assert str(domain_id) == test_oid

    def test_create_domain_id_with_oid_dict_should_extract_value(self):
        # Arrange
        test_oid = str(ObjectId())
        oid_dict = {"oid": test_oid}

        # Act
        domain_id = DomainId(oid_dict)

        # Assert
        assert str(domain_id) == test_oid

    def test_create_domain_id_with_invalid_format_should_fail(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="DomainId debe ser ObjectId o UUID válido"):
            DomainId("invalid-id-format")

    def test_create_domain_id_with_empty_dict_should_fail(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="Formato de id no soportado para DomainId"):
            DomainId({})

    def test_generate_should_create_new_uuid(self):
        # Arrange & Act
        domain_id = DomainId.generate()

        # Assert
        assert domain_id.root is not None
        # Should be a valid UUID
        uuid.UUID(domain_id.root)

    def test_generate_should_create_unique_ids(self):
        # Arrange & Act
        id1 = DomainId.generate()
        id2 = DomainId.generate()

        # Assert
        assert id1 != id2

    def test_equality_with_same_value_should_return_true(self):
        # Arrange
        test_uuid = str(uuid.uuid4())
        id1 = DomainId(test_uuid)
        id2 = DomainId(test_uuid)

        # Act & Assert
        assert id1 == id2

    def test_equality_with_different_value_should_return_false(self):
        # Arrange
        id1 = DomainId.generate()
        id2 = DomainId.generate()

        # Act & Assert
        assert id1 != id2

    def test_equality_with_string_should_work(self):
        # Arrange
        test_uuid = str(uuid.uuid4())
        domain_id = DomainId(test_uuid)

        # Act & Assert
        assert domain_id == test_uuid

    def test_hash_should_be_consistent(self):
        # Arrange
        test_uuid = str(uuid.uuid4())
        id1 = DomainId(test_uuid)
        id2 = DomainId(test_uuid)

        # Act & Assert
        assert hash(id1) == hash(id2)

    def test_domain_id_should_be_immutable(self):
        # Arrange
        domain_id = DomainId.generate()

        # Act & Assert
        with pytest.raises(Exception):  # Pydantic frozen model raises ValidationError
            domain_id.root = "new-value"

    def test_str_representation_should_return_root_value(self):
        # Arrange
        test_uuid = str(uuid.uuid4())
        domain_id = DomainId(test_uuid)

        # Act
        result = str(domain_id)

        # Assert
        assert result == test_uuid


class TestFuzzySystemId:
    """Tests for FuzzySystemId specialized id"""

    def test_create_fuzzy_system_id_should_succeed(self):
        # Arrange & Act
        system_id = FuzzySystemId()

        # Assert
        assert system_id.root is not None
        assert isinstance(system_id, FuzzySystemId)
        assert isinstance(system_id, DomainId)

    def test_generate_fuzzy_system_id_should_return_correct_type(self):
        # Arrange & Act
        system_id = FuzzySystemId.generate()

        # Assert
        assert isinstance(system_id, FuzzySystemId)


class TestFuzzyVariableId:
    """Tests for FuzzyVariableId specialized id"""

    def test_create_fuzzy_variable_id_should_succeed(self):
        # Arrange & Act
        variable_id = FuzzyVariableId()

        # Assert
        assert variable_id.root is not None
        assert isinstance(variable_id, FuzzyVariableId)
        assert isinstance(variable_id, DomainId)

    def test_generate_fuzzy_variable_id_should_return_correct_type(self):
        # Arrange & Act
        variable_id = FuzzyVariableId.generate()

        # Assert
        assert isinstance(variable_id, FuzzyVariableId)


class TestFuzzyTermId:
    """Tests for FuzzyTermId specialized id"""

    def test_create_fuzzy_term_id_should_succeed(self):
        # Arrange & Act
        term_id = FuzzyTermId()

        # Assert
        assert term_id.root is not None
        assert isinstance(term_id, FuzzyTermId)
        assert isinstance(term_id, DomainId)

    def test_generate_fuzzy_term_id_should_return_correct_type(self):
        # Arrange & Act
        term_id = FuzzyTermId.generate()

        # Assert
        assert isinstance(term_id, FuzzyTermId)


class TestFuzzyRuleId:
    """Tests for FuzzyRuleId specialized id"""

    def test_create_fuzzy_rule_id_should_succeed(self):
        # Arrange & Act
        rule_id = FuzzyRuleId()

        # Assert
        assert rule_id.root is not None
        assert isinstance(rule_id, FuzzyRuleId)
        assert isinstance(rule_id, DomainId)

    def test_generate_fuzzy_rule_id_should_return_correct_type(self):
        # Arrange & Act
        rule_id = FuzzyRuleId.generate()

        # Assert
        assert isinstance(rule_id, FuzzyRuleId)


class TestFuzzyEvaluationId:
    """Tests for FuzzyEvaluationId specialized id"""

    def test_create_fuzzy_evaluation_id_should_succeed(self):
        # Arrange & Act
        evaluation_id = FuzzyEvaluationId()

        # Assert
        assert evaluation_id.root is not None
        assert isinstance(evaluation_id, FuzzyEvaluationId)
        assert isinstance(evaluation_id, DomainId)

    def test_generate_fuzzy_evaluation_id_should_return_correct_type(self):
        # Arrange & Act
        evaluation_id = FuzzyEvaluationId.generate()

        # Assert
        assert isinstance(evaluation_id, FuzzyEvaluationId)
