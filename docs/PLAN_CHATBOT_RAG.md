# Plan: Asistente Inteligente (Chatbot) con RAG

Fecha: 2026-02-22 (Revisión & actualización post-worktree)
Autor: Agente de desarrollo
Dependencias previas: Funcionalidad CRUD de Fuzzy terminada, persistencia en MongoDB configurada.

> **Nota de revisión**: Este documento fue re-generado completamente luego de perder el historial por el cambio de worktree a repo normal. El estado real del código fue analizado directamente desde los archivos fuente para reconstruir el estado de cada tarea con precisión.

---

## 0) Contexto y Explicación de Conceptos

El sistema necesita un Chatbot basado en **RAG (Retrieval-Augmented Generation)** para asistir en la creación de rutinas de lógica difusa y responder dudas sobre los datos recolectados por sensores. Se usa **Google Gemini 2.0 Flash** vía API como motor de IA, el **SDK nativo `google-genai`** (Python), y **MongoDB Atlas Vector Search** para recuperación semántica sin base de datos adicional.

### Flujo de Datos End-to-End
```
Frontend (Web/Mobile)
  ↓  POST /chat/stream/{sessionId}  (SSE)
BFF .NET  ──YARP proxy──►  chatbot-service Python :8010
  chatbot-service:
    1. Vectoriza pregunta → text-embedding-004 (Google)
    2. $vectorSearch en Atlas → chunks relevantes
    3. Hidrata historial desde chat_sessions (MongoDB)
    4. Envía Prompt (pregunta + contexto + historial) a Gemini Flash
    5. Retorna stream SSE token a token
  ↓  SSE stream (text/event-stream)
Frontend: renderiza token a token en tiempo real
```

### Arquitectura de Componentes Frontend
El frontend sigue **arquitectura atómica** (`atoms → molecules → organisms → templates/pages`):
- **Atoms**: componentes de UI puros sin lógica de dominio (burbuja, cursor, botón icono).
- **Molecules**: combinación de atoms con lógica local simple (input con botón, item de sesión, mensaje con markdown).
- **Organisms**: secciones completas de UI que consumen el store (lista de sesiones, lista de mensajes, toggle flotante).
- **Pages/Screens**: páginas/pantallas completas que ensamblan organisms (web: integrado en layout via Drawer; mobile: ChatScreen dedicada).

### Convención de estilos
- **Web (Next.js)**: **Tailwind CSS únicamente**. Los archivos `*.module.css` existentes en `/chat` deben ser eliminados y sus estilos migrados a clases Tailwind directamente en JSX.
- **Mobile (React Native/Expo)**: `StyleSheet.create` con tokens del `@hydroespinaca/shared` (`colors`, `spacing`, `borderRadius`, `shadows`, `semanticColors`).

---

## 1) Objetivos y Alcance

**DONE cuando:**
1. ✅ Existe un `chatbot-service` en Python desacoplado que procesa requests vía SDK nativo `google-genai`.
2. ✅ Las colecciones críticas de MongoDB tienen índice Atlas Vector Search y rutinas en Python actualizando embeddings.
3. ✅ El BFF .NET tiene proxy YARP configurado para `/chat/**`.
4. ✅ El **shared package** (`@hydroespinaca/shared`) exporta tipos, `chatService`, `useChatStore` y `useChatSSE` para mobile.
5. ✅ El **frontend web** tiene el `ChatDrawer` funcional, sin CSS Modules, conectado al store.
6. ✅ El **frontend mobile** tiene una `ChatScreen` dedicada con SSE hook nativo.

---

## 2) Configuración de Arquitectura e Infraestructura ✅ COMPLETADO

- **Servicio Python** `chatbot-service`: Docker, puerto 8010.
- **Base de Datos**: Atlas Vector Search sobre clúster MongoDB actual.
- **BFF .NET**: Configuración YARP proxy nativo para `/api/chat/**`.

---

## 3) Fases de Desarrollo

### Fase 1 — Infraestructura Python (`chatbot-service`) ✅ COMPLETADO

Andamiaje del microservicio Python con FastAPI. Integración Google GenAI SDK (`google-genai`) y `motor` (AsyncIO MongoDB driver).

**Entregables completados:**
- `chatbot-service/` con estructura Clean Architecture (api, application, domain, infrastructure).
- `main.py` con app FastAPI y configuración CORS.
- `.env` con `GEMINI_API_KEY`, `MONGODB_URI`, `DB_NAME`.
- `docker-compose.yml` actualizado con servicio `chatbot-service`.
- Colección MongoDB `chat_sessions` con esquema `{ session_id, user_id, title, history: [{role, parts}], created_at, updated_at }`.
- Colección `knowledge_chunks` con campo `embedding: [Float]` y índice Atlas Vector Search.

---

### Fase 2 — Vectorización Atlas & Script de Ingestión ✅ COMPLETADO

Script Python que vectoriza datos existentes en MongoDB y crea/actualiza embeddings.

**Entregables completados:**
- `scripts/ingest_knowledge.py`: vectoriza reglas fuzzy, parámetros de sensores, notas agronómicas usando `text-embedding-004`.
- Índice `vector_index` en colección `knowledge_chunks`: dimensión 768, cosine similarity.
- Rutina de actualización incremental (cron cada 6h en producción, manual en dev).

---

### Fase 3 — Core API Chat (SSE Endpoint) ✅ COMPLETADO

Endpoints REST + SSE del `chatbot-service` y proxy en BFF.

**Entregables completados:**

#### `chatbot-service` (Python/FastAPI)
- `POST /sessions` → Crea `ChatSession` en MongoDB.
- `GET /sessions?userId=` → Lista sesiones del usuario.
- `GET /messages/{sessionId}` → Historial de mensajes.
- `DELETE /sessions/{sessionId}` → Archiva sesión.
- `POST /stream/{sessionId}` → **SSE endpoint principal**:
  1. Vectoriza pregunta con `text-embedding-004`.
  2. Ejecuta `$vectorSearch` contra `knowledge_chunks`.
  3. Lee historial desde MongoDB, hidrata objeto `client.chats.create(history=[...])`.
  4. Envía Prompt enriched a Gemini Flash y hace streaming de tokens como SSE events:
     - `event: token\ndata: {"text": "..."}\n\n`
     - `event: done\ndata: {"tokensUsed": N, "sessionTitle": "..."}\n\n`
  5. Persiste el mensaje completo en MongoDB al finalizar.

#### BFF .NET (`.NET 9 / Clean Architecture`)
- Proyecto `HydroEspinaca.BFF.Api` con YARP configurado.
- Route: `/chat/**` → proxy transparente a `http://chatbot-service:8010/**`.
- Middleware de autenticación antes del proxy (valida JWT/sesión).
- Header `X-User-Id` inyectado desde JWT claims al upstream Python.

---

### Fase 4 — Shared Package (`@hydroespinaca/shared`) 🔄 PARCIALMENTE COMPLETADO

El paquete compartido (`packages/shared`) ya tiene la mayoría de la infraestructura de chat implementada. Esta fase documenta lo existente y lo pendiente.

#### 4.1 — Tipos TypeScript (`src/types/chat.ts`) ✅ COMPLETADO

```typescript
// Ya existentes y completos:
export interface ChatSession { id, userId, title, isArchived, createdAt, updatedAt }
export interface ChatMessage { role: 'user'|'model'|'system', content, timestamp, tokensUsed? }
export interface ChatContextFilters { fuzzySystemId?, timeRangeHours? }
export interface SendMessageRequest { message, contextFilters? }
export interface CreateSessionRequest { titleHint? }
export interface CreateSessionResponse { sessionId }
export interface StreamTokenEvent { text }
export interface StreamDoneEvent { tokensUsed, sessionTitle }
```

#### 4.2 — Servicio API (`src/api/chatService.ts`) ✅ COMPLETADO

```typescript
// Ya existente y completo:
class ChatApiService extends BaseApiService {
  createSession(request?)      → POST /chat/sessions
  getSessions()                → GET  /chat/sessions
  getMessages(sessionId)       → GET  /chat/messages/{sessionId}
  deleteSession(sessionId)     → DELETE /chat/sessions/{sessionId}
  streamMessage(sessionId, r)  → POST /chat/stream/{sessionId} (raw Response para SSE)
}
export const chatService = new ChatApiService();
```

**Nota**: `streamMessage` maneja auth diferenciada: web usa cookies (`credentials: 'include'`), mobile adjunta `X-Session-Id` y `X-CSRF-Token` desde `SessionStorage`.

#### 4.3 — Store Zustand (`src/store/chatStore.ts`) ✅ COMPLETADO

```typescript
// Ya existente y completo:
interface ChatState {
  sessions: ChatSession[];          sessionsLoading; sessionsError;
  activeSessionId: string | null;
  messages: ChatMessage[];          messagesLoading; messagesError;
  streamingText: string;            isStreaming: boolean;
  createSessionLoading;             createSessionError;
  deleteSessionLoading;             deleteSessionError;
}
interface ChatActions {
  fetchSessions(); createSession(); deleteSession(id); setActiveSession(id);
  fetchMessages(id);
  startStream(); appendStreamToken(token); finalizeStream(event);
  clearErrors(); resetChatStore();
}
export const useChatStore = create<ChatState & ChatActions>()(...)
```

#### 4.4 — Hook SSE Mobile (`src/hooks/useChatSSE.ts`) ⬜ PENDIENTE

Hook dedicado para consumir el stream SSE en React Native (sin ReadableStream nativo). Usa el paquete `react-native-sse` o la API `EventSource` del polyfill de Expo.

```typescript
// Archivo: packages/shared/src/hooks/useChatSSE.ts
// (Para mobile únicamente; web usa ReadableStream en useChatStream.ts)

export function useChatSSE() {
  const activeSessionId = useChatStore(s => s.activeSessionId);
  const startStream     = useChatStore(s => s.startStream);
  const appendStreamToken = useChatStore(s => s.appendStreamToken);
  const finalizeStream  = useChatStore(s => s.finalizeStream);
  const isStreaming     = useChatStore(s => s.isStreaming);

  const sendMessage = useCallback(async (userMessage: string) => {
    if (!activeSessionId || isStreaming) return;
    startStream();

    // Obtiene URL + headers con auth mobile
    const { url, headers } = await buildStreamRequest(activeSessionId, { message: userMessage });

    // EventSource nativo via react-native-sse o polyfill
    const es = new EventSource(url, {
      method: 'POST',
      body: JSON.stringify({ message: userMessage }),
      headers,
    });

    es.addEventListener('token', (e) => appendStreamToken(JSON.parse(e.data).text));
    es.addEventListener('done',  (e) => {
      finalizeStream(JSON.parse(e.data));
      es.close();
    });
    es.addEventListener('error', () => {
      finalizeStream({ tokensUsed: 0, sessionTitle: '' });
      es.close();
    });
  }, [activeSessionId, isStreaming, startStream, appendStreamToken, finalizeStream]);

  return { sendMessage, isStreaming };
}
```

**Dependencia**: agregar `react-native-sse` o `expo-modules-core` EventSource polyfill a `apps/mobile/package.json`.

#### 4.5 — Exports del paquete ⬜ PENDIENTE

Agregar `useChatSSE` a los índices del shared package:

```typescript
// src/hooks/index.ts — agregar:
export { useChatSSE } from './useChatSSE';

// src/index.native.ts — ya debe exportar useChatSSE
// src/index.web.ts   — NO exportar useChatSSE (solo web usa ReadableStream)
// src/index.ts       — exportar ambos condicionalmente
```

---

### Fase 5 — Frontend Web (Next.js) 🔄 PARCIALMENTE COMPLETADO

Los archivos de la feature `chat` en `apps/web/src/components/chat/` existen pero usan **CSS Modules** en lugar de Tailwind. Esta fase documenta el inventario completo y las correcciones a hacer.

#### Estado actual del directorio `src/components/chat/`

```
chat/
├── ChatDrawer.tsx           ← usa ChatDrawer.module.css → MIGRAR a Tailwind
├── ChatDrawer.module.css    ← ELIMINAR
├── ChatProvider.tsx         ✅ Ya correcto (sin CSS modules)
├── ChatProvider.tsx         ✅ Ya correcto
├── index.ts                 ✅ Barrel exports completo
├── hooks/
│   ├── useChatStream.ts     ✅ Completo (SSE web con ReadableStream)
│   └── useChatSessions.ts   ✅ Completo
├── atoms/
│   ├── ChatBubbleWeb.tsx    ← usa ChatBubbleWeb.module.css → MIGRAR
│   ├── ChatBubbleWeb.module.css → ELIMINAR
│   ├── SessionDateLabel.tsx ← usa SessionDateLabel.module.css → MIGRAR
│   ├── SessionDateLabel.module.css → ELIMINAR
│   ├── TypingCursor.tsx     ← usa TypingCursor.module.css → MIGRAR
│   └── TypingCursor.module.css → ELIMINAR
├── molecules/
│   ├── ChatInput.tsx        ← usa ChatInput.module.css → MIGRAR
│   ├── ChatInput.module.css → ELIMINAR
│   ├── ChatSessionItem.tsx  ← usa ChatSessionItem.module.css → MIGRAR
│   ├── ChatSessionItem.module.css → ELIMINAR
│   ├── MarkdownMessage.tsx  ← usa MarkdownMessage.module.css → MIGRAR
│   ├── MarkdownMessage.module.css → ELIMINAR
│   ├── StreamingMessage.tsx ← usa StreamingMessage.module.css → MIGRAR
│   └── StreamingMessage.module.css → ELIMINAR
└── organisms/
    ├── ChatSessionList.tsx  ← usa ChatSessionList.module.css → MIGRAR
    ├── ChatSessionList.module.css → ELIMINAR
    ├── ChatToggleButton.tsx ← usa ChatToggleButton.module.css → MIGRAR
    ├── ChatToggleButton.module.css → ELIMINAR
    ├── MessageList.tsx      ← usa MessageList.module.css → MIGRAR
    └── MessageList.module.css → ELIMINAR
```

#### 5.1 — Atoms Web ⬜ PENDIENTE (migración CSS → Tailwind)

##### `ChatBubbleWeb.tsx`
**Responsabilidad**: Contenedor visual puro para un mensaje. Alinea a la derecha (user) o izquierda (model). Sin lógica de dominio.

```
Props:
  role: 'user' | 'model' | 'system'
  children: ReactNode
  isStreaming?: boolean
  className?: string

Clases Tailwind clave:
  wrapper: flex w-full py-1
  user  → justify-end
  model → justify-start
  system → justify-center

  bubble base: max-w-[78%] px-3.5 py-2.5 rounded-2xl text-sm leading-relaxed break-words
  user .bubble:   bg-gradient-to-br from-blue-600 to-blue-700 text-white rounded-br-sm
  model .bubble:  bg-white/[0.07] border border-white/10 text-slate-200 rounded-bl-sm
  system .bubble: italic text-xs text-slate-400 bg-transparent text-center max-w-full
  streaming:      border-l-2 border-blue-400 (solo en model)
```

##### `SessionDateLabel.tsx`
**Responsabilidad**: Etiqueta de separador de fecha entre grupos de sesiones.

```
Props:
  date: string  (ISO 8601)

Clases Tailwind:
  container: px-3 py-1
  label: text-xs font-semibold text-slate-500 uppercase tracking-wide
```

##### `TypingCursor.tsx`
**Responsabilidad**: Cursor parpadeante que indica que el bot está generando respuesta.

```
Props: ninguno

Implementación:
  <span className="inline-block w-2 h-4 bg-blue-400 rounded-sm ml-0.5 animate-pulse" />
```

#### 5.2 — Molecules Web ⬜ PENDIENTE (migración CSS → Tailwind)

##### `ChatInput.tsx`
**Responsabilidad**: Textarea auto-expandible + botón enviar. Enter envía, Shift+Enter nueva línea.

```
Props:
  onSend: (message: string) => void

Clases Tailwind clave:
  wrapper: flex items-end gap-2 p-3 border-t border-white/10 bg-slate-900/80

  textarea: flex-1 resize-none bg-transparent text-slate-100 text-sm
            placeholder-slate-500 focus:outline-none
            min-h-[40px] max-h-[120px] py-2 px-0 leading-snug

  sendBtn:  flex-shrink-0 p-2 rounded-xl
            bg-blue-600 hover:bg-blue-500 disabled:opacity-40 disabled:cursor-not-allowed
            transition-colors

  sendIcon: h-5 w-5 text-white
```

##### `ChatSessionItem.tsx`
**Responsabilidad**: Fila de una sesión en la sidebar. Muestra título truncado + botón eliminar en hover.

```
Props:
  session: ChatSession
  isActive: boolean
  onSelect: (id: string) => void
  onDelete: (id: string) => void

Clases Tailwind clave:
  row: group relative flex items-center gap-2 px-3 py-2 rounded-lg cursor-pointer
       transition-colors
       isActive → bg-blue-600/20 text-blue-400
       !isActive → hover:bg-white/[0.05] text-slate-300

  title: flex-1 truncate text-sm

  deleteBtn: hidden group-hover:flex p-1 rounded hover:bg-red-500/20
             text-slate-500 hover:text-red-400 transition-colors
  deleteIcon: h-3.5 w-3.5
```

##### `MarkdownMessage.tsx`
**Responsabilidad**: Renderiza un mensaje completado con soporte Markdown completo (GFM + código con syntax highlight).

```
Props:
  message: ChatMessage

Dependencias: react-markdown, remark-gfm, react-syntax-highlighter

Clases Tailwind clave (en la prop className de ReactMarkdown):
  prose prose-invert prose-sm max-w-none
  prose-code:before:content-none prose-code:after:content-none
  prose-pre:bg-transparent prose-pre:p-0

  inlineCode: font-mono text-xs bg-white/10 px-1.5 py-0.5 rounded text-slate-300

Compone: ChatBubbleWeb
```

##### `StreamingMessage.tsx`
**Responsabilidad**: Muestra el texto que llega token a token durante el streaming activo. Solo visible mientras `isStreaming` es true.

```
Props: ninguno (lee del store)
  streamingText ← useChatStore
  isStreaming   ← useChatStore

Clases Tailwind clave:
  Compone: ChatBubbleWeb role="model" isStreaming
  Dentro: <span>{streamingText}</span><TypingCursor />

  Si !isStreaming && !streamingText → retorna null
```

#### 5.3 — Organisms Web ⬜ PENDIENTE (migración CSS → Tailwind)

##### `ChatSessionList.tsx`
**Responsabilidad**: Sidebar izquierda del Drawer. Agrupa sesiones por fecha (Hoy, Ayer, Esta semana, Este mes, Año).

```
Props: ninguno (consume useChatStore)

Estado desde store:
  sessions, activeSessionId, sessionsLoading,
  createSession, setActiveSession, deleteSession, createSessionLoading

Clases Tailwind clave:
  panel: w-[220px] flex flex-col border-r border-white/[0.08]
         bg-slate-900/60 overflow-hidden flex-shrink-0

  header: flex items-center justify-between px-3 py-3
          border-b border-white/[0.08]

  headerTitle: text-xs font-semibold uppercase tracking-widest text-slate-500

  newBtn: p-1.5 rounded-lg hover:bg-white/10 text-slate-300 hover:text-white
          transition-colors disabled:opacity-40

  newIcon: h-4 w-4

  list: flex-1 overflow-y-auto py-1 space-y-0.5 px-1
         scrollbar-thin scrollbar-thumb-slate-700 scrollbar-track-transparent

  loadingText / emptyText: text-xs text-slate-500 px-3 py-4
```

##### `ChatToggleButton.tsx`
**Responsabilidad**: Botón flotante (FAB) fijo en esquina inferior derecha. Abre/cierra el ChatDrawer.

```
Props:
  isOpen: boolean
  onToggle: () => void

Clases Tailwind clave:
  btn: fixed bottom-6 right-6 z-[997]
       w-14 h-14 rounded-2xl
       bg-gradient-to-br from-blue-600 to-indigo-700
       shadow-lg shadow-blue-500/30
       hover:shadow-blue-500/50 hover:scale-105
       transition-all duration-200
       flex items-center justify-center
       text-white

  icon: h-6 w-6
  
  Cuando isOpen: icono XMarkIcon; cuando !isOpen: icono ChatBubbleBottomCenterTextIcon
```

##### `MessageList.tsx`
**Responsabilidad**: Área de scroll con todos los mensajes de la sesión activa.

```
Props: ninguno (consume useChatStore)

Estado desde store:
  messages, isStreaming, messagesLoading, activeSessionId

Clases Tailwind clave:
  list: flex-1 overflow-y-auto px-4 py-3 space-y-1
        scrollbar-thin scrollbar-thumb-slate-700 scrollbar-track-transparent

  empty: flex flex-col items-center justify-center h-full gap-3 p-6 text-center
  emptyIcon: text-4xl
  emptyTitle: text-sm font-semibold text-slate-300
  emptySubtitle: text-xs text-slate-500 max-w-xs

  loading: flex items-center justify-center h-full
  spinner: h-8 w-8 rounded-full border-2 border-slate-600 border-t-blue-500 animate-spin
```

#### 5.4 — Feature Assembly Web ⬜ PENDIENTE (migración CSS → Tailwind)

##### `ChatDrawer.tsx`
**Responsabilidad**: Panel principal del chat. Slide-in desde la derecha. Compone `ChatSessionList` + `MessageList` + `ChatInput`.

```
Props:
  isOpen: boolean
  onClose: () => void

Clases Tailwind clave:
  backdrop: fixed inset-0 bg-black/45 z-[998] backdrop-blur-sm animate-in fade-in

  drawer: fixed inset-y-0 right-0 w-[min(680px,95vw)] z-[999]
          flex flex-row bg-slate-950 border-l border-white/[0.08]
          shadow-[-8px_0_40px_rgba(0,0,0,0.5)]
          transition-transform duration-300 ease-[cubic-bezier(0.4,0,0.2,1)]
          translate-x-full   [data-open] → translate-x-0

  chatArea: flex-1 flex flex-col overflow-hidden min-w-0
```

**Nota**: Para la animación de slide puede usarse la clase condicional con `cn()` (clsx) o el patrón `data-state`:
```tsx
<div className={cn('...translate-x-full transition-transform ...', isOpen && 'translate-x-0')} />
```

##### `ChatProvider.tsx` ✅ Sin cambios necesarios

Componente cliente que monta globalmente `ChatToggleButton` + `ChatDrawer` en el root layout. Solo se renderiza si el usuario está autenticado. Ya está correcto, sin CSS Modules.

##### Integración en `layout.tsx` ⬜ PENDIENTE

El `ChatProvider` debe agregarse al `RootLayout` de Next.js:

```tsx
// apps/web/src/app/layout.tsx
import { ChatProvider } from '@/components/chat';

export default function RootLayout({ children }) {
  return (
    <html lang="es">
      <body className={...}>
        <Providers>
          <StoreInitializer />
          {children}
          <ChatProvider />   {/* ← AGREGAR */}
        </Providers>
      </body>
    </html>
  );
}
```

---

### Fase 6 — Frontend Mobile (React Native / Expo) ⬜ PENDIENTE

El mobile **no tiene ningún componente de chat creado**. Esta fase define todo desde cero siguiendo la arquitectura atómica del resto de la app (StyleSheet + tokens del shared package).

#### 6.1 — Atoms Mobile

##### `ChatBubbleMobile.tsx`
**Ruta**: `apps/mobile/src/components/atoms/ChatBubbleMobile.tsx`
**Responsabilidad**: Vista contenedora de un mensaje. Equivalente móvil de `ChatBubbleWeb`. Sin lógica.

```typescript
interface ChatBubbleMobileProps {
  role: 'user' | 'model' | 'system';
  children: React.ReactNode;
  isStreaming?: boolean;
  style?: ViewStyle;
}

// StyleSheet clave:
wrapper:  { flexDirection: 'row', paddingVertical: 4 }
user:     { justifyContent: 'flex-end' }
model:    { justifyContent: 'flex-start' }
system:   { justifyContent: 'center' }

bubble: {
  maxWidth: '80%',
  paddingHorizontal: 14,
  paddingVertical: 10,
  borderRadius: 18,
}
userBubble: {
  backgroundColor: colors.hidro[600],
  borderBottomRightRadius: 4,
}
modelBubble: {
  backgroundColor: colors.gray[100],
  borderBottomLeftRadius: 4,
  borderWidth: 1,
  borderColor: colors.gray[200],
}
systemBubble: {
  backgroundColor: 'transparent',
  maxWidth: '100%',
}

// Streaming: border-left pulsante via Animated
streamingBorder: {
  borderLeftWidth: 3,
  borderLeftColor: colors.hidro[400],
}
```

##### `TypingIndicator.tsx`
**Ruta**: `apps/mobile/src/components/atoms/TypingIndicator.tsx`
**Responsabilidad**: Tres puntos animados que indican que el bot está procesando (visible mientras `isStreaming` y `streamingText` está vacío).

```typescript
// 3 puntos con Animated.loop + secuencia de opacidad escalonada (delay 0, 200, 400ms)
// Uso: <TypingIndicator visible={isStreaming && !streamingText} />
interface TypingIndicatorProps { visible: boolean }
```

##### `ChatDateDivider.tsx`
**Ruta**: `apps/mobile/src/components/atoms/ChatDateDivider.tsx`
**Responsabilidad**: Separador de fecha en medio de la lista de mensajes.

```typescript
interface ChatDateDividerProps { date: string }  // ISO 8601
// Renderiza: View con líneas + texto de fecha formateado centrado
// Texto: "Hoy", "Ayer", o formato corto de fecha
```

#### 6.2 — Molecules Mobile

##### `ChatMessageItem.tsx`
**Ruta**: `apps/mobile/src/components/molecules/ChatMessageItem.tsx`
**Responsabilidad**: Renderiza un `ChatMessage` completo usando `ChatBubbleMobile`. Para mensajes `model` formatea el Markdown con `react-native-markdown-display`.

```typescript
import MarkdownDisplay from 'react-native-markdown-display';
import { ChatBubbleMobile } from '../atoms/ChatBubbleMobile';

interface ChatMessageItemProps { message: ChatMessage }

// Para role === 'user': Text normal en ChatBubbleMobile
// Para role === 'model': MarkdownDisplay con markdownStyles personalizados
// Para role === 'system': texto centrado en italica

// markdownStyles para MarkdownDisplay:
{
  body: { color: colors.gray[800], fontSize: 14, lineHeight: 21 },
  code_inline: { backgroundColor: colors.gray[100], fontFamily: 'monospace', fontSize: 12 },
  fence: { backgroundColor: colors.gray[900], borderRadius: 8, padding: 12 },
  code_block: { color: colors.gray[100], fontFamily: 'monospace', fontSize: 12 },
  link: { color: colors.hidro[600] },
}
```

**Dependencia**: agregar `react-native-markdown-display` a `apps/mobile/package.json`.

##### `ChatInputMobile.tsx`
**Ruta**: `apps/mobile/src/components/molecules/ChatInputMobile.tsx`
**Responsabilidad**: Área de entrada de texto adaptada a móvil. TextInput multilinea con expansión controlada + botón enviar + manejo correcto de teclado.

```typescript
import { KeyboardAvoidingView, TextInput, TouchableOpacity, Platform } from 'react-native';

interface ChatInputMobileProps {
  onSend: (message: string) => void;
  disabled?: boolean;
}

// Características:
// - TextInput multiline, maxHeight: 120, scrollEnabled cuando supera límite
// - Platform.OS === 'ios' ajuste de padding bottom para home indicator
// - Botón Enviar deshabilitado cuando isStreaming || !text.trim()
// - Icono: PaperAirplaneIcon de @expo/vector-icons Feather o Ionicons send

// StyleSheet clave:
container: {
  flexDirection: 'row',
  alignItems: 'flex-end',
  paddingHorizontal: spacing.md,
  paddingVertical: spacing.sm,
  borderTopWidth: 1,
  borderTopColor: colors.gray[200],
  backgroundColor: colors.white,
  paddingBottom: Platform.OS === 'ios' ? spacing.lg : spacing.sm,
}
textInput: {
  flex: 1,
  minHeight: 40,
  maxHeight: 120,
  backgroundColor: colors.gray[50],
  borderRadius: borderRadius.xl,
  paddingHorizontal: spacing.md,
  paddingTop: 10,
  paddingBottom: 10,
  fontSize: 15,
  color: colors.gray[800],
  marginRight: spacing.sm,
}
sendButton: {
  width: 40,
  height: 40,
  borderRadius: 20,
  backgroundColor: colors.hidro[600],
  alignItems: 'center',
  justifyContent: 'center',
  marginBottom: 0,
}
```

##### `ChatSessionItemMobile.tsx`
**Ruta**: `apps/mobile/src/components/molecules/ChatSessionItemMobile.tsx`
**Responsabilidad**: Fila de una sesión en el modal de sesiones. Swipeable para acción de eliminar usando `react-native-gesture-handler` `Swipeable`.

```typescript
interface ChatSessionItemMobileProps {
  session: ChatSession;
  isActive: boolean;
  onSelect: (id: string) => void;
  onDelete: (id: string) => void;
}

// Layout:
// [icono conversación] [título truncado + fecha] [chevron derecho]
// Swipe izquierda → botón "Eliminar" rojo

// StyleSheet clave:
row: { flexDirection: 'row', alignItems: 'center', padding: spacing.md,
       backgroundColor: colors.white, borderBottomWidth: 1, borderColor: colors.gray[100] }
activeRow: { backgroundColor: colors.hidro[50] }
icon: { marginRight: spacing.sm }
title: { flex: 1, fontSize: 15, color: colors.gray[800], fontWeight: '500' }
activeTitle: { color: colors.hidro[700] }
date: { fontSize: 12, color: colors.gray[400] }
deleteAction: { backgroundColor: colors.error[500], justifyContent: 'center',
                alignItems: 'flex-end', paddingHorizontal: spacing.lg }
```

#### 6.3 — Organisms Mobile

##### `ChatMessageList.tsx`
**Ruta**: `apps/mobile/src/components/organisms/ChatMessageList.tsx`
**Responsabilidad**: FlatList de mensajes de la sesión activa. Auto-scroll al último. Muestra `TypingIndicator` durante streaming.

```typescript
import { FlatList, View } from 'react-native';
import { ChatMessageItem } from '../molecules/ChatMessageItem';
import { TypingIndicator } from '../atoms/TypingIndicator';
import { ChatDateDivider } from '../atoms/ChatDateDivider';

// Props: ninguno (consume useChatStore)
// Lee: messages, isStreaming, streamingText, messagesLoading, activeSessionId

// Lógica de renderizado:
// 1. Si !activeSessionId → EmptyState (átomo existente) con mensaje "Selecciona o crea una conversación"
// 2. Si messagesLoading → ActivityIndicator centrado
// 3. FlatList con keyExtractor={index} + ListFooterComponent:
//    - Si isStreaming && streamingText: ChatMessageItem con mensaje parcial (role: 'model')
//    - Si isStreaming && !streamingText: TypingIndicator visible={true}
// 4. ref.scrollToEnd({ animated: true }) en useEffect([messages.length, streamingText])

// StyleSheet:
container: { flex: 1, backgroundColor: colors.gray[50] }
contentContainer: { paddingHorizontal: spacing.md, paddingVertical: spacing.sm }
```

##### `ChatSessionsModal.tsx`
**Ruta**: `apps/mobile/src/components/organisms/ChatSessionsModal.tsx`
**Responsabilidad**: Modal deslizable (BottomSheet) que lista sesiones y permite crear/seleccionar/eliminar. Se abre desde el header de `ChatScreen`.

```typescript
import { Modal, FlatList, SafeAreaView, TouchableOpacity } from 'react-native';
import { ChatSessionItemMobile } from '../molecules/ChatSessionItemMobile';

// Props:
interface ChatSessionsModalProps {
  visible: boolean;
  onClose: () => void;
}

// Lee desde store: sessions, activeSessionId, sessionsLoading, createSession,
//                   setActiveSession, deleteSession, createSessionLoading

// Layout:
// SafeAreaView con header "Conversaciones" + botón "Nueva" (PlusIcon)
// FlatList de ChatSessionItemMobile (con swipe para eliminar)
// Botón cerrar en header top-right o swipe down

// Al seleccionar una sesión → setActiveSession(id) → onClose()
// Al crear nueva → createSession() → onClose()

// StyleSheet:
container: { flex: 1, backgroundColor: colors.white }
header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
          paddingHorizontal: spacing.lg, paddingVertical: spacing.md,
          borderBottomWidth: 1, borderColor: colors.gray[100] }
title: { fontSize: 17, fontWeight: '600', color: colors.gray[900] }
newBtn: { flexDirection: 'row', alignItems: 'center', gap: 4,
          paddingHorizontal: spacing.md, paddingVertical: spacing.sm,
          borderRadius: borderRadius.lg, backgroundColor: colors.hidro[50] }
newBtnText: { fontSize: 14, color: colors.hidro[600], fontWeight: '500' }
```

#### 6.4 — Screen Mobile

##### `ChatScreen.tsx`
**Ruta**: `apps/mobile/src/screens/ChatScreen.tsx`
**Responsabilidad**: Pantalla completa dedicada al chat. Equivalente móvil del ChatDrawer web. Accesible desde el tab "Más" del `MainTabNavigator`.

```typescript
import { SafeAreaView, KeyboardAvoidingView, Platform, View } from 'react-native';
import { ChatMessageList }    from '../components/organisms/ChatMessageList';
import { ChatInputMobile }    from '../components/molecules/ChatInputMobile';
import { ChatSessionsModal }  from '../components/organisms/ChatSessionsModal';
import { useChatSSE }         from '@hydroespinaca/shared';
import { useChatStore }       from '@hydroespinaca/shared';
import { Text }               from '../components/atoms/Text';
import { IconButton }         from '../components/atoms/IconButton';

// Estado local:
// - showSessionsModal: boolean

// Lógica:
// 1. useChatSessions() en useEffect para cargar sesiones al montar
// 2. useChatSSE() para el hook de streaming mobile
// 3. Mostrar título de la sesión activa en el header (o "Asistente IA")

// Layout con KeyboardAvoidingView:
//  <SafeAreaView flex:1>
//    <KeyboardAvoidingView behavior={ios ? 'padding' : 'height'} flex:1>
//      [Header: título sesión + botón sesiones]
//      <ChatMessageList />
//      <ChatInputMobile onSend={sendMessage} disabled={isStreaming} />
//    </KeyboardAvoidingView>
//  </SafeAreaView>
//  <ChatSessionsModal visible={showSessionsModal} onClose={...} />

// StyleSheet:
safeArea: { flex: 1, backgroundColor: colors.white }
header: {
  flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
  paddingHorizontal: spacing.lg, paddingTop: spacing.sm, paddingBottom: spacing.md,
  borderBottomWidth: 1, borderColor: colors.gray[100],
  backgroundColor: colors.white,
}
headerTitle: { fontSize: 17, fontWeight: '600', color: colors.gray[900], flex: 1 }
sessionsBtn: { padding: spacing.xs }
```

#### 6.5 — Integración en Navegación Mobile ⬜ PENDIENTE

##### Actualizar tipos de navegación
```typescript
// apps/mobile/src/navigation/types.ts — agregar:
export type MoreStackParamList = {
  MoreMenu: undefined;
  Profile: undefined;
  NotificationSettings: undefined;
  NotificationHistory: undefined;
  AdminAccess: undefined;
  AdminSessions: undefined;
  Chat: undefined;    // ← NUEVO
};
```

##### Actualizar `MoreStack.tsx`
```typescript
// apps/mobile/src/navigation/stacks/MoreStack.tsx — agregar:
import { ChatScreen } from '../../screens/ChatScreen';

// Dentro del Stack.Navigator:
<Stack.Screen
  name="Chat"
  component={ChatScreen}
  options={{ headerShown: false, title: 'Asistente IA' }}
/>
```

##### Agregar acceso desde el menú "Más"
El componente `MoreMenuScreen` (o `screens/more/MoreMenuScreen.tsx`) debe incluir un item de navegación:

```typescript
// En la lista de opciones del menú:
{
  icon: 'chatbubble-ellipses',
  label: 'Asistente IA',
  description: 'Consulta al asistente con RAG',
  onPress: () => navigation.navigate('Chat'),
}
```

---

## 4) Dependencias de Paquetes

### Shared Package
No hay dependencias nuevas (Zustand ya existe, fetch nativo, EventSource polyfill de Expo).

### Web (`apps/web/package.json`)
Verificar que estén instalados:
```json
{
  "react-markdown": "^9.x",
  "remark-gfm": "^4.x",
  "react-syntax-highlighter": "^15.x",
  "@types/react-syntax-highlighter": "^15.x",
  "@heroicons/react": "^2.x"
}
```

### Mobile (`apps/mobile/package.json`)
Verificar / agregar:
```json
{
  "react-native-markdown-display": "^7.x",
  "react-native-gesture-handler": "^2.x"  ← ya debería estar por react-navigation
}
```
Para SSE en mobile, evaluar si `expo-modules-core` ya provee `EventSource` (Expo SDK 50+) o si se necesita `react-native-sse`:
```bash
# Opción A (preferida si Expo SDK ≥ 50):
# EventSource disponible globalmente en React Native 0.73+

# Opción B:
pnpm add react-native-sse --filter @hydroespinaca/mobile
```

---

## 5) Orden de Ejecución Recomendado

```
[COMPLETADO] Fase 1: chatbot-service Python infra
[COMPLETADO] Fase 2: Atlas Vector Search & ingestión
[COMPLETADO] Fase 3: Core API endpoints SSE + BFF YARP

[PENDIENTE - PRIORIDAD ALTA]
Fase 4.4: useChatSSE hook en shared (bloquea mobile)
Fase 4.5: Actualizar exports del shared package

[PENDIENTE - PRIORIDAD ALTA]
Fase 5: Web — migrar todos los CSS Modules a Tailwind
  5.1: Atoms (ChatBubbleWeb, SessionDateLabel, TypingCursor)
  5.2: Molecules (ChatInput, ChatSessionItem, MarkdownMessage, StreamingMessage)
  5.3: Organisms (ChatSessionList, ChatToggleButton, MessageList)
  5.4: ChatDrawer + integración en layout.tsx

[PENDIENTE - PRIORIDAD MEDIA]
Fase 6: Mobile — crear desde cero
  6.1: Atoms (ChatBubbleMobile, TypingIndicator, ChatDateDivider)
  6.2: Molecules (ChatMessageItem, ChatInputMobile, ChatSessionItemMobile)
  6.3: Organisms (ChatMessageList, ChatSessionsModal)
  6.4: Screen (ChatScreen)
  6.5: Navegación (types.ts, MoreStack, menú)
```

---

## 6) Criterios de Aceptación por Componente

### Web
- [ ] No existe ningún archivo `*.module.css` en `src/components/chat/`.
- [ ] El `ChatDrawer` se desliza suavemente desde la derecha (transition Tailwind).
- [ ] El `ChatToggleButton` es visible en todas las páginas autenticadas (fijo, z-997).
- [ ] Los mensajes del usuario aparecen alineados a la derecha con fondo azul.
- [ ] Los mensajes del bot renderizan Markdown con código resaltado.
- [ ] El `StreamingMessage` muestra tokens llegando en tiempo real con cursor parpadeante.
- [ ] La lista de sesiones se agrupa por fecha (Hoy, Ayer, Esta semana...).
- [ ] El botón de nueva sesión crea una sesión y la activa inmediatamente.
- [ ] La eliminación de sesión funciona (swipe o botón hover).
- [ ] El `ChatInput` se bloquea (disabled) mientras el bot está respondiendo.

### Mobile
- [ ] `ChatScreen` es accesible desde el tab "Más" → item "Asistente IA".
- [ ] Los mensajes del usuario aparecen alineados a la derecha con fondo `hidro[600]`.
- [ ] Los mensajes del bot renderizan Markdown con `react-native-markdown-display`.
- [ ] El `TypingIndicator` (3 puntos animados) aparece mientras el stream está activo y sin texto.
- [ ] El `ChatInputMobile` evita que el teclado tape el campo de texto (`KeyboardAvoidingView`).
- [ ] El `ChatSessionsModal` se abre y permite crear/seleccionar/eliminar sesiones.
- [ ] El swipe-to-delete funciona en `ChatSessionItemMobile`.
- [ ] El streaming SSE funciona en mobile (EventSource nativo o polyfill).
- [ ] La sesión activa persiste entre navegaciones dentro de la app.

---

## 7) Notas de Implementación

### CSS Modules vs Tailwind
Los componentes de chat actuales en web usan `*.module.css` porque fueron generados antes de que se estableciera la convención de Tailwind como único sistema de estilos. La migración es mecánica:
1. Mapear cada clase CSS a su equivalente Tailwind.
2. Inline las clases en el JSX usando `cn()` (clsx) o string interpolation.
3. Eliminar el archivo `.module.css` y su import.

> ⚠️ **No mezclar**: No usar `className={styles.algo}` y `className="..."` Tailwind en el mismo componente. Si el componente tiene CSS Modules, migrar completamente o no tocar.

### Gestión de Estado SSE in Mobile
El hook `useChatSSE` de mobile y `useChatStream` de web ambos llaman a las mismas acciones del `useChatStore` (`startStream`, `appendStreamToken`, `finalizeStream`). El store es agnóstico al mecanismo de transporte.

### Mock durante Desarrollo
Mientras el `chatbot-service` no esté corriendo localmente, se puede hacer una implementación mock del stream en `useChatStream` / `useChatSSE` para desarrollar la UI de manera aislada:

```typescript
// Mock simple para testing UI:
const mockStream = async (message: string) => {
  startStream();
  const words = `Esta es una respuesta de prueba para el mensaje: ${message}`.split(' ');
  for (const word of words) {
    await new Promise(r => setTimeout(r, 80));
    appendStreamToken(word + ' ');
  }
  finalizeStream({ tokensUsed: words.length, sessionTitle: 'Sesión de prueba' });
};
```
