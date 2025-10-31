import { useQuery } from "@tanstack/react-query";
import { useState, useEffect, useMemo } from "react";
import { useAuth } from "@/contexts/auth-context";
type StoreInfo = {
  name: string;
  address?: string;
  taxCode?: string;
  phone?: string;
  email?: string;
};
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useLocation } from "wouter";
import { Card, CardContent } from "@/components/ui/card";
import { Trash2, RotateCcw, AlertTriangle, X, Printer, Search, Filter } from "lucide-react";
import { useToast } from "@/hooks/use-toast";
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

import { apiRequest } from "../lib/utils";

// Global CSS for thermal printing
const printStyle = `
  @media print {
    * {
      color: #000 !important;
      background-color: transparent !important;
    }
    p, span, div, label, input, button, a, h1, h2, h3, h4, h5, h6 {
      color: #000 !important;
    }
    .text-gray-500, .text-gray-600, .text-gray-700, .text-gray-800, 
    .text-green-600, .text-green-700, .text-green-800,
    [class*="text-gray"], [class*="text-green"] {
      color: #000 !important;
    }
  }
`;

type OrderItem = {
  productName: string;
  quantity: number;
  price: number;
  totalPrice: number;
};

type Order = {
  orderId: number;
  customerName?: string;
  createdAt: string;
  totalAmount: number;
  items: OrderItem[];
  taxAmount?: number;
  paymentMethod?: string;
  paymentStatus?: string;
  status?: string;
  cashierName?: string;
  subtotal?: number;
  discountAmount?: number;
  cancellationReason?: string;
  storeId?: string;
  storeName?: string;
};


export default function OrdersPage() {
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  
  // Check if user can delete orders (only admin should have this permission)
  const canDeleteOrders = hasPermission("DeleteOrders");
  
  // State for cancellation reason
  const [cancellationReason, setCancellationReason] = useState("");
  
  // Filter and search states
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedStoreId, setSelectedStoreId] = useState<string>("all");
  const [selectedPaymentStatus, setSelectedPaymentStatus] = useState<string>("all");
  const [selectedOrderStatus, setSelectedOrderStatus] = useState<string>("all");
  
  const { data: orders = [], isLoading, refetch } = useQuery<Order[]>({
    queryKey: ["/api/orders"],
    queryFn: async () => {
      const res = await apiRequest("/api/orders", { method: "GET" });
      // Nếu trả về string, parse lại
      const raw = typeof res === "string" ? JSON.parse(res) : res;
      // Sửa lại items nếu là chuỗi JSON
      const mapped = raw.map((order: any) => ({
        ...order,
        items: typeof order.items === "string" ? JSON.parse(order.items) : order.items
      }));
      console.log("orders:", mapped);
      return mapped;
    },
    refetchInterval: 5000, // Auto-refresh every 5 seconds for real-time updates
  });

  // Lấy thông tin cửa hàng
  const { data: storeInfo } = useQuery<StoreInfo | null>({
    queryKey: ["/api/StoreInfo"],
    queryFn: async () => {
      const res = await apiRequest("/api/StoreInfo", { method: "GET" });
      if (res.status === 404) return null;
      return typeof res === "string" ? JSON.parse(res) : res;
    },
  });

  // Fetch QR settings
  const { data: qrSettings } = useQuery({
    queryKey: ["/api/QRSettings"],
    queryFn: async () => {
      const res = await apiRequest("/api/QRSettings", { method: "GET" });
      return res;
    },
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  // Fetch print configuration
  const { data: printConfig } = useQuery({
    queryKey: ["/api/PrintConfig"],
    queryFn: async () => {
      const res = await apiRequest("/api/PrintConfig", { method: "GET" });
      return res;
    },
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  // Fetch available stores for filter
  const { data: stores = [] } = useQuery({
    queryKey: ["/api/storeswitch/my-stores"],
    queryFn: async () => {
      try {
        const res = await apiRequest("/api/storeswitch/my-stores", { 
          method: "GET",
          headers: {
            "Username": "admin" // Tạm thời hardcode, sau này sẽ lấy từ auth context
          }
        });
        return typeof res === "string" ? JSON.parse(res) : res;
      } catch (error) {
        console.error("Error fetching stores:", error);
        return [];
      }
    },
    staleTime: 10 * 60 * 1000, // Cache for 10 minutes
  });

  // Generate QR URL based on settings
  const generateQRUrl = (amount: number, orderId?: number) => {
    if (!qrSettings?.isEnabled || !qrSettings?.bankCode || !qrSettings?.bankAccountNumber) {
      return null;
    }
    
    const template = qrSettings.qrTemplate || "compact";
    const accountName = encodeURIComponent(qrSettings.bankAccountHolder || "");
    
    // Sử dụng orderId nếu có, để tạo mô tả "thanh toan chuyen khoan don hang [mã]"
    let url = `https://api.vietqr.io/image/${qrSettings.bankCode}-${qrSettings.bankAccountNumber}-${template}.jpg?accountName=${accountName}&amount=${amount}`;
    
    if (orderId) {
      const description = encodeURIComponent(`thanh toan don hang theo hoa don ${orderId}`);
      url += `&addInfo=${description}`;
    } else {
      const description = encodeURIComponent(qrSettings.defaultDescription || "Thanh toan hoa don");
      url += `&addInfo=${description}`;
    }
    
    return url;
  };

  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);

  // Filter orders based on search query and selected filters
  const filteredOrders = useMemo(() => {
    if (!orders) return [];
    
    return orders.filter(order => {
      // Search query filter
      if (searchQuery.trim()) {
        const query = searchQuery.toLowerCase();
        const matchesOrderId = order.orderId.toString().includes(query);
        const matchesCustomerName = order.customerName?.toLowerCase().includes(query) || false;
        const matchesStoreName = order.storeName?.toLowerCase().includes(query) || false;
        
        if (!matchesOrderId && !matchesCustomerName && !matchesStoreName) {
          return false;
        }
      }
      
      // Store filter
      if (selectedStoreId !== "all" && order.storeId !== selectedStoreId) {
        return false;
      }
      
      // Payment status filter
      if (selectedPaymentStatus !== "all" && order.paymentStatus !== selectedPaymentStatus) {
        return false;
      }
      
      // Order status filter
      if (selectedOrderStatus !== "all" && order.status !== selectedOrderStatus) {
        return false;
      }
      
      return true;
    });
  }, [orders, searchQuery, selectedStoreId, selectedPaymentStatus, selectedOrderStatus]);

  // Helper functions để format trạng thái
  const formatPaymentStatus = (status?: string) => {
    switch (status) {
      case 'paid': return 'Đã thanh toán';
      case 'pending': return 'Chờ thanh toán';
      case 'failed': return 'Thanh toán thất bại';
      default: return 'Đã thanh toán';
    }
  };

  const formatOrderStatus = (status?: string) => {
    switch (status) {
      case 'completed': return 'Hoàn thành';
      case 'pending': return 'Đang xử lý';
      case 'cancelled': return 'Đã hủy';
      default: return 'Hoàn thành';
    }
  };

  const getPaymentStatusBadge = (status?: string) => {
    switch (status) {
      case 'paid':
        return <Badge className="bg-green-100 text-green-800 border-green-200 print:bg-white print:text-black print:border-black">{formatPaymentStatus(status)}</Badge>;
      case 'pending':
        return <Badge className="bg-yellow-100 text-yellow-800 border-yellow-200 print:bg-white print:text-black print:border-black">{formatPaymentStatus(status)}</Badge>;
      case 'failed':
        return <Badge className="bg-red-100 text-red-800 border-red-200 print:bg-white print:text-black print:border-black">{formatPaymentStatus(status)}</Badge>;
      default:
        return <Badge className="bg-green-100 text-green-800 border-green-200 print:bg-white print:text-black print:border-black">{formatPaymentStatus(status)}</Badge>;
    }
  };

  const getOrderStatusBadge = (status?: string) => {
    switch (status) {
      case 'completed':
        return <Badge className="bg-blue-100 text-blue-800 border-blue-200">{formatOrderStatus(status)}</Badge>;
      case 'pending':
        return <Badge className="bg-orange-100 text-orange-800 border-orange-200">{formatOrderStatus(status)}</Badge>;
      case 'cancelled':
        return <Badge className="bg-gray-100 text-gray-800 border-gray-200">{formatOrderStatus(status)}</Badge>;
      default:
        return <Badge className="bg-blue-100 text-blue-800 border-blue-200">{formatOrderStatus(status)}</Badge>;
    }
  };

  const getCardBorderClass = (paymentStatus?: string, orderStatus?: string) => {
    // Ưu tiên hiển thị trạng thái thanh toán
    if (paymentStatus === 'pending') {
      return 'border-l-4 border-l-yellow-400 bg-yellow-50';
    }
    if (paymentStatus === 'failed') {
      return 'border-l-4 border-l-red-400 bg-red-50';
    }
    if (orderStatus === 'pending') {
      return 'border-l-4 border-l-orange-400 bg-orange-50';
    }
    if (orderStatus === 'cancelled') {
      return 'border-l-4 border-l-gray-400 bg-gray-50';
    }
    if (paymentStatus === 'paid' && orderStatus === 'completed') {
      return 'border-l-4 border-l-green-400 bg-green-50 print:border-l-black print:bg-white';
    }
    return '';
  };

  const [, navigate] = useLocation();

  // Xóa đơn hàng
  const handleDeleteOrder = async (orderId: number) => {
    try {
      await apiRequest(`/api/orders/${orderId}`, { method: "DELETE" });
      toast({
        title: "Thành công",
        description: "Đã xóa đơn hàng thành công"
      });
      refetch(); // Refresh danh sách
    } catch (error: any) {
      console.log("Delete error:", error);
      
      // Hiển thị lỗi chi tiết cho user
      toast({
        variant: "destructive",
        title: "Tạm thời không thể xóa đơn hàng",
        description: "Hệ thống đang có vấn đề với việc xóa đơn hàng. Vui lòng liên hệ admin để xóa thủ công hoặc đánh dấu đơn hàng là 'Đã hủy'."
      });
    }
  };

  // Đánh dấu hủy đơn hàng
  const handleCancelOrder = async (orderId: number) => {
    try {
      await apiRequest(`/api/orders/${orderId}`, { 
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ 
          Status: "cancelled",
          CancellationReason: cancellationReason || "Không có lý do"
        })
      });
      toast({
        title: "Thành công",
        description: "Đã đánh dấu đơn hàng là đã hủy"
      });
      
      // Reset cancellation reason after successful cancellation
      setCancellationReason("");
      
      refetch(); // Refresh danh sách
    } catch (error: any) {
      toast({
        variant: "destructive",
        title: "Lỗi",
        description: "Không thể cập nhật trạng thái đơn hàng"
      });
    }
  };

  // Mở lại đơn hàng để tiếp tục thanh toán
  const handleReopenOrder = (order: Order) => {
    // Lưu thông tin đơn hàng vào localStorage để sales page có thể đọc
    const orderData = {
      orderId: order.orderId,
      items: order.items,
      customerName: order.customerName,
      totalAmount: order.totalAmount,
      paymentMethod: order.paymentMethod
    };
    localStorage.setItem('reopenOrder', JSON.stringify(orderData));
    
    toast({
      title: "Đã mở lại đơn hàng",
      description: `Đơn hàng #${order.orderId} đã được chuyển đến trang bán hàng để tiếp tục thanh toán`
    });
    
    // Chuyển đến trang sales
    navigate('/sales');
  };

  // Tính toán thống kê trạng thái (dựa trên kết quả filter)
  const stats = useMemo(() => {
    const total = filteredOrders.length;
    const paid = filteredOrders.filter(o => o.paymentStatus === 'paid').length;
    const pending = filteredOrders.filter(o => o.paymentStatus === 'pending').length;
    const failed = filteredOrders.filter(o => o.paymentStatus === 'failed').length;
    const completed = filteredOrders.filter(o => o.status === 'completed').length;
    const processing = filteredOrders.filter(o => o.status === 'pending').length;
    const cancelled = filteredOrders.filter(o => o.status === 'cancelled').length;
    
    return { total, paid, pending, failed, completed, processing, cancelled };
  }, [filteredOrders]);

  return (
    <>
      <style dangerouslySetInnerHTML={{ __html: printStyle }} />
      <div className="p-6">
        <h1 className="text-2xl font-bold mb-4">Danh sách đơn hàng</h1>
      
      {/* Filter và Search */}
      <Card className="mb-6">
        <CardContent className="p-4">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            {/* Search box */}
            <div className="relative">
              <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
              <Input
                placeholder="Tìm theo mã đơn, khách hàng, cửa hàng..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9"
              />
            </div>
            
            {/* Store filter */}
            <Select value={selectedStoreId} onValueChange={setSelectedStoreId}>
              <SelectTrigger>
                <SelectValue placeholder="Tất cả cửa hàng" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả cửa hàng</SelectItem>
                {stores.map((store: any) => (
                  <SelectItem key={store.storeId} value={store.storeId.toString()}>
                    {store.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            
            {/* Payment status filter */}
            <Select value={selectedPaymentStatus} onValueChange={setSelectedPaymentStatus}>
              <SelectTrigger>
                <SelectValue placeholder="Trạng thái thanh toán" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả</SelectItem>
                <SelectItem value="paid">Đã thanh toán</SelectItem>
                <SelectItem value="pending">Chờ thanh toán</SelectItem>
                <SelectItem value="failed">Thanh toán thất bại</SelectItem>
              </SelectContent>
            </Select>
            
            {/* Order status filter */}
            <Select value={selectedOrderStatus} onValueChange={setSelectedOrderStatus}>
              <SelectTrigger>
                <SelectValue placeholder="Trạng thái đơn hàng" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả</SelectItem>
                <SelectItem value="completed">Hoàn thành</SelectItem>
                <SelectItem value="pending">Đang xử lý</SelectItem>
                <SelectItem value="cancelled">Đã hủy</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>
      
      {/* Thống kê trạng thái */}
      <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-7 gap-4 mb-6">
        <Card className="text-center">
          <CardContent className="p-3">
            <div className="text-2xl font-bold text-blue-600">{stats.total}</div>
            <div className="text-sm text-gray-600">Tổng đơn</div>
          </CardContent>
        </Card>
        <Card className="text-center">
          <CardContent className="p-3">
            <div className="text-2xl font-bold text-green-600 print:text-black">{stats.paid}</div>
            <div className="text-sm text-gray-600">Đã thanh toán</div>
          </CardContent>
        </Card>
        <Card className="text-center">
          <CardContent className="p-3">
            <div className="text-2xl font-bold text-yellow-600">{stats.pending}</div>
            <div className="text-sm text-gray-600">Chờ thanh toán</div>
          </CardContent>
        </Card>
        <Card className="text-center">
          <CardContent className="p-3">
            <div className="text-2xl font-bold text-red-600">{stats.failed}</div>
            <div className="text-sm text-gray-600">Thanh toán thất bại</div>
          </CardContent>
        </Card>
        <Card className="text-center">
          <CardContent className="p-3">
            <div className="text-2xl font-bold text-blue-600">{stats.completed}</div>
            <div className="text-sm text-gray-600">Hoàn thành</div>
          </CardContent>
        </Card>
        <Card className="text-center">
          <CardContent className="p-3">
            <div className="text-2xl font-bold text-orange-600">{stats.processing}</div>
            <div className="text-sm text-gray-600">Đang xử lý</div>
          </CardContent>
        </Card>
        <Card className="text-center">
          <CardContent className="p-3">
            <div className="text-2xl font-bold text-gray-600">{stats.cancelled}</div>
            <div className="text-sm text-gray-600">Đã hủy</div>
          </CardContent>
        </Card>
      </div>
      
      <div className="mb-4">
        <Button
          style={{ backgroundColor: '#ef4444', color: '#fff' }}
          onClick={() => navigate("/sales")}
        >
          Đóng
        </Button>
      </div>
      {isLoading ? (
        <div>Đang tải...</div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {filteredOrders.map((order) => (
            <Card key={order.orderId} className={getCardBorderClass(order.paymentStatus, order.status)}>
              <CardContent className="p-4">
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <div className="font-semibold text-lg mb-2">Mã đơn: #{order.orderId}</div>
                    <div className="space-y-1 text-sm">
                      <div>Khách hàng: <span className="font-medium">{order.customerName || "Khách lẻ"}</span></div>
                      <div>Cửa hàng: <span className="font-medium text-purple-600">{order.storeName || "Cửa hàng chính"}</span></div>
                      <div>Ngày tạo: <span className="font-medium">{new Date(order.createdAt).toLocaleDateString('vi-VN')}</span></div>
                      <div>Giờ tạo: <span className="font-medium">{new Date(order.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</span></div>
                      <div className="flex items-center gap-2">
                        <span>Tổng tiền: <span className="font-bold text-blue-600">{Number(order.totalAmount).toLocaleString('vi-VN')}₫</span></span>
                        {order.discountAmount && order.discountAmount > 0 && (
                          <Badge className="bg-red-100 text-red-800 border-red-200 text-xs">
                            Giảm {Number(order.discountAmount).toLocaleString('vi-VN')}₫
                          </Badge>
                        )}
                      </div>
                    </div>
                    
                    {/* Trạng thái */}
                    <div className="flex flex-wrap gap-2 mt-3">
                      {getPaymentStatusBadge(order.paymentStatus)}
                      {getOrderStatusBadge(order.status)}
                    </div>
                    
                    {/* Hiển thị lý do hủy nếu đơn hàng đã bị hủy */}
                    {order.status === 'cancelled' && order.cancellationReason && (
                      <div className="mt-3 p-3 bg-red-50 border border-red-200 rounded-md">
                        <div className="text-sm">
                          <span className="font-medium text-red-700">Lý do hủy:</span>
                          <p className="text-red-600 mt-1">{order.cancellationReason}</p>
                        </div>
                      </div>
                    )}
                    
                    {/* Nút hành động */}
                    <div className="flex gap-2 mt-3">
                      {order.paymentStatus === 'pending' && (
                        <button
                          onClick={() => handleReopenOrder(order)}
                          className="inline-flex items-center px-3 py-1.5 bg-blue-600 text-white text-sm rounded-md hover:bg-blue-700 transition-colors"
                        >
                          <RotateCcw className="w-4 h-4 mr-1" />
                          Mở lại
                        </button>
                      )}
                      
                      {order.status !== 'cancelled' && (
                        <AlertDialog>
                          <AlertDialogTrigger asChild>
                            <button className="inline-flex items-center px-3 py-1.5 bg-orange-600 text-white text-sm rounded-md hover:bg-orange-700 transition-colors">
                              <X className="w-4 h-4 mr-1" />
                              Đánh dấu hủy
                            </button>
                          </AlertDialogTrigger>
                          <AlertDialogContent>
                            <AlertDialogHeader>
                              <AlertDialogTitle>Xác nhận hủy đơn hàng</AlertDialogTitle>
                              <AlertDialogDescription>
                                Bạn có chắc chắn muốn đánh dấu đơn hàng #{order.orderId} là đã hủy? Đơn hàng sẽ được giữ lại trong hệ thống với trạng thái "Đã hủy".
                              </AlertDialogDescription>
                            </AlertDialogHeader>
                            
                            {/* Cancellation Reason Input */}
                            <div className="py-4">
                              <label htmlFor="cancellation-reason" className="block text-sm font-medium text-gray-700 mb-2">
                                Lý do hủy đơn hàng <span className="text-red-500">*</span>
                              </label>
                              <textarea
                                id="cancellation-reason"
                                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                                rows={3}
                                value={cancellationReason}
                                onChange={(e) => setCancellationReason(e.target.value)}
                                placeholder="Nhập lý do hủy đơn hàng (bắt buộc)"
                                maxLength={500}
                              />
                              <div className="text-xs text-gray-500 mt-1">
                                {cancellationReason.length}/500 ký tự
                              </div>
                            </div>
                            
                            <AlertDialogFooter>
                              <AlertDialogCancel onClick={() => setCancellationReason("")}>
                                Không
                              </AlertDialogCancel>
                              <AlertDialogAction 
                                onClick={() => handleCancelOrder(order.orderId)}
                                className="bg-orange-600 hover:bg-orange-700"
                                disabled={!cancellationReason.trim()}
                              >
                                Đánh dấu hủy
                              </AlertDialogAction>
                            </AlertDialogFooter>
                          </AlertDialogContent>
                        </AlertDialog>
                      )}
                      
                      {canDeleteOrders && (
                        <AlertDialog>
                          <AlertDialogTrigger asChild>
                            <button className="inline-flex items-center px-3 py-1.5 bg-red-600 text-white text-sm rounded-md hover:bg-red-700 transition-colors">
                              <Trash2 className="w-4 h-4 mr-1" />
                              Xóa
                            </button>
                          </AlertDialogTrigger>
                          <AlertDialogContent>
                            <AlertDialogHeader>
                              <AlertDialogTitle>Xác nhận xóa đơn hàng</AlertDialogTitle>
                              <AlertDialogDescription>
                                Bạn có chắc chắn muốn xóa đơn hàng #{order.orderId}? Hành động này không thể hoàn tác.
                                <br /><br />
                                <strong>Lưu ý:</strong> Hiện tại có vấn đề kỹ thuật với việc xóa đơn hàng. Khuyến nghị sử dụng "Đánh dấu hủy" thay thế.
                              </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                              <AlertDialogCancel>Hủy</AlertDialogCancel>
                              <AlertDialogAction 
                                onClick={() => handleDeleteOrder(order.orderId)}
                                className="bg-red-600 hover:bg-red-700"
                              >
                                Thử xóa
                              </AlertDialogAction>
                            </AlertDialogFooter>
                          </AlertDialogContent>
                        </AlertDialog>
                      )}
                    </div>
                  </div>
                  
                  <div className="ml-4">
                    <Button onClick={() => setSelectedOrder(order)} size="sm" className="mb-2">
                      Xem & In
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Modal xem & in đơn hàng */}
      {selectedOrder && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4 print:bg-white print:p-0">
          <div
            className="bg-white rounded-lg shadow-lg p-6 w-full max-w-lg relative print:w-full print:max-w-full max-h-[90vh] overflow-y-auto print:overflow-visible print:max-h-none print:shadow-none print:rounded-none print:relative print:block print:no-break"
            style={{ 
              width: '80mm', 
              maxWidth: '80mm', 
              minWidth: '80mm', 
              fontSize: '14px'
            }}
          >
            <button
              className="absolute top-2 right-2 text-gray-500 hover:text-black"
              onClick={() => setSelectedOrder(null)}
            >
              Đóng
            </button>
            {/* Thông tin cửa hàng in đầu bill */}
            <div className="text-center border-b pb-2 mb-2">
              <div className="font-bold text-lg">{storeInfo?.name || "[Tên cửa hàng]"}</div>
              {storeInfo?.address && <div className="text-sm">Đ/c: {storeInfo.address}</div>}
              {storeInfo?.taxCode && <div className="text-sm">MST: {storeInfo.taxCode}</div>}
              {storeInfo?.phone && <div className="text-sm">ĐT: {storeInfo.phone}</div>}
              {storeInfo?.email && <div className="text-sm">Email: {storeInfo.email}</div>}
            </div>
            <h2 className="text-xl font-bold mb-2">Đơn hàng #{selectedOrder.orderId}</h2>
            <div>Khách hàng: {selectedOrder.customerName || "Khách lẻ"}</div>
            <div>Cửa hàng: <b className="text-purple-600">{selectedOrder.storeName || "Cửa hàng chính"}</b></div>
            <div>Ngày tạo: {new Date(selectedOrder.createdAt).toLocaleDateString('vi-VN')}</div>
            <div>Giờ tạo: {new Date(selectedOrder.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</div>
            
            {/* Trạng thái đơn hàng */}
            <div className="flex gap-2 my-2">
              {getPaymentStatusBadge(selectedOrder.paymentStatus)}
              {getOrderStatusBadge(selectedOrder.status)}
            </div>
            
            {/* Thông tin bổ sung */}
            <div>Hình thức thanh toán: <b>{selectedOrder.paymentMethod || "Tiền mặt"}</b></div>
            <div>Thu Ngân: <b>{selectedOrder.cashierName || "Admin"}</b></div>
            
            <div className="mt-4 print:no-break">
              <table className="w-full border print:no-break">
                <thead>
                  <tr>
                    <th className="border px-2 py-1">Sản phẩm</th>
                    <th className="border px-2 py-1">SL</th>
                    <th className="border px-2 py-1">Đơn giá</th>
                    <th className="border px-2 py-1">Thành tiền</th>
                  </tr>
                </thead>
                <tbody>
                  {selectedOrder.items?.map((item, idx) => (
                    <tr key={idx}>
                      <td className="border px-2 py-1">{item.productName}</td>
                      <td className="border px-2 py-1 text-center">{item.quantity}</td>
                      <td className="border px-2 py-1 text-right">{Number(item.price).toLocaleString('vi-VN')}₫</td>
                      <td className="border px-2 py-1 text-right">{Number(item.totalPrice).toLocaleString('vi-VN')}₫</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            {/* Tính lại tạm tính và VAT từ items */}
            <div className="mt-2 text-right">
              {(() => {
                const subtotal = selectedOrder.items?.reduce((sum, item) => sum + (Number(item.totalPrice) || 0), 0) || 0;
                const taxAmount = Number(selectedOrder.taxAmount) || 0;
                const discountAmount = Number(selectedOrder.discountAmount) || 0;
                return (
                  <>
                    <div>Tạm tính: <b>{subtotal.toLocaleString('vi-VN')}₫</b></div>
                    {taxAmount > 0 && (
                      <div>VAT 10%: <b>{taxAmount.toLocaleString('vi-VN')}₫</b></div>
                    )}
                    {discountAmount > 0 && (
                      <div className="text-red-600">Giảm giá: <b>-{discountAmount.toLocaleString('vi-VN')}₫</b></div>
                    )}
                  </>
                );
              })()}
            </div>
            </div>
            <div className="mt-4 text-right font-bold text-lg border-t pt-2 print:no-break">
              Tổng cộng: {Number(selectedOrder.totalAmount).toLocaleString('vi-VN')}₫
            </div>
            
            {/* QR Code cho thanh toán QR - Đặt sau tổng cộng */}
            {(selectedOrder.paymentMethod === 'qr' || selectedOrder.paymentMethod === 'QR Code' || selectedOrder.paymentMethod?.toLowerCase().includes('qr')) && (
              <div className="mt-4 text-center print:mt-2 print:border-0 print:p-0 print:bg-white border rounded-lg p-4 bg-gradient-to-br from-purple-50 to-blue-50 border-purple-200 print:no-break">
                <h4 className="font-semibold text-purple-800 mb-3 text-base print:text-black print:text-sm print:mb-1 print:font-bold">Mã QR Thanh toán</h4>
                
                {generateQRUrl(selectedOrder.totalAmount, selectedOrder.orderId) ? (
                  <>
                    <div className="flex justify-center mb-4 print:mb-1">
                      <div className="p-2 bg-white rounded-xl shadow-lg border-2 border-purple-200 w-full max-w-full print:p-0 print:shadow-none print:border-0 print:rounded-none print:bg-transparent">
                        <img 
                          src={generateQRUrl(selectedOrder.totalAmount, selectedOrder.orderId) || ""}
                          alt="QR Code thanh toán" 
                          className="w-full h-auto object-contain mx-auto print:w-full print:h-auto"
                          style={{ width: '100%', height: 'auto', minWidth: '200px', minHeight: '200px', maxWidth: '300px', display: 'block' }}
                          onError={(e) => {
                            e.currentTarget.src = "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjUwIiBoZWlnaHQ9IjI1MCIgdmlld0JveD0iMCAwIDI1MCAyNTAiIGZpbGw9Im5vbmUiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+CjxyZWN0IHdpZHRoPSIyNTAiIGhlaWdodD0iMjUwIiBmaWxsPSIjRjNGNEY2Ii8+Cjx0ZXh0IHg9IjEyNSIgeT0iMTI1IiBmb250LWZhbWlseT0iQXJpYWwiIGZvbnQtc2l6ZT0iMTQiIGZpbGw9IiM2QjczODAiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGR5PSIwLjNlbSI+UVIgRXJyb3I8L3RleHQ+Cjwvc3ZnPg==";
                          }}
                        />
                      </div>
                    </div>
                  </>
                ) : (
                  <div className="text-center text-orange-600 py-6">
                    <div className="w-full h-48 mx-auto bg-orange-100 rounded-xl flex items-center justify-center mb-3">
                      <span className="text-orange-500 text-sm font-medium">QR không khả dụng</span>
                    </div>
                    <p className="text-sm font-medium">QR Code chưa được cấu hình</p>
                    <p className="text-xs">Vui lòng vào Settings &gt; QR Code để cấu hình</p>
                  </div>
                )}
                
                <div className="mt-3 p-2 bg-purple-100 rounded-lg print:bg-transparent print:border print:border-black print:p-1 print:mt-1">
                  <p className="text-sm font-bold text-purple-800 print:text-black print:text-xs">
                    Số tiền: {Number(selectedOrder.totalAmount).toLocaleString('vi-VN')}₫
                  </p>
                </div>
              </div>
            )}
            <div className="mt-6 text-center font-semibold text-gray-700 print:text-black">
              Cảm ơn - Hẹn gặp lại
            </div>
            <div className="mt-4 flex flex-col sm:flex-row justify-end gap-2 print:hidden">
              {/* Print Buttons Group */}
              <div className="flex gap-2">
                {/* Main Print Button - Primary and prominent */}
                <Button 
                  onClick={() => window.print()} 
                  className="bg-green-600 hover:bg-green-700 text-white flex items-center justify-center print:hidden"
                  size="lg"
                >
                  <Printer className="w-4 h-4 mr-2" />
                  In đơn hàng
                </Button>
                
                {/* Print Multiple Copies Button */}
                {printConfig?.printCopies && printConfig.printCopies > 1 && (
                  <Button 
                    onClick={() => {
                      for (let i = 0; i < (printConfig.printCopies || 1); i++) {
                        setTimeout(() => window.print(), i * 1000);
                      }
                    }}
                    variant="outline"
                    size="sm"
                    className="text-green-600 border-green-600 hover:bg-green-50 print:hidden"
                  >
                    In {printConfig.printCopies} bản
                  </Button>
                )}
              </div>
              
              <Button onClick={() => setSelectedOrder(null)} variant="secondary">
                Đóng
              </Button>
            </div>
          </div>
        </div>
      )}
      </div>
    </>
  );
}
