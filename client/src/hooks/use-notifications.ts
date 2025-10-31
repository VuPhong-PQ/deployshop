import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiRequest } from '@/lib/queryClient';
import type { BackendNotification, NotificationItem } from '@/lib/types';

// Mapping từ backend notification type sang frontend type
const mapNotificationType = (backendType: number): NotificationItem['type'] => {
  switch (backendType) {
    case 1: // NewOrder
      return 'info';
    case 2: // LowStock
      return 'warning';
    case 3: // PaymentSuccess
      return 'success';
    case 4: // OutOfStock
      return 'warning';
    case 5: // SystemAlert
      return 'error';
    default:
      return 'info';
  }
};

// Helper để format thời gian
const formatTime = (dateString: string): string => {
  const now = new Date();
  const date = new Date(dateString);
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / (1000 * 60));
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffMins < 1) return 'Vừa xong';
  if (diffMins < 60) return `${diffMins} phút trước`;
  if (diffHours < 24) return `${diffHours} giờ trước`;
  return `${diffDays} ngày trước`;
};

// Convert backend notification sang frontend format
const convertNotification = (backendNotif: BackendNotification): NotificationItem => ({
  id: backendNotif.notificationId.toString(),
  type: mapNotificationType(backendNotif.type),
  title: backendNotif.title,
  message: backendNotif.message || '',
  time: formatTime(backendNotif.createdAt),
  read: backendNotif.status === 1 // 1 = Read, 0 = Unread
});

export const useNotifications = () => {
  const queryClient = useQueryClient();

  // Fetch notifications
  const { data: backendNotifications = [], refetch } = useQuery<BackendNotification[]>({
    queryKey: ['/api/notifications'],
    queryFn: () => apiRequest('/api/notifications?pageSize=50', { method: 'GET' }),
    refetchInterval: 30000, // Refetch every 30 seconds
  });

  // Fetch notification count
  const { data: notificationCount } = useQuery<{ unreadCount: number; totalCount: number }>({
    queryKey: ['/api/notifications/count'],
    queryFn: () => apiRequest('/api/notifications/count', { method: 'GET' }),
    refetchInterval: 10000, // Refetch every 10 seconds
  });

  // Mark as read mutation
  const markAsReadMutation = useMutation({
    mutationFn: (notificationId: number) => 
      apiRequest(`/api/notifications/${notificationId}/mark-read`, {
        method: 'POST',
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/notifications'] });
      queryClient.invalidateQueries({ queryKey: ['/api/notifications/count'] });
    },
  });

  // Mark all as read mutation
  const markAllAsReadMutation = useMutation({
    mutationFn: () => 
      apiRequest('/api/notifications/mark-all-read', {
        method: 'POST',
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/notifications'] });
      queryClient.invalidateQueries({ queryKey: ['/api/notifications/count'] });
    },
  });

  // Delete notification mutation
  const deleteNotificationMutation = useMutation({
    mutationFn: (notificationId: number) => 
      apiRequest(`/api/notifications/${notificationId}`, {
        method: 'DELETE',
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/notifications'] });
      queryClient.invalidateQueries({ queryKey: ['/api/notifications/count'] });
    },
  });

  // Navigation info mutation
  const getNavigationInfoMutation = useMutation({
    mutationFn: (notificationId: number) => 
      apiRequest(`/api/notifications/${notificationId}/navigate`, {
        method: 'GET',
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['/api/notifications'] });
      queryClient.invalidateQueries({ queryKey: ['/api/notifications/count'] });
    },
  });

  // Convert to frontend format
  const notifications: NotificationItem[] = backendNotifications.map(convertNotification);
  const unreadCount = notificationCount?.unreadCount || 0;

  // Handle notification click
  const handleNotificationClick = (notification: NotificationItem) => {
    const backendNotif = backendNotifications.find(n => n.notificationId.toString() === notification.id);
    
    if (backendNotif) {
      // Mark as read if unread
      if (!notification.read) {
        markAsReadMutation.mutate(backendNotif.notificationId);
      }
      
      // Return relevant data for the click handler
      return {
        type: backendNotif.type,
        orderId: backendNotif.orderId,
        productId: backendNotif.productId,
        customerId: backendNotif.customerId,
        metadata: backendNotif.metadata
      };
    }
    return null;
  };

  return {
    notifications,
    unreadCount,
    totalCount: notificationCount?.totalCount || 0,
    markAsRead: markAsReadMutation.mutate,
    markAllAsRead: markAllAsReadMutation.mutate,
    deleteNotification: deleteNotificationMutation.mutate,
    getNavigationInfo: getNavigationInfoMutation.mutateAsync,
    handleNotificationClick,
    refetch,
    isLoading: markAsReadMutation.isPending || markAllAsReadMutation.isPending || deleteNotificationMutation.isPending,
  };
};