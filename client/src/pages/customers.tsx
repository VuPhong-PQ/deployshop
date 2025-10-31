import { useState } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useToast } from "@/hooks/use-toast";
import { queryClient, apiRequest } from "@/lib/queryClient";
import { normalizeSearchText } from "@/lib/utils";
import { Plus, Search, Edit, Trash2, Users, Phone, Mail, MapPin, ShoppingBag, Calendar, Star } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { insertCustomerSchema } from "@shared/schema";
import { z } from "zod";
import type { Customer, Order } from "@shared/schema";

const customerFormSchema = insertCustomerSchema.extend({
  phone: z.string().min(1, "Số điện thoại là bắt buộc"),
});

type CustomerFormData = z.infer<typeof customerFormSchema>;

export default function Customers() {
  // ...existing code...
  // Add customer mutation
  const addCustomerMutation = useMutation({
    mutationFn: async (customerData: CustomerFormData) => {
      const formData = new URLSearchParams();
      formData.append('hoTen', customerData.name);
      formData.append('soDienThoai', customerData.phone);
      formData.append('email', customerData.email || '');
      formData.append('diaChi', customerData.address || '');
      formData.append('hangKhachHang', customerData.hangKhachHang || 'Thuong');
      formData.append('storeId', customerData.storeId || 'store-1');
      formData.append('customerType', customerData.customerType || 'regular');
      return apiRequest('/api/customers', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: formData.toString(),
      });
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Khách hàng đã được thêm thành công",
      });
  // Invalidate lại cache để react-query tự động refetch
  queryClient.invalidateQueries({ queryKey: ['/api/customers'] });
      setIsAddDialogOpen(false);
      form.reset();
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể thêm khách hàng. Vui lòng thử lại.",
        variant: "destructive",
      });
    }
  });
  const { toast } = useToast();
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedTier, setSelectedTier] = useState<string>("all");
  // Hàm reset bộ lọc
  const resetFilters = () => {
    setSearchTerm("");
    setSelectedTier("all");
  };
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(null);
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);

  // Fetch customers and orders
  const { data: rawCustomers = [], isLoading } = useQuery<any[]>({
    queryKey: ['/api/customers'],
  });

  // Debug: log dữ liệu gốc từ API
  console.log('rawCustomers:', rawCustomers);
  // Map dữ liệu từ API sang đúng định dạng frontend
  rawCustomers.forEach((c, i) => {
    console.log(`Customer[${i}] hangKhachHang:`, c.hangKhachHang);
  });
  const customers: Customer[] = rawCustomers.map((c) => ({
    id: c.customerId?.toString(),
    name: c.hoTen || "",
    phone: c.soDienThoai || "",
    email: c.email || "",
    address: c.diaChi || "",
  customerType: c.hangKhachHang === 3 ? 'vip'
    : c.hangKhachHang === 2 ? 'premium'
    : c.hangKhachHang === 1 ? 'regular'
    : (typeof c.hangKhachHang === 'string' ?
        (c.hangKhachHang.toLowerCase() === 'vip' ? 'vip'
          : c.hangKhachHang.toLowerCase() === 'premium' ? 'premium'
          : 'regular')
        : 'regular'),
    loyaltyPoints: c.loyaltyPoints || 0,
    totalSpent: c.totalSpent || "0",
    storeId: c.storeId || "store-1",
    hangKhachHang: c.hangKhachHang || "Thuong",
    dateOfBirth: c.dateOfBirth ?? null,
    isActive: c.isActive ?? true,
    createdAt: c.createdAt ? new Date(c.createdAt) : new Date(),
    updatedAt: c.updatedAt ? new Date(c.updatedAt) : new Date(),
  }));
  // Debug: log giá trị customers
  console.log('Customers:', customers);

  const { data: orders = [] } = useQuery<Order[]>({
    queryKey: ['/api/orders'],
  });

  // Form for adding/editing customers
  const form = useForm<CustomerFormData>({
    // resolver: zodResolver(customerFormSchema), // Tạm thời bỏ validate để test submit
    defaultValues: {
      name: "",
      email: "",
      phone: "",
      address: "",
      storeId: "store-1",
      customerType: "regular",
      loyaltyPoints: 0,
      totalSpent: "0",
      hangKhachHang: "Thuong",
    },
  });

  // Edit customer mutation
  const editCustomerMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string; data: Partial<CustomerFormData> }) => {
      const form = new URLSearchParams();
      form.append('hoTen', data.name || '');
      form.append('soDienThoai', data.phone || '');
      form.append('email', data.email || '');
      form.append('diaChi', data.address || '');
  let hangKhachHang = 1;
  if (data.customerType === 'vip') hangKhachHang = 3;
  else if (data.customerType === 'premium') hangKhachHang = 2;
  form.append('hangKhachHang', hangKhachHang.toString());
  form.append('customerType', data.customerType || 'regular');
      return apiRequest(`/api/customers/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: form.toString(),
      });
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Thông tin khách hàng đã được cập nhật",
      });
      queryClient.invalidateQueries({ queryKey: ['/api/customers'] });
      setEditingCustomer(null);
      form.reset();
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể cập nhật thông tin khách hàng",
        variant: "destructive",
      });
    }
  });

  // Delete customer mutation
  const deleteCustomerMutation = useMutation({
    mutationFn: async (id: string) => {
      return apiRequest(`/api/customers/${id}`, {
        method: 'DELETE',
      });
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Khách hàng đã được xóa",
      });
      queryClient.invalidateQueries({ queryKey: ['/api/customers'] });
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể xóa khách hàng",
        variant: "destructive",
      });
    }
  });

  // Filter customers with Vietnamese diacritics support
  const filteredCustomers = customers.filter(customer => {
    if (!customer || !customer.name || !customer.phone) return false;
    
    const searchNormalized = normalizeSearchText(searchTerm);
    const customerNameNormalized = normalizeSearchText(customer.name || '');
    const customerPhoneNormalized = normalizeSearchText(customer.phone || '');
    const customerEmailNormalized = normalizeSearchText(customer.email || '');
    
    const matchesSearch = customerNameNormalized.includes(searchNormalized) ||
                         customerPhoneNormalized.includes(searchNormalized) ||
                         customerEmailNormalized.includes(searchNormalized);
    // Nếu không có trường customerType, luôn cho qua filter tier
    const matchesTier = selectedTier === "all" || !customer.customerType || customer.customerType === selectedTier;
    return matchesSearch && matchesTier;
  });

  // Get customer orders
  const getCustomerOrders = (customerId: string) => {
    return orders.filter(order => order.customerId === customerId);
  };

  // Get customer tier badge
  const getTierBadge = (type: string) => {
    switch (type) {
      case 'vip':
        return { label: 'VIP', color: 'bg-purple-500' };
      case 'premium':
        return { label: 'Premium', color: 'bg-yellow-400 text-black' };
      case 'regular':
        return { label: 'Thường', color: 'bg-gray-500' };
      default:
        return { label: 'Thường', color: 'bg-gray-500' };
    }
  };

  // Handle form submission
  const onSubmit = (data: CustomerFormData) => {
    console.log('Submit customer data:', data);
    if (editingCustomer) {
      // Nếu đang chỉnh sửa, gọi mutation cập nhật
      editCustomerMutation.mutate({ id: editingCustomer.id, data });
    } else {
      // Nếu thêm mới, gọi mutation thêm mới
      addCustomerMutation.mutate(data);
    }
  };

  // Handle edit customer
  const handleEditCustomer = (customer: Customer) => {
    setEditingCustomer(customer);
    form.reset({
      name: customer.name,
      email: customer.email || "",
      phone: customer.phone,
      address: customer.address || "",
      storeId: customer.storeId,
      customerType: customer.customerType,
      loyaltyPoints: customer.loyaltyPoints,
      totalSpent: customer.totalSpent,
    });
    setIsAddDialogOpen(true);
  };

  // Handle delete customer
  const handleDeleteCustomer = (id: string) => {
    if (confirm("Bạn có chắc chắn muốn xóa khách hàng này?")) {
      deleteCustomerMutation.mutate(id);
    }
  };

  return (
    <AppLayout title="Khách hàng">
      <div data-testid="customers-page">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center space-x-4">
            <div className="relative w-80">
              <Input
                placeholder="Tìm kiếm khách hàng (có thể gõ không dấu)..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
                data-testid="input-customer-search"
              />
              <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
            </div>
            <Select value={selectedTier} onValueChange={setSelectedTier}>
              <SelectTrigger className="w-48" data-testid="select-tier-filter">
                <SelectValue placeholder="Tất cả hạng" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả hạng</SelectItem>
                <SelectItem value="regular">Thường</SelectItem>
                <SelectItem value="premium">Premium</SelectItem>
                <SelectItem value="vip">VIP</SelectItem>
              </SelectContent>
            </Select>
            <Button variant="outline" onClick={resetFilters} data-testid="button-reset-filters">
              Xóa bộ lọc
            </Button>
          </div>

          <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
            <DialogTrigger asChild>
              <Button
                onClick={() => {
                  setEditingCustomer(null);
                  form.reset();
                }}
                data-testid="button-add-customer"
              >
                <Plus className="w-4 h-4 mr-2" />
                Thêm khách hàng
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-lg">
              <DialogHeader>
                <DialogTitle>
                  {editingCustomer ? "Chỉnh sửa khách hàng" : "Thêm khách hàng mới"}
                </DialogTitle>
              </DialogHeader>

              <Form {...form}>
                {console.log('Form rendered')}
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                  <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Họ tên *</FormLabel>
                        <FormControl>
                          <Input {...field} data-testid="input-customer-name" />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="phone"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Số điện thoại *</FormLabel>
                        <FormControl>
                          <Input {...field} data-testid="input-customer-phone" />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="email"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Email</FormLabel>
                        <FormControl>
                          <Input {...field} type="email" data-testid="input-customer-email" />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="address"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Địa chỉ</FormLabel>
                        <FormControl>
                          <Input {...field} data-testid="input-customer-address" />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="customerType"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Hạng khách hàng</FormLabel>
                        <Select onValueChange={field.onChange} value={field.value}>
                          <FormControl>
                            <SelectTrigger data-testid="select-customer-type">
                              <SelectValue />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value="regular">Thường</SelectItem>
                            <SelectItem value="premium">Premium</SelectItem>
                            <SelectItem value="vip">VIP</SelectItem>
                          </SelectContent>
                        </Select>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div className="flex justify-end space-x-2">
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => setIsAddDialogOpen(false)}
                      data-testid="button-cancel"
                    >
                      Hủy
                    </Button>
                    <Button
                      type="submit"
                      disabled={addCustomerMutation.isPending || editCustomerMutation.isPending}
                      data-testid="button-save-customer"
                    >
                      {addCustomerMutation.isPending || editCustomerMutation.isPending 
                        ? "Đang lưu..." 
                        : (editingCustomer ? "Cập nhật" : "Thêm")
                      }
                    </Button>
                  </div>
                </form>
              </Form>
            </DialogContent>
          </Dialog>
        </div>

        {/* Customer Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
          {isLoading ? (
            // Loading skeleton
            [...Array(6)].map((_, i) => (
              <Card key={i} className="animate-pulse">
                <CardContent className="p-6">
                  <div className="h-4 bg-gray-200 rounded mb-2"></div>
                  <div className="h-4 bg-gray-200 rounded w-3/4 mb-4"></div>
                  <div className="h-20 bg-gray-200 rounded"></div>
                </CardContent>
              </Card>
            ))
          ) : filteredCustomers.length === 0 ? (
            <Card className="col-span-full">
              <CardContent className="p-12 text-center">
                <Users className="w-16 h-16 mx-auto mb-4 text-gray-400" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">
                  Không tìm thấy khách hàng
                </h3>
                <p className="text-gray-500">
                  {searchTerm || selectedTier !== "all" 
                    ? "Thử thay đổi bộ lọc tìm kiếm"
                    : "Bắt đầu bằng cách thêm khách hàng đầu tiên"
                  }
                </p>
              </CardContent>
            </Card>
          ) : (
            filteredCustomers.map((customer) => {
              const tierBadge = getTierBadge(customer.customerType);
              const customerOrders = getCustomerOrders(customer.id);
              const lastOrderDate = customerOrders.length > 0 
                ? new Date(customerOrders[0].createdAt).toLocaleDateString('vi-VN')
                : "Chưa có đơn hàng";

              return (
                <Card 
                  key={customer.id} 
                  className="cursor-pointer hover:shadow-md transition-shadow"
                  onClick={() => setSelectedCustomer(customer)}
                  data-testid={`customer-card-${customer.id}`}
                >
                  <CardContent className="p-6">
                    <div className="flex items-start justify-between mb-4">
                      <div>
                        <h3 className="font-semibold text-lg mb-1" data-testid={`customer-name-${customer.id}`}>
                          {customer.name}
                        </h3>
                        <Badge className={`text-white ${tierBadge.color}`}>
                          {tierBadge.label}
                        </Badge>
                      </div>
                      <div className="flex space-x-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleEditCustomer(customer);
                          }}
                          data-testid={`button-edit-${customer.id}`}
                        >
                          <Edit className="w-4 h-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-red-600 hover:text-red-700"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDeleteCustomer(customer.id);
                          }}
                          data-testid={`button-delete-${customer.id}`}
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>

                    <div className="space-y-2 text-sm text-gray-600">
                      <div className="flex items-center">
                        <Phone className="w-4 h-4 mr-2" />
                        <span data-testid={`customer-phone-${customer.id}`}>{customer.phone}</span>
                      </div>
                      {customer.email && (
                        <div className="flex items-center">
                          <Mail className="w-4 h-4 mr-2" />
                          <span data-testid={`customer-email-${customer.id}`}>{customer.email}</span>
                        </div>
                      )}
                      {customer.address && (
                        <div className="flex items-center">
                          <MapPin className="w-4 h-4 mr-2" />
                          <span className="line-clamp-1" data-testid={`customer-address-${customer.id}`}>
                            {customer.address}
                          </span>
                        </div>
                      )}
                    </div>

                    <div className="mt-4 pt-4 border-t border-gray-100">
                      <div className="grid grid-cols-2 gap-4 text-center">
                        <div>
                          <p className="text-sm text-gray-500">Điểm tích lũy</p>
                          <p className="font-semibold text-primary" data-testid={`customer-points-${customer.id}`}>
                            {customer.loyaltyPoints}
                          </p>
                        </div>
                        <div>
                          <p className="text-sm text-gray-500">Tổng chi tiêu</p>
                          <p className="font-semibold text-green-600" data-testid={`customer-spent-${customer.id}`}>
                            {parseInt(customer.totalSpent).toLocaleString('vi-VN')}₫
                          </p>
                        </div>
                      </div>
                      <div className="mt-2 text-center">
                        <p className="text-xs text-gray-500">
                          Đơn gần nhất: {lastOrderDate}
                        </p>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              );
            })
          )}
        </div>

        {/* Customer Detail Modal */}
        {selectedCustomer && (
          <Dialog open={!!selectedCustomer} onOpenChange={() => setSelectedCustomer(null)}>
            <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
              <DialogHeader>
                <DialogTitle>Chi tiết khách hàng</DialogTitle>
              </DialogHeader>

              <Tabs defaultValue="info" className="space-y-4">
                <TabsList className="grid w-full grid-cols-3">
                  <TabsTrigger value="info" data-testid="tab-customer-info">Thông tin</TabsTrigger>
                  <TabsTrigger value="orders" data-testid="tab-customer-orders">Đơn hàng</TabsTrigger>
                  <TabsTrigger value="loyalty" data-testid="tab-customer-loyalty">Điểm thưởng</TabsTrigger>
                </TabsList>

                <TabsContent value="info" className="space-y-4">
                  <Card>
                    <CardHeader>
                      <CardTitle className="flex items-center justify-between">
                        <span>{selectedCustomer.name}</span>
                        <Badge className={`text-white ${getTierBadge(selectedCustomer.customerType).color}`}>
                          {getTierBadge(selectedCustomer.customerType).label}
                        </Badge>
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-3">
                      <div className="flex items-center">
                        <Phone className="w-4 h-4 mr-2 text-gray-500" />
                        <span>{selectedCustomer.phone}</span>
                      </div>
                      {selectedCustomer.email && (
                        <div className="flex items-center">
                          <Mail className="w-4 h-4 mr-2 text-gray-500" />
                          <span>{selectedCustomer.email}</span>
                        </div>
                      )}
                      {selectedCustomer.address && (
                        <div className="flex items-center">
                          <MapPin className="w-4 h-4 mr-2 text-gray-500" />
                          <span>{selectedCustomer.address}</span>
                        </div>
                      )}
                    </CardContent>
                  </Card>
                </TabsContent>

                <TabsContent value="orders" className="space-y-4">
                  <div className="space-y-3">
                    {getCustomerOrders(selectedCustomer.id).map((order, index) => (
                      <Card key={order.id} data-testid={`order-${index}`}>
                        <CardContent className="p-4">
                          <div className="flex justify-between items-start">
                            <div>
                              <p className="font-medium">Đơn hàng #{order.orderNumber}</p>
                              <p className="text-sm text-gray-500">
                                {new Date(order.createdAt).toLocaleString('vi-VN')}
                              </p>
                            </div>
                            <div className="text-right">
                              <p className="font-bold text-primary">
                                {parseInt(order.total).toLocaleString('vi-VN')}₫
                              </p>
                              <Badge variant={order.status === 'completed' ? 'default' : 'secondary'}>
                                {order.status === 'completed' ? 'Hoàn thành' : 'Đang xử lý'}
                              </Badge>
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    ))}
                    {getCustomerOrders(selectedCustomer.id).length === 0 && (
                      <div className="text-center py-8 text-gray-500">
                        <ShoppingBag className="w-12 h-12 mx-auto mb-2 opacity-50" />
                        <p>Khách hàng chưa có đơn hàng nào</p>
                      </div>
                    )}
                  </div>
                </TabsContent>

                <TabsContent value="loyalty" className="space-y-4">
                  <Card>
                    <CardHeader>
                      <CardTitle className="flex items-center">
                        <Star className="w-5 h-5 mr-2 text-yellow-500" />
                        Chương trình điểm thưởng
                      </CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="grid grid-cols-2 gap-6">
                        <div className="text-center">
                          <p className="text-2xl font-bold text-primary">
                            {selectedCustomer.loyaltyPoints}
                          </p>
                          <p className="text-sm text-gray-500">Điểm hiện tại</p>
                        </div>
                        <div className="text-center">
                          <p className="text-2xl font-bold text-green-600">
                            {parseInt(selectedCustomer.totalSpent).toLocaleString('vi-VN')}₫
                          </p>
                          <p className="text-sm text-gray-500">Tổng chi tiêu</p>
                        </div>
                      </div>
                      <div className="mt-6">
                        <p className="text-sm text-gray-600 mb-2">Quyền lợi hạng {getTierBadge(selectedCustomer.customerType).label}:</p>
                        <ul className="text-sm space-y-1">
                          {selectedCustomer.customerType === 'vip' && (
                            <>
                              <li>• Giảm giá 15% cho tất cả sản phẩm</li>
                              <li>• Tích điểm x3</li>
                              <li>• Ưu tiên hỗ trợ khách hàng</li>
                            </>
                          )}
                          {selectedCustomer.customerType === 'premium' && (
                            <>
                              <li>• Giảm giá 10% cho tất cả sản phẩm</li>
                              <li>• Tích điểm x2</li>
                              <li>• Miễn phí giao hàng</li>
                            </>
                          )}
                          {selectedCustomer.customerType === 'regular' && (
                            <>
                              <li>• Tích điểm tiêu chuẩn</li>
                              <li>• Ưu đãi đặc biệt theo mùa</li>
                            </>
                          )}
                        </ul>
                      </div>
                    </CardContent>
                  </Card>
                </TabsContent>
              </Tabs>
            </DialogContent>
          </Dialog>
        )}
      </div>
    </AppLayout>
  );
}
