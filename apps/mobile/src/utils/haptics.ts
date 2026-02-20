/**
 * Utilidades de feedback háptico para la app.
 * Wrappea expo-haptics con funciones semánticas.
 */
import * as Haptics from 'expo-haptics';

/** Feedback ligero para taps, selecciones */
export const hapticLight = () => {
  Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light);
};

/** Feedback medio para acciones confirmadas */
export const hapticMedium = () => {
  Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium);
};

/** Feedback pesado para acciones destructivas */
export const hapticHeavy = () => {
  Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Heavy);
};

/** Feedback de éxito (notificación) */
export const hapticSuccess = () => {
  Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
};

/** Feedback de error (notificación) */
export const hapticError = () => {
  Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error);
};

/** Feedback de advertencia (notificación) */
export const hapticWarning = () => {
  Haptics.notificationAsync(Haptics.NotificationFeedbackType.Warning);
};

/** Feedback de selección (leve, para pickers/switches) */
export const hapticSelection = () => {
  Haptics.selectionAsync();
};
