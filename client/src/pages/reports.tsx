import { useState, useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { PaymentReport } from "@/components/reports/payment-report";
import { DiscountSummaryReport, DiscountOrdersResponse } from "@/types/discountReports";
import { 
  BarChart, 
  Bar, 
  XAxis, 
  YAxis, 
  CartesianGrid, 
  Tooltip, 
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  LineChart,
  Line
} from "recharts";
import { 
  TrendingUp, 
  TrendingDown,
  DollarSign, 
  ShoppingCart, 
  Users, 
  Package,
  Calendar,
  Download,
  Filter,
  BarChart3,
  PieChart as PieChartIcon,
  LineChart as LineChartIcon,
  RefreshCw,
  CreditCard,
  Eye
} from "lucide-react";
import * as XLSX from 'xlsx';

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884d8'];

export default function Reports() {
  const queryClient = useQueryClient();
  
  // Set default range để capture tất cả data hôm nay
  const today = new Date();
  const startOfToday = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  const endOfToday = new Date(today.getFullYear(), today.getMonth(), today.getDate() + 1);
  
  const todayString = startOfToday.toISOString().split('T')[0]; // 2025-10-09
  const tomorrowString = endOfToday.toISOString().split('T')[0]; // 2025-10-10
  
  const [dateRange, setDateRange] = useState({
    startDate: todayString,     // Từ hôm nay
    endDate: tomorrowString     // Đến ngày mai (để include hết hôm nay)
  });
  const [reportType, setReportType] = useState("summary");
  const [selectedStoreId, setSelectedStoreId] = useState<string>("all"); // Store filter state
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [isOrderDetailOpen, setIsOrderDetailOpen] = useState(false);

  // Auto refresh khi window focus (người dùng quay lại tab)
  useEffect(() => {
    const handleFocus = () => {
      console.log('Window focused, refreshing reports...'); // Debug log
      // Invalidate tất cả report queries để refresh data
      queryClient.invalidateQueries({ queryKey: ['/api/reports/sales-summary'] });
      queryClient.invalidateQueries({ queryKey: ['/api/reports/product-performance'] });
      queryClient.invalidateQueries({ queryKey: ['/api/reports/customer-analytics'] });
      queryClient.invalidateQueries({ queryKey: ['/api/reports/profit-analysis'] });
      queryClient.invalidateQueries({ queryKey: ['/api/discount-reports/summary'] });
    };

    const handleVisibilityChange = () => {
      if (!document.hidden) {
        handleFocus();
      }
    };

    // Listen for custom event từ sales page khi có order mới
    const handleNewOrder = () => {
      console.log('New order event received, refreshing reports...'); // Debug log
      queryClient.invalidateQueries({ queryKey: ['/api/reports/sales-summary'] });
      queryClient.invalidateQueries({ queryKey: ['/api/reports/product-performance'] });
      queryClient.invalidateQueries({ queryKey: ['/api/reports/customer-analytics'] });
      queryClient.invalidateQueries({ queryKey: ['/api/reports/profit-analysis'] });
      queryClient.invalidateQueries({ queryKey: ['/api/discount-reports/summary'] });
    };

    window.addEventListener('focus', handleFocus);
    document.addEventListener('visibilitychange', handleVisibilityChange);
    window.addEventListener('newOrderCreated', handleNewOrder);

    return () => {
      window.removeEventListener('focus', handleFocus);
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      window.removeEventListener('newOrderCreated', handleNewOrder);
    };
  }, [queryClient]);

  // Fetch available stores for filter
  const { data: stores = [] } = useQuery({
    queryKey: ["/api/storeswitch/my-stores"],
    queryFn: async () => {
      try {
        const res = await fetch("http://101.53.9.76:5273/api/storeswitch/my-stores", {
          headers: {
            "Username": "admin" // Tạm thời hardcode, sau này sẽ lấy từ auth context
          }
        });
        if (!res.ok) throw new Error('Failed to fetch stores');
        return res.json();
      } catch (error) {
        console.error("Error fetching stores:", error);
        return [];
      }
    },
    staleTime: 10 * 60 * 1000, // Cache for 10 minutes
  });

  // Fetch report data với store filter
  const storeParam = selectedStoreId !== "all" ? `&storeId=${selectedStoreId}` : "";
  
  const { data: salesSummary, isLoading: salesLoading } = useQuery({
    queryKey: ['/api/reports/sales-summary', dateRange.startDate, dateRange.endDate, selectedStoreId],
    queryFn: async () => {
      const response = await fetch(`http://101.53.9.76:5273/api/reports/sales-summary?startDate=${dateRange.startDate}&endDate=${dateRange.endDate}${storeParam}`);
      if (!response.ok) {
        throw new Error('Failed to fetch sales summary');
      }
      return response.json();
    },
    enabled: !!dateRange.startDate && !!dateRange.endDate,
    refetchInterval: 30000, // Refetch mỗi 30 giây
  });

  const { data: productPerformance, isLoading: productLoading } = useQuery({
    queryKey: ['/api/reports/product-performance', dateRange.startDate, dateRange.endDate, selectedStoreId],
    queryFn: async () => {
      const response = await fetch(`http://101.53.9.76:5273/api/reports/product-performance?startDate=${dateRange.startDate}&endDate=${dateRange.endDate}${storeParam}`);
      if (!response.ok) {
        throw new Error('Failed to fetch product performance');
      }
      return response.json();
    },
    enabled: !!dateRange.startDate && !!dateRange.endDate,
    refetchInterval: 30000, // Refetch mỗi 30 giây
  });

  const { data: customerAnalytics, isLoading: customerLoading } = useQuery({
    queryKey: ['/api/reports/customer-analytics', dateRange.startDate, dateRange.endDate, selectedStoreId],
    queryFn: async () => {
      const response = await fetch(`http://101.53.9.76:5273/api/reports/customer-analytics?startDate=${dateRange.startDate}&endDate=${dateRange.endDate}${storeParam}`);
      if (!response.ok) {
        throw new Error('Failed to fetch customer analytics');
      }
      return response.json();
    },
    enabled: !!dateRange.startDate && !!dateRange.endDate,
    refetchInterval: 30000, // Refetch mỗi 30 giây
  });

  const { data: profitAnalysis, isLoading: profitLoading } = useQuery({
    queryKey: ['/api/reports/profit-analysis', dateRange.startDate, dateRange.endDate, selectedStoreId],
    queryFn: async () => {
      const response = await fetch(`http://101.53.9.76:5273/api/reports/profit-analysis?startDate=${dateRange.startDate}&endDate=${dateRange.endDate}${storeParam}`);
      if (!response.ok) {
        throw new Error('Failed to fetch profit analysis');
      }
      return response.json();
    },
    enabled: !!dateRange.startDate && !!dateRange.endDate,
    refetchInterval: 30000, // Refetch mỗi 30 giây
  });

  const { data: discountReports, isLoading: discountLoading } = useQuery<DiscountSummaryReport>({
    queryKey: ['/api/discount-reports/summary', dateRange.startDate, dateRange.endDate],
    queryFn: async (): Promise<DiscountSummaryReport> => {
      let url = `http://101.53.9.76:5273/api/discount-reports/summary`;
      if (dateRange.startDate && dateRange.endDate) {
        url += `?startDate=${dateRange.startDate}&endDate=${dateRange.endDate}`;
      }
      
      console.log('Fetching discount summary from:', url);
      const response = await fetch(url);
      if (!response.ok) {
        throw new Error('Failed to fetch discount reports');
      }
      const result = await response.json();
      console.log('Discount Summary result:', result);
      return result;
    },
    enabled: !!dateRange.startDate && !!dateRange.endDate, // Re-enabled!
    refetchInterval: 30000, // Refetch mỗi 30 giây
  });

    const { data: discountOrders, isLoading: discountOrdersLoading } = useQuery<any>({
    queryKey: ['/api/discount-reports/orders', dateRange.startDate, dateRange.endDate],
    queryFn: async (): Promise<any> => {
      let url = `http://101.53.9.76:5273/api/discount-reports/orders`;
      if (dateRange.startDate && dateRange.endDate) {
        url += `?startDate=${dateRange.startDate}&endDate=${dateRange.endDate}`;
      }
      
      console.log('Fetching discount orders from:', url);
      const response = await fetch(url);
      if (!response.ok) {
        throw new Error('Failed to fetch discount orders');
      }
      const result = await response.json();
      console.log('Discount Orders result:', result);
      console.log('Discount Orders structure:', {
        isArray: Array.isArray(result),
        hasOrders: !!result.orders,
        firstOrder: result.orders?.[0] || result[0],
        totalCount: result.orders?.length || result.length
      });
      return result; // Return full response object
    },
    enabled: !!dateRange.startDate && !!dateRange.endDate, // Re-enabled!
    refetchInterval: 30000, // Refetch mỗi 30 giây
  });

  // Query for detailed order information
  const { data: orderDetail, isLoading: orderDetailLoading } = useQuery({
    queryKey: ['/api/orders', selectedOrderId, 'detail'],
    queryFn: async () => {
      if (!selectedOrderId) return null;
      
      console.log('Fetching order detail for ID:', selectedOrderId);
      const response = await fetch(`http://101.53.9.76:5273/api/orders/${selectedOrderId}`);
      if (!response.ok) {
        console.error('Failed to fetch order detail:', response.status, response.statusText);
        throw new Error('Failed to fetch order detail');
      }
      const result = await response.json();
      console.log('Order detail response:', result);
      return result;
    },
    enabled: !!selectedOrderId && isOrderDetailOpen,
  });

  const isLoading = salesLoading || productLoading || customerLoading || profitLoading || discountLoading || discountOrdersLoading;

  // Chart data formatting
  const chartData = productPerformance?.topProducts?.map(product => ({
    name: product.name,
    revenue: parseFloat(product.revenue.replace(/[₫.,]/g, '')),
    sold: product.totalSold
  })) || [];

  const pieData = productPerformance?.topProducts?.slice(0, 4).map((product, index) => ({
    name: product.name,
    value: parseFloat(product.revenue.replace(/[₫.,]/g, '')),
    color: COLORS[index]
  })) || [];

  const profitTrendData = profitAnalysis?.monthlyTrend?.map(item => ({
    month: item.month,
    profit: parseFloat(item.profit.replace(/[₫.,]/g, '')),
    margin: parseFloat(item.margin.replace('%', ''))
  })) || [];

  // Handle view order detail
  const handleViewOrderDetail = (orderId: number) => {
    console.log('Viewing order detail for:', orderId);
    console.log('Available discount orders:', discountOrders);
    const orderWithDiscounts = discountOrders?.orders?.find((o: any) => o.orderId === orderId);
    console.log('Found order with discounts:', orderWithDiscounts);
    setSelectedOrderId(orderId);
    setIsOrderDetailOpen(true);
  };

  // Export to Excel function
  const exportToExcel = () => {
    if (!salesSummary && !productPerformance && !customerAnalytics && !profitAnalysis) {
      alert('Chưa có dữ liệu để xuất báo cáo. Vui lòng chờ dữ liệu tải xong.');
      return;
    }

    const workbook = XLSX.utils.book_new();
    
    // Sheet 1: Tổng quan doanh thu
    const summaryData = [
      ['BÁO CÁO TỔNG QUAN DOANH THU'],
      [`Từ ngày: ${dateRange.startDate} đến ngày: ${dateRange.endDate}`],
      [`Thời gian xuất: ${new Date().toLocaleString('vi-VN')}`],
      [''],
      ['Chỉ số', 'Giá trị', 'So với tháng trước'],
      ['Tổng doanh thu', salesSummary?.totalRevenue || '0₫', salesSummary?.revenueChange || '0%'],
      ['Tổng đơn hàng', salesSummary?.totalOrders || '0', salesSummary?.ordersChange || '0%'],
      ['Tổng khách hàng', salesSummary?.totalCustomers || '0', salesSummary?.customersChange || '0%'],
      ['Sản phẩm bán ra', salesSummary?.totalProductsSold || '0', salesSummary?.productsChange || '0%']
    ];
    
    const summaryWs = XLSX.utils.aoa_to_sheet(summaryData);
    XLSX.utils.book_append_sheet(workbook, summaryWs, 'Tổng quan');

    // Sheet 2: Top sản phẩm
    const productData = [
      ['TOP SẢN PHẨM BÁN CHẠY'],
      [`Từ ngày: ${dateRange.startDate} đến ngày: ${dateRange.endDate}`],
      [''],
      ['STT', 'Tên sản phẩm', 'Doanh thu', 'Số lượng bán', 'Lợi nhuận']
    ];
    
    if (productPerformance?.topProducts?.length > 0) {
      productPerformance.topProducts.forEach((product, index) => {
        productData.push([
          (index + 1).toString(),
          product.name || 'N/A',
          product.revenue || '0₫',
          product.totalSold?.toString() || '0',
          product.profit || '0₫'
        ]);
      });
    } else {
      productData.push(['', 'Chưa có dữ liệu sản phẩm', '', '', '']);
    }
    
    const productWs = XLSX.utils.aoa_to_sheet(productData);
    XLSX.utils.book_append_sheet(workbook, productWs, 'Sản phẩm');

    // Sheet 3: Khách hàng
    const customerData = [
      ['PHÂN TÍCH KHÁCH HÀNG'],
      [`Từ ngày: ${dateRange.startDate} đến ngày: ${dateRange.endDate}`],
      [''],
      ['Chỉ số', 'Giá trị'],
      ['Khách hàng mới', customerAnalytics?.newCustomers?.toString() || '0'],
      ['Khách hàng quay lại', customerAnalytics?.returningCustomers?.toString() || '0'],
      ['Tỷ lệ giữ chân', customerAnalytics?.retentionRate || '0%'],
      ['Giá trị trung bình đơn hàng', customerAnalytics?.averageOrderValue || '0₫']
    ];
    
    const customerWs = XLSX.utils.aoa_to_sheet(customerData);
    XLSX.utils.book_append_sheet(workbook, customerWs, 'Khách hàng');

    // Sheet 4: Lợi nhuận
    const profitData = [
      ['PHÂN TÍCH LỢI NHUẬN'],
      [`Từ ngày: ${dateRange.startDate} đến ngày: ${dateRange.endDate}`],
      [''],
      ['Tháng', 'Lợi nhuận', 'Tỷ lệ lợi nhuận']
    ];
    
    if (profitAnalysis?.monthlyTrend?.length > 0) {
      profitAnalysis.monthlyTrend.forEach(item => {
        profitData.push([
          item.month || 'N/A',
          item.profit || '0₫',
          item.margin || '0%'
        ]);
      });
    } else {
      profitData.push(['', 'Chưa có dữ liệu lợi nhuận', '', '']);
    }
    
    const profitWs = XLSX.utils.aoa_to_sheet(profitData);
    XLSX.utils.book_append_sheet(workbook, profitWs, 'Lợi nhuận');

    // Xuất file
    const fileName = `Bao_cao_tong_quan_${dateRange.startDate.replace(/-/g, '')}_den_${dateRange.endDate.replace(/-/g, '')}.xlsx`;
    XLSX.writeFile(workbook, fileName);
    
    // Thông báo thành công
    alert(`Đã xuất báo cáo thành công! File: ${fileName}`);
  };

  return (
    <AppLayout title="Báo cáo & Phân tích">
      <div className="space-y-6" data-testid="reports-page">
        {/* Filters */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Filter className="w-5 h-5" />
              Bộ lọc báo cáo
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
              <div className="space-y-2">
                <Label>Từ ngày</Label>
                <Input
                  type="date"
                  value={dateRange.startDate}
                  onChange={(e) => setDateRange(prev => ({ ...prev, startDate: e.target.value }))}
                  data-testid="input-start-date"
                />
              </div>
              <div className="space-y-2">
                <Label>Đến ngày</Label>
                <Input
                  type="date"
                  value={dateRange.endDate}
                  onChange={(e) => setDateRange(prev => ({ ...prev, endDate: e.target.value }))}
                  data-testid="input-end-date"
                />
              </div>
              <div className="space-y-2">
                <Label>Cửa hàng</Label>
                <Select value={selectedStoreId} onValueChange={setSelectedStoreId}>
                  <SelectTrigger data-testid="select-store">
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
              </div>
              <div className="space-y-2">
                <Label>Loại báo cáo</Label>
                <Select value={reportType} onValueChange={setReportType}>
                  <SelectTrigger data-testid="select-report-type">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="summary">Tổng quan</SelectItem>
                    <SelectItem value="detailed">Chi tiết</SelectItem>
                    <SelectItem value="comparison">So sánh</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-end gap-2">
                <Button 
                  variant="outline" 
                  onClick={() => {
                    console.log('Manual refresh button clicked'); // Debug log
                    queryClient.invalidateQueries({ queryKey: ['/api/reports/sales-summary'] });
                    queryClient.invalidateQueries({ queryKey: ['/api/reports/product-performance'] });
                    queryClient.invalidateQueries({ queryKey: ['/api/reports/customer-analytics'] });
                    queryClient.invalidateQueries({ queryKey: ['/api/reports/profit-analysis'] });
                  }}
                  className="flex-1"
                >
                  <RefreshCw className="w-4 h-4 mr-2" />
                  Refresh
                </Button>
                <Button className="flex-1" data-testid="button-export" onClick={exportToExcel} disabled={isLoading}>
                  <Download className="w-4 h-4 mr-2" />
                  {isLoading ? 'Đang tải...' : 'Xuất báo cáo'}
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>

        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {[...Array(4)].map((_, i) => (
              <Card key={i} className="animate-pulse">
                <CardContent className="p-6">
                  <div className="h-8 bg-gray-200 rounded mb-4"></div>
                  <div className="h-4 bg-gray-200 rounded"></div>
                </CardContent>
              </Card>
            ))}
          </div>
        ) : (
          <Tabs defaultValue="overview" className="space-y-6">
            <TabsList className="grid w-full grid-cols-2 grid-rows-3 sm:grid-cols-3 sm:grid-rows-2 lg:grid-cols-6 lg:grid-rows-1 gap-1 h-auto">
              <TabsTrigger value="overview" className="text-xs sm:text-sm">Tổng quan</TabsTrigger>
              <TabsTrigger value="products" className="text-xs sm:text-sm">Sản phẩm</TabsTrigger>
              <TabsTrigger value="payment" className="text-xs sm:text-sm">Thanh toán</TabsTrigger>
              <TabsTrigger value="customers" className="text-xs sm:text-sm">Khách hàng</TabsTrigger>
              <TabsTrigger value="profit" className="text-xs sm:text-sm">Lợi nhuận</TabsTrigger>
              <TabsTrigger value="discounts" className="text-xs sm:text-sm">Giảm giá</TabsTrigger>
            </TabsList>

            <TabsContent value="overview" className="space-y-6">
              {/* Summary Cards */}
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <Card data-testid="card-total-revenue">
                  <CardContent className="p-6">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="text-sm font-medium text-muted-foreground">Tổng doanh thu</p>
                        <p className="text-2xl font-bold">{salesSummary?.totalRevenue || "0₫"}</p>
                      </div>
                      <DollarSign className="h-8 w-8 text-green-600" />
                    </div>
                    <div className="flex items-center mt-2">
                      <TrendingUp className="h-4 w-4 text-green-600 mr-1" />
                      <span className="text-xs text-green-600">+12.5% so với tháng trước</span>
                    </div>
                  </CardContent>
                </Card>

                <Card data-testid="card-total-orders">
                  <CardContent className="p-6">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="text-sm font-medium text-muted-foreground">Tổng đơn hàng</p>
                        <p className="text-2xl font-bold">{salesSummary?.totalOrders || 0}</p>
                      </div>
                      <ShoppingCart className="h-8 w-8 text-blue-600" />
                    </div>
                    <div className="flex items-center mt-2">
                      <TrendingUp className="h-4 w-4 text-green-600 mr-1" />
                      <span className="text-xs text-green-600">+8.2% so với tháng trước</span>
                    </div>
                  </CardContent>
                </Card>

                <Card data-testid="card-total-customers">
                  <CardContent className="p-6">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="text-sm font-medium text-muted-foreground">Tổng khách hàng</p>
                        <p className="text-2xl font-bold">{customerAnalytics?.totalCustomers || 0}</p>
                      </div>
                      <Users className="h-8 w-8 text-purple-600" />
                    </div>
                    <div className="flex items-center mt-2">
                      <TrendingUp className="h-4 w-4 text-green-600 mr-1" />
                      <span className="text-xs text-green-600">+{customerAnalytics?.newCustomers || 0} khách mới</span>
                    </div>
                  </CardContent>
                </Card>

                <Card data-testid="card-products-sold">
                  <CardContent className="p-6">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="text-sm font-medium text-muted-foreground">Sản phẩm bán ra</p>
                        <p className="text-2xl font-bold">{productPerformance?.totalProductsSold || 0}</p>
                      </div>
                      <Package className="h-8 w-8 text-orange-600" />
                    </div>
                    <div className="flex items-center mt-2">
                      <span className="text-xs text-muted-foreground">Phổ biến: {productPerformance?.mostPopularProduct || "N/A"}</span>
                    </div>
                  </CardContent>
                </Card>
              </div>

              {/* Revenue Chart */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <BarChart3 className="w-5 h-5" />
                    Doanh thu theo sản phẩm
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <BarChart data={chartData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="name" />
                      <YAxis />
                      <Tooltip formatter={(value) => [value.toLocaleString('vi-VN') + '₫', 'Doanh thu']} />
                      <Bar dataKey="revenue" fill="#8884d8" />
                    </BarChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="products" className="space-y-6">
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Product Performance Table */}
                <Card>
                  <CardHeader>
                    <CardTitle>Top sản phẩm bán chạy</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-4">
                      {productPerformance?.topProducts?.map((product, index) => (
                        <div key={product.name} className="flex items-center justify-between p-3 border rounded-lg">
                          <div className="flex items-center gap-3">
                            <Badge variant="outline">{index + 1}</Badge>
                            <div>
                              <p className="font-medium">{product.name}</p>
                              <p className="text-sm text-muted-foreground">Đã bán: {product.totalSold}</p>
                            </div>
                          </div>
                          <div className="text-right">
                            <p className="font-medium">{product.revenue}</p>
                            <p className="text-sm text-green-600">Lợi nhuận: {product.profit}</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>

                {/* Product Revenue Pie Chart */}
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <PieChartIcon className="w-5 h-5" />
                      Phân bố doanh thu
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ResponsiveContainer width="100%" height={300}>
                      <PieChart>
                        <Pie
                          data={pieData}
                          cx="50%"
                          cy="50%"
                          outerRadius={80}
                          fill="#8884d8"
                          dataKey="value"
                        >
                          {pieData.map((entry, index) => (
                            <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                          ))}
                        </Pie>
                        <Tooltip formatter={(value) => [value.toLocaleString('vi-VN') + '₫', 'Doanh thu']} />
                      </PieChart>
                    </ResponsiveContainer>
                    <div className="flex flex-wrap gap-2 mt-4">
                      {pieData.map((entry, index) => (
                        <div key={entry.name} className="flex items-center gap-2">
                          <div 
                            className="w-3 h-3 rounded-full" 
                            style={{ backgroundColor: COLORS[index % COLORS.length] }}
                          ></div>
                          <span className="text-xs">{entry.name}</span>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              </div>
            </TabsContent>

            <TabsContent value="payment" className="space-y-6">
              <PaymentReport />
            </TabsContent>

            <TabsContent value="customers" className="space-y-6">
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Customer Stats */}
                <Card>
                  <CardHeader>
                    <CardTitle>Thống kê khách hàng</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="flex justify-between items-center">
                      <span>Tổng khách hàng:</span>
                      <Badge variant="outline">{customerAnalytics?.totalCustomers || 0}</Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span>Khách mua hàng trong kỳ:</span>
                      <Badge className="bg-purple-100 text-purple-800">{customerAnalytics?.activeCustomers || 0}</Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span>Tổng đơn hàng trong kỳ:</span>
                      <Badge className="bg-orange-100 text-orange-800">{customerAnalytics?.totalOrdersInPeriod || 0}</Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span>Khách hàng mới:</span>
                      <Badge className="bg-green-100 text-green-800">{customerAnalytics?.newCustomers || 0}</Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span>Khách hàng quay lại:</span>
                      <Badge className="bg-blue-100 text-blue-800">{customerAnalytics?.returningCustomers || 0}</Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span>Trung bình đơn/khách:</span>
                      <Badge variant="outline">{customerAnalytics?.averageOrdersPerCustomer || '0.0'}</Badge>
                    </div>
                  </CardContent>
                </Card>

                {/* Top Customers */}
                <Card>
                  <CardHeader>
                    <CardTitle>Khách hàng VIP</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {customerAnalytics?.topCustomers?.map((customer, index) => (
                        <div key={customer.name} className="flex items-center justify-between p-3 border rounded-lg">
                          <div className="flex items-center gap-3">
                            <Badge variant="outline">{index + 1}</Badge>
                            <div>
                              <p className="font-medium">{customer.name}</p>
                              <p className="text-sm text-muted-foreground">{customer.orders} đơn hàng</p>
                            </div>
                          </div>
                          <div className="text-right">
                            <p className="font-medium text-green-600">{customer.totalSpent}</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              </div>
            </TabsContent>

            <TabsContent value="profit" className="space-y-6">
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Profit Summary */}
                <Card>
                  <CardHeader>
                    <CardTitle>Tổng quan lợi nhuận</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="space-y-2">
                      {/* Doanh thu và thuế */}
                      <div className="bg-blue-50 p-3 rounded-lg space-y-2">
                        <h4 className="font-medium text-blue-800">Doanh thu & Thuế</h4>
                        <div className="flex justify-between text-sm">
                          <span>Doanh thu (bao gồm thuế):</span>
                          <span className="font-medium">{profitAnalysis?.totalRevenueIncludingTax}</span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span>Thuế VAT ({profitAnalysis?.vatRate}):</span>
                          <span className="font-medium text-orange-600">{profitAnalysis?.totalTax}</span>
                        </div>
                        <div className="flex justify-between text-sm border-t pt-2">
                          <span>Doanh thu (chưa thuế):</span>
                          <span className="font-medium">{profitAnalysis?.totalRevenueExcludingTax}</span>
                        </div>
                      </div>
                      
                      {/* Chi phí */}
                      <div className="bg-gray-50 p-3 rounded-lg space-y-2">
                        <h4 className="font-medium text-gray-800">Chi phí</h4>
                        <div className="flex justify-between text-sm">
                          <span>Chi phí hàng bán:</span>
                          <span className="font-medium">{profitAnalysis?.costOfGoodsSold}</span>
                        </div>
                      </div>
                      
                      {/* Lợi nhuận */}
                      <div className="bg-green-50 p-3 rounded-lg space-y-2">
                        <h4 className="font-medium text-green-800">Lợi nhuận</h4>
                        <div className="flex justify-between text-sm">
                          <span>Lợi nhuận trước thuế:</span>
                          <span className="font-medium text-green-600">{profitAnalysis?.profitBeforeTax}</span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span>Lợi nhuận sau thuế:</span>
                          <span className="font-medium text-green-600">{profitAnalysis?.profitAfterTax}</span>
                        </div>
                        <div className="flex justify-between text-sm border-t pt-2">
                          <span>Tỷ suất lợi nhuận:</span>
                          <Badge className="bg-green-100 text-green-800">{profitAnalysis?.profitMargin}</Badge>
                        </div>
                      </div>
                      
                      {/* Lỗ (nếu có) */}
                      {profitAnalysis?.totalLoss && profitAnalysis.totalLoss !== "0₫" && (
                        <div className="bg-red-50 p-3 rounded-lg">
                          <h4 className="font-medium text-red-800">Cảnh báo</h4>
                          <div className="flex justify-between text-sm">
                            <span>Tổng lỗ (bán giá thấp hơn giá vốn):</span>
                            <span className="font-medium text-red-600">{profitAnalysis?.totalLoss}</span>
                          </div>
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>

                {/* Profit Trend */}
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <LineChartIcon className="w-5 h-5" />
                      Xu hướng lợi nhuận
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ResponsiveContainer width="100%" height={250}>
                      <LineChart data={profitTrendData}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="month" />
                        <YAxis />
                        <Tooltip formatter={(value) => [value.toLocaleString('vi-VN') + '₫', 'Lợi nhuận']} />
                        <Line type="monotone" dataKey="profit" stroke="#8884d8" strokeWidth={2} />
                      </LineChart>
                    </ResponsiveContainer>
                  </CardContent>
                </Card>
              </div>

              {/* Top Profitable Products */}
              <Card>
                <CardHeader>
                  <CardTitle>Sản phẩm sinh lời cao nhất</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    {profitAnalysis?.topProfitableProducts?.map((product, index) => (
                      <div key={product.name} className="flex items-center justify-between p-4 border rounded-lg bg-gradient-to-r from-green-50 to-emerald-50">
                        <div className="flex items-center gap-3">
                          <Badge variant="outline" className="bg-green-100 text-green-800">#{index + 1}</Badge>
                          <div>
                            <p className="font-medium text-gray-900">{product.name}</p>
                            <div className="text-sm text-muted-foreground space-y-1">
                              <p>Đã bán: <span className="font-medium">{product.totalSold}</span> | Tỷ suất: <span className="font-medium text-green-600">{product.margin}</span></p>
                              <p>Giá nhập: <span className="text-red-600">{product.costPrice}</span> → Giá bán: <span className="text-blue-600">{product.sellPrice}</span></p>
                            </div>
                          </div>
                        </div>
                        <div className="text-right">
                          <p className="text-lg font-bold text-green-600">{product.profit}</p>
                          <p className="text-sm text-gray-500">Lợi nhuận/sp: {product.profitPerUnit}</p>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="discounts" className="space-y-6">
              <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Tổng giảm giá</CardTitle>
                    <DollarSign className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-red-600">
                      {discountReports?.totalDiscountAmount?.toLocaleString('vi-VN') || '0'}₫
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {discountReports?.totalDiscountApplications || 0} lần áp dụng
                    </p>
                  </CardContent>
                </Card>
                
                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Giảm giá theo %</CardTitle>
                    <TrendingDown className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">
                      {(() => {
                        const percentDiscount = discountReports?.topDiscounts?.find(d => d.discountName?.includes('%')) || 
                                              discountReports?.topDiscounts?.find(d => d.discountId === 2);
                        return (percentDiscount?.totalAmount || 0).toLocaleString('vi-VN');
                      })()}₫
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {(() => {
                        const percentDiscount = discountReports?.topDiscounts?.find(d => d.discountName?.includes('%')) || 
                                              discountReports?.topDiscounts?.find(d => d.discountId === 2);
                        return percentDiscount?.usageCount || 0;
                      })()} lần
                    </p>
                  </CardContent>
                </Card>
                
                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Giảm giá cố định</CardTitle>
                    <Package className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">
                      {(() => {
                        const fixedDiscount = discountReports?.topDiscounts?.find(d => d.discountName?.includes('cố định')) || 
                                            discountReports?.topDiscounts?.find(d => d.discountName?.includes('bill')) ||
                                            discountReports?.topDiscounts?.find(d => d.discountId === 3);
                        return (fixedDiscount?.totalAmount || 0).toLocaleString('vi-VN');
                      })()}₫
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {(() => {
                        const fixedDiscount = discountReports?.topDiscounts?.find(d => d.discountName?.includes('cố định')) || 
                                            discountReports?.topDiscounts?.find(d => d.discountName?.includes('bill')) ||
                                            discountReports?.topDiscounts?.find(d => d.discountId === 3);
                        return fixedDiscount?.usageCount || 0;
                      })()} lần
                    </p>
                  </CardContent>
                </Card>
                
                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Giảm giá TB/đơn</CardTitle>
                    <BarChart3 className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">
                      {discountReports?.averageDiscountPerOrder?.toLocaleString('vi-VN') || '0'}₫
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Trung bình mỗi đơn
                    </p>
                  </CardContent>
                </Card>
              </div>

              {/* Discount Charts */}
              <div className="grid gap-6 md:grid-cols-2">
                <Card>
                  <CardHeader>
                    <CardTitle>Biểu đồ giảm giá theo loại</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ResponsiveContainer width="100%" height={300}>
                      <PieChart>
                        <Pie
                          data={discountReports?.topDiscounts?.map(discount => ({
                            name: discount.discountName,
                            value: discount.totalAmount
                          })) || []}
                          cx="50%"
                          cy="50%"
                          labelLine={false}
                          label={({ name, value }) => `${name}: ${Number(value).toLocaleString('vi-VN')}₫`}
                          outerRadius={80}
                          fill="#8884d8"
                          dataKey="value"
                        >
                          {(discountReports?.topDiscounts || []).map((entry, index) => (
                            <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                          ))}
                        </Pie>
                        <Tooltip formatter={(value) => `${Number(value).toLocaleString('vi-VN')}₫`} />
                      </PieChart>
                    </ResponsiveContainer>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle>Giảm giá theo ngày</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ResponsiveContainer width="100%" height={300}>
                      <LineChart data={discountReports?.dailyDiscounts || []}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="date" />
                        <YAxis />
                        <Tooltip formatter={(value) => `${Number(value).toLocaleString('vi-VN')}₫`} />
                        <Line type="monotone" dataKey="totalAmount" stroke="#8884d8" />
                      </LineChart>
                    </ResponsiveContainer>
                  </CardContent>
                </Card>
              </div>

              {/* Discount Orders */}
              <Card className="discount-orders">
                <CardHeader>
                  <CardTitle className="font-roboto">Chi tiết các đơn hàng được giảm giá</CardTitle>
                  <p className="text-sm text-muted-foreground font-roboto">
                    Tổng cộng: {discountOrders?.totalCount || discountOrders?.orders?.length || 0} đơn hàng
                  </p>
                </CardHeader>
                <CardContent className="font-roboto">
                  <div className="space-y-4">
                    {(discountOrders?.orders || []).map((order: any) => (
                      <div 
                        key={order.orderId} 
                        className="flex items-center justify-between p-4 border rounded-lg hover:bg-gray-50 cursor-pointer font-roboto transition-colors"
                        onClick={() => handleViewOrderDetail(order.orderId)}
                      >
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <Badge variant="outline" className="font-roboto">#{order.orderNumber}</Badge>
                            <span className="font-medium font-roboto">{order.customerName}</span>
                          </div>
                          <p className="text-sm text-muted-foreground mt-1 font-roboto">
                            {new Date(order.orderDate).toLocaleDateString('vi-VN')} • {order.discountCount} loại giảm giá
                          </p>
                          <div className="flex gap-2 mt-2">
                            {order.discountDetails.map((discount, idx) => (
                              <Badge key={idx} variant="secondary" className="text-xs font-roboto">
                                {discount.discountName}: -{discount.discountAmount.toLocaleString('vi-VN')}₫
                              </Badge>
                            ))}
                          </div>
                        </div>
                        <div className="text-right font-roboto">
                          <p className="text-lg font-bold text-green-600 font-roboto">
                            {order.orderTotal.toLocaleString('vi-VN')}₫
                          </p>
                          <p className="text-sm text-red-600 font-medium font-roboto">
                            Giảm: -{order.totalDiscountAmount.toLocaleString('vi-VN')}₫
                          </p>
                          <p className="text-xs text-gray-500 font-roboto">
                            {order.paymentMethod} • {order.paymentStatus}
                          </p>
                          <Button 
                            variant="outline" 
                            size="sm" 
                            className="mt-2"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleViewOrderDetail(order.orderId);
                            }}
                          >
                            <Eye className="w-4 h-4 mr-1" />
                            Xem chi tiết
                          </Button>
                        </div>
                      </div>
                      ))}
                    {(!discountOrders?.orders || discountOrders.orders.length === 0) && (
                      <div className="text-center py-8 text-muted-foreground font-roboto">
                        Không có đơn hàng nào có giảm giá trong khoảng thời gian này
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        )}
      </div>

      {/* Order Detail Modal */}
      <Dialog open={isOrderDetailOpen} onOpenChange={setIsOrderDetailOpen}>
        <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Chi tiết đơn hàng #{orderDetail?.orderNumber || selectedOrderId}</DialogTitle>
          </DialogHeader>
          
          {orderDetailLoading ? (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
              <span className="ml-2">Đang tải...</span>
            </div>
          ) : orderDetail ? (
            <div className="space-y-6">
              {/* Order Info */}
              <div className="grid grid-cols-2 gap-4 p-4 bg-gray-50 rounded-lg">
                <div>
                  <h3 className="font-semibold text-sm text-gray-600">Thông tin đơn hàng</h3>
                  <p><strong>Số đơn:</strong> #{orderDetail.orderNumber}</p>
                  <p><strong>Khách hàng:</strong> {orderDetail.customerName || orderDetail.customer?.hoTen || 'Khách lẻ'}</p>
                  <p><strong>Ngày:</strong> {new Date(orderDetail.createdAt).toLocaleString('vi-VN')}</p>
                  <p><strong>Trạng thái:</strong> <Badge variant="outline">{orderDetail.status}</Badge></p>
                  {orderDetail.storeName && <p><strong>Cửa hàng:</strong> {orderDetail.storeName}</p>}
                </div>
                <div>
                  <h3 className="font-semibold text-sm text-gray-600">Thanh toán</h3>
                  <p><strong>Phương thức:</strong> {orderDetail.paymentMethod || 'Chưa chọn'}</p>
                  <p><strong>Trạng thái:</strong> <Badge variant={orderDetail.paymentStatus === 'paid' ? 'default' : 'secondary'}>{orderDetail.paymentStatus}</Badge></p>
                  <p><strong>Tổng tiền:</strong> <span className="text-green-600 font-bold">{orderDetail.totalAmount?.toLocaleString('vi-VN')}₫</span></p>
                  {orderDetail.discountAmount > 0 && (
                    <p><strong>Giảm giá:</strong> <span className="text-red-600 font-bold">-{orderDetail.discountAmount?.toLocaleString('vi-VN')}₫</span></p>
                  )}
                </div>
              </div>

              {/* Order Items */}
              <div>
                <h3 className="font-semibold mb-3">Sản phẩm trong đơn hàng</h3>
                <div className="border rounded-lg overflow-hidden">
                  <table className="w-full">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-2 text-left">Sản phẩm</th>
                        <th className="px-4 py-2 text-center">Số lượng</th>
                        <th className="px-4 py-2 text-right">Đơn giá</th>
                        <th className="px-4 py-2 text-right">Thành tiền</th>
                      </tr>
                    </thead>
                    <tbody>
                      {orderDetail.items?.map((item: any, index: number) => (
                        <tr key={index} className="border-t">
                          <td className="px-4 py-2">
                            <div>
                              <p className="font-medium">{item.productName}</p>
                              {item.productId && (
                                <p className="text-sm text-gray-500">ID: {item.productId}</p>
                              )}
                            </div>
                          </td>
                          <td className="px-4 py-2 text-center">{item.quantity}</td>
                          <td className="px-4 py-2 text-right">{item.price?.toLocaleString('vi-VN')}₫</td>
                          <td className="px-4 py-2 text-right font-medium">{item.totalPrice?.toLocaleString('vi-VN')}₫</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              {/* Discount Details - Get from discountOrders data since backend doesn't include it */}
              {(() => {
                const orderWithDiscounts = discountOrders?.orders?.find(o => o.orderId === selectedOrderId);
                console.log('Modal - Looking for orderId:', selectedOrderId);
                console.log('Modal - Available orders:', discountOrders?.orders?.map(o => ({ id: o.orderId, number: o.orderNumber })));
                console.log('Modal - Found order with discounts:', orderWithDiscounts);
                console.log('Modal - Discount details:', orderWithDiscounts?.discountDetails);
                
                return orderWithDiscounts?.discountDetails?.length > 0 ? (
                  <div>
                    <h3 className="font-semibold mb-3">Chi tiết giảm giá</h3>
                    <div className="space-y-2">
                      {orderWithDiscounts.discountDetails.map((discount: any, index: number) => (
                        <div key={index} className="flex justify-between items-center p-3 bg-red-50 rounded-lg">
                          <div>
                            <p className="font-medium">{discount.discountName}</p>
                            <p className="text-sm text-gray-600">{discount.discountTypeName}</p>
                          </div>
                          <div className="text-right">
                            <p className="text-red-600 font-bold">-{discount.discountAmount?.toLocaleString('vi-VN')}₫</p>
                            {discount.discountValue && (
                              <p className="text-sm text-gray-500">
                                {discount.discountType === 1 ? `${discount.discountValue}%` : `${discount.discountValue?.toLocaleString('vi-VN')}₫`}
                              </p>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                ) : (
                  <div className="p-4 bg-yellow-50 rounded-lg">
                    <p className="text-yellow-800">Không tìm thấy chi tiết giảm giá cho đơn hàng này</p>
                    <p className="text-sm text-yellow-600 mt-1">
                      OrderID: {selectedOrderId}, Found: {orderWithDiscounts ? 'Yes' : 'No'}
                    </p>
                  </div>
                );
              })()}

              {/* Order Summary */}
              <div className="border-t pt-4">
                <div className="space-y-2 text-right">
                  <div className="flex justify-between">
                    <span>Tạm tính:</span>
                    <span>{orderDetail.subTotal?.toLocaleString('vi-VN')}₫</span>
                  </div>
                  {orderDetail.discountAmount > 0 && (
                    <div className="flex justify-between text-red-600">
                      <span>Giảm giá:</span>
                      <span>-{orderDetail.discountAmount?.toLocaleString('vi-VN')}₫</span>
                    </div>
                  )}
                  {orderDetail.taxAmount > 0 && (
                    <div className="flex justify-between">
                      <span>Thuế:</span>
                      <span>{orderDetail.taxAmount?.toLocaleString('vi-VN')}₫</span>
                    </div>
                  )}
                  <div className="flex justify-between text-lg font-bold border-t pt-2">
                    <span>Tổng cộng:</span>
                    <span className="text-green-600">{orderDetail.totalAmount?.toLocaleString('vi-VN')}₫</span>
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div className="text-center py-8 text-gray-500">
              Không thể tải thông tin đơn hàng
            </div>
          )}
        </DialogContent>
      </Dialog>
    </AppLayout>
  );
}
