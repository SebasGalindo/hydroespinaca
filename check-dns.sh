#!/bin/bash

echo "=== VERIFICACIÓN DNS ==="
echo "$(date)"
echo

echo "1. IP pública del servidor:"
curl -s https://ifconfig.me
echo
echo

echo "2. Resolución DNS de api.hydroespinaca.online:"
nslookup api.hydroespinaca.online
echo

echo "3. Dig A record:"
dig +short api.hydroespinaca.online A
echo

echo "4. Dig desde Google DNS:"
dig @8.8.8.8 +short api.hydroespinaca.online A
echo

echo "5. Ping test:"
ping -c 3 api.hydroespinaca.online
echo

echo "6. Wget test desde el mismo servidor:"
wget -O- --timeout=10 http://api.hydroespinaca.online/.well-known/acme-challenge/test 2>&1 | head -5
echo

echo "=== FIN VERIFICACIÓN DNS ==="