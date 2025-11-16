// API request utility - Force correct API URL
const API_BASE_URL = 'http://101.53.9.76:5273/api';

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
    apiRequest<any>(`/dashboard/metrics?storeId=${storeId}&_t=${Date.now()}`),
  
  getLowStockProducts: async (storeId?: number) => {
    // Get all products and filter low stock ones
    const response = await api.getProducts(storeId);
    if (response.success && response.data && (response.data as any).products) {
      const products = (response.data as any).products;
      const lowStockProducts = products
        .filter((product: any) => product.stockQuantity <= 10)
        .sort((a: any, b: any) => a.stockQuantity - b.stockQuantity)
        .slice(0, 20)
        .map((product: any) => ({
          id: product.productId || product.id,
          name: product.name,
          stockQuantity: product.stockQuantity,
          price: product.price,
          category: product.productGroupName || product.categoryName || product.category || product.groupName || 'Chưa phân loại',
          minStockLevel: product.minStockLevel || 5
        }));
      return { success: true, data: lowStockProducts };
    }
    return response;
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