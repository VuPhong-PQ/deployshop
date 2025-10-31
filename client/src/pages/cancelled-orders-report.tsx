import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { useToast } from "@/hooks/use-toast";
import { apiRequest } from "@/lib/queryClient";
import { Search, Calendar, FileX, Download, Eye, EyeOff } from "lucide-react";
import { cn } from "@/lib/utils";

type CancelledOrderItem = {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  lossAmount: number;
};

type CancelledOrder = {
  orderId: number;
  orderNumber: string;
  customerName: string;
  createdAt: string;
  cancelledAt: string;
  cancellationReason: string;
  totalAmount: number;
  subTotal: number;
  taxAmount: number;
  discountAmount: number;
  paymentMethod: string;
  items: CancelledOrderItem[];
  totalQuantityCancelled: number;
  totalLossAmount: number;
};

type CancelledOrdersReport = {
  summary: {
    totalCancelledOrders: number;
    totalQuantityCancelled: number;
    totalLossAmount: number;
    averageLossPerOrder: number;
    reportPeriod: {
      startDate: string;
      endDate: string;
    };
  };
  orders: CancelledOrder[];
};

export default function CancelledOrdersReport() {
  const { toast } = useToast();
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [orderIdFilter, setOrderIdFilter] = useState("");
  const [expandedOrders, setExpandedOrders] = useState<Set<number>>(new Set());

  // Set default dates (last 30 days)
  useEffect(() => {
    const today = new Date();
    const thirtyDaysAgo = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000);
    
    setEndDate(today.toISOString().split('T')[0]);
    setStartDate(thirtyDaysAgo.toISOString().split('T')[0]);
  }, []);

  const { data: reportData, isLoading, refetch } = useQuery<CancelledOrdersReport>({
    queryKey: ["/api/reports/cancelled-orders", startDate, endDate, orderIdFilter],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (startDate) params.append('startDate', startDate);
      if (endDate) params.append('endDate', endDate);
      if (orderIdFilter) params.append('orderId', orderIdFilter);
      
      const url = `/api/reports/cancelled-orders${params.toString() ? '?' + params.toString() : ''}`;
      return await apiRequest(url, { method: "GET" });
    },
    enabled: !!startDate && !!endDate,
  });

  const toggleOrderExpansion = (orderId: number) => {
    const newExpanded = new Set(expandedOrders);
    if (newExpanded.has(orderId)) {
      newExpanded.delete(orderId);
    } else {
      newExpanded.add(orderId);
    }
    setExpandedOrders(newExpanded);
  };

  const handleSearch = () => {
    refetch();
  };

  const handleClearFilters = () => {
    setOrderIdFilter("");
    const today = new Date();
    const thirtyDaysAgo = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000);
    setEndDate(today.toISOString().split('T')[0]);
    setStartDate(thirtyDaysAgo.toISOString().split('T')[0]);
  };

  const formatPaymentMethod = (method: string) => {
    switch (method) {
      case 'cash': return 'Tiền mặt';
      case 'card': return 'Thẻ ngân hàng';
      case 'qr': return 'QR Code';
      case 'ewallet': return 'Ví điện tử';
      case 'banktransfer': return 'Chuyển khoản';
      default: return 'Tiền mặt';
    }
  };

  return (
    <AppLayout title="Báo cáo đơn hàng đã hủy">
      <div className="container mx-auto p-6 space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold">Báo cáo đơn hàng đã hủy</h1>
            <p className="text-gray-600 mt-1">
              Theo dõi chi tiết các đơn hàng đã bị hủy và lý do hủy
            </p>
          </div>
          <FileX className="h-8 w-8 text-red-500" />
        </div>

        {/* Filters */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Search className="h-5 w-5" />
              Tìm kiếm và lọc
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div>
                <label className="block text-sm font-medium mb-2">Từ ngày</label>
                <Input
                  type="date"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2">Đến ngày</label>
                <Input
                  type="date"
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2">Mã đơn hàng</label>
                <Input
                  placeholder="Nhập mã đơn hàng..."
                  value={orderIdFilter}
                  onChange={(e) => setOrderIdFilter(e.target.value)}
                />
              </div>
              <div className="flex items-end gap-2">
                <Button onClick={handleSearch} className="flex-1">
                  <Search className="h-4 w-4 mr-2" />
                  Tìm kiếm
                </Button>
                <Button variant="outline" onClick={handleClearFilters}>
                  Xóa lọc
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Summary Cards */}
        {reportData && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <Card>
              <CardContent className="p-6">
                <div className="text-2xl font-bold text-red-600">
                  {reportData.summary.totalCancelledOrders}
                </div>
                <p className="text-sm text-gray-600">Tổng đơn hàng hủy</p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="p-6">
                <div className="text-2xl font-bold text-orange-600">
                  {reportData.summary.totalQuantityCancelled}
                </div>
                <p className="text-sm text-gray-600">Tổng số lượng hủy</p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="p-6">
                <div className="text-2xl font-bold text-red-700">
                  {reportData.summary.totalLossAmount.toLocaleString('vi-VN')}₫
                </div>
                <p className="text-sm text-gray-600">Tổng giá trị hủy</p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="p-6">
                <div className="text-2xl font-bold text-gray-600">
                  {reportData.summary.averageLossPerOrder.toLocaleString('vi-VN')}₫
                </div>
                <p className="text-sm text-gray-600">Trung bình mỗi đơn</p>
              </CardContent>
            </Card>
          </div>
        )}

        {/* Orders List */}
        <Card>
          <CardHeader>
            <CardTitle>Chi tiết đơn hàng đã hủy</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="text-center py-8">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div>
                <p className="mt-2 text-gray-500">Đang tải...</p>
              </div>
            ) : !reportData || reportData.orders.length === 0 ? (
              <div className="text-center py-8">
                <FileX className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-500">Không có đơn hàng hủy nào trong khoảng thời gian này</p>
              </div>
            ) : (
              <div className="space-y-4">
                {reportData.orders.map((order) => (
                  <div key={order.orderId} className="border rounded-lg overflow-hidden">
                    {/* Order Header */}
                    <div className="bg-red-50 p-4 border-b">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-4">
                          <div>
                            <h3 className="font-semibold text-lg">
                              Đơn hàng #{order.orderId}
                            </h3>
                            <p className="text-sm text-gray-600">
                              {order.customerName} • {new Date(order.createdAt).toLocaleString('vi-VN')}
                            </p>
                          </div>
                          <Badge variant="destructive">
                            Đã hủy
                          </Badge>
                        </div>
                        <div className="flex items-center gap-4">
                          <div className="text-right">
                            <p className="text-sm text-gray-600">Tổng giá trị hủy</p>
                            <p className="font-bold text-red-600">
                              {order.totalLossAmount.toLocaleString('vi-VN')}₫
                            </p>
                          </div>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => toggleOrderExpansion(order.orderId)}
                          >
                            {expandedOrders.has(order.orderId) ? (
                              <><EyeOff className="h-4 w-4 mr-1" /> Ẩn chi tiết</>
                            ) : (
                              <><Eye className="h-4 w-4 mr-1" /> Xem chi tiết</>
                            )}
                          </Button>
                        </div>
                      </div>

                      {/* Cancellation Reason */}
                      <div className="mt-3 p-3 bg-red-100 border border-red-200 rounded">
                        <p className="text-sm">
                          <span className="font-medium text-red-700">Lý do hủy:</span>
                          <span className="text-red-600 ml-2">{order.cancellationReason}</span>
                        </p>
                      </div>
                    </div>

                    {/* Order Details (Expandable) */}
                    {expandedOrders.has(order.orderId) && (
                      <div className="p-4 space-y-4">
                        {/* Order Info */}
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
                          <div>
                            <span className="font-medium">Phương thức thanh toán:</span>
                            <span className="ml-2">{formatPaymentMethod(order.paymentMethod)}</span>
                          </div>
                          <div>
                            <span className="font-medium">Tổng số lượng:</span>
                            <span className="ml-2">{order.totalQuantityCancelled} sản phẩm</span>
                          </div>
                          <div>
                            <span className="font-medium">Ngày hủy:</span>
                            <span className="ml-2">{new Date(order.cancelledAt).toLocaleDateString('vi-VN')}</span>
                          </div>
                        </div>

                        {/* Items Table */}
                        <div>
                          <h4 className="font-medium mb-3">Chi tiết sản phẩm hủy:</h4>
                          <div className="border rounded-lg overflow-hidden">
                            <table className="w-full">
                              <thead className="bg-gray-50">
                                <tr>
                                  <th className="px-4 py-2 text-left">Sản phẩm</th>
                                  <th className="px-4 py-2 text-center">Số lượng hủy</th>
                                  <th className="px-4 py-2 text-right">Đơn giá</th>
                                  <th className="px-4 py-2 text-right">Thành tiền hủy</th>
                                </tr>
                              </thead>
                              <tbody>
                                {order.items.map((item, index) => (
                                  <tr key={index} className="border-t">
                                    <td className="px-4 py-2">{item.productName}</td>
                                    <td className="px-4 py-2 text-center font-medium text-red-600">
                                      {item.quantity}
                                    </td>
                                    <td className="px-4 py-2 text-right">
                                      {item.unitPrice.toLocaleString('vi-VN')}₫
                                    </td>
                                    <td className="px-4 py-2 text-right font-medium text-red-600">
                                      {item.totalPrice.toLocaleString('vi-VN')}₫
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                              <tfoot className="bg-gray-50">
                                <tr>
                                  <td colSpan={3} className="px-4 py-3 text-right font-bold">
                                    Tổng giá trị hủy:
                                  </td>
                                  <td className="px-4 py-3 text-right font-bold text-red-600">
                                    {order.totalLossAmount.toLocaleString('vi-VN')}₫
                                  </td>
                                </tr>
                              </tfoot>
                            </table>
                          </div>
                        </div>

                        {/* Financial Summary */}
                        <div className="bg-gray-50 p-4 rounded-lg">
                          <h4 className="font-medium mb-2">Tóm tắt tài chính:</h4>
                          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                            <div>
                              <span className="text-gray-600">Tạm tính:</span>
                              <p className="font-medium">{order.subTotal.toLocaleString('vi-VN')}₫</p>
                            </div>
                            <div>
                              <span className="text-gray-600">Thuế:</span>
                              <p className="font-medium">{order.taxAmount.toLocaleString('vi-VN')}₫</p>
                            </div>
                            <div>
                              <span className="text-gray-600">Giảm giá:</span>
                              <p className="font-medium text-green-600">-{order.discountAmount.toLocaleString('vi-VN')}₫</p>
                            </div>
                            <div>
                              <span className="text-gray-600">Tổng cộng:</span>
                              <p className="font-bold text-red-600">{order.totalAmount.toLocaleString('vi-VN')}₫</p>
                            </div>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </AppLayout>
  );
}