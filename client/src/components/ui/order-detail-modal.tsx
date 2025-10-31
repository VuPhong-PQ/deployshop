import { useState, useEffect } from "react";
import { X, Printer, User, Calendar, Receipt } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useQuery } from "@tanstack/react-query";
import { apiRequest } from "@/lib/queryClient";
import { useLocation } from "wouter";
import { useToast } from "@/hooks/use-toast";
import { useNotificationSound } from "@/hooks/use-notification-sound";

interface OrderDetailModalProps {
  orderId: number | null;
  show: boolean;
  onClose: () => void;
  onReopenOrder?: (orderDetail: OrderDetail) => void;
}

interface OrderDetail {
  orderId: number;
  customerId?: number;
  customerName?: string;
  createdAt: string;
  totalAmount: number;
  subTotal: number;
  taxAmount: number;
  discountAmount: number;
  paymentMethod?: string;
  paymentStatus?: string;
  status?: string;
  orderNumber?: string;
  cashierId?: string;
  storeId?: string;
  notes?: string;
  customer?: {
    customerId: number;
    hoTen: string;
    soDienThoai?: string;
    email?: string;
    diaChi?: string;
  };
  items: {
    productId: number;
    productName: string;
    quantity: number;
    price: number;
    totalPrice: number;
  }[];
}

export function OrderDetailModal({ orderId, show, onClose, onReopenOrder }: OrderDetailModalProps) {
  const [isPrinting, setIsPrinting] = useState(false);
  const [, navigate] = useLocation();
  const { toast } = useToast();
  const { playNotificationSound } = useNotificationSound();

  // Fetch order details
  const { data: orderDetail, isLoading } = useQuery<OrderDetail>({
    queryKey: ['/api/orders', orderId],
    queryFn: () => apiRequest(`/api/orders/${orderId}`, { method: 'GET' }),
    enabled: !!orderId && show,
  });

  // Format payment method
  const formatPaymentMethod = (method?: string) => {
    switch (method) {
      case 'cash': return 'Tiền mặt';
      case 'card': return 'Thẻ ngân hàng';
      case 'qr': return 'QR Code';
      case 'ewallet': return 'Ví điện tử';
      default: return 'Tiền mặt';
    }
  };

  // Format payment status
  const formatPaymentStatus = (status?: string) => {
    switch (status) {
      case 'paid': return 'Đã thanh toán';
      case 'pending': return 'Chờ thanh toán';
      case 'failed': return 'Thanh toán thất bại';
      default: return 'Đã thanh toán';
    }
  };

  // Format order status
  const formatOrderStatus = (status?: string) => {
    switch (status) {
      case 'completed': return 'Hoàn thành';
      case 'pending': return 'Đang xử lý';
      case 'cancelled': return 'Đã hủy';
      default: return 'Hoàn thành';
    }
  };

  // Handle reopen order for pending orders
  const handleReopenOrder = () => {
    if (orderDetail && onReopenOrder) {
      // Thông báo ngay lập tức
      toast({
        title: "Đang mở lại đơn hàng! 🔄",
        description: `Đơn hàng #${orderDetail.orderId} của ${orderDetail.customerName || orderDetail.customer?.hoTen || 'khách vãng lai'} đã được tải vào giỏ hàng`,
      });
      
      // Phát âm thanh thông báo
      playNotificationSound();
      
      // Store order data in localStorage temporarily (same as notification modal)
      localStorage.setItem('reopenOrder', JSON.stringify(orderDetail));
      
      // Dispatch custom event to notify sales page to check localStorage
      window.dispatchEvent(new CustomEvent('reopenOrderSet'));
      
      // Navigate to sales page
      navigate('/sales');
      
      onClose(); // Close modal after reopening order
    }
  };

  // Handle print - navigate to print page
  const handlePrint = () => {
    if (orderDetail) {
      navigate(`/print-order/${orderDetail.orderId}`);
    }
  };

  if (!show || !orderId) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
      <Card className="w-full max-w-4xl max-h-[90vh] overflow-y-auto">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
          <CardTitle className="text-xl font-bold">
            Chi tiết đơn hàng #{orderId}
          </CardTitle>
          <Button 
            variant="ghost" 
            size="icon" 
            onClick={onClose}
            className="h-6 w-6"
          >
            <X className="h-4 w-4" />
          </Button>
        </CardHeader>

        <CardContent>
          {isLoading ? (
            <div className="text-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div>
              <p className="mt-2 text-gray-500">Đang tải...</p>
            </div>
          ) : orderDetail ? (
            <>
              {/* Customer Information */}
              <div className="mb-6">
                <h3 className="flex items-center gap-2 text-lg font-semibold mb-3">
                  <User className="h-5 w-5" />
                  Thông tin khách hàng
                </h3>
                <div className="bg-gray-50 p-4 rounded-lg">
                  <p><strong>Tên:</strong> {orderDetail.customerName || orderDetail.customer?.hoTen || 'Khách vãng lai'}</p>
                  {orderDetail.customer?.soDienThoai && (
                    <p><strong>Số điện thoại:</strong> {orderDetail.customer.soDienThoai}</p>
                  )}
                  {orderDetail.customer?.email && (
                    <p><strong>Email:</strong> {orderDetail.customer.email}</p>
                  )}
                  {orderDetail.customer?.diaChi && (
                    <p><strong>Địa chỉ:</strong> {orderDetail.customer.diaChi}</p>
                  )}
                </div>
              </div>

              {/* Order Information */}
              <div className="mb-6">
                <h3 className="flex items-center gap-2 text-lg font-semibold mb-3">
                  <Receipt className="h-5 w-5" />
                  Thông tin đơn hàng
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <p><strong>Mã đơn:</strong> #{orderDetail.orderId}</p>
                    <p><strong>Số đơn hàng:</strong> {orderDetail.orderNumber || 'PENDING' + Date.now()}</p>
                    <p><strong>Ngày tạo:</strong> {new Date(orderDetail.createdAt).toLocaleString('vi-VN')}</p>
                  </div>
                  <div className="space-y-2">
                    <p><strong>Trạng thái:</strong> 
                      <Badge variant={orderDetail.status === 'completed' ? 'default' : orderDetail.status === 'pending' ? 'secondary' : 'destructive'} className="ml-2">
                        {formatOrderStatus(orderDetail.status)}
                      </Badge>
                    </p>
                    <p><strong>Phương thức thanh toán:</strong> {formatPaymentMethod(orderDetail.paymentMethod)}</p>
                    <p><strong>Trạng thái thanh toán:</strong> 
                      <Badge variant={orderDetail.paymentStatus === 'paid' ? 'default' : 'secondary'} className="ml-2">
                        {formatPaymentStatus(orderDetail.paymentStatus)}
                      </Badge>
                    </p>
                    <p><strong>Thu ngân:</strong> {orderDetail.cashierId || '550e8400-e29b-41d4-a716-446655440001'}</p>
                  </div>
                </div>
              </div>

              {/* Products */}
              <div className="mb-6">
                <h3 className="flex items-center gap-2 text-lg font-semibold mb-3">
                  <Receipt className="h-5 w-5" />
                  Chi tiết sản phẩm
                </h3>
                <div className="border rounded-lg overflow-hidden">
                  <table className="w-full">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-2 text-left">Sản phẩm</th>
                        <th className="px-4 py-2 text-center">SL</th>
                        <th className="px-4 py-2 text-right">Đơn giá</th>
                        <th className="px-4 py-2 text-right">Thành tiền</th>
                      </tr>
                    </thead>
                    <tbody>
                      {orderDetail.items.map((item, index) => (
                        <tr key={index} className="border-t">
                          <td className="px-4 py-2">{item.productName}</td>
                          <td className="px-4 py-2 text-center">{item.quantity}</td>
                          <td className="px-4 py-2 text-right">{item.price.toLocaleString('vi-VN')}đ</td>
                          <td className="px-4 py-2 text-right font-medium">{item.totalPrice.toLocaleString('vi-VN')}đ</td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot className="bg-gray-50">
                      {/* Hiển thị chi tiết tính toán nếu có */}
                      {(orderDetail.subTotal > 0 || orderDetail.discountAmount > 0 || orderDetail.taxAmount > 0) && (
                        <>
                          {orderDetail.subTotal > 0 && (
                            <tr className="border-t">
                              <td colSpan={3} className="px-4 py-2 text-right">Tạm tính:</td>
                              <td className="px-4 py-2 text-right">{orderDetail.subTotal.toLocaleString('vi-VN')}đ</td>
                            </tr>
                          )}
                          {orderDetail.discountAmount > 0 && (
                            <tr>
                              <td colSpan={3} className="px-4 py-2 text-right text-red-600">Giảm giá:</td>
                              <td className="px-4 py-2 text-right text-red-600">-{orderDetail.discountAmount.toLocaleString('vi-VN')}đ</td>
                            </tr>
                          )}
                          {orderDetail.taxAmount > 0 && (
                            <tr>
                              <td colSpan={3} className="px-4 py-2 text-right">Thuế VAT:</td>
                              <td className="px-4 py-2 text-right">{orderDetail.taxAmount.toLocaleString('vi-VN')}đ</td>
                            </tr>
                          )}
                        </>
                      )}
                      <tr className="border-t-2">
                        <td colSpan={3} className="px-4 py-3 text-right font-bold">Tổng cộng:</td>
                        <td className="px-4 py-3 text-right font-bold text-lg text-blue-600">
                          {orderDetail.totalAmount.toLocaleString('vi-VN')}đ
                        </td>
                      </tr>
                    </tfoot>
                  </table>
                </div>
              </div>

              {/* Action Buttons */}
              <div className="flex flex-col sm:flex-row justify-end space-y-2 sm:space-y-0 sm:space-x-3 pt-4 border-t">
                <Button variant="outline" onClick={onClose}>
                  Đóng
                </Button>
                
                {/* Show "Mở lại đơn hàng" button for pending orders */}
                {orderDetail.paymentStatus === 'pending' && onReopenOrder && (
                  <Button 
                    onClick={handleReopenOrder}
                    className="flex items-center justify-center gap-2 bg-orange-500 hover:bg-orange-600"
                  >
                    <Receipt className="h-4 w-4" />
                    Mở lại đơn hàng
                  </Button>
                )}
                
                <Button 
                  onClick={handlePrint} 
                  disabled={isPrinting}
                  className="flex items-center justify-center gap-2 bg-green-600 hover:bg-green-700 text-white"
                  size="lg"
                >
                  <Printer className="h-4 w-4" />
                  {isPrinting ? 'Đang in...' : 'In đơn hàng'}
                </Button>
              </div>
            </>
          ) : (
            <div className="text-center py-8">
              <p className="text-gray-500">Không tìm thấy đơn hàng</p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}