import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { Camera, Smartphone, Monitor, HelpCircle, CheckCircle, AlertTriangle } from "lucide-react";

interface CameraPermissionGuideProps {
  isOpen: boolean;
  onClose: () => void;
}

export function CameraPermissionGuide({ isOpen, onClose }: CameraPermissionGuideProps) {
  const [selectedBrowser, setSelectedBrowser] = useState<string>("");

  const getBrowserInfo = () => {
    const userAgent = navigator.userAgent.toLowerCase();
    
    if (userAgent.includes('chrome')) {
      return { name: 'Chrome', icon: '🌐' };
    } else if (userAgent.includes('firefox')) {
      return { name: 'Firefox', icon: '🦊' };
    } else if (userAgent.includes('safari')) {
      return { name: 'Safari', icon: '🧭' };
    } else if (userAgent.includes('edge')) {
      return { name: 'Edge', icon: '🌊' };
    } else {
      return { name: 'Browser', icon: '🌐' };
    }
  };

  const browserInfo = getBrowserInfo();
  const isMobile = /android|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(navigator.userAgent);

  const chromeSteps = [
    {
      icon: "🔒",
      title: "Nhấn vào biểu tượng khóa",
      description: "Tìm biểu tượng khóa 🔒 hoặc camera 📷 bên trái thanh địa chỉ"
    },
    {
      icon: "📷", 
      title: "Chọn Camera",
      description: "Nhấn vào 'Camera' và chọn 'Cho phép' (Allow)"
    },
    {
      icon: "🔄",
      title: "Làm mới trang",
      description: "Nhấn F5 hoặc nút refresh để áp dụng thay đổi"
    }
  ];

  const safariSteps = [
    {
      icon: "⚙️",
      title: "Mở Safari Settings",
      description: "Nhấn Safari > Preferences > Websites > Camera"
    },
    {
      icon: "✅",
      title: "Cho phép Camera",
      description: "Tìm website hiện tại và đổi thành 'Allow'"
    },
    {
      icon: "🔄",
      title: "Reload trang",
      description: "Quay lại trang và làm mới"
    }
  ];

  const firefoxSteps = [
    {
      icon: "🔒",
      title: "Nhấn vào Shield icon",
      description: "Tìm biểu tượng shield bên trái thanh địa chỉ"
    },
    {
      icon: "📷",
      title: "Camera Permissions", 
      description: "Nhấn vào 'Camera' và chọn 'Allow'"
    },
    {
      icon: "🔄",
      title: "Refresh page",
      description: "Làm mới trang để áp dụng"
    }
  ];

  const getStepsForBrowser = () => {
    switch (browserInfo.name) {
      case 'Safari':
        return safariSteps;
      case 'Firefox':
        return firefoxSteps;
      default:
        return chromeSteps;
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Camera className="w-5 h-5 text-blue-600" />
            Hướng dẫn cấp quyền Camera
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          {/* Browser Detection */}
          <div className="flex items-center gap-2 p-3 bg-blue-50 rounded-lg">
            <span className="text-2xl">{browserInfo.icon}</span>
            <div>
              <p className="font-medium">Trình duyệt hiện tại: {browserInfo.name}</p>
              <p className="text-sm text-gray-600">
                {isMobile ? "📱 Mobile Device" : "💻 Desktop Device"}
              </p>
            </div>
          </div>

          {/* Quick Checks */}
          <div className="space-y-3">
            <h3 className="font-medium flex items-center gap-2">
              <CheckCircle className="w-4 h-4 text-green-600" />
              Kiểm tra nhanh
            </h3>
            
            <div className="space-y-2 text-sm">
              <div className="flex items-center gap-2">
                <Badge variant={window.location.protocol === 'https:' || window.location.hostname === 'localhost' ? 'default' : 'destructive'} className="text-xs">
                  {window.location.protocol === 'https:' || window.location.hostname === 'localhost' ? '✅ HTTPS/Localhost' : '❌ HTTP'}
                </Badge>
                <span className="text-gray-600">Camera cần HTTPS hoặc localhost</span>
              </div>
              
              <div className="flex items-center gap-2">
                <Badge variant={navigator.mediaDevices ? 'default' : 'destructive'} className="text-xs">
                  {navigator.mediaDevices ? '✅ MediaDevices' : '❌ Không hỗ trợ'}
                </Badge>
                <span className="text-gray-600">Trình duyệt hỗ trợ camera API</span>
              </div>
            </div>
          </div>

          {/* Step by step guide */}
          <div className="space-y-3">
            <h3 className="font-medium flex items-center gap-2">
              <HelpCircle className="w-4 h-4 text-blue-600" />
              Hướng dẫn chi tiết cho {browserInfo.name}
            </h3>
            
            <div className="space-y-3">
              {getStepsForBrowser().map((step, index) => (
                <div key={index} className="flex gap-3 p-3 border rounded-lg">
                  <div className="flex-shrink-0">
                    <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center text-sm font-medium text-blue-600">
                      {index + 1}
                    </div>
                  </div>
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <span className="text-lg">{step.icon}</span>
                      <h4 className="font-medium">{step.title}</h4>
                    </div>
                    <p className="text-sm text-gray-600">{step.description}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Troubleshooting */}
          <div className="space-y-3">
            <h3 className="font-medium flex items-center gap-2">
              <AlertTriangle className="w-4 h-4 text-orange-600" />
              Vẫn không hoạt động?
            </h3>
            
            <div className="space-y-2 text-sm">
              <div className="p-3 bg-orange-50 rounded-lg">
                <p className="font-medium mb-2">Thử các cách sau:</p>
                <ul className="space-y-1 list-disc list-inside text-gray-700">
                  <li>Đóng các ứng dụng camera khác (Zoom, Skype, v.v.)</li>
                  <li>Kiểm tra camera có hoạt động với ứng dụng khác không</li>
                  <li>Thử trình duyệt khác (Chrome, Firefox, Safari)</li>
                  <li>Khởi động lại trình duyệt</li>
                  <li>Kiểm tra antivirus có chặn camera không</li>
                </ul>
              </div>
            </div>
          </div>

          {/* Alternative */}
          <div className="p-3 bg-green-50 rounded-lg">
            <p className="font-medium text-green-800 mb-1">💡 Giải pháp thay thế</p>
            <p className="text-sm text-green-700">
              Nếu camera không hoạt động, bạn vẫn có thể nhập mã vạch thủ công bằng bàn phím hoặc sử dụng máy quét mã vạch chuyên dụng.
            </p>
          </div>

          {/* Action buttons */}
          <div className="flex gap-2 pt-4">
            <Button 
              onClick={onClose}
              variant="outline"
              className="flex-1"
            >
              Đã hiểu
            </Button>
            <Button 
              onClick={() => window.location.reload()}
              className="flex-1"
            >
              🔄 Làm mới trang
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}