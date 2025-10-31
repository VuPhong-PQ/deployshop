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
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
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
                  Đơn hàng hôm nay
                </CardTitle>
                <ShoppingCart className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {metrics?.todayOrdersCount || 0}
                </div>
                <p className="text-xs text-muted-foreground">
                  {metrics?.orderGrowth || '+0%'} so với hôm qua
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">
                  Tổng khách hàng
                </CardTitle>
                <Users className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {metrics?.totalCustomers || 0}
                </div>
                <p className="text-xs text-muted-foreground">
                  Trong hệ thống
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
                  {metrics?.lowStockCount || 0}
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
                  <p className="text-2xl font-bold">{metrics?.totalOrders || 0}</p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm font-medium">Doanh thu hôm nay</p>
                  <p className="text-2xl font-bold">
                    {metrics?.todayRevenue?.toLocaleString('vi-VN') || '0'} đ
                  </p>
                </div>
              </div>
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
                  <p className="text-sm font-medium">Tổng sản phẩm</p>
                  <p className="text-2xl font-bold">{metrics?.totalProducts || 0}</p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm font-medium">Sản phẩm sắp hết</p>
                  <p className="text-2xl font-bold text-red-600">
                    {metrics?.lowStockCount || 0}
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}