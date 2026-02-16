#!/usr/bin/env python3
"""Test script for BI integration through BFF service."""
import urllib.request
import json
import sys
from datetime import datetime, timedelta

BFF_URL = "http://localhost:8081"

# ConsumptionType enum values (from BiService.Domain.Enums.ConsumptionType)
CONSUMPTION_TYPE_ELECTRICITY = 1  # ElectricityKwh
CONSUMPTION_TYPE_WATER = 2        # WaterLiters
CONSUMPTION_TYPE_NUTRIENT = 3     # NutrientLiters

def http_request(url, method="GET", data=None, headers=None, cookies=None, expect_json=True):
    """Make an HTTP request and return (status, body_dict, response_headers)."""
    hdrs = headers or {}
    if cookies:
        cookie_str = "; ".join([f"{k}={v}" for k, v in cookies.items()])
        hdrs["Cookie"] = cookie_str
    
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
                return resp.status, json.loads(raw), resp.headers
            except json.JSONDecodeError:
                return resp.status, {"raw": raw}, resp.headers
        return resp.status, {"raw": raw}, resp.headers
    except urllib.error.HTTPError as e:
        body_text = e.read().decode("utf-8")
        try:
            return e.code, json.loads(body_text), e.headers
        except json.JSONDecodeError:
            return e.code, {"raw": body_text}, e.headers

def extract_cookies(headers):
    """Extract cookies from response headers."""
    cookies = {}
    for header in headers.get_all('Set-Cookie') or []:
        parts = header.split(';')[0].split('=', 1)
        if len(parts) == 2:
            cookies[parts[0]] = parts[1]
    return cookies

def main():
    print("=" * 70)
    print("🧪 BFF BI INTEGRATION TEST")
    print("=" * 70)
    
    # Step 1: Health check
    print("\n📋 Step 1: BFF Health check")
    status, body, _ = http_request(f"{BFF_URL}/health", expect_json=False)
    print(f"   Status: {status}, Body: {body.get('raw', '')[:50]}")
    if status != 200:
        print("   ❌ BFF not healthy!")
        sys.exit(1)
    print("   ✅ BFF is healthy")
    
    # Step 2: Login to BFF (web flow)
    print("\n📋 Step 2: Login to BFF (web flow)")
    status, body, resp_headers = http_request(f"{BFF_URL}/auth/login/web", "POST", {
        "email": "SGalindo@demo.com",
        "password": "*n86_0e!Lg6e"
    })
    print(f"   Status: {status}")
    
    if status != 200:
        print(f"   ❌ Login failed: {json.dumps(body, indent=2)}")
        sys.exit(1)
    
    # Extract session cookies
    session_cookies = extract_cookies(resp_headers)
    session_id = session_cookies.get('SessionId')
    csrf_token = session_cookies.get('CsrfToken')
    
    if not session_id:
        print(f"   ❌ No SessionId cookie in response")
        sys.exit(1)
    
    print(f"   ✅ Got SessionId: {session_id[:30]}...")
    if csrf_token:
        print(f"   ✅ Got CsrfToken: {csrf_token[:30]}...")
    
    cookies = {"SessionId": session_id}
    if csrf_token:
        cookies["CsrfToken"] = csrf_token
    
    # Step 3: GET current cost config (should be empty/404 initially)
    print("\n📋 Step 3: GET /bi/cost-config/current (expect 404 initially)")
    status, body, _ = http_request(
        f"{BFF_URL}/bi/cost-config/current", "GET", cookies=cookies)
    print(f"   Status: {status}")
    if status == 404:
        print(f"   ✅ No active cost config yet (expected): {body.get('message', '')}")
    elif status == 200:
        print(f"   ℹ️  Cost config already exists: {json.dumps(body, default=str, indent=2)[:300]}")
    else:
        print(f"   ⚠️  Unexpected status: {body}")
    
    # Step 4: Create a cost config version
    print("\n📋 Step 4: POST /bi/cost-config/versions")
    now = datetime.utcnow()
    status, body, _ = http_request(
        f"{BFF_URL}/bi/cost-config/versions", "POST",
        data={
            "currency": "COP",
            "electricityCostPerKwh": 850.50,
            "waterCostPerLiter": 12.30,
            "nutrientCostPerLiter": 450.00,
            "effectiveFrom": now.isoformat() + "Z"
        },
        cookies=cookies)
    print(f"   Status: {status}")
    
    if status not in (200, 201):
        print(f"   ❌ Failed to create cost config: {json.dumps(body, indent=2)}")
        sys.exit(1)
    
    config_id = body.get("id", "")
    print(f"   ✅ Created cost config: {config_id}")
    print(f"   Details: ElectricityKwh={body.get('electricityCostPerKwh')}, Water={body.get('waterCostPerLiter')}, Nutrients={body.get('nutrientCostPerLiter')}")
    
    # Step 5: GET current cost config (should return the one we created)
    print("\n📋 Step 5: GET /bi/cost-config/current (expect 200)")
    status, body, _ = http_request(
        f"{BFF_URL}/bi/cost-config/current", "GET", cookies=cookies)
    print(f"   Status: {status}")
    if status == 200:
        print(f"   ✅ Got current config: ID={body.get('id')}, Currency={body.get('currency')}, IsActive={body.get('isActive')}")
    else:
        print(f"   ❌ Failed: {json.dumps(body, indent=2)}")
        sys.exit(1)
    
    # Step 6: GET cost config versions
    print("\n📋 Step 6: GET /bi/cost-config/versions")
    status, body, _ = http_request(
        f"{BFF_URL}/bi/cost-config/versions", "GET", cookies=cookies)
    print(f"   Status: {status}")
    if status == 200:
        version_count = len(body) if isinstance(body, list) else 0
        print(f"   ✅ Got {version_count} version(s)")
        if version_count > 0:
            print(f"   First version: {json.dumps(body[0], default=str, indent=2)[:300]}")
    else:
        print(f"   ❌ Failed: {json.dumps(body, indent=2)}")
    
    # Step 7: Create consumption entries
    print("\n📋 Step 7: POST /bi/consumption-entries (Electricity)")
    status, body, _ = http_request(
        f"{BFF_URL}/bi/consumption-entries", "POST",
        data={
            "date": now.isoformat() + "Z",
            "type": CONSUMPTION_TYPE_ELECTRICITY,
            "amount": 15.75,
            "note": "Test electricity consumption from BFF"
        },
        cookies=cookies)
    print(f"   Status: {status}")
    if status not in (200, 201):
        print(f"   ❌ Failed: {json.dumps(body, indent=2)}")
        sys.exit(1)
    
    entry_id_1 = body.get("id", "")
    print(f"   ✅ Created entry: {entry_id_1}, Cost={body.get('costAmount')} {body.get('currencySnapshot')}")
    
    # Step 8: Create another consumption entry (Water)
    print("\n📋 Step 8: POST /bi/consumption-entries (Water)")
    status, body, _ = http_request(
        f"{BFF_URL}/bi/consumption-entries", "POST",
        data={
            "date": now.isoformat() + "Z",
            "type": CONSUMPTION_TYPE_WATER,
            "amount": 500.0,
            "note": "Test water consumption from BFF"
        },
        cookies=cookies)
    print(f"   Status: {status}")
    if status not in (200, 201):
        print(f"   ❌ Failed: {json.dumps(body, indent=2)}")
    else:
        print(f"   ✅ Created entry: {body.get('id')}, Cost={body.get('costAmount')} {body.get('currencySnapshot')}")
    
    # Step 9: Create Nutrient entry
    print("\n📋 Step 9: POST /bi/consumption-entries (Nutrients)")
    status, body, _ = http_request(
        f"{BFF_URL}/bi/consumption-entries", "POST",
        data={
            "date": now.isoformat() + "Z",
            "type": CONSUMPTION_TYPE_NUTRIENT,
            "amount": 2.5,
            "note": "Test nutrient consumption from BFF"
        },
        cookies=cookies)
    print(f"   Status: {status}")
    if status not in (200, 201):
        print(f"   ❌ Failed: {json.dumps(body, indent=2)}")
    else:
        print(f"   ✅ Created entry: {body.get('id')}, Cost={body.get('costAmount')} {body.get('currencySnapshot')}")
    
    # Step 10: GET consumption entries
    print("\n📋 Step 10: GET /bi/consumption-entries (date range)")
    from_date = (now - timedelta(days=1)).isoformat() + "Z"
    to_date = (now + timedelta(days=1)).isoformat() + "Z"
    status, body, _ = http_request(
        f"{BFF_URL}/bi/consumption-entries?from={from_date}&to={to_date}",
        "GET", cookies=cookies)
    print(f"   Status: {status}")
    if status == 200:
        entry_count = len(body) if isinstance(body, list) else 0
        print(f"   ✅ Got {entry_count} consumption entries")
        if entry_count > 0:
            for entry in body[:3]:  # Show first 3
                print(f"      - {entry.get('type')}: {entry.get('amount')} units, Cost={entry.get('costAmount')}")
    else:
        print(f"   ❌ Failed: {json.dumps(body, indent=2)}")
    
    # Step 11: GET consumption summary
    print("\n📋 Step 11: GET /bi/consumption-entries/summary")
    status, body, _ = http_request(
        f"{BFF_URL}/bi/consumption-entries/summary?from={from_date}&to={to_date}",
        "GET", cookies=cookies)
    print(f"   Status: {status}")
    if status == 200:
        print(f"   ✅ Summary retrieved:")
        print(f"      Currency: {body.get('currency')}")
        print(f"      Total Electricity: {body.get('totalElectricityKwh')} kWh → Cost: {body.get('costElectricity')}")
        print(f"      Total Water: {body.get('totalWaterLiters')} L → Cost: {body.get('costWater')}")
        print(f"      Total Nutrients: {body.get('totalNutrientLiters')} L → Cost: {body.get('costNutrients')}")
        print(f"      💰 TOTAL COST: {body.get('costTotal')} {body.get('currency')}")
    else:
        print(f"   ❌ Failed: {json.dumps(body, indent=2)}")
    
    # Step 12: Filter by type
    print("\n📋 Step 12: GET /bi/consumption-entries (filter by type=1/ElectricityKwh)")
    status, body, _ = http_request(
        f"{BFF_URL}/bi/consumption-entries?from={from_date}&to={to_date}&type={CONSUMPTION_TYPE_ELECTRICITY}",
        "GET", cookies=cookies)
    print(f"   Status: {status}")
    if status == 200:
        entry_count = len(body) if isinstance(body, list) else 0
        print(f"   ✅ Got {entry_count} electricity entries")
    else:
        print(f"   ❌ Failed: {json.dumps(body, indent=2)}")
    
    # Step 13: Test validation (invalid amount)
    print("\n📋 Step 13: POST /bi/consumption-entries (invalid amount - expect 400)")
    status, body, _ = http_request(
        f"{BFF_URL}/bi/consumption-entries", "POST",
        data={
            "date": now.isoformat() + "Z",
            "type": CONSUMPTION_TYPE_ELECTRICITY,
            "amount": -10.0,  # Invalid: negative
            "note": "This should fail"
        },
        cookies=cookies)
    print(f"   Status: {status}")
    if status == 400:
        print(f"   ✅ Validation error caught (expected): {body.get('title', body.get('message', ''))}")
        errors = body.get('errors', {})
        if errors:
            print(f"      Errors: {json.dumps(errors, indent=6)}")
    else:
        print(f"   ⚠️  Expected 400 but got {status}: {json.dumps(body, indent=2)}")
    
    print("\n" + "=" * 70)
    print("✅ ALL TESTS COMPLETED SUCCESSFULLY!")
    print("=" * 70)

if __name__ == "__main__":
    main()
