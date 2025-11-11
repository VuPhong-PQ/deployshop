// API request utility
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://101.53.9.76:5273/api';

export interface ApiResponse<T> {
  data?: T;
  error?: string;
  success: boolean;
}

export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  try {
    const url = `${API_BASE_URL}${endpoint}`;
    
    const defaultHeaders: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    const token = localStorage.getItem('authToken');
    if (token) {
      defaultHeaders['Authorization'] = `Bearer ${token}`;
    }

    const config: RequestInit = {
      ...options,
      headers: {
        ...defaultHeaders,
        ...options.headers,
      },
    };

    const response = await fetch(url, config);
    
    if (!response.ok) {
      const errorText = await response.text();
      return {
        success: false,
        error: errorText || `HTTP error! status: ${response.status}`,
      };
    }

    const data = await response.json();
    return {
      success: true,
      data,
    };
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error occurred',
    };
  }
}

// Specific API functions
export const api = {
  // Dashboard
  getDashboardMetrics: (storeId: number) =>
    apiRequest<any>(`/api/dashboard/metrics?storeId=${storeId}`),
  
  getLowStockProducts: () => {
    // Return mock low stock products for demo
    const mockProducts = [
      { id: 1, name: "Nước uống Coca Cola", stockQuantity: 5, price: 15000, category: "Đồ uống" },
      { id: 2, name: "Bánh mì sandwich", stockQuantity: 3, price: 25000, category: "Thực phẩm" },
      { id: 3, name: "Kẹo Mentos", stockQuantity: 8, price: 10000, category: "Kẹo" },
      { id: 4, name: "Nước suối Lavie", stockQuantity: 2, price: 8000, category: "Đồ uống" },
      { id: 5, name: "Bánh quy Oreo", stockQuantity: 6, price: 35000, category: "Bánh kẹo" }
    ];
    return Promise.resolve({ success: true, data: mockProducts });
  },

  // Stores
  getStores: () => apiRequest<any[]>('/stores'),
  getStore: (id: number) => apiRequest<any>(`/stores/${id}`),

  // Auth
  login: (credentials: { username: string; password: string }) =>
    apiRequest<any>('/staff/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    }),

  // Users
  getUsers: () => apiRequest<any[]>('/users'),
  getCurrentUser: () => apiRequest<any>('/users/current'),

  // Products
  getProducts: (storeId?: number) => {
    const query = storeId ? `?storeId=${storeId}` : '';
    return apiRequest<any[]>(`/products${query}`);
  },

  // Orders
  getOrders: (storeId?: number) => {
    const query = storeId ? `?storeId=${storeId}` : '';
    return apiRequest<any[]>(`/orders${query}`);
  },

  // Customers
  getCustomers: (storeId?: number) => {
    const query = storeId ? `?storeId=${storeId}` : '';
    return apiRequest<any[]>(`/customers${query}`);
  },
};