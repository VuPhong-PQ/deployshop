import { useEffect, useRef, useState } from "react";
import { BrowserMultiFormatReader, NotFoundException } from "@zxing/library";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Camera, CameraOff, X, Flashlight, FlashlightOff, HelpCircle } from "lucide-react";
import { CameraPermissionGuide } from "@/components/CameraPermissionGuide";
import { ManualBarcodeInput } from "@/components/ManualBarcodeInput";

interface BarcodeScannerProps {
  onScan: (code: string) => void;
  onClose: () => void;
  isOpen: boolean;
}

export function BarcodeScanner({ onScan, onClose, isOpen }: BarcodeScannerProps) {
  const [isScanning, setIsScanning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasFlash, setHasFlash] = useState(false);
  const [flashOn, setFlashOn] = useState(false);
  const [devices, setDevices] = useState<MediaDeviceInfo[]>([]);
  const [selectedDevice, setSelectedDevice] = useState<string>("");
  const [lastScanTime, setLastScanTime] = useState(0);
  const [showPermissionGuide, setShowPermissionGuide] = useState(false);
  const [retryCount, setRetryCount] = useState(0);
  const [isInitializing, setIsInitializing] = useState(false);
  const [showManualInput, setShowManualInput] = useState(false);
  
  const videoRef = useRef<HTMLVideoElement>(null);
  const codeReaderRef = useRef<BrowserMultiFormatReader | null>(null);
  const streamRef = useRef<MediaStream | null>(null);

  // Initialize scanner
  useEffect(() => {
    if (isOpen) {
      initializeScanner();
    } else {
      stopScanning();
    }
    
    return () => {
      stopScanning();
    };
  }, [isOpen]);

  // Helper function to get different video constraints for retry attempts
  const getVideoConstraints = (retryAttempt: number) => {
    const constraints = [
      // First attempt: High quality with back camera preference
      { 
        facingMode: "environment",
        width: { ideal: 1280 },
        height: { ideal: 720 }
      },
      // Second attempt: Medium quality with back camera
      { 
        facingMode: "environment",
        width: { ideal: 640 },
        height: { ideal: 480 }
      },
      // Third attempt: Basic quality, any camera
      { 
        width: { ideal: 640 },
        height: { ideal: 480 }
      },
      // Fourth attempt: Minimal constraints
      {}
    ];
    
    return constraints[Math.min(retryAttempt, constraints.length - 1)];
  };

  const initializeScanner = async () => {
    if (isInitializing) return; // Prevent multiple simultaneous initialization attempts
    
    try {
      setIsInitializing(true);
      setError(null);
      
      // Check if mediaDevices is supported
      if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw new Error('MEDIA_DEVICES_NOT_SUPPORTED');
      }
      
      // Try different video constraints based on retry count
      const videoConstraints = getVideoConstraints(retryCount);
      
      // Request camera permissions
      const stream = await navigator.mediaDevices.getUserMedia({ 
        video: videoConstraints
      });
      
      // Get available cameras
      const videoDevices = await navigator.mediaDevices.enumerateDevices();
      const cameras = videoDevices.filter(device => device.kind === 'videoinput');
      setDevices(cameras);
      
      // Prefer back camera if available
      const backCamera = cameras.find(camera => 
        camera.label.toLowerCase().includes('back') || 
        camera.label.toLowerCase().includes('rear') ||
        camera.label.toLowerCase().includes('environment')
      );
      
      if (backCamera) {
        setSelectedDevice(backCamera.deviceId);
      } else if (cameras.length > 0) {
        setSelectedDevice(cameras[0].deviceId);
      }
      
      // Check for flash capability
      const track = stream.getVideoTracks()[0];
      const capabilities = track.getCapabilities() as any;
      setHasFlash(!!(capabilities.torch || capabilities.fillLightMode));
      
      stream.getTracks().forEach(track => track.stop()); // Stop initial stream
      
      // Reset retry count on success
      setRetryCount(0);
      
      // Start scanning with selected device
      startScanning();
      
    } catch (err: any) {
      console.error('Camera initialization error:', err);
      
      let errorMessage = 'Không thể truy cập camera. Vui lòng kiểm tra quyền truy cập camera.';
      
      if (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError') {
        errorMessage = 'Quyền truy cập camera bị từ chối. Vui lòng:\n\n1. Nhấn vào biểu tượng khóa 🔒 bên cạnh URL\n2. Chọn "Cho phép" cho Camera\n3. Làm mới trang và thử lại';
      } else if (err.name === 'NotFoundError' || err.name === 'DevicesNotFoundError') {
        errorMessage = 'Không tìm thấy camera trên thiết bị này.\nVui lòng kiểm tra kết nối camera.';
      } else if (err.name === 'NotReadableError' || err.name === 'TrackStartError') {
        errorMessage = 'Camera đang được sử dụng bởi ứng dụng khác.\nVui lòng đóng các ứng dụng camera khác và thử lại.';
      } else if (err.name === 'OverConstrainedError' || err.name === 'ConstraintNotSatisfiedError') {
        errorMessage = 'Camera không hỗ trợ cấu hình yêu cầu.\nThử chuyển sang camera khác hoặc làm mới trang.';
      } else if (err.message === 'MEDIA_DEVICES_NOT_SUPPORTED') {
        errorMessage = 'Trình duyệt không hỗ trợ camera.\nVui lòng sử dụng Chrome, Firefox hoặc Safari phiên bản mới nhất.';
      } else if (window.location.protocol === 'http:' && window.location.hostname !== 'localhost') {
        errorMessage = 'Camera chỉ hoạt động trên HTTPS.\nVui lòng truy cập qua đường dẫn HTTPS.';
      }
      
      setError(errorMessage);
      
      // Auto-retry with different constraints if it's a constraint error
      if ((err.name === 'OverConstrainedError' || err.name === 'ConstraintNotSatisfiedError') && retryCount < 3) {
        setTimeout(() => {
          setRetryCount(prev => prev + 1);
          initializeScanner();
        }, 1000);
      }
    } finally {
      setIsInitializing(false);
    }
  };

  const startScanning = async () => {
    try {
      setIsScanning(true);
      setError(null);
      
      // Create code reader
      if (!codeReaderRef.current) {
        codeReaderRef.current = new BrowserMultiFormatReader();
      }
      
      const deviceId = selectedDevice || null;
      
      // Start decoding
      await codeReaderRef.current.decodeFromVideoDevice(
        deviceId,
        videoRef.current!,
        (result, error) => {
          if (result) {
            const now = Date.now();
            // Prevent duplicate scans within 2 seconds
            if (now - lastScanTime > 2000) {
              setLastScanTime(now);
              const code = result.getText();
              console.log('📷 Camera scanned barcode:', code);
              onScan(code);
              
              // Briefly pause scanning to prevent rapid duplicate scans
              setTimeout(() => {
                if (codeReaderRef.current && videoRef.current) {
                  // Continue scanning
                }
              }, 1000);
            }
          }
          
          if (error && !(error instanceof NotFoundException)) {
            console.error('Barcode scanning error:', error);
          }
        }
      );
      
    } catch (err) {
      console.error('Scanning error:', err);
      setError('Lỗi khi quét mã vạch. Vui lòng thử lại.');
      setIsScanning(false);
    }
  };

  const stopScanning = () => {
    if (codeReaderRef.current) {
      codeReaderRef.current.reset();
    }
    
    if (streamRef.current) {
      streamRef.current.getTracks().forEach(track => track.stop());
      streamRef.current = null;
    }
    
    setIsScanning(false);
    setFlashOn(false);
  };

  const toggleFlash = async () => {
    if (!hasFlash || !videoRef.current) return;
    
    try {
      const stream = videoRef.current.srcObject as MediaStream;
      if (stream) {
        const track = stream.getVideoTracks()[0];
        await track.applyConstraints({
          advanced: [{ torch: !flashOn } as any]
        });
        setFlashOn(!flashOn);
      }
    } catch (err) {
      console.error('Flash toggle error:', err);
      // Fallback - try different approach
      try {
        const stream = videoRef.current.srcObject as MediaStream;
        if (stream) {
          const track = stream.getVideoTracks()[0];
          await track.applyConstraints({
            advanced: [{ fillLightMode: flashOn ? 'off' : 'flash' } as any]
          });
          setFlashOn(!flashOn);
        }
      } catch (fallbackErr) {
        console.error('Flash fallback error:', fallbackErr);
      }
    }
  };

  const switchCamera = async () => {
    if (devices.length <= 1) return;
    
    const currentIndex = devices.findIndex(d => d.deviceId === selectedDevice);
    const nextIndex = (currentIndex + 1) % devices.length;
    const nextDevice = devices[nextIndex];
    
    setSelectedDevice(nextDevice.deviceId);
    
    // Restart scanning with new device
    stopScanning();
    setTimeout(() => {
      startScanning();
    }, 500);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 bg-black bg-opacity-95 flex items-center justify-center p-2 sm:p-4">
      <Card className="w-full max-w-md mx-auto bg-white max-h-[95vh] overflow-hidden">
        <CardContent className="p-0">
          {/* Header */}
          <div className="flex items-center justify-between p-4 border-b">
            <div className="flex items-center space-x-2">
              <Camera className="w-5 h-5 text-blue-600" />
              <span className="font-medium text-gray-900">Quét mã vạch</span>
              {isInitializing && (
                <Badge variant="secondary" className="bg-yellow-100 text-yellow-800">
                  Đang khởi tạo...
                </Badge>
              )}
              {isScanning && !isInitializing && (
                <Badge variant="secondary" className="bg-green-100 text-green-800">
                  Đang quét...
                </Badge>
              )}
              {retryCount > 0 && (
                <Badge variant="secondary" className="bg-blue-100 text-blue-800">
                  Thử lần {retryCount + 1}
                </Badge>
              )}
            </div>
            <Button variant="ghost" size="sm" onClick={onClose}>
              <X className="w-4 h-4" />
            </Button>
          </div>

          {/* Camera View */}
          <div className="relative bg-black aspect-square max-h-[60vh] overflow-hidden">
            {error ? (
              <div className="absolute inset-0 flex items-center justify-center text-white text-center p-4">
                <div className="max-w-sm">
                  <CameraOff className="w-12 h-12 mx-auto mb-4 text-red-400" />
                  <pre className="text-sm whitespace-pre-line leading-relaxed">{error}</pre>
                  <div className="mt-4 space-y-2">
                    <Button 
                      variant="outline" 
                      size="sm" 
                      className="bg-white text-black hover:bg-gray-100"
                      onClick={initializeScanner}
                    >
                      🔄 Thử lại
                    </Button>
                    <Button 
                      variant="outline" 
                      size="sm" 
                      className="bg-blue-600 text-white hover:bg-blue-700 ml-2"
                      onClick={() => setShowPermissionGuide(true)}
                    >
                      <HelpCircle className="w-4 h-4 mr-1" />
                      Hướng dẫn
                    </Button>
                    <Button 
                      variant="outline" 
                      size="sm" 
                      className="bg-green-600 text-white hover:bg-green-700 ml-2"
                      onClick={() => setShowManualInput(true)}
                    >
                      ⌨️ Nhập thủ công
                    </Button>
                  </div>
                </div>
              </div>
            ) : (
              <>
                <video
                  ref={videoRef}
                  className="w-full h-full object-cover"
                  playsInline
                  muted
                />
                
                {/* Scanning overlay */}
                <div className="absolute inset-0 flex items-center justify-center">
                  <div className="relative w-48 h-32 sm:w-64 sm:h-40 border-2 border-white border-dashed rounded-lg">
                    <div className="absolute top-0 left-0 w-6 h-6 border-t-4 border-l-4 border-blue-400 rounded-tl-lg"></div>
                    <div className="absolute top-0 right-0 w-6 h-6 border-t-4 border-r-4 border-blue-400 rounded-tr-lg"></div>
                    <div className="absolute bottom-0 left-0 w-6 h-6 border-b-4 border-l-4 border-blue-400 rounded-bl-lg"></div>
                    <div className="absolute bottom-0 right-0 w-6 h-6 border-b-4 border-r-4 border-blue-400 rounded-br-lg"></div>
                    
                    {/* Scanning line animation */}
                    <div className="absolute inset-x-0 top-1/2 h-0.5 bg-blue-400 animate-pulse"></div>
                  </div>
                </div>
                
                {/* Controls */}
                <div className="absolute bottom-4 left-0 right-0 flex justify-center space-x-4">
                  {/* Flash toggle */}
                  {hasFlash && (
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={toggleFlash}
                      className="bg-white bg-opacity-20 hover:bg-opacity-30 text-white border-white border-opacity-30"
                    >
                      {flashOn ? <FlashlightOff className="w-4 h-4" /> : <Flashlight className="w-4 h-4" />}
                    </Button>
                  )}
                  
                  {/* Camera switch */}
                  {devices.length > 1 && (
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={switchCamera}
                      className="bg-white bg-opacity-20 hover:bg-opacity-30 text-white border-white border-opacity-30"
                    >
                      <Camera className="w-4 h-4" />
                    </Button>
                  )}
                </div>
              </>
            )}
          </div>

          {/* Instructions */}
          <div className="p-4 text-center text-sm text-gray-600">
            <p>Đưa mã vạch vào khung hình để quét tự động</p>
            <p className="text-xs text-gray-500 mt-1">
              Hỗ trợ: Code 128, EAN, UPC, QR Code và nhiều định dạng khác
            </p>
            
            {/* Debug info in development */}
            {process.env.NODE_ENV === 'development' && (
              <details className="mt-3 text-left">
                <summary className="text-xs text-gray-400 cursor-pointer">Debug Info</summary>
                <div className="text-xs text-gray-400 mt-2 space-y-1">
                  <p>🌐 Protocol: {window.location.protocol}</p>
                  <p>📱 User Agent: {navigator.userAgent.includes('Mobile') ? 'Mobile' : 'Desktop'}</p>
                  <p>📷 MediaDevices: {navigator.mediaDevices ? '✅' : '❌'}</p>
                  <p>🔄 Retry Count: {retryCount}</p>
                  <p>📹 Devices: {devices.length}</p>
                  <p>🔦 Flash: {hasFlash ? '✅' : '❌'}</p>
                </div>
              </details>
            )}
          </div>
        </CardContent>
      </Card>
      
      {/* Camera Permission Guide */}
      <CameraPermissionGuide 
        isOpen={showPermissionGuide}
        onClose={() => setShowPermissionGuide(false)}
      />
      
      {/* Manual Barcode Input */}
      <ManualBarcodeInput
        isOpen={showManualInput}
        onClose={() => setShowManualInput(false)}
        onScan={onScan}
      />
    </div>
  );
}