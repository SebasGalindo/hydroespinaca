/**
 * Sistema de notificaciones toast para la app.
 * Usa un store simple para manejar toasts desde cualquier lugar.
 */

type ToastType = 'success' | 'error' | 'warning' | 'info';

interface Toast {
  id: string;
  type: ToastType;
  message: string;
  duration?: number;
}

type ToastListener = (toast: Toast | null) => void;

let currentToast: Toast | null = null;
let timeoutId: ReturnType<typeof setTimeout> | null = null;
const listeners: Set<ToastListener> = new Set();

const notifyListeners = () => {
  listeners.forEach((listener) => listener(currentToast));
};

/** Muestra un toast con un mensaje y tipo */
export const showToast = (type: ToastType, message: string, duration = 3000) => {
  if (timeoutId) clearTimeout(timeoutId);

  currentToast = {
    id: Date.now().toString(),
    type,
    message,
    duration,
  };
  notifyListeners();

  timeoutId = setTimeout(() => {
    currentToast = null;
    notifyListeners();
    timeoutId = null;
  }, duration);
};

/** Oculta el toast actual */
export const hideToast = () => {
  if (timeoutId) clearTimeout(timeoutId);
  currentToast = null;
  notifyListeners();
};

/** Suscribe un listener a cambios de toast. Retorna función de unsuscribe */
export const subscribeToast = (listener: ToastListener): (() => void) => {
  listeners.add(listener);
  return () => listeners.delete(listener);
};

/** Obtiene el toast actual */
export const getCurrentToast = () => currentToast;
