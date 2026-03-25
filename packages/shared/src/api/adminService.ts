// Admin API Service — extends BaseApiService for DRY request handling
import { BaseApiService, ApiServiceError } from './BaseApiService';
import type {
  UserCreateDto,
  UserUpdateDto,
  UserResponseDto,
  CreateRoleRequestDto,
  UpdateRoleRequestDto,
  RoleResponseDto,
  CreatePermissionRequestDto,
  UpdatePermissionRequestDto,
  PermissionResponseDto,
  GroupedPermissionResponseDto,
  UserSessionsDto,
} from '../types/admin';

// Re-export ApiServiceError as ApiError for backward compatibility within this file
export { ApiServiceError as ApiError } from './BaseApiService';

// ==================== Service Class ====================

export class AdminApiService extends BaseApiService {

  // ==================== USER MANAGEMENT ====================

  async getAllUsers(): Promise<UserResponseDto[]> {
    return this.request<UserResponseDto[]>('/users');
  }

  async getUserById(id: string): Promise<UserResponseDto> {
    return this.request<UserResponseDto>(`/users/${id}`);
  }

  async createUser(user: UserCreateDto): Promise<UserResponseDto> {
    return this.request<UserResponseDto>('/users', {
      method: 'POST',
      body: user,
    });
  }

  async updateUser(id: string, user: UserUpdateDto): Promise<UserResponseDto> {
    return this.request<UserResponseDto>(`/users/${id}`, {
      method: 'PUT',
      body: user,
    });
  }

  async deleteUser(id: string): Promise<void> {
    return this.deleteRequest(`/users/${id}`);
  }

  // ==================== ROLE MANAGEMENT ====================

  async getAllRoles(): Promise<RoleResponseDto[]> {
    return this.request<RoleResponseDto[]>('/roles');
  }

  async getRoleByCode(code: string): Promise<RoleResponseDto> {
    return this.request<RoleResponseDto>(`/roles/${code}`);
  }

  async createRole(role: CreateRoleRequestDto): Promise<RoleResponseDto> {
    return this.request<RoleResponseDto>('/roles', {
      method: 'POST',
      body: role,
    });
  }

  async updateRole(code: string, role: UpdateRoleRequestDto): Promise<RoleResponseDto> {
    return this.request<RoleResponseDto>(`/roles/${code}`, {
      method: 'PUT',
      body: role,
    });
  }

  async deleteRole(code: string): Promise<void> {
    return this.deleteRequest(`/roles/${code}`);
  }

  // ==================== PERMISSION MANAGEMENT ====================

  async getAllPermissions(): Promise<PermissionResponseDto[]> {
    return this.request<PermissionResponseDto[]>('/permissions');
  }

  async getGroupedPermissions(): Promise<GroupedPermissionResponseDto[]> {
    return this.request<GroupedPermissionResponseDto[]>('/permissions/grouped');
  }

  async getPermissionByCode(code: string): Promise<PermissionResponseDto> {
    return this.request<PermissionResponseDto>(`/permissions/${code}`);
  }

  async createPermission(permission: CreatePermissionRequestDto): Promise<PermissionResponseDto> {
    return this.request<PermissionResponseDto>('/permissions', {
      method: 'POST',
      body: permission,
    });
  }

  async updatePermission(code: string, permission: UpdatePermissionRequestDto): Promise<PermissionResponseDto> {
    return this.request<PermissionResponseDto>(`/permissions/${code}`, {
      method: 'PUT',
      body: permission,
    });
  }

  async deletePermission(code: string): Promise<void> {
    return this.deleteRequest(`/permissions/${code}`);
  }

  // ==================== SESSION MANAGEMENT ====================

  async getActiveSessions(): Promise<UserSessionsDto[]> {
    return this.request<UserSessionsDto[]>('/sessions');
  }

  async revokeSession(sessionId: string): Promise<void> {
    return this.deleteRequest(`/sessions/${sessionId}`);
  }
}

// Singleton instance
export const adminService = new AdminApiService();
