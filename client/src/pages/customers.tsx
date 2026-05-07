import { useState, useMemo } from "react";
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
import { Plus, Search, Edit, Trash2, Users, Phone, Mail, MapPin, ShoppingBag, Calendar, Star, RotateCcw } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { z } from "zod";
import type { ApiCustomer, CustomerFormData, ApiStore, Customer, Order, OrderItem, CustomerDetailData } from "@/types/api";
import { useAuth } from "@/contexts/auth-context";

const customerFormSchema = z.object({
  name: z.string().min(1, "Há» tÃªn lÃ  báº¯t buá»™c"),
  phone: z.string().min(1, "Sá»‘ Ä‘iá»‡n thoáº¡i lÃ  báº¯t buá»™c"),
  email: z.string().email().optional().or(z.literal("")),
  address: z.string().optional(),
  storeId: z.string().min(1, "Cá»­a hÃ ng lÃ  báº¯t buá»™c"),
  customerType: z.enum(["regular", "premium", "vip"]).optional().default("regular"),
  dateOfBirth: z.date().optional(),
  loyaltyPoints: z.number().optional().default(0),
  totalSpent: z.string().optional().default("0"),
  isActive: z.boolean().optional().default(true),
});

type CustomerFormData = z.infer<typeof customerFormSchema>;

export default function Customers() {
  // ...existing code...
  const { currentStore } = useAuth();
  // Add customer mutation
  const addCustomerMutation = useMutation({
    mutationFn: async (customerData: CustomerFormData) => {
      const requestData = {
        hoTen: customerData.name,
        soDienThoai: customerData.phone,
        email: customerData.email || null,
        diaChi: customerData.address || null,
        hangKhachHang: customerData.customerType === 'vip' ? 'VIP' : 
                      customerData.customerType === 'platinum' ? 'Platinum' :
                      customerData.customerType === 'premium' ? 'Premium' : 'Thuong',
        storeId: customerData.storeId ? parseInt(customerData.storeId) : null,
        loyaltyPoints: customerData.loyaltyPoints || 0,
        totalSpent: parseFloat(customerData.totalSpent || "0"),
        isActive: customerData.isActive !== undefined ? customerData.isActive : true,
        dateOfBirth: customerData.dateOfBirth?.toISOString() || null,
      };
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
        title: "ThÃ nh cÃ´ng",
        description: "KhÃ¡ch hÃ ng Ä‘Ã£ Ä‘Æ°á»£c thÃªm thÃ nh cÃ´ng",
      });
      // Force reload data immediately from server
      refetch();
      setIsAddDialogOpen(false);
      form.reset();
    },
    onError: (error: any) => {
      console.error('Add customer error:', error);
      toast({
        title: "Lá»—i",
        description: "KhÃ´ng thá»ƒ thÃªm khÃ¡ch hÃ ng. Vui lÃ²ng thá»­ láº¡i.",
        variant: "destructive",
      });
    }
  });
  const { toast } = useToast();
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedTier, setSelectedTier] = useState<string>("all");
  const [showInactive, setShowInactive] = useState(false);
  // HÃ m reset bá»™ lá»c
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
    queryKey: ['/api/customers', currentStore?.storeId, showInactive],
    queryFn: async () => {
      const storeParam = currentStore?.storeId ? `?storeId=${currentStore.storeId}` : '';
      const endpointBase = showInactive ? '/api/customers/inactive' : '/api/customers';
      const endpoint = `${endpointBase}${storeParam}`;
      const response = await apiRequest(endpoint, { method: 'GET' });
      return response;
    },
    // Only fetch when a store is selected to avoid empty responses from store-scoped endpoints
    enabled: !!currentStore?.storeId,
    // Override global settings - force fresh data
    staleTime: 0,
    gcTime: 0,
    refetchOnMount: true,
    refetchOnWindowFocus: true,
    retry: 1,
  });

  // Debug: log dá»¯ liá»‡u gá»‘c tá»« API
  // Map dá»¯ liá»‡u tá»« API sang Ä‘Ãºng Ä‘á»‹nh dáº¡ng frontend
  rawCustomers.forEach((c, i) => {
  });
  const customers: Customer[] = rawCustomers.map((c) => ({
    id: c.customerId?.toString(),
    name: c.hoTen || "",
    phone: c.soDienThoai || "",
    email: c.email || "",
    address: c.diaChi || "",
    customerType: 
      // Kiá»ƒm tra theo string chÃ­nh xÃ¡c
      c.hangKhachHang === 'VIP' ? 'vip'
      : c.hangKhachHang === 'Platinum' ? 'platinum'
      : c.hangKhachHang === 'Premium' ? 'premium'
      : c.hangKhachHang === 'Silver' ? 'premium'
      : c.hangKhachHang === 'Bronze' ? 'regular'
      : c.hangKhachHang === 'Thuong' ? 'regular'
      // Fallback cho cÃ¡c giÃ¡ trá»‹ sá»‘ cÅ© (náº¿u cÃ³)
      : c.hangKhachHang === 3 ? 'platinum'
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
  // Debug: log giÃ¡ trá»‹ customers
  const { data: orders = [] } = useQuery<Order[]>({
    queryKey: ['/api/orders'],
  });

  // Fetch stores
  const { data: stores = [] } = useQuery<ApiStore[]>({
    queryKey: ['/api/stores'],
    queryFn: async () => {
      const response = await apiRequest('/api/stores', { method: 'GET' });
      return response;
    },
  });

  // Fetch customer detail data when selected
  const { data: customerDetail, isLoading: isLoadingDetail, error: customerDetailError } = useQuery<CustomerDetailData>({
    queryKey: ['/api/customers', selectedCustomer?.customerId, 'detail'],
    queryFn: async () => {
      if (!selectedCustomer?.customerId) {
        return null;
      }
      try {
        const response = await apiRequest(`/api/customers/${selectedCustomer.customerId}`, { method: 'GET' });
        return response;
      } catch (error) {
        console.error('Customer detail API error:', error);
        throw error;
      }
    },
    enabled: !!selectedCustomer?.customerId,
    retry: 1,
  });

  // Fetch loyalty transactions separately to ensure fresh data
  const { data: loyaltyTransactions, isLoading: isLoadingLoyalty, refetch: refetchTransactions } = useQuery({
    queryKey: ['/api/LoyaltyTransactions/customer', selectedCustomer?.customerId],
    queryFn: async () => {
      if (!selectedCustomer?.customerId) return null;
      try {
        const response = await apiRequest(`/api/LoyaltyTransactions/customer/${selectedCustomer.customerId}`, { method: 'GET' });
        return response?.transactions || [];
      } catch (error) {
        console.error('Loyalty transactions API error:', error);
        return [];
      }
    },
    enabled: !!selectedCustomer?.customerId,
    retry: 1,
    refetchInterval: 30000, // Refresh every 30 seconds
    refetchIntervalInBackground: false,
  });



  // Fetch inactive customers
  const { data: inactiveCustomers = [] } = useQuery<ApiCustomer[]>({
    queryKey: ['/api/customers/inactive', currentStore?.storeId],
    queryFn: async () => {
      const storeParam = currentStore?.storeId ? `?storeId=${currentStore.storeId}` : '';
      const response = await apiRequest(`/api/customers/inactive${storeParam}`, { method: 'GET' });
      return response;
    },
    enabled: showInactive && !!currentStore?.storeId,
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
                      data.customerType === 'platinum' ? 'Platinum' :
                      data.customerType === 'premium' ? 'Premium' : 'Thuong',
        storeId: data.storeId ? parseInt(data.storeId) : null,
        loyaltyPoints: data.loyaltyPoints || 0,
        totalSpent: parseFloat(data.totalSpent || "0"),
        isActive: data.isActive !== undefined ? data.isActive : true,
      };
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
        title: "ThÃ nh cÃ´ng",
        description: "ThÃ´ng tin khÃ¡ch hÃ ng Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t",
      });
      // Force reload data immediately from server
      refetch();
      setEditingCustomer(null);
      form.reset();
    },
    onError: (error: any) => {
      console.error('Update customer error:', error);
      toast({
        title: "Lá»—i",
        description: "KhÃ´ng thá»ƒ cáº­p nháº­t thÃ´ng tin khÃ¡ch hÃ ng",
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
        title: "ThÃ nh cÃ´ng",
        description: "KhÃ¡ch hÃ ng Ä‘Ã£ Ä‘Æ°á»£c xÃ³a",
      });
      refetch();
    },
    onError: () => {
      toast({
        title: "Lá»—i",
        description: "KhÃ´ng thá»ƒ xÃ³a khÃ¡ch hÃ ng",
        variant: "destructive",
      });
    }
  });

  // Restore customer mutation
  const restoreCustomerMutation = useMutation({
    mutationFn: async (id: number) => {
      return apiRequest(`/api/customers/restore/${id}`, {
        method: 'PUT',
      });
    },
    onSuccess: () => {
      toast({
        title: "ThÃ nh cÃ´ng",
        description: "KhÃ¡ch hÃ ng Ä‘Ã£ Ä‘Æ°á»£c khÃ´i phá»¥c",
      });
      refetch();
    },
    onError: () => {
      toast({
        title: "Lá»—i",
        description: "KhÃ´ng thá»ƒ khÃ´i phá»¥c khÃ¡ch hÃ ng",
        variant: "destructive",
      });
    }
  });

  // Filter customers with Vietnamese diacritics support
  const sourceCustomers = showInactive ? inactiveCustomers : customers;
  const filteredCustomers = sourceCustomers.filter(customer => {
    if (!customer) return false;
    
    // Handle different data structures (mapped vs raw)
    const customerName = customer.name || customer.hoTen || '';
    const customerPhone = customer.phone || customer.soDienThoai || '';
    const customerEmail = customer.email || '';
    
    if (!customerName || !customerPhone) return false;
    
    const searchNormalized = normalizeSearchText(searchTerm);
    const customerNameNormalized = normalizeSearchText(customerName);
    const customerPhoneNormalized = normalizeSearchText(customerPhone);
    const customerEmailNormalized = normalizeSearchText(customerEmail);
    
    const matchesSearch = customerNameNormalized.includes(searchNormalized) ||
                         customerPhoneNormalized.includes(searchNormalized) ||
                         customerEmailNormalized.includes(searchNormalized);
    
    // Filter by tier using backend field (both mapped and raw)
    const tierField = customer.hangKhachHang || customer.customerType;
    const matchesTier = selectedTier === "all" || 
      (selectedTier === "vip" && (tierField === "VIP" || tierField === "vip")) ||
      (selectedTier === "premium" && (tierField === "Premium" || tierField === "premium")) ||
      (selectedTier === "regular" && (tierField === "Thuong" || tierField === "regular")) ||
      (selectedTier === "platinum" && tierField === "Platinum");
    return matchesSearch && matchesTier;
  });

  // Get customer orders from original orders data (for card display)
  const getCustomerOrdersForCard = (customerId: string) => {
    const id = parseInt(customerId);
    return orders.filter(order => order.customerId === id);
  };

  // Get customer orders from detailed API (for modal tabs)
  const getCustomerOrders = () => {
    const orders = customerDetail?.orders || [];
    return orders;
  };

  // Handle view order detail
  const handleViewOrderDetail = async (orderId: number) => {
    try {
      const orderDetail = await apiRequest(`/api/customers/orders/${orderId}`, { method: 'GET' });
      setSelectedOrder(orderDetail);
      setIsOrderDetailOpen(true);
    } catch (error) {
      console.error('Failed to fetch order detail:', error);
      toast({
        title: "Lá»—i",
        description: "KhÃ´ng thá»ƒ táº£i chi tiáº¿t Ä‘Æ¡n hÃ ng",
        variant: "destructive",
      });
    }
  };

  // Get customer tier badge with Vietnamese names
  const getTierBadge = (hangKhachHang: string) => {
    // Map English tier names to Vietnamese
    const tierMapping = {
      "Bronze": "Äá»“ng",
      "Silver": "Báº¡c", 
      "Platinum": "VÃ ng",
      "VIP": "Kim cÆ°Æ¡ng"
    };

    // Get Vietnamese name, fallback to original if not found
    const vietnameseName = tierMapping[hangKhachHang as keyof typeof tierMapping] || hangKhachHang;

    switch (hangKhachHang) {
      case 'VIP':
      case 'Kim cÆ°Æ¡ng':  // ThÃªm case cho tÃªn tiáº¿ng Viá»‡t tá»« backend
        return { label: 'Kim cÆ°Æ¡ng', color: 'bg-purple-500' };
      case 'Premium':
        return { label: 'Premium', color: 'bg-yellow-400 text-black' };
      case 'Platinum':
      case 'VÃ ng':  // ThÃªm case cho tÃªn tiáº¿ng Viá»‡t tá»« backend
        return { label: 'VÃ ng', color: 'bg-yellow-500' };
      case 'Silver':
      case 'Báº¡c':  // ThÃªm case cho tÃªn tiáº¿ng Viá»‡t tá»« backend
        return { label: 'Báº¡c', color: 'bg-gray-400' };
      case 'Bronze':
      case 'Äá»“ng':  // ThÃªm case cho tÃªn tiáº¿ng Viá»‡t tá»« backend
        return { label: 'Äá»“ng', color: 'bg-orange-600' };
      case 'Thuong':
      default:
        return { label: 'ThÆ°á»ng', color: 'bg-gray-500' };
    }
  };

  // Handle form submission
  const onSubmit = (data: CustomerFormData) => {
    if (editingCustomer) {
      // Náº¿u Ä‘ang chá»‰nh sá»­a, gá»i mutation cáº­p nháº­t
      editCustomerMutation.mutate({ id: editingCustomer.customerId, data });
    } else {
      // Náº¿u thÃªm má»›i, gá»i mutation thÃªm má»›i
      addCustomerMutation.mutate(data);
    }
  };

  // Handle edit customer
  const handleEditCustomer = (customer: ApiCustomer) => {
    setEditingCustomer(customer);
    form.reset({
      name: customer.hoTen || "",
      email: customer.email || "",
      phone: customer.soDienThoai || "",
      address: customer.diaChi || "",
      storeId: customer.storeId?.toString() || "",
      customerType: customer.hangKhachHang === 'VIP' ? 'vip' : 
                   customer.hangKhachHang === 'Platinum' ? 'platinum' :
                   customer.hangKhachHang === 'Premium' ? 'premium' : 
                   customer.hangKhachHang === 'Silver' ? 'premium' : 'regular',
      loyaltyPoints: customer.loyaltyPoints || 0,
      totalSpent: customer.totalSpent.toString() || "0",
      isActive: customer.isActive,
    });
    setIsAddDialogOpen(true);
  };

  // Handle delete customer
  const handleDeleteCustomer = (id: number) => {
    if (confirm("Báº¡n cÃ³ cháº¯c cháº¯n muá»‘n xÃ³a khÃ¡ch hÃ ng nÃ y?")) {
      deleteCustomerMutation.mutate(id.toString());
    }
  };

  // Handle restore customer
  const handleRestoreCustomer = (id: number) => {
    if (confirm("Báº¡n cÃ³ cháº¯c cháº¯n muá»‘n khÃ´i phá»¥c khÃ¡ch hÃ ng nÃ y?")) {
      restoreCustomerMutation.mutate(id);
    }
  };

  return (
    <AppLayout title="KhÃ¡ch hÃ ng">
      <div data-testid="customers-page">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center space-x-4">
            <div className="relative w-80">
              <Input
                placeholder="TÃ¬m kiáº¿m khÃ¡ch hÃ ng (cÃ³ thá»ƒ gÃµ khÃ´ng dáº¥u)..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
                data-testid="input-customer-search"
              />
              <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
            </div>
            <Select value={selectedTier} onValueChange={setSelectedTier}>
              <SelectTrigger className="w-48" data-testid="select-tier-filter">
                <SelectValue placeholder="Táº¥t cáº£ háº¡ng" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Táº¥t cáº£ háº¡ng</SelectItem>
                <SelectItem value="regular">ThÆ°á»ng</SelectItem>
                <SelectItem value="premium">Premium</SelectItem>
                <SelectItem value="platinum">VÃ ng</SelectItem>
                <SelectItem value="vip">Kim cÆ°Æ¡ng</SelectItem>
              </SelectContent>
            </Select>
            <Button variant="outline" onClick={resetFilters} data-testid="button-reset-filters">
              XÃ³a bá»™ lá»c
            </Button>
            <Button 
              variant={showInactive ? "default" : "outline"}
              onClick={() => setShowInactive(!showInactive)} 
              data-testid="button-toggle-inactive"
            >
              {showInactive ? "Hiá»ƒn thá»‹ hoáº¡t Ä‘á»™ng" : "Hiá»ƒn thá»‹ Ä‘Ã£ xÃ³a"}
            </Button>
              <Button 
              variant="outline" 
              onClick={() => refetch()}
              data-testid="button-refresh-data"
            >
              ðŸ”„ Refresh Data
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
                ThÃªm khÃ¡ch hÃ ng
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-lg">
              <DialogHeader>
                <DialogTitle>
                  {editingCustomer ? "Chá»‰nh sá»­a khÃ¡ch hÃ ng" : "ThÃªm khÃ¡ch hÃ ng má»›i"}
                </DialogTitle>
              </DialogHeader>

              <Form {...form}>
                {/* form render debug removed */}
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                  <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Há» tÃªn *</FormLabel>
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
                        <FormLabel>Sá»‘ Ä‘iá»‡n thoáº¡i *</FormLabel>
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
                        <FormLabel>Äá»‹a chá»‰</FormLabel>
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
                        <FormLabel>Cá»­a hÃ ng *</FormLabel>
                        <Select onValueChange={field.onChange} value={field.value}>
                          <FormControl>
                            <SelectTrigger data-testid="select-store">
                              <SelectValue placeholder="Chá»n cá»­a hÃ ng" />
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
                        <FormLabel>Háº¡ng khÃ¡ch hÃ ng</FormLabel>
                        <Select onValueChange={field.onChange} value={field.value}>
                          <FormControl>
                            <SelectTrigger data-testid="select-customer-type">
                              <SelectValue />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value="regular">ThÆ°á»ng</SelectItem>
                            <SelectItem value="premium">Premium</SelectItem>
                            <SelectItem value="platinum">VÃ ng</SelectItem>
                            <SelectItem value="vip">Kim cÆ°Æ¡ng</SelectItem>
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
                      Há»§y
                    </Button>
                    <Button
                      type="submit"
                      disabled={addCustomerMutation.isPending || editCustomerMutation.isPending}
                      data-testid="button-save-customer"
                    >
                      {addCustomerMutation.isPending || editCustomerMutation.isPending 
                        ? "Äang lÆ°u..." 
                        : (editingCustomer ? "Cáº­p nháº­t" : "ThÃªm")
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
                  KhÃ´ng tÃ¬m tháº¥y khÃ¡ch hÃ ng
                </h3>
                <p className="text-gray-500">
                  {searchTerm || selectedTier !== "all" 
                    ? "Thá»­ thay Ä‘á»•i bá»™ lá»c tÃ¬m kiáº¿m"
                    : "Báº¯t Ä‘áº§u báº±ng cÃ¡ch thÃªm khÃ¡ch hÃ ng Ä‘áº§u tiÃªn"
                  }
                </p>
              </CardContent>
            </Card>
          ) : (
            filteredCustomers.map((customer) => {
              // Handle both mapped and raw customer data
              const customerId = customer.customerId || customer.id;
              const customerName = customer.hoTen || customer.name;
              const customerPhone = customer.soDienThoai || customer.phone;
              const customerEmail = customer.email;
              const customerAddress = customer.diaChi || customer.address;
              const loyaltyPoints = customer.loyaltyPoints || 0;
              const totalSpent = customer.totalSpent || "0";
              const tierField = customer.hangKhachHang;
              
              const tierBadge = getTierBadge(tierField);
              const customerOrdersForCard = getCustomerOrdersForCard(customerId?.toString() || "0");
              const lastOrderDate = customerOrdersForCard.length > 0 
                ? new Date(customerOrdersForCard[0].createdAt).toLocaleDateString('vi-VN')
                : "ChÆ°a cÃ³ Ä‘Æ¡n hÃ ng";

              return (
                <Card 
                  key={customerId} 
                  className="cursor-pointer hover:shadow-md transition-shadow"
                    onClick={() => {
                      const rawCustomer = showInactive 
                        ? customer 
                        : rawCustomers.find(rc => rc.customerId.toString() === customerId.toString());
                      setSelectedCustomer(rawCustomer || null);
                    }}
                  data-testid={`customer-card-${customerId}`}
                >
                  <CardContent className="p-6">
                    <div className="flex items-start justify-between mb-4">
                      <div>
                        <h3 className="font-semibold text-lg mb-1" data-testid={`customer-name-${customerId}`}>
                          {customerName}
                        </h3>
                        <Badge className={`text-white ${tierBadge.color}`}>
                          {tierBadge.label}
                        </Badge>
                      </div>
                      <div className="flex space-x-1">
                        {!showInactive && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleEditCustomer(customer);
                            }}
                            data-testid={`button-edit-${customerId}`}
                          >
                            <Edit className="w-4 h-4" />
                          </Button>
                        )}
                        {showInactive ? (
                          <Button
                            variant="ghost"
                            size="sm"
                            className="text-green-600 hover:text-green-700"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleRestoreCustomer(customerId);
                            }}
                            data-testid={`button-restore-${customerId}`}
                          >
                            <RotateCcw className="w-4 h-4" />
                          </Button>
                        ) : (
                          <Button
                            variant="ghost"
                            size="sm"
                            className="text-red-600 hover:text-red-700"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleDeleteCustomer(customerId);
                            }}
                            data-testid={`button-delete-${customerId}`}
                          >
                            <Trash2 className="w-4 h-4" />
                          </Button>
                        )}
                      </div>
                    </div>

                    <div className="space-y-2 text-sm text-gray-600">
                      <div className="flex items-center">
                        <Phone className="w-4 h-4 mr-2" />
                        <span data-testid={`customer-phone-${customerId}`}>{customerPhone}</span>
                      </div>
                      {customerEmail && (
                        <div className="flex items-center">
                          <Mail className="w-4 h-4 mr-2" />
                          <span data-testid={`customer-email-${customerId}`}>{customerEmail}</span>
                        </div>
                      )}
                      {customerAddress && (
                        <div className="flex items-center">
                          <MapPin className="w-4 h-4 mr-2" />
                          <span className="line-clamp-1" data-testid={`customer-address-${customerId}`}>
                            {customerAddress}
                          </span>
                        </div>
                      )}
                    </div>

                    <div className="mt-4 pt-4 border-t border-gray-100">
                      <div className="grid grid-cols-2 gap-4 text-center">
                        <div>
                          <p className="text-sm text-gray-500">Äiá»ƒm tÃ­ch lÅ©y</p>
                          <p className="font-semibold text-primary" data-testid={`customer-points-${customerId}`}>
                            {loyaltyPoints}
                          </p>
                        </div>
                        <div>
                          <p className="text-sm text-gray-500">Tá»•ng chi tiÃªu</p>
                          <p className="font-semibold text-green-600" data-testid={`customer-spent-${customerId}`}>
                            {parseInt(totalSpent.toString()).toLocaleString('vi-VN')}â‚«
                          </p>
                        </div>
                      </div>
                      <div className="mt-2 text-center">
                        <p className="text-xs text-gray-500">
                          ÄÆ¡n gáº§n nháº¥t: {lastOrderDate}
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
  {/* Modal render debug removed */}
        {selectedCustomer && (
          <Dialog open={!!selectedCustomer} onOpenChange={() => setSelectedCustomer(null)}>
            <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
              <DialogHeader>
                <DialogTitle>Chi tiáº¿t khÃ¡ch hÃ ng</DialogTitle>
              </DialogHeader>

              {/* Modal content render debug removed */}
              <Tabs defaultValue="info" className="space-y-4">
                <TabsList className="grid w-full grid-cols-3">
                  <TabsTrigger value="info" data-testid="tab-customer-info">ThÃ´ng tin</TabsTrigger>
                  <TabsTrigger value="orders" data-testid="tab-customer-orders">ÄÆ¡n hÃ ng</TabsTrigger>
                  <TabsTrigger value="loyalty" data-testid="tab-customer-loyalty">Äiá»ƒm thÆ°á»Ÿng</TabsTrigger>
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
                  {/* Orders tab render debug removed */}
                  {isLoadingDetail ? (
                    <div className="text-center py-8">
                      <p>Äang táº£i Ä‘Æ¡n hÃ ng...</p>
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
                                <p className="font-medium">ÄÆ¡n hÃ ng #{order.orderNumber || order.orderId}</p>
                                <p className="text-sm text-gray-500">
                                  {new Date(order.createdAt).toLocaleString('vi-VN')}
                                </p>
                                <p className="text-sm text-gray-600 mt-1">
                                  {order.items?.length || 0} sáº£n pháº©m
                                </p>
                              </div>
                              <div className="text-right">
                                <p className="font-bold text-primary">
                                  {parseFloat(order.totalAmount || "0").toLocaleString('vi-VN')}â‚«
                                </p>
                                <Badge variant={order.status === 'completed' ? 'default' : 'secondary'}>
                                  {order.status === 'completed' ? 'HoÃ n thÃ nh' : 
                                   order.status === 'pending' ? 'Chá» xá»­ lÃ½' : 
                                   order.status === 'processing' ? 'Äang xá»­ lÃ½' : 'Äang xá»­ lÃ½'}
                                </Badge>
                              </div>
                            </div>
                          </CardContent>
                        </Card>
                      ))}
                      {getCustomerOrders().length === 0 && (
                        <div className="text-center py-8 text-gray-500">
                          <ShoppingBag className="w-12 h-12 mx-auto mb-2 opacity-50" />
                          <p>KhÃ¡ch hÃ ng chÆ°a cÃ³ Ä‘Æ¡n hÃ ng nÃ o</p>
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
                        ChÆ°Æ¡ng trÃ¬nh Ä‘iá»ƒm thÆ°á»Ÿng
                      </CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="grid grid-cols-2 gap-6">
                        <div className="text-center">
                          <p className="text-2xl font-bold text-primary">
                            {customerDetail?.loyaltyPoints || selectedCustomer.loyaltyPoints}
                          </p>
                          <p className="text-sm text-gray-500">Äiá»ƒm hiá»‡n táº¡i</p>
                        </div>
                        <div className="text-center">
                          <p className="text-2xl font-bold text-green-600">
                            {parseInt(customerDetail?.totalSpent || selectedCustomer.totalSpent.toString() || "0").toLocaleString('vi-VN')}â‚«
                          </p>
                          <p className="text-sm text-gray-500">Tá»•ng chi tiÃªu</p>
                        </div>
                      </div>
                      
                      {/* Loyalty Transactions */}
                      <div className="mt-6">
                        <div className="flex justify-between items-center mb-3">
                          <h4 className="font-medium">Lá»‹ch sá»­ giao dá»‹ch Ä‘iá»ƒm</h4>
                          <button 
                            onClick={() => refetchTransactions()}
                            disabled={isLoadingLoyalty}
                            className="text-sm text-blue-600 hover:text-blue-800 disabled:text-gray-400"
                          >
                            {isLoadingLoyalty ? 'Äang táº£i...' : 'ðŸ”„ LÃ m má»›i'}
                          </button>
                        </div>
                        
                        {isLoadingLoyalty ? (
                          <div className="text-center text-gray-500">Äang táº£i...</div>
                        ) : loyaltyTransactions && loyaltyTransactions.length > 0 ? (
                          <div className="space-y-2 max-h-40 overflow-y-auto">
                            {loyaltyTransactions.slice(0, 10).map((transaction: any, index: number) => (
                              <div key={transaction.transactionId || index} className="flex justify-between items-center text-sm py-2 border-b">
                                <div>
                                  <p className="font-medium">{transaction.reason || 'Giao dá»‹ch Ä‘iá»ƒm'}</p>
                                  <p className="text-gray-500">
                                    {new Date(transaction.processedAt).toLocaleDateString('vi-VN')} {new Date(transaction.processedAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}
                                  </p>
                                </div>
                                <div className={`font-bold ${transaction.points > 0 ? 'text-green-600' : 'text-red-600'}`}>
                                  {transaction.points > 0 ? '+' : ''}{transaction.points}
                                </div>
                              </div>
                            ))}
                            {loyaltyTransactions.length > 10 && (
                              <div className="text-center text-gray-500 text-xs pt-2">
                                Hiá»ƒn thá»‹ 10 giao dá»‹ch gáº§n nháº¥t tá»« {loyaltyTransactions.length} giao dá»‹ch
                              </div>
                            )}
                          </div>
                        ) : (
                          <div className="text-center text-gray-500">ChÆ°a cÃ³ giao dá»‹ch nÃ o</div>
                        )}
                      </div>

                      <div className="mt-6">
                        <p className="text-sm text-gray-600 mb-2">
                          Quyá»n lá»£i háº¡ng {getTierBadge(customerDetail?.hangKhachHang || selectedCustomer.hangKhachHang).label}:
                        </p>
                        <ul className="text-sm space-y-1">
                          {(customerDetail?.hangKhachHang || selectedCustomer.hangKhachHang) === 'VIP' && (
                            <>
                              <li>â€¢ Giáº£m giÃ¡ 15% cho táº¥t cáº£ sáº£n pháº©m</li>
                              <li>â€¢ TÃ­ch Ä‘iá»ƒm x3</li>
                              <li>â€¢ Æ¯u tiÃªn há»— trá»£ khÃ¡ch hÃ ng</li>
                            </>
                          )}
                          {(customerDetail?.hangKhachHang || selectedCustomer.hangKhachHang) === 'Premium' && (
                            <>
                              <li>â€¢ Giáº£m giÃ¡ 10% cho táº¥t cáº£ sáº£n pháº©m</li>
                              <li>â€¢ TÃ­ch Ä‘iá»ƒm x2</li>
                              <li>â€¢ Miá»…n phÃ­ giao hÃ ng</li>
                            </>
                          )}
                          {(customerDetail?.hangKhachHang || selectedCustomer.hangKhachHang) === 'Thuong' && (
                            <>
                              <li>â€¢ TÃ­ch Ä‘iá»ƒm tiÃªu chuáº©n</li>
                              <li>â€¢ Æ¯u Ä‘Ã£i Ä‘áº·c biá»‡t theo mÃ¹a</li>
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
                <DialogTitle>Chi tiáº¿t Ä‘Æ¡n hÃ ng #{selectedOrder.orderNumber || selectedOrder.orderId}</DialogTitle>
              </DialogHeader>

              <div className="space-y-6">
                {/* Order Info */}
                <Card>
                  <CardHeader>
                    <CardTitle className="text-lg">ThÃ´ng tin Ä‘Æ¡n hÃ ng</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <p className="text-sm text-gray-500">NgÃ y táº¡o</p>
                        <p className="font-medium">
                          {new Date(selectedOrder.createdAt).toLocaleString('vi-VN')}
                        </p>
                      </div>
                      <div>
                        <p className="text-sm text-gray-500">Tráº¡ng thÃ¡i</p>
                        <Badge variant={selectedOrder.status === 'completed' ? 'default' : 'secondary'}>
                          {selectedOrder.status === 'completed' ? 'HoÃ n thÃ nh' : 
                           selectedOrder.status === 'pending' ? 'Chá» xá»­ lÃ½' : 
                           selectedOrder.status === 'processing' ? 'Äang xá»­ lÃ½' : 'Äang xá»­ lÃ½'}
                        </Badge>
                      </div>
                      <div>
                        <p className="text-sm text-gray-500">Tá»•ng tiá»n</p>
                        <p className="font-bold text-primary text-lg">
                          {parseFloat(selectedOrder.totalAmount || "0").toLocaleString('vi-VN')}â‚«
                        </p>
                      </div>
                      <div>
                        <p className="text-sm text-gray-500">PhÆ°Æ¡ng thá»©c thanh toÃ¡n</p>
                        <p className="font-medium">
                          {selectedOrder.paymentMethod === 'cash' ? 'Tiá»n máº·t' : 
                           selectedOrder.paymentMethod === 'card' ? 'Tháº»' : 'KhÃ¡c'}
                        </p>
                      </div>
                    </div>
                  </CardContent>
                </Card>

                {/* Order Items */}
                <Card>
                  <CardHeader>
                    <CardTitle className="text-lg">Sáº£n pháº©m trong Ä‘Æ¡n hÃ ng</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {selectedOrder.items?.map((item, index) => (
                        <div key={index} className="flex justify-between items-center py-3 border-b">
                          <div className="flex-1">
                            <p className="font-medium">{item.productName || item.product?.name || 'Sáº£n pháº©m'}</p>
                            <p className="text-sm text-gray-500">
                              {parseFloat(item.price || "0").toLocaleString('vi-VN')}â‚« Ã— {item.quantity}
                            </p>
                            {item.product?.sku && (
                              <p className="text-xs text-gray-400">SKU: {item.product.sku}</p>
                            )}
                          </div>
                          <div className="text-right">
                            <p className="font-bold">
                              {parseFloat(item.totalPrice || (parseFloat(item.price || "0") * item.quantity).toString()).toLocaleString('vi-VN')}â‚«
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                    
                    <div className="mt-4 pt-4 border-t">
                      <div className="flex justify-between items-center text-lg font-bold">
                        <span>Tá»•ng cá»™ng:</span>
                        <span className="text-primary">
                          {parseFloat(selectedOrder.totalAmount || "0").toLocaleString('vi-VN')}â‚«
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

