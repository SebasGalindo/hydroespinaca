// Form validation utilities

export const validateEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

export const validatePassword = (password: string): { isValid: boolean; errors: string[] } => {
  const errors: string[] = [];
  
  if (password.length < 6) {
    errors.push('La contraseña debe tener al menos 6 caracteres');
  }
  
  if (!/[A-Z]/.test(password)) {
    errors.push('La contraseña debe contener al menos una letra mayúscula');
  }
  
  if (!/[a-z]/.test(password)) {
    errors.push('La contraseña debe contener al menos una letra minúscula');
  }
  
  if (!/[0-9]/.test(password)) {
    errors.push('La contraseña debe contener al menos un número');
  }
  
  return {
    isValid: errors.length === 0,
    errors,
  };
};

export const validateLoginForm = (email: string, password: string) => {
  const errors: Record<string, string> = {};
  
  if (!email.trim()) {
    errors.email = 'El email es requerido';
  } else if (!validateEmail(email)) {
    errors.email = 'Email inválido';
  }
  
  if (!password.trim()) {
    errors.password = 'La contraseña es requerida';
  }
  
  return {
    isValid: Object.keys(errors).length === 0,
    errors,
  };
};