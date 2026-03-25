/**
 * Formatea una fecha en formato legible
 * @param dateString - String de fecha ISO
 * @returns Fecha formateada
 */
export const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return new Intl.DateTimeFormat('es-ES', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    timeZone: 'UTC',
  }).format(date);
};

/**
 * Formatea un valor de sensor con su unidad
 * @param value - Valor numérico
 * @param unit - Unidad de medida
 * @returns Valor formateado con unidad
 */
export const formatSensorValue = (value: number, unit: string): string => {
  // Formatear según el tipo de unidad
  switch (unit) {
    case '°C':
    case '°F':
      return `${value.toFixed(1)}${unit}`;
    case 'pH':
      return `${value.toFixed(2)} ${unit}`;
    case '%':
      return `${value.toFixed(1)}${unit}`;
    case 'ppm':
    case 'EC':
      return `${value} ${unit}`;
    default:
      return `${value} ${unit}`;
  }
};

/**
 * Trunca un texto a una longitud máxima
 * @param text - Texto a truncar
 * @param maxLength - Longitud máxima
 * @returns Texto truncado
 */
export const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text;
  return `${text.substring(0, maxLength)}...`;
};

/**
 * Formatea un número como moneda
 * @param amount - Cantidad
 * @param currency - Código de moneda
 * @returns Cantidad formateada como moneda
 */
export const formatCurrency = (amount: number, currency: string = 'COP'): string => {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
  }).format(amount);
};

/**
 * Formatea un valor numérico a 2 decimales eliminando ceros innecesarios
 * @param value - Valor numérico
 * @returns Valor formateado como string
 * @example
 * formatNumericValue(23.50) // "23.5"
 * formatNumericValue(23.00) // "23"
 * formatNumericValue(23.456) // "23.46"
 */
export const formatNumericValue = (value: number): string => {
  // Formatear a 2 decimales y eliminar ceros innecesarios
  const formatted = parseFloat(value.toFixed(2));
  return formatted.toString();
};