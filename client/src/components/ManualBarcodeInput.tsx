import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Card, CardContent } from "@/components/ui/card";
import { Keyboard, Scan } from "lucide-react";

interface ManualBarcodeInputProps {
  isOpen: boolean;
  onClose: () => void;
  onScan: (code: string) => void;
}

export function ManualBarcodeInput({ isOpen, onClose, onScan }: ManualBarcodeInputProps) {
  const [barcodeValue, setBarcodeValue] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!barcodeValue.trim()) return;
    
    setIsSubmitting(true);
    try {
      await onScan(barcodeValue.trim());
      setBarcodeValue("");
      onClose();
    } catch (error) {
      console.error('Manual barcode input error:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSubmit(e);
    }
  };

  const commonBarcodes = [
    { label: "Coca Cola", code: "8901030805091" },
    { label: "Pepsi", code: "8901030750045" },
    { label: "Example 1", code: "1234567890123" },
    { label: "Example 2", code: "9876543210987" },
  ];

  if (!isOpen) return null;

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Keyboard className="w-5 h-5 text-blue-600" />
            Nhập mã vạch thủ công
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          <Card>
            <CardContent className="p-4">
              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium text-gray-700">
                    Mã vạch (EAN, UPC, Code 128, v.v.)
                  </label>
                  <Input
                    value={barcodeValue}
                    onChange={(e) => setBarcodeValue(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder="Nhập hoặc dán mã vạch..."
                    className="text-center text-lg font-mono tracking-wider"
                    autoFocus
                    disabled={isSubmitting}
                  />
                  <p className="text-xs text-gray-500 text-center">
                    Thông thường có 8-13 chữ số
                  </p>
                </div>

                <div className="flex gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={onClose}
                    className="flex-1"
                    disabled={isSubmitting}
                  >
                    Hủy
                  </Button>
                  <Button
                    type="submit"
                    disabled={!barcodeValue.trim() || isSubmitting}
                    className="flex-1"
                  >
                    {isSubmitting ? (
                      <>
                        <span className="animate-spin mr-2">⏳</span>
                        Đang xử lý...
                      </>
                    ) : (
                      <>
                        <Scan className="w-4 h-4 mr-2" />
                        Quét
                      </>
                    )}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>

          {/* Sample barcodes for testing */}
          <Card>
            <CardContent className="p-4">
              <h3 className="text-sm font-medium text-gray-700 mb-3">
                Mã vạch mẫu (cho test):
              </h3>
              <div className="grid grid-cols-1 gap-2">
                {commonBarcodes.map((item, index) => (
                  <Button
                    key={index}
                    variant="outline"
                    size="sm"
                    onClick={() => setBarcodeValue(item.code)}
                    className="justify-between text-xs"
                    disabled={isSubmitting}
                  >
                    <span>{item.label}</span>
                    <span className="font-mono text-gray-500">{item.code}</span>
                  </Button>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* Instructions */}
          <div className="text-xs text-gray-500 space-y-1">
            <p>💡 <strong>Mẹo:</strong> Bạn có thể:</p>
            <ul className="list-disc list-inside space-y-1 ml-2">
              <li>Sao chép/dán mã vạch từ nguồn khác</li>
              <li>Gõ trực tiếp bằng bàn phím</li>
              <li>Sử dụng máy quét mã vạch USB (tự động nhập)</li>
              <li>Nhấn Enter để quét nhanh</li>
            </ul>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}