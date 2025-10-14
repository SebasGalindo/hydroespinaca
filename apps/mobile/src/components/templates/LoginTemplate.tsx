import React from 'react';
import { View, StyleSheet, SafeAreaView, ImageBackground } from 'react-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';
import { LoginForm } from '../organisms/LoginForm';

interface LoginTemplateProps {
  onLoginSuccess?: (email: string, password: string) => void;
  onForgotPassword?: () => void;
  onSignUp?: () => void;
  backgroundImage?: string;
  testID?: string;
}

export function LoginTemplate({
  onLoginSuccess,
  onForgotPassword,
  onSignUp,
  backgroundImage = 'https://images.unsplash.com/photo-1416879595882-3373a0480b5b?w=800&q=80',
  testID = 'login-template'
}: LoginTemplateProps): React.ReactElement {
  return (
    <SafeAreaView style={styles.container} testID={testID}>
      <ImageBackground
        source={{ uri: backgroundImage }}
        style={styles.backgroundImage}
        resizeMode="cover"
      >
        {/* Overlay opaco */}
        <View style={styles.overlay} />
        
        {/* Contenido del login */}
        <View style={styles.content}>
          <LoginForm
          {...(onLoginSuccess && { onLoginSuccess })}
          {...(onForgotPassword && { onForgotPassword })}
          {...(onSignUp && { onSignUp })}
          testID={`${testID}-login-form`}
        />
        </View>
      </ImageBackground>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.background,
  },
  backgroundImage: {
    flex: 1,
    width: '100%',
    height: '100%',
  },
  overlay: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: 'rgba(0, 0, 0, 0.5)', // Overlay opaco
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.lg,
    paddingHorizontal: spacing.xl,
  },
});