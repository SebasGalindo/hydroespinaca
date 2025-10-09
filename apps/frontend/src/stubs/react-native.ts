// React Native stub for web builds
// This file replaces react-native imports when building for web

export const View = 'div';
export const Text = 'span';
export const TextInput = 'input';
export const TouchableOpacity = 'button';
export const ScrollView = 'div';
export const ActivityIndicator = 'div';
export const Alert = {
  alert: () => window.alert,
};
export const Platform = {
  OS: 'web' as const,
  select: (config: { web?: any; native?: any; ios?: any; android?: any }) => config.web,
};

// Styles
export const StyleSheet = {
  create: (styles: any) => styles,
};

// Common style types - return empty objects/functions
export interface ViewStyle {}
export interface TextStyle {}
export interface ImageStyle {}

// Dimensions
export const Dimensions = {
  get: () => ({
    width: typeof window !== 'undefined' ? window.innerWidth : 0,
    height: typeof window !== 'undefined' ? window.innerHeight : 0,
  }),
};

// Default export (in case something imports React Native as default)
export default {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  ScrollView,
  ActivityIndicator,
  Alert,
  Platform,
  StyleSheet,
  Dimensions,
};