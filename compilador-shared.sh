#!/bin/bash
set -e

SHARED_DIR="./shared/HydroEspinaca.Shared"
OUTPUT_DIR="$SHARED_DIR/nupkg"
PROJECT_NAME="HydroEspinaca.Shared"
NUGET_SOURCE_NAME="hydro-local"
CSPROJ_PATH="$SHARED_DIR/$PROJECT_NAME.csproj"
SERVICES_ROOTS=(
  "./hardware-project/sensor-service"
  "./hardware-project/actuator-service"
  "./software-project/auth-service"
)

echo "🧹 Limpiando artefactos anteriores..."
rm -rf "$SHARED_DIR/bin" "$SHARED_DIR/obj" "$OUTPUT_DIR"
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

# ➕ Agregar feed local si no existe
if ! dotnet nuget list source | grep -q "$NUGET_SOURCE_NAME"; then
    dotnet nuget add source "$OUTPUT_DIR" --name "$NUGET_SOURCE_NAME" --configfile ./NuGet.config
else
    echo "✔ NuGet source '$NUGET_SOURCE_NAME' ya existe"
fi

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
    for sln in "$SERVICE_ROOT"/*.sln; do
        if [[ -f "$sln" ]]; then
            echo "🔧 Restaurando $sln"
            dotnet restore "$sln" --no-cache
        fi
    done
done

# 📁 Copiar al feed local .nuget/packages
CUSTOM_FEED_DIR="./.nuget/packages/$PROJECT_NAME/$PACKAGE_VERSION"
mkdir -p "$CUSTOM_FEED_DIR"
cp "$PACKAGE_FILE" "$CUSTOM_FEED_DIR/${PROJECT_NAME}.${PACKAGE_VERSION}.nupkg"

echo "✅ Paquete '$PROJECT_NAME' versión $PACKAGE_VERSION listo y actualizado en todos los proyectos"