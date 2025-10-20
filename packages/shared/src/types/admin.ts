// Admin types matching HydroEspinaca.Shared.DTOs.Authentication

// User DTOs
export interface UserCreateDto {
  username: string;
  email: string;
  password: string;
  roleId?: string | null;
}

export interface UserUpdateDto {
  username?: string | null;
  email?: string | null;
  password?: string | null;
  roleId?: string | null;
}

export interface UserResponseDto {
  id: string;
  username: string;
  email: string;
  roleId?: string | null;
}

// Role DTOs
export interface CreateRoleRequestDto {
  code: string;
  name: string;
  permissionCodes: string[];
}

export interface UpdateRoleRequestDto {
  name: string;
  permissionCodes: string[];
}

export interface RoleResponseDto {
  id: string;
  code: string;
  name: string;
  permissionCodes: string[];
}

// Permission DTOs
export interface CreatePermissionRequestDto {
  code: string;
  name: string;
  description?: string | null;
}

export interface UpdatePermissionRequestDto {
  name: string;
  description?: string | null;
}

export interface PermissionResponseDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
}

export interface GroupedPermissionResponseDto {
  category: string;
  permissions: PermissionResponseDto[];
}

// Session DTOs (matching SessionMonitorDto and UserSessionsDto from backend)
export interface SessionMonitorDto {
  sessionId: string;
  clientId: string;
  createdAt: string;
  expiresAt: string;
  lastActivity: string;
  revoked: boolean;
  revokedAt: string | null;
}

export interface UserSessionsDto {
  userId: string;
  userName: string;
  sessions: SessionMonitorDto[];
}
