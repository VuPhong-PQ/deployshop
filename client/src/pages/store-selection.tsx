import React from "react";
import { useAuth } from "@/contexts/auth-context";
import { useLocation } from "wouter";
import StoreSelector from "@/components/store-selector";
import { Card, CardContent } from "@/components/ui/card";
import { User, LogOut } from "lucide-react";
import { Button } from "@/components/ui/button";

export default function StoreSelectionPage() {
  const { user, logout } = useAuth();
  const [, navigate] = useLocation();

  const handleStoreSelected = () => {
    // Check for intended route before redirecting
    const intendedRoute = localStorage.getItem("intendedRoute");
    
    if (intendedRoute && intendedRoute !== "/login" && intendedRoute !== "/store-selection") {
      localStorage.removeItem("intendedRoute");
      navigate(intendedRoute);
    } else {
      // Default redirect to dashboard
      navigate("/dashboard");
    }
  };

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  if (!user) {
    navigate("/login");
    return null;
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="w-full max-w-6xl mx-auto">
        {/* Header với thông tin user */}
        <Card className="mb-6">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
                  <User className="w-5 h-5 text-blue-600" />
                </div>
                <div>
                  <h3 className="font-semibold">{user.fullName}</h3>
                  <p className="text-sm text-gray-600">
                    {user.username} • {user.roleName}
                  </p>
                </div>
              </div>
              
              <Button 
                variant="outline" 
                size="sm" 
                onClick={handleLogout}
                className="flex items-center gap-2"
              >
                <LogOut className="w-4 h-4" />
                Đăng xuất
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Store Selector */}
        <Card>
          <CardContent className="p-6">
            <StoreSelector 
              onStoreSelected={handleStoreSelected}
              showTitle={true}
            />
          </CardContent>
        </Card>

        {/* Footer */}
        <div className="text-center mt-6 text-sm text-gray-500">
          <p>RetailPoint POS • Hệ thống quản lý bán hàng đa cửa hàng</p>
        </div>
      </div>
    </div>
  );
}