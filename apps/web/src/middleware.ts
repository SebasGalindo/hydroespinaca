import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

/**
 * Middleware para protección de rutas basado en sesión
 *
 * Flujo:
 * 1. Rutas públicas: /, /login (acceso sin sesión)
 * 2. Rutas protegidas: /dashboard/*, /app/* (requieren sesión)
 * 3. Si no hay SessionId cookie y la ruta es protegida → redirige a /login
 * 4. Si hay SessionId y va a /login → redirige a /dashboard
 */
export async function middleware(req: NextRequest) {
  const url = req.nextUrl.clone();
  const path = url.pathname;

  // Rutas públicas (no requieren autenticación)
  const publicPaths = ['/', '/login', '/forgot-password', '/reset-password'];
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

  // Permitir el acceso
  return NextResponse.next();
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
