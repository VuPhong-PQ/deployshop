
const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://101.53.9.76:5273";
import { QueryClient, QueryFunction } from "@tanstack/react-query";


async function throwIfResNotOk(res: Response) {
  if (!res.ok) {
    const text = (await res.text()) || res.statusText;
    throw new Error(`${res.status}: ${text}`);
  }
}


// Sửa lại apiRequest để nhận (url, options)
export async function apiRequest(
  url: string,
  options: RequestInit
): Promise<any> {
  const fullUrl = url.startsWith("http") ? url : API_BASE + url;
  
  // Nếu body là FormData, xóa header Content-Type nếu có (chỉ khi headers là object)
  if (options.body instanceof FormData && options.headers && typeof options.headers === 'object') {
    if ('Content-Type' in options.headers) {
      delete options.headers['Content-Type'];
    }
  }
  const res = await fetch(fullUrl, {
    ...options,
    credentials: "include",
  });
  
  // Response status info removed from console to reduce noise
  
  await throwIfResNotOk(res);
  
  // Trả về json nếu có, nếu không thì trả về text
  const contentType = res.headers.get("content-type") || "";
  
  if (contentType.includes("application/json")) {
  const jsonData = await res.json();
  return jsonData;
  }
  const textData = await res.text();
  return textData;
}

type UnauthorizedBehavior = "returnNull" | "throw";
export const getQueryFn: <T>(options: {
  on401: UnauthorizedBehavior;
}) => QueryFunction<T> =
  ({ on401: unauthorizedBehavior }) =>
  async ({ queryKey }) => {

    const url = queryKey.join("/") as string;
    const fullUrl = url.startsWith("http") ? url : API_BASE + url;
    const res = await fetch(fullUrl, {
      credentials: "include",
    });

    if (unauthorizedBehavior === "returnNull" && res.status === 401) {
      return null;
    }

    await throwIfResNotOk(res);
    return await res.json();
  };

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      queryFn: getQueryFn({ on401: "throw" }),
      refetchInterval: false,
      refetchOnWindowFocus: false,
      staleTime: Infinity,
      retry: false,
    },
    mutations: {
      retry: false,
    },
  },
});
