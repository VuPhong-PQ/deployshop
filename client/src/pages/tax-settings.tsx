import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiRequest } from "../lib/utils";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Calculator } from "lucide-react";

export type TaxConfig = {
  enableVAT: boolean;
  vatIncludedInPrice: boolean;
  vatRate: number;
  vatLabel: string;
  enableEnvTax: boolean;
  envTaxRate: number;
};

export function TaxSettings() {
  const queryClient = useQueryClient();
  const { data: taxConfig } = useQuery<TaxConfig | null>({
    queryKey: ["/api/TaxConfig"],
    queryFn: async () => {
      const res = await apiRequest("/api/TaxConfig", { method: "GET" });
      return typeof res === "string" ? JSON.parse(res) : res;
    },
  });
  const [form, setForm] = useState<TaxConfig>({
    enableVAT: false,
    vatIncludedInPrice: false,
    vatRate: 0,
    vatLabel: "VAT",
    enableEnvTax: false,
    envTaxRate: 0,
  });
  useEffect(() => {
    if (taxConfig) setForm(taxConfig);
  }, [taxConfig]);
  const mutation = useMutation({
    mutationFn: async (data: TaxConfig) => {
      const res = await apiRequest("/api/TaxConfig", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
      });
      return typeof res === "string" ? JSON.parse(res) : res;
    },
    onSuccess: () => {
  queryClient.invalidateQueries({ queryKey: ["/api/TaxConfig"] });
      alert("Đã lưu cấu hình thuế!");
    },
  });
  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value, type, checked } = e.target;
    setForm(f => ({
      ...f,
      [name]: type === "checkbox" ? checked : value,
    }));
  }
  return (
    <Card>
      <CardContent className="p-6">
        <form onSubmit={e => { e.preventDefault(); mutation.mutate(form); }} className="space-y-8">
          <h2 className="text-lg font-semibold mb-4 flex items-center gap-2"><Calculator className="w-5 h-5" /> Cấu hình thuế</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold">Bật thuế VAT</div>
                <div className="text-gray-500 text-sm">Áp dụng thuế giá trị gia tăng cho đơn hàng</div>
              </div>
              <input type="checkbox" name="enableVAT" checked={form.enableVAT} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between">
              <div>
                <div className="font-semibold">Thuế bao gồm trong giá</div>
                <div className="text-gray-500 text-sm">Giá sản phẩm đã bao gồm thuế</div>
              </div>
              <input type="checkbox" name="vatIncludedInPrice" checked={form.vatIncludedInPrice} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div>
              <label className="block font-medium">Thuế suất VAT (%)</label>
              <input name="vatRate" type="number" min={0} value={form.vatRate} onChange={handleChange} className="border rounded px-2 py-1 w-full" />
            </div>
            <div>
              <label className="block font-medium">Nhãn thuế</label>
              <input name="vatLabel" value={form.vatLabel || ""} onChange={handleChange} className="border rounded px-2 py-1 w-full" />
            </div>
            <div className="bg-gray-50 rounded-xl p-4 flex items-center justify-between md:col-span-2">
              <div>
                <div className="font-semibold">Thuế bảo vệ môi trường</div>
                <div className="text-gray-500 text-sm">Áp dụng cho một số mặt hàng</div>
              </div>
              <input type="checkbox" name="enableEnvTax" checked={form.enableEnvTax} onChange={handleChange} className="w-10 h-5 accent-blue-600" />
            </div>
            <div>
              <label className="block font-medium">Thuế suất bảo vệ môi trường (%)</label>
              <input name="envTaxRate" type="number" min={0} value={form.envTaxRate} onChange={handleChange} className="border rounded px-2 py-1 w-full" />
            </div>
          </div>
          <div className="flex justify-end pt-2">
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
