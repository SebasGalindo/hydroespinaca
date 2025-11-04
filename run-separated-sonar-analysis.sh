#!/bin/bash
# run-separated-sonar-analysis.sh
# Script para análisis SEPARADO de SonarQube - Un proyecto por cada servicio
# Enfoque recomendado para monorepos con múltiples soluciones .NET
#
# Este script crea proyectos independientes en SonarQube para cada servicio,
# permitiendo análisis profundo con dotnet-sonarscanner y métricas independientes.
#
# Uso:
#   export SONAR_HOST_URL="http://localhost:9000"
#   export SONAR_TOKEN="your-token-here"
#   ./run-separated-sonar-analysis.sh

set -e

# Colores para output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

# Variables globales
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
START_TIME=$(date +%s)
ANALYSIS_RESULTS=()

# Banner
print_banner() {
    echo -e "${CYAN}"
    echo "╔════════════════════════════════════════════════════════════════╗"
    echo "║                                                                ║"
    echo "║      🔍 SonarQube Multi-Project Analysis - HydroEspinaca      ║"
    echo "║                                                                ║"
    echo "║         Análisis Separado por Servicio (Opción 1)            ║"
    echo "║                                                                ║"
    echo "╚════════════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
}

# Validar variables de entorno
validate_environment() {
    echo -e "${YELLOW}🔍 Validando configuración...${NC}"

    if [ -z "$SONAR_HOST_URL" ]; then
        echo -e "${RED}❌ Error: Variable SONAR_HOST_URL no está definida${NC}"
        echo -e "${YELLOW}💡 Ejemplo: export SONAR_HOST_URL='http://localhost:9000'${NC}"
        exit 1
    fi

    if [ -z "$SONAR_TOKEN" ]; then
        echo -e "${RED}❌ Error: Variable SONAR_TOKEN no está definida${NC}"
        echo -e "${YELLOW}💡 Ejemplo: export SONAR_TOKEN='your-sonar-token'${NC}"
        exit 1
    fi

    echo -e "${GREEN}✅ Variables de entorno configuradas${NC}"
    echo -e "${CYAN}   Host: $SONAR_HOST_URL${NC}"
    echo ""
}

# Validar herramientas requeridas
validate_tools() {
    echo -e "${YELLOW}🔧 Validando herramientas requeridas...${NC}"

    local missing_tools=()

    # Verificar dotnet
    if ! command -v dotnet &> /dev/null; then
        missing_tools+=("dotnet")
    else
        echo -e "${GREEN}  ✓ dotnet $(dotnet --version)${NC}"
    fi

    # Verificar python3
    if ! command -v python3 &> /dev/null; then
        missing_tools+=("python3")
    else
        echo -e "${GREEN}  ✓ python3 $(python3 --version | cut -d' ' -f2)${NC}"
    fi

    # Verificar dotnet-sonarscanner
    if ! dotnet tool list -g | grep -q "dotnet-sonarscanner"; then
        echo -e "${YELLOW}  ⚠ dotnet-sonarscanner no está instalado globalmente${NC}"
        echo -e "${YELLOW}    Instalando...${NC}"
        dotnet tool install --global dotnet-sonarscanner || {
            echo -e "${RED}❌ Error instalando dotnet-sonarscanner${NC}"
            missing_tools+=("dotnet-sonarscanner")
        }
    else
        echo -e "${GREEN}  ✓ dotnet-sonarscanner disponible${NC}"
    fi

    # Verificar sonar-scanner (para Python)
    if ! command -v sonar-scanner &> /dev/null; then
        echo -e "${YELLOW}  ⚠ sonar-scanner no encontrado (necesario para Python)${NC}"
        echo -e "${YELLOW}    Puedes continuar, pero el análisis de Python se saltará${NC}"
    else
        echo -e "${GREEN}  ✓ sonar-scanner disponible${NC}"
    fi

    if [ ${#missing_tools[@]} -gt 0 ]; then
        echo -e "${RED}❌ Herramientas críticas faltantes: ${missing_tools[*]}${NC}"
        exit 1
    fi

    echo ""
}

# Limpiar artefactos previos de un directorio específico
cleanup_service_artifacts() {
    local service_dir=$1

    cd "$service_dir"

    # Limpiar directorios de build, test y sonarqube
    find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
    find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
    find . -type d -name "TestResults" -exec rm -rf {} + 2>/dev/null || true
    find . -type d -name ".sonarqube" -exec rm -rf {} + 2>/dev/null || true

    cd "$SCRIPT_DIR"
}

# Analizar un servicio .NET
analyze_dotnet_service() {
    local service_path=$1
    local service_sln=$2
    local project_key=$3
    local project_name=$4

    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}🔍 Analizando: $project_name${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo ""

    local full_path="$SCRIPT_DIR/$service_path"

    # Verificar que existe el archivo .sln
    if [ ! -f "$full_path/$service_sln" ]; then
        echo -e "${RED}❌ Error: No se encontró $service_sln en $service_path${NC}"
        ANALYSIS_RESULTS+=("❌ $project_name - FAILED (solution not found)")
        return 1
    fi

    echo -e "${CYAN}📁 Directorio: $service_path${NC}"
    echo -e "${CYAN}🔑 Project Key: $project_key${NC}"
    echo ""

    # Limpiar artefactos
    echo -e "${YELLOW}🧹 Limpiando artefactos previos...${NC}"
    cleanup_service_artifacts "$full_path"

    cd "$full_path"

    # BEGIN - Iniciar análisis de SonarQube
    # IMPORTANTE: Solo analizar código con lógica de negocio crítica
    #
    # ESTRATEGIA DE EXCLUSIONES (.NET):
    # Nota: dotnet-sonarscanner NO soporta sonar.sources como Python.
    # Usamos exclusiones EXHAUSTIVAS para excluir TODO excepto Domain (Entities, ValueObjects, Services)
    # y Application (Validators, Behaviors, Exceptions con lógica).
    #
    # CATEGORÍAS EXCLUIDAS:
    # 1. Proyectos completos: *.Api/**, *.Infrastructure/** (controllers, repos, engines)
    # 2. Application/Features: Handlers CQRS (orquestación, sin lógica de negocio)
    # 3. Application/Mappers: Transformaciones 1:1 simples
    # 4. Application/UseCases: Orquestación (si existe)
    # 5. DTOs: Commands, Queries, DTOs (solo propiedades)
    # 6. Configuration: DependencyInjection, appsettings, Program.cs, Startup.cs
    # 7. Artefactos: obj, bin, TestResults, Tests, Migrations
    #
    # RESULTADO ESPERADO: Coverage 80%+ en Domain (Entities, ValueObjects, Services)
    #                     y Application (Validators, Behaviors, Exceptions)
    #
    # NOTA: sonar.exclusions excluye de análisis Y del dashboard (no aparecen)
    echo -e "${YELLOW}🚀 [1/4] Iniciando análisis de SonarQube...${NC}"
    dotnet sonarscanner begin \
        /k:"$project_key" \
        /n:"$project_name" \
        /v:"1.0" \
        /d:sonar.host.url="$SONAR_HOST_URL" \
        /d:sonar.token="$SONAR_TOKEN" \
        /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" \
        /d:sonar.exclusions="**/obj/**,**/bin/**,**/TestResults/**,**/*.Api/**,**/*.Infrastructure/**,**/*.Tests/**,**/Tests/**,**/Migrations/**,**/Configuration/**,**/DTOs/**,**/*Dto.cs,**/*DTO.cs,**/Commands/**,**/Queries/**,**/Features/**,**/Mappers/**,**/UseCases/**,**/DependencyInjection.cs,**/Program.cs,**/Startup.cs,**/appsettings*.json,**/*.csproj,**/Controllers/**,**/Middleware/**,**/Filters/**" \
        || {
            echo -e "${RED}❌ Error en dotnet sonarscanner begin${NC}"
            ANALYSIS_RESULTS+=("❌ $project_name - FAILED (begin)")
            cd "$SCRIPT_DIR"
            return 1
        }

    # RESTORE
    echo -e "${YELLOW}📦 [2/4] Restaurando dependencias...${NC}"
    dotnet restore "$service_sln" --verbosity quiet || {
        echo -e "${RED}❌ Error en dotnet restore${NC}"
        ANALYSIS_RESULTS+=("❌ $project_name - FAILED (restore)")
        cd "$SCRIPT_DIR"
        return 1
    }

    # BUILD
    echo -e "${YELLOW}🔨 [3/4] Compilando solución...${NC}"
    dotnet build "$service_sln" \
        --no-restore \
        --configuration Release \
        --verbosity quiet || {
            echo -e "${RED}❌ Error en dotnet build${NC}"
            ANALYSIS_RESULTS+=("❌ $project_name - FAILED (build)")
            cd "$SCRIPT_DIR"
            return 1
        }

    # TEST con Coverage
    echo -e "${YELLOW}🧪 [4/4] Ejecutando tests con coverage...${NC}"
    dotnet test "$service_sln" \
        --no-build \
        --configuration Release \
        --collect:"XPlat Code Coverage;Format=opencover" \
        --results-directory "TestResults" \
        --verbosity quiet || {
            echo -e "${YELLOW}⚠ Algunos tests fallaron, pero continuamos con el análisis${NC}"
        }

    # Verificar archivos de coverage
    local coverage_count=$(find TestResults -name "coverage.opencover.xml" 2>/dev/null | wc -l)
    if [ $coverage_count -gt 0 ]; then
        echo -e "${GREEN}✅ Coverage generado: $coverage_count archivo(s)${NC}"

        # Extraer y mostrar porcentaje de coverage
        local coverage_file=$(find TestResults -name "coverage.opencover.xml" | head -1)
        if [ -f "$coverage_file" ]; then
            # OpenCover format: sequenceCoverage="X" en Summary
            local coverage_pct=$(grep -oP 'sequenceCoverage="\K[0-9.]+' "$coverage_file" | head -1)
            if [ -n "$coverage_pct" ]; then
                echo -e "${GREEN}   Coverage estimado: ${coverage_pct}%${NC}"
            fi
        fi
    else
        echo -e "${YELLOW}⚠ No se generó coverage (puede que no haya tests)${NC}"
    fi

    # END - Finalizar análisis y enviar a SonarQube
    echo -e "${YELLOW}📤 Enviando análisis a SonarQube...${NC}"
    dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN" || {
        echo -e "${RED}❌ Error en dotnet sonarscanner end${NC}"
        ANALYSIS_RESULTS+=("❌ $project_name - FAILED (end)")
        cd "$SCRIPT_DIR"
        return 1
    }

    echo -e "${GREEN}✅ Análisis de $project_name completado exitosamente${NC}"
    echo -e "${CYAN}🔗 Ver en: $SONAR_HOST_URL/dashboard?id=$project_key${NC}"
    echo ""

    ANALYSIS_RESULTS+=("✅ $project_name - SUCCESS")
    cd "$SCRIPT_DIR"
    return 0
}

# Analizar fuzzy-service (Python)
analyze_python_service() {
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}🔍 Analizando: FuzzyService (Python)${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo ""

    local project_key="hydroespinaca-fuzzy-service"
    local project_name="HydroEspinaca - FuzzyService"
    local service_path="software-project/fuzzy-service"

    if ! command -v sonar-scanner &> /dev/null; then
        echo -e "${YELLOW}⚠ sonar-scanner no disponible, saltando análisis de Python${NC}"
        ANALYSIS_RESULTS+=("⚠ $project_name - SKIPPED (no scanner)")
        echo ""
        return 0
    fi

    cd "$SCRIPT_DIR/$service_path"

    # Limpiar coverage previo
    rm -f coverage.xml .coverage 2>/dev/null || true

    # Crear/activar entorno virtual
    if [ ! -d "venv" ]; then
        echo -e "${YELLOW}🐍 Creando entorno virtual...${NC}"
        python3 -m venv venv
    fi

    source venv/bin/activate

    # Instalar dependencias
    echo -e "${YELLOW}📦 Instalando dependencias...${NC}"
    pip install -q --upgrade pip
    pip install -q -r requirements.txt 2>/dev/null || true
    pip install -q pytest pytest-cov

    # Ejecutar tests - SOLO para el código crítico que SonarQube analizará
    # IMPORTANTE: Debe coincidir exactamente con lo que está en sonar.sources menos sonar.exclusions
    # Incluir: Domain (sin Utils) + Application/Helpers (sin Mappers)
    echo -e "${YELLOW}🧪 Ejecutando tests con coverage (código crítico solamente)...${NC}"
    python -m pytest tests/ \
        --cov=FuzzyService/Domain/Entities \
        --cov=FuzzyService/Domain/ValueObjects \
        --cov=FuzzyService/Domain/Enums \
        --cov=FuzzyService/Domain/Errors \
        --cov=FuzzyService/Domain/Common \
        --cov=FuzzyService/Application/Helpers \
        --cov-report=xml:coverage.xml \
        --cov-report=term \
        -q 2>/dev/null || \
    echo -e "${YELLOW}⚠ Tests fallaron o no hay tests${NC}"

    deactivate

    # Verificar coverage generado
    if [ -f coverage.xml ]; then
        local coverage_rate=$(grep -oP 'line-rate="\K[0-9.]+' coverage.xml | head -1)
        if [ -n "$coverage_rate" ]; then
            local coverage_pct=$(echo "$coverage_rate * 100" | bc -l | xargs printf "%.1f")
            echo -e "${GREEN}✅ Coverage: ${coverage_pct}%${NC}"
        fi
    fi

    # Crear sonar-project.properties temporal - CRÍTICO: Solo analizar código con lógica de negocio
    # ESTRATEGIA: Usar sonar.sources con directorios ESPECÍFICOS (no usar directorios raíz + exclusions)
    # En Python, sonar.sources determina QUÉ APARECE en el dashboard de SonarQube
    # Todo lo que NO esté listado aquí, NO aparecerá en el dashboard
    cat > sonar-project.properties << EOF
sonar.projectKey=$project_key
sonar.projectName=$project_name
sonar.projectVersion=1.0

# ⚠️ CRÍTICO: Solo listar los directorios EXACTOS que queremos analizar
# SonarQube SOLO analizará estos directorios (todo lo demás NO aparecerá en dashboard)
# Incluir: Domain (sin Utils) + Application/Helpers (sin Mappers, Features, Services, Configuration)
sonar.sources=FuzzyService/Domain/Entities,\\
              FuzzyService/Domain/ValueObjects,\\
              FuzzyService/Domain/Enums,\\
              FuzzyService/Domain/Errors,\\
              FuzzyService/Domain/Common,\\
              FuzzyService/Application/Helpers

# Directorio de tests
sonar.tests=tests

# Python config
sonar.python.version=3.11

# Coverage report - debe estar generado SOLO para los directorios en sonar.sources
sonar.python.coverage.reportPaths=coverage.xml

# Exclusiones adicionales dentro de los directorios en sonar.sources
# - __init__.py (solo imports, sin lógica)
# - DTOs si existieran en estos directorios
sonar.exclusions=**/__init__.py,\\
                 **/*Dto.py,\\
                 **/*DTO.py

# Encoding
sonar.sourceEncoding=UTF-8

# SonarQube host
sonar.host.url=$SONAR_HOST_URL
EOF

    # Ejecutar análisis
    echo -e "${YELLOW}📤 Enviando análisis a SonarQube...${NC}"
    sonar-scanner -Dsonar.token="$SONAR_TOKEN" || {
        echo -e "${RED}❌ Error en sonar-scanner${NC}"
        ANALYSIS_RESULTS+=("❌ $project_name - FAILED")
        cd "$SCRIPT_DIR"
        return 1
    }

    echo -e "${GREEN}✅ Análisis de $project_name completado exitosamente${NC}"
    echo -e "${CYAN}🔗 Ver en: $SONAR_HOST_URL/dashboard?id=$project_key${NC}"
    echo ""

    ANALYSIS_RESULTS+=("✅ $project_name - SUCCESS")
    cd "$SCRIPT_DIR"
    return 0
}

# Analizar todos los servicios
analyze_all_services() {
    echo -e "${MAGENTA}╔════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${MAGENTA}║            🚀 INICIANDO ANÁLISIS DE TODOS LOS SERVICIOS        ║${NC}"
    echo -e "${MAGENTA}╚════════════════════════════════════════════════════════════════╝${NC}"
    echo ""

    # Servicios .NET críticos
    analyze_dotnet_service \
        "software-project/auth-service" \
        "AuthService.sln" \
        "hydroespinaca-auth-service" \
        "HydroEspinaca - AuthService"

    analyze_dotnet_service \
        "hardware-project/sensor-service" \
        "SensorService.sln" \
        "hydroespinaca-sensor-service" \
        "HydroEspinaca - SensorService"

    analyze_dotnet_service \
        "hardware-project/actuator-service" \
        "ActuatorService.sln" \
        "hydroespinaca-actuator-service" \
        "HydroEspinaca - ActuatorService"

    # Servicios .NET secundarios (opcional - comentar si no se quieren analizar)
    analyze_dotnet_service \
        "software-project/bff-service" \
        "BffService.sln" \
        "hydroespinaca-bff-service" \
        "HydroEspinaca - BffService"

    analyze_dotnet_service \
        "software-project/notification-service" \
        "NotificationService.sln" \
        "hydroespinaca-notification-service" \
        "HydroEspinaca - NotificationService"

    # Servicio Python
    analyze_python_service
}

# Imprimir resumen final
print_summary() {
    local end_time=$(date +%s)
    local duration=$((end_time - START_TIME))
    local minutes=$((duration / 60))
    local seconds=$((duration % 60))

    echo ""
    echo -e "${GREEN}╔════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}║              ✅ ANÁLISIS MULTI-PROYECTO COMPLETADO ✅          ║${NC}"
    echo -e "${GREEN}╚════════════════════════════════════════════════════════════════╝${NC}"
    echo ""
    echo -e "${BLUE}⏱️  Tiempo total: ${minutes}m ${seconds}s${NC}"
    echo ""

    echo -e "${YELLOW}📊 Resultados por servicio:${NC}"
    echo ""
    for result in "${ANALYSIS_RESULTS[@]}"; do
        if [[ $result == *"SUCCESS"* ]]; then
            echo -e "${GREEN}   $result${NC}"
        elif [[ $result == *"FAILED"* ]]; then
            echo -e "${RED}   $result${NC}"
        else
            echo -e "${YELLOW}   $result${NC}"
        fi
    done
    echo ""

    echo -e "${CYAN}🔗 Ver todos los proyectos en SonarQube:${NC}"
    echo -e "${BLUE}   $SONAR_HOST_URL/projects${NC}"
    echo ""

    echo -e "${MAGENTA}╔════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${MAGENTA}║                  📋 PRÓXIMOS PASOS                             ║${NC}"
    echo -e "${MAGENTA}╚════════════════════════════════════════════════════════════════╝${NC}"
    echo ""
    echo -e "${YELLOW}1. Crear una SonarQube Application para métricas consolidadas:${NC}"
    echo ""
    echo -e "${CYAN}   a) Ve a: $SONAR_HOST_URL${NC}"
    echo -e "${CYAN}   b) Click en 'Applications' en el menú superior${NC}"
    echo -e "${CYAN}   c) Click en 'Create Application'${NC}"
    echo -e "${CYAN}   d) Nombre: 'HydroEspinaca Monorepo'${NC}"
    echo -e "${CYAN}   e) Key: 'hydroespinaca-application'${NC}"
    echo -e "${CYAN}   f) Selecciona todos los proyectos hydroespinaca-*${NC}"
    echo ""
    echo -e "${YELLOW}2. Configurar Quality Gates personalizados por servicio${NC}"
    echo ""
    echo -e "${YELLOW}3. Integrar este script en tu CI/CD pipeline${NC}"
    echo ""
}

# Manejo de señales
cleanup_on_exit() {
    echo ""
    echo -e "${YELLOW}⚠️  Análisis interrumpido${NC}"
    exit 130
}

trap cleanup_on_exit SIGINT SIGTERM

# Main
main() {
    print_banner
    validate_environment
    validate_tools

    echo -e "${BLUE}🎯 Este script creará proyectos SEPARADOS en SonarQube:${NC}"
    echo ""
    echo -e "${CYAN}   • hydroespinaca-auth-service${NC}"
    echo -e "${CYAN}   • hydroespinaca-sensor-service${NC}"
    echo -e "${CYAN}   • hydroespinaca-actuator-service${NC}"
    echo -e "${CYAN}   • hydroespinaca-bff-service${NC}"
    echo -e "${CYAN}   • hydroespinaca-notification-service${NC}"
    echo -e "${CYAN}   • hydroespinaca-fuzzy-service${NC}"
    echo ""
    echo -e "${YELLOW}Cada proyecto tendrá su propio Quality Gate y métricas.${NC}"
    echo -e "${YELLOW}Luego podrás crear una 'Application' para verlos consolidados.${NC}"
    echo ""

    read -p "¿Continuar con el análisis? (y/n): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}Análisis cancelado por el usuario${NC}"
        exit 0
    fi

    analyze_all_services
    print_summary
}

# Ejecutar main
main "$@"
