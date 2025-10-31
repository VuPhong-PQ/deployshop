import React, { useState, useEffect, useCallback } from "react";
import { useAuth } from "@/contexts/auth-context";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Store, MapPin, Phone, Mail, User, Check } from "lucide-react";

interface StoreOption {
  storeId: number;
  name: string;
  address?: string;
  phone?: string;
  email?: string;
  manager?: string;
  isActive: boolean;
}

interface StoreSelectorProps {
  onStoreSelected?: (store: StoreOption) => void;
  showTitle?: boolean;
}

export function StoreSelector({ onStoreSelected, showTitle = true }: StoreSelectorProps) {
  const { user, currentStore, availableStores, switchStore, loadAvailableStores } = useAuth();
  const [selectedStoreId, setSelectedStoreId] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    loadAvailableStores();
  }, []);

  useEffect(() => {
    if (currentStore) {
      setSelectedStoreId(currentStore.storeId);
    } else if (user?.storeId) {
      setSelectedStoreId(user.storeId);
    }
  }, [currentStore, user]);

  const handleStoreSelect = async (store: StoreOption) => {
    setIsLoading(true);
    try {
      await switchStore(store.storeId);
      setSelectedStoreId(store.storeId);
      onStoreSelected?.(store);
    } catch (error) {
      console.error("Error switching store:", error);
    } finally {
      setIsLoading(false);
    }
  };

  // Admin có thể truy cập tất cả stores, nhân viên chỉ có thể truy cập store được assign
  // availableStores đã được lọc theo quyền từ backend rồi, chỉ cần dùng trực tiếp
  const accessibleStores = availableStores;

  if (accessibleStores.length === 0) {
    return (
      <div className="text-center py-12">
        <div className="max-w-md mx-auto">
          <Store className="w-16 h-16 text-gray-300 mx-auto mb-6" />
          <h3 className="text-xl font-semibold text-gray-700 mb-3">
            Chưa có cửa hàng nào được phân quyền
          </h3>
          <p className="text-gray-500 mb-6 leading-relaxed">
            Hiện tại bạn chưa được phân quyền vào cửa hàng nào. Vui lòng liên hệ với quản trị viên để được cấp quyền truy cập các cửa hàng.
          </p>
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <p className="text-sm text-blue-800">
              <strong>Lưu ý:</strong> Sau khi được cấp quyền, bạn cần đăng xuất và đăng nhập lại để cập nhật quyền truy cập.
            </p>
          </div>
        </div>
      </div>
    );
  }

  // Tạm thời bỏ auto-select để tránh infinite loop
  // Sẽ xử lý auto-redirect ở level cao hơn

  return (
    <div className="space-y-4">
      {showTitle && (
        <div className="text-center">
          <h2 className="text-2xl font-bold mb-2">Chọn cửa hàng</h2>
          <p className="text-gray-600">Vui lòng chọn cửa hàng để tiếp tục làm việc</p>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {accessibleStores.map((store) => (
          <Card 
            key={store.storeId}
            className={`cursor-pointer transition-all hover:shadow-md ${
              selectedStoreId === store.storeId 
                ? "ring-2 ring-blue-500 bg-blue-50" 
                : "hover:border-blue-300"
            }`}
            onClick={() => handleStoreSelect(store)}
          >
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-lg flex items-center gap-2">
                  <Store className="w-5 h-5" />
                  {store.name}
                </CardTitle>
                {selectedStoreId === store.storeId && (
                  <Badge className="bg-green-100 text-green-800">
                    <Check className="w-3 h-3 mr-1" />
                    Đang chọn
                  </Badge>
                )}
              </div>
            </CardHeader>
            <CardContent className="space-y-2">
              {store.address && (
                <div className="flex items-start gap-2 text-sm text-gray-600">
                  <MapPin className="w-4 h-4 mt-0.5 flex-shrink-0" />
                  <span>{store.address}</span>
                </div>
              )}
              
              {store.phone && (
                <div className="flex items-center gap-2 text-sm text-gray-600">
                  <Phone className="w-4 h-4" />
                  <span>{store.phone}</span>
                </div>
              )}
              
              {store.email && (
                <div className="flex items-center gap-2 text-sm text-gray-600">
                  <Mail className="w-4 h-4" />
                  <span>{store.email}</span>
                </div>
              )}
              
              {store.manager && (
                <div className="flex items-center gap-2 text-sm text-gray-600">
                  <User className="w-4 h-4" />
                  <span>QL: {store.manager}</span>
                </div>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      {user?.roleName === "Admin" && (
        <div className="text-center pt-4 border-t">
          <p className="text-sm text-gray-500">
            Với quyền Admin, bạn có thể truy cập tất cả cửa hàng
          </p>
        </div>
      )}
    </div>
  );
}

export default StoreSelector;