import React, { createContext, useContext, useState, useEffect, useCallback } from "react";

interface User {
  staffId: number;
  fullName: string;
  username: string;
  email?: string;
  roleId: number;
  roleName: string;
  permissions: string[];
  lastLogin?: string;
  storeId?: number;
  storeName?: string;
}

interface Store {
  storeId: number;
  name: string;
  address?: string;
  phone?: string;
  email?: string;
  manager?: string;
  isActive: boolean;
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  currentStore: Store | null;
  availableStores: Store[];
  isLoading: boolean;
  login: (userData: User, token: string) => void;
  logout: () => void;
  switchStore: (storeId: number) => Promise<void>;
  hasPermission: (permission: string) => boolean;
  refreshPermissions: () => Promise<void>;
  loadAvailableStores: () => Promise<void>;
  loadCurrentStore: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [currentStore, setCurrentStore] = useState<Store | null>(null);
  const [availableStores, setAvailableStores] = useState<Store[]>([]);
  const [isLoading, setIsLoading] = useState(true); // Add loading state

  useEffect(() => {
    // Kiểm tra sessionStorage khi component mount
    const savedUser = sessionStorage.getItem("user");
    const savedAuth = sessionStorage.getItem("isAuthenticated");
    const savedStore = sessionStorage.getItem("currentStore");

    if (savedUser && savedAuth === "true") {
      try {
        const userData = JSON.parse(savedUser);
        setUser(userData);
        setIsAuthenticated(true);
        
        if (savedStore) {
          const storeData = JSON.parse(savedStore);
          setCurrentStore(storeData);
        }
      } catch (error) {
        sessionStorage.removeItem("user");
        sessionStorage.removeItem("isAuthenticated");
        sessionStorage.removeItem("currentStore");
        sessionStorage.removeItem("authToken");
      }
    }
    
    setIsLoading(false);
  }, []);

  const loadAvailableStores = useCallback(async (): Promise<void> => {
    if (!user) return;
    
    try {
  const base = import.meta.env.VITE_API_BASE_URL || (import.meta.env.VITE_API_BASE_URL||'http://localhost:5273');
  const token = sessionStorage.getItem("authToken");
  const response = await fetch(`${base}/api/storeswitch/my-stores`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': token ? `Bearer ${token}` : ''
        },
        credentials: 'include'
      });
      
      if (response.ok) {
        const stores = await response.json();
        setAvailableStores(stores);
      }
    } catch (error) {
      console.error("Error loading stores:", error);
    }
  }, [user?.username]);

  const loadCurrentStore = useCallback(async (): Promise<void> => {
    if (!user) return;
    
    try {
  const base = import.meta.env.VITE_API_BASE_URL || (import.meta.env.VITE_API_BASE_URL||'http://localhost:5273');
  const token = sessionStorage.getItem("authToken");
  const response = await fetch(`${base}/api/storeswitch/current`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': token ? `Bearer ${token}` : ''
        },
        credentials: 'include'
      });
      
      if (response.ok) {
        const data = await response.json();
        if (data.storeId) {
          // Find store in available stores
          const store = availableStores.find(s => s.storeId === data.storeId);
          if (store) {
            setCurrentStore(store);
            sessionStorage.setItem("currentStore", JSON.stringify(store));
          }
        }
      }
    } catch (error) {
      // silent error
    }
  }, [user?.username, availableStores]);

  useEffect(() => {
    // Load available stores when authenticated
    if (isAuthenticated && user) {
      loadAvailableStores();
    }
  }, [isAuthenticated, user, loadAvailableStores]);

  useEffect(() => {
    // Load current store after available stores are loaded
    if (isAuthenticated && user && availableStores.length > 0 && !currentStore) {
      loadCurrentStore();
    }
  }, [isAuthenticated, user, availableStores, currentStore, loadCurrentStore]);

  const login = (userData: User, token: string) => {
    setUser(userData);
    setIsAuthenticated(true);
    sessionStorage.setItem("user", JSON.stringify(userData));
    sessionStorage.setItem("isAuthenticated", "true");
    sessionStorage.setItem("authToken", token);
  };

  const logout = () => {
    setUser(null);
    setIsAuthenticated(false);
    setCurrentStore(null);
    setAvailableStores([]);
    sessionStorage.removeItem("user");
    sessionStorage.removeItem("isAuthenticated");
    sessionStorage.removeItem("currentStore");
    sessionStorage.removeItem("authToken");
    localStorage.removeItem("intendedRoute");
  };

  const hasPermission = (permission: string): boolean => {
    if (!user || !user.permissions) return false;
    return user.permissions.includes(permission);
  };

  const refreshPermissions = async (): Promise<void> => {
    if (!user) return;
    
    try {
      const base = import.meta.env.VITE_API_BASE_URL || (import.meta.env.VITE_API_BASE_URL||'http://localhost:5273');
      const token = sessionStorage.getItem("authToken");
      const response = await fetch(`${base}/api/Staff/refresh-permissions/${user.staffId}`, {
        headers: { 'Authorization': token ? `Bearer ${token}` : '' }
      });
      if (response.ok) {
        const userData = await response.json();
        const updatedUser: User = {
          staffId: userData.staffId,
          fullName: userData.fullName,
          username: userData.username,
          email: userData.email,
          roleId: userData.roleId,
          roleName: userData.roleName,
          permissions: userData.permissions,
          lastLogin: userData.lastLogin,
          storeId: userData.storeId,
          storeName: userData.storeName
        };
        
        setUser(updatedUser);
        sessionStorage.setItem("user", JSON.stringify(updatedUser));
      }
    } catch {
      // silent error
    }
  };

  const switchStore = async (storeId: number): Promise<void> => {
    if (!user) return;
    
    try {
      const base = import.meta.env.VITE_API_BASE_URL || (import.meta.env.VITE_API_BASE_URL||'http://localhost:5273');
      const token = sessionStorage.getItem("authToken");
      const response = await fetch(`${base}/api/storeswitch/set-current`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': token ? `Bearer ${token}` : ''
        },
        body: JSON.stringify({ storeId }),
        credentials: 'include'
      });
      
      if (response.ok) {
        const selectedStore = availableStores.find(s => s.storeId === storeId);
        if (selectedStore) {
          setCurrentStore(selectedStore);
          const updatedUser = { ...user, storeId, storeName: selectedStore.name };
          setUser(updatedUser);
          sessionStorage.setItem("user", JSON.stringify(updatedUser));
          sessionStorage.setItem("currentStore", JSON.stringify(selectedStore));
        }
      } else {
        throw new Error('Failed to switch store');
      }
    } catch (error) {
      throw error;
    }
  };

  const value: AuthContextType = {
    user,
    isAuthenticated,
    currentStore,
    availableStores,
    isLoading,
    login,
    logout,
    switchStore,
    hasPermission,
    refreshPermissions,
    loadAvailableStores,
    loadCurrentStore
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}