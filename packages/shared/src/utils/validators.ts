/**
 * Valida un correo electrónico
 * @param email - Correo electrónico a validar
 * @returns true si el correo es válido, false en caso contrario
 */
export const isValidEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

/**
 * Valida una contraseña según criterios de seguridad
 * @param password - Contraseña a validar
 * @returns Objeto con resultado y mensaje de error si aplica
 */
export const validatePassword = (password: string): { isValid: boolean; message?: string } => {
  if (password.length < 8) {
    return { isValid: false, message: 'La contraseña debe tener al menos 8 caracteres' };
  }
  
  if (!/[A-Z]/.test(password)) {
    return { isValid: false, message: 'La contraseña debe contener al menos una letra mayúscula' };
  }
  
  if (!/[a-z]/.test(password)) {
    return { isValid: false, message: 'La contraseña debe contener al menos una letra minúscula' };
  }
  
  if (!/[0-9]/.test(password)) {
    return { isValid: false, message: 'La contraseña debe contener al menos un número' };
  }
  
  return { isValid: true };
};

/**
 * Valida un valor de sensor según su tipo
 * @param value - Valor del sensor
 * @param sensorType - Tipo de sensor
 * @returns true si el valor es válido para ese tipo de sensor, false en caso contrario
 */
export const isValidSensorValue = (
  value: number,
  sensorType: 'ph' | 'temperature' | 'humidity' | 'nutrient' | 'light'
): boolean => {
  switch (sensorType) {
    case 'ph':
      return value >= 0 && value <= 14;
    case 'temperature':
      return value >= -10 && value <= 50; // Rango típico para cultivos
    case 'humidity':
      return value >= 0 && value <= 100; // Porcentaje
    case 'nutrient':
      return value >= 0 && value <= 5000; // ppm típico
    case 'light':
      return value >= 0 && value <= 100000; // lux típico
    default:
      return false;
  }
};

/**
 * Valida si una fecha es futura
 * @param dateString - Fecha en formato string
 * @returns true si la fecha es futura, false en caso contrario
 */
export const isFutureDate = (dateString: string): boolean => {
  const date = new Date(dateString);
  const now = new Date();
  return date > now;
};

/**
 * Valida las credenciales de login
 * @param email - Correo electrónico del usuario
 * @param password - Contraseña del usuario
 * @returns Objeto con resultado de validación y mensaje de error si aplica
 */
export const validateLoginCredentials = (
  email: string,
  password: string
): { isValid: boolean; message?: string } => {
  // Validar que los campos no estén vacíos
  if (!email || email.trim() === '') {
    return { isValid: false, message: 'El correo electrónico es requerido' };
  }
  
  if (!password || password.trim() === '') {
    return { isValid: false, message: 'La contraseña es requerida' };
  }
  
  // Validar formato de email
  if (!isValidEmail(email)) {
    return { isValid: false, message: 'El formato del correo electrónico no es válido' };
  }
  
  // Por ahora, solo validamos que no estén vacíos y el formato del email
  // En el futuro aquí se podría agregar validación contra el servidor
  return { isValid: true };
};