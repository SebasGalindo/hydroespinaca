/**
 * Centralized SweetAlert2 helpers.
 *
 * All confirm/success/error dialogs should use these functions so colours,
 * button labels, and toast behaviour stay consistent across the app.
 *
 * SweetAlert2 is lazy-loaded on first use to keep it out of the initial bundle.
 */
import type SwalType from 'sweetalert2';

// ── Lazy singleton ────────────────────────────────────────────────────────────
let _swal: typeof SwalType | null = null;
async function getSwal() {
  if (!_swal) _swal = (await import('sweetalert2')).default;
  return _swal;
}

// ── Theme colours (match Tailwind @theme tokens) ──────────────────────────────
const CONFIRM_COLOR = '#16a34a'; // hidro-green
const CANCEL_COLOR = '#6b7280'; // hidro-gray
const DANGER_COLOR = '#ef4444'; // hidro-error

// ── Success toast ─────────────────────────────────────────────────────────────

interface ShowSuccessOptions {
  title: string;
  text?: string;
}

/**
 * Shows a short success toast after a mutation (create / update / delete).
 * Auto-dismisses after 1.5 s; no confirm button.
 *
 * @example
 * showSuccess({ title: '¡Creado!', text: 'El registro fue creado.' });
 */
export async function showSuccess({ title, text }: ShowSuccessOptions) {
  const Swal = await getSwal();
  return Swal.fire({
    icon: 'success',
    title,
    text,
    timer: 1500,
    showConfirmButton: false,
    confirmButtonColor: CONFIRM_COLOR,
  });
}

// ── Error alert ───────────────────────────────────────────────────────────────

interface ShowErrorOptions {
  title?: string;
  text: string;
}

/**
 * Shows an error alert — typically after a caught API error.
 *
 * @example
 * showError({ text: 'No se pudo guardar la configuración.' });
 */
export async function showError({ title = 'Error', text }: ShowErrorOptions) {
  const Swal = await getSwal();
  return Swal.fire({
    icon: 'error',
    title,
    text,
    confirmButtonColor: CONFIRM_COLOR,
  });
}

// ── Warning alert ─────────────────────────────────────────────────────────────

interface ShowWarningOptions {
  title: string;
  text: string;
}

/**
 * Shows a warning alert (non-destructive — no cancel button).
 *
 * @example
 * showWarning({ title: 'Acción no permitida', text: 'No puedes eliminar tu propia cuenta.' });
 */
export async function showWarning({ title, text }: ShowWarningOptions) {
  const Swal = await getSwal();
  return Swal.fire({
    icon: 'warning',
    title,
    text,
    confirmButtonColor: CONFIRM_COLOR,
  });
}

// ── Confirm dialog ────────────────────────────────────────────────────────────

interface ShowConfirmOptions {
  title: string;
  text?: string;
  /** HTML content for the dialog body (use instead of `text` when markup is needed). */
  html?: string;
  confirmText?: string;
  cancelText?: string;
  /** When true the confirm button is red (for destructive actions like delete). */
  danger?: boolean;
  /** SweetAlert2 icon. Defaults to 'warning'. */
  icon?: 'warning' | 'question' | 'info';
}

/**
 * Shows a confirm dialog with confirm + cancel buttons.
 * Returns `true` if the user confirmed, `false` otherwise.
 *
 * @example
 * const ok = await showConfirm({
 *   title: '¿Estás seguro?',
 *   text: 'Se eliminará el usuario "admin"',
 *   danger: true,
 * });
 * if (ok) { … }
 */
export async function showConfirm({
  title,
  text,
  html,
  confirmText = 'Sí, continuar',
  cancelText = 'Cancelar',
  danger = false,
  icon = 'warning',
}: ShowConfirmOptions): Promise<boolean> {
  const Swal = await getSwal();
  const result = await Swal.fire({
    title,
    text,
    html,
    icon,
    showCancelButton: true,
    confirmButtonColor: danger ? DANGER_COLOR : CONFIRM_COLOR,
    cancelButtonColor: CANCEL_COLOR,
    confirmButtonText: confirmText,
    cancelButtonText: cancelText,
  });
  return result.isConfirmed;
}

// ── Text input dialog ─────────────────────────────────────────────────────────

interface ShowInputOptions {
  title: string;
  label?: string;
  initialValue?: string;
  confirmText?: string;
  cancelText?: string;
  required?: boolean;
}

/**
 * Shows a text-input dialog and returns the entered value, or `null` if cancelled.
 *
 * @example
 * const name = await showInput({ title: 'Duplicar rutina', label: 'Nombre para la copia', initialValue: 'Copia de ...' });
 * if (name) { ... }
 */
export async function showInput({
  title,
  label,
  initialValue = '',
  confirmText = 'Aceptar',
  cancelText = 'Cancelar',
  required = true,
}: ShowInputOptions): Promise<string | null> {
  const Swal = await getSwal();
  const { value } = await Swal.fire({
    title,
    input: 'text',
    ...(label !== undefined && { inputLabel: label }),
    inputValue: initialValue,
    showCancelButton: true,
    confirmButtonColor: CONFIRM_COLOR,
    cancelButtonColor: CANCEL_COLOR,
    confirmButtonText: confirmText,
    cancelButtonText: cancelText,
    ...(required && { inputValidator: (v: string) => (!v ? 'Este campo es requerido' : null) }),
  });
  return value ?? null;
}
