import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { AppLayout } from "@/components/layout/app-layout";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useToast } from "@/hooks/use-toast";
import { apiRequest } from "@/lib/utils";
import {
  Store,
  Plus,
  Edit,
  Trash2,
  MapPin,
  Phone,
  Mail,
  User,
  Building,
  AlertTriangle
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";

interface StoreData {
  storeId?: number;
  name: string;
  address?: string;
  phone?: string;
  email?: string;
  taxCode?: string;
  manager?: string;
  isActive: boolean;
  notes?: string;
}

const initialFormData: StoreData = {
  name: "",
  address: "",
  phone: "",
  email: "",
  taxCode: "",
  manager: "",
  isActive: true,
  notes: ""
};

export default function StoreManagementPage() {
  const { user } = useAuth();
  const { toast } = useToast();
  const queryClient = useQueryClient();
  
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingStore, setEditingStore] = useState<StoreData | null>(null);
  const [formData, setFormData] = useState<StoreData>(initialFormData);

  // Query to fetch all stores (including inactive)
  const { data: stores = [], isLoading } = useQuery({
    queryKey: ["/api/Stores/all"],
    queryFn: async () => {
      return await apiRequest("/api/Stores/all", { method: "GET" });
    },
  });

  // Query to fetch store statistics
  const { data: storeStats } = useQuery({
    queryKey: ["/api/Stores/stats"],
    queryFn: async () => {
      return await apiRequest("/api/Stores/stats", { method: "GET" });
    },
  });

  // Create store mutation
  const createStoreMutation = useMutation({
    mutationFn: async (data: StoreData) => {
      return await apiRequest("/api/Stores", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/Stores/all"] });
      queryClient.invalidateQueries({ queryKey: ["/api/Stores/stats"] });
      setIsDialogOpen(false);
      setFormData(initialFormData);
      toast({
        title: "Thành công",
        description: "Cửa hàng đã được tạo thành công!",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Có lỗi xảy ra khi tạo cửa hàng",
        variant: "destructive",
      });
    },
  });

  // Update store mutation
  const updateStoreMutation = useMutation({
    mutationFn: async (data: StoreData) => {
      return await apiRequest(`/api/Stores/${data.storeId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/Stores/all"] });
      queryClient.invalidateQueries({ queryKey: ["/api/Stores/stats"] });
      setIsDialogOpen(false);
      setEditingStore(null);
      setFormData(initialFormData);
      toast({
        title: "Thành công",
        description: "Cửa hàng đã được cập nhật thành công!",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Có lỗi xảy ra khi cập nhật cửa hàng",
        variant: "destructive",
      });
    },
  });

  // Delete store mutation (soft delete)
  const deleteStoreMutation = useMutation({
    mutationFn: async (storeId: number) => {
      return await apiRequest(`/api/Stores/${storeId}`, {
        method: "DELETE",
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/Stores/all"] });
      queryClient.invalidateQueries({ queryKey: ["/api/Stores/stats"] });
      toast({
        title: "Thành công",
        description: "Cửa hàng đã được vô hiệu hóa!",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Có lỗi xảy ra khi vô hiệu hóa cửa hàng",
        variant: "destructive",
      });
    },
  });

  // Activate store mutation
  const activateStoreMutation = useMutation({
    mutationFn: async (storeId: number) => {
      return await apiRequest(`/api/Stores/${storeId}/activate`, {
        method: "POST",
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/Stores/all"] });
      queryClient.invalidateQueries({ queryKey: ["/api/Stores/stats"] });
      toast({
        title: "Thành công",
        description: "Cửa hàng đã được kích hoạt!",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Có lỗi xảy ra khi kích hoạt cửa hàng",
        variant: "destructive",
      });
    },
  });

  // Deactivate store mutation
  const deactivateStoreMutation = useMutation({
    mutationFn: async (storeId: number) => {
      return await apiRequest(`/api/Stores/${storeId}/deactivate`, {
        method: "POST",
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/Stores/all"] });
      queryClient.invalidateQueries({ queryKey: ["/api/Stores/stats"] });
      toast({
        title: "Thành công",
        description: "Cửa hàng đã được vô hiệu hóa!",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Có lỗi xảy ra khi vô hiệu hóa cửa hàng",
        variant: "destructive",
      });
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (editingStore) {
      updateStoreMutation.mutate({ ...formData, storeId: editingStore.storeId });
    } else {
      createStoreMutation.mutate(formData);
    }
  };

  const handleEdit = (store: StoreData) => {
    setEditingStore(store);
    setFormData(store);
    setIsDialogOpen(true);
  };

  const handleDelete = (storeId: number) => {
    deleteStoreMutation.mutate(storeId);
  };

  const handleActivate = (storeId: number) => {
    activateStoreMutation.mutate(storeId);
  };

  const handleDeactivate = (storeId: number) => {
    deactivateStoreMutation.mutate(storeId);
  };

  const resetForm = () => {
    setFormData(initialFormData);
    setEditingStore(null);
  };

  // Check if user is admin
  if (user?.roleName !== "Admin") {
    return (
      <AppLayout title="Quản lý cửa hàng">
        <div className="text-center py-8">
          <AlertTriangle className="w-12 h-12 text-red-500 mx-auto mb-4" />
          <h2 className="text-xl font-semibold mb-2">Không có quyền truy cập</h2>
          <p className="text-gray-600">Chỉ Admin mới có thể quản lý cửa hàng</p>
        </div>
      </AppLayout>
    );
  }

  return (
    <AppLayout title="Quản lý cửa hàng">
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold mb-1">Quản lý cửa hàng</h1>
            <p className="text-gray-600">Quản lý thông tin các cửa hàng trong hệ thống</p>
          </div>
          
          <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
            <DialogTrigger asChild>
              <Button 
                onClick={resetForm}
                className="flex items-center gap-2"
              >
                <Plus className="w-4 h-4" />
                Thêm cửa hàng
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl">
              <DialogHeader>
                <DialogTitle>
                  {editingStore ? "Chỉnh sửa cửa hàng" : "Thêm cửa hàng mới"}
                </DialogTitle>
                <DialogDescription>
                  {editingStore 
                    ? "Cập nhật thông tin cửa hàng" 
                    : "Nhập thông tin cửa hàng mới"
                  }
                </DialogDescription>
              </DialogHeader>

              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <Label htmlFor="name">Tên cửa hàng *</Label>
                    <Input
                      id="name"
                      value={formData.name}
                      onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                      placeholder="Nhập tên cửa hàng"
                      required
                    />
                  </div>

                  <div>
                    <Label htmlFor="manager">Quản lý</Label>
                    <Input
                      id="manager"
                      value={formData.manager || ""}
                      onChange={(e) => setFormData(prev => ({ ...prev, manager: e.target.value }))}
                      placeholder="Tên quản lý cửa hàng"
                    />
                  </div>
                </div>

                <div>
                  <Label htmlFor="address">Địa chỉ</Label>
                  <Textarea
                    id="address"
                    value={formData.address || ""}
                    onChange={(e) => setFormData(prev => ({ ...prev, address: e.target.value }))}
                    placeholder="Nhập địa chỉ cửa hàng"
                    rows={2}
                  />
                </div>

                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div>
                    <Label htmlFor="phone">Số điện thoại</Label>
                    <Input
                      id="phone"
                      value={formData.phone || ""}
                      onChange={(e) => setFormData(prev => ({ ...prev, phone: e.target.value }))}
                      placeholder="0xxx-xxx-xxx"
                    />
                  </div>

                  <div>
                    <Label htmlFor="email">Email</Label>
                    <Input
                      id="email"
                      type="email"
                      value={formData.email || ""}
                      onChange={(e) => setFormData(prev => ({ ...prev, email: e.target.value }))}
                      placeholder="store@company.com"
                    />
                  </div>

                  <div>
                    <Label htmlFor="taxCode">Mã số thuế</Label>
                    <Input
                      id="taxCode"
                      value={formData.taxCode || ""}
                      onChange={(e) => setFormData(prev => ({ ...prev, taxCode: e.target.value }))}
                      placeholder="0123456789"
                    />
                  </div>
                </div>

                <div>
                  <Label htmlFor="notes">Ghi chú</Label>
                  <Textarea
                    id="notes"
                    value={formData.notes || ""}
                    onChange={(e) => setFormData(prev => ({ ...prev, notes: e.target.value }))}
                    placeholder="Ghi chú về cửa hàng"
                    rows={2}
                  />
                </div>

                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isActive"
                    checked={formData.isActive}
                    onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
                    className="rounded"
                  />
                  <Label htmlFor="isActive">Cửa hàng đang hoạt động</Label>
                </div>

                <div className="flex justify-end gap-2 pt-4">
                  <Button 
                    type="button" 
                    variant="outline" 
                    onClick={() => setIsDialogOpen(false)}
                  >
                    Hủy
                  </Button>
                  <Button 
                    type="submit"
                    disabled={createStoreMutation.isPending || updateStoreMutation.isPending}
                  >
                    {editingStore ? "Cập nhật" : "Tạo mới"}
                  </Button>
                </div>
              </form>
            </DialogContent>
          </Dialog>
        </div>

        {/* Stores Grid */}
        {isLoading ? (
          <div className="text-center py-8">
            <p>Đang tải dữ liệu...</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {stores.map((store: StoreData) => (
              <Card key={store.storeId} className="relative">
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-lg flex items-center gap-2">
                      <Store className="w-5 h-5" />
                      {store.name}
                    </CardTitle>
                    <Badge variant={store.isActive ? "default" : "secondary"}>
                      {store.isActive ? "Hoạt động" : "Tạm dừng"}
                    </Badge>
                  </div>
                </CardHeader>

                <CardContent className="space-y-3">
                  {store.address && (
                    <div className="flex items-start gap-2 text-sm">
                      <MapPin className="w-4 h-4 mt-0.5 text-gray-500 flex-shrink-0" />
                      <span className="text-gray-700">{store.address}</span>
                    </div>
                  )}

                  {store.phone && (
                    <div className="flex items-center gap-2 text-sm">
                      <Phone className="w-4 h-4 text-gray-500" />
                      <span className="text-gray-700">{store.phone}</span>
                    </div>
                  )}

                  {store.email && (
                    <div className="flex items-center gap-2 text-sm">
                      <Mail className="w-4 h-4 text-gray-500" />
                      <span className="text-gray-700">{store.email}</span>
                    </div>
                  )}

                  {store.manager && (
                    <div className="flex items-center gap-2 text-sm">
                      <User className="w-4 h-4 text-gray-500" />
                      <span className="text-gray-700">QL: {store.manager}</span>
                    </div>
                  )}

                  {store.taxCode && (
                    <div className="flex items-center gap-2 text-sm">
                      <Building className="w-4 h-4 text-gray-500" />
                      <span className="text-gray-700">MST: {store.taxCode}</span>
                    </div>
                  )}

                  {store.notes && (
                    <div className="text-sm text-gray-600 italic">
                      {store.notes}
                    </div>
                  )}

                  {/* Action buttons */}
                  <div className="flex gap-2 pt-3 border-t">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handleEdit(store)}
                      className="flex-1"
                    >
                      <Edit className="w-3 h-3 mr-1" />
                      Sửa
                    </Button>

                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button
                          size="sm"
                          variant="outline"
                          className="text-red-600 hover:text-red-700 hover:bg-red-50"
                        >
                          <Trash2 className="w-3 h-3" />
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>Xác nhận xóa cửa hàng</AlertDialogTitle>
                          <AlertDialogDescription>
                            Bạn có chắc chắn muốn xóa cửa hàng "{store.name}"? 
                            Hành động này sẽ vô hiệu hóa cửa hàng và không thể hoàn tác.
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>Hủy</AlertDialogCancel>
                          <AlertDialogAction
                            onClick={() => store.storeId && handleDelete(store.storeId)}
                            className="bg-red-600 hover:bg-red-700"
                          >
                            Xóa
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}

        {/* Empty state */}
        {!isLoading && stores.length === 0 && (
          <div className="text-center py-12">
            <Store className="w-16 h-16 text-gray-400 mx-auto mb-4" />
            <h3 className="text-xl font-semibold mb-2">Chưa có cửa hàng nào</h3>
            <p className="text-gray-600 mb-4">Tạo cửa hàng đầu tiên để bắt đầu</p>
            <Button onClick={() => setIsDialogOpen(true)}>
              <Plus className="w-4 h-4 mr-2" />
              Thêm cửa hàng đầu tiên
            </Button>
          </div>
        )}
      </div>
    </AppLayout>
  );
}