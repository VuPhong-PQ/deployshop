import React, { useState, useEffect } from 'react';
import { Store } from 'lucide-react';

interface StoreInfo {
  storeId: number;
  storeName: string;
  shortName: string;
}

const StoreInfoHeader: React.FC = () => {
  const [storeInfo, setStoreInfo] = useState<StoreInfo | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchCurrentStoreInfo();
    
    // Lắng nghe event khi store thay đổi
    const handleStoreChange = () => {
      fetchCurrentStoreInfo();
    };
    
    window.addEventListener('storeChanged', handleStoreChange);
    
    return () => {
      window.removeEventListener('storeChanged', handleStoreChange);
    };
  }, []);

  const fetchCurrentStoreInfo = async () => {
    try {
      const response = await fetch('http://101.53.9.76:5273/api/storeswitch/current-info', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Username': 'admin'
        },
        credentials: 'include'
      });

      if (response.ok) {
        const data = await response.json();
        if (data.storeId) {
          setStoreInfo(data);
        } else {
          setStoreInfo(null);
        }
      }
    } catch (error) {
      console.error('Error fetching store info:', error);
      setStoreInfo(null);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center space-x-1 sm:space-x-2 px-2 sm:px-3 lg:px-4 py-1 sm:py-2 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-lg border border-blue-200">
        <div className="animate-pulse flex items-center space-x-1 sm:space-x-2">
          <div className="w-3 h-3 sm:w-4 sm:h-4 bg-blue-300 rounded"></div>
          <div className="w-16 sm:w-24 lg:w-32 h-3 sm:h-4 bg-blue-300 rounded"></div>
        </div>
      </div>
    );
  }

  if (!storeInfo) {
    return (
      <div className="flex items-center space-x-1 sm:space-x-2 px-2 sm:px-3 lg:px-4 py-1 sm:py-2 bg-gradient-to-r from-gray-50 to-gray-100 rounded-lg border border-gray-200">
        <Store className="w-3 h-3 sm:w-4 sm:h-4 text-gray-400 flex-shrink-0" />
        <span className="text-xs sm:text-sm text-gray-500 font-medium truncate">
          <span className="hidden sm:inline">Chưa chọn cửa hàng</span>
          <span className="sm:hidden">Chưa chọn</span>
        </span>
      </div>
    );
  }

  return (
    <div className="flex items-center space-x-1 sm:space-x-2 px-2 sm:px-3 lg:px-4 py-1 sm:py-2 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-lg border border-blue-200 shadow-sm max-w-32 sm:max-w-40 lg:max-w-none">
      <Store className="w-3 h-3 sm:w-4 sm:h-4 text-blue-600 flex-shrink-0" />
      <div className="flex flex-col min-w-0 flex-1">
        <span className="text-xs sm:text-sm font-semibold text-blue-900 truncate" title={storeInfo.storeName}>
          {storeInfo.shortName}
        </span>
        <span className="text-xs text-blue-600 hidden lg:block">Cửa hàng hiện tại</span>
      </div>
    </div>
  );
};

export default StoreInfoHeader;