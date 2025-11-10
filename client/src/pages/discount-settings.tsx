import { useState } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { useToast } from "@/hooks/use-toast";
import { queryClient, apiRequest } from "@/lib/queryClient";
import { Plus, Edit, Trash2, Percent, DollarSign, Package, Calendar, Users } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { z } from "zod";

export enum DiscountType {
  PercentageTotal = 1,
  FixedAmountItem = 2,
  FixedAmountTotal = 3
}

interface Discount {
  discountId: number;
  name: string;
  description?: string;
  type: DiscountType;
  value: number;
  minOrderValue?: number;
  minQuantity?: number;
  productId?: number;
  categoryId?: number;
  startDate?: string;
  endDate?: string;
  maxUsage?: number;
  usageCount: number;
  isActive: boolean;
  createdAt: string;
  product?: {
    productId: number;
    productName: string;
  };
  category?: {
    categoryId: number;
    categoryName: string;
  };
}

interface Product {
  productId: number;
  productName: string;
}

interface Category {
  categoryId: number;
  categoryName: string;
}

const discountFormSchema = z.object({
  name: z.string().min(1, "Tên giảm giá là bắt buộc").max(100, "Tên không được quá 100 ký tự"),
  description: z.string().optional(),
  type: z.nativeEnum(DiscountType),
  value: z.number().min(0, "Giá trị phải >= 0"),
  minOrderValue: z.number().min(0, "Giá trị đơn hàng tối thiểu phải >= 0").optional().nullable(),
  minQuantity: z.number().min(0, "Số lượng tối thiểu phải >= 0").optional().nullable(),
  productId: z.number().optional().nullable(),
  categoryId: z.number().optional().nullable(),
  startDate: z.string().optional().nullable(),
  endDate: z.string().optional().nullable(),
  maxUsage: z.number().min(0, "Số lần sử dụng tối đa phải >= 0").optional().nullable(),
});

type DiscountFormData = z.infer<typeof discountFormSchema>;

export function DiscountSettings() {
  const { toast } = useToast();
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [editingDiscount, setEditingDiscount] = useState<Discount | null>(null);

  // Fetch discounts
  const { data: discounts = [], isLoading, refetch } = useQuery<Discount[]>({
    queryKey: ['/api/Discounts'],
    queryFn: async () => {
      const response = await apiRequest('/api/Discounts', { method: 'GET' });
      return response;
    },
  });

  // Fetch products for dropdown
  const { data: products = [] } = useQuery<Product[]>({
    queryKey: ['/api/products'],
    queryFn: async () => {
      const response = await apiRequest('/api/products', { method: 'GET' });
      return response;
    },
  });

  // Fetch categories for dropdown
  const { data: categories = [] } = useQuery<Category[]>({
    queryKey: ['/api/categories'],
    queryFn: async () => {
      const response = await apiRequest('/api/categories', { method: 'GET' });
      return response;
    },
  });

  // Form
  const form = useForm<DiscountFormData>({
    resolver: zodResolver(discountFormSchema),
    defaultValues: {
      name: "",
      description: "",
      type: DiscountType.PercentageTotal,
      value: 0,
      minOrderValue: null,
      minQuantity: null,
      productId: null,
      categoryId: null,
      startDate: null,
      endDate: null,
      maxUsage: null,
    },
  });

  // Create mutation
  const createMutation = useMutation({
    mutationFn: async (discountData: DiscountFormData) => {
      return apiRequest('/api/Discounts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          ...discountData,
          createdBy: 1, // TODO: Get from auth context
        })
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/Discounts'] });
      toast({
        title: "Thành công",
        description: "Giảm giá đã được tạo thành công",
        variant: "default",
      });
      setIsAddDialogOpen(false);
      form.reset();
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error?.message || "Có lỗi xảy ra khi tạo giảm giá",
        variant: "destructive",
      });
    },
  });

  // Update mutation
  const updateMutation = useMutation({
    mutationFn: async ({ discountId, ...discountData }: { discountId: number } & DiscountFormData & { isActive: boolean }) => {
      return apiRequest(`/api/Discounts/${discountId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(discountData)
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/Discounts'] });
      toast({
        title: "Thành công",
        description: "Giảm giá đã được cập nhật thành công",
        variant: "default",
      });
      setIsAddDialogOpen(false);
      setEditingDiscount(null);
      form.reset();
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error?.message || "Có lỗi xảy ra khi cập nhật giảm giá",
        variant: "destructive",
      });
    },
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: async (discountId: number) => {
      return apiRequest(`/api/Discounts/${discountId}`, {
        method: 'DELETE'
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/Discounts'] });
      toast({
        title: "Thành công",
        description: "Giảm giá đã được xóa thành công",
        variant: "default",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error?.message || "Có lỗi xảy ra khi xóa giảm giá",
        variant: "destructive",
      });
    },
  });

  // Toggle status mutation
  const toggleStatusMutation = useMutation({
    mutationFn: async (discountId: number) => {
      return apiRequest(`/api/Discounts/toggle-status/${discountId}`, {
        method: 'POST'
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/Discounts'] });
      toast({
        title: "Thành công",
        description: "Trạng thái giảm giá đã được cập nhật",
        variant: "default",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error?.message || "Có lỗi xảy ra khi cập nhật trạng thái",
        variant: "destructive",
      });
    },
  });

  const onSubmit = (data: DiscountFormData) => {
    if (editingDiscount) {
      updateMutation.mutate({ 
        discountId: editingDiscount.discountId, 
        ...data,
        isActive: editingDiscount.isActive
      });
    } else {
      createMutation.mutate(data);
    }
  };

  const handleEdit = (discount: Discount) => {
    setEditingDiscount(discount);
    form.reset({
      name: discount.name,
      description: discount.description || "",
      type: discount.type,
      value: discount.value,
      minOrderValue: discount.minOrderValue,
      minQuantity: discount.minQuantity,
      productId: discount.productId,
      categoryId: discount.categoryId,
      startDate: discount.startDate ? discount.startDate.split('T')[0] : null,
      endDate: discount.endDate ? discount.endDate.split('T')[0] : null,
      maxUsage: discount.maxUsage,
    });
    setIsAddDialogOpen(true);
  };

  const handleDelete = (discountId: number) => {
    if (window.confirm("Bạn có chắc chắn muốn xóa giảm giá này không?")) {
      deleteMutation.mutate(discountId);
    }
  };

  const handleToggleStatus = (discountId: number) => {
    toggleStatusMutation.mutate(discountId);
  };

  const getDiscountTypeLabel = (type: DiscountType) => {
    switch (type) {
      case DiscountType.PercentageTotal:
        return "Giảm % tổng bill";
      case DiscountType.FixedAmountItem:
        return "Giảm tiền mặt hàng";
      case DiscountType.FixedAmountTotal:
        return "Giảm tiền tổng bill";
      default:
        return "Không xác định";
    }
  };

  const formatValue = (type: DiscountType, value: number) => {
    if (type === DiscountType.PercentageTotal) {
      return `${value}%`;
    }
    return `${value.toLocaleString()} ₫`;
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-2"></div>
          <p>Đang tải dữ liệu...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold">Quản lý giảm giá</h2>
          <p className="text-gray-600">Cấu hình các loại giảm giá áp dụng khi bán hàng</p>
        </div>
        
        <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
          <DialogTrigger asChild>
            <Button
              onClick={() => {
                setEditingDiscount(null);
                form.reset();
              }}
            >
              <Plus className="w-4 h-4 mr-2" />
              Thêm giảm giá mới
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>
                {editingDiscount ? "Chỉnh sửa giảm giá" : "Thêm giảm giá mới"}
              </DialogTitle>
            </DialogHeader>

            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Tên giảm giá</FormLabel>
                        <FormControl>
                          <Input placeholder="Giảm giá sinh nhật, khuyến mãi..." {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  
                  <FormField
                    control={form.control}
                    name="type"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Loại giảm giá</FormLabel>
                        <FormControl>
                          <select 
                            {...field}
                            value={field.value}
                            onChange={e => field.onChange(Number(e.target.value))}
                            className="w-full p-2 border rounded-md"
                          >
                            <option value={DiscountType.PercentageTotal}>Giảm % tổng bill</option>
                            <option value={DiscountType.FixedAmountItem}>Giảm tiền mặt hàng</option>
                            <option value={DiscountType.FixedAmountTotal}>Giảm tiền tổng bill</option>
                          </select>
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
                        <Input placeholder="Mô tả chi tiết về giảm giá..." {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="value"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Giá trị giảm</FormLabel>
                        <FormControl>
                          <Input
                            type="number"
                            min="0"
                            step="0.01"
                            placeholder={form.watch('type') === DiscountType.PercentageTotal ? "%" : "VND"}
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
                    name="minOrderValue"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Giá trị đơn hàng tối thiểu</FormLabel>
                        <FormControl>
                          <Input
                            type="number"
                            min="0"
                            placeholder="VND (để trống nếu không có)"
                            {...field}
                            value={field.value || ""}
                            onChange={e => field.onChange(e.target.value ? Number(e.target.value) : null)}
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
                    name="productId"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Sản phẩm cụ thể</FormLabel>
                        <FormControl>
                          <select 
                            {...field}
                            value={field.value || ""}
                            onChange={e => field.onChange(e.target.value ? Number(e.target.value) : null)}
                            className="w-full p-2 border rounded-md"
                          >
                            <option value="">Tất cả sản phẩm</option>
                            {products.map(product => (
                              <option key={product.productId} value={product.productId}>
                                {product.productName}
                              </option>
                            ))}
                          </select>
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  
                  <FormField
                    control={form.control}
                    name="categoryId"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Danh mục cụ thể</FormLabel>
                        <FormControl>
                          <select 
                            {...field}
                            value={field.value || ""}
                            onChange={e => field.onChange(e.target.value ? Number(e.target.value) : null)}
                            className="w-full p-2 border rounded-md"
                          >
                            <option value="">Tất cả danh mục</option>
                            {categories.map(category => (
                              <option key={category.categoryId} value={category.categoryId}>
                                {category.categoryName}
                              </option>
                            ))}
                          </select>
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="startDate"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Ngày bắt đầu</FormLabel>
                        <FormControl>
                          <Input
                            type="date"
                            {...field}
                            value={field.value || ""}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  
                  <FormField
                    control={form.control}
                    name="endDate"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Ngày kết thúc</FormLabel>
                        <FormControl>
                          <Input
                            type="date"
                            {...field}
                            value={field.value || ""}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <FormField
                  control={form.control}
                  name="maxUsage"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Số lần sử dụng tối đa</FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          min="0"
                          placeholder="Để trống nếu không giới hạn"
                          {...field}
                          value={field.value || ""}
                          onChange={e => field.onChange(e.target.value ? Number(e.target.value) : null)}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <div className="flex justify-end space-x-2 pt-4">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => {
                      setIsAddDialogOpen(false);
                      setEditingDiscount(null);
                    }}
                  >
                    Hủy
                  </Button>
                  <Button 
                    type="submit" 
                    disabled={createMutation.isPending || updateMutation.isPending}
                  >
                    {createMutation.isPending || updateMutation.isPending ? "Đang xử lý..." : (editingDiscount ? "Cập nhật" : "Tạo mới")}
                  </Button>
                </div>
              </form>
            </Form>
          </DialogContent>
        </Dialog>
      </div>

      {/* Discounts Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {discounts.map((discount) => {
          const isExpired = discount.endDate && new Date(discount.endDate) < new Date();
          const isLimitReached = discount.maxUsage && discount.usageCount >= discount.maxUsage;
          
          return (
            <Card key={discount.discountId} className={`relative ${!discount.isActive ? 'opacity-60' : ''}`}>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center">
                      <Percent className="w-4 h-4 text-blue-600" />
                    </div>
                    <div>
                      <CardTitle className="text-lg">{discount.name}</CardTitle>
                      <div className="flex gap-1 mt-1">
                        <Badge variant={discount.isActive ? "default" : "secondary"} className="text-xs">
                          {discount.isActive ? "Hoạt động" : "Tạm dừng"}
                        </Badge>
                        {isExpired && <Badge variant="destructive" className="text-xs">Hết hạn</Badge>}
                        {isLimitReached && <Badge variant="destructive" className="text-xs">Hết lượt</Badge>}
                      </div>
                    </div>
                  </div>
                  <div className="flex space-x-1">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleEdit(discount)}
                    >
                      <Edit className="w-4 h-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleStatus(discount.discountId)}
                      className={discount.isActive ? "text-orange-600 hover:text-orange-700" : "text-green-600 hover:text-green-700"}
                    >
                      {discount.isActive ? "Tạm dừng" : "Kích hoạt"}
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleDelete(discount.discountId)}
                      className="text-red-600 hover:text-red-700"
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              
              <CardContent className="space-y-3">
                <div className="text-sm space-y-2">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Loại:</span>
                    <span className="font-medium">{getDiscountTypeLabel(discount.type)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Giá trị:</span>
                    <span className="font-medium text-blue-600">{formatValue(discount.type, discount.value)}</span>
                  </div>
                  {discount.minOrderValue && (
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Đơn hàng tối thiểu:</span>
                      <span className="font-medium">{discount.minOrderValue.toLocaleString()} ₫</span>
                    </div>
                  )}
                  {discount.product && (
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Sản phẩm:</span>
                      <span className="font-medium">{discount.product.productName}</span>
                    </div>
                  )}
                  {discount.category && (
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Danh mục:</span>
                      <span className="font-medium">{discount.category.categoryName}</span>
                    </div>
                  )}
                </div>
                
                <div className="pt-2 border-t">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Đã sử dụng</span>
                    <Badge variant="outline">
                      <Users className="w-3 h-3 mr-1" />
                      {discount.usageCount}{discount.maxUsage ? `/${discount.maxUsage}` : ''}
                    </Badge>
                  </div>
                  {discount.endDate && (
                    <div className="flex items-center justify-between text-sm mt-1">
                      <span className="text-muted-foreground">Hết hạn</span>
                      <span className={`${isExpired ? 'text-red-600' : 'text-gray-600'}`}>
                        {new Date(discount.endDate).toLocaleDateString('vi-VN')}
                      </span>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {discounts.length === 0 && (
        <div className="text-center py-12">
          <div className="text-gray-400 mb-4">
            <Percent className="w-12 h-12 mx-auto" />
          </div>
          <h3 className="text-lg font-medium text-gray-900 mb-2">Chưa có giảm giá nào</h3>
          <p className="text-gray-500 mb-4">Bắt đầu bằng cách tạo chương trình giảm giá đầu tiên</p>
          <Button onClick={() => setIsAddDialogOpen(true)}>
            <Plus className="w-4 h-4 mr-2" />
            Tạo giảm giá đầu tiên
          </Button>
        </div>
      )}
    </div>
  );
}