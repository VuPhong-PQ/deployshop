import { Link, useLocation } from "wouter";
import { useState } from "react";
import { useAuth } from "@/contexts/auth-context";
import {
  ShoppingCart,
  Package,
  Users,
  Warehouse,
  BarChart3,
  UserCheck,
  Settings,
  ScanBarcode,
  LogOut,
  ChevronDown,
  ChevronRight,
  Folder,
  Tag,
  ClipboardList,
  FileX,
  TrendingUp,
  Database,
  Store
} from "lucide-react";
import { cn } from "@/lib/utils";

const navigation = [
  { name: 'Bán hàng', href: '/', icon: ShoppingCart, permission: 'ViewOrders' },
  { name: 'Đơn hàng', href: '/orders', icon: ClipboardList, permission: 'ViewOrders' },
  { 
    name: 'Sản phẩm', 
    icon: Package,
    permission: 'ViewProducts',
    submenu: [
      { name: 'Danh sách sản phẩm', href: '/products', icon: Package, permission: 'ViewProducts' },
      { name: 'Nhóm sản phẩm', href: '/product-groups', icon: Folder, permission: 'ViewProducts' }
    ]
  },
  { name: 'Khách hàng', href: '/customers', icon: Users, permission: 'ViewCustomers' },
  { name: 'Kho hàng', href: '/inventory', icon: Warehouse, permission: 'ViewProducts' },
  { 
    name: 'Báo cáo', 
    icon: BarChart3,
    permission: 'ViewReports',
    submenu: [
      { name: 'Báo cáo tổng hợp', href: '/reports', icon: TrendingUp, permission: 'ViewReports' },
      { name: 'Báo cáo hủy hàng', href: '/cancelled-orders-report', icon: FileX, permission: 'ViewReports' }
    ]
  },
  { name: 'Nhân viên', href: '/staff', icon: UserCheck, permission: 'ViewStaff' },
  { name: 'Quản lý cửa hàng', href: '/store-management', icon: Store, permission: 'ViewSettings' },
  { name: 'Cài đặt', href: '/settings', icon: Settings, permission: 'ViewSettings' },
  { name: 'Quản lý dữ liệu', href: '/data-management', icon: Database, permission: 'ViewDataManagement' },
];

export function Sidebar() {
  const [location] = useLocation();
  const { hasPermission } = useAuth();
  const [expandedMenus, setExpandedMenus] = useState<string[]>(['Sản phẩm']);
  const [isHovered, setIsHovered] = useState(false);

  // Filter navigation items based on permissions
  const visibleNavigation = navigation.filter(item => 
    !item.permission || hasPermission(item.permission)
  );

  const toggleMenu = (menuName: string) => {
    setExpandedMenus(prev => 
      prev.includes(menuName) 
        ? prev.filter(name => name !== menuName)
        : [...prev, menuName]
    );
  };

  return (
    <>
      {/* Trigger area for hover when sidebar is hidden */}
      <div 
        className={cn(
          "fixed left-0 top-0 h-full w-4 z-40 transition-opacity duration-200",
          isHovered ? "opacity-0" : "opacity-100"
        )}
        onMouseEnter={() => setIsHovered(true)}
      />
      
      <aside 
        className={cn(
          "bg-white shadow-lg border-r border-gray-200 transition-all duration-300 ease-in-out fixed left-0 top-0 h-full z-50",
          isHovered ? "w-64 translate-x-0" : "w-16 -translate-x-48"
        )}
        data-testid="sidebar"
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
      <div className={cn("p-6 border-b border-gray-200", !isHovered && "px-3")}>
        <div className="flex items-center space-x-3">
          <div className="w-10 h-10 bg-primary rounded-lg flex items-center justify-center flex-shrink-0">
            <ScanBarcode className="text-white text-lg" />
          </div>
          {isHovered && (
            <div className="transition-opacity duration-200">
              <h1 className="text-xl font-bold text-gray-900">PinkWish Shop</h1>
              <p className="text-sm text-gray-500"></p>
            </div>
          )}
        </div>
      </div>
      
      <nav className={cn("p-4 space-y-2", !isHovered && "px-2")}>
        <div className="space-y-1">
          {visibleNavigation.map((item) => {
            const Icon = item.icon;
            
            // Handle menu items with submenu
            if (item.submenu) {
              const isExpanded = expandedMenus.includes(item.name);
              // Filter submenu items based on permissions
              const visibleSubmenu = item.submenu.filter(subitem => 
                !subitem.permission || hasPermission(subitem.permission)
              );
              
              // Don't show menu if no visible submenu items
              if (visibleSubmenu.length === 0) return null;
              
              const hasActiveSubmenu = visibleSubmenu.some(subitem => location === subitem.href);
              
              return (
                <div key={item.name}>
                  <div
                    onClick={() => isHovered && toggleMenu(item.name)}
                    className={cn(
                      "flex items-center justify-between rounded-lg font-medium transition-colors cursor-pointer",
                      isHovered ? "px-4 py-3" : "px-2 py-3 justify-center",
                      hasActiveSubmenu
                        ? "text-primary bg-blue-50"
                        : "text-gray-600 hover:bg-gray-50"
                    )}
                    data-testid={`nav-${item.name.toLowerCase()}`}
                    title={!isHovered ? item.name : undefined}
                  >
                    <div className={cn("flex items-center", isHovered ? "space-x-3" : "justify-center")}>
                      <Icon className="w-5 h-5 flex-shrink-0" />
                      {isHovered && <span>{item.name}</span>}
                    </div>
                    {isHovered && (isExpanded ? (
                      <ChevronDown className="w-4 h-4" />
                    ) : (
                      <ChevronRight className="w-4 h-4" />
                    ))}
                  </div>
                  
                  {isHovered && isExpanded && (
                    <div className="ml-6 mt-1 space-y-1">
                      {visibleSubmenu.map((subitem) => {
                        const isActive = location === subitem.href;
                        const SubIcon = subitem.icon;
                        
                        return (
                          <Link key={subitem.name} href={subitem.href}>
                            <div
                              className={cn(
                                "flex items-center space-x-3 px-4 py-2 rounded-lg text-sm font-medium transition-colors cursor-pointer",
                                isActive
                                  ? "text-primary bg-blue-50"
                                  : "text-gray-600 hover:bg-gray-50"
                              )}
                              data-testid={`nav-${subitem.href.slice(1)}`}
                            >
                              <SubIcon className="w-4 h-4" />
                              <span>{subitem.name}</span>
                            </div>
                          </Link>
                        );
                      })}
                    </div>
                  )}
                </div>
              );
            }
            
            // Handle regular menu items
            const isActive = location === item.href;
            
            return (
              <Link key={item.name} href={item.href}>
                <div
                  className={cn(
                    "flex items-center rounded-lg font-medium transition-colors cursor-pointer",
                    isHovered ? "space-x-3 px-4 py-3" : "px-2 py-3 justify-center",
                    isActive
                      ? "text-primary bg-blue-50"
                      : "text-gray-600 hover:bg-gray-50"
                  )}
                  data-testid={`nav-${item.href.slice(1) || 'dashboard'}`}
                  title={!isHovered ? item.name : undefined}
                >
                  <Icon className="w-5 h-5 flex-shrink-0" />
                  {isHovered && <span>{item.name}</span>}
                </div>
              </Link>
            );
          })}
        </div>
      </nav>
    </aside>
    </>
  );
}
