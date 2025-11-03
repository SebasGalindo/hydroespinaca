#!/bin/bash

# Script para levantar la app móvil de HydroEspinaca con Expo en modo tunnel
# Limpia caché y verifica tipos antes de iniciar
#
# NOTA: Metro transpila directamente desde packages/shared/src/
#       No es necesario compilar el paquete compartido para React Native

set -e  # Exit on error

echo "🚀 HydroEspinaca Mobile - Iniciando en modo tunnel..."
echo ""

# Colores para output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Navegar al directorio raíz del monorepo
cd "$(dirname "$0")/../.."
MONOREPO_ROOT=$(pwd)
echo -e "${BLUE}📁 Monorepo root: ${MONOREPO_ROOT}${NC}"
echo ""

# Paso 1: Limpiar caché de Metro y Expo
echo -e "${YELLOW}🧹 Paso 1/5: Limpiando caché de Metro y Expo...${NC}"
cd "${MONOREPO_ROOT}/apps/mobile"
rm -rf .expo
rm -rf node_modules/.cache
rm -rf $TMPDIR/metro-* 2>/dev/null || true
rm -rf $TMPDIR/haste-map-* 2>/dev/null || true
echo -e "${GREEN}✓ Caché limpiada${NC}"
echo ""

# Paso 2: Nota sobre paquete compartido
echo -e "${YELLOW}📦 Paso 2/5: Verificando paquete compartido...${NC}"
echo -e "${BLUE}ℹ️  Metro transpila directamente desde 'src/' - no se requiere build${NC}"
echo -e "${GREEN}✓ Paquete compartido listo (source code)${NC}"
echo ""

# Paso 3: Instalar dependencias (si es necesario)
echo -e "${YELLOW}📥 Paso 3/5: Verificando dependencias...${NC}"
cd "${MONOREPO_ROOT}"
if [ ! -d "node_modules" ] || [ ! -d "apps/mobile/node_modules" ]; then
  echo "Instalando dependencias..."
  pnpm install
else
  echo "Dependencias ya instaladas"
fi
echo -e "${GREEN}✓ Dependencias verificadas${NC}"
echo ""

# Paso 4: Verificar TypeScript
echo -e "${YELLOW}🔍 Paso 4/5: Verificando tipos TypeScript...${NC}"
cd "${MONOREPO_ROOT}/apps/mobile"
pnpm exec tsc --noEmit
echo -e "${GREEN}✓ TypeScript OK${NC}"
echo ""

# Paso 5: Iniciar Expo en modo tunnel
echo -e "${YELLOW}🌐 Paso 5/5: Iniciando Expo en modo tunnel...${NC}"
echo ""
echo -e "${GREEN}┌─────────────────────────────────────────────┐${NC}"
echo -e "${GREEN}│  Expo Dev Server - Modo Tunnel              │${NC}"
echo -e "${GREEN}│                                             │${NC}"
echo -e "${GREEN}│  Escanea el QR con Expo Go para conectar   │${NC}"
echo -e "${GREEN}│  desde cualquier red                        │${NC}"
echo -e "${GREEN}└─────────────────────────────────────────────┘${NC}"
echo ""

# Limpiar caché de Metro al iniciar
pnpm exec expo start --tunnel --clear

# Nota: Para detener el servidor, presiona Ctrl+C
