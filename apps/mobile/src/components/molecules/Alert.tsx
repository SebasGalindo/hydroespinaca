import React from 'react';
import { View, ViewStyle } from 'react-native';
import { semanticColors, spacing, borderRadius, IconName } from '@hydroespinaca/shared';
import { Text, IconButton, Icon } from '../atoms';

export interface AlertProps {
  type?: 'info' | 'success' | 'warning' | 'error';
  title?: string;
  message: string;
  onClose?: () => void;
  closable?: boolean;
  style?: ViewStyle;
  testID?: string;
}

const alertStyles: Record<string, {
  backgroundColor: string;
  borderColor: string;
  textColor: string;
  icon: IconName;
}> = {
  info: {
    backgroundColor: semanticColors.infoBg,
    borderColor: semanticColors.infoText,
    textColor: semanticColors.infoText,
    icon: 'info' as IconName,
  },
  success: {
    backgroundColor: semanticColors.successBg,
    borderColor: semanticColors.successText,
    textColor: semanticColors.successText,
    icon: 'check' as IconName,
  },
  warning: {
    backgroundColor: semanticColors.warningBg,
    borderColor: semanticColors.warningText,
    textColor: semanticColors.warningText,
    icon: 'warning' as IconName,
  },
  error: {
    backgroundColor: semanticColors.errorBg,
    borderColor: semanticColors.errorText,
    textColor: semanticColors.errorText,
    icon: 'error' as IconName,
  },
};

export function Alert({
  type = 'info',
  title,
  message,
  onClose,
  closable = false,
  style,
  testID,
}: AlertProps): React.ReactElement {
  const alertStyle = (alertStyles[type] || alertStyles.info) as NonNullable<typeof alertStyles[keyof typeof alertStyles]>;

  const containerStyle: ViewStyle = {
    flexDirection: 'row',
    alignItems: 'flex-start',
    backgroundColor: alertStyle.backgroundColor,
    borderLeftWidth: 4,
    borderLeftColor: alertStyle.borderColor,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    margin: spacing.sm,
  };

  return (
    <View style={[containerStyle, style]} testID={testID}>
      {/* Icono del tipo de alerta */}
      <View style={{ marginRight: spacing.sm, marginTop: 2 }}>
        <Icon 
          name={alertStyle.icon}
          size={20}
          color={alertStyle.textColor}
        />
      </View>

      {/* Contenido */}
      <View style={{ flex: 1 }}>
        {title && (
          <Text
            variant="bodyLarge"
            weight="semibold"
            color={alertStyle.textColor}
            style={{ marginBottom: title ? spacing.xs : 0 }}
          >
            {title}
          </Text>
        )}
        <Text
          variant="body"
          color={alertStyle.textColor}
        >
          {message}
        </Text>
      </View>

      {/* Botón de cerrar */}
      {(closable || onClose) && (
        <View style={{ marginLeft: spacing.sm }}>
          <IconButton
            icon="close"
            size="sm"
            color={alertStyle.textColor}
            onPress={onClose || (() => {})}
            accessibilityLabel="Cerrar alerta"
          />
        </View>
      )}
    </View>
  );
}