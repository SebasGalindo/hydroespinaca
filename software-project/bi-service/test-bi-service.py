#!/usr/bin/env python3
"""Test script for bi-service endpoints."""
import urllib.request
import json
import sys

AUTH_URL = "http://localhost:5001"
BI_URL = "http://localhost:5020"

def http_request(url, method="GET", data=None, headers=None, expect_json=True):
    """Make an HTTP request and return (status, body_dict)."""
    hdrs = headers or {}
    if data is not None:
        body = json.dumps(data).encode("utf-8")
        hdrs.setdefault("Content-Type", "application/json")
    else:
        body = None
    
    req = urllib.request.Request(url, data=body, headers=hdrs, method=method)
    try:
        resp = urllib.request.urlopen(req)
        raw = resp.read().decode("utf-8")
        if expect_json:
            try:
                return resp.status, json.loads(raw)
            except json.JSONDecodeError:
                return resp.status, {"raw": raw}
        return resp.status, {"raw": raw}
    except urllib.error.HTTPError as e:
        body_text = e.read().decode("utf-8")
        try:
            return e.code, json.loads(body_text)
        except json.JSONDecodeError:
            return e.code, {"raw": body_text}

def main():
    print("=" * 60)
    print("🧪 BI-SERVICE INTEGRATION TEST")
    print("=" * 60)
    
    # Step 1: Health check
    print("\n📋 Step 1: Health check")
    status, body = http_request(f"{BI_URL}/health", expect_json=False)
    print(f"   Status: {status}, Body: {body}")
    
    # Step 2: Login to auth-service
    print("\n📋 Step 2: Login to auth-service")
    status, body = http_request(f"{AUTH_URL}/api/auth/login", "POST", {
        "email": "SGalindo@demo.com",
        "password": "*n86_0e!Lg6e"
    })
    print(f"   Status: {status}")
    
    if status != 200:
        print(f"   ❌ Login failed: {json.dumps(body, indent=2)}")
        sys.exit(1)
    
    token = body.get("accessToken") or body.get("access_token") or body.get("token")
    if not token:
        print(f"   ❌ No token in response: {json.dumps(body, indent=2, default=str)[:500]}")
        sys.exit(1)
    
    print(f"   ✅ Got token: {token[:50]}...")
    auth_headers = {"Authorization": f"Bearer {token}"}
    
    # Step 3: Test GET current cost config (should be empty/404)
    print("\n📋 Step 3: GET current cost config (expect 404 - none exists)")
    status, body = http_request(
        f"{BI_URL}/api/bi/cost-config/current", "GET", headers=auth_headers)
    print(f"   Status: {status}, Body: {json.dumps(body, default=str)[:200]}")
    
    # Step 4: Create a cost config version
    print("\n📋 Step 4: POST create cost config version")
    status, body = http_request(
        f"{BI_URL}/api/bi/cost-config/versions", "POST",
        data={
            "currency": "COP",
            "electricityCostPerKwh": 850.50,
            "waterCostPerLiter": 12.30,
            "nutrientCostPerLiter": 450.00
        },
        headers={**auth_headers, "Content-Type": "application/json"})
    print(f"   Status: {status}")
    print(f"   Body: {json.dumps(body, indent=2, default=str)[:500]}")
    
    if status not in (200, 201):
        print(f"   ❌ Failed to create cost config")
        sys.exit(1)
    
    config_id = body.get("id", "")
    print(f"   ✅ Created cost config: {config_id}")
    
    # Step 5: GET current cost config (should return the one we created)
    print("\n📋 Step 5: GET current cost config (expect 200)")
    status, body = http_request(
        f"{BI_URL}/api/bi/cost-config/current", "GET", headers=auth_headers)
    print(f"   Status: {status}")
    print(f"   Body: {json.dumps(body, indent=2, default=str)[:500]}")
    
    # Step 6: Create consumption entries
    print("\n📋 Step 6: POST consumption entries")
    entries_data = [
        {"date": "2026-02-12T10:00:00Z", "type": 1, "amount": 15.5, "note": "Electricidad bomba"},
        {"date": "2026-02-12T10:00:00Z", "type": 2, "amount": 200.0, "note": "Agua riego"},
        {"date": "2026-02-12T10:00:00Z", "type": 3, "amount": 5.0, "note": "Nutrientes A+B"},
    ]
    
    for i, entry in enumerate(entries_data):
        status, body = http_request(
            f"{BI_URL}/api/bi/consumption-entries", "POST",
            data=entry,
            headers={**auth_headers, "Content-Type": "application/json"})
        type_names = {1: "Electricity", 2: "Water", 3: "Nutrients"}
        type_name = type_names.get(entry["type"], "Unknown")
        print(f"   [{type_name}] Status: {status}, CostAmount: {body.get('costAmount', 'N/A')}")
        if status not in (200, 201):
            print(f"   ❌ Failed: {json.dumps(body, indent=2, default=str)[:300]}")
    
    # Step 7: GET consumption entries
    print("\n📋 Step 7: GET consumption entries")
    status, body = http_request(
        f"{BI_URL}/api/bi/consumption-entries?from=2026-02-01&to=2026-02-28",
        "GET", headers=auth_headers)
    print(f"   Status: {status}, Count: {len(body) if isinstance(body, list) else 'N/A'}")
    if isinstance(body, list):
        for e in body:
            print(f"   - Type={e.get('type')}, Amount={e.get('amount')}, Cost={e.get('costAmount')}")
    
    # Step 8: GET summary
    print("\n📋 Step 8: GET consumption summary")
    status, body = http_request(
        f"{BI_URL}/api/bi/consumption-entries/summary?from=2026-02-01&to=2026-02-28",
        "GET", headers=auth_headers)
    print(f"   Status: {status}")
    print(f"   Body: {json.dumps(body, indent=2, default=str)[:500]}")
    
    # Step 9: GET versions
    print("\n📋 Step 9: GET cost config versions")
    status, body = http_request(
        f"{BI_URL}/api/bi/cost-config/versions", "GET", headers=auth_headers)
    print(f"   Status: {status}, Count: {len(body) if isinstance(body, list) else 'N/A'}")
    
    # Step 10: Create a second cost config version (should deactivate the first)
    print("\n📋 Step 10: POST second cost config (should deactivate first)")
    status, body = http_request(
        f"{BI_URL}/api/bi/cost-config/versions", "POST",
        data={
            "currency": "COP",
            "electricityCostPerKwh": 900.00,
            "waterCostPerLiter": 13.00,
            "nutrientCostPerLiter": 475.00
        },
        headers={**auth_headers, "Content-Type": "application/json"})
    print(f"   Status: {status}")
    if status in (200, 201):
        print(f"   ✅ New config created. Old config should now have effectiveTo set.")
    
    # Step 11: Verify versions history
    print("\n📋 Step 11: GET versions (expect 2 versions, only latest active)")
    status, body = http_request(
        f"{BI_URL}/api/bi/cost-config/versions", "GET", headers=auth_headers)
    if isinstance(body, list):
        for v in body:
            print(f"   - id={v.get('id','')[:12]}... active={v.get('isActive')}, from={v.get('effectiveFrom','')[:19]}, to={v.get('effectiveTo','None')}")
    
    # Step 12: Test validation errors
    print("\n📋 Step 12: Validation tests")
    # Negative cost
    status, body = http_request(
        f"{BI_URL}/api/bi/cost-config/versions", "POST",
        data={"currency": "COP", "electricityCostPerKwh": -100, "waterCostPerLiter": 10, "nutrientCostPerLiter": 10},
        headers={**auth_headers, "Content-Type": "application/json"})
    print(f"   Negative cost → Status: {status} (expect 400)")
    
    # Zero amount entry
    status, body = http_request(
        f"{BI_URL}/api/bi/consumption-entries", "POST",
        data={"date": "2026-02-12T10:00:00Z", "type": 1, "amount": 0},
        headers={**auth_headers, "Content-Type": "application/json"})
    print(f"   Zero amount  → Status: {status} (expect 400)")
    
    # Bad date range
    status, body = http_request(
        f"{BI_URL}/api/bi/consumption-entries?from=2026-03-01&to=2026-02-01",
        "GET", headers=auth_headers)
    print(f"   Bad range    → Status: {status} (expect 400)")
    
    print("\n" + "=" * 60)
    print("🏁 TEST COMPLETE")
    print("=" * 60)

if __name__ == "__main__":
    main()
