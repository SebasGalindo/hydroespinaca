'use strict';

var zustand = require('zustand');
var React = require('react');
var jsxRuntime = require('react/jsx-runtime');

var _documentCurrentScript = typeof document !== 'undefined' ? document.currentScript : null;
// API Configuration utilities
/**
 * Detect the platform we're running on
 */
function detectPlatform() {
    // Check for React Native
    if (typeof navigator !== 'undefined' && navigator.product === 'ReactNative') {
        return 'mobile';
    }
    // Check for browser environment
    if (typeof window !== 'undefined' && typeof document !== 'undefined') {
        return 'web';
    }
    return 'unknown';
}
/**
 * Check if we're in development mode
 */
function isDevelopmentMode() {
    // Node environment
    if (typeof process !== 'undefined' && process.env) {
        const nodeEnv = process.env.NODE_ENV;
        if (nodeEnv === 'development' || nodeEnv === 'dev') {
            return true;
        }
    }
    // Vite environment variables
    if (typeof ({ url: (typeof document === 'undefined' ? require('u' + 'rl').pathToFileURL(__filename).href : (_documentCurrentScript && _documentCurrentScript.tagName.toUpperCase() === 'SCRIPT' && _documentCurrentScript.src || new URL('index.js', document.baseURI).href)) }) !== 'undefined' && undefined) {
        const mode = undefined.MODE || undefined.VITE_MODE;
        if (mode === 'development' || mode === 'dev') {
            return true;
        }
    }
    // Default to production
    return false;
}
/**
 * Get API URL from environment variables
 */
function getApiUrlFromEnv() {
    // Check Vite environment variables (web)
    if (typeof ({ url: (typeof document === 'undefined' ? require('u' + 'rl').pathToFileURL(__filename).href : (_documentCurrentScript && _documentCurrentScript.tagName.toUpperCase() === 'SCRIPT' && _documentCurrentScript.src || new URL('index.js', document.baseURI).href)) }) !== 'undefined' && undefined) {
        const viteApiUrl = undefined.VITE_API_URL;
        if (viteApiUrl) {
            return viteApiUrl;
        }
    }
    // Check Next.js environment variables (web)
    if (typeof process !== 'undefined' && process.env) {
        const nextApiUrl = process.env.NEXT_PUBLIC_API_URL;
        if (nextApiUrl) {
            return nextApiUrl;
        }
    }
    // Check Expo environment variables (mobile)
    if (typeof process !== 'undefined' && process.env) {
        const expoApiUrl = process.env.EXPO_PUBLIC_API_URL;
        if (expoApiUrl) {
            return expoApiUrl;
        }
    }
    return null;
}
/**
 * Get default API URL based on platform and environment
 */
function getDefaultApiUrl(platform, isDev) {
    if (platform === 'mobile') {
        // For mobile, use IP address that works for both Android emulator and physical devices
        return isDev
            ? 'http://localhost/api' // Development IP
            : 'https://api.hydroespinaca.online/api'; // Production
    }
    else if (platform === 'web') {
        return isDev
            ? 'http://localhost/api' // Development IP
            : 'https://api.hydroespinaca.online/api'; // Production
    }
    // Fallback to production URL
    return 'https://api.hydroespinaca.online/api';
}
/**
 * Get the API URL to use for requests
 * Priority: Environment Variables > Default based on platform/env
 */
function getApiUrl() {
    const platform = detectPlatform();
    const isDev = isDevelopmentMode();
    // Try to get from environment variables first
    const envApiUrl = getApiUrlFromEnv();
    if (envApiUrl) {
        return envApiUrl;
    }
    // Fall back to default
    return getDefaultApiUrl(platform, isDev);
}

// Secure Storage utilities for managing session tokens
/**
 * Detect if we're running in React Native
 */
const isReactNative = () => {
    return typeof navigator !== 'undefined' && navigator.product === 'ReactNative';
};
/**
 * Web storage implementation using localStorage
 */
const webStorage = {
    async getItem(key) {
        if (typeof window !== 'undefined' && window.localStorage) {
            return localStorage.getItem(key);
        }
        return null;
    },
    async setItem(key, value) {
        if (typeof window !== 'undefined' && window.localStorage) {
            localStorage.setItem(key, value);
        }
    },
    async removeItem(key) {
        if (typeof window !== 'undefined' && window.localStorage) {
            localStorage.removeItem(key);
        }
    }
};
/**
 * Mobile storage implementation using SecureStore or AsyncStorage
 * Priority: SecureStore > AsyncStorage > localStorage (fallback)
 */
const mobileStorage = {
    async getItem(key) {
        // Try expo-secure-store first (most secure)
        try {
            // Dynamically import to avoid errors in web
            const SecureStore = await import('expo-secure-store');
            // Check if SecureStore is available
            if (SecureStore && SecureStore.getItemAsync) {
                // Check availability on device
                if (SecureStore.isAvailableAsync) {
                    const isAvailable = await SecureStore.isAvailableAsync();
                    if (isAvailable) {
                        return await SecureStore.getItemAsync(key);
                    }
                }
                else {
                    // If isAvailableAsync doesn't exist, try to use it anyway
                    return await SecureStore.getItemAsync(key);
                }
            }
        }
        catch (error) {
            // SecureStore not available or error, continue to fallback
            console.warn('SecureStore not available, falling back to AsyncStorage');
        }
        // Try AsyncStorage as fallback
        try {
            const AsyncStorage = await import('@react-native-async-storage/async-storage');
            if (AsyncStorage && AsyncStorage.default && AsyncStorage.default.getItem) {
                return await AsyncStorage.default.getItem(key);
            }
        }
        catch (error) {
            // AsyncStorage not available, continue to fallback
            console.warn('AsyncStorage not available, falling back to localStorage');
        }
        // Final fallback to web localStorage
        return webStorage.getItem(key);
    },
    async setItem(key, value) {
        // Try expo-secure-store first
        try {
            const SecureStore = await import('expo-secure-store');
            if (SecureStore && SecureStore.setItemAsync) {
                if (SecureStore.isAvailableAsync) {
                    const isAvailable = await SecureStore.isAvailableAsync();
                    if (isAvailable) {
                        await SecureStore.setItemAsync(key, value);
                        return;
                    }
                }
                else {
                    await SecureStore.setItemAsync(key, value);
                    return;
                }
            }
        }
        catch (error) {
            console.warn('SecureStore not available, falling back to AsyncStorage');
        }
        // Try AsyncStorage as fallback
        try {
            const AsyncStorage = await import('@react-native-async-storage/async-storage');
            if (AsyncStorage && AsyncStorage.default && AsyncStorage.default.setItem) {
                await AsyncStorage.default.setItem(key, value);
                return;
            }
        }
        catch (error) {
            console.warn('AsyncStorage not available, falling back to localStorage');
        }
        // Final fallback to web localStorage
        await webStorage.setItem(key, value);
    },
    async removeItem(key) {
        // Try expo-secure-store first
        try {
            const SecureStore = await import('expo-secure-store');
            if (SecureStore && SecureStore.deleteItemAsync) {
                if (SecureStore.isAvailableAsync) {
                    const isAvailable = await SecureStore.isAvailableAsync();
                    if (isAvailable) {
                        await SecureStore.deleteItemAsync(key);
                        return;
                    }
                }
                else {
                    await SecureStore.deleteItemAsync(key);
                    return;
                }
            }
        }
        catch (error) {
            console.warn('SecureStore not available, falling back to AsyncStorage');
        }
        // Try AsyncStorage as fallback
        try {
            const AsyncStorage = await import('@react-native-async-storage/async-storage');
            if (AsyncStorage && AsyncStorage.default && AsyncStorage.default.removeItem) {
                await AsyncStorage.default.removeItem(key);
                return;
            }
        }
        catch (error) {
            console.warn('AsyncStorage not available, falling back to localStorage');
        }
        // Final fallback to web localStorage
        await webStorage.removeItem(key);
    }
};
/**
 * Secure storage instance that automatically uses the right implementation
 */
const secureStorage = isReactNative() ? mobileStorage : webStorage;
/**
 * Session storage helper functions
 */
const SessionStorage = {
    /**
     * Get the session ID
     */
    async getSessionId() {
        return await secureStorage.getItem('sessionId');
    },
    /**
     * Get the CSRF token
     */
    async getCsrfToken() {
        return await secureStorage.getItem('csrfToken');
    },
    /**
     * Store session credentials
     */
    async storeSession(sessionId, csrfToken) {
        await Promise.all([
            secureStorage.setItem('sessionId', sessionId),
            secureStorage.setItem('csrfToken', csrfToken)
        ]);
    },
    /**
     * Clear all session data
     */
    async clearSession() {
        await Promise.all([
            secureStorage.removeItem('sessionId'),
            secureStorage.removeItem('csrfToken')
        ]);
    }
};

// Authentication API Service
class ApiError extends Error {
    constructor(status, message) {
        super(message);
        this.status = status;
        this.name = 'ApiError';
    }
}
class AuthApiService {
    constructor(baseUrl) {
        // Use provided baseUrl or get from centralized config
        this.baseUrl = baseUrl || getApiUrl();
    }
    /**
     * Internal request method that handles platform-specific authentication
     */
    async request(url, options = {}, platform = 'web') {
        const fullUrl = `${this.baseUrl}${url}`;
        const defaultHeaders = {
            'Content-Type': 'application/json',
        };
        // For mobile: add session headers if they exist
        if (platform === 'mobile') {
            const sessionId = await SessionStorage.getSessionId();
            const csrfToken = await SessionStorage.getCsrfToken();
            if (sessionId) {
                defaultHeaders['X-Session-Id'] = sessionId;
            }
            if (csrfToken) {
                defaultHeaders['X-CSRF-Token'] = csrfToken;
            }
        }
        try {
            const response = await fetch(fullUrl, {
                ...options,
                headers: {
                    ...defaultHeaders,
                    ...options.headers,
                },
                // For web: include cookies automatically
                // For mobile: don't use cookies
                credentials: platform === 'web' ? 'include' : 'omit',
            });
            const isJson = response.headers.get('content-type')?.includes('application/json');
            const data = isJson ? await response.json() : null;
            if (!response.ok) {
                throw new ApiError(response.status, data?.message || `HTTP ${response.status}`);
            }
            return {
                data,
                status: response.status,
            };
        }
        catch (error) {
            if (error instanceof ApiError) {
                throw error;
            }
            throw new ApiError(0, error instanceof Error ? error.message : 'Network error');
        }
    }
    /**
     * Web login - uses HttpOnly cookies set by the server
     */
    async loginWeb(credentials) {
        await this.request('/auth/login/web', {
            method: 'POST',
            body: JSON.stringify(credentials),
        }, 'web');
    }
    /**
     * Mobile login - returns tokens in response body
     */
    async loginMobile(credentials) {
        const response = await this.request('/auth/login/mobile', {
            method: 'POST',
            body: JSON.stringify(credentials),
        }, 'mobile');
        if (!response.data) {
            throw new Error('No data in mobile login response');
        }
        if (!response.data.sessionId || !response.data.csrfToken) {
            throw new Error('Missing sessionId or csrfToken in mobile login response');
        }
        return response.data;
    }
    /**
     * Get current session information
     */
    async getCurrentSession(platform = 'web') {
        const response = await this.request('/auth/session', {
            method: 'GET',
        }, platform);
        if (!response.data) {
            throw new Error('No session data received');
        }
        return response.data;
    }
    /**
     * Logout - clear session on server
     */
    async logout(platform = 'web', sessionId) {
        const options = {
            method: 'POST',
        };
        // For mobile, send sessionId in body if provided
        if (platform === 'mobile' && sessionId) {
            options.body = JSON.stringify({ sessionId });
        }
        await this.request('/auth/logout', options, platform);
    }
    /**
     * Store session in secure storage (for mobile)
     */
    async storeSession(sessionId, csrfToken) {
        await SessionStorage.storeSession(sessionId, csrfToken);
    }
    /**
     * Clear session from secure storage (for mobile)
     */
    async clearSession() {
        await SessionStorage.clearSession();
    }
}
// Export a singleton instance
const authService$1 = new AuthApiService();

// Create API service instance
const authService = new AuthApiService();
const platform = detectPlatform();
const useAuthStore = zustand.create((set, get) => ({
    user: null,
    session: null,
    isAuthenticated: false,
    isLoading: false,
    error: null,
    login: async (email, password) => {
        set({ isLoading: true, error: null });
        try {
            const credentials = { Email: email, Password: password };
            if (platform === 'web') {
                // Web: cookies are set automatically by server
                await authService.loginWeb(credentials);
                // Get user session data
                const userSession = await authService.getCurrentSession('web');
                const session = {
                    sessionId: null, // HttpOnly cookie
                    csrfToken: null, // Cookie
                    userId: userSession.userId,
                    userRole: userSession.userRole,
                };
                const user = {
                    id: userSession.userId,
                    email,
                    name: email.split('@')[0], // Use email prefix as name temporarily
                    role: userSession.userRole,
                };
                set({
                    user,
                    session,
                    isAuthenticated: true,
                    isLoading: false,
                    error: null,
                });
            }
            else {
                // Mobile: get tokens from response and store them
                const mobileResponse = await authService.loginMobile(credentials);
                // Store tokens in secure storage
                await SessionStorage.storeSession(mobileResponse.sessionId, mobileResponse.csrfToken);
                // Get user session data
                const userSession = await authService.getCurrentSession('mobile');
                const session = {
                    sessionId: mobileResponse.sessionId,
                    csrfToken: mobileResponse.csrfToken,
                    userId: userSession.userId,
                    userRole: userSession.userRole,
                };
                const user = {
                    id: userSession.userId,
                    email,
                    name: email.split('@')[0], // Use email prefix as name temporarily
                    role: userSession.userRole,
                };
                set({
                    user,
                    session,
                    isAuthenticated: true,
                    isLoading: false,
                    error: null,
                });
            }
        }
        catch (error) {
            let errorMessage = 'Error al iniciar sesión';
            if (error instanceof ApiError) {
                if (error.status === 401) {
                    errorMessage = 'Credenciales inválidas';
                }
                else if (error.status === 0) {
                    errorMessage = 'Error de conexión con el servidor';
                }
                else {
                    errorMessage = error.message;
                }
            }
            else if (error instanceof Error) {
                errorMessage = error.message;
            }
            set({
                user: null,
                session: null,
                isAuthenticated: false,
                isLoading: false,
                error: errorMessage,
            });
        }
    },
    logout: async () => {
        set({ isLoading: true });
        try {
            const currentSession = get().session;
            const sessionId = platform === 'mobile' && currentSession
                ? currentSession.sessionId || undefined
                : undefined;
            // Call logout endpoint
            await authService.logout(platform === 'web' ? 'web' : 'mobile', sessionId);
            // For mobile, clear stored tokens
            if (platform === 'mobile') {
                await SessionStorage.clearSession();
            }
            set({
                user: null,
                session: null,
                isAuthenticated: false,
                isLoading: false,
                error: null,
            });
        }
        catch (error) {
            // Even if logout fails on server, clear local session
            if (platform === 'mobile') {
                await SessionStorage.clearSession();
            }
            set({
                user: null,
                session: null,
                isAuthenticated: false,
                isLoading: false,
                error: null,
            });
        }
    },
    clearError: () => {
        set({ error: null });
    },
    // Check if there's an existing session
    checkSession: async () => {
        set({ isLoading: true });
        try {
            // Try to get current session from server
            const userSession = await authService.getCurrentSession(platform === 'web' ? 'web' : 'mobile');
            // For mobile, get tokens from storage
            const sessionId = platform === 'mobile'
                ? await SessionStorage.getSessionId()
                : null;
            const csrfToken = platform === 'mobile'
                ? await SessionStorage.getCsrfToken()
                : null;
            const session = {
                sessionId,
                csrfToken,
                userId: userSession.userId,
                userRole: userSession.userRole,
            };
            const user = {
                id: userSession.userId,
                email: '', // We don't have email from session endpoint
                name: userSession.userId, // Use userId as name temporarily
                role: userSession.userRole,
            };
            set({
                user,
                session,
                isAuthenticated: true,
                isLoading: false,
                error: null,
            });
        }
        catch (error) {
            // No valid session found
            set({
                user: null,
                session: null,
                isAuthenticated: false,
                isLoading: false,
                error: null,
            });
        }
    },
}));

const useSensorStore = zustand.create((set, get) => ({
    // Initial state
    sensorData: [],
    currentMetrics: [],
    systemComponents: [],
    individualSensors: [],
    timeRange: '24h',
    isRealTime: true,
    loading: true,
    // Actions
    setSensorData: (data) => {
        set({ sensorData: data });
        get().updateCurrentMetrics();
    },
    addSensorDataPoint: (dataPoint) => {
        const { sensorData, timeRange } = get();
        const updated = [...sensorData, dataPoint];
        // Mantener solo los últimos datos según el rango de tiempo
        const maxPoints = timeRange === '1h' ? 60 : timeRange === '6h' ? 360 : timeRange === '24h' ? 1440 : 10080;
        const newData = updated.slice(-maxPoints);
        set({ sensorData: newData });
        get().updateCurrentMetrics();
    },
    setTimeRange: (range) => {
        set({ timeRange: range });
        get().generateMockData();
    },
    setIsRealTime: (isRealTime) => set({ isRealTime }),
    setLoading: (loading) => set({ loading }),
    generateMockData: () => {
        const { timeRange } = get();
        set({ loading: true });
        // Simular carga de datos
        setTimeout(() => {
            const data = [];
            const now = new Date();
            const intervals = timeRange === '1h' ? 60 : timeRange === '6h' ? 360 : timeRange === '24h' ? 1440 : 10080;
            const step = timeRange === '1h' ? 1 : timeRange === '6h' ? 6 : timeRange === '24h' ? 24 : 168;
            for (let i = intervals; i >= 0; i -= step) {
                const timestamp = new Date(now.getTime() - i * 60000);
                data.push({
                    timestamp: timestamp.toISOString(),
                    temperature: 24 + Math.sin(i / 100) * 2 + Math.random() * 0.5,
                    humidity: 65 + Math.cos(i / 80) * 10 + Math.random() * 2,
                    ph: 6.5 + Math.sin(i / 120) * 0.3 + Math.random() * 0.1,
                    light: 800 + Math.sin(i / 60) * 200 + Math.random() * 50,
                    conductivity: 1.2 + Math.sin(i / 90) * 0.2 + Math.random() * 0.05
                });
            }
            get().setSensorData(data);
            set({ loading: false });
        }, 1000);
    },
    updateCurrentMetrics: () => {
        const { sensorData } = get();
        if (sensorData.length === 0) {
            set({ currentMetrics: [] });
            return;
        }
        const latest = sensorData[sensorData.length - 1];
        if (!latest) {
            set({ currentMetrics: [] });
            return;
        }
        const metrics = [
            {
                title: 'Temperatura',
                value: latest.temperature.toFixed(1),
                unit: '°C',
                status: latest.temperature >= 18 && latest.temperature <= 25 ? 'optimal' :
                    latest.temperature >= 15 && latest.temperature <= 30 ? 'warning' : 'critical',
                trend: 'stable',
                change: '+0.2°C',
                iconType: 'temperature'
            },
            {
                title: 'Humedad',
                value: latest.humidity.toFixed(1),
                unit: '%',
                status: latest.humidity >= 60 && latest.humidity <= 80 ? 'optimal' :
                    latest.humidity >= 50 && latest.humidity <= 90 ? 'warning' : 'critical',
                trend: 'up',
                change: '+1.5%',
                iconType: 'humidity'
            },
            {
                title: 'pH',
                value: latest.ph.toFixed(2),
                unit: 'pH',
                status: latest.ph >= 5.5 && latest.ph <= 6.5 ? 'optimal' :
                    latest.ph >= 5.0 && latest.ph <= 7.0 ? 'warning' : 'critical',
                trend: 'down',
                change: '-0.1',
                iconType: 'ph'
            },
            {
                title: 'Luz',
                value: Math.round(latest.light).toString(),
                unit: 'lux',
                status: latest.light >= 200 && latest.light <= 400 ? 'optimal' :
                    latest.light >= 100 && latest.light <= 500 ? 'warning' : 'critical',
                trend: 'up',
                change: '+50 lux',
                iconType: 'light'
            },
            {
                title: 'Conductividad',
                value: latest.conductivity.toFixed(2),
                unit: 'mS/cm',
                status: latest.conductivity >= 1.2 && latest.conductivity <= 2.0 ? 'optimal' :
                    latest.conductivity >= 1.0 && latest.conductivity <= 2.5 ? 'warning' : 'critical',
                trend: 'stable',
                change: '0.00',
                iconType: 'electric'
            }
        ];
        set({ currentMetrics: metrics });
    },
    initializeSystemComponents: () => {
        const components = [
            {
                name: 'Sensor de Temperatura',
                status: 'online',
                lastUpdate: 'Hace 30 segundos',
                details: 'ESP32-001'
            },
            {
                name: 'Sensor de Humedad',
                status: 'online',
                lastUpdate: 'Hace 30 segundos',
                details: 'ESP32-001'
            },
            {
                name: 'Sensor de pH',
                status: 'warning',
                lastUpdate: 'Hace 2 minutos',
                details: 'ESP32-002 - Calibración requerida'
            },
            {
                name: 'Sensor de Luz',
                status: 'online',
                lastUpdate: 'Hace 30 segundos',
                details: 'ESP32-003'
            },
            {
                name: 'Sensor de Conductividad',
                status: 'online',
                lastUpdate: 'Hace 30 segundos',
                details: 'ESP32-002'
            },
            {
                name: 'Conectividad WiFi',
                status: 'online',
                lastUpdate: 'Conectado',
                details: 'Señal: -45 dBm'
            }
        ];
        set({ systemComponents: components });
    },
    updateSystemComponentStatus: (name, status, lastUpdate, details) => {
        const { systemComponents } = get();
        const updated = systemComponents.map(component => component.name === name
            ? { ...component, status, lastUpdate, ...(details !== undefined && { details }) }
            : component);
        set({ systemComponents: updated });
    },
    // Individual Sensors CRUD
    addIndividualSensor: (sensor) => {
        const { individualSensors } = get();
        const newSensor = {
            ...sensor,
            id: sensor.id || `SENSOR-${Date.now()}`,
            createdAt: sensor.createdAt || new Date().toISOString(),
            lastModified: new Date().toISOString(),
        };
        set({ individualSensors: [...individualSensors, newSensor] });
    },
    updateIndividualSensor: (sensorId, sensor) => {
        const { individualSensors } = get();
        const updated = individualSensors.map(s => s.id === sensorId
            ? { ...s, ...sensor, id: sensorId, lastModified: new Date().toISOString() }
            : s);
        set({ individualSensors: updated });
    },
    removeIndividualSensor: (sensorId) => {
        const { individualSensors } = get();
        const updated = individualSensors.filter(s => s.id !== sensorId);
        set({ individualSensors: updated });
    },
    initializeIndividualSensors: () => {
        const sensors = [
            {
                id: 'SENSOR-001',
                idFisico: 'TEMP-001',
                ubicacion: 'Zona A - Cultivo Principal',
                esp32Id: 'ESP32-001',
                frecuenciaLectura: 60,
                variablesAMedir: ['VAR-001'], // Temperatura
                unidadMedida: '°C',
                rangoMinimo: 0,
                rangoMaximo: 50,
                rangoOptimoMinimo: 18,
                rangoOptimoMaximo: 25,
                estado: 'activo',
                createdAt: new Date().toISOString(),
                lastModified: new Date().toISOString(),
            },
            {
                id: 'SENSOR-002',
                idFisico: 'HUM-001',
                ubicacion: 'Zona A - Cultivo Principal',
                esp32Id: 'ESP32-001',
                frecuenciaLectura: 60,
                variablesAMedir: ['VAR-002'], // Humedad
                unidadMedida: '%',
                rangoMinimo: 0,
                rangoMaximo: 100,
                rangoOptimoMinimo: 60,
                rangoOptimoMaximo: 80,
                estado: 'activo',
                createdAt: new Date().toISOString(),
                lastModified: new Date().toISOString(),
            },
        ];
        set({ individualSensors: sensors });
    }
}));

const useAlertStore = zustand.create((set, get) => ({
    // Initial state
    alerts: [],
    filter: 'all',
    // Computed properties
    get filteredAlerts() {
        const state = get();
        let filtered = state.alerts;
        switch (state.filter) {
            case 'unread':
                filtered = filtered.filter(alert => !alert.isRead);
                break;
            case 'warning':
                filtered = filtered.filter(alert => alert.type === 'warning');
                break;
            case 'error':
                filtered = filtered.filter(alert => alert.type === 'error');
                break;
        }
        return filtered;
    },
    get unreadCount() {
        const state = get();
        return state.alerts.filter(alert => !alert.isRead).length;
    },
    // Actions
    addAlert: (alertData) => {
        const newAlert = {
            ...alertData,
            id: Date.now().toString() + Math.random().toString(36).substr(2, 9)
        };
        set(state => ({
            alerts: [newAlert, ...state.alerts]
        }));
    },
    markAsRead: (alertId) => {
        set(state => ({
            alerts: state.alerts.map(alert => alert.id === alertId ? { ...alert, isRead: true } : alert)
        }));
    },
    markAllAsRead: () => {
        set(state => ({
            alerts: state.alerts.map(alert => ({ ...alert, isRead: true }))
        }));
    },
    removeAlert: (alertId) => {
        set(state => ({
            alerts: state.alerts.filter(alert => alert.id !== alertId)
        }));
    },
    clearAllAlerts: () => {
        set({ alerts: [] });
    },
    setFilter: (filter) => {
        set({ filter });
    },
    initializeAlerts: () => {
        const initialAlerts = [
            {
                id: '1',
                type: 'warning',
                title: 'pH fuera de rango',
                message: 'El nivel de pH (6.2) está por debajo del rango óptimo (6.5-7.0)',
                timestamp: 'Hace 5 minutos',
                isRead: false,
                sensor: 'ESP32-002'
            },
            {
                id: '2',
                type: 'info',
                title: 'Calibración programada',
                message: 'Calibración automática del sensor de pH programada para mañana',
                timestamp: 'Hace 1 hora',
                isRead: false,
                sensor: 'ESP32-002'
            },
            {
                id: '3',
                type: 'success',
                title: 'Sistema estabilizado',
                message: 'Todos los parámetros han vuelto a niveles óptimos',
                timestamp: 'Hace 2 horas',
                isRead: true
            },
            {
                id: '4',
                type: 'info',
                title: 'Actualización disponible',
                message: 'Nueva versión del firmware disponible para ESP32-001',
                timestamp: 'Hace 3 horas',
                isRead: true,
                sensor: 'ESP32-001'
            }
        ];
        set({ alerts: initialAlerts });
    },
    // Computed functions
    getFilteredAlerts: () => {
        const { alerts, filter } = get();
        switch (filter) {
            case 'unread':
                return alerts.filter(alert => !alert.isRead);
            case 'warning':
                return alerts.filter(alert => alert.type === 'warning');
            case 'error':
                return alerts.filter(alert => alert.type === 'error');
            default:
                return alerts;
        }
    },
    getUnreadCount: () => {
        const { alerts } = get();
        return alerts.filter(alert => !alert.isRead).length;
    }
}));

const useActuatorStore = zustand.create((set, get) => ({
    // Estado inicial
    actuadores: [],
    // Acciones
    addActuador: (actuadorData) => {
        const newActuador = {
            // Usamos el tipo Omit para asegurar que solo pasamos los campos del formulario
            ...actuadorData,
            id: 'ACT-' + Date.now().toString().slice(-6),
            createdAt: new Date().toISOString().split('T')[0],
            lastModified: new Date().toISOString().split('T')[0],
        };
        set(state => ({
            actuadores: [...state.actuadores, newActuador],
        }));
    },
    updateActuador: (id, updates) => {
        set(state => ({
            actuadores: state.actuadores.map(actuador => actuador.id === id
                ? { ...actuador, ...updates, lastModified: new Date().toISOString().split('T')[0] }
                : actuador),
        }));
    },
    removeActuador: (id) => {
        set(state => ({
            actuadores: state.actuadores.filter(actuador => actuador.id !== id),
        }));
    },
    // ⭐ PASO 2: Simplificar los datos de ejemplo para que coincidan.
    initializeActuadores: () => {
        const initialActuadores = [
            {
                id: 'ACT-001',
                name: 'Bomba de Riego Principal',
                type: 'pump',
                location: 'Zona A - Sector 1',
                pin: 12,
                esp32Id: 'ESP32-001',
                status: 'active',
                createdAt: '2023-12-01',
                lastModified: '2024-01-20',
            },
            {
                id: 'ACT-002',
                name: 'Ventilador Extractor Norte',
                type: 'fan',
                location: 'Zona B - Ventilación',
                pin: 14,
                esp32Id: 'ESP32-002',
                status: 'inactive',
                createdAt: '2023-11-15',
                lastModified: '2024-01-18',
            },
        ];
        set({ actuadores: initialActuadores });
    },
}));

const useVariableStore = zustand.create((set, get) => ({
    // Initial state
    variables: [],
    loading: false,
    // Actions
    setVariables: (variables) => {
        set({ variables });
    },
    addVariable: (variableData) => {
        const newVariable = {
            ...variableData,
            id: variableData.name.toLowerCase().replace(/\s+/g, '-') + '-' + Date.now().toString().slice(-4),
            createdAt: new Date().toISOString().split('T')[0],
            lastModified: new Date().toISOString().split('T')[0]
        };
        set(state => ({
            variables: [...state.variables, newVariable]
        }));
    },
    updateVariable: (id, updates) => {
        set(state => ({
            variables: state.variables.map(variable => variable.id === id
                ? { ...variable, ...updates, lastModified: new Date().toISOString().split('T')[0] }
                : variable)
        }));
    },
    removeVariable: (id) => {
        set(state => ({
            variables: state.variables.filter(variable => variable.id !== id)
        }));
    },
    setLoading: (loading) => {
        set({ loading });
    },
    initializeVariables: () => {
        const initialVariables = [
            {
                id: 'temp-ambiente',
                name: 'Temperatura Ambiente',
                description: 'Temperatura del aire en el invernadero',
                unit: '°C',
                type: 'input',
                dataType: 'numeric',
                minValue: 0,
                maxValue: 50,
                isRequired: true,
                category: 'environmental',
                status: 'active',
                createdAt: '2024-01-15',
                lastModified: '2024-01-18'
            },
            {
                id: 'humedad-relativa',
                name: 'Humedad Relativa',
                description: 'Porcentaje de humedad en el ambiente',
                unit: '%',
                type: 'input',
                dataType: 'numeric',
                minValue: 0,
                maxValue: 100,
                isRequired: true,
                category: 'environmental',
                status: 'active',
                createdAt: '2024-01-15',
                lastModified: '2024-01-16'
            },
            {
                id: 'ph-agua',
                name: 'pH del Agua',
                description: 'Nivel de acidez del agua de riego',
                unit: 'pH',
                type: 'input',
                dataType: 'numeric',
                minValue: 0,
                maxValue: 14,
                isRequired: true,
                category: 'environmental',
                status: 'active',
                createdAt: '2024-01-10',
                lastModified: '2024-01-20'
            },
            {
                id: 'riego-activo',
                name: 'Sistema de Riego',
                description: 'Estado del sistema de riego automático',
                unit: 'bool',
                type: 'output',
                dataType: 'boolean',
                isRequired: false,
                category: 'control',
                status: 'active',
                createdAt: '2024-01-12',
                lastModified: '2024-01-19'
            },
            {
                id: 'indice-crecimiento',
                name: 'Índice de Crecimiento',
                description: 'Índice calculado basado en condiciones ambientales',
                unit: 'índice',
                type: 'calculated',
                dataType: 'numeric',
                minValue: 0,
                maxValue: 100,
                isRequired: false,
                category: 'system',
                status: 'active',
                createdAt: '2024-01-08',
                lastModified: '2024-01-17'
            },
            {
                id: 'conductividad-electrica',
                name: 'Conductividad Eléctrica',
                description: 'Medición de la conductividad del agua',
                unit: 'mS/cm',
                type: 'input',
                dataType: 'numeric',
                minValue: 0,
                maxValue: 5,
                isRequired: true,
                category: 'environmental',
                status: 'active',
                createdAt: '2024-01-05',
                lastModified: '2024-01-15'
            },
            {
                id: 'intensidad-luminica',
                name: 'Intensidad Lumínica',
                description: 'Medición de la intensidad de luz en el invernadero',
                unit: 'lux',
                type: 'input',
                dataType: 'numeric',
                minValue: 0,
                maxValue: 2000,
                isRequired: true,
                category: 'environmental',
                status: 'active',
                createdAt: '2024-01-03',
                lastModified: '2024-01-14'
            }
        ];
        set({ variables: initialVariables });
    },
    // Computed functions
    getVariableById: (id) => {
        const { variables } = get();
        return variables.find(variable => variable.id === id);
    },
    getVariablesByType: (type) => {
        const { variables } = get();
        return variables.filter(variable => variable.type === type);
    },
    getVariablesByCategory: (category) => {
        const { variables } = get();
        return variables.filter(variable => variable.category === category);
    },
    getActiveVariables: () => {
        const { variables } = get();
        return variables.filter(variable => variable.status === 'active');
    },
    getRequiredVariables: () => {
        const { variables } = get();
        return variables.filter(variable => variable.isRequired);
    }
}));

const useReadingsStore = zustand.create((set, get) => ({
    // Initial state
    sensorSummary: [],
    individualReadings: [],
    // Actions
    setSensorSummary: (summary) => {
        set({ sensorSummary: summary });
    },
    setIndividualReadings: (readings) => {
        set({ individualReadings: readings });
    },
    addReading: (reading) => {
        set(state => ({
            individualReadings: [reading, ...state.individualReadings]
        }));
    },
    initializeReadings: () => {
        const initialSummary = [
            {
                sensor: 'Temperatura',
                media: 25.5,
                minimo: 24.8,
                maximo: 26.2,
                unidad: '°C',
                ultimaLectura: '2024-01-26 10:01:10'
            },
            {
                sensor: 'Humedad',
                media: 60.2,
                minimo: 59.5,
                maximo: 61.0,
                unidad: '%',
                ultimaLectura: '2024-01-26 10:01:00'
            },
            {
                sensor: 'Intensidad Lumínica',
                media: 850,
                minimo: 820,
                maximo: 880,
                unidad: 'lux',
                ultimaLectura: '2024-01-26 10:01:00'
            },
            {
                sensor: 'Conductividad Eléctrica',
                media: 45,
                minimo: 44,
                maximo: 46,
                unidad: '%',
                ultimaLectura: '2024-01-26 10:00:10'
            }
        ];
        const initialReadings = [
            { id: '1', fecha: '2024-01-26 10:00:00', sensor: 'Temperatura', valor: 25.2, unidad: '°C' },
            { id: '2', fecha: '2024-01-26 10:00:00', sensor: 'Humedad', valor: 60.1, unidad: '%' },
            { id: '3', fecha: '2024-01-26 10:00:00', sensor: 'Intensidad Lumínica', valor: 845, unidad: 'lux' },
            { id: '4', fecha: '2024-01-26 10:00:00', sensor: 'Conductividad Eléctrica', valor: 44, unidad: '%' },
            { id: '5', fecha: '2024-01-26 10:00:10', sensor: 'Temperatura', valor: 25.3, unidad: '°C' },
            { id: '6', fecha: '2024-01-26 10:00:10', sensor: 'Humedad', valor: 60.3, unidad: '%' },
            { id: '7', fecha: '2024-01-26 10:00:10', sensor: 'Intensidad Lumínica', valor: 855, unidad: 'lux' },
            { id: '8', fecha: '2024-01-26 10:00:10', sensor: 'Conductividad Eléctrica', valor: 46, unidad: '%' },
            { id: '9', fecha: '2024-01-26 10:00:20', sensor: 'Temperatura', valor: 25.5, unidad: '°C' },
            { id: '10', fecha: '2024-01-26 10:00:20', sensor: 'Humedad', valor: 60.5, unidad: '%' },
            { id: '11', fecha: '2024-01-26 10:00:30', sensor: 'Temperatura', valor: 25.4, unidad: '°C' },
            { id: '12', fecha: '2024-01-26 10:00:30', sensor: 'Humedad', valor: 60.2, unidad: '%' },
            { id: '13', fecha: '2024-01-26 10:00:30', sensor: 'Intensidad Lumínica', valor: 860, unidad: 'lux' },
            { id: '14', fecha: '2024-01-26 10:00:40', sensor: 'Temperatura', valor: 25.6, unidad: '°C' },
            { id: '15', fecha: '2024-01-26 10:00:40', sensor: 'Humedad', valor: 60.4, unidad: '%' },
            { id: '16', fecha: '2024-01-26 10:00:50', sensor: 'Temperatura', valor: 25.3, unidad: '°C' },
            { id: '17', fecha: '2024-01-26 10:01:00', sensor: 'Temperatura', valor: 25.7, unidad: '°C' },
            { id: '18', fecha: '2024-01-26 10:01:00', sensor: 'Humedad', valor: 60.6, unidad: '%' },
            { id: '19', fecha: '2024-01-26 10:01:00', sensor: 'Intensidad Lumínica', valor: 865, unidad: 'lux' },
            { id: '20', fecha: '2024-01-26 10:01:10', sensor: 'Temperatura', valor: 25.8, unidad: '°C' }
        ];
        set({
            sensorSummary: initialSummary,
            individualReadings: initialReadings
        });
    }
}));

// Términos para temperatura
const temperatureTerms = [
    {
        id: 'temp-low',
        variable_id: 'temp-input',
        label: 'baja',
        membership_function: {
            function_type: 'triangular',
            parameters: [10, 15, 20],
            universe_min: 10,
            universe_max: 35
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'temp-medium',
        variable_id: 'temp-input',
        label: 'media',
        membership_function: {
            function_type: 'triangular',
            parameters: [18, 23, 28],
            universe_min: 10,
            universe_max: 35
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'temp-high',
        variable_id: 'temp-input',
        label: 'alta',
        membership_function: {
            function_type: 'triangular',
            parameters: [25, 30, 35],
            universe_min: 10,
            universe_max: 35
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    }
];
// Términos para humedad
const humidityTerms = [
    {
        id: 'hum-low',
        variable_id: 'humidity-input',
        label: 'baja',
        membership_function: {
            function_type: 'triangular',
            parameters: [30, 40, 50],
            universe_min: 30,
            universe_max: 90
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'hum-medium',
        variable_id: 'humidity-input',
        label: 'media',
        membership_function: {
            function_type: 'triangular',
            parameters: [50, 65, 80],
            universe_min: 30,
            universe_max: 90
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'hum-high',
        variable_id: 'humidity-input',
        label: 'alta',
        membership_function: {
            function_type: 'triangular',
            parameters: [70, 80, 90],
            universe_min: 30,
            universe_max: 90
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    }
];
// Términos para duración de riego
const irrigationTerms = [
    {
        id: 'irrig-short',
        variable_id: 'irrigation-output',
        label: 'corta',
        membership_function: {
            function_type: 'triangular',
            parameters: [1, 3, 5],
            universe_min: 0,
            universe_max: 15
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'irrig-medium',
        variable_id: 'irrigation-output',
        label: 'media',
        membership_function: {
            function_type: 'triangular',
            parameters: [4, 7, 10],
            universe_min: 0,
            universe_max: 15
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'irrig-long',
        variable_id: 'irrigation-output',
        label: 'larga',
        membership_function: {
            function_type: 'triangular',
            parameters: [8, 12, 15],
            universe_min: 0,
            universe_max: 15
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    }
];
// Términos para potencia del ventilador
const fanPowerTerms = [
    {
        id: 'fan-low',
        variable_id: 'fan-output',
        label: 'baja',
        membership_function: {
            function_type: 'triangular',
            parameters: [0, 25, 50],
            universe_min: 0,
            universe_max: 100
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'fan-medium',
        variable_id: 'fan-output',
        label: 'media',
        membership_function: {
            function_type: 'triangular',
            parameters: [30, 50, 70],
            universe_min: 0,
            universe_max: 100
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'fan-high',
        variable_id: 'fan-output',
        label: 'alta',
        membership_function: {
            function_type: 'triangular',
            parameters: [60, 80, 100],
            universe_min: 0,
            universe_max: 100
        },
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    }
];
// Variables de entrada
const inputVariables = [
    {
        id: 'temp-input',
        system_id: 'hydro-control-system',
        name: 'Temperatura',
        description: 'Temperatura ambiente en grados Celsius',
        variable_type: 'input',
        device_id: 'sensor-temp-001',
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'humidity-input',
        system_id: 'hydro-control-system',
        name: 'Humedad',
        description: 'Humedad relativa del ambiente en porcentaje',
        variable_type: 'input',
        device_id: 'sensor-hum-001',
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    }
];
// Variables de salida
const outputVariables = [
    {
        id: 'irrigation-output',
        system_id: 'hydro-control-system',
        name: 'Duración de Riego',
        description: 'Duración del riego en minutos',
        variable_type: 'output',
        device_id: 'actuator-pump-001',
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'fan-output',
        system_id: 'hydro-control-system',
        name: 'Potencia del Ventilador',
        description: 'Potencia del ventilador en porcentaje',
        variable_type: 'output',
        device_id: 'actuator-fan-001',
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z'
    }
];
// Rutinas
const routines = [
    {
        id: 'routine-irrigation-short',
        system_id: 'hydro-control-system',
        routine_name: 'Riego Corto',
        created_at: '2024-01-15T10:00:00Z',
        steps: [
            {
                step_id: 0,
                condition: 'Actuador: bomba de agua',
                power_term_id: 'irrig-short',
                duration_term_id: 'irrig-short'
            }
        ]
    },
    {
        id: 'routine-irrigation-medium',
        system_id: 'hydro-control-system',
        routine_name: 'Riego Medio',
        created_at: '2024-01-15T10:00:00Z',
        steps: [
            {
                step_id: 0,
                condition: 'Actuador: bomba de agua',
                power_term_id: 'irrig-medium',
                duration_term_id: 'irrig-medium'
            }
        ]
    },
    {
        id: 'routine-fan-cooling',
        system_id: 'hydro-control-system',
        routine_name: 'Enfriamiento con Ventilador',
        created_at: '2024-01-15T10:00:00Z',
        steps: [
            {
                step_id: 0,
                condition: 'Actuador: ventilador',
                power_term_id: 'fan-high',
                duration_term_id: 'irrig-medium'
            }
        ]
    }
];
// Reglas fuzzy
const rules = [
    {
        id: 'rule-temp-high-hum-low',
        name: 'Temperatura alta y humedad baja => Riego medio',
        system_id: 'hydro-control-system',
        description: 'Si la temperatura es alta y la humedad es baja, entonces aplicar riego medio',
        conditions: [
            {
                variableId: 'temp-input',
                operator: 'IS',
                value: 'alta'
            },
            {
                variableId: 'humidity-input',
                operator: 'IS',
                value: 'baja'
            }
        ],
        connectors: ['AND'],
        routine: routines[1], // Riego Medio
        created_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'rule-temp-high',
        name: 'Temperatura alta => Ventilador',
        system_id: 'hydro-control-system',
        description: 'Si la temperatura es alta, entonces activar ventilador',
        conditions: [
            {
                variableId: 'temp-input',
                operator: 'IS',
                value: 'alta'
            }
        ],
        connectors: [],
        routine: routines[2], // Enfriamiento con Ventilador
        created_at: '2024-01-15T10:00:00Z'
    },
    {
        id: 'rule-hum-low',
        name: 'Humedad baja => Riego corto',
        system_id: 'hydro-control-system',
        description: 'Si la humedad es baja, entonces aplicar riego corto',
        conditions: [
            {
                variableId: 'humidity-input',
                operator: 'IS',
                value: 'baja'
            }
        ],
        connectors: [],
        routine: routines[0], // Riego Corto
        created_at: '2024-01-15T10:00:00Z'
    }
];
// Sistema fuzzy principal
const mockFuzzySystems = [
    {
        id: 'hydro-control-system',
        name: 'Sistema de Control Hidropónico',
        status: 'ACTIVE',
        defuzzification_method: 'centroid',
        operators: {
            and: 'min',
            or: 'max',
            not: 'complement'
        },
        input_variables: inputVariables,
        output_variables: outputVariables,
        rules: rules,
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z',
        created_by: null
    },
    {
        id: 'climate-control-system',
        name: 'Sistema de Control de Clima',
        status: 'INACTIVE',
        defuzzification_method: 'centroid',
        operators: {
            and: 'min',
            or: 'max',
            not: 'complement'
        },
        input_variables: [inputVariables[0]], // Solo temperatura
        output_variables: [outputVariables[1]], // Solo ventilador
        rules: [rules[1]], // Solo regla de ventilador
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z',
        created_by: null
    },
    {
        id: 'pressure-monitoring-system',
        name: 'Monitorización de Presión',
        status: 'INACTIVE',
        defuzzification_method: 'centroid',
        operators: {
            and: 'min',
            or: 'max',
            not: 'complement'
        },
        input_variables: [],
        output_variables: [],
        rules: [],
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z',
        created_by: null
    },
    {
        id: 'smart-lighting-system',
        name: 'Sistema de Iluminación Inteligente',
        status: 'INACTIVE',
        defuzzification_method: 'centroid',
        operators: {
            and: 'min',
            or: 'max',
            not: 'complement'
        },
        input_variables: [],
        output_variables: [],
        rules: [],
        created_at: '2024-01-15T10:00:00Z',
        updated_at: '2024-01-15T10:00:00Z',
        created_by: null
    }
];
// Exportar todos los datos de ejemplo
const mockFuzzyVariables = [...inputVariables, ...outputVariables];
const mockFuzzyTerms = [...temperatureTerms, ...humidityTerms, ...irrigationTerms, ...fanPowerTerms];
const mockFuzzyRules = rules;
const mockFuzzyRoutines = routines;

const useFuzzyStore = zustand.create((set, get) => ({
    // State
    fuzzySystems: [],
    fuzzyVariables: [],
    fuzzyTerms: [],
    fuzzyRules: [],
    fuzzyRoutines: [],
    loading: false,
    selectedSystem: null,
    // Actions
    setFuzzySystems: (systems) => set({ fuzzySystems: systems }),
    setFuzzyVariables: (variables) => set({ fuzzyVariables: variables }),
    setFuzzyTerms: (terms) => set({ fuzzyTerms: terms }),
    setFuzzyRules: (rules) => set({ fuzzyRules: rules }),
    setFuzzyRoutines: (routines) => set({ fuzzyRoutines: routines }),
    setLoading: (loading) => set({ loading }),
    // Initialize with mock data
    initializeFuzzyData: () => {
        set({
            fuzzySystems: mockFuzzySystems,
            fuzzyVariables: mockFuzzyVariables,
            fuzzyTerms: mockFuzzyTerms,
            fuzzyRules: mockFuzzyRules,
            fuzzyRoutines: mockFuzzyRoutines,
            loading: false
        });
    },
    // Add new entities
    addFuzzySystem: (system) => set(state => ({
        fuzzySystems: [...state.fuzzySystems, system]
    })),
    addFuzzyVariable: (variable) => set(state => ({
        fuzzyVariables: [...state.fuzzyVariables, variable]
    })),
    addFuzzyTerm: (term) => set(state => ({
        fuzzyTerms: [...state.fuzzyTerms, term]
    })),
    addFuzzyRule: (rule) => set(state => ({
        fuzzyRules: [...state.fuzzyRules, rule]
    })),
    addFuzzyRoutine: (routine) => set(state => ({
        fuzzyRoutines: [...state.fuzzyRoutines, routine]
    })),
    // Update entities
    updateFuzzySystem: (id, updates) => set(state => ({
        fuzzySystems: state.fuzzySystems.map(system => system.id === id ? { ...system, ...updates } : system)
    })),
    updateFuzzyVariable: (id, updates) => set(state => ({
        fuzzyVariables: state.fuzzyVariables.map(variable => variable.id === id ? { ...variable, ...updates } : variable)
    })),
    updateFuzzyTerm: (id, updates) => set(state => ({
        fuzzyTerms: state.fuzzyTerms.map(term => term.id === id ? { ...term, ...updates } : term)
    })),
    updateFuzzyRule: (id, updates) => set(state => ({
        fuzzyRules: state.fuzzyRules.map(rule => rule.id === id ? { ...rule, ...updates } : rule)
    })),
    updateFuzzyRoutine: (id, updates) => set(state => ({
        fuzzyRoutines: state.fuzzyRoutines.map(routine => routine.id === id ? { ...routine, ...updates } : routine)
    })),
    // Remove entities
    removeFuzzySystem: (id) => set(state => ({
        fuzzySystems: state.fuzzySystems.filter(system => system.id !== id)
    })),
    removeFuzzyVariable: (id) => set(state => ({
        fuzzyVariables: state.fuzzyVariables.filter(variable => variable.id !== id)
    })),
    removeFuzzyTerm: (id) => set(state => ({
        fuzzyTerms: state.fuzzyTerms.filter(term => term.id !== id)
    })),
    removeFuzzyRule: (id) => set(state => ({
        fuzzyRules: state.fuzzyRules.filter(rule => rule.id !== id)
    })),
    removeFuzzyRoutine: (id) => set(state => ({
        fuzzyRoutines: state.fuzzyRoutines.filter(routine => routine.id !== id)
    })),
    // Computed getters
    getFuzzySystemById: (id) => {
        const state = get();
        return state.fuzzySystems.find(system => system.id === id);
    },
    getFuzzyVariableById: (id) => {
        const state = get();
        return state.fuzzyVariables.find(variable => variable.id === id);
    },
    getFuzzyTermById: (id) => {
        const state = get();
        return state.fuzzyTerms.find(term => term.id === id);
    },
    getFuzzyRuleById: (id) => {
        const state = get();
        return state.fuzzyRules.find(rule => rule.id === id);
    },
    getFuzzyRoutineById: (id) => {
        const state = get();
        return state.fuzzyRoutines.find(routine => routine.id === id);
    },
    getActiveFuzzySystems: () => {
        const state = get();
        return state.fuzzySystems.filter(system => system.status === 'ACTIVE');
    },
    getInactiveFuzzySystems: () => {
        const state = get();
        return state.fuzzySystems.filter(system => system.status === 'INACTIVE');
    },
    getVariablesBySystemId: (systemId) => {
        const state = get();
        return state.fuzzyVariables.filter(variable => variable.system_id === systemId);
    },
    getInputVariablesBySystemId: (systemId) => {
        const state = get();
        return state.fuzzyVariables.filter(variable => variable.system_id === systemId && variable.variable_type === 'input');
    },
    getOutputVariablesBySystemId: (systemId) => {
        const state = get();
        return state.fuzzyVariables.filter(variable => variable.system_id === systemId && variable.variable_type === 'output');
    },
    getTermsByVariableId: (variableId) => {
        const state = get();
        return state.fuzzyTerms.filter(term => term.variable_id === variableId);
    },
    getRulesBySystemId: (systemId) => {
        const state = get();
        return state.fuzzyRules.filter(rule => rule.system_id === systemId);
    },
    getRoutinesBySystemId: (systemId) => {
        const state = get();
        return state.fuzzyRoutines.filter(routine => routine.system_id === systemId);
    }
}));

// Definición de variables disponibles para graficar
const CHART_VARIABLES = {
    '1': {
        id: '1',
        name: 'Temperatura',
        unit: '°C',
        color: '#FF6B6B',
        baseValue: 25,
        range: 10,
    },
    '2': {
        id: '2',
        name: 'Humedad',
        unit: '%',
        color: '#4ECDC4',
        baseValue: 60,
        range: 30,
    },
    '3': {
        id: '3',
        name: 'Presión',
        unit: 'hPa',
        color: '#45B7D1',
        baseValue: 1013,
        range: 100,
    },
    '4': {
        id: '4',
        name: 'pH',
        unit: '',
        color: '#96CEB4',
        baseValue: 7.2,
        range: 2,
    },
    '5': {
        id: '5',
        name: 'Conductividad',
        unit: 'µS/cm',
        color: '#F7DC6F',
        baseValue: 500,
        range: 200,
    },
    '6': {
        id: '6',
        name: 'Oxígeno Disuelto',
        unit: 'mg/L',
        color: '#BB8FCE',
        baseValue: 8.5,
        range: 4,
    },
};
// Función helper para generar datos realistas de series temporales
// Usa una semilla basada en el variableId para generar datos determinísticos
const generateTimeSeriesData = (variableId, days = 30) => {
    const variable = CHART_VARIABLES[variableId];
    if (!variable)
        return [];
    const { baseValue, range } = variable;
    const data = [];
    // Usar el ID de la variable como semilla para generar datos consistentes
    const seed = parseInt(variableId) || 1;
    for (let i = 0; i < days; i++) {
        const date = new Date();
        date.setDate(date.getDate() - (days - 1 - i));
        // Generar valor con patrón senoidal determinístico (sin Math.random)
        const dayOfYear = Math.floor((date.getTime() - new Date(date.getFullYear(), 0, 0).getTime()) / 86400000);
        const trend = Math.sin((dayOfYear / 365) * 2 * Math.PI) * 0.3; // Tendencia anual
        const daily = Math.sin((i / 7) * 2 * Math.PI) * 0.4; // Patrón semanal
        // Ruido pseudo-aleatorio determinístico basado en la semilla
        const noise = Math.sin((i + seed) * 123.456) * 0.3;
        const variation = (trend + daily + noise);
        const value = baseValue + variation * range;
        data.push({
            value: Math.round(value * 100) / 100,
            timestamp: date.toISOString(),
            label: date.toISOString().split('T')[0],
        });
    }
    return data;
};
// Función helper para generar datos de dispersión con correlación
// IMPORTANTE: Usa los mismos datos de series temporales para mantener consistencia
const generateScatterData = (variableXId, variableYId, points = 30) => {
    const variableX = CHART_VARIABLES[variableXId];
    const variableY = CHART_VARIABLES[variableYId];
    if (!variableX || !variableY)
        return [];
    // Generar datos de series temporales para ambas variables
    // Esto asegura que los datos sean consistentes con las gráficas de serie de tiempo
    const dataX = generateTimeSeriesData(variableXId, points);
    const dataY = generateTimeSeriesData(variableYId, points);
    // Definir correlaciones realistas entre variables
    const correlations = {
        '1': { '2': -0.6, '3': -0.2, '4': 0.3, '5': 0.5, '6': -0.7 }, // Temperatura
        '2': { '1': -0.6, '3': 0.1, '4': -0.2, '5': -0.2, '6': -0.4 }, // Humedad
        '3': { '1': -0.2, '2': 0.1, '4': 0.1, '5': 0.2, '6': 0.3 }, // Presión
        '4': { '1': 0.3, '2': -0.2, '3': 0.1, '5': 0.4, '6': 0.5 }, // pH
        '5': { '1': 0.5, '2': -0.2, '3': 0.2, '4': 0.4, '6': -0.3 }, // Conductividad
        '6': { '1': -0.7, '2': -0.4, '3': 0.3, '4': 0.5, '5': -0.3 }, // Oxígeno
    };
    const correlation = correlations[variableXId]?.[variableYId] ||
        correlations[variableYId]?.[variableXId] ||
        0;
    const { baseValue: baseX, range: rangeX } = variableX;
    const { baseValue: baseY, range: rangeY } = variableY;
    const data = [];
    for (let i = 0; i < Math.min(dataX.length, dataY.length); i++) {
        // Usar los valores de las series temporales
        let x = dataX[i].value;
        let y = dataY[i].value;
        // Aplicar correlación ajustando el valor Y en función de X
        if (correlation !== 0) {
            const seed = (parseInt(variableXId) + parseInt(variableYId)) * i;
            const noise = Math.sin(seed * 78.91) * 0.2; // Ruido determinístico pequeño
            // Ajustar Y para que esté correlacionado con X
            const xNormalized = (x - baseX) / rangeX;
            const correlationEffect = correlation * xNormalized * rangeY;
            const noiseEffect = Math.sqrt(Math.max(0, 1 - correlation * correlation)) * noise * rangeY;
            y = baseY + correlationEffect + noiseEffect;
        }
        data.push({
            value: Math.round(x * 100) / 100,
            value1: Math.round(y * 100) / 100,
            label: `P${i + 1}`,
        });
    }
    return data;
};
const useDashboardStore = zustand.create((set, get) => ({
    // Initial state
    timeSeriesCache: new Map(),
    scatterCache: new Map(),
    loading: false,
    error: null,
    // Actions
    fetchTimeSeriesData: async (variableId, days = 30) => {
        set({ loading: true, error: null });
        try {
            // Simular llamada a API con delay
            await new Promise(resolve => setTimeout(resolve, 300));
            // En producción, aquí harías: const response = await fetch(`/api/timeseries/${variableId}?days=${days}`)
            const data = generateTimeSeriesData(variableId, days);
            set(state => {
                const newCache = new Map(state.timeSeriesCache);
                newCache.set(variableId, data);
                return { timeSeriesCache: newCache, loading: false };
            });
        }
        catch (error) {
            set({
                error: error instanceof Error ? error.message : 'Error al cargar datos de series temporales',
                loading: false
            });
        }
    },
    fetchScatterData: async (variableXId, variableYId, points = 30) => {
        set({ loading: true, error: null });
        try {
            // Simular llamada a API con delay
            await new Promise(resolve => setTimeout(resolve, 300));
            // En producción: const response = await fetch(`/api/scatter/${variableXId}/${variableYId}?points=${points}`)
            const data = generateScatterData(variableXId, variableYId, points);
            const cacheKey = `${variableXId}-${variableYId}`;
            set(state => {
                const newCache = new Map(state.scatterCache);
                newCache.set(cacheKey, data);
                return { scatterCache: newCache, loading: false };
            });
        }
        catch (error) {
            set({
                error: error instanceof Error ? error.message : 'Error al cargar datos de dispersión',
                loading: false
            });
        }
    },
    clearCache: () => {
        set({ timeSeriesCache: new Map(), scatterCache: new Map() });
    },
    setLoading: (loading) => {
        set({ loading });
    },
    setError: (error) => {
        set({ error });
    },
    // Getters
    getTimeSeriesData: (variableId) => {
        return get().timeSeriesCache.get(variableId) || null;
    },
    getScatterData: (variableXId, variableYId) => {
        const cacheKey = `${variableXId}-${variableYId}`;
        return get().scatterCache.get(cacheKey) || null;
    },
}));

const useLoginForm = () => {
    const [formState, setFormState] = React.useState({
        email: '',
        password: '',
    });
    const { login, isLoading, error, clearError } = useAuthStore();
    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormState(prev => ({ ...prev, [name]: value }));
    };
    const handleSubmit = async (e) => {
        e.preventDefault();
        await login(formState.email, formState.password);
    };
    return {
        formState,
        isLoading,
        error,
        handleChange,
        handleSubmit,
        clearError,
    };
};

// Base authentication hook that works across platforms
function useAuth(config) {
    const [state, setState] = React.useState({
        session: null,
        isLoading: true,
        error: null,
    });
    const apiService = new AuthApiService(config?.apiBaseUrl);
    const platform = config?.platform || 'web';
    // Initialize: check existing session
    React.useEffect(() => {
        const checkAuthStatus = async () => {
            try {
                // Try to get current session from server
                const userSession = await apiService.getCurrentSession(platform);
                // For mobile, get tokens from storage
                const sessionId = platform === 'mobile'
                    ? await SessionStorage.getSessionId()
                    : null;
                const csrfToken = platform === 'mobile'
                    ? await SessionStorage.getCsrfToken()
                    : null;
                const session = {
                    sessionId,
                    csrfToken,
                    userId: userSession.userId,
                    userRole: userSession.userRole,
                };
                setState({ session, isLoading: false, error: null });
            }
            catch (error) {
                // No valid session found
                setState({ session: null, isLoading: false, error: null });
            }
        };
        checkAuthStatus();
    }, [platform]);
    /**
     * Login function
     */
    const login = React.useCallback(async (email, password) => {
        setState(prev => ({ ...prev, isLoading: true, error: null }));
        try {
            const credentials = { Email: email, Password: password };
            if (platform === 'web') {
                // Web: cookies are set automatically by the server
                await apiService.loginWeb(credentials);
                // Get user session data
                const userSession = await apiService.getCurrentSession('web');
                const session = {
                    sessionId: null, // HttpOnly cookie, not accessible
                    csrfToken: null, // Cookie
                    userId: userSession.userId,
                    userRole: userSession.userRole,
                };
                setState({ session, isLoading: false, error: null });
            }
            else {
                // Mobile: get tokens from response and store them
                const mobileResponse = await apiService.loginMobile(credentials);
                // Store tokens in secure storage
                await SessionStorage.storeSession(mobileResponse.sessionId, mobileResponse.csrfToken);
                // Get user session data
                const userSession = await apiService.getCurrentSession('mobile');
                const session = {
                    sessionId: mobileResponse.sessionId,
                    csrfToken: mobileResponse.csrfToken,
                    userId: userSession.userId,
                    userRole: userSession.userRole,
                };
                setState({ session, isLoading: false, error: null });
            }
        }
        catch (error) {
            let errorMessage = 'Error de login';
            if (error instanceof ApiError) {
                if (error.status === 401) {
                    errorMessage = 'Credenciales inválidas';
                }
                else if (error.status === 0) {
                    errorMessage = 'Error de conexión con el servidor';
                }
                else {
                    errorMessage = error.message;
                }
            }
            else if (error instanceof Error) {
                errorMessage = error.message;
            }
            setState(prev => ({ ...prev, isLoading: false, error: errorMessage }));
            throw error;
        }
    }, [platform, apiService]);
    /**
     * Logout function
     */
    const logout = React.useCallback(async () => {
        setState(prev => ({ ...prev, isLoading: true }));
        try {
            const sessionId = platform === 'mobile'
                ? state.session?.sessionId || undefined
                : undefined;
            // Call logout endpoint
            await apiService.logout(platform, sessionId);
            // For mobile, clear stored tokens
            if (platform === 'mobile') {
                await SessionStorage.clearSession();
            }
            setState({ session: null, isLoading: false, error: null });
        }
        catch (error) {
            // Even if logout fails on server, clear local session
            if (platform === 'mobile') {
                await SessionStorage.clearSession();
            }
            setState({ session: null, isLoading: false, error: null });
        }
    }, [platform, state.session?.sessionId, apiService]);
    /**
     * Clear error function
     */
    const clearError = React.useCallback(() => {
        setState(prev => ({ ...prev, error: null }));
    }, []);
    return {
        session: state.session,
        isLoading: state.isLoading,
        error: state.error,
        login,
        logout,
        clearError,
        isAuthenticated: !!state.session,
    };
}

// Web-specific authentication hook
/**
 * Hook for web authentication
 * Uses HttpOnly cookies for session management
 */
const useWebAuth = (apiBaseUrl) => {
    const authConfig = React.useMemo(() => ({
        platform: 'web',
        apiBaseUrl,
    }), [apiBaseUrl]);
    return useAuth(authConfig);
};

// Mobile/Native-specific authentication hook
/**
 * Hook for mobile/native authentication
 * Uses SecureStore/AsyncStorage for session management
 */
const useNativeAuth = (apiBaseUrl) => {
    const authConfig = React.useMemo(() => ({
        platform: 'mobile',
        apiBaseUrl,
    }), [apiBaseUrl]);
    return useAuth(authConfig);
};

/**
 * ProtectedRoute component
 * Protects routes that require authentication
 * Works only for web applications (uses useWebAuth)
 */
const ProtectedRoute = ({ children, fallback, redirectTo = '/login' }) => {
    const { isAuthenticated, isLoading } = useWebAuth();
    // Show loading state while checking authentication
    if (isLoading) {
        return (jsxRuntime.jsx("div", { style: {
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
                height: '100vh'
            }, children: jsxRuntime.jsx("div", { children: "Cargando..." }) }));
    }
    // If not authenticated, show fallback or redirect
    if (!isAuthenticated) {
        if (fallback) {
            return jsxRuntime.jsx(jsxRuntime.Fragment, { children: fallback });
        }
        // Redirect to login page
        if (typeof window !== 'undefined') {
            window.location.href = redirectTo;
        }
        return (jsxRuntime.jsx("div", { style: {
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
                height: '100vh'
            }, children: jsxRuntime.jsx("div", { children: "Redirigiendo al login..." }) }));
    }
    // User is authenticated, render children
    return jsxRuntime.jsx(jsxRuntime.Fragment, { children: children });
};

/**
 * Formatea una fecha en formato legible
 * @param dateString - String de fecha ISO
 * @returns Fecha formateada
 */
const formatDate = (dateString) => {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('es-ES', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
    }).format(date);
};
/**
 * Formatea un valor de sensor con su unidad
 * @param value - Valor numérico
 * @param unit - Unidad de medida
 * @returns Valor formateado con unidad
 */
const formatSensorValue = (value, unit) => {
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
const truncateText = (text, maxLength) => {
    if (text.length <= maxLength)
        return text;
    return `${text.substring(0, maxLength)}...`;
};
/**
 * Formatea un número como moneda
 * @param amount - Cantidad
 * @param currency - Código de moneda
 * @returns Cantidad formateada como moneda
 */
const formatCurrency = (amount, currency = 'COP') => {
    return new Intl.NumberFormat('es-CO', {
        style: 'currency',
        currency,
        minimumFractionDigits: 0,
    }).format(amount);
};

/**
 * Valida un correo electrónico
 * @param email - Correo electrónico a validar
 * @returns true si el correo es válido, false en caso contrario
 */
const isValidEmail = (email) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
};
/**
 * Valida una contraseña según criterios de seguridad
 * @param password - Contraseña a validar
 * @returns Objeto con resultado y mensaje de error si aplica
 */
const validatePassword = (password) => {
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
const isValidSensorValue = (value, sensorType) => {
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
const isFutureDate = (dateString) => {
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
const validateLoginCredentials = (email, password) => {
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

// Design tokens de colores extraídos de la aplicación web
// Basado en globals.css y shared/styles/colors.ts
const colors = {
    // Paleta principal HydroEspinaca
    primary: {
        50: '#f0fdf4',
        100: '#dcfce7',
        500: '#22c55e',
        600: '#16a34a',
        700: '#15803d',
        800: '#166534', // hidro-green-primary
        900: '#14532d'
    },
    // Grises del sistema
    gray: {
        50: '#f9fafb',
        100: '#f3f4f6',
        200: '#e5e7eb',
        300: '#d1d5db',
        400: '#9ca3af',
        500: '#6b7280', // hidro-gray
        600: '#4b5563',
        700: '#374151',
        800: '#1f2937',
        900: '#111827'
    },
    // Estados de feedback
    success: '#10b981',
    warning: '#f59e0b',
    error: '#ef4444',
    info: '#3b82f6',
    // Colores específicos Hidro
    hidro: {
        primary: '#166534',
        light: '#BEEEBE',
        bg: '#dcfce7',
        bgLight: '#f0fdf4',
        bgPale: '#f8fffe', // Verde pálido muy suave para fondos
        hover: '#A8E6A8'
    },
    // Colores base
    white: '#ffffff',
    black: '#000000',
    transparent: 'transparent',
    // Overlay
    overlay: 'rgba(0, 0, 0, 0.5)'
};
// Colores semánticos para uso en componentes
const semanticColors = {
    // Texto
    textPrimary: colors.gray[900],
    textSecondary: colors.gray[600],
    textMuted: colors.gray[500],
    textInverse: colors.white,
    textPlaceholder: colors.gray[400],
    textDisabled: colors.gray[400],
    // Fondos
    background: colors.white,
    backgroundSecondary: colors.gray[50],
    backgroundMuted: colors.gray[100],
    backgroundPrimary: colors.white, // Cambiado de colors.primary[600] a colors.white para mejor accesibilidad
    // Superficies
    surface: colors.white,
    surfaceElevated: colors.white,
    // Bordes
    border: colors.gray[200],
    borderMuted: colors.gray[100],
    borderFocus: colors.primary[600],
    borderDisabled: colors.gray[300],
    // Colores primarios
    primary: colors.primary[600],
    // Estados
    successText: colors.primary[600],
    successBg: colors.primary[50],
    warningText: colors.warning,
    warningBg: '#fef3c7',
    errorText: colors.error,
    errorBg: '#fef2f2',
    infoText: colors.info,
    infoBg: '#eff6ff',
    destructiveText: colors.error,
    destructiveBg: '#fef2f2'
};

// Design tokens de tipografía extraídos de la aplicación web
// Basado en globals.css y componentes existentes
const typography = {
    // Familias de fuentes
    fontFamily: {
        primary: 'Inter',
        mono: 'Consolas',
        icons: 'Material Icons'
    },
    // Tamaños de fuente (en px para web, se convertirán a sp en RN)
    fontSize: {
        xs: 10, // text-[10px] en bottom nav
        sm: 12, // text-xs
        base: 14, // text-sm
        md: 16, // text-base
        lg: 18, // text-lg
        xl: 20, // text-xl
        '2xl': 24,
        '3xl': 30,
        '4xl': 36,
        '5xl': 48
    },
    // Pesos de fuente
    fontWeight: {
        normal: '400',
        medium: '500',
        semibold: '600',
        bold: '700',
        extrabold: '800'
    },
    // Altura de línea
    lineHeight: {
        tight: 1.25,
        normal: 1.5,
        relaxed: 1.75,
        loose: 2
    },
    // Espaciado de letras
    letterSpacing: {
        tighter: '-0.05em',
        tight: '-0.025em',
        normal: '0em',
        wide: '0.025em',
        wider: '0.05em',
        widest: '0.1em'
    }
};
// Estilos de texto predefinidos basados en la web
const textStyles = {
    // Headings
    h1: {
        fontSize: typography.fontSize['4xl'],
        fontWeight: typography.fontWeight.bold,
        lineHeight: typography.lineHeight.tight
    },
    h2: {
        fontSize: typography.fontSize['3xl'],
        fontWeight: typography.fontWeight.bold,
        lineHeight: typography.lineHeight.tight
    },
    h3: {
        fontSize: typography.fontSize['2xl'],
        fontWeight: typography.fontWeight.semibold,
        lineHeight: typography.lineHeight.tight
    },
    h4: {
        fontSize: typography.fontSize.xl,
        fontWeight: typography.fontWeight.semibold,
        lineHeight: typography.lineHeight.normal
    },
    h5: {
        fontSize: typography.fontSize.lg,
        fontWeight: typography.fontWeight.semibold,
        lineHeight: typography.lineHeight.normal
    },
    h6: {
        fontSize: typography.fontSize.md,
        fontWeight: typography.fontWeight.semibold,
        lineHeight: typography.lineHeight.normal
    },
    // Body text
    body: {
        fontSize: typography.fontSize.base,
        fontWeight: typography.fontWeight.normal,
        lineHeight: typography.lineHeight.normal
    },
    bodyLarge: {
        fontSize: typography.fontSize.md,
        fontWeight: typography.fontWeight.normal,
        lineHeight: typography.lineHeight.normal
    },
    bodySmall: {
        fontSize: typography.fontSize.sm,
        fontWeight: typography.fontWeight.normal,
        lineHeight: typography.lineHeight.normal
    },
    // Labels
    label: {
        fontSize: typography.fontSize.sm,
        fontWeight: typography.fontWeight.semibold,
        lineHeight: typography.lineHeight.normal
    },
    labelLarge: {
        fontSize: typography.fontSize.base,
        fontWeight: typography.fontWeight.semibold,
        lineHeight: typography.lineHeight.normal
    },
    // Caption
    caption: {
        fontSize: typography.fontSize.xs,
        fontWeight: typography.fontWeight.normal,
        lineHeight: typography.lineHeight.normal
    },
    // Button text
    button: {
        fontSize: typography.fontSize.base,
        fontWeight: typography.fontWeight.medium,
        lineHeight: typography.lineHeight.normal
    },
    buttonSmall: {
        fontSize: typography.fontSize.sm,
        fontWeight: typography.fontWeight.medium,
        lineHeight: typography.lineHeight.normal
    },
    buttonLarge: {
        fontSize: typography.fontSize.lg,
        fontWeight: typography.fontWeight.medium,
        lineHeight: typography.lineHeight.normal
    },
    // Navigation
    navItem: {
        fontSize: typography.fontSize.xs,
        fontWeight: typography.fontWeight.medium,
        lineHeight: typography.lineHeight.tight
    }
};

// Design tokens de espaciado extraídos de la aplicación web
// Sistema basado en múltiplos de 4px (escala 4/8)
const spacing = {
    // Espaciado base (múltiplos de 4px)
    xs: 4, // 0.25rem
    sm: 8, // 0.5rem
    md: 12, // 0.75rem
    lg: 16, // 1rem
    xl: 20, // 1.25rem
    '2xl': 24, // 1.5rem
    '3xl': 32, // 2rem
    '4xl': 40, // 2.5rem
    '5xl': 48, // 3rem
    '6xl': 64, // 4rem
    '7xl': 80, // 5rem
    '8xl': 96 // 6rem
};
// Espaciado específico para componentes
const componentSpacing = {
    // Padding interno de componentes
    buttonPadding: {
        sm: { horizontal: spacing.md, vertical: spacing.xs },
        md: { horizontal: spacing.lg, vertical: spacing.sm },
        lg: { horizontal: spacing['2xl'], vertical: spacing.md }
    },
    // Padding de cards (basado en hidro-card)
    cardPadding: {
        sm: spacing.lg, // p-4
        md: spacing['2xl'], // p-6 (default)
        lg: spacing['3xl'] // p-8
    },
    // Espaciado de formularios
    formSpacing: {
        fieldGap: spacing.lg, // Espacio entre campos
        labelGap: spacing.sm, // Espacio entre label e input
        sectionGap: spacing['2xl'] // Espacio entre secciones
    },
    // Espaciado de listas
    listSpacing: {
        itemGap: spacing.sm, // Espacio entre items
        sectionGap: spacing.lg // Espacio entre secciones
    },
    // Espaciado de navegación
    navigationSpacing: {
        tabPadding: spacing.xs, // Padding interno de tabs
        tabGap: spacing.xs, // Espacio entre tabs
        headerPadding: spacing.lg // Padding de headers
    }
};
// Border radius (basado en Tailwind y componentes web)
const borderRadius = {
    none: 0,
    sm: 4, // rounded-sm
    md: 6, // rounded-md (default)
    lg: 8, // rounded-lg (hidro-card)
    xl: 12, // rounded-xl
    '2xl': 16,
    '3xl': 24,
    full: 9999 // rounded-full
};
// Elevaciones/sombras (equivalentes a shadow de Tailwind)
const elevation = {
    none: {
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 0 },
        shadowOpacity: 0,
        shadowRadius: 0,
        elevation: 0
    },
    sm: {
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 1 },
        shadowOpacity: 0.05,
        shadowRadius: 2,
        elevation: 1
    },
    md: {
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 4 },
        shadowOpacity: 0.1,
        shadowRadius: 6,
        elevation: 3
    },
    lg: {
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 10 },
        shadowOpacity: 0.15,
        shadowRadius: 15,
        elevation: 6
    },
    xl: {
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 20 },
        shadowOpacity: 0.25,
        shadowRadius: 25,
        elevation: 10
    }
};
// Z-index para layering
const zIndex = {
    base: 0,
    dropdown: 1000,
    sticky: 1020,
    fixed: 1030,
    modal: 1040,
    popover: 1050,
    tooltip: 1060,
    toast: 1070
};

// Exportación centralizada de todos los design tokens
// Para uso en aplicaciones web y móvil
// Tema base que combina todos los tokens
const baseTokens = {
    colors: {
        primary: '#166534',
        secondary: '#6b7280',
        success: '#10b981',
        warning: '#f59e0b',
        error: '#ef4444',
        info: '#3b82f6',
        background: '#ffffff',
        surface: '#ffffff',
        text: '#111827'
    },
    spacing: {
        xs: 4,
        sm: 8,
        md: 16,
        lg: 24,
        xl: 32
    },
    typography: {
        fontFamily: 'Inter',
        fontSize: {
            sm: 12,
            md: 14,
            lg: 16,
            xl: 18
        }
    },
    borderRadius: {
        sm: 4,
        md: 8,
        lg: 12
    }
};

// API Placeholder Functions
// These functions serve as placeholders for future API integration
// Replace hardcoded data with actual API calls when backend is ready
// ============================================================================
// READINGS API PLACEHOLDERS
// ============================================================================
/**
 * Fetch sensor summary data from API
 * @returns Promise<SensorSummary[]>
 */
async function fetchSensorSummary() {
    // TODO: Replace with actual API call
    // return await fetch('/api/sensors/summary').then(res => res.json());
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve([
                {
                    sensor: 'Temperatura',
                    media: 25.5,
                    minimo: 24.8,
                    maximo: 26.2,
                    unidad: '°C',
                    ultimaLectura: '2024-01-26 10:01:10'
                },
                {
                    sensor: 'Humedad',
                    media: 60.2,
                    minimo: 59.5,
                    maximo: 61.0,
                    unidad: '%',
                    ultimaLectura: '2024-01-26 10:01:00'
                },
                {
                    sensor: 'Intensidad Lumínica',
                    media: 850,
                    minimo: 820,
                    maximo: 880,
                    unidad: 'lux',
                    ultimaLectura: '2024-01-26 10:01:00'
                },
                {
                    sensor: 'Conductividad Eléctrica',
                    media: 45,
                    minimo: 44,
                    maximo: 46,
                    unidad: '%',
                    ultimaLectura: '2024-01-26 10:00:10'
                }
            ]);
        }, 500);
    });
}
/**
 * Fetch individual readings from API
 * @returns Promise<IndividualReading[]>
 */
async function fetchIndividualReadings() {
    // TODO: Replace with actual API call
    // return await fetch('/api/readings').then(res => res.json());
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve([
                { id: '1', fecha: '2024-01-26 10:00:00', sensor: 'Temperatura', valor: 25.2, unidad: '°C' },
                { id: '2', fecha: '2024-01-26 10:00:00', sensor: 'Humedad', valor: 60.1, unidad: '%' },
                { id: '3', fecha: '2024-01-26 10:00:00', sensor: 'Intensidad Lumínica', valor: 845, unidad: 'lux' },
                { id: '4', fecha: '2024-01-26 10:00:00', sensor: 'Conductividad Eléctrica', valor: 44, unidad: '%' },
                { id: '5', fecha: '2024-01-26 10:00:10', sensor: 'Temperatura', valor: 25.3, unidad: '°C' },
                { id: '6', fecha: '2024-01-26 10:00:10', sensor: 'Humedad', valor: 60.3, unidad: '%' },
                { id: '7', fecha: '2024-01-26 10:00:10', sensor: 'Intensidad Lumínica', valor: 855, unidad: 'lux' },
                { id: '8', fecha: '2024-01-26 10:00:10', sensor: 'Conductividad Eléctrica', valor: 46, unidad: '%' },
                { id: '9', fecha: '2024-01-26 10:00:20', sensor: 'Temperatura', valor: 25.5, unidad: '°C' },
                { id: '10', fecha: '2024-01-26 10:00:20', sensor: 'Humedad', valor: 60.5, unidad: '%' },
                { id: '11', fecha: '2024-01-26 10:00:30', sensor: 'Temperatura', valor: 25.4, unidad: '°C' },
                { id: '12', fecha: '2024-01-26 10:00:30', sensor: 'Humedad', valor: 60.2, unidad: '%' },
                { id: '13', fecha: '2024-01-26 10:00:30', sensor: 'Intensidad Lumínica', valor: 860, unidad: 'lux' },
                { id: '14', fecha: '2024-01-26 10:00:40', sensor: 'Temperatura', valor: 25.6, unidad: '°C' },
                { id: '15', fecha: '2024-01-26 10:00:40', sensor: 'Humedad', valor: 60.4, unidad: '%' },
                { id: '16', fecha: '2024-01-26 10:00:50', sensor: 'Temperatura', valor: 25.3, unidad: '°C' },
                { id: '17', fecha: '2024-01-26 10:01:00', sensor: 'Temperatura', valor: 25.7, unidad: '°C' },
                { id: '18', fecha: '2024-01-26 10:01:00', sensor: 'Humedad', valor: 60.6, unidad: '%' },
                { id: '19', fecha: '2024-01-26 10:01:00', sensor: 'Intensidad Lumínica', valor: 865, unidad: 'lux' },
                { id: '20', fecha: '2024-01-26 10:01:10', sensor: 'Temperatura', valor: 25.8, unidad: '°C' }
            ]);
        }, 500);
    });
}
// ============================================================================
// ACTUATORS API PLACEHOLDERS
// ============================================================================
/**
 * Fetch actuators data from API
 * @returns Promise<ActuadorData[]>
 */
async function fetchActuators() {
    // TODO: Replace with actual API call
    // return await fetch('/api/actuators').then(res => res.json());
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve([
                {
                    id: '1',
                    name: 'Bomba de Agua Principal',
                    type: 'pump',
                    location: 'Tanque Principal',
                    pin: 2,
                    esp32Id: 'ESP32_001',
                    status: 'active',
                    createdAt: '2024-01-15',
                    lastModified: '2024-01-20'
                },
                {
                    id: '2',
                    name: 'Ventilador de Circulación',
                    type: 'fan',
                    location: 'Área de Cultivo',
                    pin: 3,
                    esp32Id: 'ESP32_001',
                    status: 'active',
                    createdAt: '2024-01-16',
                    lastModified: '2024-01-18'
                },
                {
                    id: '3',
                    name: 'LED de Crecimiento',
                    type: 'light',
                    location: 'Área de Cultivo',
                    pin: 4,
                    esp32Id: 'ESP32_002',
                    status: 'inactive',
                    createdAt: '2024-01-10',
                    lastModified: '2024-01-15'
                }
            ]);
        }, 500);
    });
}
/**
 * Create new actuator via API
 * @param actuator ActuadorData
 * @returns Promise<ActuadorData>
 */
async function createActuator(actuator) {
    // TODO: Replace with actual API call
    // return await fetch('/api/actuators', {
    //   method: 'POST',
    //   headers: { 'Content-Type': 'application/json' },
    //   body: JSON.stringify(actuator)
    // }).then(res => res.json());
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve({
                id: Date.now().toString(),
                ...actuator
            });
        }, 500);
    });
}
/**
 * Update actuator via API
 * @param id string
 * @param actuator Partial<ActuadorData>
 * @returns Promise<ActuadorData>
 */
async function updateActuator(id, actuator) {
    // TODO: Replace with actual API call
    // return await fetch(`/api/actuators/${id}`, {
    //   method: 'PUT',
    //   headers: { 'Content-Type': 'application/json' },
    //   body: JSON.stringify(actuator)
    // }).then(res => res.json());
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve({
                id,
                name: 'Actuador Actualizado',
                type: 'pump',
                location: 'Ubicación Actualizada',
                pin: 1,
                esp32Id: 'ESP32_001',
                status: 'active',
                createdAt: '2024-01-01',
                lastModified: new Date().toISOString().split('T')[0],
                ...actuator
            });
        }, 500);
    });
}
/**
 * Delete actuator via API
 * @param id string
 * @returns Promise<void>
 */
async function deleteActuator(id) {
    // TODO: Replace with actual API call
    // return await fetch(`/api/actuators/${id}`, { method: 'DELETE' });
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve();
        }, 500);
    });
}
// ============================================================================
// SENSORS API PLACEHOLDERS
// ============================================================================
/**
 * Fetch sensor data for monitoring dashboard
 * @returns Promise<SensorData[]>
 */
async function fetchSensorData() {
    // TODO: Replace with actual API call
    // return await fetch('/api/sensors/data').then(res => res.json());
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            const now = new Date();
            const data = [];
            for (let i = 0; i < 60; i++) {
                const timestamp = new Date(now.getTime() - i * 60000); // 1 minute intervals
                data.push({
                    timestamp: timestamp.toISOString(),
                    temperature: 24 + Math.random() * 4, // 24-28°C
                    humidity: 60 + Math.random() * 20, // 60-80%
                    ph: 6.0 + Math.random() * 1.5, // 6.0-7.5
                    light: 800 + Math.random() * 200, // 800-1000 lux
                    conductivity: 1.0 + Math.random() * 0.5 // 1.0-1.5 mS/cm
                });
            }
            resolve(data.reverse());
        }, 500);
    });
}
/**
 * Fetch metrics data for dashboard
 * @returns Promise<MetricData[]>
 */
async function fetchMetrics() {
    // TODO: Replace with actual API call
    // return await fetch('/api/metrics').then(res => res.json());
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve([
                {
                    title: 'Temperatura',
                    value: '25.2',
                    unit: '°C',
                    status: 'optimal',
                    trend: 'up',
                    change: '+0.5',
                    iconType: 'temperature'
                },
                {
                    title: 'Humedad',
                    value: '68.5',
                    unit: '%',
                    status: 'optimal',
                    trend: 'stable',
                    change: '+0.1',
                    iconType: 'humidity'
                },
                {
                    title: 'pH',
                    value: '6.8',
                    unit: '',
                    status: 'warning',
                    trend: 'down',
                    change: '-0.2',
                    iconType: 'ph'
                },
                {
                    title: 'Luz',
                    value: '850',
                    unit: 'lux',
                    status: 'optimal',
                    trend: 'up',
                    change: '+25',
                    iconType: 'sun'
                }
            ]);
        }, 500);
    });
}
// ============================================================================
// VARIABLES API PLACEHOLDERS
// ============================================================================
/**
 * Fetch variables configuration from API
 * @returns Promise<VariableData[]>
 */
async function fetchVariables() {
    // TODO: Replace with actual API call
    // return await fetch('/api/variables').then(res => res.json());
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve([
                {
                    id: '1',
                    name: 'Temperatura Óptima',
                    description: 'Rango de temperatura ideal para el crecimiento de espinacas hidropónicas',
                    unit: '°C',
                    type: 'input',
                    dataType: 'numeric',
                    minValue: 22,
                    maxValue: 26,
                    isRequired: true,
                    category: 'environmental',
                    status: 'active',
                    createdAt: '2024-01-15',
                    lastModified: '2024-01-20'
                },
                {
                    id: '2',
                    name: 'Humedad Relativa',
                    description: 'Nivel de humedad óptimo para prevenir enfermedades y promover crecimiento',
                    unit: '%',
                    type: 'input',
                    dataType: 'numeric',
                    minValue: 65,
                    maxValue: 75,
                    isRequired: true,
                    category: 'environmental',
                    status: 'active',
                    createdAt: '2024-01-16',
                    lastModified: '2024-01-18'
                },
                {
                    id: '3',
                    name: 'pH del Agua',
                    description: 'Rango de pH ideal para la absorción de nutrientes',
                    unit: '',
                    type: 'input',
                    dataType: 'numeric',
                    minValue: 6.0,
                    maxValue: 7.0,
                    isRequired: true,
                    category: 'environmental',
                    status: 'active',
                    createdAt: '2024-01-10',
                    lastModified: '2024-01-15'
                }
            ]);
        }, 500);
    });
}
/**
 * Create new variable via API
 * @param variable VariableData
 * @returns Promise<VariableData>
 */
async function createVariable(variable) {
    // TODO: Replace with actual API call
    // return await fetch('/api/variables', {
    //   method: 'POST',
    //   headers: { 'Content-Type': 'application/json' },
    //   body: JSON.stringify(variable)
    // }).then(res => res.json());
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve({
                id: Date.now().toString(),
                ...variable
            });
        }, 500);
    });
}
/**
 * Update variable via API
 * @param id string
 * @param variable Partial<VariableData>
 * @returns Promise<VariableData>
 */
async function updateVariable(id, variable) {
    // TODO: Replace with actual API call
    // return await fetch(`/api/variables/${id}`, {
    //   method: 'PUT',
    //   headers: { 'Content-Type': 'application/json' },
    //   body: JSON.stringify(variable)
    // }).then(res => res.json());
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve({
                id,
                name: 'Variable Actualizada',
                description: 'Descripción actualizada',
                unit: '°C',
                type: 'input',
                dataType: 'numeric',
                minValue: 20,
                maxValue: 30,
                isRequired: true,
                category: 'environmental',
                status: 'active',
                createdAt: '2024-01-01',
                lastModified: new Date().toISOString().split('T')[0],
                ...variable
            });
        }, 500);
    });
}
/**
 * Delete variable via API
 * @param id string
 * @returns Promise<void>
 */
async function deleteVariable(id) {
    // TODO: Replace with actual API call
    // return await fetch(`/api/variables/${id}`, { method: 'DELETE' });
    // Placeholder implementation
    return new Promise((resolve) => {
        setTimeout(() => {
            resolve();
        }, 500);
    });
}

const getWelcomeMessage = ({ userName, appName = 'Hidroespinaca' } = {}) => {
    if (userName) {
        return `¡Bienvenido/a ${userName} a ${appName}!`;
    }
    return `¡Bienvenido/a a ${appName}!`;
};
const getAppInfo = () => {
    return {
        name: 'Hidroespinaca',
        version: '1.0.0',
        description: 'Sistema de monitoreo hidropónico inteligente'
    };
};

// Rutas SVG centralizadas para todos los iconos
// Estas rutas son compatibles con viewBox="0 0 24 24"
const svgPaths = {
    home: 'M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z M9 22V12h6v10',
    settings: 'M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z',
    user: 'M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2 M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
    close: 'M18 6L6 18 M6 6l12 12',
    menu: 'M3 12h18 M3 6h18 M3 18h18',
    search: 'M21 21l-6-6m2-5a7 7 0 1 1-14 0 7 7 0 0 1 14 0z',
    plus: 'M12 5v14 M5 12h14',
    minus: 'M5 12h14',
    check: 'M20 6L9 17l-5-5',
    'arrow-left': 'M19 12H5 M12 19l-7-7 7-7',
    'arrow-right': 'M5 12h14 M12 5l7 7-7 7',
    heart: 'M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z',
    star: 'M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z',
    bell: 'M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9 M13.73 21a2 2 0 0 1-3.46 0',
    mail: 'M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z M22 6l-10 7L2 6',
    phone: 'M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z',
    camera: 'M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z M12 17a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
    edit: 'M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7 M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z',
    delete: 'M3 6h18 M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2 M10 11v6 M14 11v6',
    save: 'M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z M17 21v-8H7v8 M7 3v5h8',
    refresh: 'M23 4v6h-6 M1 20v-6h6 M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4l-4.64 4.36A9 9 0 0 1 3.51 15',
    download: 'M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4 M7 10l5 5 5-5 M12 15V3',
    upload: 'M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4 M17 8l-5-5-5 5 M12 3v12',
    lock: 'M19 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2z M7 11V7a5 5 0 0 1 10 0v4',
    unlock: 'M19 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2z M7 11V7a5 5 0 0 1 9.9-1',
    eye: 'M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z M12 16a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
    'eye-off': 'M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24 M1 1l22 22',
    calendar: 'M19 3h-1V1h-2v2H8V1H6v2H5c-1.11 0-1.99.9-1.99 2L3 19c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V8h14v11zM7 10h5v5H7z',
    clock: 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 6v6l4 2',
    location: 'M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z M12 13a3 3 0 1 0 0-6 3 3 0 0 0 0 6z',
    wifi: 'M5 12.55a11 11 0 0 1 14.08 0 M1.42 9a16 16 0 0 1 21.16 0 M8.53 16.11a6 6 0 0 1 6.95 0 M12 20h.01',
    battery: 'M1 6v12h5l2 2h8l2-2h5V6H1zm4 10V8h14v8H5z M23 10v4',
    power: 'M12 2v10 M18.4 6.6a9 9 0 1 1-12.77.04',
    warning: 'M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z M12 9v4 M12 17h.01',
    info: 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 8v4 M12 16h.01',
    error: 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M15 9l-6 6 M9 9l6 6',
    success: 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M9 12l2 2 4-4',
    // Pagination icons
    'chevron-back': 'M15.75 19.5L8.25 12l7.5-7.5',
    'chevron-forward': 'M8.25 4.5l7.5 7.5-7.5 7.5',
    'play-skip-back': 'M5.25 5.25v13.5m7.5-13.5v13.5L21 12l-8.25-6.75z',
    'play-skip-forward': 'M18.75 18.75v-13.5m-7.5 13.5v-13.5L3 12l8.25 6.75z',
    // Iconos específicos del menú de navegación
    'chart': 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z',
    'book': 'M4 19.5A2.5 2.5 0 0 1 6.5 17H20M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z',
    'trending-up': 'M23 6 13.5 15.5 8.5 10.5 1 18M17 6h6v6',
    'brain': 'M9.5 2A2.5 2.5 0 0 1 12 4.5v15a2.5 2.5 0 0 1-4.96.44 2.5 2.5 0 0 1-2.96-3.08 3 3 0 0 1-.34-5.58 2.5 2.5 0 0 1 1.32-4.24 2.5 2.5 0 0 1 1.98-3A2.5 2.5 0 0 1 9.5 2ZM14.5 2A2.5 2.5 0 0 0 12 4.5v15a2.5 2.5 0 0 0 4.96.44 2.5 2.5 0 0 0 2.96-3.08 3 3 0 0 0 .34-5.58 2.5 2.5 0 0 0-1.32-4.24 2.5 2.5 0 0 0-1.98-3A2.5 2.5 0 0 0 14.5 2Z',
    // Iconos adicionales
    'temperature': 'M14 14.76V3.5a2.5 2.5 0 0 0-5 0v11.26a4.5 4.5 0 1 0 5 0zM12 17a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3z',
    // Iconos específicos para variables de cultivo
    'humidity': 'M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0L12 2.69z M9 10a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M15 16a1 1 0 1 0 0-2 1 1 0 0 0 0 2z M10.5 8.5l4 4',
    'ph': 'M10 2h4a1 1 0 0 1 1 1v16a1 1 0 0 1-1 1h-4a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1z M9 19h6 M12 19v2 M10 4h4v8a2 2 0 0 1-2 2 2 2 0 0 1-2-2V4z M11 6h2v2h-2V6z M11 9h2v1h-2V9z',
    'sun': 'M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42M12 17a5 5 0 1 0 0-10 5 5 0 0 0 0 10z',
    'electric': 'M13 2L3 14h9l-1 8 10-12h-9l1-8z',
    'light': 'M9 2h6l2 2v1a9 9 0 1 1-10 0V4l2-2z M12 7a5 5 0 1 0 0 10 5 5 0 0 0 0-10z M8 1h8v1H8V1z',
    'ruler': 'M21.71 2.29a1 1 0 0 0-1.42 0L2.29 20.29a1 1 0 0 0 0 1.42 1 1 0 0 0 1.42 0L21.71 3.71a1 1 0 0 0 0-1.42zM7 7l1.5 1.5L7 10l-1.5-1.5L7 7zM10 10l1.5 1.5L10 13l-1.5-1.5L10 10zM13 13l1.5 1.5L13 16l-1.5-1.5L13 13z',
    'water': 'M6 4h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z M4 14h16 M6 16h2v2H6v-2z M10 16h2v2h-2v-2z M14 16h2v2h-2v-2z M7 12h1v1H7v-1z M9 11h1v1H9v-1z M11 12h1v1h-1v-1z M13 11h1v1h-1v-1z M15 12h1v1h-1v-1z'
};

const Icon = ({ name, size = 24, color = 'currentColor', stroke, strokeWidth = 2, fill = 'none', platform = 'web', // Por defecto web para compatibilidad
...props }) => {
    const pathData = svgPaths[name];
    if (!pathData) {
        console.warn(`Icon "${name}" not found`);
        return null;
    }
    // Para React Native (mobile)
    if (platform === 'mobile') {
        try {
            // Importar dinámicamente react-native-svg solo cuando sea necesario
            const { Svg, Path } = require('react-native-svg');
            return React.createElement(Svg, {
                width: size,
                height: size,
                viewBox: '0 0 24 24',
                fill: fill,
                ...props
            }, React.createElement(Path, {
                d: pathData,
                stroke: stroke || color,
                strokeWidth: strokeWidth,
                strokeLinecap: 'round',
                strokeLinejoin: 'round',
                fill: fill
            }));
        }
        catch (error) {
            // Fallback si react-native-svg no está disponible
            try {
                const { Text } = require('react-native');
                return React.createElement(Text, {
                    style: {
                        fontSize: size,
                        color: stroke || color,
                        textAlign: 'center',
                        ...props.style
                    },
                    ...props
                }, '□'); // Símbolo simple como fallback
            }
            catch {
                return null;
            }
        }
    }
    // Para web, usar SVG estándar
    return (jsxRuntime.jsx("svg", { width: size, height: size, viewBox: "0 0 24 24", fill: fill, stroke: stroke || color, strokeWidth: strokeWidth, strokeLinecap: "round", strokeLinejoin: "round", ...props, children: jsxRuntime.jsx("path", { d: pathData }) }));
};

const navigationConfig = [
    {
        id: 'info-main',
        href: '/dashboard',
        label: 'Info-Main',
        iconName: 'chart',
        order: 1,
        isMainTab: true
    },
    {
        id: 'lecturas',
        href: '/dashboard/lecturas',
        label: 'Lecturas',
        iconName: 'book',
        order: 2,
        isMainTab: true
    },
    {
        id: 'dashboard',
        href: '/dashboard-monitoreo',
        label: 'Dashboard',
        iconName: 'trending-up',
        order: 3,
        isMainTab: true
    },
    {
        id: 'logica-fuzzy',
        href: '/dashboard/logica-fuzzy',
        label: 'Lógica Fuzzy',
        iconName: 'brain',
        order: 4,
        isMainTab: true
    },
    {
        id: 'crear-variable',
        href: '/dashboard/nueva-variable',
        label: 'Crear Variable',
        iconName: 'plus',
        order: 5,
        isMainTab: true
    },
    {
        id: 'mas',
        href: '#',
        label: 'Más',
        iconName: 'menu',
        order: 6,
        isMainTab: true
    }
];
// Función para obtener solo las pestañas principales
const getMainTabs = () => {
    return navigationConfig.filter(item => item.isMainTab).sort((a, b) => a.order - b.order);
};
// Función para obtener un elemento por ID
const getNavigationItem = (id) => {
    return navigationConfig.find(item => item.id === id);
};

const drawerMenuConfig = [
    {
        id: 'info-main',
        label: 'Info-Main',
        iconName: 'chart',
        href: '/dashboard'
    },
    {
        id: 'lecturas',
        label: 'Lecturas',
        iconName: 'book',
        href: '/dashboard/lecturas'
    },
    {
        id: 'dashboard',
        label: 'Dashboard',
        iconName: 'trending-up',
        href: '/dashboard-monitoreo'
    },
    {
        id: 'logica-fuzzy',
        label: 'Lógica Fuzzy',
        iconName: 'brain',
        href: '/dashboard/logica-fuzzy'
    },
    {
        id: 'crear-variable',
        label: 'Crear Variable Manual',
        iconName: 'plus',
        href: '/dashboard/nueva-variable'
    },
    {
        id: 'listas',
        label: 'Listas',
        iconName: 'menu',
        isExpandable: true,
        children: [
            {
                id: 'variables',
                label: 'Variables',
                iconName: 'settings',
                href: '/dashboard/variables-config'
            },
            {
                id: 'sensores',
                label: 'Sensores',
                iconName: 'settings',
                href: '/dashboard/sensores-config'
            },
            {
                id: 'actuadores',
                label: 'Actuadores',
                iconName: 'settings',
                href: '/dashboard/actuadores-config'
            },
            {
                id: 'reglas-fuzzy',
                label: 'Reglas Fuzzy',
                iconName: 'brain',
                href: '/dashboard/reglas-fuzzy'
            }
        ]
    },
    {
        id: 'estado-sistema',
        label: 'Estado del Sistema',
        iconName: 'chart',
        isExpandable: true,
        children: [
            {
                id: 'api-principal',
                label: 'API Principal',
                iconName: 'wifi',
                href: '/dashboard/sistema/api',
                status: 'connected'
            },
            {
                id: 'base-datos',
                label: 'Base de Datos',
                iconName: 'wifi',
                href: '/dashboard/sistema/database',
                status: 'connected'
            },
            {
                id: 'sensores-iot',
                label: 'Sensores IoT',
                iconName: 'wifi',
                href: '/dashboard/sistema/sensors',
                status: 'disconnected'
            }
        ]
    },
    {
        id: 'ajustes',
        label: 'Ajustes',
        iconName: 'settings',
        href: '/dashboard/ajustes'
    },
    {
        id: 'historial',
        label: 'Historial',
        iconName: 'clock',
        href: '/dashboard/historial'
    }
];
// Función para obtener un elemento del drawer por ID
const getDrawerMenuItem = (id) => {
    const findItem = (items) => {
        for (const item of items) {
            if (item.id === id)
                return item;
            if (item.children) {
                const found = findItem(item.children);
                if (found)
                    return found;
            }
        }
        return undefined;
    };
    return findItem(drawerMenuConfig);
};
// Función para obtener todos los elementos expandibles
const getExpandableItems = () => {
    return drawerMenuConfig.filter(item => item.isExpandable);
};
// Función para obtener elementos con estado de conexión
const getItemsWithStatus = () => {
    const itemsWithStatus = [];
    const findItemsWithStatus = (items) => {
        items.forEach(item => {
            if (item.status) {
                itemsWithStatus.push(item);
            }
            if (item.children) {
                findItemsWithStatus(item.children);
            }
        });
    };
    findItemsWithStatus(drawerMenuConfig);
    return itemsWithStatus;
};

exports.ApiError = ApiError;
exports.AuthApiService = AuthApiService;
exports.CHART_VARIABLES = CHART_VARIABLES;
exports.Icon = Icon;
exports.ProtectedRoute = ProtectedRoute;
exports.SessionStorage = SessionStorage;
exports.authService = authService$1;
exports.baseTokens = baseTokens;
exports.borderRadius = borderRadius;
exports.colors = colors;
exports.componentSpacing = componentSpacing;
exports.createActuator = createActuator;
exports.createVariable = createVariable;
exports.deleteActuator = deleteActuator;
exports.deleteVariable = deleteVariable;
exports.detectPlatform = detectPlatform;
exports.drawerMenuConfig = drawerMenuConfig;
exports.elevation = elevation;
exports.fetchActuators = fetchActuators;
exports.fetchIndividualReadings = fetchIndividualReadings;
exports.fetchMetrics = fetchMetrics;
exports.fetchSensorData = fetchSensorData;
exports.fetchSensorSummary = fetchSensorSummary;
exports.fetchVariables = fetchVariables;
exports.formatCurrency = formatCurrency;
exports.formatDate = formatDate;
exports.formatSensorValue = formatSensorValue;
exports.getApiUrl = getApiUrl;
exports.getAppInfo = getAppInfo;
exports.getDrawerMenuItem = getDrawerMenuItem;
exports.getExpandableItems = getExpandableItems;
exports.getItemsWithStatus = getItemsWithStatus;
exports.getMainTabs = getMainTabs;
exports.getNavigationItem = getNavigationItem;
exports.getWelcomeMessage = getWelcomeMessage;
exports.isDevelopmentMode = isDevelopmentMode;
exports.isFutureDate = isFutureDate;
exports.isValidEmail = isValidEmail;
exports.isValidSensorValue = isValidSensorValue;
exports.navigationConfig = navigationConfig;
exports.secureStorage = secureStorage;
exports.semanticColors = semanticColors;
exports.spacing = spacing;
exports.svgPaths = svgPaths;
exports.textStyles = textStyles;
exports.truncateText = truncateText;
exports.typography = typography;
exports.updateActuator = updateActuator;
exports.updateVariable = updateVariable;
exports.useActuatorStore = useActuatorStore;
exports.useAlertStore = useAlertStore;
exports.useAuth = useAuth;
exports.useAuthStore = useAuthStore;
exports.useDashboardStore = useDashboardStore;
exports.useFuzzyStore = useFuzzyStore;
exports.useLoginForm = useLoginForm;
exports.useNativeAuth = useNativeAuth;
exports.useReadingsStore = useReadingsStore;
exports.useSensorStore = useSensorStore;
exports.useVariableStore = useVariableStore;
exports.useWebAuth = useWebAuth;
exports.validateLoginCredentials = validateLoginCredentials;
exports.validatePassword = validatePassword;
exports.zIndex = zIndex;
//# sourceMappingURL=index.js.map
