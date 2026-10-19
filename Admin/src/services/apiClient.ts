
type RequestOptions = Omit<RequestInit, 'headers'> & {
  headers?: Record<string, string>;
};

export class ApiClientError extends Error {
  constructor(message: string, readonly status: number) {
    super(message);
    this.name = 'ApiClientError';
  }
}

export class ApiClient {
  private readonly baseUrl: string;

  constructor(baseUrl = import.meta.env.VITE_API_BASE_URL ?? '') {
    this.baseUrl = baseUrl.replace(/\/+$/, '').replace(/\/api$/i, '');
  }

  get<T>(path: string): Promise<T> {
    return this.request<T>(path, { method: 'GET' });
  }

  post<T>(path: string, body?: unknown): Promise<T> {
    const isFormData = body instanceof FormData;

    return this.request<T>(path, {
      method: 'POST',
      body: isFormData
        ? body
        : body === undefined
          ? undefined
          : JSON.stringify(body),
      headers: isFormData
        ? {}
        : { 'Content-Type': 'application/json' },
    });
  }

  put<T>(path: string, body: unknown): Promise<T> {
    return this.request<T>(path, {
      method: 'PUT',
      body: JSON.stringify(body),
      headers: { 'Content-Type': 'application/json' },
    });
  }

  private async request<T>(
    path: string,
    options: RequestOptions,
  ): Promise<T> {
    const token = localStorage.getItem('Abp.AuthToken');
    let response: Response;
    try {
      response = await fetch(`${this.baseUrl}${path}`, {
      ...options,
      headers: {
        Accept: 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...options.headers,
      },
      });
    } catch (error) {
      if (error instanceof TypeError) {
        throw new Error(
          `Không kết nối được backend tại ${this.baseUrl || 'địa chỉ API chưa cấu hình'}. ` +
            `Kiểm tra backend, VITE_API_BASE_URL và chứng chỉ HTTPS Development. ` +
            `Lỗi gốc: ${error.message}`,
        );
      }
      throw error;
    }

    if (!response.ok) {
      if (response.status === 401) {
        localStorage.removeItem('Abp.AuthToken');
        localStorage.removeItem('Abp.AuthTokenExpiresAt');
      }

      const responseText = await response.text();
      let message = responseText;

      try {
        const payload = JSON.parse(responseText) as {
          error?: string | { message?: string; details?: string };
          errors?: Array<{ message?: string }>;
        };
        const errorObject =
          typeof payload.error === 'object' ? payload.error : undefined;
        const error =
          typeof payload.error === 'string' ? payload.error : errorObject?.message;
        message = [
          error,
          errorObject?.details,
          ...(payload.errors?.map((item) => item.message) ?? []),
        ]
          .filter((item): item is string => Boolean(item))
          .join(' ') || responseText;
      } catch {
        // Keep the original response text for non-JSON errors.
      }

      const statusMessage = response.status === 401
        ? 'Yêu cầu đăng nhập Admin hoặc phiên đăng nhập đã hết hạn.'
        : response.status === 403
          ? 'Tài khoản hiện tại không có quyền quản lý đề thi.'
          : response.status === 404
            ? 'Không tìm thấy API hoặc dữ liệu được yêu cầu.'
            : response.status >= 500
              ? 'Backend gặp lỗi khi xử lý yêu cầu.'
              : '';
      throw new ApiClientError(
        `API trả về lỗi ${response.status}${statusMessage ? `: ${statusMessage}` : ''}${message ? ` (${message})` : ''}`,
        response.status,
      );
    }

    if (response.status === 204) {
      return undefined as T;
    }

    return (await response.json()) as T;
  }
}
