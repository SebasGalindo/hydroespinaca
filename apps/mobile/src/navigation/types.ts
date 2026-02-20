/**
 * Definiciones de tipos para la navegación de la app.
 * Centraliza todos los ParamList para type-safety en screens y hooks.
 */

// ─── Root Stack ────────────────────────────────────────────────
export type RootStackParamList = {
  Login: undefined;
  MainTabs: undefined;
};

// ─── Bottom Tabs ───────────────────────────────────────────────
export type MainTabParamList = {
  DashboardTab: undefined;
  AnalyticsTab: undefined;
  BiTab: undefined;
  FuzzyTab: undefined;
  MoreTab: undefined;
};

// ─── Stacks internos por tab ───────────────────────────────────

export type DashboardStackParamList = {
  Dashboard: undefined;
};

export type AnalyticsStackParamList = {
  Analytics: undefined;
};

export type BiStackParamList = {
  Bi: undefined;
};

export type FuzzyStackParamList = {
  FuzzyList: undefined;
  FuzzyDetail: { systemId: string; systemName: string };
};

export type MoreStackParamList = {
  MoreMenu: undefined;
  Profile: undefined;
  AdminAccess: undefined;
  AdminSessions: undefined;
};
