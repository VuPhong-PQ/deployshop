import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { apiRequest } from "@/lib/queryClient";
import { Banknote, CreditCard, QrCode, Smartphone, TrendingUp, Calendar, RefreshCw, ChevronDown, ChevronRight, Eye, Package, User, Clock, Download, Receipt, DollarSign, Euro } from "lucide-react";
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
  splitPaymentDetails?: string | null;
  splitAmount?: number | null;
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

interface PaymentReportProps {
  startDate?: string;
  endDate?: string;
}

export function PaymentReport({ startDate: propStartDate, endDate: propEndDate }: PaymentReportProps = {}) {
  // NgÃ y máº·c Ä‘á»‹nh: hÃ´m nay náº¿u khÃ´ng cÃ³ props
  const [localFromDate, setLocalFromDate] = useState(() => {
    const date = new Date();
    return date.toISOString().split('T')[0];
  });
  
  const [localToDate, setLocalToDate] = useState(() => {
    const date = new Date();
    return date.toISOString().split('T')[0];
  });

  // Sá»­ dá»¥ng props náº¿u cÃ³, khÃ´ng thÃ¬ dÃ¹ng state local
  const fromDate = propStartDate || localFromDate;
  const toDate = propEndDate || localToDate;
  const setFromDate = setLocalFromDate;
  const setToDate = setLocalToDate;

  const [expandedPaymentMethods, setExpandedPaymentMethods] = useState<Set<string>>(new Set());
  const [expandedOrders, setExpandedOrders] = useState<Set<number>>(new Set());

  const { data: paymentStats, isLoading, refetch } = useQuery<PaymentStatsData>({
    queryKey: ["/api/PaymentStats", fromDate, toDate],
    queryFn: async () => {
      // Normalize user input date (support dd/mm/yyyy or yyyy-mm-dd) -> yyyy-mm-dd
      const parseToISO = (input: string) => {
        if (!input) return input;
        if (/^\d{4}-\d{2}-\d{2}$/.test(input)) return input;
        if (/^\d{2}\/\d{2}\/\d{4}$/.test(input)) {
          const [d, m, y] = input.split('/');
          return `${y}-${m.padStart(2, '0')}-${d.padStart(2, '0')}`;
        }
        const dt = new Date(input);
        if (isNaN(dt.getTime())) return input;
        return dt.toISOString().split('T')[0];
      };

      // TÃ­nh ngÃ y káº¿t thÃºc + 1 Ä‘á»ƒ bao gá»“m toÃ n bá»™ ngÃ y Ä‘Æ°á»£c chá»n
      const parsedStart = parseToISO(fromDate);
      const parsedEnd = parseToISO(toDate);
      
      // Táº¡o Date object vÃ  cá»™ng 1 ngÃ y
      const endDateObj = new Date(parsedEnd + 'T12:00:00'); // DÃ¹ng 12:00 Ä‘á»ƒ trÃ¡nh váº¥n Ä‘á» timezone
      endDateObj.setDate(endDateObj.getDate() + 1);
      const apiEndPlusOne = endDateObj.toISOString().split('T')[0];

      const params = new URLSearchParams({
        fromDate: parsedStart,
        toDate: apiEndPlusOne,
      });
      const res = await apiRequest(`/api/PaymentStats?${params.toString()}`, { method: "GET" });
      return res;
    },
    // Tá»± Ä‘á»™ng refetch khi ngÃ y thay Ä‘á»•i, khÃ´ng cache
    staleTime: 0,
    gcTime: 0,
    refetchOnMount: true,
  });

  const getPaymentIcon = (methodId: string) => {
    switch (methodId) {
      case 'cash': return <Banknote className="w-5 h-5 text-green-600" />;
      case 'card': return <CreditCard className="w-5 h-5 text-blue-600" />;
      case 'qr': return <QrCode className="w-5 h-5 text-purple-600" />;
  case 'ewallet': return <Smartphone className="w-5 h-5 text-orange-600" />;
  case 'banktransfer': return <CreditCard className="w-5 h-5 text-indigo-600" />;
  case 'foreignusd': return <DollarSign className="w-5 h-5 text-emerald-600" />;
  case 'foreigneur': return <Euro className="w-5 h-5 text-yellow-600" />;
      default: return <Banknote className="w-5 h-5 text-gray-600" />;
    }
  };

  const getPaymentColor = (methodId: string) => {
    switch (methodId) {
      case 'cash': return 'bg-green-500';
      case 'card': return 'bg-blue-500';
      case 'qr': return 'bg-purple-500';
      case 'ewallet': return 'bg-orange-500';
  case 'banktransfer': return 'bg-indigo-500';
  case 'foreignusd': return 'bg-emerald-500';
  case 'foreigneur': return 'bg-yellow-500';
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
      alert('KhÃ´ng cÃ³ dá»¯ liá»‡u Ä‘á»ƒ xuáº¥t');
      return;
    }

    const workbook = XLSX.utils.book_new();

    // Sheet 1: Tá»•ng quan
    const overviewData = [
      ['BÃ¡o cÃ¡o HÃ¬nh thá»©c Thanh toÃ¡n'],
      [`Tá»« ngÃ y: ${fromDate} Ä‘áº¿n ${toDate}`],
      [`Tá»•ng doanh thu: ${paymentStats.totalRevenue?.toLocaleString('vi-VN')}â‚«`],
      [`Tá»•ng Ä‘Æ¡n hÃ ng: ${paymentStats.totalOrders}`],
      [''],
      ['HÃ¬nh thá»©c thanh toÃ¡n', 'Sá»‘ Ä‘Æ¡n hÃ ng', 'Doanh thu', 'Tá»· lá»‡ %']
    ];

    paymentStats.paymentStats.forEach(stat => {
      overviewData.push([
        stat.paymentMethod,
        stat.orderCount.toString(),
        `${stat.totalAmount.toLocaleString('vi-VN')}â‚«`,
        `${stat.percentage}%`
      ]);
    });

    const overviewWs = XLSX.utils.aoa_to_sheet(overviewData);
    XLSX.utils.book_append_sheet(workbook, overviewWs, 'Tá»•ng quan');

    // Sheet 2: Chi tiáº¿t tá»«ng Ä‘Æ¡n hÃ ng
    const detailData = [
      ['Chi tiáº¿t ÄÆ¡n hÃ ng theo HÃ¬nh thá»©c Thanh toÃ¡n'],
      [''],
      ['HÃ¬nh thá»©c thanh toÃ¡n', 'Sá»‘ Ä‘Æ¡n', 'MÃ£ Ä‘Æ¡n hÃ ng', 'KhÃ¡ch hÃ ng', 'Thá»i gian', 'Tá»•ng tiá»n', 'Sáº£n pháº©m', 'Sá»‘ lÆ°á»£ng', 'ÄÆ¡n giÃ¡', 'ThÃ nh tiá»n']
    ];

    paymentStats.paymentStats.forEach(stat => {
      if (stat.orders && stat.orders.length > 0) {
        stat.orders.forEach(order => {
          if (order.items && order.items.length > 0) {
            order.items.forEach((item, itemIndex) => {
              detailData.push([
                itemIndex === 0 ? stat.paymentMethod : '', // Chá»‰ hiá»‡n tÃªn phÆ°Æ¡ng thá»©c á»Ÿ dÃ²ng Ä‘áº§u
                itemIndex === 0 ? order.orderId.toString() : '',
                itemIndex === 0 ? (order.orderNumber || `ÄÆ¡n #${order.orderId}`) : '',
                itemIndex === 0 ? order.customerName : '',
                itemIndex === 0 ? formatDate(order.createdAt) : '',
                itemIndex === 0 ? `${order.totalAmount.toLocaleString('vi-VN')}â‚«` : '',
                item.productName,
                item.quantity.toString(),
                `${item.price.toLocaleString('vi-VN')}â‚«`,
                `${item.totalPrice.toLocaleString('vi-VN')}â‚«`
              ]);
            });
          } else {
            // Náº¿u Ä‘Æ¡n hÃ ng khÃ´ng cÃ³ items
            detailData.push([
              stat.paymentMethod,
              order.orderId.toString(),
              order.orderNumber || `ÄÆ¡n #${order.orderId}`,
              order.customerName,
              formatDate(order.createdAt),
              `${order.totalAmount.toLocaleString('vi-VN')}â‚«`,
              'KhÃ´ng cÃ³ sáº£n pháº©m',
              '',
              '',
              ''
            ]);
          }
        });
        // ThÃªm dÃ²ng trá»‘ng giá»¯a cÃ¡c phÆ°Æ¡ng thá»©c thanh toÃ¡n
        detailData.push(['', '', '', '', '', '', '', '', '', '']);
      }
    });

    const detailWs = XLSX.utils.aoa_to_sheet(detailData);
    XLSX.utils.book_append_sheet(workbook, detailWs, 'Chi tiáº¿t Ä‘Æ¡n hÃ ng');

    // Sheet 3: Xáº¿p háº¡ng phÆ°Æ¡ng thá»©c thanh toÃ¡n
    const rankingData = [
      ['Xáº¿p háº¡ng HÃ¬nh thá»©c Thanh toÃ¡n'],
      [''],
      ['Háº¡ng', 'HÃ¬nh thá»©c thanh toÃ¡n', 'Sá»‘ Ä‘Æ¡n hÃ ng', 'Doanh thu', 'Tá»· lá»‡ %']
    ];

    paymentStats.paymentStats.forEach((stat, index) => {
      rankingData.push([
        (index + 1).toString(),
        stat.paymentMethod,
        stat.orderCount.toString(),
        `${stat.totalAmount.toLocaleString('vi-VN')}â‚«`,
        `${stat.percentage}%`
      ]);
    });

    const rankingWs = XLSX.utils.aoa_to_sheet(rankingData);
    XLSX.utils.book_append_sheet(workbook, rankingWs, 'Xáº¿p háº¡ng');

    // Xuáº¥t file
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
      {/* Header vá»›i bá»™ lá»c ngÃ y */}
      <div className="bg-white rounded-lg border p-6 sm:p-8">
        <div className="space-y-6">
          <div className="text-center sm:text-left">
            <h2 className="text-xl sm:text-2xl font-bold text-gray-900 flex items-center justify-center sm:justify-start gap-2 mb-3">
              <TrendingUp className="w-5 h-5 sm:w-6 sm:h-6 text-blue-600" />
              BÃ¡o cÃ¡o HÃ¬nh thá»©c Thanh toÃ¡n
            </h2>
            <p className="text-gray-600 text-sm mb-4">
              Thá»‘ng kÃª doanh thu theo phÆ°Æ¡ng thá»©c thanh toÃ¡n
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
                  LÃ m má»›i
                </Button>
                <Button 
                  onClick={exportToExcel} 
                  size="sm" 
                  className="bg-green-600 hover:bg-green-700 text-white w-full sm:w-auto"
                >
                  <Download className="w-4 h-4 mr-1" />
                  Xuáº¥t Excel
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Tá»•ng quan */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="min-w-0 flex-1">
                <p className="text-sm text-gray-600 truncate">Tá»•ng doanh thu</p>
                <p className="text-lg sm:text-2xl font-bold text-green-600 truncate">
                  {paymentStats?.totalRevenue?.toLocaleString('vi-VN')}â‚«
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
                <p className="text-sm text-gray-600 truncate">Tá»•ng Ä‘Æ¡n hÃ ng</p>
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
                <p className="text-sm text-gray-600 truncate">PhÆ°Æ¡ng thá»©c phá»• biáº¿n</p>
                <p className="text-sm sm:text-lg font-semibold text-purple-600 truncate">
                  {paymentStats?.paymentStats?.[0]?.paymentMethod || "ChÆ°a cÃ³ dá»¯ liá»‡u"}
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
                <p className="text-sm text-gray-600 truncate">Sá»‘ phÆ°Æ¡ng thá»©c sá»­ dá»¥ng</p>
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

      {/* Biá»ƒu Ä‘á»“ vÃ  báº£ng xáº¿p háº¡ng */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Biá»ƒu Ä‘á»“ dáº¡ng cá»™t */}
        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-base sm:text-lg flex items-center gap-2 mb-2">
              <TrendingUp className="w-4 h-4 sm:w-5 sm:h-5 text-green-600" />
              Biá»ƒu Ä‘á»“ Doanh thu theo HÃ¬nh thá»©c
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
                      <div className="font-semibold text-sm sm:text-base">{stat.totalAmount.toLocaleString('vi-VN')}â‚«</div>
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
                    {stat.orderCount} Ä‘Æ¡n hÃ ng
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Báº£ng xáº¿p háº¡ng */}
        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-base sm:text-lg flex items-center gap-2 mb-2">
              <TrendingUp className="w-4 h-4 sm:w-5 sm:h-5 text-yellow-600" />
              Xáº¿p háº¡ng HÃ¬nh thá»©c Thanh toÃ¡n
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
                      {stat.orderCount} Ä‘Æ¡n hÃ ng â€¢ {stat.percentage}%
                    </div>
                  </div>
                  
                  <div className="text-right flex-shrink-0">
                    <div className="font-semibold text-sm sm:text-lg">
                      {stat.totalAmount.toLocaleString('vi-VN')}â‚«
                    </div>
                  </div>
                </div>
              ))}
              
              {(!paymentStats?.paymentStats || paymentStats.paymentStats.length === 0) && (
                <div className="text-center py-8 text-gray-500">
                  <TrendingUp className="w-8 h-8 sm:w-12 sm:h-12 mx-auto mb-3 text-gray-300" />
                  <p className="text-sm sm:text-base">ChÆ°a cÃ³ dá»¯ liá»‡u thanh toÃ¡n</p>
                  <p className="text-xs sm:text-sm">HÃ£y thá»±c hiá»‡n má»™t sá»‘ giao dá»‹ch Ä‘á»ƒ xem bÃ¡o cÃ¡o</p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Chi tiáº¿t Ä‘Æ¡n hÃ ng theo hÃ¬nh thá»©c thanh toÃ¡n */}
      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-base sm:text-lg flex items-center gap-2 mb-2">
            <Receipt className="w-4 h-4 sm:w-5 sm:h-5 text-blue-600" />
            Chi tiáº¿t ÄÆ¡n hÃ ng theo HÃ¬nh thá»©c Thanh toÃ¡n
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
                        {stat.orderCount} Ä‘Æ¡n hÃ ng â€¢ {stat.totalAmount.toLocaleString('vi-VN')}â‚«
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
                                <div className="font-medium text-sm sm:text-base truncate">{order.orderNumber || `ÄÆ¡n #${order.orderId}`}</div>
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
                              <div className="font-semibold text-sm sm:text-base">{order.totalAmount.toLocaleString('vi-VN')}â‚«</div>
                              {/* Split payment indicator */}
                              {order.splitPaymentDetails && (
                                <div className="text-xs text-orange-600 font-medium">âœ‚ï¸ Thanh toÃ¡n chia nhá»</div>
                              )}
                              {order.splitAmount && order.splitAmount !== order.totalAmount && (
                                <div className="text-xs text-blue-600 font-medium">
                                  Pháº§n nÃ y: {Number(order.splitAmount).toLocaleString('vi-VN')}â‚«
                                </div>
                              )}
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
                                {/* Split payment details */}
                                {order.splitPaymentDetails && (() => {
                                  try {
                                    const splits = typeof order.splitPaymentDetails === 'string' 
                                      ? JSON.parse(order.splitPaymentDetails) 
                                      : order.splitPaymentDetails;
                                    if (Array.isArray(splits) && splits.length > 0) {
                                      return (
                                        <div className="mb-3">
                                          <h5 className="font-medium mb-2 text-xs sm:text-sm flex items-center gap-1">
                                            <span>âœ‚ï¸</span> Chi tiáº¿t thanh toÃ¡n chia nhá»:
                                          </h5>
                                          <div className="space-y-1">
                                            {splits.map((sp: any, idx: number) => (
                                              <div key={idx} className="flex items-center justify-between p-2 bg-blue-50 rounded border border-blue-100 text-xs sm:text-sm">
                                                <div className="flex items-center gap-2">
                                                  <span className="font-medium">{sp.methodName}</span>
                                                </div>
                                                <div className="font-semibold text-blue-700">
                                                  {Number(sp.amount).toLocaleString('vi-VN')}â‚«
                                                </div>
                                              </div>
                                            ))}
                                          </div>
                                        </div>
                                      );
                                    }
                                  } catch (e) { /* ignore parse error */ }
                                  return null;
                                })()}
                                
                                <h5 className="font-medium mb-2 text-xs sm:text-sm">Chi tiáº¿t sáº£n pháº©m:</h5>
                                <div className="space-y-2">
                                  {order.items?.map((item, index) => (
                                    <div key={index} className="flex items-center justify-between p-2 bg-white rounded border text-xs sm:text-sm">
                                      <div className="flex-1 min-w-0 pr-2">
                                        <div className="font-medium truncate">{item.productName}</div>
                                        <div className="text-gray-600">
                                          {item.quantity} x {item.price.toLocaleString('vi-VN')}â‚«
                                        </div>
                                      </div>
                                      <div className="font-semibold flex-shrink-0">
                                        {item.totalPrice.toLocaleString('vi-VN')}â‚«
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
                          <p className="text-sm">ChÆ°a cÃ³ Ä‘Æ¡n hÃ ng nÃ o</p>
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
                <p className="text-sm sm:text-base">ChÆ°a cÃ³ dá»¯ liá»‡u thanh toÃ¡n</p>
                <p className="text-xs sm:text-sm">HÃ£y thá»±c hiá»‡n má»™t sá»‘ giao dá»‹ch Ä‘á»ƒ xem bÃ¡o cÃ¡o</p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
