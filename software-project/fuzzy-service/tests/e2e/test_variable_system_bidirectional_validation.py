"""End-to-end tests for bidirectional validation between Variables and Systems."""

import pytest
import pytest_asyncio
import httpx
from typing import Dict, Any
import time

pytestmark = pytest.mark.asyncio


class TestVariableSystemBidirectionalValidation:
    """Test bidirectional validation when deleting variables from systems."""
    
    @pytest_asyncio.fixture
    async def base_url(self) -> str:
        """Base URL for the API."""
        return "http://localhost:8000/api"
    
    @pytest_asyncio.fixture
    async def http_client(self) -> httpx.AsyncClient:
        """HTTP client for making requests."""
        async with httpx.AsyncClient() as client:
            yield client
    
    @pytest_asyncio.fixture
    async def sample_system(self, http_client: httpx.AsyncClient, base_url: str) -> Dict[str, Any]:
        """Create a sample fuzzy system for testing."""
        timestamp = int(time.time() * 1000)
        system_data = {
            "name": f"Test System for Variable Deletion {timestamp}",
            "defuzzification_method": "centroid",
            "operators": {
                "and_operator": "min",
                "or_operator": "max",
                "not_operator": "complement"
            }
        }
        
        response = await http_client.post(f"{base_url}/fuzzy-systems", json=system_data)
        assert response.status_code == 201
        return response.json()
    
    @pytest_asyncio.fixture
    async def sample_input_variable(self, http_client: httpx.AsyncClient, base_url: str) -> Dict[str, Any]:
        """Create a sample input variable for testing."""
        timestamp = int(time.time() * 1000)
        variable_data = {
            "name": f"Test Input Variable {timestamp}",
            "variable_type": "input",
            "description": "Variable to test deletion"
        }
        
        response = await http_client.post(f"{base_url}/fuzzy-variables", json=variable_data)
        assert response.status_code == 201
        return response.json()
    
    @pytest_asyncio.fixture
    async def sample_output_variable(self, http_client: httpx.AsyncClient, base_url: str) -> Dict[str, Any]:
        """Create a sample output variable for testing."""
        timestamp = int(time.time() * 1000)
        variable_data = {
            "name": f"Test Output Variable {timestamp}",
            "variable_type": "output",
            "description": "Variable to test deletion"
        }
        
        response = await http_client.post(f"{base_url}/fuzzy-variables", json=variable_data)
        assert response.status_code == 201
        return response.json()
    
    @pytest.mark.asyncio
    async def test_delete_input_variable_removes_from_system(
        self, 
        http_client: httpx.AsyncClient, 
        base_url: str,
        sample_system: Dict[str, Any],
        sample_input_variable: Dict[str, Any]
    ):
        """Test that deleting an input variable removes it from associated systems."""
        system_id = sample_system["id"]
        variable_id = sample_input_variable["id"]
        
        # Add the input variable to the system by updating the system
        response = await http_client.put(
            f"{base_url}/fuzzy-systems/{system_id}",
            json={"input_variable_ids": [variable_id]}
        )
        assert response.status_code == 200
        
        # Verify the variable is in the system
        response = await http_client.get(f"{base_url}/fuzzy-systems/{system_id}")
        assert response.status_code == 200
        system_data = response.json()
        assert variable_id in system_data["input_variable_ids"]
        
        # Delete the variable
        response = await http_client.delete(f"{base_url}/fuzzy-variables/{variable_id}")
        assert response.status_code == 200
        
        # Verify the variable is removed from the system
        response = await http_client.get(f"{base_url}/fuzzy-systems/{system_id}")
        assert response.status_code == 200
        updated_system = response.json()
        assert variable_id not in updated_system["input_variable_ids"]
        
        # Verify the variable no longer exists
        response = await http_client.get(f"{base_url}/fuzzy-variables/{variable_id}")
        assert response.status_code == 404
    
    @pytest.mark.asyncio
    async def test_delete_output_variable_removes_from_system(
        self, 
        http_client: httpx.AsyncClient, 
        base_url: str,
        sample_system: Dict[str, Any],
        sample_output_variable: Dict[str, Any]
    ):
        """Test that deleting an output variable removes it from associated systems."""
        system_id = sample_system["id"]
        variable_id = sample_output_variable["id"]
        
        # Add the output variable to the system by updating the system
        response = await http_client.put(
            f"{base_url}/fuzzy-systems/{system_id}",
            json={"output_variable_ids": [variable_id]}
        )
        assert response.status_code == 200
        
        # Verify the variable is in the system
        response = await http_client.get(f"{base_url}/fuzzy-systems/{system_id}")
        assert response.status_code == 200
        system_data = response.json()
        assert variable_id in system_data["output_variable_ids"]
        
        # Delete the variable
        response = await http_client.delete(f"{base_url}/fuzzy-variables/{variable_id}")
        assert response.status_code == 200
        
        # Verify the variable is removed from the system
        response = await http_client.get(f"{base_url}/fuzzy-systems/{system_id}")
        assert response.status_code == 200
        updated_system = response.json()
        assert variable_id not in updated_system["output_variable_ids"]
        
        # Verify the variable no longer exists
        response = await http_client.get(f"{base_url}/fuzzy-variables/{variable_id}")
        assert response.status_code == 404
    
    @pytest.mark.asyncio
    async def test_delete_variable_from_multiple_systems(
        self, 
        http_client: httpx.AsyncClient, 
        base_url: str,
        sample_input_variable: Dict[str, Any]
    ):
        """Test that deleting a variable removes it from multiple systems."""
        variable_id = sample_input_variable["id"]
        
        # Create two systems with unique names
        import uuid
        unique_id = uuid.uuid4().hex[:8]
        
        system1_data = {
            "name": f"System 1 for Multi-System Test {unique_id}",
            "defuzzification_method": "centroid",
            "operators": {
                "and_operator": "min",
                "or_operator": "max",
                "not_operator": "complement"
            }
        }

        system2_data = {
            "name": f"System 2 for Multi-System Test {unique_id}",
            "defuzzification_method": "bisector",
            "operators": {
                "and_operator": "product",
                "or_operator": "probabilistic_sum",
                "not_operator": "complement"
            }
        }
        
        response1 = await http_client.post(f"{base_url}/fuzzy-systems", json=system1_data)
        assert response1.status_code == 201
        system1 = response1.json()
        
        response2 = await http_client.post(f"{base_url}/fuzzy-systems", json=system2_data)
        assert response2.status_code == 201
        system2 = response2.json()
        
        # Add the variable to both systems by updating them
        response = await http_client.put(
            f"{base_url}/fuzzy-systems/{system1['id']}",
            json={"input_variable_ids": [variable_id]}
        )
        assert response.status_code == 200
        
        response = await http_client.put(
            f"{base_url}/fuzzy-systems/{system2['id']}",
            json={"input_variable_ids": [variable_id]}
        )
        assert response.status_code == 200
        
        # Verify the variable is in both systems
        response = await http_client.get(f"{base_url}/fuzzy-systems/{system1['id']}")
        assert response.status_code == 200
        assert variable_id in response.json()["input_variable_ids"]
        
        response = await http_client.get(f"{base_url}/fuzzy-systems/{system2['id']}")
        assert response.status_code == 200
        assert variable_id in response.json()["input_variable_ids"]
        
        # Delete the variable
        response = await http_client.delete(f"{base_url}/fuzzy-variables/{variable_id}")
        assert response.status_code == 200
        
        # Verify the variable is removed from both systems
        response = await http_client.get(f"{base_url}/fuzzy-systems/{system1['id']}")
        assert response.status_code == 200
        assert variable_id not in response.json()["input_variable_ids"]
        
        response = await http_client.get(f"{base_url}/fuzzy-systems/{system2['id']}")
        assert response.status_code == 200
        assert variable_id not in response.json()["input_variable_ids"]
        
        # Clean up: delete the test systems
        await http_client.delete(f"{base_url}/fuzzy-systems/{system1['id']}")
        await http_client.delete(f"{base_url}/fuzzy-systems/{system2['id']}")
    
    @pytest.mark.asyncio
    async def test_delete_variable_with_terms_removes_from_system(
        self, 
        http_client: httpx.AsyncClient, 
        base_url: str,
        sample_system: Dict[str, Any],
        sample_input_variable: Dict[str, Any]
    ):
        """Test that deleting a variable with terms removes it from systems."""
        system_id = sample_system["id"]
        variable_id = sample_input_variable["id"]
        
        # Add a term to the variable using the fuzzy-terms endpoint
        term_data = {
            "variable_id": variable_id,
            "label": "Low",
            "membership_function": {
                "function_type": "triangular",
                "parameters": [0.0, 0.0, 50.0],
                "universe_min": 0.0,
                "universe_max": 100.0
            }
        }
        
        response = await http_client.post(
            f"{base_url}/fuzzy-terms", 
            json=term_data
        )
        assert response.status_code == 201
        
        # Add the variable to the system by updating the system
        response = await http_client.put(
            f"{base_url}/fuzzy-systems/{system_id}",
            json={"input_variable_ids": [variable_id]}
        )
        assert response.status_code == 200
        
        # Verify the variable is in the system
        response = await http_client.get(f"{base_url}/fuzzy-systems/{system_id}")
        assert response.status_code == 200
        assert variable_id in response.json()["input_variable_ids"]
        
        # Delete the variable (should also delete its terms)
        response = await http_client.delete(f"{base_url}/fuzzy-variables/{variable_id}")
        assert response.status_code == 200
        
        # Verify the variable is removed from the system
        response = await http_client.get(f"{base_url}/fuzzy-systems/{system_id}")
        assert response.status_code == 200
        assert variable_id not in response.json()["input_variable_ids"]
        
        # Verify the variable no longer exists
        response = await http_client.get(f"{base_url}/fuzzy-variables/{variable_id}")
        assert response.status_code == 404