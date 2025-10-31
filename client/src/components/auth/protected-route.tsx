import { useAuth } from "@/contexts/auth-context";
import { useLocation } from "wouter";
import { useEffect, useState } from "react";
import { AccessDenied } from "./access-denied";

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredPermission?: string;
}

export function ProtectedRoute({ children, requiredPermission }: ProtectedRouteProps) {
  const { isAuthenticated, hasPermission, user, currentStore, isLoading } = useAuth();
  const [location, setLocation] = useLocation();
  const [showAccessDenied, setShowAccessDenied] = useState(false);
  const [hasCheckedIntendedRoute, setHasCheckedIntendedRoute] = useState(false);

  useEffect(() => {
    // Don't do anything while still loading from localStorage
    if (isLoading) {
      console.log("⏳ Auth still loading from localStorage...");
      return;
    }
    
    console.log("🔍 ProtectedRoute check:", { isAuthenticated, user: !!user, currentStore: !!currentStore, location });

    if (!isAuthenticated) {
      // Store current location before redirecting to login
      if (location !== "/login") {
        localStorage.setItem("intendedRoute", location);
        console.log("🔄 Stored intended route:", location);
      }
      setLocation("/login");
      return;
    }

    // Skip store check for store-selection page
    if (location === "/store-selection") {
      if (requiredPermission && !hasPermission(requiredPermission)) {
        setShowAccessDenied(true);
        return;
      }
      setShowAccessDenied(false);
      return;
    }

    // Check if user needs to select store
    if (user && !currentStore) {
      // Store current location before redirecting to store selection
      if (location !== "/store-selection") {
        localStorage.setItem("intendedRoute", location);
        console.log("🔄 Stored intended route before store selection:", location);
      }
      
      // Đối với Admin hoặc user có nhiều stores - cần chọn store
      if (user.roleName === "Admin") {
        setLocation("/store-selection");
        return;
      }
      // Đối với staff không có currentStore - cũng cần đi store-selection để xem thông báo
      setLocation("/store-selection");
      return;
    }

    // Check for intended route after authentication and store selection are complete
    if (isAuthenticated && user && currentStore && !hasCheckedIntendedRoute) {
      const intendedRoute = localStorage.getItem("intendedRoute");
      console.log("🔍 Checking intended route:", intendedRoute, "current location:", location);
      
      if (intendedRoute && intendedRoute !== location && 
          intendedRoute !== "/login" && intendedRoute !== "/store-selection") {
        console.log("🎯 Redirecting to intended route:", intendedRoute);
        localStorage.removeItem("intendedRoute");
        setHasCheckedIntendedRoute(true);
        setLocation(intendedRoute);
        return;
      }
      setHasCheckedIntendedRoute(true);
    }

    if (requiredPermission && !hasPermission(requiredPermission)) {
      setShowAccessDenied(true);
      return;
    }

    setShowAccessDenied(false);
  }, [isAuthenticated, requiredPermission, hasPermission, setLocation, user, currentStore, location, hasCheckedIntendedRoute, isLoading]);

  // Show loading while auth data is being loaded from localStorage
  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  if (showAccessDenied) {
    return <AccessDenied />;
  }

  return <>{children}</>;
}