import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { api } from "@/lib/api";
import { useAuth } from "@/contexts/auth-context";
import { useLocation } from "wouter";
import { 
  TrendingUp, 
  DollarSign, 
  ShoppingCart, 
  Users, 
  Package,
  AlertTriangle,
  Store,
  ArrowRight
} from "lucide-react";

interface DashboardMetrics {
  todayRevenue: number;
  revenueGrowth: string;
  totalOrders: number;
  todayOrdersCount: number;
  orderGrowth: string;
  totalCustomers: number;
  totalProducts: number;
  lowStockCount: number;
  // Additional properties
  lowStockItems?: number;
  ordersCount?: number;
  monthRevenue?: number | string;
  newCustomers?: number;
  ordersByStatus?: {
    completed: number;
    paid: number;
    pending: number;
    cancelled: number;
  };
}

export default function Dashboard() {
  const { currentStore, user, availableStores } = useAuth();
  const [, navigate] = useLocation();

  const { data: metricsResponse, isLoading } = useQuery({
    queryKey: ["/api/dashboard/metrics", currentStore?.storeId],
    queryFn: async () => {
      if (!currentStore?.storeId) return null;
      const response = await api.getDashboardMetrics(currentStore.storeId);
      return response.success ? response.data : null;
    },
    enabled: !!currentStore?.storeId,
  });

  const { data: storesResponse } = useQuery({
    queryKey: ["/api/dashboard/metrics/stores", user?.username],
    queryFn: async () => {
      const response = await fetch("http://101.53.9.76:5273/api/dashboard/metrics/stores", {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Username': user?.username || 'admin'
        },
        credentials: 'include'
      });
      if (!response.ok) throw new Error("Failed to fetch stores");
      return response.json();
    },
    enabled: !!user?.username,
  });

  const { data: lowStockProducts, isLoading: isLoadingLowStock } = useQuery({
    queryKey: ["/api/dashboard/low-stock-products"],
    queryFn: async () => {
      const response = await api.getLowStockProducts();
      return response.success ? response.data : [];
    },
  });

  const metrics = metricsResponse as DashboardMetrics;

  const handleStoreClick = (storeId: number) => {
    // Kiểm tra xem user có quyền truy cập store này không
    const hasAccess = availableStores?.some(store => store.storeId === storeId);
    
    if (hasAccess) {
      // Chuyển đến trang bán hàng với storeId
      console.log('Dashboard - Clicking authorized store with ID:', storeId);
      navigate(`/sales?storeId=${storeId}`);
    } else {
      console.warn('Dashboard - User không có quyền truy cập store:', storeId);
      // Có thể thêm toast thông báo ở đây
    }
  };

  const handleChangeStore = () => {
    // Chuyển đến trang chọn cửa hàng
    navigate("/store-selection");
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-gray-900"></div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <Button 
          variant="outline" 
          onClick={handleChangeStore}
          className="flex items-center gap-2"
        >
          <Store className="w-4 h-4" />
          Đổi cửa hàng
        </Button>
      </div>

      {currentStore && (
        <div className="mb-6 p-4 bg-blue-50 rounded-lg border-l-4 border-blue-500">
          <h2 className="text-lg font-semibold text-blue-800">
            Đang xem dữ liệu của: {currentStore.name}
          </h2>
          <p className="text-blue-600">{currentStore.address}</p>
        </div>
      )}

      {/* Store Selection Cards */}
      {storesResponse && storesResponse.length > 0 && (
        <div className="mb-6">
          <h2 className="text-xl font-semibold mb-4">Các cửa hàng được phép truy cập</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {storesResponse.map((store: any) => {
              // Kiểm tra xem user có quyền truy cập store này không
              const hasAccess = availableStores?.some(s => s.storeId === store.id);
              
              return (
                <Card 
                  key={store.id}
                  className={`transition-all ${
                    hasAccess 
                      ? `cursor-pointer hover:shadow-md ${
                          currentStore?.storeId === store.id 
                            ? "ring-2 ring-blue-500 bg-blue-50" 
                            : "hover:border-blue-300"
                        }`
                      : "opacity-50 cursor-not-allowed bg-gray-50"
                  }`}
                  onClick={() => hasAccess && handleStoreClick(store.id)}
                >
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-lg flex items-center gap-2">
                      <Store className="w-5 h-5" />
                      {store.name}
                      {!hasAccess && <span className="text-xs text-red-500 ml-2">(Không có quyền)</span>}
                    </CardTitle>
                    {hasAccess && <ArrowRight className="w-4 h-4 text-gray-400" />}
                  </div>
                </CardHeader>
                <CardContent className="space-y-2">
                  {store.address && (
                    <p className="text-sm text-gray-600">{store.address}</p>
                  )}
                  <div className="flex justify-between text-sm">
                    <span>Doanh thu hôm nay:</span>
                    <span className="font-medium">{store.todayRevenue?.toLocaleString('vi-VN') || '0'} đ</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span>Tổng đơn hàng:</span>
                    <span className="font-medium">{store.totalOrders || 0}</span>
                  </div>
                  <Button 
                    size="sm" 
                    className="w-full mt-2" 
                    disabled={!hasAccess}
                    onClick={(e) => {
                      e.stopPropagation();
                      hasAccess && handleStoreClick(store.id);
                    }}
                  >
                    {hasAccess ? "Vào bán hàng" : "Không có quyền"}
                  </Button>
                </CardContent>
              </Card>
              );
            })}
          </div>
        </div>
      )}

      <Tabs defaultValue="overview" className="space-y-4">
        <TabsList>
          <TabsTrigger value="overview">Tổng quan</TabsTrigger>
          <TabsTrigger value="sales">Bán hàng</TabsTrigger>
          <TabsTrigger value="inventory">Kho hàng</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">
                  Doanh thu hôm nay
                </CardTitle>
                <DollarSign className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {metrics?.todayRevenue?.toLocaleString('vi-VN') || '0'} đ
                </div>
                <p className="text-xs text-muted-foreground">
                  {metrics?.revenueGrowth || '+0%'} so với hôm qua
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">
                  Sản phẩm sắp hết
                </CardTitle>
                <AlertTriangle className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-red-600">
                  {metrics?.lowStockItems || 0}
                </div>
                <p className="text-xs text-muted-foreground">
                  Cần nhập thêm
                </p>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="sales" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Thống kê bán hàng</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <p className="text-sm font-medium">Tổng đơn hàng</p>
                  <p className="text-2xl font-bold">{metrics?.ordersCount || 0}</p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm font-medium">Doanh thu hôm nay</p>
                  <p className="text-2xl font-bold">
                    {metrics?.todayRevenue || '0₫'}
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm font-medium">Doanh thu tháng này</p>
                  <p className="text-2xl font-bold">
                    {metrics?.monthRevenue || '0₫'}
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm font-medium">Khách hàng mới</p>
                  <p className="text-2xl font-bold">{metrics?.newCustomers || 0}</p>
                </div>
              </div>
              
              {/* Orders by Status */}
              {metrics?.ordersByStatus && (
                <div className="mt-6">
                  <p className="text-sm font-medium mb-3">Trạng thái đơn hàng</p>
                  <div className="grid gap-2 md:grid-cols-4">
                    <div className="bg-green-50 p-3 rounded-lg">
                      <p className="text-xs text-green-600">Hoàn thành</p>
                      <p className="text-lg font-bold text-green-700">{metrics.ordersByStatus.completed}</p>
                    </div>
                    <div className="bg-blue-50 p-3 rounded-lg">
                      <p className="text-xs text-blue-600">Đã thanh toán</p>
                      <p className="text-lg font-bold text-blue-700">{metrics.ordersByStatus.paid}</p>
                    </div>
                    <div className="bg-yellow-50 p-3 rounded-lg">
                      <p className="text-xs text-yellow-600">Đang chờ</p>
                      <p className="text-lg font-bold text-yellow-700">{metrics.ordersByStatus.pending}</p>
                    </div>
                    <div className="bg-red-50 p-3 rounded-lg">
                      <p className="text-xs text-red-600">Đã hủy</p>
                      <p className="text-lg font-bold text-red-700">{metrics.ordersByStatus.cancelled}</p>
                    </div>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="inventory" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Thông tin kho hàng</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <p className="text-sm font-medium">Sản phẩm sắp hết</p>
                  <p className="text-2xl font-bold text-red-600">
                    {metrics?.lowStockItems || 0}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    Cần nhập thêm hàng
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm font-medium">Tình trạng kho</p>
                  <div className="flex items-center gap-2">
                    <AlertTriangle className={`w-5 h-5 ${(metrics?.lowStockItems || 0) > 0 ? 'text-red-500' : 'text-green-500'}`} />
                    <span className={`font-bold ${(metrics?.lowStockItems || 0) > 0 ? 'text-red-600' : 'text-green-600'}`}>
                      {(metrics?.lowStockItems || 0) > 0 ? 'Cần chú ý' : 'Ổn định'}
                    </span>
                  </div>
                </div>
              </div>
              
              {/* Inventory Status Alert */}
              {(metrics?.lowStockItems || 0) > 0 && (
                <div className="mt-4 p-4 bg-red-50 border border-red-200 rounded-lg">
                  <div className="flex items-center">
                    <AlertTriangle className="w-5 h-5 text-red-500 mr-2" />
                    <div>
                      <p className="text-sm font-medium text-red-800">
                        Cảnh báo tồn kho thấp!
                      </p>
                      <p className="text-xs text-red-600 mt-1">
                        Có {metrics.lowStockItems} sản phẩm sắp hết hàng. Hãy kiểm tra và nhập thêm hàng.
                      </p>
                    </div>
                  </div>
                </div>
              )}

              {/* Low Stock Products List */}
              <div className="mt-6">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-lg font-semibold">Danh sách sản phẩm sắp hết</h3>
                  <Button 
                    variant="outline" 
                    size="sm"
                    onClick={() => navigate('/products')}
                    className="text-blue-600 border-blue-600 hover:bg-blue-50"
                  >
                    Quản lý sản phẩm
                  </Button>
                </div>

                {isLoadingLowStock ? (
                  <div className="text-center py-8">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-2"></div>
                    <p className="text-gray-500">Đang tải danh sách sản phẩm...</p>
                  </div>
                ) : lowStockProducts && lowStockProducts.length > 0 ? (
                  <div className="bg-white border rounded-lg overflow-hidden">
                    <div className="overflow-x-auto">
                      <table className="w-full">
                        <thead className="bg-gray-50">
                          <tr>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                              Tên sản phẩm
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                              Nhóm sản phẩm
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                              Tồn kho
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                              Tối thiểu
                            </th>
                            <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                              Giá bán
                            </th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-200">
                          {lowStockProducts.map((product: any) => (
                            <tr key={product.id} className="hover:bg-gray-50">
                              <td className="px-4 py-3 text-sm font-medium text-gray-900">
                                {product.name}
                              </td>
                              <td className="px-4 py-3 text-sm text-gray-500">
                                <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                                  product.category === 'Chưa phân loại' 
                                    ? 'bg-gray-100 text-gray-600' 
                                    : 'bg-blue-100 text-blue-700'
                                }`}>
                                  {product.category}
                                </span>
                              </td>
                              <td className="px-4 py-3 text-sm">
                                <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                                  product.stockQuantity <= 3 
                                    ? 'bg-red-100 text-red-800' 
                                    : product.stockQuantity <= 7 
                                    ? 'bg-yellow-100 text-yellow-800' 
                                    : 'bg-orange-100 text-orange-800'
                                }`}>
                                  {product.stockQuantity}
                                </span>
                              </td>
                              <td className="px-4 py-3 text-sm text-gray-500">
                                <span className="text-xs bg-gray-100 px-2 py-1 rounded">
                                  {product.minStockLevel || 5}
                                </span>
                              </td>
                              <td className="px-4 py-3 text-sm text-gray-900">
                                {product.price?.toLocaleString('vi-VN')}₫
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    
                    {/* Info about categories */}
                    {lowStockProducts.some((p: any) => p.category === 'Chưa phân loại') && (
                      <div className="px-4 py-3 bg-yellow-50 border-t border-yellow-200">
                        <div className="flex items-center">
                          <AlertTriangle className="w-4 h-4 text-yellow-500 mr-2" />
                          <p className="text-sm text-yellow-700">
                            <span className="font-medium">Lưu ý:</span> Một số sản phẩm chưa được phân loại. 
                            Hãy vào <button 
                              onClick={() => navigate('/products')} 
                              className="text-blue-600 underline hover:text-blue-800"
                            >
                              trang quản lý sản phẩm
                            </button> để gán nhóm sản phẩm.
                          </p>
                        </div>
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="text-center py-8 bg-green-50 rounded-lg border border-green-200">
                    <Package className="w-12 h-12 text-green-500 mx-auto mb-2" />
                    <h4 className="text-lg font-medium text-green-800 mb-1">Tuyệt vời!</h4>
                    <p className="text-green-600">Không có sản phẩm nào sắp hết hàng.</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}