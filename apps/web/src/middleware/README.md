# Middleware de Next.js

Este directorio contiene el middleware de Next.js que maneja la autenticación, autorización y protección de rutas en la aplicación de monitoreo hidropónico.

## Arquitectura

### Principios de Diseño

- **Seguridad First**: Protección robusta de rutas sensibles
- **Performance**: Ejecución eficiente en Edge Runtime
- **Flexibilidad**: Configuración granular de permisos
- **Observabilidad**: Logging detallado de accesos
- **Escalabilidad**: Preparado para múltiples roles y permisos

### Estructura del Directorio

```
src/middleware/
├── middleware.ts         # Middleware principal de Next.js
├── auth.ts              # Lógica de autenticación
├── permissions.ts       # Sistema de permisos
├── routes.ts           # Configuración de rutas
├── utils.ts            # Utilidades del middleware
└── README.md           # Esta documentación
```

## Middleware Principal

### middleware.ts - Punto de Entrada

**Propósito**: Middleware principal que intercepta todas las requests

```typescript
import { NextRequest, NextResponse } from 'next/server';
import { authMiddleware } from './auth';
import { checkPermissions } from './permissions';
import { getRouteConfig } from './routes';
import { logRequest } from './utils';

/**
 * Middleware principal de Next.js
 * Se ejecuta en todas las rutas configuradas
 */
export async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  
  // Log de la request
  logRequest(request);
  
  // Obtener configuración de la ruta
  const routeConfig = getRouteConfig(pathname);
  
  // Si la ruta es pública, permitir acceso
  if (routeConfig.isPublic) {
    return NextResponse.next();
  }
  
  // Verificar autenticación
  const authResult = await authMiddleware(request);
  if (!authResult.success) {
    return authResult.response;
  }
  
  // Verificar permisos
  const permissionResult = await checkPermissions(
    request,
    authResult.user,
    routeConfig
  );
  if (!permissionResult.success) {
    return permissionResult.response;
  }
  
  // Agregar headers de usuario a la request
  const requestHeaders = new Headers(request.headers);
  requestHeaders.set('x-user-id', authResult.user.id);
  requestHeaders.set('x-user-role', authResult.user.role);
  requestHeaders.set('x-user-permissions', JSON.stringify(authResult.user.permissions));
  
  return NextResponse.next({
    request: {
      headers: requestHeaders
    }
  });
}

/**
 * Configuración del matcher
 * Define qué rutas debe interceptar el middleware
 */
export const config = {
  matcher: [
    /*
     * Interceptar todas las rutas excepto:
     * - API routes de Next.js (_next)
     * - Archivos estáticos (favicon, images, etc.)
     * - API routes públicas específicas
     */
    '/((?!_next/static|_next/image|favicon.ico|api/auth|api/public).*)',
  ],
};
```

### auth.ts - Lógica de Autenticación

**Propósito**: Manejo de autenticación de usuarios

```typescript
import { NextRequest, NextResponse } from 'next/server';
import { jwtVerify } from 'jose';
import type { User } from '@/shared/types';

/**
 * Resultado de autenticación
 */
interface AuthResult {
  success: boolean;
  user?: User;
  response?: NextResponse;
}

/**
 * Secret para verificación de JWT
 */
const JWT_SECRET = new TextEncoder().encode(
  process.env.JWT_SECRET || 'your-secret-key'
);

/**
 * Middleware de autenticación
 */
export async function authMiddleware(request: NextRequest): Promise<AuthResult> {
  try {
    // Obtener token del header Authorization o cookie
    const token = getTokenFromRequest(request);
    
    if (!token) {
      return {
        success: false,
        response: redirectToLogin(request)
      };
    }
    
    // Verificar y decodificar el token
    const { payload } = await jwtVerify(token, JWT_SECRET);
    
    // Construir objeto de usuario
    const user: User = {
      id: payload.sub as string,
      email: payload.email as string,
      name: payload.name as string,
      role: payload.role as any,
      permissions: payload.permissions as any[],
      status: 'active'
    };
    
    // Verificar si el usuario está activo
    if (user.status !== 'active') {
      return {
        success: false,
        response: redirectToLogin(request, 'account_inactive')
      };
    }
    
    return {
      success: true,
      user
    };
    
  } catch (error) {
    console.error('Auth middleware error:', error);
    return {
      success: false,
      response: redirectToLogin(request, 'invalid_token')
    };
  }
}

/**
 * Extrae el token de la request
 */
function getTokenFromRequest(request: NextRequest): string | null {
  // Intentar obtener del header Authorization
  const authHeader = request.headers.get('authorization');
  if (authHeader && authHeader.startsWith('Bearer ')) {
    return authHeader.substring(7);
  }
  
  // Intentar obtener de las cookies
  const tokenCookie = request.cookies.get('auth_token');
  if (tokenCookie) {
    return tokenCookie.value;
  }
  
  return null;
}

/**
 * Redirige al login con parámetros opcionales
 */
function redirectToLogin(
  request: NextRequest,
  reason?: string
): NextResponse {
  const loginUrl = new URL('/auth/login', request.url);
  
  // Agregar URL de retorno
  loginUrl.searchParams.set('returnUrl', request.nextUrl.pathname);
  
  // Agregar razón si se proporciona
  if (reason) {
    loginUrl.searchParams.set('reason', reason);
  }
  
  return NextResponse.redirect(loginUrl);
}

/**
 * Verifica si un token es válido sin decodificarlo completamente
 */
export async function isTokenValid(token: string): Promise<boolean> {
  try {
    await jwtVerify(token, JWT_SECRET);
    return true;
  } catch {
    return false;
  }
}

/**
 * Refresca un token próximo a expirar
 */
export async function refreshTokenIfNeeded(
  request: NextRequest
): Promise<NextResponse | null> {
  const token = getTokenFromRequest(request);
  if (!token) return null;
  
  try {
    const { payload } = await jwtVerify(token, JWT_SECRET);
    const exp = payload.exp as number;
    const now = Math.floor(Date.now() / 1000);
    const timeUntilExpiry = exp - now;
    
    // Si el token expira en menos de 5 minutos, intentar refrescarlo
    if (timeUntilExpiry < 300) {
      // Aquí iría la lógica para refrescar el token
      // Por ahora, solo loggeamos
      console.log('Token needs refresh');
    }
    
    return null;
  } catch {
    return null;
  }
}
```

### permissions.ts - Sistema de Permisos

**Propósito**: Verificación granular de permisos por ruta

```typescript
import { NextRequest, NextResponse } from 'next/server';
import type { User, Permission } from '@/shared/types';
import type { RouteConfig } from './routes';

/**
 * Resultado de verificación de permisos
 */
interface PermissionResult {
  success: boolean;
  response?: NextResponse;
}

/**
 * Verifica permisos del usuario para una ruta específica
 */
export async function checkPermissions(
  request: NextRequest,
  user: User,
  routeConfig: RouteConfig
): Promise<PermissionResult> {
  // Si la ruta no requiere permisos específicos
  if (!routeConfig.requiredPermissions || routeConfig.requiredPermissions.length === 0) {
    return { success: true };
  }
  
  // Verificar si el usuario tiene los permisos requeridos
  const hasPermission = checkUserPermissions(
    user,
    routeConfig.requiredPermissions,
    routeConfig.permissionMode || 'any'
  );
  
  if (!hasPermission) {
    return {
      success: false,
      response: createForbiddenResponse(request, routeConfig)
    };
  }
  
  // Verificar restricciones adicionales
  const additionalChecks = await performAdditionalChecks(
    request,
    user,
    routeConfig
  );
  
  if (!additionalChecks.success) {
    return additionalChecks;
  }
  
  return { success: true };
}

/**
 * Verifica si el usuario tiene los permisos necesarios
 */
function checkUserPermissions(
  user: User,
  requiredPermissions: Permission[],
  mode: 'any' | 'all' = 'any'
): boolean {
  if (mode === 'all') {
    return requiredPermissions.every(permission => 
      user.permissions.includes(permission)
    );
  } else {
    return requiredPermissions.some(permission => 
      user.permissions.includes(permission)
    );
  }
}

/**
 * Realiza verificaciones adicionales específicas de la ruta
 */
async function performAdditionalChecks(
  request: NextRequest,
  user: User,
  routeConfig: RouteConfig
): Promise<PermissionResult> {
  // Verificar restricciones de horario
  if (routeConfig.timeRestrictions) {
    const now = new Date();
    const currentHour = now.getHours();
    const { startHour, endHour } = routeConfig.timeRestrictions;
    
    if (currentHour < startHour || currentHour > endHour) {
      return {
        success: false,
        response: createTimeRestrictedResponse(request)
      };
    }
  }
  
  // Verificar límites de rate limiting por usuario
  if (routeConfig.rateLimiting) {
    const isRateLimited = await checkRateLimit(
      user.id,
      routeConfig.rateLimiting
    );
    
    if (isRateLimited) {
      return {
        success: false,
        response: createRateLimitedResponse(request)
      };
    }
  }
  
  // Verificar restricciones de IP
  if (routeConfig.ipRestrictions) {
    const clientIP = getClientIP(request);
    const isAllowed = routeConfig.ipRestrictions.allowedIPs.includes(clientIP);
    
    if (!isAllowed) {
      return {
        success: false,
        response: createIPRestrictedResponse(request)
      };
    }
  }
  
  return { success: true };
}

/**
 * Crea respuesta de acceso denegado
 */
function createForbiddenResponse(
  request: NextRequest,
  routeConfig: RouteConfig
): NextResponse {
  // Para rutas de API, devolver JSON
  if (request.nextUrl.pathname.startsWith('/api/')) {
    return NextResponse.json(
      {
        error: 'Forbidden',
        message: 'No tienes permisos para acceder a este recurso',
        requiredPermissions: routeConfig.requiredPermissions
      },
      { status: 403 }
    );
  }
  
  // Para rutas de página, redirigir a página de error
  const errorUrl = new URL('/403', request.url);
  errorUrl.searchParams.set('returnUrl', request.nextUrl.pathname);
  return NextResponse.redirect(errorUrl);
}

/**
 * Crea respuesta de restricción de horario
 */
function createTimeRestrictedResponse(request: NextRequest): NextResponse {
  if (request.nextUrl.pathname.startsWith('/api/')) {
    return NextResponse.json(
      {
        error: 'Time Restricted',
        message: 'Acceso no permitido en este horario'
      },
      { status: 403 }
    );
  }
  
  return NextResponse.redirect(new URL('/time-restricted', request.url));
}

/**
 * Crea respuesta de rate limiting
 */
function createRateLimitedResponse(request: NextRequest): NextResponse {
  if (request.nextUrl.pathname.startsWith('/api/')) {
    return NextResponse.json(
      {
        error: 'Rate Limited',
        message: 'Demasiadas requests. Intenta más tarde.'
      },
      { status: 429 }
    );
  }
  
  return NextResponse.redirect(new URL('/rate-limited', request.url));
}

/**
 * Crea respuesta de restricción de IP
 */
function createIPRestrictedResponse(request: NextRequest): NextResponse {
  return NextResponse.json(
    {
      error: 'IP Restricted',
      message: 'Acceso no permitido desde esta IP'
    },
    { status: 403 }
  );
}

/**
 * Verifica rate limiting para un usuario
 */
async function checkRateLimit(
  userId: string,
  rateLimiting: { requests: number; windowMs: number }
): Promise<boolean> {
  // Implementación simplificada
  // En producción, usar Redis o similar
  const key = `rate_limit:${userId}`;
  const now = Date.now();
  
  // Aquí iría la lógica real de rate limiting
  // Por ahora, siempre permitir
  return false;
}

/**
 * Obtiene la IP del cliente
 */
function getClientIP(request: NextRequest): string {
  const forwarded = request.headers.get('x-forwarded-for');
  const realIP = request.headers.get('x-real-ip');
  
  if (forwarded) {
    return forwarded.split(',')[0].trim();
  }
  
  if (realIP) {
    return realIP;
  }
  
  return request.ip || 'unknown';
}
```

### routes.ts - Configuración de Rutas

**Propósito**: Definición de permisos y restricciones por ruta

```typescript
import type { Permission } from '@/shared/types';

/**
 * Configuración de una ruta
 */
export interface RouteConfig {
  /** Si la ruta es pública (no requiere autenticación) */
  isPublic: boolean;
  /** Permisos requeridos para acceder */
  requiredPermissions?: Permission[];
  /** Modo de verificación de permisos */
  permissionMode?: 'any' | 'all';
  /** Restricciones de horario */
  timeRestrictions?: {
    startHour: number;
    endHour: number;
  };
  /** Configuración de rate limiting */
  rateLimiting?: {
    requests: number;
    windowMs: number;
  };
  /** Restricciones de IP */
  ipRestrictions?: {
    allowedIPs: string[];
  };
  /** Descripción de la ruta */
  description?: string;
}

/**
 * Configuración de rutas de la aplicación
 */
const ROUTE_CONFIGS: Record<string, RouteConfig> = {
  // Rutas públicas
  '/': {
    isPublic: true,
    description: 'Página de inicio'
  },
  '/auth/login': {
    isPublic: true,
    description: 'Página de login'
  },
  '/auth/register': {
    isPublic: true,
    description: 'Página de registro'
  },
  '/auth/forgot-password': {
    isPublic: true,
    description: 'Recuperación de contraseña'
  },
  
  // Dashboard principal
  '/dashboard': {
    isPublic: false,
    requiredPermissions: ['DASHBOARD_VIEW'],
    description: 'Dashboard principal'
  },
  
  // Lecturas de sensores
  '/dashboard/lecturas': {
    isPublic: false,
    requiredPermissions: ['SENSORS_VIEW', 'READINGS_VIEW'],
    permissionMode: 'any',
    description: 'Página de lecturas de sensores'
  },
  
  // Configuración de sensores
  '/dashboard/sensores-config': {
    isPublic: false,
    requiredPermissions: ['SENSORS_MANAGE'],
    description: 'Configuración de sensores'
  },
  
  // Configuración de actuadores
  '/dashboard/actuadores-config': {
    isPublic: false,
    requiredPermissions: ['ACTUATORS_MANAGE'],
    description: 'Configuración de actuadores'
  },
  
  // Variables del sistema
  '/dashboard/variables-config': {
    isPublic: false,
    requiredPermissions: ['VARIABLES_MANAGE'],
    description: 'Configuración de variables'
  },
  
  // Nueva variable
  '/dashboard/nueva-variable': {
    isPublic: false,
    requiredPermissions: ['VARIABLES_CREATE'],
    description: 'Crear nueva variable'
  },
  
  // Monitoreo del sistema
  '/dashboard/monitoreo': {
    isPublic: false,
    requiredPermissions: ['SYSTEM_MONITOR'],
    description: 'Monitoreo del sistema'
  },
  
  // APIs públicas
  '/api/auth/*': {
    isPublic: true,
    description: 'APIs de autenticación'
  },
  '/api/public/*': {
    isPublic: true,
    description: 'APIs públicas'
  },
  
  // APIs de sensores
  '/api/sensors': {
    isPublic: false,
    requiredPermissions: ['SENSORS_VIEW'],
    rateLimiting: {
      requests: 100,
      windowMs: 60000 // 1 minuto
    },
    description: 'API de sensores'
  },
  
  // APIs de lecturas
  '/api/readings': {
    isPublic: false,
    requiredPermissions: ['READINGS_VIEW'],
    rateLimiting: {
      requests: 200,
      windowMs: 60000
    },
    description: 'API de lecturas'
  },
  
  // APIs de actuadores
  '/api/actuators': {
    isPublic: false,
    requiredPermissions: ['ACTUATORS_VIEW'],
    description: 'API de actuadores'
  },
  
  // APIs de administración
  '/api/admin/*': {
    isPublic: false,
    requiredPermissions: ['ADMIN_ACCESS'],
    timeRestrictions: {
      startHour: 6,
      endHour: 22
    },
    description: 'APIs de administración'
  }
};

/**
 * Obtiene la configuración de una ruta
 */
export function getRouteConfig(pathname: string): RouteConfig {
  // Buscar coincidencia exacta
  if (ROUTE_CONFIGS[pathname]) {
    return ROUTE_CONFIGS[pathname];
  }
  
  // Buscar coincidencia con wildcards
  for (const [pattern, config] of Object.entries(ROUTE_CONFIGS)) {
    if (pattern.endsWith('/*')) {
      const basePattern = pattern.slice(0, -2);
      if (pathname.startsWith(basePattern)) {
        return config;
      }
    }
  }
  
  // Configuración por defecto para rutas no definidas
  return {
    isPublic: false,
    requiredPermissions: ['DASHBOARD_VIEW'],
    description: 'Ruta protegida por defecto'
  };
}

/**
 * Obtiene todas las rutas configuradas
 */
export function getAllRoutes(): Record<string, RouteConfig> {
  return ROUTE_CONFIGS;
}

/**
 * Verifica si una ruta es pública
 */
export function isPublicRoute(pathname: string): boolean {
  const config = getRouteConfig(pathname);
  return config.isPublic;
}

/**
 * Obtiene rutas por permiso
 */
export function getRoutesByPermission(permission: Permission): string[] {
  return Object.entries(ROUTE_CONFIGS)
    .filter(([_, config]) => 
      config.requiredPermissions?.includes(permission)
    )
    .map(([route]) => route);
}
```

### utils.ts - Utilidades del Middleware

**Propósito**: Funciones helper para el middleware

```typescript
import { NextRequest } from 'next/server';

/**
 * Información de la request para logging
 */
interface RequestInfo {
  method: string;
  url: string;
  userAgent?: string;
  ip?: string;
  timestamp: string;
  headers: Record<string, string>;
}

/**
 * Loggea información de la request
 */
export function logRequest(request: NextRequest): void {
  const requestInfo: RequestInfo = {
    method: request.method,
    url: request.url,
    userAgent: request.headers.get('user-agent') || undefined,
    ip: getClientIP(request),
    timestamp: new Date().toISOString(),
    headers: Object.fromEntries(request.headers.entries())
  };
  
  // En desarrollo, log a consola
  if (process.env.NODE_ENV === 'development') {
    console.log('🔍 Request:', {
      method: requestInfo.method,
      url: requestInfo.url,
      ip: requestInfo.ip,
      userAgent: requestInfo.userAgent
    });
  }
  
  // En producción, enviar a servicio de logging
  if (process.env.NODE_ENV === 'production') {
    // Aquí iría la integración con servicio de logging
    // como DataDog, LogRocket, etc.
  }
}

/**
 * Obtiene la IP del cliente
 */
export function getClientIP(request: NextRequest): string {
  // Verificar headers de proxy
  const forwarded = request.headers.get('x-forwarded-for');
  if (forwarded) {
    return forwarded.split(',')[0].trim();
  }
  
  const realIP = request.headers.get('x-real-ip');
  if (realIP) {
    return realIP;
  }
  
  const cfConnectingIP = request.headers.get('cf-connecting-ip');
  if (cfConnectingIP) {
    return cfConnectingIP;
  }
  
  return request.ip || 'unknown';
}

/**
 * Verifica si la request viene de un bot
 */
export function isBot(request: NextRequest): boolean {
  const userAgent = request.headers.get('user-agent') || '';
  const botPatterns = [
    /googlebot/i,
    /bingbot/i,
    /slurp/i,
    /duckduckbot/i,
    /baiduspider/i,
    /yandexbot/i,
    /facebookexternalhit/i,
    /twitterbot/i,
    /rogerbot/i,
    /linkedinbot/i,
    /embedly/i,
    /quora link preview/i,
    /showyoubot/i,
    /outbrain/i,
    /pinterest/i,
    /developers.google.com\/\+\/web\/snippet\//i
  ];
  
  return botPatterns.some(pattern => pattern.test(userAgent));
}

/**
 * Obtiene información del dispositivo
 */
export function getDeviceInfo(request: NextRequest): {
  type: 'mobile' | 'tablet' | 'desktop';
  os?: string;
  browser?: string;
} {
  const userAgent = request.headers.get('user-agent') || '';
  
  // Detectar tipo de dispositivo
  let type: 'mobile' | 'tablet' | 'desktop' = 'desktop';
  if (/Mobile|Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(userAgent)) {
    type = /iPad|Android(?!.*Mobile)/i.test(userAgent) ? 'tablet' : 'mobile';
  }
  
  // Detectar OS
  let os: string | undefined;
  if (/Windows/i.test(userAgent)) os = 'Windows';
  else if (/Mac OS/i.test(userAgent)) os = 'macOS';
  else if (/Linux/i.test(userAgent)) os = 'Linux';
  else if (/Android/i.test(userAgent)) os = 'Android';
  else if (/iOS|iPhone|iPad/i.test(userAgent)) os = 'iOS';
  
  // Detectar browser
  let browser: string | undefined;
  if (/Chrome/i.test(userAgent)) browser = 'Chrome';
  else if (/Firefox/i.test(userAgent)) browser = 'Firefox';
  else if (/Safari/i.test(userAgent)) browser = 'Safari';
  else if (/Edge/i.test(userAgent)) browser = 'Edge';
  
  return { type, os, browser };
}

/**
 * Genera un ID único para la request
 */
export function generateRequestId(): string {
  return `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
}

/**
 * Verifica si la request es HTTPS
 */
export function isSecureRequest(request: NextRequest): boolean {
  return request.nextUrl.protocol === 'https:' || 
         request.headers.get('x-forwarded-proto') === 'https';
}

/**
 * Obtiene el origen de la request
 */
export function getOrigin(request: NextRequest): string {
  return request.headers.get('origin') || 
         request.headers.get('referer') || 
         'unknown';
}

/**
 * Verifica si la request es de un origen permitido
 */
export function isAllowedOrigin(
  request: NextRequest,
  allowedOrigins: string[]
): boolean {
  const origin = getOrigin(request);
  return allowedOrigins.includes(origin) || origin === 'unknown';
}

/**
 * Sanitiza headers sensibles para logging
 */
export function sanitizeHeaders(
  headers: Headers
): Record<string, string> {
  const sensitiveHeaders = [
    'authorization',
    'cookie',
    'x-api-key',
    'x-auth-token'
  ];
  
  const sanitized: Record<string, string> = {};
  
  for (const [key, value] of headers.entries()) {
    if (sensitiveHeaders.includes(key.toLowerCase())) {
      sanitized[key] = '[REDACTED]';
    } else {
      sanitized[key] = value;
    }
  }
  
  return sanitized;
}

/**
 * Calcula el hash de una request para cache
 */
export async function calculateRequestHash(
  request: NextRequest
): Promise<string> {
  const data = {
    method: request.method,
    url: request.url,
    headers: sanitizeHeaders(request.headers)
  };
  
  const encoder = new TextEncoder();
  const dataBuffer = encoder.encode(JSON.stringify(data));
  const hashBuffer = await crypto.subtle.digest('SHA-256', dataBuffer);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  
  return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
}
```

## Configuración y Deployment

### Variables de Entorno

```bash
# Autenticación
JWT_SECRET=your-super-secret-jwt-key
JWT_EXPIRES_IN=24h

# Rate Limiting
REDIS_URL=redis://localhost:6379
RATE_LIMIT_ENABLED=true

# Logging
LOG_LEVEL=info
LOG_SERVICE_URL=https://your-logging-service.com

# Security
ALLOWED_ORIGINS=http://localhost:3000,https://yourdomain.com
CSRF_SECRET=your-csrf-secret
```

### Edge Runtime

El middleware se ejecuta en Edge Runtime para mejor performance:

```typescript
// Configuración automática para Edge Runtime
export const runtime = 'edge';
```

## Testing

### Estrategias de Testing

```typescript
// Ejemplo de test para middleware
import { NextRequest } from 'next/server';
import { middleware } from './middleware';

describe('Middleware', () => {
  it('should allow access to public routes', async () => {
    const request = new NextRequest('http://localhost:3000/');
    const response = await middleware(request);
    
    expect(response.status).toBe(200);
  });
  
  it('should redirect to login for protected routes without token', async () => {
    const request = new NextRequest('http://localhost:3000/dashboard');
    const response = await middleware(request);
    
    expect(response.status).toBe(307); // Redirect
    expect(response.headers.get('location')).toContain('/auth/login');
  });
  
  it('should allow access with valid token and permissions', async () => {
    const request = new NextRequest('http://localhost:3000/dashboard');
    request.headers.set('authorization', 'Bearer valid-token');
    
    const response = await middleware(request);
    expect(response.status).toBe(200);
  });
});
```

## Performance y Optimización

### Mejores Prácticas

- **Caché de verificaciones**: Cachear resultados de verificación de tokens
- **Rate limiting eficiente**: Usar Redis para rate limiting distribuido
- **Logging asíncrono**: No bloquear requests con logging
- **Minimizar lógica**: Mantener el middleware ligero
- **Edge Runtime**: Aprovechar la distribución global

### Monitoreo

```typescript
// Métricas del middleware
export const middlewareMetrics = {
  requestsTotal: 0,
  authSuccessful: 0,
  authFailed: 0,
  permissionDenied: 0,
  rateLimited: 0,
  averageResponseTime: 0
};
```

## Seguridad

### Consideraciones de Seguridad

- **Validación de tokens**: Verificación robusta de JWT
- **Rate limiting**: Protección contra ataques de fuerza bruta
- **Logging de seguridad**: Registro de intentos de acceso
- **Headers de seguridad**: CSP, HSTS, etc.
- **Sanitización**: Limpieza de datos sensibles en logs

### Headers de Seguridad

```typescript
// Agregar headers de seguridad
const securityHeaders = {
  'X-Frame-Options': 'DENY',
  'X-Content-Type-Options': 'nosniff',
  'Referrer-Policy': 'strict-origin-when-cross-origin',
  'Permissions-Policy': 'camera=(), microphone=(), geolocation=()'
};
```

## Contribución

### Agregar Nueva Funcionalidad

1. **Identificar necesidad**: ¿Qué problema resuelve?
2. **Diseñar solución**: Mantener performance y seguridad
3. **Implementar**: Seguir patrones existentes
4. **Testear**: Casos de uso y edge cases
5. **Documentar**: Actualizar esta documentación
6. **Revisar**: Code review enfocado en seguridad

### Code Review Checklist

- [ ] Performance considerada (Edge Runtime)
- [ ] Seguridad verificada
- [ ] Tests incluidos
- [ ] Logging apropiado
- [ ] Documentación actualizada
- [ ] Manejo de errores robusto
- [ ] Compatibilidad con Edge Runtime

## Recursos

- [Next.js Middleware](https://nextjs.org/docs/app/building-your-application/routing/middleware)
- [Edge Runtime](https://nextjs.org/docs/app/api-reference/edge)
- [JWT Best Practices](https://auth0.com/blog/a-look-at-the-latest-draft-for-jwt-bcp/)
- [Web Security](https://web.dev/security/)