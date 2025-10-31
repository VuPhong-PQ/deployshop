import { X, Info, AlertTriangle, CheckCircle } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import type { NotificationItem } from "@/lib/types";
import { useNotifications } from "@/hooks/use-notifications";
import { useNotificationSound } from "@/hooks/use-notification-sound";
import { useToast } from "@/hooks/use-toast";
import { OrderDetailModal } from "./order-detail-modal";
import { useState } from "react";
import { useLocation } from "wouter";

interface NotificationModalProps {
  show: boolean;
  onClose: () => void;
}

export function NotificationModal({ show, onClose }: NotificationModalProps) {
  const { notifications, markAsRead, markAllAsRead, deleteNotification, getNavigationInfo, handleNotificationClick, isLoading } = useNotifications();
  const { playNotificationSound } = useNotificationSound();
  const { toast } = useToast();
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [showOrderDetail, setShowOrderDetail] = useState(false);
  const [, navigate] = useLocation();
  
  if (!show) return null;

  const handleNotificationItemClick = async (notification: NotificationItem) => {
    try {
      console.log('Clicking notification:', notification);
      // Get navigation info from API
      const navigationInfo = await getNavigationInfo(parseInt(notification.id));
      console.log('Navigation info received:', navigationInfo);
      
      if (navigationInfo.type === 'order' && navigationInfo.data.orderId) {
        // Handle order notifications (new order, payment success)
        setSelectedOrderId(navigationInfo.data.orderId);
        setShowOrderDetail(true);
      } else if (navigationInfo.type === 'product') {
        // Handle product notifications (low stock, out of stock)
        const searchParams = new URLSearchParams();
        if (navigationInfo.data.productId) {
          searchParams.set('productId', navigationInfo.data.productId.toString());
        }
        if (navigationInfo.data.searchTerm) {
          searchParams.set('search', navigationInfo.data.searchTerm);
        }
        
        toast({
          title: "Chuyển đến quản lý kho 📦",
          description: `Đang mở trang kho hàng để xem chi tiết sản phẩm`,
        });
        
        navigate(`/inventory?${searchParams.toString()}`);
        onClose(); // Close notification modal
      } else {
        // Handle other notification types
        navigate(navigationInfo.path || '/');
        onClose();
      }
    } catch (error) {
      console.error('Error getting navigation info:', error);
      toast({
        title: "Lỗi",
        description: "Không thể mở chi tiết thông báo",
        variant: "destructive"
      });
    }
  };

  const handleReopenOrder = (orderDetail: any) => {
    // Thông báo ngay lập tức
    toast({
      title: "Đang mở lại đơn hàng! 🔄",
      description: `Đơn hàng #${orderDetail.orderId} của ${orderDetail.customerName || orderDetail.customer?.hoTen || 'khách vãng lai'} đã được tải vào giỏ hàng`,
    });
    
    // Phát âm thanh thông báo
    playNotificationSound();
    
    // Store order data in localStorage temporarily
    localStorage.setItem('reopenOrder', JSON.stringify(orderDetail));
    
    // Dispatch custom event to notify sales page to check localStorage
    window.dispatchEvent(new CustomEvent('reopenOrderSet'));
    
    // Navigate to sales page
    navigate('/sales');
    onClose(); // Close notification modal
  };

  const getIcon = (type: NotificationItem['type']) => {
    switch (type) {
      case 'info':
        return Info;
      case 'warning':
        return AlertTriangle;
      case 'success':
        return CheckCircle;
      default:
        return Info;
    }
  };

  const getIconStyle = (type: NotificationItem['type']) => {
    switch (type) {
      case 'info':
        return "bg-primary text-white";
      case 'warning':
        return "bg-warning text-white";
      case 'success':
        return "bg-success text-white";
      default:
        return "bg-gray-500 text-white";
    }
  };

  const getBackgroundStyle = (type: NotificationItem['type']) => {
    switch (type) {
      case 'info':
        return "bg-blue-50";
      case 'warning':
        return "bg-orange-50";
      case 'success':
        return "bg-green-50";
      default:
        return "bg-gray-50";
    }
  };

  return (
    <>
      <div 
        className="fixed inset-0 bg-black bg-opacity-50 z-50"
        onClick={onClose}
        data-testid="notification-overlay"
      />
      <div className="fixed right-4 top-16 w-96 bg-white rounded-lg shadow-xl border border-gray-200 max-h-96 overflow-y-auto z-50">
        <div className="p-4 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <h3 className="font-semibold text-gray-900" data-testid="notification-title">
              Thông báo
            </h3>
            <div className="flex items-center space-x-2">
              {notifications.length > 0 && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => markAllAsRead()}
                  disabled={isLoading}
                  className="text-xs"
                >
                  Đánh dấu tất cả đã đọc
                </Button>
              )}
              <Button
                variant="ghost" 
                size="sm"
                onClick={onClose}
                data-testid="button-close-notifications"
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </div>
        <div className="p-4 space-y-3">
          {notifications.map((notification, index) => {
            const Icon = getIcon(notification.type);
            const isClickable = (notification.type === 'info' && notification.title === 'Đơn hàng mới') ||
                               (notification.type === 'warning' && (notification.title === 'Cảnh báo tồn kho thấp' || notification.title === 'Hết hàng'));
            
            return (
              <div 
                key={notification.id} 
                className={`flex items-start space-x-3 p-3 rounded-lg ${getBackgroundStyle(notification.type)} ${
                  isClickable ? 'cursor-pointer hover:bg-opacity-80 transition-colors' : ''
                }`}
                data-testid={`notification-${index}`}
                onClick={isClickable ? () => handleNotificationItemClick(notification) : undefined}
              >
                <div className={`w-8 h-8 rounded-full flex items-center justify-center ${getIconStyle(notification.type)}`}>
                  <Icon className="h-4 w-4" />
                </div>
                <div className="flex-1">
                  <p className="font-medium text-gray-900" data-testid={`notification-title-${index}`}>
                    {notification.title}
                    {isClickable && <span className="text-xs text-blue-600 ml-2">(Click để xem chi tiết)</span>}
                  </p>
                  <p className="text-sm text-gray-600" data-testid={`notification-message-${index}`}>
                    {notification.message}
                  </p>
                  <p className="text-xs text-gray-500 mt-1" data-testid={`notification-time-${index}`}>
                    {notification.time}
                  </p>
                </div>
                {!notification.read && (
                  <div className="w-2 h-2 bg-blue-500 rounded-full mt-2"></div>
                )}
              </div>
            );
          })}
          
          {notifications.length === 0 && (
            <div className="text-center py-8 text-gray-500">
              Không có thông báo nào
            </div>
          )}
        </div>
        
        {/* Order Detail Modal */}
        <OrderDetailModal
          orderId={selectedOrderId}
          show={showOrderDetail}
          onClose={() => {
            setShowOrderDetail(false);
            setSelectedOrderId(null);
          }}
          onReopenOrder={handleReopenOrder}
        />
      </div>
    </>
  );
}