// Admin API Service for users, roles, and permissions management
import { getApiUrl } from '../utils/apiConfig';
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

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = 'ApiError';
  }
}

export class AdminApiService {
  private baseUrl: string;

  constructor(baseUrl?: string) {
    this.baseUrl = baseUrl || getApiUrl();
  }

  /**
   * Internal request method with cookie-based authentication
   */
  private async request<T>(
    url: string,
    options: RequestInit = {}
  ): Promise<T> {
    const fullUrl = `${this.baseUrl}${url}`;

    const defaultHeaders: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    const response = await fetch(fullUrl, {
      ...options,
      headers: {
        ...defaultHeaders,
        ...options.headers,
      },
      credentials: 'include', // Include cookies for session
    });

    if (!response.ok) {
      const errorText = await response.text();
      let errorMessage = `Request failed with status ${response.status}`;

      try {
        const errorJson = JSON.parse(errorText);
        errorMessage = errorJson.message || errorMessage;
      } catch {
        errorMessage = errorText || errorMessage;
      }

      throw new ApiError(response.status, errorMessage);
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return null as T;
    }

    return response.json();
  }

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
      body: JSON.stringify(user),
    });
  }

  async updateUser(id: string, user: UserUpdateDto): Promise<UserResponseDto> {
    return this.request<UserResponseDto>(`/users/${id}`, {
      method: 'PUT',
      body: JSON.stringify(user),
    });
  }

  async deleteUser(id: string): Promise<void> {
    return this.request<void>(`/users/${id}`, {
      method: 'DELETE',
    });
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
      body: JSON.stringify(role),
    });
  }

  async updateRole(code: string, role: UpdateRoleRequestDto): Promise<RoleResponseDto> {
    return this.request<RoleResponseDto>(`/roles/${code}`, {
      method: 'PUT',
      body: JSON.stringify(role),
    });
  }

  async deleteRole(code: string): Promise<void> {
    return this.request<void>(`/roles/${code}`, {
      method: 'DELETE',
    });
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
      body: JSON.stringify(permission),
    });
  }

  async updatePermission(code: string, permission: UpdatePermissionRequestDto): Promise<PermissionResponseDto> {
    return this.request<PermissionResponseDto>(`/permissions/${code}`, {
      method: 'PUT',
      body: JSON.stringify(permission),
    });
  }

  async deletePermission(code: string): Promise<void> {
    return this.request<void>(`/permissions/${code}`, {
      method: 'DELETE',
    });
  }

  // ==================== SESSION MANAGEMENT ====================

  async getActiveSessions(): Promise<UserSessionsDto[]> {
    return this.request<UserSessionsDto[]>('/sessions');
  }

  async revokeSession(sessionId: string): Promise<void> {
    return this.request<void>(`/sessions/${sessionId}`, {
      method: 'DELETE',
    });
  }
}

// Singleton instance
export const adminService = new AdminApiService();
