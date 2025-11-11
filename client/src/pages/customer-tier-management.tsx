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
import { Plus, Edit, Trash2, Star, Users, TrendingUp, Palette, Award, Loader2, Archive } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { z } from "zod";
import { useLocation } from "wouter";

interface CustomerTier {
  tierId: number;
  tierName: string;
  minSpent: number;
  minPoints: number;
  pointsMultiplier: number; // Note: API uses 'pointsMultiplier' not 'pointMultiplier'
  discountPercentage: number;
  isActive: boolean;
  tierColor: string;
  description?: string;
}

interface TierStatistics {
  tierId: number;
  tierName: string;
  customerCount: number;
}

const tierFormSchema = z.object({
  tierName: z.string().min(1, "Tên hạng là bắt buộc").max(50, "Tên hạng không được quá 50 ký tự"),
  minSpent: z.number().min(0, "Chi tiêu tối thiểu phải >= 0"),
  minPoints: z.number().min(0, "Điểm tối thiểu phải >= 0"),
  pointsMultiplier: z.number().min(1, "Hệ số điểm phải >= 1").max(10, "Hệ số điểm không được quá 10"),
  discountPercentage: z.number().min(0, "Giảm giá phải >= 0").max(100, "Giảm giá không được quá 100%"),
  isActive: z.boolean(),
  tierColor: z.string().min(1, "Màu sắc là bắt buộc"),
  description: z.string().optional()
});

type TierFormData = z.infer<typeof tierFormSchema>;

export default function CustomerTierManagement() {
  const { toast } = useToast();
  const [, navigate] = useLocation();
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

  // Fetch statistics
  const { data: statistics = [] } = useQuery<TierStatistics[]>({
    queryKey: ['/api/CustomerTierManagement/statistics'],
    queryFn: async () => {
      const response = await apiRequest('/api/CustomerTierManagement/statistics', { method: 'GET' });
      return response;
    },
  });

  // Form
  const form = useForm<TierFormData>({
    resolver: zodResolver(tierFormSchema),
    defaultValues: {
      tierName: "",
      minSpent: 0,
      minPoints: 0,
      pointsMultiplier: 1,
      discountPercentage: 0,
      isActive: true,
      tierColor: "#3B82F6",
      description: ""
    },
  });

  // Create mutation
  const createMutation = useMutation({
    mutationFn: async (tierData: TierFormData) => {
      return apiRequest('/api/CustomerTierManagement', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json; charset=utf-8' },
        body: JSON.stringify(tierData)
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement'] });
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement/statistics'] });
      toast({
        title: "Thành công",
        description: "Hạng khách hàng đã được tạo thành công",
        variant: "default",
      });
      setIsAddDialogOpen(false);
      form.reset();
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error?.message || "Có lỗi xảy ra khi tạo hạng khách hàng",
        variant: "destructive",
      });
    },
  });

  // Update mutation
  const updateMutation = useMutation({
    mutationFn: async ({ tierId, ...tierData }: { tierId: number } & TierFormData) => {
      return apiRequest(`/api/CustomerTierManagement/${tierId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json; charset=utf-8' },
        body: JSON.stringify({ TierId: tierId, ...tierData })
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement'] });
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement/statistics'] });
      toast({
        title: "Thành công",
        description: "Hạng khách hàng đã được cập nhật thành công",
        variant: "default",
      });
      setIsAddDialogOpen(false);
      setEditingTier(null);
      form.reset();
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error?.message || "Có lỗi xảy ra khi cập nhật hạng khách hàng",
        variant: "destructive",
      });
    },
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: async (tierId: number) => {
      return apiRequest(`/api/CustomerTierManagement/${tierId}`, {
        method: 'DELETE'
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement'] });
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement/statistics'] });
      toast({
        title: "Thành công",
        description: "Hạng khách hàng đã được xóa thành công",
        variant: "default",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error?.message || "Có lỗi xảy ra khi xóa hạng khách hàng",
        variant: "destructive",
      });
    },
  });



  const onSubmit = (data: TierFormData) => {
    if (editingTier) {
      updateMutation.mutate({ tierId: editingTier.tierId, ...data });
    } else {
      createMutation.mutate(data);
    }
  };

  const handleEdit = (tier: CustomerTier) => {
    setEditingTier(tier);
    form.reset({
      tierName: tier.tierName,
      minSpent: tier.minSpent,
      minPoints: tier.minPoints,
      pointsMultiplier: tier.pointsMultiplier,
      discountPercentage: tier.discountPercentage,
      isActive: tier.isActive,
      tierColor: tier.tierColor,
      description: tier.description || ""
    });
    setIsAddDialogOpen(true);
  };

  const handleDelete = (tierId: number) => {
    if (window.confirm("Bạn có chắc chắn muốn xóa hạng khách hàng này không?")) {
      deleteMutation.mutate(tierId);
    }
  };

  const getStatistics = (tierId: number) => {
    return statistics.find(s => s.tierId === tierId)?.customerCount || 0;
  };

  if (isLoading) {
    return (
      <AppLayout>
        <div className="flex items-center justify-center h-64">
          <div className="text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-2"></div>
            <p>Đang tải dữ liệu...</p>
          </div>
        </div>
      </AppLayout>
    );
  }

  return (
    <AppLayout>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">Quản lý hạng khách hàng</h1>
            <p className="text-gray-600">Cấu hình các hạng khách hàng và quyền lợi tương ứng</p>
          </div>
          
          <div className="flex items-center gap-2">
            {/* Archive disabled tiers button - only show if there are disabled tiers */}
            {tiers.some(t => !t.isActive) && (
              <Button
                variant="outline"
                onClick={() => navigate("/disabled-tiers-archive")}
                className="text-orange-600 hover:text-orange-700 hover:bg-orange-50"
              >
                <Archive className="w-4 h-4 mr-2" />
                Kho lưu trữ ({tiers.filter(t => !t.isActive).length})
              </Button>
            )}
            
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
                            <FormLabel>Tên hạng</FormLabel>
                            <FormControl>
                              <Input placeholder="VIP, Gold, Silver..." {...field} />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                      
                      <FormField
                        control={form.control}
                        name="isActive"
                        render={({ field }) => (
                          <FormItem className="flex items-center space-x-2 space-y-0 pt-6">
                            <FormControl>
                              <input
                                type="checkbox"
                                checked={field.value}
                                onChange={field.onChange}
                                className="rounded"
                              />
                            </FormControl>
                            <FormLabel>Kích hoạt</FormLabel>
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
                            <FormLabel>Chi tiêu tối thiểu (VND)</FormLabel>
                            <FormControl>
                              <Input
                                type="number"
                                min="0"
                                {...field}
                                onChange={e => field.onChange(Number(e.target.value))}
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
                            <FormLabel>Điểm tối thiểu</FormLabel>
                            <FormControl>
                              <Input
                                type="number"
                                min="0"
                                {...field}
                                onChange={e => field.onChange(Number(e.target.value))}
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
                            <FormLabel>Hệ số điểm</FormLabel>
                            <FormControl>
                              <Input
                                type="number"
                                step="0.1"
                                min="1"
                                max="10"
                                {...field}
                                onChange={e => field.onChange(Number(e.target.value))}
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
                            <FormLabel>Giảm giá (%)</FormLabel>
                            <FormControl>
                              <Input
                                type="number"
                                min="0"
                                max="100"
                                {...field}
                                onChange={e => field.onChange(Number(e.target.value))}
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
                        name="tierColor"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Màu sắc</FormLabel>
                            <FormControl>
                              <Input type="color" {...field} />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                      
                      <FormField
                        control={form.control}
                        name="description"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Mô tả</FormLabel>
                            <FormControl>
                              <Input placeholder="Mô tả hạng..." {...field} />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                    </div>

                    <div className="flex justify-end space-x-2 pt-4">
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => {
                          setIsAddDialogOpen(false);
                          setEditingTier(null);
                        }}
                      >
                        Hủy
                      </Button>
                      <Button 
                        type="submit" 
                        disabled={createMutation.isPending || updateMutation.isPending}
                      >
                        {createMutation.isPending || updateMutation.isPending ? "Đang xử lý..." : (editingTier ? "Cập nhật" : "Tạo mới")}
                      </Button>
                    </div>
                  </form>
                </Form>
              </DialogContent>
            </Dialog>
          </div>
        </div>

        {/* Active Tiers Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {tiers.filter(tier => tier.isActive).map((tier) => {
            const customerCount = getStatistics(tier.tierId);
            
            return (
              <Card key={tier.tierId} className={`relative ${!tier.isActive ? 'opacity-60' : ''}`}>
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-2">
                      <div 
                        className="w-8 h-8 rounded-full flex items-center justify-center"
                        style={{ backgroundColor: tier.tierColor }}
                      >
                        <Star className="w-4 h-4 text-white" />
                      </div>
                      <div>
                        <CardTitle className="text-lg">{tier.tierName}</CardTitle>
                        <Badge variant={tier.isActive ? "default" : "secondary"} className="text-xs">
                          {tier.isActive ? "Hoạt động" : "Tạm dừng"}
                        </Badge>
                      </div>
                    </div>
                    <div className="flex space-x-1">
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
                        onClick={() => handleDelete(tier.tierId)}
                        className="text-red-600 hover:text-red-700"
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  </div>
                </CardHeader>
                
                <CardContent className="space-y-3">
                  <div className="grid grid-cols-2 gap-3 text-sm">
                    <div>
                      <Label className="text-muted-foreground">Chi tiêu tối thiểu</Label>
                      <p className="font-medium">{tier.minSpent.toLocaleString()} ₫</p>
                    </div>
                    <div>
                      <Label className="text-muted-foreground">Điểm tối thiểu</Label>
                      <p className="font-medium">{tier.minPoints.toLocaleString()}</p>
                    </div>
                    <div>
                      <Label className="text-muted-foreground">Hệ số điểm</Label>
                      <p className="font-medium">x{tier.pointsMultiplier}</p>
                    </div>
                    <div>
                      <Label className="text-muted-foreground">Giảm giá</Label>
                      <p className="font-medium">{tier.discountPercentage}%</p>
                    </div>
                  </div>
                  
                  {tier.description && (
                    <div className="pt-2 border-t">
                      <Label className="text-muted-foreground">Mô tả</Label>
                      <p className="text-sm">{tier.description}</p>
                    </div>
                  )}
                  
                  <div className="pt-2 border-t">
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-muted-foreground">Số khách hàng</span>
                      <Badge variant="outline">
                        <Users className="w-3 h-3 mr-1" />
                        {customerCount}
                      </Badge>
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>

        {tiers.filter(tier => tier.isActive).length === 0 && (
          <div className="text-center py-12">
            <div className="text-gray-400 mb-4">
              <Award className="w-12 h-12 mx-auto" />
            </div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">Chưa có hạng khách hàng nào</h3>
            <p className="text-gray-500 mb-4">Bắt đầu bằng cách tạo hạng khách hàng đầu tiên</p>
            <Button onClick={() => setIsAddDialogOpen(true)}>
              <Plus className="w-4 h-4 mr-2" />
              Tạo hạng đầu tiên
            </Button>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
