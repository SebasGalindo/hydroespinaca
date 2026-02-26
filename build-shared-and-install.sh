#!/bin/bash
set -e

# Directorios (debe ejecutarse desde la raíz del repositorio)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

SHARED_DIR="./shared/HydroEspinaca.Shared"
OUTPUT_DIR="./deploy-artifacts/nuget"  # Consistente con NuGet.config
PROJECT_NAME="HydroEspinaca.Shared"
NUGET_SOURCE_NAME="hydro-local"
CSPROJ_PATH="$SHARED_DIR/$PROJECT_NAME.csproj"

# Servicios que consumen HydroEspinaca.Shared
SERVICES_ROOTS=(
  "./software-project/auth-service"
  "./software-project/notification-service"
  "./software-project/bff-service"
  "./software-project/bi-service"
  "./software-project/weather-service"
  "./software-project/chatbot-service"
  "./hardware-project/sensor-service"
  "./hardware-project/actuator-service"
)

echo "🧹 Limpiando artefactos anteriores del proyecto Shared..."
rm -rf "$SHARED_DIR/bin" "$SHARED_DIR/obj"
mkdir -p "$OUTPUT_DIR"

# 🔖 Generar versión dinámica con timestamp
TIMESTAMP=$(date +"%Y%m%d%H%M%S")
PACKAGE_VERSION="1.0.3-dev-$TIMESTAMP"
echo "📦 Versión: $PACKAGE_VERSION"

# 📦 Empaquetar sin tocar el .csproj
dotnet clean "$SHARED_DIR" -v quiet
dotnet pack "$SHARED_DIR" \
    -o "$OUTPUT_DIR" \
    --include-symbols \
    --include-source \
    -p:Version="$PACKAGE_VERSION"

PACKAGE_FILE="$OUTPUT_DIR/${PROJECT_NAME}.${PACKAGE_VERSION}.nupkg"
if [[ ! -f "$PACKAGE_FILE" ]]; then
    echo "❌ Error: paquete no generado: $PACKAGE_FILE"
    exit 1
fi

echo "✅ Paquete generado: $PACKAGE_FILE"

# Verificar que existe NuGet.config con hydro-local configurado
if grep -q "hydro-local" ./NuGet.config; then
    echo "✔ NuGet.config tiene configurado 'hydro-local'"
else
    echo "⚠ Advertencia: NuGet.config no tiene 'hydro-local' configurado"
fi

# Guardar versión generada para otros scripts
echo "$PACKAGE_VERSION" > "$OUTPUT_DIR/latest-version.txt"
echo "📝 Versión guardada en: $OUTPUT_DIR/latest-version.txt"

# 🔄 Actualizar referencias de versión en consumidores
echo "✏ Actualizando referencias..."
for SERVICE_ROOT in "${SERVICES_ROOTS[@]}"; do
    echo "📂 Escaneando $SERVICE_ROOT"
    find "$SERVICE_ROOT" -name "*.csproj" | while read -r csproj; do
        if grep -q "<PackageReference Include=\"$PROJECT_NAME\"" "$csproj"; then
            echo "  👉 $csproj"
            sed -i "s|<PackageReference Include=\"$PROJECT_NAME\" Version=\"[^\"]*\"|<PackageReference Include=\"$PROJECT_NAME\" Version=\"$PACKAGE_VERSION\"|g" "$csproj"
        fi
    done
done

# 🧹 Limpiar bin/obj en consumidores
echo "🧹 Limpiando bin/obj..."
for SERVICE_ROOT in "${SERVICES_ROOTS[@]}"; do
    find "$SERVICE_ROOT" -type d \( -name bin -o -name obj \) -exec rm -rf {} +
done

# 🧽 Limpiar cache global
GLOBAL_NUGET_CACHE="$HOME/.nuget/packages/${PROJECT_NAME,,}"
echo "🧽 Limpiando cache global NuGet: $GLOBAL_NUGET_CACHE"
rm -rf "$GLOBAL_NUGET_CACHE/$PACKAGE_VERSION"

# 🔄 Restaurar soluciones
echo "🔄 Restaurando dependencias..."
for SERVICE_ROOT in "${SERVICES_ROOTS[@]}"; do
    # Solo procesar si el directorio existe
    if [[ ! -d "$SERVICE_ROOT" ]]; then
        echo "⚠ Omitiendo $SERVICE_ROOT (no existe)"
        continue
    fi

    for sln in "$SERVICE_ROOT"/*.sln; do
        if [[ -f "$sln" ]]; then
            echo "🔧 Restaurando $sln"
            dotnet restore "$sln" --configfile ./NuGet.config --no-cache
        fi
    done
done

echo ""
echo "✅ Paquete '$PROJECT_NAME' versión $PACKAGE_VERSION listo y actualizado en todos los proyectos"
echo "📦 Ubicación: $OUTPUT_DIR/${PROJECT_NAME}.${PACKAGE_VERSION}.nupkg"
