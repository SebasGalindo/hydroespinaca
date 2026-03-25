# 📱 Plan de Desarrollo Mobile — HydroEspinaca

> **Objetivo:** Llevar la app móvil al mismo nivel funcional que la web, adaptando la UX a patrones nativos de React Native.

---

## 📊 Diagnóstico Actual

### Lo que TIENE el móvil (2 pantallas)
| Pantalla | Descripción |
|----------|-------------|
| `LoginScreen` | Login con imagen de fondo + `LoginForm` |
| `DashboardScreen` | Monolito de 613 líneas: weather, variables, controller status, reglas fuzzy read-only |

### Lo que TIENE la web (10 rutas)
| Ruta Web | Funcionalidad | ¿Existe en móvil? |
|----------|---------------|--------------------|
| `/login` | Autenticación | ✅ |
| `/dashboard` | Monitoreo en tiempo real (weather, variables, controller) | ✅ (parcial) |
| `/analytics` | Gráficos de series de tiempo, boxplots, actuadores | ❌ |
| `/consumo` | BI: costos, consumo, producción, rentabilidad | ❌ |
| `/rutinas` | Lista de sistemas fuzzy (CRUD, import/export) | ❌ |
| `/rutinas/[id]` | Detalle fuzzy: variables, términos, reglas, simulación (CRUD completo) | ❌ |
| `/perfil` | Perfil de usuario | ❌ |
| `/admin/access` | Gestión de usuarios y roles (CRUD) | ❌ |
| `/admin/dashboard` | Sesiones activas | ❌ |

### Lo que tenía el viejo móvil (referencia `olv_frontend_version`)
- **18 pantallas** con bottom tabs + drawer "Más"
- Charts con `victory-native`, CRUD con BottomSheets
- Fuzzy: lista → detalle con tabs (variables/reglas/rutinas) + MembershipChart
- Variables/Sensores/Actuadores CRUD
- Lecturas con filtros y tabla

### Recursos disponibles del shared package
- **3 stores Zustand:** `useAuthStore`, `useBiStore`, `useFuzzyStore` — todos con CRUD completo
- **8 servicios API** ya listos (auth, systemStatus, fuzzyRules, weather, analytics, admin, bi, fuzzy)
- **Todos los tipos**, hooks, tokens de diseño, utilidades
- **Componente Icon nativo** con Ionicons mapeados

---

## 🏗️ Arquitectura Propuesta

### Patrón de Navegación
```
RootStack (NativeStack)
├── Login (sin auth)
└── MainTabs (BottomTab — 5 tabs)
    ├── Tab: Dashboard (Stack)
    │   └── DashboardScreen
    ├── Tab: Análisis (Stack)
    │   └── AnalyticsScreen
    ├── Tab: Consumo (Stack)
    │   ├── BiScreen (tabs internos: costos/consumo/producción/rentabilidad)
    │   └── BiFormScreen (formularios push)
    ├── Tab: Rutinas (Stack)
    │   ├── FuzzyListScreen
    │   └── FuzzyDetailScreen (tabs internos: info/variables/reglas/simular)
    └── Tab: Más (Stack)
        ├── MoreMenuScreen (lista de opciones)
        ├── ProfileScreen
        ├── AdminAccessScreen (solo admin)
        └── AdminSessionsScreen (solo admin)
```

### Principios de Diseño
1. **Atomic Design** — mantener la jerarquía atoms → molecules → organisms → screens
2. **Shared-first** — reutilizar stores, services, types, tokens del package `@hydroespinaca/shared`
3. **BottomSheet para CRUD** — usar modales bottom-sheet para crear/editar (patrón nativo mobile)
4. **Pull-to-refresh** — en todas las listas
5. **Skeleton loaders** — feedback visual consistente durante carga
6. **Responsive cards** — diseño mobile-first, no adaptar desktop a mobile
7. **Offline-awareness** — banner de desconexión + retry (ya existe parcialmente en Dashboard)
8. **Accesibilidad** — `accessibilityLabel`, `accessibilityRole`, `accessibilityHint` en todos los componentes interactivos

---

## 📋 Fases de Implementación

### Fase 0 — Fundación y Navegación
> **Prerequisito para todo lo demás. Refactoriza la base.**

| # | Tarea | Archivos | Detalle |
|---|-------|----------|---------|
| 0.1 | **Instalar dependencias** | `package.json` | `@react-navigation/bottom-tabs`, `@gorhom/bottom-sheet`, `react-native-reanimated`, `victory-native` (charts), `react-native-gesture-handler` (ya existe) |
| 0.2 | **Crear estructura de carpetas** | `src/` | Ver sección "Estructura de Carpetas" abajo |
| 0.3 | **Configurar navegación completa** | `navigation/` | `RootNavigator` (stack: Login + MainTabs), `MainTabNavigator` (5 tabs), stacks internos por tab |
| 0.4 | **Crear átomos faltantes** | `components/atoms/` | `Badge`, `Select`, `Switch`, `Divider`, `Avatar`, `FloatingActionButton`, `StatusIndicator`, `Checkbox`, `TextArea` |
| 0.5 | **Crear moléculas base** | `components/molecules/` | `Card`, `FormField` (mejorado), `EmptyState`, `SearchBar`, `StatCard`, `ListItem`, `Alert` |
| 0.6 | **Crear organismos base** | `components/organisms/` | `BottomSheetForm` (wrapper reutilizable), `ScreenLayout` (SafeArea + ScrollView + header), `SkeletonLoader`, `TabBar` (custom), `ConfirmationSheet` (SweetAlert2 equivalente nativo) |
| 0.7 | **Refactorizar DashboardScreen** | `screens/dashboard/` | Romper el monolito de 613 líneas en componentes modulares |

**Estimación:** 3-4 días

---

### Fase 1 — Dashboard Mejorado
> **Mejorar lo que ya existe: modularizar + polish.**

| # | Tarea | Componentes | Detalle |
|---|-------|-------------|---------|
| 1.1 | **Refactorizar DashboardScreen** | `screens/dashboard/DashboardScreen.tsx` | Extraer a componentes: `WeatherSection`, `VariablesGrid`, `ControllerSection`, `LastUpdateBanner` |
| 1.2 | **Mejorar VariableCard** | `components/dashboard/VariableCard.tsx` | Agregar animaciones de entrada, skeleton durante carga, tap para expandir detalle |
| 1.3 | **Mejorar WeatherCard** | `components/dashboard/WeatherCard.tsx` | Layout más compacto, iconos de clima mejorados |
| 1.4 | **Mejorar ControllerStatus** | `components/dashboard/ControllerStatus.tsx` | Refactorizar stats grid, queue list, routines table |
| 1.5 | **Pull-to-refresh** | `DashboardScreen` | `RefreshControl` nativo integrado al polling existente |
| 1.6 | **Banner de desconexión** | `components/molecules/DisconnectionBanner.tsx` | Reutilizar lógica existente con mejor UI |

**Estimación:** 1-2 días

---

### Fase 2 — Analytics (Gráficos)
> **Pantalla nueva. Equivalente a `/analytics` de web.**

| # | Tarea | Componentes | Detalle |
|---|-------|-------------|---------|
| 2.1 | **AnalyticsScreen** | `screens/analytics/AnalyticsScreen.tsx` | Tabs internos: "Ambiental" y "Actuadores". Usa `analyticsService` del shared |
| 2.2 | **Filtros** | `components/analytics/FiltersBar.tsx` | Selector de rango de fechas (DateTimePicker), selector de variable. Versión compacta mobile |
| 2.3 | **TimelineChart** | `components/analytics/TimelineChart.tsx` | Gráfico de línea temporal con `victory-native`. Zoom horizontal con pinch gesture |
| 2.4 | **BoxplotChart** | `components/analytics/BoxplotChart.tsx` | Chart de boxplot por variable |
| 2.5 | **ActuatorCharts** | `components/analytics/ActuatorTimelineChart.tsx`, `ActuatorDurationChart.tsx`, `ActuatorProportionChart.tsx` | Charts de actuadores: timeline, duración (bar), proporción (pie/donut) |
| 2.6 | **SummaryCards** | `components/analytics/SummaryCards.tsx` | Tarjetas con estadísticas resumidas (min, max, avg, std) |
| 2.7 | **Export** | `components/analytics/ExportButton.tsx` | Botón para compartir datos (expo-sharing + expo-file-system) |

**Estimación:** 3-4 días

---

### Fase 3 — Consumo y Costos (BI)
> **Pantalla nueva. Equivalente a `/consumo` de web.**

| # | Tarea | Componentes | Detalle |
|---|-------|-------------|---------|
| 3.1 | **BiScreen** | `screens/bi/BiScreen.tsx` | Tabs internos: Configuración / Consumo / Producción / Rentabilidad. Usa `useBiStore` |
| 3.2 | **CostConfigSection** | `components/bi/CostConfigSection.tsx` | Formulario de configuración de costos con `BottomSheetForm` para editar |
| 3.3 | **ConsumptionSection** | `components/bi/ConsumptionSection.tsx` | Lista de registros de consumo + `ConsumptionSummary` cards + FAB para agregar |
| 3.4 | **ConsumptionForm** | `components/bi/ConsumptionForm.tsx` | BottomSheet form: tipo (agua/electricidad/nutrientes), valor, unidad, fecha, notas |
| 3.5 | **ProductionSection** | `components/bi/ProductionSection.tsx` | Lista de cosechas + FAB para agregar |
| 3.6 | **ProductionForm** | `components/bi/ProductionForm.tsx` | BottomSheet form: tipo de cultivo, cantidad, peso, fecha, precio de venta |
| 3.7 | **ProfitabilitySection** | `components/bi/ProfitabilitySection.tsx` | Resumen de rentabilidad con cards de costos vs ingresos, margen, ROI |

**Estimación:** 3-4 días

---

### Fase 4 — Rutinas Fuzzy
> **Pantalla nueva. Equivalente a `/rutinas` + `/rutinas/[id]` de web.**

| # | Tarea | Componentes | Detalle |
|---|-------|-------------|---------|
| 4.1 | **FuzzyListScreen** | `screens/fuzzy/FuzzyListScreen.tsx` | FlatList de `FuzzySystemCard` + SearchBar + status filter chips + FAB crear + pull-to-refresh. Usa `useFuzzyStore` |
| 4.2 | **FuzzySystemCard** | `components/fuzzy/FuzzySystemCard.tsx` | Card con nombre, status badge, variable count, rule count. Swipeable (slide para activar/duplicar/eliminar) o long-press menú contextual |
| 4.3 | **SystemForm** | `components/fuzzy/SystemForm.tsx` | BottomSheet form: nombre, método de defuzzificación, operadores (AND/OR/agregación) |
| 4.4 | **FuzzyDetailScreen** | `screens/fuzzy/FuzzyDetailScreen.tsx` | Header con nombre + status + action buttons. Tabs internos: Info / Variables / Reglas / Simular |
| 4.5 | **InfoTab** | `components/fuzzy/InfoTab.tsx` | Resumen: cards de variables entrada/salida, preview de reglas (como en web) |
| 4.6 | **VariablesTab** | `components/fuzzy/VariablesTab.tsx` | Lista de variables. Cada variable expandible con sus términos + MembershipChart inline. Botones: agregar variable, agregar término, editar, eliminar |
| 4.7 | **VariableForm** | `components/fuzzy/VariableForm.tsx` | BottomSheet form: nombre, tipo (input/output), actuador, universo min/max, código referencia |
| 4.8 | **TermForm** | `components/fuzzy/TermForm.tsx` | BottomSheet form: etiqueta, tipo MF, parámetros dinámicos. Preview del chart en miniatura dentro del sheet |
| 4.9 | **MembershipChart** | `components/fuzzy/MembershipChart.tsx` | Chart con `victory-native` mostrando todas las funciones de membresía de una variable. Touch para highlight individual |
| 4.10 | **RulesTab** | `components/fuzzy/RulesTab.tsx` | Lista de reglas con texto en lenguaje natural. Cada regla como card expandible |
| 4.11 | **RuleForm** | `components/fuzzy/RuleForm.tsx` | BottomSheet multi-step: Step 1 (nombre/descripción), Step 2 (condiciones con pickers), Step 3 (consecuentes con term chips). Preview de la regla al final |
| 4.12 | **SimulationTab** | `components/fuzzy/SimulationTab.tsx` | Sliders por variable de entrada + botón simular + resultado con cards de output. Usa `simulateSystem` del store |
| 4.13 | **Import/Export** | Integrado en `FuzzyListScreen` | Import: `DocumentPicker` para JSON. Export: `expo-sharing` + `expo-file-system` |

**Estimación:** 5-6 días

---

### Fase 5 — Perfil y Menú "Más"
> **Pantallas de soporte.**

| # | Tarea | Componentes | Detalle |
|---|-------|-------------|---------|
| 5.1 | **MoreMenuScreen** | `screens/more/MoreMenuScreen.tsx` | Lista de opciones: Perfil, Admin (si admin), Cerrar sesión. Secciones agrupadas con `SectionList` |
| 5.2 | **ProfileScreen** | `screens/more/ProfileScreen.tsx` | Avatar + nombre + rol + email. Card read-only. Botón logout |
| 5.3 | **Tab "Más" con badge** | `navigation/MainTabNavigator.tsx` | El tab "Más" muestra badge si hay items admin disponibles |

**Estimación:** 1 día

---

### Fase 6 — Administración (Solo Admin)
> **Pantallas admin. Equivalente a `/admin/*` de web.**

| # | Tarea | Componentes | Detalle |
|---|-------|-------------|---------|
| 6.1 | **AdminAccessScreen** | `screens/admin/AdminAccessScreen.tsx` | Tabs: Usuarios / Roles. Usa `adminService` del shared |
| 6.2 | **UserList + UserForm** | `components/admin/UserList.tsx`, `UserForm.tsx` | FlatList de usuarios + BottomSheet form CRUD. Incluye selector de rol |
| 6.3 | **RoleList + RoleForm** | `components/admin/RoleList.tsx`, `RoleForm.tsx` | FlatList de roles + BottomSheet form CRUD. Incluye `PermissionTree` (tree checkboxes) |
| 6.4 | **AdminSessionsScreen** | `screens/admin/AdminSessionsScreen.tsx` | Lista de sesiones activas + pull-to-refresh + auto-refresh 30s + revocar sesión |
| 6.5 | **AdminGuard** | `components/organisms/AdminGuard.tsx` | HOC que verifica rol admin antes de renderizar. Redirect si no es admin |

**Estimación:** 2-3 días

---

### Fase 7 — Polish y UX
> **Mejoras transversales de calidad.**

| # | Tarea | Detalle |
|---|-------|---------|
| 7.1 | **Transiciones de navegación** | Animaciones de entrada/salida consistentes entre screens |
| 7.2 | **Haptic feedback** | `expo-haptics` en acciones destructivas y confirmaciones |
| 7.3 | **Toast notifications** | Sistema de toast nativo para feedback de operaciones CRUD |
| 7.4 | **Skeleton loaders** | Plantillas skeleton en todas las pantallas de carga |
| 7.5 | **Error boundaries** | Error boundary global + pantallas de error por feature |
| 7.6 | **Deep linking** | Configurar deep links para `/rutinas/:id`, etc. |
| 7.7 | **App icon + splash** | Actualizar assets con nuevo diseño |
| 7.8 | **Accessibility audit** | Revisar labels, roles, hints en todos los componentes |

**Estimación:** 2-3 días

---

## 📁 Estructura de Carpetas Propuesta

```
apps/mobile/src/
├── components/
│   ├── atoms/              # Componentes atómicos puros
│   │   ├── Avatar.tsx
│   │   ├── Badge.tsx
│   │   ├── Button.tsx       (ya existe)
│   │   ├── Checkbox.tsx
│   │   ├── Divider.tsx
│   │   ├── FloatingActionButton.tsx
│   │   ├── Heading.tsx      (ya existe)
│   │   ├── Icon.tsx         (ya existe)
│   │   ├── IconButton.tsx   (ya existe)
│   │   ├── index.ts         (ya existe)
│   │   ├── Input.tsx        (ya existe)
│   │   ├── Label.tsx        (ya existe)
│   │   ├── Pressable.tsx    (ya existe)
│   │   ├── Select.tsx
│   │   ├── Spinner.tsx      (ya existe)
│   │   ├── StatusIndicator.tsx
│   │   ├── Switch.tsx
│   │   ├── Text.tsx         (ya existe)
│   │   └── TextArea.tsx
│   ├── molecules/
│   │   ├── Alert.tsx
│   │   ├── Card.tsx
│   │   ├── ConfirmationDialog.tsx
│   │   ├── DisconnectionBanner.tsx
│   │   ├── EmptyState.tsx
│   │   ├── FormField.tsx
│   │   ├── ListItem.tsx
│   │   ├── SearchBar.tsx
│   │   ├── StatCard.tsx
│   │   └── index.ts
│   ├── organisms/
│   │   ├── AdminGuard.tsx
│   │   ├── BottomSheetForm.tsx
│   │   ├── ConfirmationSheet.tsx
│   │   ├── LoginForm.tsx    (ya existe)
│   │   ├── ScreenLayout.tsx
│   │   ├── SkeletonLoader.tsx
│   │   └── index.ts
│   ├── dashboard/           (ya existe, refactorizar)
│   │   ├── ControllerStatus.tsx
│   │   ├── FuzzyRulesInfo.tsx
│   │   ├── LastUpdateBanner.tsx
│   │   ├── VariableCard.tsx
│   │   ├── VariablesGrid.tsx
│   │   ├── WeatherCard.tsx
│   │   ├── WeatherSection.tsx
│   │   └── index.ts
│   ├── analytics/
│   │   ├── ActuatorCharts.tsx
│   │   ├── BoxplotChart.tsx
│   │   ├── ExportButton.tsx
│   │   ├── FiltersBar.tsx
│   │   ├── SummaryCards.tsx
│   │   ├── TimelineChart.tsx
│   │   └── index.ts
│   ├── bi/
│   │   ├── ConsumptionForm.tsx
│   │   ├── ConsumptionSection.tsx
│   │   ├── CostConfigSection.tsx
│   │   ├── ProductionForm.tsx
│   │   ├── ProductionSection.tsx
│   │   ├── ProfitabilitySection.tsx
│   │   └── index.ts
│   ├── fuzzy/
│   │   ├── FuzzySystemCard.tsx
│   │   ├── InfoTab.tsx
│   │   ├── MembershipChart.tsx
│   │   ├── RuleForm.tsx
│   │   ├── RulesTab.tsx
│   │   ├── SimulationTab.tsx
│   │   ├── SystemForm.tsx
│   │   ├── TermForm.tsx
│   │   ├── VariableForm.tsx
│   │   ├── VariablesTab.tsx
│   │   └── index.ts
│   ├── admin/
│   │   ├── RoleForm.tsx
│   │   ├── RoleList.tsx
│   │   ├── UserForm.tsx
│   │   ├── UserList.tsx
│   │   └── index.ts
│   ├── MobileAuthInitializer.tsx  (ya existe)
│   └── index.ts
├── context/
│   └── AuthProvider.tsx     (ya existe)
├── navigation/
│   ├── RootNavigator.tsx
│   ├── MainTabNavigator.tsx
│   ├── stacks/
│   │   ├── DashboardStack.tsx
│   │   ├── AnalyticsStack.tsx
│   │   ├── BiStack.tsx
│   │   ├── FuzzyStack.tsx
│   │   └── MoreStack.tsx
│   ├── types.ts             # RootStackParamList, TabParamList, etc.
│   └── index.ts
├── screens/
│   ├── LoginScreen.tsx      (ya existe)
│   ├── dashboard/
│   │   └── DashboardScreen.tsx  (refactorizar)
│   ├── analytics/
│   │   └── AnalyticsScreen.tsx
│   ├── bi/
│   │   └── BiScreen.tsx
│   ├── fuzzy/
│   │   ├── FuzzyListScreen.tsx
│   │   └── FuzzyDetailScreen.tsx
│   ├── more/
│   │   ├── MoreMenuScreen.tsx
│   │   └── ProfileScreen.tsx
│   ├── admin/
│   │   ├── AdminAccessScreen.tsx
│   │   └── AdminSessionsScreen.tsx
│   └── index.ts
└── utils/                   # Utilidades mobile-only
    ├── haptics.ts
    └── toast.ts
```

---

## 📦 Dependencias a Agregar

```json
{
  "@react-navigation/bottom-tabs": "^7.x",
  "@gorhom/bottom-sheet": "^5.x",
  "react-native-reanimated": "~3.x",
  "victory-native": "^41.x",
  "@react-native-community/datetimepicker": "^8.x",
  "expo-haptics": "~14.x",
  "expo-document-picker": "~13.x",
  "expo-sharing": "~13.x",
  "expo-file-system": "~18.x"
}
```

> Nota: `react-native-gesture-handler`, `react-native-svg`, `expo-file-system` ya están instalados.

---

## 🗓️ Resumen de Estimación

| Fase | Descripción | Días estimados |
|------|-------------|----------------|
| **0** | Fundación y Navegación | 3-4 |
| **1** | Dashboard Mejorado | 1-2 |
| **2** | Analytics (Gráficos) | 3-4 |
| **3** | Consumo y Costos (BI) | 3-4 |
| **4** | Rutinas Fuzzy (CRUD completo) | 5-6 |
| **5** | Perfil y Menú "Más" | 1 |
| **6** | Administración | 2-3 |
| **7** | Polish y UX | 2-3 |
| | **TOTAL** | **~20-27 días** |

---

## 🎯 Prioridad de Implementación

```
Fase 0 (Fundación) ──► Fase 1 (Dashboard) ──► Fase 4 (Fuzzy) ──► Fase 2 (Analytics)
                                                                          │
                                                                          ▼
                                                              Fase 3 (BI) ──► Fase 5 (Perfil)
                                                                                      │
                                                                                      ▼
                                                                            Fase 6 (Admin) ──► Fase 7 (Polish)
```

**Justificación del orden:**
1. **Fase 0** es prerequisito obligatorio (navegación + atoms)
2. **Fase 1** mejora lo que ya existe — quick win visible
3. **Fase 4** (Fuzzy) antes de Analytics porque el CRUD fuzzy es la funcionalidad core diferenciadora y el backend ya está 100% listo
4. **Fase 2** (Analytics) es alto valor visual
5. **Fase 3** (BI) aprovecha el store que ya existe
6. **Fases 5-6** son complementarias
7. **Fase 7** se aplica al final sobre todo el conjunto

---

## 📝 Notas Técnicas

### Patrones a seguir
- **BottomSheet para formularios:** Usar `@gorhom/bottom-sheet` para todos los formularios CRUD. Es el patrón nativo esperado en móvil (vs Modal en web).
- **Zustand stores del shared:** Reutilizar `useFuzzyStore`, `useBiStore`, `useAuthStore` directamente. NO crear stores duplicados en mobile.
- **Diseño tokens del shared:** Usar `colors`, `spacing`, `typography`, `borderRadius`, `shadows` de `@hydroespinaca/shared` para consistencia visual.
- **`ScreenLayout` wrapper:** Todos los screens deben usar un layout consistente con SafeArea + header + content area.
- **`ConfirmationSheet`:** Reemplaza SweetAlert2 (que es web-only) con un bottom sheet nativo para confirmaciones de delete y acciones destructivas.
- **Charts:** `victory-native` v41+ usa `react-native-reanimated` + `@shopify/react-native-skia` para rendering nativo. Considerar alternativa `react-native-gifted-charts` si el setup de Skia es problemático en Expo.

### Convenciones
- Archivos en **PascalCase** para componentes, **camelCase** para utils
- Un `index.ts` barrel export por carpeta de componentes
- Props interfaces nombradas `{ComponentName}Props`
- Usar `StyleSheet.create()` (no objetos inline) para estilos
- Colores siempre desde `colors` del shared, nunca hardcoded
- Strings en español (consistente con web)

### Testing (para después)
- Unit tests de componentes con `@testing-library/react-native`
- Snapshot tests de screens
- Integration tests de stores (ya cubiertos en shared)
