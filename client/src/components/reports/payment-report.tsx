import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { apiRequest } from "@/lib/queryClient";
import { Banknote, CreditCard, QrCode, Smartphone, TrendingUp, Calendar, RefreshCw, ChevronDown, ChevronRight, Eye, Package, User, Clock, Download, Receipt } from "lucide-react";
import * as XLSX from 'xlsx';

interface OrderItem {
  productName: string;
  quantity: number;
  price: number;
  totalPrice: number;
}

interface OrderDetail {
  orderId: number;
  orderNumber: string;
  customerName: string;
  totalAmount: number;
  createdAt: string;
  items: OrderItem[];
}

interface PaymentStat {
  paymentMethod: string;
  paymentMethodId: string;
  totalAmount: number;
  orderCount: number;
  percentage: number;
  orders: OrderDetail[];
}

interface PaymentStatsData {
  fromDate: string;
  toDate: string;
  totalRevenue: number;
  totalOrders: number;
  paymentStats: PaymentStat[];
}

export function PaymentReport() {
  const [fromDate, setFromDate] = useState(() => {
    const date = new Date();
    date.setDate(date.getDate() - 7); // Chỉ lấy 7 ngày gần nhất
    return date.toISOString().split('T')[0];
  });
  
  const [toDate, setToDate] = useState(() => {
    const date = new Date();
    date.setDate(date.getDate() + 1); // Thêm 1 ngày để đảm bảo bao gồm hôm nay
    return date.toISOString().split('T')[0];
  });

  const [expandedPaymentMethods, setExpandedPaymentMethods] = useState<Set<string>>(new Set());
  const [expandedOrders, setExpandedOrders] = useState<Set<number>>(new Set());

  const { data: paymentStats, isLoading, refetch } = useQuery<PaymentStatsData>({
    queryKey: ["/api/PaymentStats", fromDate, toDate],
    queryFn: async () => {
      const params = new URLSearchParams({
        fromDate: fromDate,
        toDate: toDate
      });
      const res = await apiRequest(`/api/PaymentStats?${params}`, { method: "GET" });
      console.log('Payment stats data:', res); // Debug log
      console.log('Payment stats structure:', JSON.stringify(res, null, 2)); // Detailed debug
      return res;
    },
  });

  const getPaymentIcon = (methodId: string) => {
    switch (methodId) {
      case 'cash': return <Banknote className="w-5 h-5 text-green-600" />;
      case 'card': return <CreditCard className="w-5 h-5 text-blue-600" />;
      case 'qr': return <QrCode className="w-5 h-5 text-purple-600" />;
      case 'ewallet': return <Smartphone className="w-5 h-5 text-orange-600" />;
      default: return <Banknote className="w-5 h-5 text-gray-600" />;
    }
  };

  const getPaymentColor = (methodId: string) => {
    switch (methodId) {
      case 'cash': return 'bg-green-500';
      case 'card': return 'bg-blue-500';
      case 'qr': return 'bg-purple-500';
      case 'ewallet': return 'bg-orange-500';
      default: return 'bg-gray-500';
    }
  };

  const handleRefresh = () => {
    refetch();
  };

  const togglePaymentMethod = (methodId: string) => {
    setExpandedPaymentMethods(prev => {
      const newSet = new Set(prev);
      if (newSet.has(methodId)) {
        newSet.delete(methodId);
      } else {
        newSet.add(methodId);
      }
      return newSet;
    });
  };

  const toggleOrder = (orderId: number) => {
    setExpandedOrders(prev => {
      const newSet = new Set(prev);
      if (newSet.has(orderId)) {
        newSet.delete(orderId);
      } else {
        newSet.add(orderId);
      }
      return newSet;
    });
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleString('vi-VN');
  };

  const exportToExcel = () => {
    if (!paymentStats?.paymentStats || paymentStats.paymentStats.length === 0) {
      alert('Không có dữ liệu để xuất');
      return;
    }

    const workbook = XLSX.utils.book_new();

    // Sheet 1: Tổng quan
    const overviewData = [
      ['Báo cáo Hình thức Thanh toán'],
      [`Từ ngày: ${fromDate} đến ${toDate}`],
      [`Tổng doanh thu: ${paymentStats.totalRevenue?.toLocaleString('vi-VN')}₫`],
      [`Tổng đơn hàng: ${paymentStats.totalOrders}`],
      [''],
      ['Hình thức thanh toán', 'Số đơn hàng', 'Doanh thu', 'Tỷ lệ %']
    ];

    paymentStats.paymentStats.forEach(stat => {
      overviewData.push([
        stat.paymentMethod,
        stat.orderCount.toString(),
        `${stat.totalAmount.toLocaleString('vi-VN')}₫`,
        `${stat.percentage}%`
      ]);
    });

    const overviewWs = XLSX.utils.aoa_to_sheet(overviewData);
    XLSX.utils.book_append_sheet(workbook, overviewWs, 'Tổng quan');

    // Sheet 2: Chi tiết từng đơn hàng
    const detailData = [
      ['Chi tiết Đơn hàng theo Hình thức Thanh toán'],
      [''],
      ['Hình thức thanh toán', 'Số đơn', 'Mã đơn hàng', 'Khách hàng', 'Thời gian', 'Tổng tiền', 'Sản phẩm', 'Số lượng', 'Đơn giá', 'Thành tiền']
    ];

    paymentStats.paymentStats.forEach(stat => {
      if (stat.orders && stat.orders.length > 0) {
        stat.orders.forEach(order => {
          if (order.items && order.items.length > 0) {
            order.items.forEach((item, itemIndex) => {
              detailData.push([
                itemIndex === 0 ? stat.paymentMethod : '', // Chỉ hiện tên phương thức ở dòng đầu
                itemIndex === 0 ? order.orderId.toString() : '',
                itemIndex === 0 ? (order.orderNumber || `Đơn #${order.orderId}`) : '',
                itemIndex === 0 ? order.customerName : '',
                itemIndex === 0 ? formatDate(order.createdAt) : '',
                itemIndex === 0 ? `${order.totalAmount.toLocaleString('vi-VN')}₫` : '',
                item.productName,
                item.quantity.toString(),
                `${item.price.toLocaleString('vi-VN')}₫`,
                `${item.totalPrice.toLocaleString('vi-VN')}₫`
              ]);
            });
          } else {
            // Nếu đơn hàng không có items
            detailData.push([
              stat.paymentMethod,
              order.orderId.toString(),
              order.orderNumber || `Đơn #${order.orderId}`,
              order.customerName,
              formatDate(order.createdAt),
              `${order.totalAmount.toLocaleString('vi-VN')}₫`,
              'Không có sản phẩm',
              '',
              '',
              ''
            ]);
          }
        });
        // Thêm dòng trống giữa các phương thức thanh toán
        detailData.push(['', '', '', '', '', '', '', '', '', '']);
      }
    });

    const detailWs = XLSX.utils.aoa_to_sheet(detailData);
    XLSX.utils.book_append_sheet(workbook, detailWs, 'Chi tiết đơn hàng');

    // Sheet 3: Xếp hạng phương thức thanh toán
    const rankingData = [
      ['Xếp hạng Hình thức Thanh toán'],
      [''],
      ['Hạng', 'Hình thức thanh toán', 'Số đơn hàng', 'Doanh thu', 'Tỷ lệ %']
    ];

    paymentStats.paymentStats.forEach((stat, index) => {
      rankingData.push([
        (index + 1).toString(),
        stat.paymentMethod,
        stat.orderCount.toString(),
        `${stat.totalAmount.toLocaleString('vi-VN')}₫`,
        `${stat.percentage}%`
      ]);
    });

    const rankingWs = XLSX.utils.aoa_to_sheet(rankingData);
    XLSX.utils.book_append_sheet(workbook, rankingWs, 'Xếp hạng');

    // Xuất file
    const fileName = `Bao_cao_hinh_thuc_thanh_toan_${fromDate}_den_${toDate}.xlsx`;
    XLSX.writeFile(workbook, fileName);
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="animate-pulse">
          <div className="h-8 bg-gray-200 rounded w-1/4 mb-4"></div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            {[1, 2, 3, 4].map(i => (
              <div key={i} className="h-24 bg-gray-200 rounded"></div>
            ))}
          </div>
          <div className="h-64 bg-gray-200 rounded"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header với bộ lọc ngày */}
      <div className="bg-white rounded-lg border p-6 sm:p-8">
        <div className="space-y-6">
          <div className="text-center sm:text-left">
            <h2 className="text-xl sm:text-2xl font-bold text-gray-900 flex items-center justify-center sm:justify-start gap-2 mb-3">
              <TrendingUp className="w-5 h-5 sm:w-6 sm:h-6 text-blue-600" />
              Báo cáo Hình thức Thanh toán
            </h2>
            <p className="text-gray-600 text-sm mb-4">
              Thống kê doanh thu theo phương thức thanh toán
            </p>
          </div>
          
          <div className="border-t pt-5">
            <div className="flex flex-col gap-3">
              <div className="flex flex-col sm:flex-row gap-2 items-start sm:items-center">
                <Calendar className="w-4 h-4 text-gray-500 flex-shrink-0" />
                <div className="flex flex-col sm:flex-row gap-2 w-full sm:w-auto">
                  <Input
                    type="date"
                    value={fromDate}
                    onChange={(e) => setFromDate(e.target.value)}
                    className="w-full sm:w-auto text-sm"
                  />
                  <span className="text-gray-500 self-center">-</span>
                  <Input
                    type="date"
                    value={toDate}
                    onChange={(e) => setToDate(e.target.value)}
                    className="w-full sm:w-auto text-sm"
                  />
                </div>
              </div>
              <div className="flex flex-col sm:flex-row gap-2">
                <Button onClick={handleRefresh} size="sm" variant="outline" className="w-full sm:w-auto">
                  <RefreshCw className="w-4 h-4 mr-1" />
                  Làm mới
                </Button>
                <Button 
                  onClick={exportToExcel} 
                  size="sm" 
                  className="bg-green-600 hover:bg-green-700 text-white w-full sm:w-auto"
                >
                  <Download className="w-4 h-4 mr-1" />
                  Xuất Excel
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Tổng quan */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="min-w-0 flex-1">
                <p className="text-sm text-gray-600 truncate">Tổng doanh thu</p>
                <p className="text-lg sm:text-2xl font-bold text-green-600 truncate">
                  {paymentStats?.totalRevenue?.toLocaleString('vi-VN')}₫
                </p>
              </div>
              <TrendingUp className="w-6 h-6 sm:w-8 sm:h-8 text-green-600 flex-shrink-0" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="min-w-0 flex-1">
                <p className="text-sm text-gray-600 truncate">Tổng đơn hàng</p>
                <p className="text-lg sm:text-2xl font-bold text-blue-600">
                  {paymentStats?.totalOrders || 0}
                </p>
              </div>
              <div className="w-6 h-6 sm:w-8 sm:h-8 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0">
                <span className="text-blue-600 font-bold text-xs sm:text-sm">{paymentStats?.totalOrders || 0}</span>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="min-w-0 flex-1">
                <p className="text-sm text-gray-600 truncate">Phương thức phổ biến</p>
                <p className="text-sm sm:text-lg font-semibold text-purple-600 truncate">
                  {paymentStats?.paymentStats?.[0]?.paymentMethod || "Chưa có dữ liệu"}
                </p>
              </div>
              <div className="flex-shrink-0">
                {paymentStats?.paymentStats?.[0] && getPaymentIcon(paymentStats.paymentStats[0].paymentMethodId)}
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="min-w-0 flex-1">
                <p className="text-sm text-gray-600 truncate">Số phương thức sử dụng</p>
                <p className="text-lg sm:text-2xl font-bold text-orange-600">
                  {paymentStats?.paymentStats?.length || 0}
                </p>
              </div>
              <div className="w-6 h-6 sm:w-8 sm:h-8 bg-orange-100 rounded-full flex items-center justify-center flex-shrink-0">
                <span className="text-orange-600 font-bold text-xs sm:text-sm">{paymentStats?.paymentStats?.length || 0}</span>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Biểu đồ và bảng xếp hạng */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Biểu đồ dạng cột */}
        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-base sm:text-lg flex items-center gap-2 mb-2">
              <TrendingUp className="w-4 h-4 sm:w-5 sm:h-5 text-green-600" />
              Biểu đồ Doanh thu theo Hình thức
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {paymentStats?.paymentStats?.map((stat, index) => (
                <div key={stat.paymentMethodId} className="space-y-2">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2 min-w-0 flex-1">
                      {getPaymentIcon(stat.paymentMethodId)}
                      <span className="font-medium text-sm sm:text-base truncate">{stat.paymentMethod}</span>
                    </div>
                    <div className="text-right flex-shrink-0 ml-2">
                      <div className="font-semibold text-sm sm:text-base">{stat.totalAmount.toLocaleString('vi-VN')}₫</div>
                      <div className="text-xs sm:text-sm text-gray-500">{stat.percentage}%</div>
                    </div>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-2 sm:h-3">
                    <div
                      className={`h-2 sm:h-3 rounded-full ${getPaymentColor(stat.paymentMethodId)}`}
                      style={{ width: `${stat.percentage}%` }}
                    ></div>
                  </div>
                  <div className="text-xs text-gray-500">
                    {stat.orderCount} đơn hàng
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Bảng xếp hạng */}
        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-base sm:text-lg flex items-center gap-2 mb-2">
              <TrendingUp className="w-4 h-4 sm:w-5 sm:h-5 text-yellow-600" />
              Xếp hạng Hình thức Thanh toán
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {paymentStats?.paymentStats?.map((stat, index) => (
                <div key={stat.paymentMethodId} className="flex items-center gap-3 p-3 rounded-lg bg-gray-50 hover:bg-gray-100 transition-colors">
                  <div className="flex-shrink-0">
                    <div className={`w-6 h-6 sm:w-8 sm:h-8 rounded-full flex items-center justify-center text-white font-bold text-xs sm:text-sm ${
                      index === 0 ? 'bg-yellow-500' : 
                      index === 1 ? 'bg-gray-400' : 
                      index === 2 ? 'bg-orange-600' : 'bg-gray-300'
                    }`}>
                      #{index + 1}
                    </div>
                  </div>
                  
                  <div className="flex-shrink-0">
                    {getPaymentIcon(stat.paymentMethodId)}
                  </div>
                  
                  <div className="flex-grow min-w-0">
                    <div className="font-medium text-sm sm:text-base truncate">{stat.paymentMethod}</div>
                    <div className="text-xs sm:text-sm text-gray-500">
                      {stat.orderCount} đơn hàng • {stat.percentage}%
                    </div>
                  </div>
                  
                  <div className="text-right flex-shrink-0">
                    <div className="font-semibold text-sm sm:text-lg">
                      {stat.totalAmount.toLocaleString('vi-VN')}₫
                    </div>
                  </div>
                </div>
              ))}
              
              {(!paymentStats?.paymentStats || paymentStats.paymentStats.length === 0) && (
                <div className="text-center py-8 text-gray-500">
                  <TrendingUp className="w-8 h-8 sm:w-12 sm:h-12 mx-auto mb-3 text-gray-300" />
                  <p className="text-sm sm:text-base">Chưa có dữ liệu thanh toán</p>
                  <p className="text-xs sm:text-sm">Hãy thực hiện một số giao dịch để xem báo cáo</p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Chi tiết đơn hàng theo hình thức thanh toán */}
      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-base sm:text-lg flex items-center gap-2 mb-2">
            <Receipt className="w-4 h-4 sm:w-5 sm:h-5 text-blue-600" />
            Chi tiết Đơn hàng theo Hình thức Thanh toán
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {paymentStats?.paymentStats?.map((stat) => (
              <div key={stat.paymentMethodId} className="border rounded-lg overflow-hidden">
                <div 
                  className="p-3 sm:p-4 bg-gray-50 cursor-pointer flex items-center justify-between hover:bg-gray-100 transition-colors"
                  onClick={() => togglePaymentMethod(stat.paymentMethodId)}
                >
                  <div className="flex items-center gap-2 sm:gap-3 min-w-0 flex-1">
                    {getPaymentIcon(stat.paymentMethodId)}
                    <div className="min-w-0 flex-1">
                      <h3 className="font-semibold text-sm sm:text-base truncate">{stat.paymentMethod}</h3>
                      <p className="text-xs sm:text-sm text-gray-600 truncate">
                        {stat.orderCount} đơn hàng • {stat.totalAmount.toLocaleString('vi-VN')}₫
                      </p>
                    </div>
                  </div>
                  <div className="flex-shrink-0">
                    {expandedPaymentMethods.has(stat.paymentMethodId) ? 
                      <ChevronDown className="w-4 h-4 sm:w-5 sm:h-5" /> : 
                      <ChevronRight className="w-4 h-4 sm:w-5 sm:h-5" />
                    }
                  </div>
                </div>
                
                {expandedPaymentMethods.has(stat.paymentMethodId) && (
                  <div className="border-t">
                    <div className="p-3 sm:p-4 space-y-3">
                      {(() => {
                        console.log(`Orders for ${stat.paymentMethodId}:`, stat.orders);
                        return null;
                      })()}
                      {stat.orders?.map((order) => (
                        <div key={order.orderId} className="border rounded-lg overflow-hidden">
                          <div 
                            className="p-3 bg-white cursor-pointer flex items-center justify-between hover:bg-gray-50 transition-colors"
                            onClick={() => toggleOrder(order.orderId)}
                          >
                            <div className="flex items-center gap-2 sm:gap-3 min-w-0 flex-1">
                              <div className="w-6 h-6 sm:w-8 sm:h-8 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0">
                                <span className="text-blue-600 font-semibold text-xs">#{order.orderId}</span>
                              </div>
                              <div className="min-w-0 flex-1">
                                <div className="font-medium text-sm sm:text-base truncate">{order.orderNumber || `Đơn #${order.orderId}`}</div>
                                <div className="text-xs sm:text-sm text-gray-600">
                                  <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-4">
                                    <span className="flex items-center gap-1 truncate">
                                      <User className="w-3 h-3 flex-shrink-0" />
                                      <span className="truncate">{order.customerName}</span>
                                    </span>
                                    <span className="flex items-center gap-1">
                                      <Clock className="w-3 h-3 flex-shrink-0" />
                                      {formatDate(order.createdAt)}
                                    </span>
                                  </div>
                                </div>
                              </div>
                            </div>
                            <div className="text-right flex-shrink-0 ml-2">
                              <div className="font-semibold text-sm sm:text-base">{order.totalAmount.toLocaleString('vi-VN')}₫</div>
                              <div className="flex items-center gap-1 text-xs sm:text-sm text-gray-500 justify-end">
                                <Package className="w-3 h-3" />
                                <span>{order.items?.length || 0} items</span>
                                {expandedOrders.has(order.orderId) ? 
                                  <ChevronDown className="w-3 h-3 sm:w-4 sm:h-4 ml-1" /> : 
                                  <ChevronRight className="w-3 h-3 sm:w-4 sm:h-4 ml-1" />
                                }
                              </div>
                            </div>
                          </div>
                          
                          {expandedOrders.has(order.orderId) && (
                            <div className="border-t bg-gray-50">
                              <div className="p-3">
                                <h5 className="font-medium mb-2 text-xs sm:text-sm">Chi tiết sản phẩm:</h5>
                                <div className="space-y-2">
                                  {order.items?.map((item, index) => (
                                    <div key={index} className="flex items-center justify-between p-2 bg-white rounded border text-xs sm:text-sm">
                                      <div className="flex-1 min-w-0 pr-2">
                                        <div className="font-medium truncate">{item.productName}</div>
                                        <div className="text-gray-600">
                                          {item.quantity} x {item.price.toLocaleString('vi-VN')}₫
                                        </div>
                                      </div>
                                      <div className="font-semibold flex-shrink-0">
                                        {item.totalPrice.toLocaleString('vi-VN')}₫
                                      </div>
                                    </div>
                                  ))}
                                </div>
                              </div>
                            </div>
                          )}
                        </div>
                      ))}
                      
                      {(!stat.orders || stat.orders.length === 0) && (
                        <div className="text-center py-4 text-gray-500">
                          <Package className="w-6 h-6 sm:w-8 sm:h-8 mx-auto mb-2 text-gray-300" />
                          <p className="text-sm">Chưa có đơn hàng nào</p>
                        </div>
                      )}
                    </div>
                  </div>
                )}
              </div>
            ))}
            
            {(!paymentStats?.paymentStats || paymentStats.paymentStats.length === 0) && (
              <div className="text-center py-8 text-gray-500">
                <TrendingUp className="w-8 h-8 sm:w-12 sm:h-12 mx-auto mb-3 text-gray-300" />
                <p className="text-sm sm:text-base">Chưa có dữ liệu thanh toán</p>
                <p className="text-xs sm:text-sm">Hãy thực hiện một số giao dịch để xem báo cáo</p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}