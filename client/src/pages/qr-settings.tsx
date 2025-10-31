import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiRequest } from "../lib/utils";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { QrCode, Eye, TestTube } from "lucide-react";

export type QRSettings = {
  bankCode: string;
  bankAccountNumber: string;
  bankAccountHolder: string;
  bankName: string;
  bankBranch?: string;
  qrProvider: string;
  vietQRClientId?: string;
  vietQRApiKey?: string;
  vnPayApiKey?: string;
  vnPaySecretKey?: string;
  qrTemplate: string;
  isEnabled: boolean;
  defaultDescription?: string;
};

export function QRSettings() {
  const queryClient = useQueryClient();
  const { data: config } = useQuery<QRSettings | null>({
    queryKey: ["/api/QRSettings"],
    queryFn: async () => {
      const res = await apiRequest("/api/QRSettings", { method: "GET" });
      if (res.status === 404) return null;
      return typeof res === "string" ? JSON.parse(res) : res;
    },
  });

  const [form, setForm] = useState<QRSettings>({
    bankCode: "",
    bankAccountNumber: "",
    bankAccountHolder: "",
    bankName: "",
    bankBranch: "",
    qrProvider: "vietqr",
    vietQRClientId: "",
    vietQRApiKey: "",
    vnPayApiKey: "",
    vnPaySecretKey: "",
    qrTemplate: "compact",
    isEnabled: true,
    defaultDescription: "Thanh toan hoa don",
  });

  const [hasChanges, setHasChanges] = useState(false);
  const [showTestResult, setShowTestResult] = useState<any>(null);

  useEffect(() => {
    if (config) {
      setForm(config);
      setHasChanges(false);
    }
  }, [config]);

  const mutation = useMutation({
    mutationFn: async (data: QRSettings) => {
      const res = await apiRequest("/api/QRSettings", { 
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
      });
      return typeof res === "string" ? JSON.parse(res) : res;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/QRSettings"] });
      setHasChanges(false);
      alert("Đã lưu cấu hình QR thành công!");
    },
    onError: (error: any) => {
      alert(`Lỗi khi lưu: ${error.message}`);
    },
  });

  const testMutation = useMutation({
    mutationFn: async () => {
      const res = await apiRequest("/api/QRSettings/test", { method: "GET" });
      return typeof res === "string" ? JSON.parse(res) : res;
    },
    onSuccess: (data) => {
      setShowTestResult(data);
    },
    onError: (error: any) => {
      setShowTestResult({ error: error.message });
    },
  });

  function handleChange(e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
    const { name, value, type } = e.target;
    const checked = (e.target as HTMLInputElement).checked;
    
    setForm(prev => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value
    }));
    setHasChanges(true);
  }

  const bankCodes = [
    { value: "970436", label: "Vietcombank", fullName: "Ngân hàng TMCP Ngoại thương Việt Nam" },
    { value: "970415", label: "Vietinbank", fullName: "Ngân hàng TMCP Công Thương Việt Nam" },
    { value: "970422", label: "MB Bank", fullName: "Ngân hàng TMCP Quân đội" },
    { value: "970418", label: "BIDV", fullName: "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam" },
    { value: "970405", label: "Agribank", fullName: "Ngân hàng Nông nghiệp và Phát triển Nông thôn Việt Nam" },
    { value: "970448", label: "OCB", fullName: "Ngân hàng TMCP Phương Đông" },
    { value: "970432", label: "VPBank", fullName: "Ngân hàng TMCP Việt Nam Thịnh vượng" },
    { value: "970407", label: "Techcombank", fullName: "Ngân hàng TMCP Kỹ thương Việt Nam" },
    { value: "970403", label: "Sacombank", fullName: "Ngân hàng TMCP Sài Gòn Thương tín" },
    { value: "970416", label: "ACB Bank", fullName: "Ngân hàng TMCP Á Châu" },
    { value: "970419", label: "NCB Bank", fullName: "Ngân hàng TMCP Quốc dân" },
  ];

  // Tự động điền tên ngân hàng khi chọn bank code
  const handleBankCodeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const selectedBankCode = e.target.value;
    const selectedBank = bankCodes.find(bank => bank.value === selectedBankCode);
    
    setForm(prev => ({
      ...prev,
      bankCode: selectedBankCode,
      bankName: selectedBank ? selectedBank.fullName : ""
    }));
    setHasChanges(true);
  };

  // Tạo preview QR URL
  const getPreviewQRUrl = () => {
    if (!form.bankCode || !form.bankAccountNumber || !form.bankAccountHolder) {
      return null;
    }
    
    const template = form.qrTemplate || "compact";
    const accountName = encodeURIComponent(form.bankAccountHolder);
    return `https://api.vietqr.io/image/${form.bankCode}-${form.bankAccountNumber}-${template}.jpg?accountName=${accountName}&amount=100000`;
  };

  return (
    <Card>
      <CardContent className="p-6">
        <form onSubmit={e => { e.preventDefault(); mutation.mutate(form); }} className="space-y-8">
          <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
            <QrCode className="w-5 h-5" /> Cài đặt QR Code
          </h2>

          {/* Bật/tắt QR */}
          <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
            <div>
              <div className="font-semibold">Kích hoạt QR Code</div>
              <div className="text-gray-500 text-sm">Cho phép thanh toán qua mã QR</div>
            </div>
            <input 
              type="checkbox" 
              name="isEnabled" 
              checked={form.isEnabled} 
              onChange={handleChange} 
              className="w-10 h-5 accent-blue-600" 
            />
          </div>

          {/* Thông tin ngân hàng */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block font-medium mb-2">Ngân hàng *</label>
              <select
                name="bankCode"
                value={form.bankCode}
                onChange={handleBankCodeChange}
                className="w-full border rounded px-3 py-2"
                required
              >
                <option value="">Chọn ngân hàng</option>
                {bankCodes.map(bank => (
                  <option key={bank.value} value={bank.value}>{bank.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block font-medium mb-2">Tên ngân hàng</label>
              <input
                type="text"
                name="bankName"
                value={form.bankName}
                onChange={handleChange}
                className="w-full border rounded px-3 py-2"
                placeholder="Ví dụ: Ngân hàng TMCP Công Thương Việt Nam"
                readOnly
              />
              <p className="text-xs text-gray-500 mt-1">Tự động điền theo ngân hàng đã chọn</p>
            </div>

            <div>
              <label className="block font-medium mb-2">Số tài khoản *</label>
              <input
                type="text"
                name="bankAccountNumber"
                value={form.bankAccountNumber}
                onChange={handleChange}
                className="w-full border rounded px-3 py-2"
                placeholder="Nhập số tài khoản"
                required
              />
            </div>

            <div>
              <label className="block font-medium mb-2">Tên chủ tài khoản *</label>
              <input
                type="text"
                name="bankAccountHolder"
                value={form.bankAccountHolder}
                onChange={handleChange}
                className="w-full border rounded px-3 py-2"
                placeholder="Nhập tên chủ tài khoản"
                required
              />
            </div>

            <div className="md:col-span-2">
              <label className="block font-medium mb-2">Chi nhánh</label>
              <input
                type="text"
                name="bankBranch"
                value={form.bankBranch || ""}
                onChange={handleChange}
                className="w-full border rounded px-3 py-2"
                placeholder="Chi nhánh ngân hàng (tùy chọn)"
              />
            </div>
          </div>

          {/* Cấu hình QR */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block font-medium mb-2">Nhà cung cấp QR</label>
              <select
                name="qrProvider"
                value={form.qrProvider}
                onChange={handleChange}
                className="w-full border rounded px-3 py-2"
              >
                <option value="vietqr">VietQR</option>
                <option value="vnpayqr">VNPay QR</option>
              </select>
            </div>

            <div>
              <label className="block font-medium mb-2">Template QR</label>
              <select
                name="qrTemplate"
                value={form.qrTemplate}
                onChange={handleChange}
                className="w-full border rounded px-3 py-2"
              >
                <option value="compact">Compact</option>
                <option value="print">Print</option>
                <option value="qr_only">QR Only</option>
              </select>
            </div>

            <div className="md:col-span-2">
              <label className="block font-medium mb-2">Nội dung mặc định</label>
              <input
                type="text"
                name="defaultDescription"
                value={form.defaultDescription || ""}
                onChange={handleChange}
                className="w-full border rounded px-3 py-2"
                placeholder="Nội dung hiển thị trên QR"
              />
            </div>
          </div>

          {/* Preview QR Code */}
          {form.bankCode && form.bankAccountNumber && form.bankAccountHolder && (
            <div className="border rounded-lg p-4 bg-green-50">
              <h3 className="font-semibold mb-4 flex items-center gap-2">
                <Eye className="w-5 h-5" /> Xem trước QR Code
              </h3>
              <div className="flex flex-col md:flex-row gap-4 items-center">
                <div className="text-center">
                  <img 
                    src={getPreviewQRUrl() || ""}
                    alt="Preview QR Code" 
                    className="w-40 h-40 border rounded-lg mx-auto mb-2"
                    onError={(e) => {
                      e.currentTarget.src = "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjIwMCIgdmlld0JveD0iMCAwIDIwMCAyMDAiIGZpbGw9Im5vbmUiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+CjxyZWN0IHdpZHRoPSIyMDAiIGhlaWdodD0iMjAwIiBmaWxsPSIjRjNGNEY2Ii8+Cjx0ZXh0IHg9IjEwMCIgeT0iMTAwIiBmb250LWZhbWlseT0iQXJpYWwiIGZvbnQtc2l6ZT0iMTQiIGZpbGw9IiM2QjczODAiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGR5PSIwLjNlbSI+TG9hZGluZy4uLjwvdGV4dD4KPC9zdmc+";
                    }}
                  />
                  <p className="text-sm text-green-600 font-medium print:text-black">QR Code mẫu - 100,000₫</p>
                </div>
                <div className="flex-1">
                  <div className="space-y-2">
                    <p><span className="font-medium">Ngân hàng:</span> {form.bankName}</p>
                    <p><span className="font-medium">Số tài khoản:</span> {form.bankAccountNumber}</p>
                    <p><span className="font-medium">Chủ tài khoản:</span> {form.bankAccountHolder}</p>
                    <p><span className="font-medium">Template:</span> {form.qrTemplate}</p>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* API Configuration cho VietQR */}
          {form.qrProvider === "vietqr" && (
            <div className="border rounded-lg p-4 bg-blue-50">
              <h3 className="font-semibold mb-4">Cấu hình VietQR API</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block font-medium mb-2">Client ID</label>
                  <input
                    type="text"
                    name="vietQRClientId"
                    value={form.vietQRClientId || ""}
                    onChange={handleChange}
                    className="w-full border rounded px-3 py-2"
                    placeholder="VietQR Client ID (để trống dùng mặc định)"
                  />
                </div>
                <div>
                  <label className="block font-medium mb-2">API Key</label>
                  <input
                    type="text"
                    name="vietQRApiKey"
                    value={form.vietQRApiKey || ""}
                    onChange={handleChange}
                    className="w-full border rounded px-3 py-2"
                    placeholder="VietQR API Key (để trống dùng mặc định)"
                  />
                </div>
              </div>
            </div>
          )}

          {/* API Configuration cho VNPay */}
          {form.qrProvider === "vnpayqr" && (
            <div className="border rounded-lg p-4 bg-orange-50">
              <h3 className="font-semibold mb-4">Cấu hình VNPay QR API</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block font-medium mb-2">API Key</label>
                  <input
                    type="text"
                    name="vnPayApiKey"
                    value={form.vnPayApiKey || ""}
                    onChange={handleChange}
                    className="w-full border rounded px-3 py-2"
                    placeholder="VNPay API Key"
                  />
                </div>
                <div>
                  <label className="block font-medium mb-2">Secret Key</label>
                  <input
                    type="password"
                    name="vnPaySecretKey"
                    value={form.vnPaySecretKey || ""}
                    onChange={handleChange}
                    className="w-full border rounded px-3 py-2"
                    placeholder="VNPay Secret Key"
                  />
                </div>
              </div>
            </div>
          )}

          {/* Test Result */}
          {showTestResult && (
            <div className={`border rounded-lg p-4 ${showTestResult.error ? "bg-red-50 border-red-200" : "bg-green-50 border-green-200"}`}>
              <h3 className="font-semibold mb-2">Kết quả kiểm tra</h3>
              {showTestResult.error ? (
                <p className="text-red-600">{showTestResult.error}</p>
              ) : (
                <div className="text-green-600">
                  <p>✓ {showTestResult.message}</p>
                  <p>Provider: {showTestResult.provider}</p>
                  <p>Ngân hàng: {showTestResult.bankName}</p>
                  <p>Chủ tài khoản: {showTestResult.accountHolder}</p>
                </div>
              )}
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex flex-col sm:flex-row gap-4 justify-between items-center pt-2">
            <div className="flex gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => testMutation.mutate()}
                disabled={testMutation.isPending}
                className="flex items-center gap-2"
              >
                <TestTube className="w-4 h-4" />
                {testMutation.isPending ? "Đang kiểm tra..." : "Kiểm tra cấu hình"}
              </Button>
            </div>

            <div className="flex gap-2 items-center">
              {hasChanges && (
                <span className="text-orange-600 text-sm flex items-center gap-1">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
                  </svg>
                  Có thay đổi chưa lưu
                </span>
              )}
              
              <Button 
                type="submit" 
                disabled={mutation.isPending || !hasChanges}
                className={hasChanges ? "bg-orange-600 hover:bg-orange-700" : ""}
              >
                {mutation.isPending ? (
                  <>
                    <svg className="animate-spin w-4 h-4 mr-2" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Đang lưu...
                  </>
                ) : (
                  <>
                    <svg xmlns="http://www.w3.org/2000/svg" className="w-5 h-5 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                    Lưu cấu hình
                  </>
                )}
              </Button>
            </div>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}