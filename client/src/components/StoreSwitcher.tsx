import React, { useState, useEffect } from 'react';
import { ChevronDown, Store, MapPin } from 'lucide-react';
import { useAuth } from '@/contexts/auth-context';

interface Store {
  storeId: number;
  name: string;
  address: string;
  phone?: string;
  manager?: string;
}

interface StoreSwitcherProps {
  onStoreChange?: (store: Store) => void;
}

export default function StoreSwitcher({ onStoreChange }: StoreSwitcherProps) {
  const { availableStores, currentStore: authCurrentStore, switchStore } = useAuth();
  const [currentStore, setCurrentStore] = useState<Store | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState(false);

  // Sử dụng availableStores từ auth context thay vì fetch riêng
  const stores = availableStores || [];

  // Sync with auth context current store
  useEffect(() => {
    if (authCurrentStore) {
      setCurrentStore(authCurrentStore);
    }
  }, [authCurrentStore]);

  const handleStoreSelect = async (store: Store) => {
    setLoading(true);
    try {
      // Sử dụng switchStore từ auth context - đã có validation bên trong
      await switchStore(store.storeId);
      setCurrentStore(store);
      setIsOpen(false);
      onStoreChange?.(store);
      
      // Gửi event để các component khác biết store đã thay đổi
      window.dispatchEvent(new CustomEvent('storeChanged', { detail: store }));
    } catch (error) {
      console.error('Error switching store:', error);
      alert('Có lỗi xảy ra khi chuyển cửa hàng');
    } finally {
      setLoading(false);
    }
  };

  if (stores.length === 0) {
    return (
      <div className="text-sm text-gray-500">
        Không có quyền truy cập cửa hàng nào
      </div>
    );
  }

  if (stores.length === 1) {
    return (
      <div className="flex items-center space-x-2 text-sm">
        <Store className="w-4 h-4 text-blue-600" />
        <div>
          <div className="font-medium">{stores[0].name}</div>
          <div className="text-gray-500 text-xs flex items-center">
            <MapPin className="w-3 h-3 mr-1" />
            {stores[0].address}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        disabled={loading}
        className="w-full flex items-center justify-between space-x-2 p-3 bg-gray-50 hover:bg-gray-100 rounded-lg border transition-colors duration-200"
      >
        <div className="flex items-center space-x-2">
          <Store className={`w-4 h-4 ${currentStore ? 'text-blue-600' : 'text-gray-400'}`} />
          <div className="text-left">
            <div className={`font-medium text-sm ${currentStore ? 'text-gray-900' : 'text-gray-500'}`}>
              {currentStore?.name || (stores.length === 0 ? 'Chưa có cửa hàng' : 'Chọn cửa hàng')}
            </div>
            {currentStore && (
              <div className="text-gray-500 text-xs flex items-center">
                <MapPin className="w-3 h-3 mr-1" />
                {currentStore.address}
              </div>
            )}
            {!currentStore && stores.length === 0 && (
              <div className="text-gray-400 text-xs">
                Chưa được phân quyền
              </div>
            )}
          </div>
        </div>
        <ChevronDown className={`w-4 h-4 text-gray-400 transition-transform duration-200 ${isOpen ? 'rotate-180' : ''}`} />
      </button>

      {isOpen && (
        <div className="absolute top-full left-0 mt-1 bg-white border border-gray-200 rounded-lg shadow-lg z-50 max-h-80 overflow-y-auto min-w-[400px] max-w-[500px]">
          {stores.length === 0 ? (
            <div className="p-6 text-center">
              <div className="text-gray-400 mb-3">
                <Store className="w-12 h-12 mx-auto" />
              </div>
              <div className="text-gray-700 font-medium mb-2">Chưa có cửa hàng nào</div>
              <div className="text-gray-500 text-sm">
                Bạn chưa được phân quyền vào cửa hàng nào. Vui lòng liên hệ với quản trị viên để được cấp quyền truy cập.
              </div>
            </div>
          ) : (
            stores.map((store) => (
              <button
                key={store.storeId}
                onClick={() => handleStoreSelect(store)}
                disabled={loading}
                className={`w-full text-left p-4 hover:bg-gray-50 transition-colors duration-200 border-b border-gray-100 last:border-b-0 ${
                  currentStore?.storeId === store.storeId ? 'bg-blue-50 border-l-4 border-blue-500' : ''
                }`}
              >
              <div className="flex items-start space-x-3">
                <Store className={`w-5 h-5 mt-0.5 ${currentStore?.storeId === store.storeId ? 'text-blue-600' : 'text-gray-400'}`} />
                <div className="flex-1 min-w-0">
                  <div className="font-medium text-sm text-gray-900 truncate">{store.name}</div>
                  <div className="text-gray-500 text-xs flex items-center mt-2">
                    <MapPin className="w-3 h-3 mr-1 text-red-400" />
                    <span className="truncate">{store.address}</span>
                  </div>
                  {store.manager && (
                    <div className="text-blue-600 text-xs mt-1 flex items-center">
                      <span className="mr-1">👤</span>
                      Quản lý: {store.manager}
                    </div>
                  )}
                  {store.phone && (
                    <div className="text-green-600 text-xs mt-1 flex items-center">
                      <span className="mr-1">📞</span>
                      {store.phone}
                    </div>
                  )}
                </div>
                {currentStore?.storeId === store.storeId && (
                  <div className="text-blue-600 text-sm font-medium">✓</div>
                )}
              </div>
              </button>
            ))
          )}
        </div>
      )}

      {loading && (
        <div className="absolute inset-0 bg-white bg-opacity-75 flex items-center justify-center rounded-lg">
          <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600"></div>
        </div>
      )}
    </div>
  );
}