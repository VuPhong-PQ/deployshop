import { useState } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useToast } from "@/hooks/use-toast";
import { queryClient, apiRequest } from "@/lib/queryClient";
import { 
  ArrowLeft, 
  RotateCcw, 
  Trash2, 
  Star, 
  Users, 
  TrendingUp, 
  Palette, 
  Award, 
  Archive,
  AlertTriangle
} from "lucide-react";
import { Label } from "@/components/ui/label";
import { useLocation } from "wouter";

interface CustomerTier {
  tierId: number;
  tierName: string;
  minSpent: number;
  minPoints: number;
  pointsMultiplier: number;
  discountPercentage: number;
  isActive: boolean;
  tierColor: string;
  tierIcon: string;
  displayOrder: number;
}

interface TierStatistics {
  tierId: number;
  tierName: string;
  customerCount: number;
}

export default function DisabledTiersArchive() {
  const { toast } = useToast();
  const [, navigate] = useLocation();

  // Fetch all tiers and filter disabled ones
  const { data: allTiers = [], isLoading, refetch } = useQuery<CustomerTier[]>({
    queryKey: ['/api/CustomerTierManagement'],
    queryFn: async () => {
      const response = await apiRequest('/api/CustomerTierManagement', { method: 'GET' });
      return response;
    },
  });

  // Filter only disabled tiers
  const disabledTiers = allTiers.filter(tier => !tier.isActive);

  // Fetch statistics
  const { data: statistics = [] } = useQuery<TierStatistics[]>({
    queryKey: ['/api/CustomerTierManagement/statistics'],
    queryFn: async () => {
      const response = await apiRequest('/api/CustomerTierManagement/statistics', { method: 'GET' });
      return response;
    },
  });

  // Restore tier mutation
  const restoreMutation = useMutation({
    mutationFn: async (tierId: number) => {
      const tier = allTiers.find(t => t.tierId === tierId);
      if (!tier) throw new Error('Tier not found');
      
      return apiRequest(`/api/CustomerTierManagement/${tierId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...tier, isActive: true })
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement'] });
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement/statistics'] });
      toast({
        title: "Thành công",
        description: "Hạng khách hàng đã được khôi phục",
        variant: "default",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error?.message || "Có lỗi xảy ra khi khôi phục hạng khách hàng",
        variant: "destructive",
      });
    },
  });

  // Delete permanently mutation
  const deleteMutation = useMutation({
    mutationFn: async (tierId: number) => {
      return apiRequest(`/api/CustomerTierManagement/${tierId}`, {
        method: 'DELETE'
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement'] });
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement/statistics'] });
      toast({
        title: "Thành công",
        description: "Hạng khách hàng đã được xóa vĩnh viễn",
        variant: "default",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error?.message || "Có lỗi xảy ra khi xóa hạng khách hàng",
        variant: "destructive",
      });
    },
  });

  // Cleanup all disabled tiers mutation
  const cleanupAllMutation = useMutation({
    mutationFn: async () => {
      return apiRequest('/api/CustomerTierManagement/cleanup-disabled', {
        method: 'POST'
      });
    },
    onSuccess: (response) => {
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement'] });
      queryClient.invalidateQueries({ queryKey: ['/api/CustomerTierManagement/statistics'] });
      toast({
        title: "Thành công",
        description: `Đã xóa ${response.deletedCount || 0} hạng vô hiệu`,
        variant: "default",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error?.message || "Có lỗi xảy ra khi dọn dẹp tất cả hạng vô hiệu",
        variant: "destructive",
      });
    },
  });

  const handleRestore = (tierId: number) => {
    if (window.confirm("Bạn có chắc chắn muốn khôi phục hạng khách hàng này không?")) {
      restoreMutation.mutate(tierId);
    }
  };

  const handleDelete = (tierId: number) => {
    const customerCount = getStatistics(tierId);
    if (customerCount > 0) {
      toast({
        title: "Không thể xóa",
        description: `Hạng này còn ${customerCount} khách hàng đang sử dụng`,
        variant: "destructive",
      });
      return;
    }

    if (window.confirm("Bạn có chắc chắn muốn xóa VĨNH VIỄN hạng khách hàng này không? Thao tác này không thể hoàn tác.")) {
      deleteMutation.mutate(tierId);
    }
  };

  const handleCleanupAll = () => {
    const hasCustomers = disabledTiers.some(tier => getStatistics(tier.tierId) > 0);
    if (hasCustomers) {
      toast({
        title: "Không thể xóa tất cả",
        description: "Một số hạng còn khách hàng đang sử dụng",
        variant: "destructive",
      });
      return;
    }

    if (window.confirm(`Bạn có chắc chắn muốn xóa VĨNH VIỄN tất cả ${disabledTiers.length} hạng đã vô hiệu không? Thao tác này không thể hoàn tác.`)) {
      cleanupAllMutation.mutate();
    }
  };

  const getStatistics = (tierId: number) => {
    return statistics.find(s => s.tierId === tierId)?.customerCount || 0;
  };

  const iconMap: Record<string, React.ComponentType<any>> = {
    star: Star,
    users: Users,
    trending: TrendingUp,
    palette: Palette,
    award: Award
  };

  if (isLoading) {
    return (
      <AppLayout>
        <div className="flex items-center justify-center h-64">
          <div className="text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-2"></div>
            <p>Đang tải dữ liệu...</p>
          </div>
        </div>
      </AppLayout>
    );
  }

  return (
    <AppLayout>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/customer-tier-management')}
              className="text-gray-600"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Quay lại
            </Button>
            <div>
              <h1 className="text-2xl font-bold flex items-center">
                <Archive className="w-6 h-6 mr-2 text-orange-600" />
                Kho lưu trữ hạng đã vô hiệu
              </h1>
              <p className="text-gray-600">
                Quản lý các hạng khách hàng đã bị vô hiệu hóa ({disabledTiers.length} hạng)
              </p>
            </div>
          </div>
          
          {disabledTiers.length > 0 && (
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                onClick={handleCleanupAll}
                disabled={cleanupAllMutation.isPending}
                className="text-red-600 hover:text-red-700 hover:bg-red-50"
              >
                <Trash2 className="w-4 h-4 mr-2" />
                Xóa tất cả
              </Button>
            </div>
          )}
        </div>

        {/* Warning if there are customers in disabled tiers */}
        {disabledTiers.some(tier => getStatistics(tier.tierId) > 0) && (
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <div className="flex items-start space-x-3">
              <AlertTriangle className="w-5 h-5 text-yellow-600 mt-0.5" />
              <div>
                <h3 className="font-medium text-yellow-800">Cảnh báo</h3>
                <p className="text-yellow-700 text-sm mt-1">
                  Một số hạng đã vô hiệu vẫn còn khách hàng đang sử dụng. 
                  Bạn cần di chuyển khách hàng sang hạng khác trước khi xóa vĩnh viễn.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Disabled Tiers Grid */}
        {disabledTiers.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {disabledTiers.map((tier) => {
              const IconComponent = iconMap[tier.tierIcon] || Star;
              const customerCount = getStatistics(tier.tierId);
              const hasCustomers = customerCount > 0;
              
              return (
                <Card key={tier.tierId} className="relative opacity-75 border-orange-200 bg-orange-50/30">
                  <CardHeader className="pb-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-2">
                        <div 
                          className="w-8 h-8 rounded-full flex items-center justify-center opacity-60"
                          style={{ backgroundColor: tier.tierColor }}
                        >
                          <IconComponent className="w-4 h-4 text-white" />
                        </div>
                        <div>
                          <CardTitle className="text-lg text-gray-700">{tier.tierName}</CardTitle>
                          <Badge variant="secondary" className="text-xs bg-orange-100 text-orange-800">
                            Đã vô hiệu
                          </Badge>
                        </div>
                      </div>
                      <div className="flex space-x-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleRestore(tier.tierId)}
                          className="text-green-600 hover:text-green-700 hover:bg-green-50"
                          title="Khôi phục hạng"
                        >
                          <RotateCcw className="w-4 h-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(tier.tierId)}
                          className="text-red-600 hover:text-red-700"
                          disabled={hasCustomers}
                          title={hasCustomers ? "Không thể xóa vì còn khách hàng" : "Xóa vĩnh viễn"}
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>
                  </CardHeader>
                  
                  <CardContent className="space-y-3">
                    <div className="grid grid-cols-2 gap-3 text-sm">
                      <div>
                        <Label className="text-muted-foreground">Chi tiêu tối thiểu</Label>
                        <p className="font-medium text-gray-600">{tier.minSpent.toLocaleString()} ₫</p>
                      </div>
                      <div>
                        <Label className="text-muted-foreground">Điểm tối thiểu</Label>
                        <p className="font-medium text-gray-600">{tier.minPoints.toLocaleString()}</p>
                      </div>
                      <div>
                        <Label className="text-muted-foreground">Hệ số điểm</Label>
                        <p className="font-medium text-gray-600">x{tier.pointsMultiplier}</p>
                      </div>
                      <div>
                        <Label className="text-muted-foreground">Giảm giá</Label>
                        <p className="font-medium text-gray-600">{tier.discountPercentage}%</p>
                      </div>
                    </div>
                    
                    <div className="pt-2 border-t">
                      <div className="flex items-center justify-between text-sm">
                        <span className="text-muted-foreground">Số khách hàng</span>
                        <Badge 
                          variant={hasCustomers ? "destructive" : "outline"}
                          className="flex items-center"
                        >
                          <Users className="w-3 h-3 mr-1" />
                          {customerCount}
                          {hasCustomers && <AlertTriangle className="w-3 h-3 ml-1" />}
                        </Badge>
                      </div>
                    </div>
                    
                    {hasCustomers && (
                      <div className="text-xs text-yellow-700 bg-yellow-100 p-2 rounded">
                        Cần di chuyển khách hàng trước khi xóa
                      </div>
                    )}
                  </CardContent>
                </Card>
              );
            })}
          </div>
        ) : (
          <div className="text-center py-12">
            <div className="text-gray-400 mb-4">
              <Archive className="w-12 h-12 mx-auto" />
            </div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">Không có hạng nào đã vô hiệu</h3>
            <p className="text-gray-500 mb-4">Tất cả hạng khách hàng đều đang hoạt động</p>
            <Button onClick={() => navigate('/customer-tier-management')}>
              <ArrowLeft className="w-4 h-4 mr-2" />
              Quay lại quản lý hạng
            </Button>
          </div>
        )}
      </div>
    </AppLayout>
  );
}