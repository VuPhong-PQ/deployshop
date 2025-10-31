import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiRequest } from "../lib/utils";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { AppLayout } from "@/components/layout/app-layout";
import { Store, Printer, CreditCard, Calculator, QrCode, FileText } from "lucide-react";
import { TaxSettings } from "./tax-settings";
import { PaymentSettings } from "./payment-settings";
import { PrintSettings } from "./print-settings";
import { QRSettings } from "./qr-settings";
import { useLocation } from "wouter";

type StoreInfo = {
  name: string;
  address?: string;
  taxCode?: string;
  phone?: string;
  email?: string;
  bankAccountName?: string;
  bankAccountNumber?: string;
  bankName?: string;
  bankBranch?: string;
};

export default function SettingsPage() {
  const [tab, setTab] = useState("store");
  const [, navigate] = useLocation();
  
  // Handle e-invoice tab click
  const handleEInvoiceTab = () => {
    navigate("/einvoice-settings");
  };
  
  // --- Store Info logic ---
  const queryClient = useQueryClient();
  const { data: storeInfo } = useQuery<StoreInfo | null>({
    queryKey: ["/api/StoreInfo"],
    queryFn: async () => {
      const res = await apiRequest("/api/StoreInfo", { method: "GET" });
      return typeof res === "string" ? JSON.parse(res) : res;
    },
  });
  const [form, setForm] = useState<StoreInfo>({
    name: "",
    address: "",
    taxCode: "",
    phone: "",
    email: "",
    bankAccountName: "",
    bankAccountNumber: "",
    bankName: "",
    bankBranch: "",
  });
  useEffect(() => {
    if (storeInfo) setForm(storeInfo);
  }, [storeInfo]);
  const mutation = useMutation({
    mutationFn: async (data: StoreInfo) => {
      const res = await apiRequest("/api/StoreInfo", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
      });
      return typeof res === "string" ? JSON.parse(res) : res;
    },
    onSuccess: () => {
  queryClient.invalidateQueries({ queryKey: ["/api/StoreInfo"] });
      alert("Đã lưu thông tin cửa hàng!");
    },
  });
  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    setForm({ ...form, [e.target.name]: e.target.value });
  }
  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    mutation.mutate(form);
  }

  return (
    <AppLayout title="Cài đặt hệ thống">
      <div>
        <h1 className="text-2xl font-bold mb-1">Cài đặt hệ thống</h1>
        <p className="text-gray-500 mb-6">Quản lý cấu hình và tuỳ chỉnh hệ thống POS</p>
        <div className="flex mb-8 bg-gray-100 rounded-full overflow-hidden">
          <button
            className={`flex-1 py-3 flex items-center justify-center gap-2 font-medium transition ${tab === "store" ? "bg-white shadow text-primary" : "text-gray-700"}`}
            onClick={() => setTab("store")}
          >
            <Store className="w-5 h-5" /> Cửa hàng
          </button>
          <button
            className={`flex-1 py-3 flex items-center justify-center gap-2 font-medium transition ${tab === "tax" ? "bg-white shadow text-primary" : "text-gray-700"}`}
            onClick={() => setTab("tax")}
          >
            <Calculator className="w-5 h-5" /> Thuế
          </button>
          <button
            className={`flex-1 py-3 flex items-center justify-center gap-2 font-medium transition ${tab === "payment" ? "bg-white shadow text-primary" : "text-gray-700"}`}
            onClick={() => setTab("payment")}
          >
            <CreditCard className="w-5 h-5" /> Thanh toán
          </button>
          <button
            className={`flex-1 py-3 flex items-center justify-center gap-2 font-medium transition ${tab === "qr" ? "bg-white shadow text-primary" : "text-gray-700"}`}
            onClick={() => setTab("qr")}
          >
            <QrCode className="w-5 h-5" /> QR Code
          </button>
          <button
            className={`flex-1 py-3 flex items-center justify-center gap-2 font-medium transition ${tab === "einvoice" ? "bg-white shadow text-primary" : "text-gray-700"}`}
            onClick={handleEInvoiceTab}
          >
            <FileText className="w-5 h-5" /> Hóa đơn điện tử
          </button>
          <button
            className={`flex-1 py-3 flex items-center justify-center gap-2 font-medium transition ${tab === "print" ? "bg-white shadow text-primary" : "text-gray-700"}`}
            onClick={() => setTab("print")}
          >
            <Printer className="w-5 h-5" /> In ấn
          </button>
        </div>

        {tab === "store" && (
          <div className="max-w-4xl mx-auto">
            <Card>
              <CardContent className="p-6">
                <form onSubmit={handleSubmit} className="space-y-8">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div>
                      <label className="block font-medium">Tên cửa hàng *</label>
                      <input name="name" value={form.name || ""} onChange={handleChange} required className="border rounded px-2 py-1 w-full" />
                    </div>
                    <div>
                      <label className="block font-medium">Mã số thuế</label>
                      <input name="taxCode" value={form.taxCode || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" />
                    </div>
                    <div>
                      <label className="block font-medium">Số điện thoại</label>
                      <input name="phone" value={form.phone || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" />
                    </div>
                    <div>
                      <label className="block font-medium">Email</label>
                      <input name="email" value={form.email || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" />
                    </div>
                    <div className="md:col-span-2">
                      <label className="block font-medium">Địa chỉ</label>
                      <input name="address" value={form.address || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" />
                    </div>
                  </div>
                  <div className="mt-8">
                    <h2 className="text-lg font-semibold mb-4 flex items-center gap-2"><CreditCard className="w-5 h-5" /> Thông tin tài khoản ngân hàng</h2>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <div>
                        <label className="block font-medium">Tên tài khoản</label>
                        <input name="bankAccountName" value={form.bankAccountName || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" placeholder="Tên chủ tài khoản" />
                      </div>
                      <div>
                        <label className="block font-medium">Số tài khoản</label>
                        <input name="bankAccountNumber" value={form.bankAccountNumber || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" placeholder="Nhập số tài khoản" />
                      </div>
                      <div>
                        <label className="block font-medium">Tên ngân hàng</label>
                        <input name="bankName" value={form.bankName || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" placeholder="Tên ngân hàng" />
                      </div>
                      <div>
                        <label className="block font-medium">Chi nhánh</label>
                        <input name="bankBranch" value={form.bankBranch || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" placeholder="Chi nhánh ngân hàng" />
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center justify-between bg-gray-50 rounded-xl p-4 mt-8">
                    <div>
                      <div className="font-semibold">Kiểm tra QR thanh toán</div>
                      <div className="text-gray-500 text-sm">Tạo mã QR thử nghiệm để kiểm tra cấu hình</div>
                    </div>
                    <Button type="button" variant="outline" className="flex items-center gap-2">
                      <svg xmlns="http://www.w3.org/2000/svg" className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4h4v4H4V4zm6 0h4v4h-4V4zm6 0h4v4h-4V4zM4 10h4v4H4v-4zm6 0h4v4h-4v-4zm6 0h4v4h-4v-4zM4 16h4v4H4v-4zm6 0h4v4h-4v-4zm6 0h4v4h-4v-4z" /></svg>
                      Tạo QR thử nghiệm
                    </Button>
                  </div>
                  <div className="pt-2">
                    <Button type="submit" disabled={mutation.isPending}>
                      {mutation.isPending ? "Đang lưu..." : "Lưu thông tin"}
                    </Button>
                  </div>
                </form>
              </CardContent>
            </Card>
          </div>
        )}
        {tab === "tax" && (
          <div className="max-w-4xl mx-auto"><TaxSettings /></div>
        )}
        {tab === "payment" && (
          <div className="max-w-4xl mx-auto"><PaymentSettings /></div>
        )}
        {tab === "qr" && (
          <div className="max-w-4xl mx-auto"><QRSettings /></div>
        )}
        {tab === "print" && (
          <div className="max-w-6xl mx-auto"><PrintSettings /></div>
        )}
      </div>
    </AppLayout>
  );
}