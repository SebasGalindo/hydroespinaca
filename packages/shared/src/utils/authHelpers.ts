/**
 * Shared auth helper utilities.
 * Pure functions — no platform dependency.
 */

interface SessionLike {
  role?: string | null;
}

/**
 * Check if a user session has admin privileges.
 * Matches both 'administrador' and 'admin' role names (case-insensitive).
 *
 * @example
 * isUserAdmin({ role: 'Administrador' }) // true
 * isUserAdmin({ role: 'admin' })         // true
 * isUserAdmin({ role: 'viewer' })        // false
 * isUserAdmin(null)                      // false
 */
export function isUserAdmin(session: SessionLike | null | undefined): boolean {
  const role = session?.role?.toLowerCase();
  return role === 'administrador' || role === 'admin';
}
