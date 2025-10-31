import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiRequest } from "@/lib/queryClient";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Printer } from "lucide-react";

export type PrintConfig = {
  printerName: string;
  paperSize: string;
  printCopies: number;
  autoPrintBill: boolean;
  autoPrintOnOrder: boolean;
  printBarcode: boolean;
  printLogo: boolean;
  billHeader: string;
  billFooter: string;
};

const paperSizes = [
  { value: "80mm", label: "80mm" },
  { value: "A4", label: "A4" },
  { value: "58mm", label: "58mm" },
];

export function PrintSettings() {
  const queryClient = useQueryClient();
  
  // Get current print config
  const { data: config } = useQuery<PrintConfig | null>({
    queryKey: ["/api/PrintConfig"],
    queryFn: async () => {
      const res = await apiRequest("/api/PrintConfig", { method: "GET" });
      return res;
    },
  });

  // Get installed printers
  const { data: printersData, isLoading: printersLoading, error: printersError } = useQuery<{printers: string[]} | null>({
    queryKey: ["/api/PrintConfig/printers"],
    queryFn: async () => {
      console.log('Loading printers...');
      const res = await apiRequest("/api/PrintConfig/printers", { method: "GET" });
      console.log('Printers data:', res);
      return res;
    },
  });

  const installedPrinters = printersData?.printers || [];
  console.log('Available printers:', installedPrinters);

  const [form, setForm] = useState<PrintConfig>({
    printerName: "",
    paperSize: "80mm",
    printCopies: 1,
    autoPrintBill: true,
    autoPrintOnOrder: false,
    printBarcode: true,
    printLogo: true,
    billHeader: "",
    billFooter: "",
  });
  
  useEffect(() => {
    if (config) setForm(config);
  }, [config]);
  
  const mutation = useMutation({
    mutationFn: async (data: PrintConfig) => {
      const res = await apiRequest("/api/PrintConfig", { 
        method: "PUT", 
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
      });
      return res;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/PrintConfig"] });
      alert("Đã lưu cấu hình in ấn!");
    },
  });

  const testPrintMutation = useMutation({
    mutationFn: async (printerName: string) => {
      const res = await apiRequest("/api/PrintConfig/test-printer", { 
        method: "POST", 
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ printerName })
      });
      return res;
    },
    onSuccess: (data) => {
      if (data.isConnected) {
        alert(`✅ Kết nối thành công với máy in: ${data.printerName}`);
      } else {
        alert(`❌ ${data.message || 'Không thể kết nối với máy in'}`);
      }
    },
    onError: () => {
      alert("❌ Lỗi khi test máy in");
    }
  });

  function handleChange(e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) {
    const target = e.target as HTMLInputElement;
    const { name, value, type } = target;
    const checked = target.checked;
    setForm(f => ({
      ...f,
      [name]: type === "checkbox" ? checked : value,
    }));
  }

  function handleTestPrint() {
    console.log('Test print clicked, printer:', form.printerName);
    if (!form.printerName) {
      alert("Vui lòng chọn máy in trước khi test!");
      return;
    }
    console.log('Calling test print API...');
    testPrintMutation.mutate(form.printerName);
  }
  return (
    <Card>
      <CardContent className="p-6">
        <form onSubmit={e => { e.preventDefault(); mutation.mutate(form); }} className="space-y-8">
          <h2 className="text-lg font-semibold mb-4 flex items-center gap-2"><Printer className="w-5 h-5" /> Cấu hình in ấn</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block font-medium">Tên máy in</label>
              <select 
                name="printerName" 
                value={form.printerName || ""} 
                onChange={handleChange} 
                className="border rounded px-2 py-1 w-full"
              >
                <option value="">Chọn máy in...</option>
                {installedPrinters.map(printer => (
                  <option key={printer} value={printer}>{printer}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block font-medium">Khổ giấy</label>
              <select name="paperSize" value={form.paperSize} onChange={handleChange} className="border rounded px-2 py-1 w-full">
                {paperSizes.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block font-medium">Số bản in</label>
              <input name="printCopies" type="number" min={1} value={form.printCopies} onChange={handleChange} className="border rounded px-2 py-1 w-full" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold">In hóa đơn</div>
                <div className="text-gray-500 text-sm">Tự động in hóa đơn sau thanh toán</div>
              </div>
              <input type="checkbox" name="autoPrintBill" checked={form.autoPrintBill} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold">Tự động in</div>
                <div className="text-gray-500 text-sm">In ngay khi hoàn thành đơn hàng</div>
              </div>
              <input type="checkbox" name="autoPrintOnOrder" checked={form.autoPrintOnOrder} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold">In mã vạch</div>
                <div className="text-gray-500 text-sm">In mã vạch sản phẩm trên hóa đơn</div>
              </div>
              <input type="checkbox" name="printBarcode" checked={form.printBarcode} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold">In logo</div>
                <div className="text-gray-500 text-sm">Hiển thị logo cửa hàng trên hóa đơn</div>
              </div>
              <input type="checkbox" name="printLogo" checked={form.printLogo} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
          </div>
          <div>
            <label className="block font-medium">Tiêu đề hóa đơn</label>
            <textarea name="billHeader" value={form.billHeader || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" rows={2} />
          </div>
          <div>
            <label className="block font-medium">Chân trang hóa đơn</label>
            <textarea name="billFooter" value={form.billFooter || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" rows={2} />
          </div>
          <div className="flex justify-between pt-2">
            <Button 
              type="button" 
              variant="outline" 
              className="flex items-center gap-2"
              onClick={handleTestPrint}
              disabled={testPrintMutation.isPending || !form.printerName}
            >
              <svg xmlns="http://www.w3.org/2000/svg" className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 8v6M8 8v6m-4 4h16a2 2 0 002-2V6a2 2 0 00-2-2H4a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>
              {testPrintMutation.isPending ? "Đang test..." : "In thử nghiệm"}
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              <svg xmlns="http://www.w3.org/2000/svg" className="w-5 h-5 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" /></svg>
              Lưu cấu hình
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
