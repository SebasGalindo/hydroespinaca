/**
 * Utility function to read cookies accessible by JavaScript
 * @param name Cookie name to read
 * @returns Cookie value or null if not found
 */
export function getCookie(name: string): string | null {
  const value = `; ${document.cookie}`;
  const parts = value.split(`; ${name}=`);
  
  if (parts.length === 2) {
    const cookieValue = parts.pop()?.split(';').shift() ?? null;
    return cookieValue ? decodeURIComponent(cookieValue) : null;
  }
  
  return null;
}

/**
 * Debug function to log all cookies and their status (development only)
 */
export function debugCookies(): void {
  // Only show debug logs in development
  if (typeof process !== 'undefined' && process.env.NODE_ENV === 'production') {
    return;
  }
  
  console.log('🍪 === Cookie Debug ===');
  console.log('🍪 document.cookie:', document.cookie);
  
  if (!document.cookie) {
    console.log('🍪 No cookies found');
    return;
  }
  
  const cookies = document.cookie.split(';');
  console.log('🍪 All cookies:');
  cookies.forEach((cookie, index) => {
    const [name, value] = cookie.trim().split('=');
    console.log(`🍪 [${index}] ${name} = ${value ? decodeURIComponent(value) : 'empty'}`);
  });
  
  // Check specific auth cookies
  const sessionId = getCookie('SessionId');
  const csrfToken = getCookie('CsrfToken');
  
  console.log('🍪 SessionId:', sessionId ? '[HttpOnly - not accessible]' : 'null');
  console.log('🍪 CsrfToken:', csrfToken ? `${csrfToken.substring(0, 20)}...` : 'null');
  console.log('🍪 === End Cookie Debug ===');
}

/**
 * Sets a cookie (for client-side use, mainly for development/testing)
 * @param name Cookie name
 * @param value Cookie value
 * @param days Days until expiration
 */
export function setCookie(name: string, value: string, days: number = 7): void {
  const expires = new Date();
  expires.setTime(expires.getTime() + days * 24 * 60 * 60 * 1000);
  document.cookie = `${name}=${encodeURIComponent(value)};expires=${expires.toUTCString()};path=/`;
}

/**
 * Deletes a cookie
 * @param name Cookie name to delete
 */
export function deleteCookie(name: string): void {
  document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
}