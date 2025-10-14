export interface WelcomeMessageProps {
  userName?: string;
  appName?: string;
}

export const getWelcomeMessage = ({ userName, appName = 'Hidroespinaca' }: WelcomeMessageProps = {}) => {
  if (userName) {
    return `¡Bienvenido/a ${userName} a ${appName}!`;
  }
  return `¡Bienvenido/a a ${appName}!`;
};

export const getAppInfo = () => {
  return {
    name: 'Hidroespinaca',
    version: '1.0.0',
    description: 'Sistema de monitoreo hidropónico inteligente'
  };
};