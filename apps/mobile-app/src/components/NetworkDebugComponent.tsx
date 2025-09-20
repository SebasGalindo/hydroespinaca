import React, { useState, useEffect } from 'react';
import { View, Text, TouchableOpacity, StyleSheet, ScrollView } from 'react-native';
import { getApiUrl, SessionStorage } from '@hydroespinaca/shared-utils';

export const NetworkDebugComponent: React.FC = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<string>('');
  const [storageInfo, setStorageInfo] = useState<string>('Loading storage info...');

  // Check storage availability on component mount
  useEffect(() => {
    checkStorageAvailability();
  }, []);

  const checkStorageAvailability = async () => {
    try {
      let info = '📱 Storage Status:\n';
      
      // Test SecureStore availability
      try {
        const SecureStore = require('expo-secure-store');
        if (SecureStore.isAvailableAsync) {
          const isAvailable = await SecureStore.isAvailableAsync();
          info += `✅ SecureStore: ${isAvailable ? 'Available' : 'Not Available'}\n`;
          
          if (isAvailable) {
            // Test basic functionality
            await SecureStore.setItemAsync('test_key', 'test_value');
            const testValue = await SecureStore.getItemAsync('test_key');
            info += `✅ SecureStore Test: ${testValue === 'test_value' ? 'Working' : 'Failed'}\n`;
            await SecureStore.deleteItemAsync('test_key');
          }
        } else {
          info += `❌ SecureStore: No isAvailableAsync method\n`;
        }
      } catch (error) {
        info += `❌ SecureStore: Not loaded (${error.message})\n`;
      }

      // Test AsyncStorage availability
      try {
        const AsyncStorage = require('@react-native-async-storage/async-storage');
        await AsyncStorage.setItem('test_async_key', 'test_async_value');
        const asyncTestValue = await AsyncStorage.getItem('test_async_key');
        info += `✅ AsyncStorage: ${asyncTestValue === 'test_async_value' ? 'Working' : 'Failed'}\n`;
        await AsyncStorage.removeItem('test_async_key');
      } catch (error) {
        info += `❌ AsyncStorage: Not available (${error.message})\n`;
      }

      setStorageInfo(info);
    } catch (error) {
      setStorageInfo(`❌ Storage check failed: ${error.message}`);
    }
  };

  const testConnection = async () => {
    setIsLoading(true);
    setResult('');
    
    try {
      const apiUrl = getApiUrl();
      console.log('Testing connection to:', apiUrl);
      
      const response = await fetch(`${apiUrl}/health`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });
      
      const data = await response.text();
      setResult(`✅ Connection successful!\nStatus: ${response.status}\nResponse: ${data}`);
    } catch (error) {
      console.error('Connection test failed:', error);
      setResult(`❌ Connection failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setIsLoading(false);
    }
  };

  const testLogin = async () => {
    setIsLoading(true);
    setResult('');
    
    try {
      const apiUrl = getApiUrl();
      console.log('Testing login to:', apiUrl);
      
      const response = await fetch(`${apiUrl}/auth/login/mobile`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          Email: 'admin@demo.com',
          Password: 'Admin123!'
        })
      });
      
      const data = await response.text();
      let resultText = `Login test result:\nStatus: ${response.status}\nResponse: ${data}`;
      
      // Si el login fue exitoso, guardar los tokens
      if (response.ok && data) {
        try {
          const loginResponse = JSON.parse(data);
          if (loginResponse.sessionId && loginResponse.csrfToken) {
            await SessionStorage.storeSession(loginResponse.sessionId, loginResponse.csrfToken);
            resultText += '\n\n✅ Tokens stored successfully';
            console.log('Tokens stored successfully');
          }
        } catch (e) {
          resultText += '\n\n❌ Failed to parse/store tokens';
          console.error('Failed to parse login response:', e);
        }
      }
      
      setResult(resultText);
    } catch (error) {
      console.error('Login test failed:', error);
      setResult(`❌ Login test failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setIsLoading(false);
    }
  };

  const testSession = async () => {
    setIsLoading(true);
    setResult('');
    
    try {
      const apiUrl = getApiUrl();
      
      // Obtener tokens del storage
      const sessionId = await SessionStorage.getSessionId();
      const csrfToken = await SessionStorage.getCsrfToken();
      
      console.log('Stored sessionId:', sessionId);
      console.log('Stored csrfToken:', csrfToken);
      
      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
      };
      
      if (sessionId) {
        headers['X-Session-Id'] = sessionId;
      }
      if (csrfToken) {
        headers['X-CSRF-Token'] = csrfToken;
      }
      
      console.log('Sending headers:', headers);
      
      const response = await fetch(`${apiUrl}/auth/session`, {
        method: 'GET',
        headers
      });
      
      const data = await response.text();
      setResult(`Session test result:\nStored sessionId: ${sessionId || 'null'}\nStored csrfToken: ${csrfToken || 'null'}\n\nHeaders sent: ${JSON.stringify(headers, null, 2)}\n\nStatus: ${response.status}\nResponse: ${data}`);
    } catch (error) {
      console.error('Session test failed:', error);
      setResult(`❌ Session test failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setIsLoading(false);
    }
  };

  const checkStorage = async () => {
    setIsLoading(true);
    setResult('');
    
    try {
      const sessionId = await SessionStorage.getSessionId();
      const csrfToken = await SessionStorage.getCsrfToken();
      
      setResult(`Storage contents:\nSessionId: ${sessionId || 'null'}\nCsrfToken: ${csrfToken || 'null'}`);
    } catch (error) {
      console.error('Storage check failed:', error);
      setResult(`❌ Storage check failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setIsLoading(false);
    }
  };

  const clearStorage = async () => {
    setIsLoading(true);
    setResult('');
    
    try {
      await SessionStorage.clearSession();
      setResult('✅ Storage cleared successfully');
    } catch (error) {
      console.error('Storage clear failed:', error);
      setResult(`❌ Storage clear failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>Network & Storage Debug</Text>
      <Text style={styles.subtitle}>API URL: {getApiUrl()}</Text>
      
      {/* Storage Status Display */}
      <View style={styles.statusContainer}>
        <Text style={styles.statusText}>{storageInfo}</Text>
      </View>
      
      <TouchableOpacity 
        style={[styles.button, styles.buttonInfo, isLoading && styles.buttonDisabled]} 
        onPress={checkStorageAvailability}
        disabled={isLoading}
      >
        <Text style={styles.buttonText}>🔄 Refresh Storage Status</Text>
      </TouchableOpacity>
      
      <TouchableOpacity 
        style={[styles.button, isLoading && styles.buttonDisabled]} 
        onPress={testConnection}
        disabled={isLoading}
      >
        <Text style={styles.buttonText}>1. Test Connection</Text>
      </TouchableOpacity>
      
      <TouchableOpacity 
        style={[styles.button, isLoading && styles.buttonDisabled]} 
        onPress={testLogin}
        disabled={isLoading}
      >
        <Text style={styles.buttonText}>2. Test Login + Store Tokens</Text>
      </TouchableOpacity>
      
      <TouchableOpacity 
        style={[styles.button, isLoading && styles.buttonDisabled]} 
        onPress={checkStorage}
        disabled={isLoading}
      >
        <Text style={styles.buttonText}>3. Check Storage</Text>
      </TouchableOpacity>
      
      <TouchableOpacity 
        style={[styles.button, isLoading && styles.buttonDisabled]} 
        onPress={testSession}
        disabled={isLoading}
      >
        <Text style={styles.buttonText}>4. Test Session (with headers)</Text>
      </TouchableOpacity>
      
      <TouchableOpacity 
        style={[styles.button, styles.buttonDanger, isLoading && styles.buttonDisabled]} 
        onPress={clearStorage}
        disabled={isLoading}
      >
        <Text style={styles.buttonText}>Clear Storage</Text>
      </TouchableOpacity>
      
      {result ? (
        <View style={styles.resultContainer}>
          <ScrollView style={styles.resultScroll}>
            <Text style={styles.resultText}>{result}</Text>
          </ScrollView>
        </View>
      ) : null}
      
      {isLoading && <Text style={styles.loading}>Testing...</Text>}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    padding: 20,
    backgroundColor: '#f5f5f5',
  },
  title: {
    fontSize: 18,
    fontWeight: 'bold',
    marginBottom: 5,
  },
  subtitle: {
    fontSize: 12,
    color: '#666',
    marginBottom: 10,
  },
  statusContainer: {
    backgroundColor: '#e8f5e8',
    padding: 15,
    borderRadius: 8,
    marginBottom: 15,
    borderLeftWidth: 4,
    borderLeftColor: '#4caf50',
  },
  statusText: {
    fontSize: 12,
    fontFamily: 'monospace',
    color: '#2e7d32',
    lineHeight: 18,
  },
  button: {
    backgroundColor: '#2E7D32',
    padding: 15,
    borderRadius: 8,
    marginBottom: 10,
    alignItems: 'center',
  },
  buttonInfo: {
    backgroundColor: '#1976d2',
  },
  buttonDanger: {
    backgroundColor: '#d32f2f',
  },
  buttonDisabled: {
    backgroundColor: '#ccc',
  },
  buttonText: {
    color: 'white',
    fontWeight: 'bold',
  },
  resultContainer: {
    backgroundColor: '#fff',
    padding: 15,
    borderRadius: 8,
    marginTop: 10,
    borderLeftWidth: 4,
    borderLeftColor: '#2E7D32',
    maxHeight: 300,
  },
  resultScroll: {
    maxHeight: 250,
  },
  resultText: {
    fontSize: 12,
    fontFamily: 'monospace',
    color: '#333',
  },
  loading: {
    textAlign: 'center',
    fontStyle: 'italic',
    color: '#666',
    marginTop: 10,
  },
});