import React, { useEffect, useRef } from 'react';
import {
  Modal,
  View,
  StyleSheet,
  Animated,
  Dimensions,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  Pressable,
} from 'react-native';
import { semanticColors, spacing, borderRadius, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';

// Importación condicional para evitar errores en web
let GestureHandlerRootView: any;
let PanGestureHandler: any;
let State: any;

if (Platform.OS !== 'web') {
  try {
    const gestureHandler = require('react-native-gesture-handler');
    GestureHandlerRootView = gestureHandler.GestureHandlerRootView;
    PanGestureHandler = gestureHandler.PanGestureHandler;
    State = gestureHandler.State;
  } catch (error) {
    console.warn('react-native-gesture-handler not available');
  }
}

const { height: SCREEN_HEIGHT } = Dimensions.get('window');

export interface BottomSheetProps {
  isVisible: boolean;
  onClose: () => void;
  children: React.ReactNode;
  title?: string;
  height?: 'auto' | 'half' | 'full' | number;
  scrollable?: boolean;
  footer?: React.ReactNode;
  showHandle?: boolean;
  closeOnBackdrop?: boolean;
  testID?: string;
}

export function BottomSheet({
  isVisible,
  onClose,
  children,
  title,
  height = 'auto',
  scrollable = true,
  footer,
  showHandle = true,
  closeOnBackdrop = true,
  testID,
}: BottomSheetProps): React.ReactElement {
  const translateY = useRef(new Animated.Value(SCREEN_HEIGHT)).current;

  const getSheetHeight = () => {
    switch (height) {
      case 'half':
        return SCREEN_HEIGHT * 0.5;
      case 'full':
        return SCREEN_HEIGHT * 0.95;
      case 'auto':
        return 'auto'; // Permitir que el contenido determine la altura
      default:
        if (typeof height === 'number') {
          // Si es un número entre 0 y 1, tratarlo como porcentaje
          if (height > 0 && height <= 1) {
            return SCREEN_HEIGHT * height;
          }
          // Si es mayor a 1, tratarlo como píxeles
          return height;
        }
        return SCREEN_HEIGHT * 0.6;
    }
  };

  useEffect(() => {
    if (isVisible) {
      // Animar entrada desde abajo
      Animated.spring(translateY, {
        toValue: 0,
        useNativeDriver: true,
        tension: 100,
        friction: 8,
      }).start();
    } else {
      // Animar salida hacia abajo
      Animated.timing(translateY, {
        toValue: SCREEN_HEIGHT,
        duration: 250,
        useNativeDriver: true,
      }).start();
    }
  }, [isVisible, translateY]);

  const onGestureEvent = Platform.OS !== 'web' && PanGestureHandler 
    ? Animated.event(
        [{ nativeEvent: { translationY: translateY } }],
        { useNativeDriver: true }
      )
    : undefined;

  const onHandlerStateChange = (event: any) => {
    if (Platform.OS === 'web' || !State) return;
    
    if (event.nativeEvent.oldState === State.ACTIVE) {
      let { translationY } = event.nativeEvent;
      const { velocityY } = event.nativeEvent;

      // Limitar el movimiento hacia arriba
      translationY = Math.max(translationY, 0);

      if (velocityY > 500 || translationY > SCREEN_HEIGHT / 3) {
        onClose();
      } else {
        Animated.spring(translateY, {
          toValue: 0,
          useNativeDriver: true,
        }).start();
      }
    }
  };

  const handleBackdropPress = () => {
    if (closeOnBackdrop) {
      onClose();
    }
  };

  const renderContent = () => (
    <Animated.View
      style={[
        styles.bottomSheet,
        {
          height: getSheetHeight(),
          transform: [{ translateY }],
        },
      ]}
    >
      {/* Header con PanGestureHandler solo en esta área */}
      {Platform.OS !== 'web' && GestureHandlerRootView && PanGestureHandler ? (
        <PanGestureHandler
          onGestureEvent={onGestureEvent}
          onHandlerStateChange={onHandlerStateChange}
        >
          <View style={styles.header}>
            <View style={styles.handle} />
            {title && (
              <View style={styles.titleContainer}>
                <Text variant="h3" color={semanticColors.textPrimary} style={styles.title}>
                  {title}
                </Text>
                <Pressable onPress={onClose} style={styles.closeButton}>
                  <Icon name="close" size={24} color={semanticColors.textSecondary} />
                </Pressable>
              </View>
            )}
          </View>
        </PanGestureHandler>
      ) : (
        <View style={styles.header}>
          <View style={styles.handle} />
          {title && (
            <View style={styles.titleContainer}>
              <Text variant="h3" color={semanticColors.textPrimary} style={styles.title}>
                {title}
              </Text>
              <Pressable onPress={onClose} style={styles.closeButton}>
                <Icon name="close" size={24} color={semanticColors.textSecondary} />
              </Pressable>
            </View>
          )}
        </View>
      )}

      {/* Content */}
      {scrollable ? (
        <ScrollView
          style={styles.content}
          showsVerticalScrollIndicator={false}
          keyboardShouldPersistTaps="handled"
        >
          {children}
        </ScrollView>
      ) : (
        <View style={styles.content}>
          {children}
        </View>
      )}

      {/* Footer */}
      {footer && (
        <View style={styles.footer}>
          {footer}
        </View>
      )}
    </Animated.View>
  );

  return (
    <Modal
      visible={isVisible}
      transparent
      animationType="none"
      onRequestClose={onClose}
    >
      {Platform.OS !== 'web' && GestureHandlerRootView ? (
        <GestureHandlerRootView style={styles.overlay}>
          <Pressable style={styles.backdrop} onPress={handleBackdropPress} />
          <KeyboardAvoidingView
            behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
            style={styles.keyboardAvoidingView}
          >
            {renderContent()}
          </KeyboardAvoidingView>
        </GestureHandlerRootView>
      ) : (
        <View style={styles.overlay}>
          <Pressable style={styles.backdrop} onPress={handleBackdropPress} />
          <KeyboardAvoidingView
            behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
            style={styles.keyboardAvoidingView}
          >
            {renderContent()}
          </KeyboardAvoidingView>
        </View>
      )}
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'flex-end',
  },
  backdrop: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
  },
  keyboardAvoidingView: {
    flex: 1,
    justifyContent: 'flex-end',
  },
  bottomSheet: {
    backgroundColor: semanticColors.background,
    borderTopLeftRadius: borderRadius.lg,
    borderTopRightRadius: borderRadius.lg,
    paddingBottom: Platform.OS === 'ios' ? 34 : spacing.md,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: -2,
    },
    shadowOpacity: 0.25,
    shadowRadius: 3.84,
    elevation: 5,
    maxHeight: SCREEN_HEIGHT * 0.95,
    minHeight: 200,
  },
  header: {
    paddingTop: spacing.md,
    paddingHorizontal: spacing.md,
    paddingBottom: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.border,
  },
  handle: {
    width: 40,
    height: 4,
    backgroundColor: semanticColors.border,
    borderRadius: 2,
    alignSelf: 'center',
    marginBottom: spacing.sm,
  },
  titleContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  title: {
    flex: 1,
    fontWeight: typography.fontWeight.bold as any,
  },
  closeButton: {
    padding: spacing.xs,
    marginLeft: spacing.sm,
  },
  content: {
    flex: 1,
    paddingHorizontal: spacing.md,
    paddingTop: spacing.md,
  },
  footer: {
    paddingHorizontal: spacing.md,
    paddingTop: spacing.md,
    borderTopWidth: 1,
    borderTopColor: semanticColors.border,
  },
});