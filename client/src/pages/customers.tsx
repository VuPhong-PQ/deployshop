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
import { z } from "zod";
import type { ApiCustomer, CustomerFormData, ApiStore, Customer, Order, OrderItem, CustomerDetailData } from "@/types/api";

const customerFormSchema = z.object({
  name: z.string().min(1, "Họ tên là bắt buộc"),
  phone: z.string().min(1, "Số điện thoại là bắt buộc"),
  email: z.string().email().optional().or(z.literal("")),
  address: z.string().optional(),
  storeId: z.string().min(1, "Cửa hàng là bắt buộc"),
  customerType: z.enum(["regular", "premium", "vip"]).optional().default("regular"),
  dateOfBirth: z.date().optional(),
  loyaltyPoints: z.number().optional().default(0),
  totalSpent: z.string().optional().default("0"),
  isActive: z.boolean().optional().default(true),
});

type CustomerFormData = z.infer<typeof customerFormSchema>;

export default function Customers() {
  // ...existing code...
  // Add customer mutation
  const addCustomerMutation = useMutation({
    mutationFn: async (customerData: CustomerFormData) => {
      const requestData = {
        hoTen: customerData.name,
        soDienThoai: customerData.phone,
        email: customerData.email || null,
        diaChi: customerData.address || null,
        hangKhachHang: customerData.customerType === 'vip' ? 'VIP' : 
                      customerData.customerType === 'premium' ? 'Premium' : 'Thuong',
        storeId: customerData.storeId ? parseInt(customerData.storeId) : null,
        loyaltyPoints: customerData.loyaltyPoints || 0,
        totalSpent: parseFloat(customerData.totalSpent || "0"),
        isActive: customerData.isActive !== undefined ? customerData.isActive : true,
        dateOfBirth: customerData.dateOfBirth?.toISOString() || null,
      };
      
      console.log('Sending add request for customer:', requestData);
      
      return apiRequest('/api/customers', {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify(requestData),
      });
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Khách hàng đã được thêm thành công",
      });
      // Force reload data immediately from server
      console.log('Customer added, refetching data...');
      refetch();
      setIsAddDialogOpen(false);
      form.reset();
    },
    onError: (error: any) => {
      console.error('Add customer error:', error);
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
  const [editingCustomer, setEditingCustomer] = useState<ApiCustomer | null>(null);
  const [selectedCustomer, setSelectedCustomer] = useState<ApiCustomer | null>(null);
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [isOrderDetailOpen, setIsOrderDetailOpen] = useState(false);

  // Fetch customers and orders
  const { data: rawCustomers = [], isLoading, refetch } = useQuery<ApiCustomer[]>({
    queryKey: ['/api/customers'],
    queryFn: async () => {
      console.log('Fetching customers from API...');
      const response = await apiRequest('/api/customers', { method: 'GET' });
      console.log('Raw API response:', response);
      return response;
    },
    // Override global settings - force fresh data
    staleTime: 0,
    gcTime: 0,
    refetchOnMount: true,
    refetchOnWindowFocus: true,
    retry: 1,
  });

  // Debug: log dữ liệu gốc từ API
  console.log('rawCustomers:', rawCustomers);
  // Map dữ liệu từ API sang đúng định dạng frontend
  rawCustomers.forEach((c, i) => {
    console.log(`Customer[${i}] hangKhachHang:`, c.hangKhachHang, 'type:', typeof c.hangKhachHang);
  });
  const customers: Customer[] = rawCustomers.map((c) => ({
    id: c.customerId?.toString(),
    name: c.hoTen || "",
    phone: c.soDienThoai || "",
    email: c.email || "",
    address: c.diaChi || "",
    customerType: 
      // Kiểm tra theo string chính xác
      c.hangKhachHang === 'VIP' ? 'vip'
      : c.hangKhachHang === 'Premium' ? 'premium'
      : c.hangKhachHang === 'Thuong' ? 'regular'
      // Fallback cho các giá trị số cũ (nếu có)
      : c.hangKhachHang === 3 ? 'vip'
      : c.hangKhachHang === 2 ? 'premium'
      : 'regular',
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

  // Fetch stores
  const { data: stores = [] } = useQuery<ApiStore[]>({
    queryKey: ['/api/stores'],
    queryFn: async () => {
      console.log('Fetching stores from API...');
      const response = await apiRequest('/api/stores', { method: 'GET' });
      console.log('Stores API response:', response);
      return response;
    },
  });

  // Fetch customer detail data when selected
  const { data: customerDetail, isLoading: isLoadingDetail, error: customerDetailError } = useQuery<CustomerDetailData>({
    queryKey: ['/api/customers', selectedCustomer?.customerId, 'detail'],
    queryFn: async () => {
      if (!selectedCustomer?.customerId) {
        console.log('No customerId available:', selectedCustomer);
        return null;
      }
      console.log('Fetching customer detail for:', selectedCustomer.customerId);
      try {
        const response = await apiRequest(`/api/customers/${selectedCustomer.customerId}`, { method: 'GET' });
        console.log('Customer detail response:', response);
        return response;
      } catch (error) {
        console.error('Customer detail API error:', error);
        throw error;
      }
    },
    enabled: !!selectedCustomer?.customerId,
    retry: 1,
  });

  // Form for adding/editing customers
  const form = useForm<CustomerFormData>({
    resolver: zodResolver(customerFormSchema),
    defaultValues: {
      name: "",
      phone: "",
      email: "",
      address: "",
      storeId: "",
      customerType: "regular",
      loyaltyPoints: 0,
      totalSpent: "0",
      isActive: true,
    },
  });

  // Edit customer mutation
  const editCustomerMutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: Partial<CustomerFormData> }) => {
      const requestData = {
        hoTen: data.name || '',
        soDienThoai: data.phone || '',
        email: data.email || '',
        diaChi: data.address || '',
        hangKhachHang: data.customerType === 'vip' ? 'VIP' : 
                      data.customerType === 'premium' ? 'Premium' : 'Thuong',
        storeId: data.storeId ? parseInt(data.storeId) : null,
        loyaltyPoints: data.loyaltyPoints || 0,
        totalSpent: parseFloat(data.totalSpent || "0"),
        isActive: data.isActive !== undefined ? data.isActive : true,
      };
      
      console.log('Sending update request for customer:', id, requestData);
      
      return apiRequest(`/api/customers/${id}`, {
        method: 'PUT',
        headers: { 
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify(requestData),
      });
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Thông tin khách hàng đã được cập nhật",
      });
      // Force reload data immediately from server
      console.log('Customer updated, refetching data...');
      refetch();
      setEditingCustomer(null);
      form.reset();
    },
    onError: (error: any) => {
      console.error('Update customer error:', error);
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
      console.log('Customer deleted, refetching data...');
      refetch();
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
    // Filter by tier using backend field
    const matchesTier = selectedTier === "all" || 
      (selectedTier === "vip" && customer.hangKhachHang === "VIP") ||
      (selectedTier === "premium" && customer.hangKhachHang === "Premium") ||
      (selectedTier === "regular" && customer.hangKhachHang === "Thuong") ||
      (selectedTier === "platinum" && customer.hangKhachHang === "Platinum");
    return matchesSearch && matchesTier;
  });

  // Get customer orders from original orders data (for card display)
  const getCustomerOrdersForCard = (customerId: string) => {
    const id = parseInt(customerId);
    return orders.filter(order => order.customerId === id);
  };

  // Get customer orders from detailed API (for modal tabs)
  const getCustomerOrders = () => {
    console.log('getCustomerOrders called - customerDetail:', customerDetail);
    console.log('customerDetail?.orders:', customerDetail?.orders);
    console.log('customerDetailError:', customerDetailError);
    const orders = customerDetail?.orders || [];
    console.log('Returning orders array length:', orders.length);
    return orders;
  };

  // Handle view order detail
  const handleViewOrderDetail = async (orderId: number) => {
    try {
      console.log('Fetching order detail for:', orderId);
      const orderDetail = await apiRequest(`/api/customers/orders/${orderId}`, { method: 'GET' });
      console.log('Order detail response:', orderDetail);
      setSelectedOrder(orderDetail);
      setIsOrderDetailOpen(true);
    } catch (error) {
      console.error('Failed to fetch order detail:', error);
      toast({
        title: "Lỗi",
        description: "Không thể tải chi tiết đơn hàng",
        variant: "destructive",
      });
    }
  };

  // Get customer tier badge
  const getTierBadge = (hangKhachHang: string) => {
    switch (hangKhachHang) {
      case 'VIP':
        return { label: 'VIP', color: 'bg-purple-500' };
      case 'Premium':
        return { label: 'Premium', color: 'bg-yellow-400 text-black' };
      case 'Platinum':
        return { label: 'Platinum', color: 'bg-gray-800' };
      case 'Thuong':
      default:
        return { label: 'Thường', color: 'bg-gray-500' };
    }
  };

  // Handle form submission
  const onSubmit = (data: CustomerFormData) => {
    console.log('Submit customer data:', data);
    if (editingCustomer) {
      // Nếu đang chỉnh sửa, gọi mutation cập nhật
      editCustomerMutation.mutate({ id: editingCustomer.customerId, data });
    } else {
      // Nếu thêm mới, gọi mutation thêm mới
      addCustomerMutation.mutate(data);
    }
  };

  // Handle edit customer
  const handleEditCustomer = (customer: ApiCustomer) => {
    console.log('Editing customer:', customer);
    setEditingCustomer(customer);
    form.reset({
      name: customer.hoTen || "",
      email: customer.email || "",
      phone: customer.soDienThoai || "",
      address: customer.diaChi || "",
      storeId: customer.storeId?.toString() || "",
      customerType: customer.hangKhachHang === 'VIP' ? 'vip' : 
                   customer.hangKhachHang === 'Premium' ? 'premium' : 'regular',
      loyaltyPoints: customer.loyaltyPoints || 0,
      totalSpent: customer.totalSpent.toString() || "0",
      isActive: customer.isActive,
    });
    setIsAddDialogOpen(true);
  };

  // Handle delete customer
  const handleDeleteCustomer = (id: number) => {
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
            <Button 
              variant="outline" 
              onClick={() => {
                console.log('Force refreshing customers data...');
                refetch();
              }}
              data-testid="button-refresh-data"
            >
              🔄 Refresh Data
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
                    name="storeId"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Cửa hàng *</FormLabel>
                        <Select onValueChange={field.onChange} value={field.value}>
                          <FormControl>
                            <SelectTrigger data-testid="select-store">
                              <SelectValue placeholder="Chọn cửa hàng" />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {stores.map((store) => (
                              <SelectItem key={store.storeId} value={store.storeId.toString()}>
                                {store.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
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
              const tierBadge = getTierBadge(customer.hangKhachHang);
              const customerOrders = getCustomerOrders(customer.id);
              const lastOrderDate = customerOrders.length > 0 
                ? new Date(customerOrders[0].createdAt).toLocaleDateString('vi-VN')
                : "Chưa có đơn hàng";

              return (
                <Card 
                  key={customer.customerId} 
                  className="cursor-pointer hover:shadow-md transition-shadow"
                  onClick={() => {
                    console.log('Clicked customer (mapped):', customer);
                    const rawCustomer = rawCustomers.find(raw => raw.customerId.toString() === customer.id);
                    console.log('Found rawCustomer:', rawCustomer);
                    setSelectedCustomer(rawCustomer || null);
                  }}
                  data-testid={`customer-card-${customer.customerId}`}
                >
                  <CardContent className="p-6">
                    <div className="flex items-start justify-between mb-4">
                      <div>
                        <h3 className="font-semibold text-lg mb-1" data-testid={`customer-name-${customer.customerId}`}>
                          {customer.hoTen}
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
                          data-testid={`button-edit-${customer.customerId}`}
                        >
                          <Edit className="w-4 h-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-red-600 hover:text-red-700"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDeleteCustomer(customer.customerId);
                          }}
                          data-testid={`button-delete-${customer.customerId}`}
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>

                    <div className="space-y-2 text-sm text-gray-600">
                      <div className="flex items-center">
                        <Phone className="w-4 h-4 mr-2" />
                        <span data-testid={`customer-phone-${customer.customerId}`}>{customer.soDienThoai}</span>
                      </div>
                      {customer.email && (
                        <div className="flex items-center">
                          <Mail className="w-4 h-4 mr-2" />
                          <span data-testid={`customer-email-${customer.customerId}`}>{customer.email}</span>
                        </div>
                      )}
                      {customer.diaChi && (
                        <div className="flex items-center">
                          <MapPin className="w-4 h-4 mr-2" />
                          <span className="line-clamp-1" data-testid={`customer-address-${customer.customerId}`}>
                            {customer.diaChi}
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
                        <span>{selectedCustomer.hoTen}</span>
                        <Badge className={`text-white ${getTierBadge(selectedCustomer.hangKhachHang).color}`}>
                          {getTierBadge(selectedCustomer.hangKhachHang).label}
                        </Badge>
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-3">
                      <div className="flex items-center">
                        <Phone className="w-4 h-4 mr-2 text-gray-500" />
                        <span>{selectedCustomer.soDienThoai}</span>
                      </div>
                      {selectedCustomer.email && (
                        <div className="flex items-center">
                          <Mail className="w-4 h-4 mr-2 text-gray-500" />
                          <span>{selectedCustomer.email}</span>
                        </div>
                      )}
                      {selectedCustomer.diaChi && (
                        <div className="flex items-center">
                          <MapPin className="w-4 h-4 mr-2 text-gray-500" />
                          <span>{selectedCustomer.address}</span>
                        </div>
                      )}
                    </CardContent>
                  </Card>
                </TabsContent>

                <TabsContent value="orders" className="space-y-4">
                  {console.log('Orders tab rendering - isLoadingDetail:', isLoadingDetail, 'getCustomerOrders().length:', getCustomerOrders().length)}
                  {isLoadingDetail ? (
                    <div className="text-center py-8">
                      <p>Đang tải đơn hàng...</p>
                    </div>
                  ) : (
                    <div className="space-y-3">
                      {getCustomerOrders().map((order, index) => (
                        <Card 
                          key={order.orderId} 
                          data-testid={`order-${index}`}
                          className="cursor-pointer hover:shadow-md transition-shadow"
                          onClick={() => handleViewOrderDetail(order.orderId)}
                        >
                          <CardContent className="p-4">
                            <div className="flex justify-between items-start">
                              <div>
                                <p className="font-medium">Đơn hàng #{order.orderNumber || order.orderId}</p>
                                <p className="text-sm text-gray-500">
                                  {new Date(order.createdAt).toLocaleString('vi-VN')}
                                </p>
                                <p className="text-sm text-gray-600 mt-1">
                                  {order.items?.length || 0} sản phẩm
                                </p>
                              </div>
                              <div className="text-right">
                                <p className="font-bold text-primary">
                                  {parseFloat(order.totalAmount || "0").toLocaleString('vi-VN')}₫
                                </p>
                                <Badge variant={order.status === 'completed' ? 'default' : 'secondary'}>
                                  {order.status === 'completed' ? 'Hoàn thành' : 
                                   order.status === 'pending' ? 'Chờ xử lý' : 
                                   order.status === 'processing' ? 'Đang xử lý' : 'Đang xử lý'}
                                </Badge>
                              </div>
                            </div>
                          </CardContent>
                        </Card>
                      ))}
                      {getCustomerOrders().length === 0 && (
                        <div className="text-center py-8 text-gray-500">
                          <ShoppingBag className="w-12 h-12 mx-auto mb-2 opacity-50" />
                          <p>Khách hàng chưa có đơn hàng nào</p>
                        </div>
                      )}
                    </div>
                  )}
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
                            {customerDetail?.loyaltyPoints || selectedCustomer.loyaltyPoints}
                          </p>
                          <p className="text-sm text-gray-500">Điểm hiện tại</p>
                        </div>
                        <div className="text-center">
                          <p className="text-2xl font-bold text-green-600">
                            {parseInt(customerDetail?.totalSpent || selectedCustomer.totalSpent.toString() || "0").toLocaleString('vi-VN')}₫
                          </p>
                          <p className="text-sm text-gray-500">Tổng chi tiêu</p>
                        </div>
                      </div>
                      
                      {/* Loyalty Transactions */}
                      {customerDetail?.loyaltyTransactions && customerDetail.loyaltyTransactions.length > 0 && (
                        <div className="mt-6">
                          <h4 className="font-medium mb-3">Lịch sử giao dịch điểm</h4>
                          <div className="space-y-2 max-h-40 overflow-y-auto">
                            {customerDetail.loyaltyTransactions.slice(0, 5).map((transaction, index) => (
                              <div key={index} className="flex justify-between items-center text-sm py-2 border-b">
                                <div>
                                  <p className="font-medium">{transaction.reason || 'Giao dịch điểm'}</p>
                                  <p className="text-gray-500">
                                    {new Date(transaction.processedAt).toLocaleDateString('vi-VN')}
                                  </p>
                                </div>
                                <div className={`font-bold ${transaction.points > 0 ? 'text-green-600' : 'text-red-600'}`}>
                                  {transaction.points > 0 ? '+' : ''}{transaction.points}
                                </div>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                      <div className="mt-6">
                        <p className="text-sm text-gray-600 mb-2">
                          Quyền lợi hạng {getTierBadge(customerDetail?.hangKhachHang || selectedCustomer.hangKhachHang).label}:
                        </p>
                        <ul className="text-sm space-y-1">
                          {(customerDetail?.hangKhachHang || selectedCustomer.hangKhachHang) === 'VIP' && (
                            <>
                              <li>• Giảm giá 15% cho tất cả sản phẩm</li>
                              <li>• Tích điểm x3</li>
                              <li>• Ưu tiên hỗ trợ khách hàng</li>
                            </>
                          )}
                          {(customerDetail?.hangKhachHang || selectedCustomer.hangKhachHang) === 'Premium' && (
                            <>
                              <li>• Giảm giá 10% cho tất cả sản phẩm</li>
                              <li>• Tích điểm x2</li>
                              <li>• Miễn phí giao hàng</li>
                            </>
                          )}
                          {(customerDetail?.hangKhachHang || selectedCustomer.hangKhachHang) === 'Thuong' && (
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

        {/* Order Detail Modal */}
        {selectedOrder && (
          <Dialog open={isOrderDetailOpen} onOpenChange={setIsOrderDetailOpen}>
            <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
              <DialogHeader>
                <DialogTitle>Chi tiết đơn hàng #{selectedOrder.orderNumber || selectedOrder.orderId}</DialogTitle>
              </DialogHeader>

              <div className="space-y-6">
                {/* Order Info */}
                <Card>
                  <CardHeader>
                    <CardTitle className="text-lg">Thông tin đơn hàng</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <p className="text-sm text-gray-500">Ngày tạo</p>
                        <p className="font-medium">
                          {new Date(selectedOrder.createdAt).toLocaleString('vi-VN')}
                        </p>
                      </div>
                      <div>
                        <p className="text-sm text-gray-500">Trạng thái</p>
                        <Badge variant={selectedOrder.status === 'completed' ? 'default' : 'secondary'}>
                          {selectedOrder.status === 'completed' ? 'Hoàn thành' : 
                           selectedOrder.status === 'pending' ? 'Chờ xử lý' : 
                           selectedOrder.status === 'processing' ? 'Đang xử lý' : 'Đang xử lý'}
                        </Badge>
                      </div>
                      <div>
                        <p className="text-sm text-gray-500">Tổng tiền</p>
                        <p className="font-bold text-primary text-lg">
                          {parseFloat(selectedOrder.totalAmount || "0").toLocaleString('vi-VN')}₫
                        </p>
                      </div>
                      <div>
                        <p className="text-sm text-gray-500">Phương thức thanh toán</p>
                        <p className="font-medium">
                          {selectedOrder.paymentMethod === 'cash' ? 'Tiền mặt' : 
                           selectedOrder.paymentMethod === 'card' ? 'Thẻ' : 'Khác'}
                        </p>
                      </div>
                    </div>
                  </CardContent>
                </Card>

                {/* Order Items */}
                <Card>
                  <CardHeader>
                    <CardTitle className="text-lg">Sản phẩm trong đơn hàng</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {selectedOrder.items?.map((item, index) => (
                        <div key={index} className="flex justify-between items-center py-3 border-b">
                          <div className="flex-1">
                            <p className="font-medium">{item.productName || item.product?.name || 'Sản phẩm'}</p>
                            <p className="text-sm text-gray-500">
                              {parseFloat(item.price || "0").toLocaleString('vi-VN')}₫ × {item.quantity}
                            </p>
                            {item.product?.sku && (
                              <p className="text-xs text-gray-400">SKU: {item.product.sku}</p>
                            )}
                          </div>
                          <div className="text-right">
                            <p className="font-bold">
                              {parseFloat(item.totalPrice || (parseFloat(item.price || "0") * item.quantity).toString()).toLocaleString('vi-VN')}₫
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                    
                    <div className="mt-4 pt-4 border-t">
                      <div className="flex justify-between items-center text-lg font-bold">
                        <span>Tổng cộng:</span>
                        <span className="text-primary">
                          {parseFloat(selectedOrder.totalAmount || "0").toLocaleString('vi-VN')}₫
                        </span>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </div>
            </DialogContent>
          </Dialog>
        )}
      </div>
    </AppLayout>
  );
}
