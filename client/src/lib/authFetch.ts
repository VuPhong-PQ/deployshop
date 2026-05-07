/**
 * authFetch - Wrapper của fetch() tự động thêm JWT Authorization header.
 * Dùng thay cho fetch() trong toàn bộ ứng dụng khi gọi API.
 */
export function authFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const token = sessionStorage.getItem("authToken");

  const headers = new Headers(init?.headers);

  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  return fetch(input, {
    ...init,
    headers,
  });
}
