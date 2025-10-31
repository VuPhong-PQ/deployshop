import { useEffect, useState } from "react";
import { useLocation } from "wouter";
import { Bell, Plus, User, LogOut, ChevronDown, Menu } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useNotifications } from "@/hooks/use-notifications";
import { useAuth } from "@/contexts/auth-context";
import { RefreshPermissionsButton } from "@/components/debug/refresh-permissions-button";
import StoreSwitcher from "@/components/StoreSwitcher";
import StoreInfoHeader from "@/components/StoreInfoHeader";

interface HeaderProps {
  title: string;
  onToggleNotifications: () => void;
  isWebSocketConnected: boolean;
}

export function Header({ title, onToggleNotifications, isWebSocketConnected }: HeaderProps) {
  const [currentTime, setCurrentTime] = useState(new Date());
  const [, navigate] = useLocation();
  const { unreadCount } = useNotifications();
  const { user, logout } = useAuth();

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentTime(new Date());
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <header className="bg-white shadow-sm border-b border-gray-200" data-testid="header">
      {/* Mobile Header (xs screens) */}
      <div className="sm:hidden p-3">
        <div className="flex items-center justify-between">
          {/* Left: Title */}
          <h2 className="text-lg font-bold text-gray-900 truncate flex-1 mr-2" data-testid="page-title">
            {title}
          </h2>
          
          {/* Right: Compact Actions */}
          <div className="flex items-center space-x-1">
            {/* Notifications */}
            <button
              className="relative p-2 text-gray-600 hover:bg-gray-100 rounded-lg"
              onClick={onToggleNotifications}
              data-testid="button-notifications"
            >
              <Bell className="h-4 w-4" />
              {unreadCount > 0 && (
                <span className="absolute -top-1 -right-1 bg-error text-white text-xs rounded-full w-4 h-4 flex items-center justify-center">
                  {unreadCount > 9 ? '9+' : unreadCount}
                </span>
              )}
            </button>

            {/* Mobile Menu */}
            {user && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm" className="px-2">
                    <Menu className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-80">
                  <DropdownMenuLabel>
                    <div className="flex flex-col space-y-1">
                      <p className="text-sm font-medium">{user.fullName}</p>
                      <p className="text-xs text-gray-500">{user.email || user.username}</p>
                      <p className="text-xs text-gray-500">Vai trò: {user.roleName}</p>
                    </div>
                  </DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  
                  {/* Store info in mobile menu */}
                  <div className="p-2">
                    <p className="text-xs font-medium text-gray-700 mb-2">Cửa hàng hiện tại:</p>
                    <StoreSwitcher onStoreChange={(store) => {
                      console.log('Store changed to:', store);
                    }} />
                  </div>
                  <DropdownMenuSeparator />
                  
                  {/* Quick Sale */}
                  <DropdownMenuItem onClick={() => navigate('/sales')}>
                    <Plus className="h-4 w-4 mr-2" />
                    Bán hàng nhanh
                  </DropdownMenuItem>
                  
                  {/* Debug */}
                  <DropdownMenuItem asChild>
                    <div className="p-0">
                      <RefreshPermissionsButton />
                    </div>
                  </DropdownMenuItem>
                  
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={handleLogout} className="text-red-600">
                    <LogOut className="h-4 w-4 mr-2" />
                    Đăng xuất
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>
        </div>
        
        {/* Second row - Time and Status */}
        <div className="flex items-center justify-between mt-2 pt-2 border-t border-gray-100">
          <div className="flex items-center space-x-2 text-xs text-gray-500">
            <span data-testid="current-date">
              {currentTime.toLocaleDateString('vi-VN')}
            </span>
            <span>•</span>
            <span data-testid="current-time">
              {currentTime.toLocaleTimeString('vi-VN')}
            </span>
            {isWebSocketConnected && (
              <>
                <span>•</span>
                <span className="text-green-600" data-testid="ws-status">
                  ● Live
                </span>
              </>
            )}
          </div>
          
          {/* Store info in mobile */}
          <div className="flex-shrink-0">
            <StoreInfoHeader />
          </div>
        </div>
      </div>

      {/* Desktop Header (sm+ screens) */}
      <div className="hidden sm:block p-4 lg:p-6">
        <div className="flex items-center justify-between">
          {/* Left side - Title and Time */}
          <div className="flex items-center space-x-2 sm:space-x-4 min-w-0 flex-1 mr-4">
            <h2 className="text-xl lg:text-2xl font-bold text-gray-900 truncate" data-testid="page-title">
              {title}
            </h2>
            <div className="flex items-center space-x-2 text-xs sm:text-sm text-gray-500">
              <span data-testid="current-date">
                {currentTime.toLocaleDateString('vi-VN')}
              </span>
              <span className="hidden md:inline">•</span>
              <span data-testid="current-time" className="hidden md:inline">
                {currentTime.toLocaleTimeString('vi-VN')}
              </span>
              {isWebSocketConnected && (
                <>
                  <span className="hidden lg:inline">•</span>
                  <span className="text-green-600 hidden lg:inline" data-testid="ws-status">
                    ● Live
                  </span>
                </>
              )}
            </div>
          </div>
          
          {/* Right side - Store Info and Actions */}
          <div className="flex items-center space-x-2 lg:space-x-4 flex-shrink-0">
            {/* Store Info */}
            <StoreInfoHeader />
            
            {/* Debug button - only on large screens */}
            <div className="hidden lg:block">
              <RefreshPermissionsButton />
            </div>
            
            {/* Notifications */}
            <button
              className="relative p-2 text-gray-600 hover:bg-gray-100 rounded-lg flex-shrink-0"
              onClick={onToggleNotifications}
              data-testid="button-notifications"
            >
              <Bell className="h-4 w-4 sm:h-5 sm:w-5" />
              {unreadCount > 0 && (
                <span className="absolute -top-1 -right-1 bg-error text-white text-xs rounded-full w-4 h-4 sm:w-5 sm:h-5 flex items-center justify-center">
                  {unreadCount > 99 ? '99+' : unreadCount}
                </span>
              )}
            </button>
            
            {/* Quick Sale Button */}
            <Button 
              className="bg-primary hover:bg-blue-700 text-sm px-2 sm:px-4 py-1 sm:py-2" 
              data-testid="button-quick-sale"
              onClick={() => navigate('/sales')}
            >
              <Plus className="w-4 h-4 sm:mr-2" />
              <span className="hidden md:inline">Bán hàng nhanh</span>
            </Button>

            {/* User Menu */}
            {user && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" className="flex items-center space-x-1 sm:space-x-2 px-2 sm:px-3 py-1 sm:py-2 flex-shrink-0">
                    <User className="h-4 w-4" />
                    <span className="hidden lg:inline text-sm truncate max-w-20 xl:max-w-none">
                      {user.fullName}
                    </span>
                    <ChevronDown className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-64 sm:w-72">
                  <DropdownMenuLabel>
                    <div className="flex flex-col space-y-1">
                      <p className="text-sm font-medium">{user.fullName}</p>
                      <p className="text-xs text-gray-500">{user.email || user.username}</p>
                      <p className="text-xs text-gray-500">Vai trò: {user.roleName}</p>
                    </div>
                  </DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <div className="p-2">
                    <p className="text-xs font-medium text-gray-700 mb-2">Cửa hàng hiện tại:</p>
                    <StoreSwitcher onStoreChange={(store) => {
                      console.log('Store changed to:', store);
                    }} />
                  </div>
                  <DropdownMenuSeparator />
                  {/* Debug option in mobile menu */}
                  <div className="block lg:hidden p-2">
                    <RefreshPermissionsButton />
                  </div>
                  <DropdownMenuSeparator className="block lg:hidden" />
                  <DropdownMenuItem onClick={handleLogout} className="text-red-600">
                    <LogOut className="h-4 w-4 mr-2" />
                    Đăng xuất
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}
