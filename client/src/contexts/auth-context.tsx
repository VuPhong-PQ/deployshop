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
  login: (userData: User) => void;
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
    // Kiểm tra localStorage khi component mount
    const savedUser = localStorage.getItem("user");
    const savedAuth = localStorage.getItem("isAuthenticated");
    const savedStore = localStorage.getItem("currentStore");
    
    console.log("🔄 AuthProvider loading from localStorage:", { savedUser: !!savedUser, savedAuth, savedStore: !!savedStore });

    if (savedUser && savedAuth === "true") {
      try {
        const userData = JSON.parse(savedUser);
        setUser(userData);
        setIsAuthenticated(true);
        
        if (savedStore) {
          const storeData = JSON.parse(savedStore);
          setCurrentStore(storeData);
          console.log("🏪 Loaded store from localStorage:", storeData.name);
        }
      } catch (error) {
        console.error("Error parsing saved user data:", error);
        localStorage.removeItem("user");
        localStorage.removeItem("isAuthenticated");
        localStorage.removeItem("currentStore");
      }
    }
    
    setIsLoading(false); // Mark loading as complete
  }, []);

  const loadAvailableStores = useCallback(async (): Promise<void> => {
    if (!user) return;
    
    try {
      const response = await fetch('http://101.53.9.76:5273/api/storeswitch/my-stores', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Username': user.username
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
      const response = await fetch('http://101.53.9.76:5273/api/storeswitch/current', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Username': user.username
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
            localStorage.setItem("currentStore", JSON.stringify(store));
          }
        }
      }
    } catch (error) {
      console.error("Error loading current store:", error);
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

  const login = (userData: User) => {
    setUser(userData);
    setIsAuthenticated(true);
    localStorage.setItem("user", JSON.stringify(userData));
    localStorage.setItem("isAuthenticated", "true");
  };

  const logout = () => {
    setUser(null);
    setIsAuthenticated(false);
    setCurrentStore(null);
    setAvailableStores([]);
    localStorage.removeItem("user");
    localStorage.removeItem("isAuthenticated");
    localStorage.removeItem("currentStore");
    localStorage.removeItem("intendedRoute"); // Clear intended route on logout
  };

  const hasPermission = (permission: string): boolean => {
    if (!user || !user.permissions) return false;
    return user.permissions.includes(permission);
  };

  const refreshPermissions = async (): Promise<void> => {
    if (!user) return;
    
    try {
      const response = await fetch(`http://101.53.9.76:5273/api/Staff/refresh-permissions/${user.staffId}`);
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
        localStorage.setItem("user", JSON.stringify(updatedUser));
      }
    } catch (error) {
      console.error("Error refreshing permissions:", error);
    }
  };

  const switchStore = async (storeId: number): Promise<void> => {
    if (!user) return;
    
    try {
      // Call backend to set current store
      const response = await fetch('http://101.53.9.76:5273/api/storeswitch/set-current', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Username': user.username
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
          localStorage.setItem("user", JSON.stringify(updatedUser));
          localStorage.setItem("currentStore", JSON.stringify(selectedStore));
        }
      } else {
        throw new Error('Failed to switch store');
      }
    } catch (error) {
      console.error("Error switching store:", error);
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