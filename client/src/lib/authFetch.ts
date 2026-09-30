/**
 * authFetch - Wrapper của fetch() tự động thêm JWT Authorization header.
 * Dùng thay cho fetch() trong toàn bộ ứng dụng khi gọi API.
 */
export async function authFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const token = sessionStorage.getItem("authToken");

  const headers = new Headers(init?.headers);

  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(input, {
    ...init,
    headers,
  });

  if (response.status === 401) {
    sessionStorage.removeItem("user");
    sessionStorage.removeItem("isAuthenticated");
    sessionStorage.removeItem("currentStore");
    sessionStorage.removeItem("authToken");
    
    if (!window.location.pathname.includes('/login')) {
      const loginPath = window.location.pathname.startsWith('/shop') ? '/shop/login' : '/login';
      window.location.href = loginPath;
    }
  }

  return response;
}
