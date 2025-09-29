"""End-to-end tests for fuzzy evaluations endpoints."""

import pytest
import pytest_asyncio
import httpx
from typing import Dict, Any
import time

pytestmark = pytest.mark.asyncio


class TestFuzzyEvaluationsEndpoints:
    """Test fuzzy evaluations endpoints end-to-end."""
    
    @pytest_asyncio.fixture
    async def base_url(self) -> str:
        return "http://localhost:8000/api"
    
    @pytest_asyncio.fixture
    async def http_client(self) -> httpx.AsyncClient:
        async with httpx.AsyncClient(follow_redirects=True, timeout=30.0) as client:
            yield client
    
    @pytest_asyncio.fixture
    async def sample_system(self, http_client: httpx.AsyncClient, base_url: str) -> Dict[str, Any]:
        """Create a sample fuzzy system for testing."""
        timestamp = int(time.time() * 1000)
        system_data = {
            "name": f"Test System for Evaluations {timestamp}",
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
    
    @pytest.mark.asyncio
    async def test_get_all_evaluations_basic(self, http_client: httpx.AsyncClient, base_url: str):
        """Test GET /api/fuzzy-evaluations basic functionality."""
        response = await http_client.get(f"{base_url}/fuzzy-evaluations")
        assert response.status_code == 200
        
        data = response.json()
        assert "evaluations" in data
        assert "total_count" in data
        assert "page" in data
        assert "page_size" in data
        assert "total_pages" in data
        assert "has_next" in data
        assert "has_previous" in data
    
    @pytest.mark.asyncio
    async def test_get_evaluation_by_invalid_id(self, http_client: httpx.AsyncClient, base_url: str):
        """Test GET /api/fuzzy-evaluations/{id} with invalid ID."""
        response = await http_client.get(f"{base_url}/fuzzy-evaluations/invalid_id")
        assert response.status_code == 422  # Invalid ID format returns validation error
    
    @pytest.mark.asyncio
    async def test_get_evaluations_by_system(self, http_client: httpx.AsyncClient, base_url: str, sample_system: Dict[str, Any]):
        """Test GET /api/fuzzy-evaluations/system/{id}."""
        system_id = sample_system["id"]
        response = await http_client.get(f"{base_url}/fuzzy-evaluations/system/{system_id}")
        assert response.status_code == 200
        
        data = response.json()
        assert "evaluations" in data
        assert "total_count" in data
        assert "page" in data
        assert "page_size" in data
    
    @pytest.mark.asyncio
    async def test_get_recent_evaluations(self, http_client: httpx.AsyncClient, base_url: str):
        """Test GET /api/fuzzy-evaluations/recent."""
        response = await http_client.get(f"{base_url}/fuzzy-evaluations/recent")
        assert response.status_code == 200
        
        data = response.json()
        assert "evaluations" in data
        assert "total_count" in data
        assert "page" in data
        assert "page_size" in data
    
    @pytest.mark.asyncio
    async def test_get_stats_summary(self, http_client: httpx.AsyncClient, base_url: str):
        """Test GET /api/fuzzy-evaluations/stats/summary."""
        response = await http_client.get(f"{base_url}/fuzzy-evaluations/stats/summary")
        assert response.status_code == 200
        
        data = response.json()
        assert "total_evaluations" in data
        assert "systems_stats" in data
        assert "daily_stats" in data
        assert "avg_evaluations_per_day" in data
        assert "date_range" in data
    
    @pytest.mark.asyncio
    async def test_pagination_parameters(self, http_client: httpx.AsyncClient, base_url: str):
        """Test pagination parameters."""
        response = await http_client.get(f"{base_url}/fuzzy-evaluations?page=1&page_size=5")
        assert response.status_code == 200
        
        data = response.json()
        assert data["page"] == 1
        assert data["page_size"] == 5
    
    @pytest.mark.asyncio
    async def test_invalid_pagination(self, http_client: httpx.AsyncClient, base_url: str):
        """Test invalid pagination parameters."""
        response = await http_client.get(f"{base_url}/fuzzy-evaluations?page=0")
        assert response.status_code == 422
        
        response = await http_client.get(f"{base_url}/fuzzy-evaluations?page_size=0")
        assert response.status_code == 422