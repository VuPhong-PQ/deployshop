import { useState } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { useToast } from "@/hooks/use-toast";
import { queryClient, apiRequest } from "@/lib/queryClient";
import { Plus, Edit, Trash2, Star, Users, TrendingUp, Palette, Award } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { z } from "zod";

interface CustomerTier {
  tierId: number;
  tierName: string;
  minSpent: number;
  minPoints: number;
  pointsMultiplier: number;
  discountPercentage: number;
  description: string;
  tierColor: string;
  isActive: boolean;
}

interface TierStatistics {
  tierId: number;
  tierName: string;
  customerCount: number;
  totalSpent: number;
  averageSpent: number;
  tierColor: string;
}

const tierFormSchema = z.object({
  tierName: z.string().min(1, "Tên hạng là bắt buộc").max(50, "Tên hạng không được quá 50 ký tự"),
  minSpent: z.number().min(0, "Chi tiêu tối thiểu phải >= 0"),
  minPoints: z.number().min(0, "Điểm tối thiểu phải >= 0"),
  pointsMultiplier: z.number().min(0.1).max(10, "Hệ số điểm từ 0.1 đến 10"),
  discountPercentage: z.number().min(0).max(100, "Phần trăm giảm giá từ 0 đến 100"),
  description: z.string().max(200, "Mô tả không được quá 200 ký tự").optional(),
  tierColor: z.string().min(1, "Màu sắc là bắt buộc"),
  isActive: z.boolean().default(true),
});

type TierFormData = z.infer<typeof tierFormSchema>;

export default function CustomerTierManagement() {
  const { toast } = useToast();
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [editingTier, setEditingTier] = useState<CustomerTier | null>(null);

  // Fetch tiers
  const { data: tiers = [], isLoading, refetch } = useQuery<CustomerTier[]>({
    queryKey: ['/api/CustomerTierManagement'],
    queryFn: async () => {
      const response = await apiRequest('/api/CustomerTierManagement', { method: 'GET' });
      return response;
    },
  });

  // Fetch tier statistics
  const { data: statistics = [] } = useQuery<TierStatistics[]>({
    queryKey: ['/api/CustomerTierManagement/statistics'],
    queryFn: async () => {
      const response = await apiRequest('/api/CustomerTierManagement/statistics', { method: 'GET' });
      return response;
    },
  });

  // Form setup
  const form = useForm<TierFormData>({
    resolver: zodResolver(tierFormSchema),
    defaultValues: {
      tierName: "",
      minSpent: 0,
      minPoints: 0,
      pointsMultiplier: 1.0,
      discountPercentage: 0,
      description: "",
      tierColor: "#3B82F6",
      isActive: true,
    },
  });

  // Add tier mutation
  const addTierMutation = useMutation({
    mutationFn: async (tierData: TierFormData) => {
      return apiRequest('/api/CustomerTierManagement', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(tierData),
      });
    },
    onSuccess: () => {
      toast({ title: "Thành công", description: "Hạng khách hàng đã được thêm" });
      refetch();
      setIsAddDialogOpen(false);
      form.reset();
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Không thể thêm hạng khách hàng",
        variant: "destructive",
      });
    }
  });

  // Update tier mutation
  const updateTierMutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: TierFormData }) => {
      return apiRequest(`/api/CustomerTierManagement/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...data, tierId: id }),
      });
    },
    onSuccess: () => {
      toast({ title: "Thành công", description: "Hạng khách hàng đã được cập nhật" });
      refetch();
      setEditingTier(null);
      setIsAddDialogOpen(false);
      form.reset();
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Không thể cập nhật hạng khách hàng",
        variant: "destructive",
      });
    }
  });

  // Delete tier mutation
  const deleteTierMutation = useMutation({
    mutationFn: async (id: number) => {
      return apiRequest(`/api/CustomerTierManagement/${id}`, { method: 'DELETE' });
    },
    onSuccess: () => {
      toast({ title: "Thành công", description: "Hạng khách hàng đã được xóa" });
      refetch();
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Không thể xóa hạng khách hàng",
        variant: "destructive",
      });
    }
  });

  // Handle form submission
  const onSubmit = (data: TierFormData) => {
    if (editingTier) {
      updateTierMutation.mutate({ id: editingTier.tierId, data });
    } else {
      addTierMutation.mutate(data);
    }
  };

  // Handle edit
  const handleEdit = (tier: CustomerTier) => {
    setEditingTier(tier);
    form.reset({
      tierName: tier.tierName,
      minSpent: tier.minSpent,
      minPoints: tier.minPoints,
      pointsMultiplier: tier.pointsMultiplier,
      discountPercentage: tier.discountPercentage,
      description: tier.description || "",
      tierColor: tier.tierColor,
      isActive: tier.isActive,
    });
    setIsAddDialogOpen(true);
  };

  // Handle delete
  const handleDelete = (tier: CustomerTier) => {
    if (confirm(`Bạn có chắc chắn muốn xóa hạng "${tier.tierName}"?`)) {
      deleteTierMutation.mutate(tier.tierId);
    }
  };

  // Predefined colors
  const predefinedColors = [
    "#3B82F6", "#EF4444", "#10B981", "#F59E0B",
    "#8B5CF6", "#06B6D4", "#84CC16", "#F97316",
    "#6366F1", "#EC4899", "#14B8A6", "#A3A3A3"
  ];

  // Format currency
  const formatCurrency = (amount: number) => {
    return amount.toLocaleString('vi-VN') + ' VNĐ';
  };

  return (
    <AppLayout title="Quản lý hạng khách hàng">
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">Quản lý hạng khách hàng</h1>
            <p className="text-gray-600">Cấu hình các hạng khách hàng và quyền lợi tương ứng</p>
          </div>
          
          <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
            <DialogTrigger asChild>
              <Button
                onClick={() => {
                  setEditingTier(null);
                  form.reset();
                }}
              >
                <Plus className="w-4 h-4 mr-2" />
                Thêm hạng mới
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl">
              <DialogHeader>
                <DialogTitle>
                  {editingTier ? "Chỉnh sửa hạng khách hàng" : "Thêm hạng khách hàng mới"}
                </DialogTitle>
              </DialogHeader>

              <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="tierName"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Tên hạng *</FormLabel>
                          <FormControl>
                            <Input {...field} placeholder="VD: Vàng, Bạc, Kim cương" />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="tierColor"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Màu sắc *</FormLabel>
                          <div className="space-y-2">
                            <FormControl>
                              <Input {...field} type="color" className="h-10 w-20" />
                            </FormControl>
                            <div className="flex flex-wrap gap-1">
                              {predefinedColors.map((color) => (
                                <button
                                  key={color}
                                  type="button"
                                  className="w-6 h-6 rounded border-2 border-gray-300"
                                  style={{ backgroundColor: color }}
                                  onClick={() => form.setValue('tierColor', color)}
                                />
                              ))}
                            </div>
                          </div>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="minSpent"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Chi tiêu tối thiểu (VNĐ) *</FormLabel>
                          <FormControl>
                            <Input 
                              {...field} 
                              type="number"
                              onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)}
                              placeholder="0"
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="minPoints"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Điểm tối thiểu *</FormLabel>
                          <FormControl>
                            <Input 
                              {...field} 
                              type="number"
                              onChange={(e) => field.onChange(parseInt(e.target.value) || 0)}
                              placeholder="0"
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="pointsMultiplier"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Hệ số tích điểm *</FormLabel>
                          <FormControl>
                            <Input 
                              {...field} 
                              type="number"
                              step="0.1"
                              onChange={(e) => field.onChange(parseFloat(e.target.value) || 1)}
                              placeholder="1.0"
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="discountPercentage"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Giảm giá (%) *</FormLabel>
                          <FormControl>
                            <Input 
                              {...field} 
                              type="number"
                              max="100"
                              onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)}
                              placeholder="0"
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  <FormField
                    control={form.control}
                    name="description"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Mô tả</FormLabel>
                        <FormControl>
                          <Input {...field} placeholder="Mô tả quyền lợi của hạng này" />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div className="flex justify-end space-x-2 pt-4">
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => setIsAddDialogOpen(false)}
                    >
                      Hủy
                    </Button>
                    <Button
                      type="submit"
                      disabled={addTierMutation.isPending || updateTierMutation.isPending}
                    >
                      {addTierMutation.isPending || updateTierMutation.isPending 
                        ? "Đang lưu..." 
                        : (editingTier ? "Cập nhật" : "Thêm mới")
                      }
                    </Button>
                  </div>
                </form>
              </Form>
            </DialogContent>
          </Dialog>
        </div>

        {/* Statistics Overview */}
        {statistics.length > 0 && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {statistics.map((stat) => (
              <Card key={stat.tierId}>
                <CardContent className="p-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm text-gray-600">Hạng {stat.tierName}</p>
                      <p className="text-2xl font-bold">{stat.customerCount}</p>
                      <p className="text-xs text-gray-500">khách hàng</p>
                    </div>
                    <div 
                      className="w-8 h-8 rounded-full flex items-center justify-center"
                      style={{ backgroundColor: stat.tierColor }}
                    >
                      <Star className="w-4 h-4 text-white" />
                    </div>
                  </div>
                  <div className="mt-2 text-xs text-gray-600">
                    <p>Tổng chi: {formatCurrency(stat.totalSpent)}</p>
                    <p>TB/khách: {formatCurrency(stat.averageSpent)}</p>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}

        {/* Tiers List */}
        <div className="grid gap-4">
          {isLoading ? (
            <Card>
              <CardContent className="p-8 text-center">
                <p>Đang tải...</p>
              </CardContent>
            </Card>
          ) : tiers.length === 0 ? (
            <Card>
              <CardContent className="p-8 text-center">
                <Award className="w-16 h-16 mx-auto mb-4 text-gray-400" />
                <h3 className="text-lg font-medium mb-2">Chưa có hạng khách hàng</h3>
                <p className="text-gray-500 mb-4">
                  Tạo hạng khách hàng đầu tiên để bắt đầu phân loại và tạo quyền lợi
                </p>
                <Button onClick={() => setIsAddDialogOpen(true)}>
                  <Plus className="w-4 h-4 mr-2" />
                  Thêm hạng đầu tiên
                </Button>
              </CardContent>
            </Card>
          ) : (
            tiers.map((tier) => (
              <Card key={tier.tierId} className={tier.isActive ? "" : "opacity-60"}>
                <CardContent className="p-6">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center space-x-3 mb-3">
                        <Badge 
                          className="text-white"
                          style={{ backgroundColor: tier.tierColor }}
                        >
                          {tier.tierName}
                        </Badge>
                        {!tier.isActive && (
                          <Badge variant="secondary">Đã tắt</Badge>
                        )}
                      </div>

                      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                        <div>
                          <p className="text-gray-600">Chi tiêu tối thiểu</p>
                          <p className="font-semibold">{formatCurrency(tier.minSpent)}</p>
                        </div>
                        <div>
                          <p className="text-gray-600">Điểm tối thiểu</p>
                          <p className="font-semibold">{tier.minPoints} điểm</p>
                        </div>
                        <div>
                          <p className="text-gray-600">Hệ số tích điểm</p>
                          <p className="font-semibold">x{tier.pointsMultiplier}</p>
                        </div>
                        <div>
                          <p className="text-gray-600">Giảm giá</p>
                          <p className="font-semibold text-green-600">{tier.discountPercentage}%</p>
                        </div>
                      </div>

                      {tier.description && (
                        <p className="mt-3 text-gray-600 text-sm">{tier.description}</p>
                      )}
                    </div>

                    <div className="flex space-x-1 ml-4">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleEdit(tier)}
                      >
                        <Edit className="w-4 h-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDelete(tier)}
                        className="text-red-600 hover:text-red-700"
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))
          )}
        </div>
      </div>
    </AppLayout>
  );
}