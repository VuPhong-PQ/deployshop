import { useState, useEffect } from "react";
import { useLocation } from "wouter";
import { useMutation } from "@tanstack/react-query";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { useToast } from "@/hooks/use-toast";
import { useAuth } from "@/contexts/auth-context";
import { LogIn, Eye, EyeOff, User, Lock } from "lucide-react";

interface LoginRequest {
  username: string;
  password: string;
}

interface LoginResponse {
  staffId: number;
  fullName: string;
  username: string;
  email?: string;
  roleId: number;
  roleName: string;
  permissions: string[];
  lastLogin?: string;
}

export default function Login() {
  const [, setLocation] = useLocation();
  const { login, isAuthenticated } = useAuth();
  const [formData, setFormData] = useState<LoginRequest>({
    username: "",
    password: "",
  });
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string>("");
  const { toast } = useToast();

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      setLocation("/");
    }
  }, [isAuthenticated, setLocation]);

  const loginMutation = useMutation({
    mutationFn: async (data: LoginRequest): Promise<LoginResponse> => {
      try {
        const response = await fetch("http://101.53.9.76:5273/api/staff/login", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(data),
        });

        if (!response.ok) {
          const errorText = await response.text();
          throw new Error(errorText || "Đăng nhập thất bại");
        }

        const result = await response.json();
        return result;
      } catch (error) {
        if (error instanceof Error) {
          throw error;
        }
        throw new Error("Đăng nhập thất bại");
      }
    },
    onSuccess: async (data) => {
      // Sử dụng context để lưu thông tin đăng nhập
      login(data);
      
      toast({
        title: "Đăng nhập thành công",
        description: `Chào mừng ${data.fullName}!`,
      });

      // Check for intended route
      const intendedRoute = localStorage.getItem("intendedRoute");
      
      // Check số lượng stores được assign để quyết định redirect
      try {
        const response = await fetch('http://101.53.9.76:5273/api/storeswitch/my-stores', {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
            'Username': data.username
          },
          credentials: 'include'
        });
        
        if (response.ok) {
          const stores = await response.json();
          
          if (stores.length === 0) {
            // Không có store nào - đi đến store selection để hiển thị thông báo
            localStorage.removeItem("intendedRoute"); // Clear intended route
            setLocation("/store-selection");
          } else if (stores.length === 1 && data.roleName !== "Admin") {
            // Chỉ có 1 store và không phải Admin - redirect to intended route or sales
            const store = stores[0];
            
            // Set current store trước khi redirect
            await fetch('http://101.53.9.76:5273/api/storeswitch/set-current', {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
                'Username': data.username
              },
              credentials: 'include',
              body: JSON.stringify({ storeId: store.storeId })
            });
            
            // Redirect to intended route or default to sales
            const targetRoute = intendedRoute && intendedRoute !== "/login" ? intendedRoute : "/sales";
            localStorage.removeItem("intendedRoute");
            setLocation(targetRoute);
          } else {
            // Nhiều stores hoặc là Admin - đi đến store selection (sẽ redirect về intended route sau khi chọn store)
            setLocation("/store-selection");
          }
        } else {
          // Lỗi API - fallback về store selection
          setLocation("/store-selection");
        }
      } catch (error) {
        console.error("Error checking stores:", error);
        // Lỗi - fallback về store selection
        setLocation("/store-selection");
      }
    },
    onError: (error: Error) => {
      setError(error.message);
      toast({
        title: "Đăng nhập thất bại",
        description: error.message,
        variant: "destructive",
      });
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (!formData.username.trim()) {
      setError("Vui lòng nhập tên đăng nhập");
      return;
    }

    if (!formData.password.trim()) {
      setError("Vui lòng nhập mật khẩu");
      return;
    }

    loginMutation.mutate(formData);
  };

  const handleInputChange = (field: keyof LoginRequest) => (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    setFormData((prev) => ({
      ...prev,
      [field]: e.target.value,
    }));
    // Clear error when user starts typing
    if (error) setError("");
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100 p-4">
      <div className="w-full max-w-md">
        <Card className="shadow-xl border-0">
          <CardHeader className="space-y-4 text-center">
            <div className="mx-auto w-16 h-16 bg-indigo-600 rounded-full flex items-center justify-center">
              <LogIn className="w-8 h-8 text-white" />
            </div>
            <div>
              <CardTitle className="text-2xl font-bold text-gray-900">
                Đăng nhập RetailPoint
              </CardTitle>
              <CardDescription className="text-gray-600">
                Nhập thông tin đăng nhập để truy cập hệ thống
              </CardDescription>
            </div>
          </CardHeader>

          <CardContent className="space-y-6">
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="username" className="text-sm font-medium text-gray-700">
                  Tên đăng nhập
                </Label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                  <Input
                    id="username"
                    type="text"
                    placeholder="Nhập tên đăng nhập"
                    value={formData.username}
                    onChange={handleInputChange("username")}
                    className="pl-10 h-11"
                    disabled={loginMutation.isPending}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="password" className="text-sm font-medium text-gray-700">
                  Mật khẩu
                </Label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                  <Input
                    id="password"
                    type={showPassword ? "text" : "password"}
                    placeholder="Nhập mật khẩu"
                    value={formData.password}
                    onChange={handleInputChange("password")}
                    className="pl-10 pr-10 h-11"
                    disabled={loginMutation.isPending}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600"
                    disabled={loginMutation.isPending}
                  >
                    {showPassword ? (
                      <EyeOff className="w-4 h-4" />
                    ) : (
                      <Eye className="w-4 h-4" />
                    )}
                  </button>
                </div>
              </div>

              <Button
                type="submit"
                className="w-full h-11 bg-indigo-600 hover:bg-indigo-700 text-white font-medium"
                disabled={loginMutation.isPending}
              >
                {loginMutation.isPending ? (
                  <div className="flex items-center justify-center">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                    Đang đăng nhập...
                  </div>
                ) : (
                  <>
                    <LogIn className="w-4 h-4 mr-2" />
                    Đăng nhập
                  </>
                )}
              </Button>
            </form>

            <div className="text-center text-sm text-gray-500">
              <p>Hệ thống quản lý bán hàng</p>
              <p className="font-medium">RetailPoint POS</p>
              
              <div className="mt-4 pt-4 border-t border-gray-200">
                <p className="text-xs text-gray-400">Vui lòng liên hệ Admin: <span className="font-medium">Vũ Phong</span></p>
                <p className="text-xs text-gray-400">ĐT: <span className="font-medium">0907 999 841</span></p>
                <p className="text-xs text-gray-400">Email: <span className="font-medium">vuphongpq@gmail.com</span></p>
                <p className="text-xs text-gray-400 mt-1">Xin cảm ơn</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}