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
    try {
      // Call API directly with full parameters to ensure we get product group info
      const params = new URLSearchParams({
        page: '1',
        pageSize: '9999' // Get all products to filter locally
      });
      
      if (storeId) {
        params.append('storeId', storeId.toString());
      }
      
      const response = await fetch(`http://101.53.9.76:5273/api/products?${params}`);
      if (!response.ok) {
        throw new Error('Failed to fetch products');
      }
      
      const data = await response.json();
      const products = data?.products || data?.Products || [];
      
      // Debug logs removed: raw products, sample structure and keys.
      
      const lowStockProducts = products
        .filter((product: any) => product.stockQuantity <= 10)
        .sort((a: any, b: any) => a.stockQuantity - b.stockQuantity)
        .slice(0, 20)
        .map((product: any) => {
          // Try multiple possible field names for product group/category
          const possibleCategoryFields = [
            'productGroupName',
            'categoryName', 
            'category',
            'groupName',
            'ProductGroupName',
            'CategoryName',
            'Group',
            'productGroup',
            'ProductGroup'
          ];
          
          let categoryValue = 'Chưa phân loại';
          for (const field of possibleCategoryFields) {
            if (product[field] && product[field].trim()) {
              categoryValue = product[field];
              // Removed debug log for found category field
              break;
            }
          }
          
          if (categoryValue === 'Chưa phân loại') {
            // No category found for product; debug log removed
          }
          
          return {
            id: product.productId || product.id,
            name: product.name,
            stockQuantity: product.stockQuantity,
            price: product.price,
            category: categoryValue,
            minStockLevel: product.minStockLevel || 5
          };
        });
      
  return { success: true, data: lowStockProducts };
    } catch (error) {
      // keep error return but avoid noisy debug output
      return { success: false, error: error };
    }
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