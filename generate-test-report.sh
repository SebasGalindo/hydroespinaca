#!/bin/bash

# Script para generar reporte consolidado de pruebas unitarias
# de todos los microservicios del proyecto HydroEspinaca

set -e

# Colores para output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
RED='\033[0;31m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# Directorio base
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Variables para contadores globales
TOTAL_TESTS=0
TOTAL_PASSED=0
TOTAL_FAILED=0
TOTAL_SKIPPED=0
TOTAL_SERVICES=0

# Función para extraer métricas de cobertura desde archivo Cobertura XML
extract_coverage_metrics() {
    local coverage_file=$1

    if [ -f "$coverage_file" ]; then
        # Intentar extraer line-rate (cobertura de líneas)
        local line_rate=$(grep -o 'line-rate="[0-9.]*"' "$coverage_file" | head -1 | grep -o '[0-9.]*')
        if [ -n "$line_rate" ] && [ "$line_rate" != "" ]; then
            # Convertir a porcentaje
            local coverage=$(awk "BEGIN {printf \"%.2f\", $line_rate * 100}")
            echo "$coverage"
        else
            echo "N/A"
        fi
    else
        echo "N/A"
    fi
}

# Función para ejecutar tests de un microservicio .NET
run_dotnet_tests() {
    local service_name=$1
    local service_path=$2
    local test_project=$3

    echo -e "\n${CYAN}═══════════════════════════════════════════════════${NC}"
    echo -e "${BOLD}${BLUE}📦 $service_name${NC}"
    echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"

    cd "$service_path"

    # Limpiar resultados anteriores
    rm -rf TestResults

    echo -e "${YELLOW}Ejecutando tests...${NC}"

    # Ejecutar tests y capturar output
    local test_output=$(dotnet test "$test_project" \
        --configuration Release \
        --logger "trx;LogFileName=test-results.trx" \
        --collect:"XPlat Code Coverage" \
        --results-directory "./TestResults" \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat=cobertura \
        /p:CoverletOutput="./TestResults/" \
        --verbosity normal 2>&1)

    # Extraer métricas del output (formato: "Total tests: 23")
    local tests_total=$(echo "$test_output" | grep "Total tests:" | grep -oP "\d+" | head -1)
    local tests_passed=$(echo "$test_output" | grep "Passed:" | grep -oP "\d+" | head -1)
    local tests_failed=$(echo "$test_output" | grep "Failed:" | grep -oP "\d+" | head -1)
    local tests_skipped=$(echo "$test_output" | grep "Skipped:" | grep -oP "\d+" | head -1)

    # Valores por defecto si están vacíos (bash parameter expansion)
    tests_total=${tests_total:-0}
    tests_passed=${tests_passed:-0}
    tests_failed=${tests_failed:-0}
    tests_skipped=${tests_skipped:-0}

    # Buscar archivo de cobertura
    local coverage_file=$(find ./TestResults -name "coverage.cobertura.xml" -o -name "*.cobertura.xml" 2>/dev/null | head -n 1)
    local coverage=$(extract_coverage_metrics "$coverage_file")

    # Mostrar resultados
    echo -e "${GREEN}✓ Tests ejecutados${NC}"
    echo ""
    echo -e "  ${BOLD}Resultados:${NC}"
    echo -e "    • Total de tests:    ${BOLD}$tests_total${NC}"
    echo -e "    • Tests exitosos:    ${GREEN}$tests_passed${NC}"
    echo -e "    • Tests fallidos:    ${RED}$tests_failed${NC}"
    echo -e "    • Tests omitidos:    ${YELLOW}$tests_skipped${NC}"
    echo -e "    • Cobertura:         ${CYAN}$coverage%${NC}"

    # Actualizar contadores globales
    TOTAL_TESTS=$((TOTAL_TESTS + tests_total))
    TOTAL_PASSED=$((TOTAL_PASSED + tests_passed))
    TOTAL_FAILED=$((TOTAL_FAILED + tests_failed))
    TOTAL_SKIPPED=$((TOTAL_SKIPPED + tests_skipped))
    TOTAL_SERVICES=$((TOTAL_SERVICES + 1))

    cd "$SCRIPT_DIR"
}

# Función para ejecutar tests del servicio Python
run_python_tests() {
    local service_name=$1
    local service_path=$2

    echo -e "\n${CYAN}═══════════════════════════════════════════════════${NC}"
    echo -e "${BOLD}${BLUE}📦 $service_name${NC}"
    echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"

    cd "$service_path"

    # Limpiar resultados anteriores
    rm -rf htmlcov .coverage coverage.xml

    echo -e "${YELLOW}Ejecutando tests...${NC}"

    # Ejecutar tests y capturar output
    local test_output=$(python3 -m pytest tests/ \
        --cov=FuzzyService/Domain \
        --cov=FuzzyService/Application \
        --cov-report=xml:coverage.xml \
        --cov-report=term \
        -v 2>&1)

    # Extraer métricas del output usando diferentes patrones
    local tests_passed=$(echo "$test_output" | grep -oP "\d+(?= passed)" | tail -1)
    local tests_failed=$(echo "$test_output" | grep -oP "\d+(?= failed)" | tail -1)
    local tests_skipped=$(echo "$test_output" | grep -oP "\d+(?= skipped)" | tail -1)

    # Valores por defecto
    tests_passed=${tests_passed:-0}
    tests_failed=${tests_failed:-0}
    tests_skipped=${tests_skipped:-0}

    # Calcular total
    local tests_total=$((tests_passed + tests_failed + tests_skipped))

    # Extraer cobertura
    local coverage=$(echo "$test_output" | grep -oP "TOTAL\s+\d+\s+\d+\s+\K\d+%" | head -1 | tr -d '%')
    coverage=${coverage:-N/A}

    # Mostrar resultados
    echo -e "${GREEN}✓ Tests ejecutados${NC}"
    echo ""
    echo -e "  ${BOLD}Resultados:${NC}"
    echo -e "    • Total de tests:    ${BOLD}$tests_total${NC}"
    echo -e "    • Tests exitosos:    ${GREEN}$tests_passed${NC}"
    echo -e "    • Tests fallidos:    ${RED}$tests_failed${NC}"
    echo -e "    • Tests omitidos:    ${YELLOW}$tests_skipped${NC}"
    echo -e "    • Cobertura:         ${CYAN}$coverage%${NC}"

    # Actualizar contadores globales
    TOTAL_TESTS=$((TOTAL_TESTS + tests_total))
    TOTAL_PASSED=$((TOTAL_PASSED + tests_passed))
    TOTAL_FAILED=$((TOTAL_FAILED + tests_failed))
    TOTAL_SKIPPED=$((TOTAL_SKIPPED + tests_skipped))
    TOTAL_SERVICES=$((TOTAL_SERVICES + 1))

    cd "$SCRIPT_DIR"
}

# Banner inicial
echo ""
echo -e "${BOLD}${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BOLD}${BLUE}║                                                        ║${NC}"
echo -e "${BOLD}${BLUE}║           REPORTE DE PRUEBAS UNITARIAS                 ║${NC}"
echo -e "${BOLD}${BLUE}║              Proyecto HydroEspinaca                    ║${NC}"
echo -e "${BOLD}${BLUE}║                                                        ║${NC}"
echo -e "${BOLD}${BLUE}╚════════════════════════════════════════════════════════╝${NC}"
echo ""

# Ejecutar tests de todos los microservicios

# Hardware Services
echo -e "${BOLD}${GREEN}━━━ HARDWARE SERVICES ━━━${NC}"

run_dotnet_tests \
    "Sensor Service - Domain" \
    "./hardware-project/sensor-service" \
    "SensorService.Domain.Tests/SensorService.Domain.Tests.csproj"

run_dotnet_tests \
    "Sensor Service - Application" \
    "./hardware-project/sensor-service" \
    "SensorService.Application.Tests/SensorService.Application.Tests.csproj"

run_dotnet_tests \
    "Actuator Service - Domain" \
    "./hardware-project/actuator-service" \
    "ActuatorService.Domain.Tests/ActuatorService.Domain.Tests.csproj"

run_dotnet_tests \
    "Actuator Service - Application" \
    "./hardware-project/actuator-service" \
    "ActuatorService.Application.Tests/ActuatorService.Application.Tests.csproj"

# Software Services
echo -e "\n${BOLD}${GREEN}━━━ SOFTWARE SERVICES ━━━${NC}"

# auth-service
run_dotnet_tests \
    "Auth Service - Domain" \
    "./software-project/auth-service" \
    "AuthService.Domain.Tests/AuthService.Domain.Tests.csproj"

run_dotnet_tests \
    "Auth Service - Application" \
    "./software-project/auth-service" \
    "AuthService.Application.Tests/AuthService.Application.Tests.csproj"

#bff-service
run_dotnet_tests \
    "BFF Service - Domain" \
    "./software-project/bff-service" \
    "BffService.Domain.Tests/BffService.Domain.Tests.csproj"

run_dotnet_tests \
    "BFF Service - Application" \
    "./software-project/bff-service" \
    "BffService.Application.Tests/BffService.Application.Tests.csproj"

# fuzzy-service
run_python_tests \
    "Fuzzy Service" \
    "./software-project/fuzzy-service"

# notification-service
run_dotnet_tests \
    "Notification Service - Domain" \
    "./software-project/notification-service" \
    "NotificationService.Domain.Tests/NotificationService.Domain.Tests.csproj"

run_dotnet_tests \
    "Notification Service - Application" \
    "./software-project/notification-service" \
    "NotificationService.Application.Tests/NotificationService.Application.Tests.csproj"

#chatbot-service
run_dotnet_tests \
    "Chatbot Service - Domain" \
    "./software-project/chatbot-service" \
    "ChatbotService.Domain.Tests/ChatbotService.Domain.Tests.csproj"

run_dotnet_tests \
    "Chatbot Service - Application" \
    "./software-project/chatbot-service" \
    "ChatbotService.Application.Tests/ChatbotService.Application.Tests.csproj"

# weather-service
run_dotnet_tests \
    "Weather Service - Domain" \
    "./software-project/weather-service" \
    "WeatherService.Domain.Tests/WeatherService.Domain.Tests.csproj"

run_dotnet_tests \
    "Weather Service - Application" \
    "./software-project/weather-service" \
    "WeatherService.Application.Tests/WeatherService.Application.Tests.csproj"

# bi-service
run_dotnet_tests \
    "BI Service - Domain" \
    "./software-project/bi-service" \
    "BiService.Domain.Tests/BiService.Domain.Tests.csproj"

run_dotnet_tests \
    "BI Service - Application" \
    "./software-project/bi-service" \
    "BiService.Application.Tests/BiService.Application.Tests.csproj"



# Reporte consolidado final
echo ""
echo ""
echo -e "${BOLD}${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BOLD}${BLUE}║                                                        ║${NC}"
echo -e "${BOLD}${BLUE}║              RESUMEN CONSOLIDADO                       ║${NC}"
echo -e "${BOLD}${BLUE}║                                                        ║${NC}"
echo -e "${BOLD}${BLUE}╚════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "  ${BOLD}Microservicios analizados:${NC}    ${CYAN}$TOTAL_SERVICES${NC}"
echo -e "  ${BOLD}Total de tests ejecutados:${NC}    ${BOLD}$TOTAL_TESTS${NC}"
echo -e "  ${BOLD}Tests exitosos:${NC}               ${GREEN}$TOTAL_PASSED${NC}"
echo -e "  ${BOLD}Tests fallidos:${NC}               ${RED}$TOTAL_FAILED${NC}"
echo -e "  ${BOLD}Tests omitidos:${NC}               ${YELLOW}$TOTAL_SKIPPED${NC}"
echo ""

# Calcular tasa de éxito
if [ $TOTAL_TESTS -gt 0 ]; then
    SUCCESS_RATE=$(echo "scale=2; $TOTAL_PASSED * 100 / $TOTAL_TESTS" | bc)
    echo -e "  ${BOLD}Tasa de éxito:${NC}                ${GREEN}${SUCCESS_RATE}%${NC}"
fi

echo ""
echo -e "${BOLD}${BLUE}════════════════════════════════════════════════════════${NC}"
echo ""

if [ $TOTAL_FAILED -eq 0 ]; then
    echo -e "${GREEN}${BOLD}✓ Todos los tests pasaron exitosamente${NC}"
else
    echo -e "${RED}${BOLD}✗ Algunos tests fallaron. Revisar logs arriba.${NC}"
    exit 1
fi

echo ""
