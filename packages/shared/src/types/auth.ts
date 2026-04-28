// Authentication Types
export interface LoginRequest {
  Email: string;
  Password: string;
  AcceptTerms?: boolean;
}

export interface MobileLoginResponse {
  sessionId: string;
  csrfToken: string;
  message?: string;
}

export interface WebLoginResponse {
  message?: string;
}

export interface UserSession {
  userId: string;
  sessionId: string;
  username: string;
  email: string;
  role: string;
  hasAcceptedTerms: boolean;
}

export interface TermsSection {
  title: string;
  content: string;
}

export interface TermsContent {
  title: string;
  lastUpdated: string;
  sections: TermsSection[];
}

export interface Session {
  sessionId: string | null; // null for web (HttpOnly cookie), string for mobile
  csrfToken: string | null; // null for web (cookie), string for mobile
  username: string;
  email: string;
  role: string;
  hasAcceptedTerms: boolean;
}

export interface AuthState {
  session: Session | null;
  isLoading: boolean;
  error: string | null;
}

export interface UseAuthReturn {
  session: Session | null;
  isLoading: boolean;
  error: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  clearError: () => void;
  isAuthenticated: boolean;
}

export type Platform = 'web' | 'mobile';

export interface AuthConfig {
  platform: Platform;
  apiBaseUrl?: string;
}
