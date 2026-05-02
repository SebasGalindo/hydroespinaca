import { registerRootComponent } from 'expo';
import RNEventSource from 'react-native-sse';

import App from './App';

if (typeof (globalThis as { EventSource?: unknown }).EventSource === 'undefined') {
  (globalThis as { EventSource?: unknown }).EventSource = RNEventSource;
}

// registerRootComponent calls AppRegistry.registerComponent('main', () => App);
// It also ensures that whether you load the app in Expo Go or in a native build,
// the environment is set up appropriately
registerRootComponent(App);
