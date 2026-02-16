// Base API Service — centralized request handling for all API services
import { getApiUrl } from '../utils/apiConfig';
import { authFetch } from '../utils/authFetch';

// ==================== Shared Error Type ====================

/**
 * Unified API error class used by all services.
 * Preserves backward compatibility — each service re-exports a type alias.
 */
export class ApiServiceError extends Error {
  constructor(
    public status: number,
    message: string,
    public code?: string
  ) {
    super(message);
    this.name = 'ApiServiceError';
  }
}

// ==================== Request Options ====================

export interface RequestOptions extends Omit<RequestInit, 'body'> {
  /** JSON-serializable body — will be stringified automatically */
  body?: unknown;
  /**
   * When true, a 404 response returns `null` instead of throwing.
   * Useful for "get by id" endpoints where absence is expected.
   */
  nullOn404?: boolean;
}

// ==================== Base Service ====================

/**
 * Base API service with a centralized `request()` method.
 *
 * Handles:
 * - URL construction (baseUrl + path)
 * - JSON serialization of body
 * - Default headers (`Content-Type: application/json`)
 * - Credentials (`include` for cookie-based auth)
 * - Response parsing (JSON / null for 204)
 * - Unified error handling via `ApiServiceError`
 * - Uses `authFetch` for automatic 401 handling
 *
 * Each concrete service only needs to declare slim public methods:
 * ```ts
 * async getItems(): Promise<Item[]> {
 *   return this.request<Item[]>('/items') ?? [];
 * }
 * ```
 */
export abstract class BaseApiService {
  protected readonly baseUrl: string;

  constructor(baseUrl?: string) {
    this.baseUrl = baseUrl || getApiUrl();
  }

  /**
   * Centralized request method.
   *
   * @param path   Relative endpoint path (e.g. `/bi/cost-config/current`)
   * @param opts   Request options (method, body, nullOn404, etc.)
   * @returns      Parsed JSON response typed as `T`
   */
  protected async request<T>(
    path: string,
    opts: RequestOptions = {}
  ): Promise<T> {
    const { body, nullOn404, headers, ...fetchOpts } = opts;
    const url = `${this.baseUrl}${path}`;

    const init: RequestInit = {
      method: 'GET',
      ...fetchOpts,
      headers: {
        'Content-Type': 'application/json',
        ...(headers as Record<string, string>),
      },
      credentials: 'include',
    };

    if (body !== undefined) {
      init.body = JSON.stringify(body);
    }

    try {
      const response = await authFetch(url, init);

      // Handle 404 → null when opted-in
      if (nullOn404 && response.status === 404) {
        return null as T;
      }

      // Handle 204 No Content
      if (response.status === 204) {
        return null as T;
      }

      // Parse response body
      const isJson = response.headers
        .get('content-type')
        ?.includes('application/json');
      const data: unknown = isJson ? await response.json() : null;

      // Handle errors
      if (!response.ok) {
        const record = data as Record<string, unknown> | null;
        const msg = String(
          record?.message ?? record?.detail ?? record?.error ?? `HTTP ${response.status}`
        );
        const code = record?.code as string | undefined;
        throw new ApiServiceError(response.status, msg, code);
      }

      return data as T;
    } catch (error) {
      if (error instanceof ApiServiceError) throw error;
      throw new ApiServiceError(
        0,
        error instanceof Error ? error.message : `Error requesting ${path}`,
        'NETWORK_ERROR'
      );
    }
  }

  /**
   * Convenience: DELETE request that returns void.
   * Throws on failure, returns nothing on success.
   */
  protected async deleteRequest(path: string): Promise<void> {
    await this.request<void>(path, { method: 'DELETE' });
  }
}
