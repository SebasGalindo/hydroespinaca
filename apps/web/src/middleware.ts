import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

/**
 * Middleware para protección de rutas basado en sesión
 *
 * Flujo:
 * 1. Rutas públicas: /, /login (acceso sin sesión)
 * 2. Rutas protegidas: /dashboard/*, /app/*, /admin/* (requieren sesión)
 * 3. Si no hay SessionId cookie y la ruta es protegida → redirige a /login
 * 4. Si hay SessionId y va a /login → redirige a /dashboard
 *
 * Nota: La validación de roles se hace en el componente AdminRoute (client-side)
 * después de que checkSession() obtenga los datos del usuario del BFF.
 * El estado inicial isLoading=true previene redirects prematuros durante SSR.
 */
export async function middleware(req: NextRequest) {
  const url = req.nextUrl.clone();
  const path = url.pathname;

  // Rutas públicas (no requieren autenticación)
  const publicPaths = ['/', '/login'];
  const isPublicPath = publicPaths.includes(path);

  // Rutas estáticas de Next.js (excluir del middleware)
  if (
    path.startsWith('/_next/') ||
    path.startsWith('/api/') ||
    path.includes('.') // archivos estáticos (.js, .css, .ico, etc.)
  ) {
    return NextResponse.next();
  }

  // Verificar si existe la cookie de sesión
  const hasSession = req.cookies.has('SessionId');

  // Si no hay sesión y la ruta es protegida → redirigir a /login
  if (!hasSession && !isPublicPath) {
    if (process.env.NODE_ENV === 'development') {
      console.info(`[Middleware] No session found, redirecting to /login from ${path}`);
    }
    url.pathname = '/login';
    url.searchParams.set('from', path);
    return NextResponse.redirect(url);
  }

  // Si hay sesión y el usuario intenta acceder a /login → redirigir a /dashboard
  if (hasSession && path === '/login') {
    if (process.env.NODE_ENV === 'development') {
      console.info('[Middleware] Session found, redirecting to /dashboard');
    }
    url.pathname = '/dashboard';
    return NextResponse.redirect(url);
  }

  // Permitir el acceso - la validación de rol admin se hace en AdminRoute
  const response = NextResponse.next();

  // Agregar Content Security Policy headers para permitir ejecución de scripts
  // Esta política permite:
  // - Scripts propios ('self')
  // - Scripts inline necesarios para React/Next.js ('unsafe-inline')
  // - Scripts de Cloudflare para protección y optimización
  // - Conexiones a la API y WebSocket
  const csp = [
    "default-src 'self'",
    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://hydroespinaca.online https://*.cloudflare.com https://static.cloudflareinsights.com https://cdn.jsdelivr.net",
    "connect-src 'self' https://api.hydroespinaca.online wss://hydroespinaca.online wss://mqtt.hydroespinaca.online https://*.cloudflare.com https://static.cloudflareinsights.com",
    "img-src 'self' data: https: blob:",
    "style-src 'self' 'unsafe-inline' https:",
    "font-src 'self' data: https:",
    "frame-ancestors 'none'",
    "base-uri 'self'",
    "form-action 'self'"
  ].join('; ');


  response.headers.set('Content-Security-Policy', csp);

  return response;
}

/**
 * Configuración del matcher
 * Aplica el middleware a todas las rutas excepto:
 * - _next/static (archivos estáticos de Next.js)
 * - _next/image (optimización de imágenes)
 * - favicon.ico
 * - Archivos en /images/
 */
export const config = {
  matcher: [
    '/((?!_next/static|_next/image|favicon.ico|images).*)',
  ],
};
