import { useState, useEffect } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { useToast } from "@/hooks/use-toast";
import { queryClient, apiRequest } from "@/lib/queryClient";
import { FileText, Save, Settings, AlertCircle, CheckCircle, TestTube } from "lucide-react";
import { useLocation } from "wouter";

interface EInvoiceConfig {
  eInvoiceConfigId: number;
  isEnabled: boolean;
  provider: string;
  apiUrl: string;
  username?: string;
  password?: string;
  token?: string;
  companyCode?: string;
  defaultTemplate: string;
  defaultSymbol: string;
  autoIssue: boolean;
  autoSendEmail: boolean;
  autoSendSMS: boolean;
  companyTaxCode: string;
  companyName: string;
  companyAddress?: string;
  companyPhone?: string;
  companyEmail?: string;
  companyBankAccount?: string;
  companyBankName?: string;
  defaultTaxRate: string;
  emailTemplate?: string;
  smsTemplate?: string;
}

const providers = [
  { value: "VNPT", label: "VNPT" },
  { value: "Viettel", label: "Viettel" },
  { value: "FPT", label: "FPT" },
  { value: "MISA", label: "MISA" },
  { value: "Other", label: "Khác" }
];

const taxRates = [
  { value: "0%", label: "0%" },
  { value: "5%", label: "5%" },
  { value: "10%", label: "10%" },
  { value: "KCT", label: "Không chịu thuế (KCT)" },
  { value: "KKKNT", label: "Không kê khai không nộp thuế (KKKNT)" }
];

export default function EInvoiceSettings() {
  const { toast } = useToast();
  const [, navigate] = useLocation();
  
  const [config, setConfig] = useState<EInvoiceConfig>({
    eInvoiceConfigId: 0,
    isEnabled: false,
    provider: "VNPT",
    apiUrl: "",
    defaultTemplate: "01GTKT0/001",
    defaultSymbol: "C22TKT",
    autoIssue: false,
    autoSendEmail: false,
    autoSendSMS: false,
    companyTaxCode: "",
    companyName: "",
    defaultTaxRate: "10%"
  });

  // Fetch current config
  const { data: currentConfig, isLoading } = useQuery({
    queryKey: ["/api/EInvoice/config"],
    queryFn: async () => {
      return await apiRequest("/api/EInvoice/config", { method: "GET" });
    },
  });

  // Update config when data is loaded
  useEffect(() => {
    if (currentConfig) {
      setConfig(currentConfig);
    }
  }, [currentConfig]);

  // Save config mutation
  const saveConfigMutation = useMutation({
    mutationFn: async (data: EInvoiceConfig) => {
      return await apiRequest('/api/EInvoice/config', { 
        method: 'PUT', 
        body: JSON.stringify(data),
        headers: {
          'Content-Type': 'application/json'
        }
      });
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Cấu hình hóa đơn điện tử đã được lưu",
      });
      queryClient.invalidateQueries({ queryKey: ["/api/EInvoice/config"] });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Không thể lưu cấu hình",
        variant: "destructive",
      });
    },
  });

  const handleSave = () => {
    // Validate required fields
    if (config.isEnabled) {
      if (!config.companyTaxCode || !config.companyName || !config.apiUrl) {
        toast({
          title: "Lỗi",
          description: "Vui lòng điền đầy đủ thông tin bắt buộc",
          variant: "destructive",
        });
        return;
      }
    }

    saveConfigMutation.mutate(config);
  };

  const updateConfig = (field: keyof EInvoiceConfig, value: any) => {
    setConfig(prev => ({ ...prev, [field]: value }));
  };

  if (isLoading) {
    return (
      <AppLayout title="Cấu hình hóa đơn điện tử">
        <div className="flex items-center justify-center h-64">
          <div className="text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
            <p className="mt-2 text-gray-600">Đang tải cấu hình...</p>
          </div>
        </div>
      </AppLayout>
    );
  }

  return (
    <AppLayout title="Cấu hình hóa đơn điện tử">
      <div className="p-6 max-w-4xl mx-auto space-y-6">
        <div className="flex items-center gap-3">
          <FileText className="w-8 h-8 text-blue-600" />
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Cấu hình hóa đơn điện tử</h1>
            <p className="text-gray-600">Cấu hình hệ thống hóa đơn điện tử theo TT78/2022/TT-BTC</p>
          </div>
        </div>

        {/* Status Card */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              {config.isEnabled ? (
                <CheckCircle className="w-5 h-5 text-green-600" />
              ) : (
                <AlertCircle className="w-5 h-5 text-orange-600" />
              )}
              Trạng thái hóa đơn điện tử
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium">
                  {config.isEnabled ? "Đã kích hoạt" : "Chưa kích hoạt"}
                </p>
                <p className="text-sm text-gray-600">
                  {config.isEnabled 
                    ? "Hệ thống hóa đơn điện tử đang hoạt động" 
                    : "Bật để sử dụng chức năng hóa đơn điện tử"
                  }
                </p>
              </div>
              <Switch
                checked={config.isEnabled}
                onCheckedChange={(checked) => updateConfig('isEnabled', checked)}
              />
            </div>
          </CardContent>
        </Card>

        {/* Provider Configuration */}
        <Card>
          <CardHeader>
            <CardTitle>Cấu hình nhà cung cấp</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Nhà cung cấp *
                </label>
                <Select value={config.provider} onValueChange={(value) => updateConfig('provider', value)}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {providers.map((provider) => (
                      <SelectItem key={provider.value} value={provider.value}>
                        {provider.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  URL API *
                </label>
                <Input
                  value={config.apiUrl || ""}
                  onChange={(e) => updateConfig('apiUrl', e.target.value)}
                  placeholder="https://api.example.com"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Tài khoản
                </label>
                <Input
                  value={config.username || ""}
                  onChange={(e) => updateConfig('username', e.target.value)}
                  placeholder="Tên tài khoản"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Mật khẩu
                </label>
                <Input
                  type="password"
                  value={config.password || ""}
                  onChange={(e) => updateConfig('password', e.target.value)}
                  placeholder="Mật khẩu"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Token xác thực
                </label>
                <Input
                  value={config.token || ""}
                  onChange={(e) => updateConfig('token', e.target.value)}
                  placeholder="Token API"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Mã công ty
                </label>
                <Input
                  value={config.companyCode || ""}
                  onChange={(e) => updateConfig('companyCode', e.target.value)}
                  placeholder="Mã công ty trên hệ thống"
                />
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Company Information */}
        <Card>
          <CardHeader>
            <CardTitle>Thông tin công ty (Người bán)</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Mã số thuế *
                </label>
                <Input
                  value={config.companyTaxCode || ""}
                  onChange={(e) => updateConfig('companyTaxCode', e.target.value)}
                  placeholder="0123456789"
                  maxLength={13}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Tên công ty *
                </label>
                <Input
                  value={config.companyName || ""}
                  onChange={(e) => updateConfig('companyName', e.target.value)}
                  placeholder="Tên công ty"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Địa chỉ công ty
              </label>
              <Input
                value={config.companyAddress || ""}
                onChange={(e) => updateConfig('companyAddress', e.target.value)}
                placeholder="Địa chỉ đầy đủ"
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Số điện thoại
                </label>
                <Input
                  value={config.companyPhone || ""}
                  onChange={(e) => updateConfig('companyPhone', e.target.value)}
                  placeholder="0123456789"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Email
                </label>
                <Input
                  type="email"
                  value={config.companyEmail || ""}
                  onChange={(e) => updateConfig('companyEmail', e.target.value)}
                  placeholder="company@example.com"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Số tài khoản
                </label>
                <Input
                  value={config.companyBankAccount || ""}
                  onChange={(e) => updateConfig('companyBankAccount', e.target.value)}
                  placeholder="Số tài khoản ngân hàng"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Tên ngân hàng
                </label>
                <Input
                  value={config.companyBankName || ""}
                  onChange={(e) => updateConfig('companyBankName', e.target.value)}
                  placeholder="Tên ngân hàng"
                />
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Invoice Template */}
        <Card>
          <CardHeader>
            <CardTitle>Cấu hình mẫu hóa đơn</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Mẫu số hóa đơn
                </label>
                <Input
                  value={config.defaultTemplate}
                  onChange={(e) => updateConfig('defaultTemplate', e.target.value)}
                  placeholder="01GTKT0/001"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Ký hiệu hóa đơn
                </label>
                <Input
                  value={config.defaultSymbol}
                  onChange={(e) => updateConfig('defaultSymbol', e.target.value)}
                  placeholder="C22TKT"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Thuế suất mặc định
                </label>
                <Select value={config.defaultTaxRate} onValueChange={(value) => updateConfig('defaultTaxRate', value)}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {taxRates.map((rate) => (
                      <SelectItem key={rate.value} value={rate.value}>
                        {rate.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Automation Settings */}
        <Card>
          <CardHeader>
            <CardTitle>Cài đặt tự động</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-medium">Tự động phát hành</p>
                  <p className="text-sm text-gray-600">Tự động phát hành hóa đơn sau khi tạo</p>
                </div>
                <Switch
                  checked={config.autoIssue}
                  onCheckedChange={(checked) => updateConfig('autoIssue', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <p className="font-medium">Tự động gửi email</p>
                  <p className="text-sm text-gray-600">Gửi hóa đơn qua email cho khách hàng</p>
                </div>
                <Switch
                  checked={config.autoSendEmail}
                  onCheckedChange={(checked) => updateConfig('autoSendEmail', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <p className="font-medium">Tự động gửi SMS</p>
                  <p className="text-sm text-gray-600">Gửi thông báo hóa đơn qua SMS</p>
                </div>
                <Switch
                  checked={config.autoSendSMS}
                  onCheckedChange={(checked) => updateConfig('autoSendSMS', checked)}
                />
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Template Settings */}
        <Card>
          <CardHeader>
            <CardTitle>Template thông báo</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Template Email
              </label>
              <textarea
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                rows={4}
                value={config.emailTemplate || ""}
                onChange={(e) => updateConfig('emailTemplate', e.target.value)}
                placeholder="Kính gửi quý khách,&#10;&#10;Hóa đơn điện tử của quý khách đã được phát hành.&#10;&#10;Trân trọng!"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Template SMS
              </label>
              <textarea
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                rows={2}
                value={config.smsTemplate || ""}
                onChange={(e) => updateConfig('smsTemplate', e.target.value)}
                placeholder="Hóa đơn điện tử đã được phát hành. Xem tại: {link}"
              />
            </div>
          </CardContent>
        </Card>

        {/* Save Button */}
        <div className="flex justify-between items-center">
          <Button 
            onClick={() => navigate('/vnpt-test')}
            variant="outline"
            className="text-green-600 border-green-600 hover:bg-green-50"
          >
            <TestTube className="w-4 h-4 mr-2" />
            Test VNPT API
          </Button>
          
          <Button 
            onClick={handleSave}
            disabled={saveConfigMutation.isPending}
            className="bg-blue-600 hover:bg-blue-700"
          >
            <Save className="w-4 h-4 mr-2" />
            {saveConfigMutation.isPending ? "Đang lưu..." : "Lưu cấu hình"}
          </Button>
        </div>
      </div>
    </AppLayout>
  );
}