import requests
import json
from uuid import uuid4

# Test the update system endpoint
base_url = "http://localhost:8000"

# Create system
sys_resp = requests.post(f"{base_url}/api/fuzzy-systems", json={
    "name": f"TestSystem{uuid4().hex[:8]}",
    "isActive": False
})
print(f"Create system: {sys_resp.status_code}")
if sys_resp.status_code == 201:
    sys_id = sys_resp.json()["id"]
    print(f"System ID: {sys_id}")
    
    # Create variables
    var1_resp = requests.post(f"{base_url}/api/fuzzy-variables", json={
        "name": f"TestVar1{uuid4().hex[:8]}",
        "variable_type": "input"
    })
    print(f"Create var1: {var1_resp.status_code}")
    
    var2_resp = requests.post(f"{base_url}/api/fuzzy-variables", json={
        "name": f"TestVar2{uuid4().hex[:8]}",
        "variable_type": "output"
    })
    print(f"Create var2: {var2_resp.status_code}")
    
    if var1_resp.status_code == 201 and var2_resp.status_code == 201:
        var1_id = var1_resp.json()["id"]
        var2_id = var2_resp.json()["id"]
        print(f"Variable IDs: {var1_id}, {var2_id}")
        
        # Update system with variables
        update_resp = requests.put(f"{base_url}/api/fuzzy-systems/{sys_id}", json={
            "input_variable_ids": [var1_id],
            "output_variable_ids": [var2_id]
        })
        print(f"Update system: {update_resp.status_code}")
        print(f"Response: {update_resp.text}")
    else:
        print("Failed to create variables")
else:
    print(f"Failed to create system: {sys_resp.text}")
