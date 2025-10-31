import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiRequest } from "../lib/utils";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { CreditCard, QrCode, Wallet, Banknote, BanknoteIcon, Split, Archive } from "lucide-react";

export type PaymentMethodConfig = {
  enableCash: boolean;
  enableBankCard: boolean;
  enableQRCode: boolean;
  enableEWallet: boolean;
  enableBankTransfer: boolean;
  enablePartialPayment: boolean;
  enableDrawer: boolean;
  defaultMethod: string;
};

const paymentOptions = [
  { value: "cash", label: "Tiền mặt", icon: <BanknoteIcon className="w-4 h-4 inline" /> },
  { value: "bankcard", label: "Thẻ ngân hàng", icon: <CreditCard className="w-4 h-4 inline" /> },
  { value: "qrcode", label: "QR Code", icon: <QrCode className="w-4 h-4 inline" /> },
  { value: "ewallet", label: "Ví điện tử", icon: <Wallet className="w-4 h-4 inline" /> },
  { value: "banktransfer", label: "Chuyển khoản", icon: <Banknote className="w-4 h-4 inline" /> },
  { value: "partial", label: "Thanh toán một phần", icon: <Split className="w-4 h-4 inline" /> },
];

export function PaymentSettings() {
  const queryClient = useQueryClient();
  const { data: config } = useQuery<PaymentMethodConfig | null>({
    queryKey: ["/api/PaymentMethodConfig"],
    queryFn: async () => {
      const res = await apiRequest("/api/PaymentMethodConfig", { method: "GET" });
      if (res.status === 404) return null;
      return res;
    },
  });
  const [form, setForm] = useState<PaymentMethodConfig>({
    enableCash: true,
    enableBankCard: true,
    enableQRCode: true,
    enableEWallet: true,
    enableBankTransfer: true,
    enablePartialPayment: false,
    enableDrawer: true,
    defaultMethod: "cash",
  });
  const [hasChanges, setHasChanges] = useState(false);

  useEffect(() => {
    if (config) {
      setForm(config);
      setHasChanges(false);
    }
  }, [config]);

  const mutation = useMutation({
    mutationFn: async (data: PaymentMethodConfig) => {
      const res = await apiRequest("/api/PaymentMethodConfig", { 
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
      });
      return res;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/PaymentMethodConfig"] });
      queryClient.invalidateQueries({ queryKey: ["/api/PaymentMethodConfig/enabled"] });
      setHasChanges(false);
      
      // Dispatch event to notify other components about config change
      window.dispatchEvent(new CustomEvent('paymentConfigChanged'));
      
      alert("Đã lưu cấu hình thanh toán!");
    },
  });

  function handleChange(e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
    const { name, value, type } = e.target;
    const checked = (e.target as HTMLInputElement).checked;
    setForm(f => ({
      ...f,
      [name]: type === "checkbox" ? checked : value,
    }));
    setHasChanges(true);
  }
  return (
    <Card>
      <CardContent className="p-6">
        <form onSubmit={e => { e.preventDefault(); mutation.mutate(form); }} className="space-y-8">
          <h2 className="text-lg font-semibold mb-4 flex items-center gap-2"><CreditCard className="w-5 h-5" /> Phương thức thanh toán</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold flex items-center gap-2"><BanknoteIcon /> Tiền mặt</div>
                <div className="text-gray-500 text-sm">Thanh toán bằng tiền mặt</div>
              </div>
              <input type="checkbox" name="enableCash" checked={form.enableCash} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold flex items-center gap-2"><CreditCard /> Thẻ ngân hàng</div>
                <div className="text-gray-500 text-sm">Thẻ tín dụng, thẻ ghi nợ</div>
              </div>
              <input type="checkbox" name="enableBankCard" checked={form.enableBankCard} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold flex items-center gap-2"><QrCode /> QR Code</div>
                <div className="text-gray-500 text-sm">VietQR, VNPAY QR</div>
              </div>
              <input type="checkbox" name="enableQRCode" checked={form.enableQRCode} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold flex items-center gap-2"><Wallet /> Ví điện tử</div>
                <div className="text-gray-500 text-sm">MoMo, ZaloPay, ShopeePay</div>
              </div>
              <input type="checkbox" name="enableEWallet" checked={form.enableEWallet} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold flex items-center gap-2"><Banknote /> Chuyển khoản</div>
                <div className="text-gray-500 text-sm">Chuyển khoản ngân hàng</div>
              </div>
              <input type="checkbox" name="enableBankTransfer" checked={form.enableBankTransfer} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold flex items-center gap-2"><Split /> Thanh toán một phần</div>
                <div className="text-gray-500 text-sm">Cho phép thanh toán không đủ số tiền</div>
              </div>
              <input type="checkbox" name="enablePartialPayment" checked={form.enablePartialPayment} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between md:col-span-2">
              <div>
                <div className="font-semibold flex items-center gap-2"><Archive /> Ngăn kéo tiền</div>
                <div className="text-gray-500 text-sm">Tự động mở ngăn kéo khi thanh toán</div>
              </div>
              <input type="checkbox" name="enableDrawer" checked={form.enableDrawer} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
          </div>
          <div className="mt-8">
            <label className="block font-medium mb-2">Phương thức mặc định</label>
            <select name="defaultMethod" value={form.defaultMethod} onChange={handleChange} className="border rounded px-2 py-1 w-full">
              {paymentOptions.map(opt => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>
          <div className="flex justify-between items-center pt-2">
            {hasChanges && (
              <span className="text-orange-600 text-sm flex items-center gap-1">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
                </svg>
                Có thay đổi chưa lưu
              </span>
            )}
            {!hasChanges && (
              <span className="text-green-600 text-sm flex items-center gap-1">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
                </svg>
                Đã lưu
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
        </form>
      </CardContent>
    </Card>
  );
}
