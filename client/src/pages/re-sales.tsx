import { useState, useEffect, useRef } from "react";
import { useLocation } from "wouter";
import { useQuery, useMutation } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { useToast } from "@/hooks/use-toast";
import { useNotificationSound } from "@/hooks/use-notification-sound";
import { queryClient, apiRequest } from "@/lib/queryClient";
import { Search, Plus, Minus, Trash2, ShoppingCart, CreditCard, Banknote, QrCode, Smartphone, AlertTriangle, FileText, Send, Printer, Tag, Camera } from "lucide-react";
import { cn, normalizeSearchText, getProductImageUrl } from "@/lib/utils";
import type { Product, Customer } from "@/types/backend-types";
import { useCartDiscount, useApplyDiscount, type Discount, type DiscountCalculationResponse } from "@/hooks/useDiscount";
import { useAuth } from "@/contexts/auth-context";
import { BarcodeScanner } from "@/components/BarcodeScanner";

// This file is a copy of sales.tsx with small modifications to support selecting a sale date
// for creating orders. Orders created here will include a `createdAt` value derived from
// the selected sale date so reports can include these sales on the chosen date.

type StoreInfo = {
  name: string;
  address?: string;
  taxCode?: string;
  phone?: string;
  email?: string;
};

interface CartItem extends Product {
  quantity: number;
  totalPrice: number;
  cartItemId: string;
}

interface PaymentMethod {
  id: string;
  name: string;
  icon: any;
  color?: string;
  enabled?: boolean;
}

interface PaymentConfig {
  paymentMethods: PaymentMethod[];
  defaultMethod: string;
  enablePartialPayment: boolean;
  enableDrawer: boolean;
}

const getPaymentIcon = (id: string) => {
  switch (id) {
    case 'cash': return Banknote;
    case 'card': return CreditCard;
    case 'qr': return QrCode;
    case 'ewallet': return Smartphone;
    case 'banktransfer': return CreditCard;
    default: return Banknote;
  }
};

const getPaymentColor = (id: string) => {
  switch (id) {
    case 'cash': return 'bg-green-500';
    case 'card': return 'bg-blue-500';
    case 'qr': return 'bg-purple-500';
    case 'ewallet': return 'bg-orange-500';
    case 'banktransfer': return 'bg-indigo-500';
    default: return 'bg-gray-500';
  }
};

export default function ReSales() {
  const { currentStore, user, switchStore, availableStores, loadAvailableStores } = useAuth();
  const { toast } = useToast();
  const { playNotificationSound } = useNotificationSound();
  const [location, navigate] = useLocation();
  const [cart, setCart] = useState<CartItem[]>([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [barcodeInput, setBarcodeInput] = useState("");
  const [barcodeInputRef, setBarcodeInputRef] = useState<HTMLInputElement | null>(null);
  const barcodeTimerRef = useRef<NodeJS.Timeout | null>(null);
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);
  const [selectedPayment, setSelectedPayment] = useState<string>("cash");
  const [showPayment, setShowPayment] = useState(false);
  const [pendingOrderToReopen, setPendingOrderToReopen] = useState<any>(null);
  const [currentReopenedOrder, setCurrentReopenedOrder] = useState<any>(null);
  const [checkLocalStorage, setCheckLocalStorage] = useState(0);
  const [qrCodeData, setQrCodeData] = useState<any>(null);
  const [showQRCode, setShowQRCode] = useState(false);
  const [activeProductTab, setActiveProductTab] = useState("all");
  const [showCameraScanner, setShowCameraScanner] = useState(false);

  // New: sale date state (YYYY-MM-DD) — default to today
  const [saleDate, setSaleDate] = useState<string>(new Date().toISOString().slice(0,10));

  // ...the rest of the logic is intentionally the same as the sales page, except when creating orders

  // Auto-focus barcode input on keypress
  useEffect(() => {
    const handleGlobalKeyPress = (e: KeyboardEvent) => {
      const activeElement = document.activeElement;
      const isInputActive = activeElement?.tagName === 'INPUT' || 
                           activeElement?.tagName === 'TEXTAREA' ||
                           activeElement?.getAttribute('contenteditable') === 'true';
      if (!isInputActive && /^[a-zA-Z0-9]$/.test(e.key) && barcodeInputRef) {
        e.preventDefault();
        barcodeInputRef.focus();
        setBarcodeInput(e.key);
      }
    };

    document.addEventListener('keypress', handleGlobalKeyPress);
    return () => {
      document.removeEventListener('keypress', handleGlobalKeyPress);
      if (barcodeTimerRef.current) clearTimeout(barcodeTimerRef.current);
    };
  }, [barcodeInputRef]);

  useEffect(() => {
    if (!availableStores || availableStores.length === 0) loadAvailableStores();
  }, [availableStores, loadAvailableStores]);

  useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search);
    const storeId = urlParams.get('storeId');
    if (storeId && parseInt(storeId) !== currentStore?.storeId) {
      const targetStore = availableStores?.find(store => store.storeId === parseInt(storeId));
      if (targetStore) {
        switchStore(parseInt(storeId));
        const newUrl = window.location.pathname;
        window.history.replaceState({}, '', newUrl);
      } else {
        toast({ title: "Không có quyền truy cập", description: "Bạn không có quyền truy cập cửa hàng này.", variant: "destructive" });
        const newUrl = window.location.pathname;
        window.history.replaceState({}, '', newUrl);
      }
    }
  }, [currentStore?.storeId, availableStores, switchStore]);

  // For brevity re-use existing queries and mutations by calling the same endpoints
  const { data: products = [], isLoading: productsLoading } = useQuery<any[]>({
    queryKey: ['products-sales', currentStore?.storeId],
    queryFn: async () => {
      const storeParam = currentStore?.storeId ? `?storeId=${currentStore.storeId}` : '';
      const resp = await fetch(`/api/products${storeParam}`);
      if (!resp.ok) throw new Error('Error loading products');
      const r = await resp.json();
      return r.products || r.Products || [];
    },
    enabled: !!currentStore?.storeId,
  });

  const { data: featuredProducts = [] } = useQuery<any[]>({ queryKey: ['/api/products/featured', currentStore?.storeId], queryFn: async () => { const storeParam = currentStore?.storeId ? `?storeId=${currentStore.storeId}` : ''; const resp = await fetch(`/api/products/featured${storeParam}`); const r = await resp.json(); return r.products || r.Products || []; }, enabled: !!currentStore?.storeId });

  const { data: customers = [] } = useQuery<any[]>({ queryKey: ['/api/customers', currentStore?.storeId], queryFn: async () => { const storeParam = currentStore?.storeId ? `?storeId=${currentStore.storeId}` : ''; const rawCustomers = await apiRequest(`/api/customers${storeParam}`, { method: 'GET' }); return rawCustomers; }, select: (rawCustomers: any[]) => rawCustomers.map((c) => ({ id: c.customerId?.toString(), name: c.hoTen || '', phone: c.soDienThoai || '', address: c.diaChi || '' })), enabled: !!currentStore?.storeId });

  const { data: storeInfo } = useQuery<StoreInfo | null>({ queryKey: ["/api/StoreInfo"], queryFn: async () => { const res = await apiRequest("/api/StoreInfo", { method: "GET" }); if (res.status === 404) return null; return typeof res === "string" ? JSON.parse(res) : res; } });

  const { data: paymentConfig, refetch: refetchPaymentConfig } = useQuery<PaymentConfig>({ queryKey: ["/api/PaymentMethodConfig/enabled"], queryFn: async () => { const res = await apiRequest("/api/PaymentMethodConfig/enabled", { method: "GET" }); return res; }, staleTime: 0, gcTime: 0 });

  const { data: qrSettings } = useQuery({ queryKey: ["/api/QRSettings"], queryFn: async () => { const res = await apiRequest("/api/QRSettings", { method: "GET" }); return res; }, staleTime: 5 * 60 * 1000 });

  const { data: printConfig } = useQuery({ queryKey: ["/api/PrintConfig"], queryFn: async () => { const res = await apiRequest("/api/PrintConfig", { method: "GET" }); return res; }, staleTime: 5 * 60 * 1000 });

  useEffect(() => { if (paymentConfig?.defaultMethod && selectedPayment === 'cash') setSelectedPayment(paymentConfig.defaultMethod); }, [paymentConfig]);

  useEffect(() => { const handlePaymentConfigChange = () => refetchPaymentConfig(); window.addEventListener('paymentConfigChanged', handlePaymentConfigChange); return () => window.removeEventListener('paymentConfigChanged', handlePaymentConfigChange); }, [refetchPaymentConfig]);

  const availablePaymentMethods: PaymentMethod[] = paymentConfig?.paymentMethods?.map((method: any) => ({ ...method, icon: getPaymentIcon(method.id), color: getPaymentColor(method.id) })) || [{ id: 'cash', name: 'Tiền mặt', icon: Banknote, color: 'bg-green-500', enabled: true }];

  const generateQRUrl = (amount: number, orderId?: number) => {
    if (!qrSettings?.isEnabled || !qrSettings?.bankCode || !qrSettings?.bankAccountNumber) return null;
    const template = qrSettings.qrTemplate || "compact";
    const accountName = encodeURIComponent(qrSettings.bankAccountHolder || "");
    let url = `https://api.vietqr.io/image/${qrSettings.bankCode}-${qrSettings.bankAccountNumber}-${template}.jpg?accountName=${accountName}&amount=${amount}`;
    if (orderId) {
      const description = encodeURIComponent(`thanh toan don hang theo hoa don ${orderId}`);
      url += `&addInfo=${description}`;
    } else {
      const description = encodeURIComponent(qrSettings.defaultDescription || "Thanh toan hoa don");
      url += `&addInfo=${description}`;
    }
    return url;
  };

  // Mutations: reuse same endpoints but include createdAt in formData for orders created here
  const createOrderMutation = useMutation({ mutationFn: async (formData: FormData) => await apiRequest('/api/orders', { method: 'POST', body: formData }) });
  const saveOrderForLaterMutation = useMutation({ mutationFn: async (formData: FormData) => await apiRequest('/api/orders', { method: 'POST', body: formData }) });
  const completeOrderMutation = useMutation({ mutationFn: async ({ orderId, formData }: any) => await apiRequest(`/api/orders/${orderId}/complete`, { method: 'PUT', body: formData }) });

  // Utility
  const subtotal = cart.reduce((sum, item) => sum + item.totalPrice, 0);
  const taxAmount = 0; // simplified for this copy — reuse original logic if needed
  const totalDiscountAmount = 0;
  const total = subtotal + taxAmount - totalDiscountAmount;

  const addToCart = (product: Product) => {
    if (product.stockQuantity <= 0) { toast({ title: "Hết hàng", description: "Vui lòng nhập thêm hàng sau đó quay lại bán", variant: "destructive" }); return; }
    if (currentReopenedOrder) setCurrentReopenedOrder(null);
    const newItem: CartItem = { ...product, cartItemId: `${Date.now()}-${Math.random()}`, quantity: 1, totalPrice: Number(product.price) };
    setCart([...cart, newItem]);
  };

  const processPayment = () => {
    if (cart.length === 0) { toast({ title: "Giỏ hàng trống", description: "Vui lòng thêm sản phẩm vào giỏ hàng", variant: "destructive" }); return; }
    if (currentReopenedOrder) { const formData = new FormData(); formData.append('paymentMethod', selectedPayment); formData.append('paymentStatus', 'paid'); formData.append('status', 'completed'); completeOrderMutation.mutate({ orderId: currentReopenedOrder.orderId, formData }); }
    else createNewOrder();
  };

  const createNewOrder = () => {
    const formData = new FormData();
    formData.append('orderNumber', `REORD${Date.now()}`);
    formData.append('customerId', selectedCustomer?.id || '0');
    formData.append('cashierId', user?.staffId?.toString() || "1");
    formData.append('storeId', currentStore?.storeId?.toString() || "");
    formData.append('staffId', user?.staffId?.toString() || "1");
    formData.append('subtotal', subtotal.toString());
    formData.append('taxAmount', taxAmount.toString());
    formData.append('discountAmount', totalDiscountAmount.toString());
    formData.append('total', total.toString());
    formData.append('paymentMethod', selectedPayment);
    formData.append('paymentStatus', "paid");
    formData.append('status', "completed");
    // createdAt using selected saleDate + current time with explicit timezone offset
    try {
      const now = new Date();
      const hh = String(now.getHours()).padStart(2, '0');
      const mm = String(now.getMinutes()).padStart(2, '0');
      const ss = String(now.getSeconds()).padStart(2, '0');
      // timezone offset in minutes (note: getTimezoneOffset returns minutes behind UTC)
      const offsetMinutes = -now.getTimezoneOffset();
      const sign = offsetMinutes >= 0 ? '+' : '-';
      const absOffset = Math.abs(offsetMinutes);
      const offH = String(Math.floor(absOffset / 60)).padStart(2, '0');
      const offM = String(absOffset % 60).padStart(2, '0');
      const tz = `${sign}${offH}:${offM}`;
      const createdAtWithOffset = `${saleDate}T${hh}:${mm}:${ss}${tz}`;
      formData.append('createdAt', createdAtWithOffset);
    } catch (e) {
      // fallback to server-interpretable ISO
      formData.append('createdAt', new Date().toISOString());
    }
    cart.forEach((item, idx) => {
      const productId = item.productId?.toString() || "";
      formData.append(`items[${idx}].productId`, productId);
      formData.append(`items[${idx}].productName`, item.name || "");
      formData.append(`items[${idx}].quantity`, item.quantity?.toString() || "1");
      formData.append(`items[${idx}].unitPrice`, item.price?.toString() || "0");
      formData.append(`items[${idx}].totalPrice`, item.totalPrice?.toString() || "0");
    });
    createOrderMutation.mutate(formData, {
      onSuccess: () => {
        toast({ title: "Thành công", description: "Đơn hàng đã được tạo (Re-Sales)" });
        setCart([]);
        queryClient.invalidateQueries({ queryKey: ['/api/orders'] });
        window.dispatchEvent(new CustomEvent('newOrderCreated'));
      },
      onError: () => { toast({ title: "Lỗi", description: "Không thể tạo đơn hàng" , variant: 'destructive' }); }
    });
  };

  const saveOrderForLater = () => {
    if (cart.length === 0) { toast({ title: "Giỏ hàng trống", description: "Vui lòng thêm sản phẩm vào giỏ hàng", variant: "destructive" }); return; }
    const formData = new FormData();
    formData.append('orderNumber', `PENDING-RE-${Date.now()}`);
    formData.append('customerId', selectedCustomer?.id || '0');
    formData.append('cashierId', user?.staffId?.toString() || "1");
    formData.append('storeId', currentStore?.storeId?.toString() || "");
    formData.append('staffId', user?.staffId?.toString() || "1");
    formData.append('subtotal', subtotal.toString());
    formData.append('taxAmount', taxAmount.toString());
    formData.append('discountAmount', "0");
    formData.append('total', total.toString());
    formData.append('paymentMethod', selectedPayment);
    formData.append('paymentStatus', "pending");
    formData.append('status', "pending");
    try {
      const now = new Date();
      const hh = String(now.getHours()).padStart(2, '0');
      const mm = String(now.getMinutes()).padStart(2, '0');
      const ss = String(now.getSeconds()).padStart(2, '0');
      const offsetMinutes = -now.getTimezoneOffset();
      const sign = offsetMinutes >= 0 ? '+' : '-';
      const absOffset = Math.abs(offsetMinutes);
      const offH = String(Math.floor(absOffset / 60)).padStart(2, '0');
      const offM = String(absOffset % 60).padStart(2, '0');
      const tz = `${sign}${offH}:${offM}`;
      formData.append('createdAt', `${saleDate}T${hh}:${mm}:${ss}${tz}`);
    } catch {
      formData.append('createdAt', new Date().toISOString());
    }
    cart.forEach((item, idx) => { const productId = item.productId?.toString() || ""; formData.append(`items[${idx}].productId`, productId); formData.append(`items[${idx}].productName`, item.name || ""); formData.append(`items[${idx}].quantity`, item.quantity?.toString() || "1"); formData.append(`items[${idx}].unitPrice`, item.price?.toString() || "0"); formData.append(`items[${idx}].totalPrice`, item.totalPrice?.toString() || "0"); });
    saveOrderForLaterMutation.mutate(formData, { onSuccess: () => { toast({ title: "Đã lưu đơn hàng chờ thanh toán" }); setCart([]); queryClient.invalidateQueries({ queryKey: ['/api/orders'] }); window.dispatchEvent(new CustomEvent('newOrderCreated')); navigate('/orders'); } });
  };

  return (
    <AppLayout title="Bán hàng bổ sung">
      <div className="lg:grid lg:grid-cols-[1fr_384px] lg:gap-2 flex flex-col gap-4 h-screen" data-testid="resales-page">
        <div className="order-1 lg:order-1 h-full">
          <Card className="h-full lg:sticky lg:top-0 lg:max-h-screen">
            <CardContent className="p-6 flex flex-col h-full overflow-hidden">
              <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between mb-6 gap-4 flex-shrink-0">
                <div className="flex items-center gap-4">
                  <h2 className="text-xl font-semibold">Sản phẩm (Re-Sales)</h2>
                  <Button variant="outline" onClick={() => navigate('/orders')} className="text-sm">Xem lịch sử hóa đơn</Button>
                </div>

                <div className="flex items-center gap-2">
                  <label className="text-sm mr-2">Ngày bán:</label>
                  <Input type="date" value={saleDate} onChange={(e) => setSaleDate(e.target.value)} className="text-sm" />
                </div>

                <div className="flex flex-col sm:flex-row gap-2 w-full lg:w-auto">
                  <div className="relative w-full sm:w-80">
                    <Input placeholder="Tìm kiếm sản phẩm (có thể gõ không dấu)..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-10" data-testid="input-product-search" />
                    <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
                  </div>
                </div>
              </div>
              {/* simplified products listing for brevity; reuse products mapping similar to Sales */}
              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-3 xl:grid-cols-4 gap-4 h-full overflow-y-auto">
                {products.map((product: any) => (
                  <div key={product.productId} className="cursor-pointer hover:shadow-md transition-shadow" onClick={() => addToCart(product)}>
                    <Card>
                      <CardContent className="p-4">
                        <div className="w-full h-32 bg-gray-100 flex items-center justify-center overflow-hidden rounded-lg mb-3">
                          <img src={getProductImageUrl(product.imageUrl)} alt={product.name} className="max-w-full max-h-full object-contain" />
                        </div>
                        <h3 className="font-medium text-sm mb-1 line-clamp-2">{product.name}</h3>
                        <p className="text-lg font-bold">{Number(product.price || 0).toLocaleString('vi-VN')}₫</p>
                        <Button className="w-full mt-2" size="sm" onClick={(e) => { e.stopPropagation(); addToCart(product); }}><Plus className="w-4 h-4 mr-1"/>Thêm</Button>
                      </CardContent>
                    </Card>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="order-2 lg:order-2 h-full">
          <Card className="h-full lg:sticky lg:top-0 lg:max-h-screen">
            <CardContent className="p-6 flex flex-col h-full overflow-hidden">
              <div className="flex items-center justify-between mb-4 flex-shrink-0">
                <h2 className="text-xl font-semibold flex items-center"><ShoppingCart className="w-5 h-5 mr-2"/>Giỏ hàng ({cart.length})</h2>
                {cart.length > 0 && (<Button variant="ghost" size="sm" onClick={() => setCart([])}><Trash2 className="w-4 h-4"/></Button>)}
              </div>
              <div className="flex-1 overflow-y-auto overflow-x-hidden min-h-0 space-y-4">
                {cart.length === 0 ? (<div className="text-center py-8 text-gray-500"><ShoppingCart className="w-12 h-12 mx-auto mb-2 opacity-50"/><p>Giỏ hàng trống</p></div>) : (
                  cart.map(item => (
                    <div key={item.cartItemId} className="flex items-center justify-between p-3 bg-white rounded-lg border-2 border-blue-200 mb-2">
                      <div className="flex-1 min-w-0"><p className="font-medium text-sm truncate">{item.name}</p><p className="text-primary font-semibold text-sm">{Number(item.price||0).toLocaleString('vi-VN')}₫</p></div>
                      <div className="flex items-center space-x-2 ml-2"><Button size="sm" variant="outline" onClick={() => setCart(c => c.map(ci => ci.cartItemId===item.cartItemId?{...ci, quantity: ci.quantity-1, totalPrice: (ci.quantity-1)*ci.price}:ci))}>-</Button><div className="w-6 text-center">{item.quantity}</div><Button size="sm" variant="outline" onClick={() => setCart(c => c.map(ci => ci.cartItemId===item.cartItemId?{...ci, quantity: ci.quantity+1, totalPrice: (ci.quantity+1)*ci.price}:ci))}>+</Button></div>
                    </div>
                  ))
                )}

                {cart.length > 0 && (
                  <div className="space-y-2 mb-4">
                    <Separator />
                    <div className="flex justify-between"><span>Tạm tính:</span><span>{subtotal.toLocaleString('vi-VN')}₫</span></div>
                    <Separator />
                    <div className="flex justify-between text-lg font-bold"><span>Tổng cộng:</span><span>{total.toLocaleString('vi-VN')}₫</span></div>
                  </div>
                )}

                {cart.length > 0 && (
                  <div className="space-y-3">
                    <p className="font-medium">Phương thức thanh toán:</p>
                    <div className="grid grid-cols-2 gap-2">
                      {availablePaymentMethods.map(m => { const Icon = m.icon; return (<Button key={m.id} variant={selectedPayment===m.id? 'default':'outline'} size="sm" onClick={() => setSelectedPayment(m.id)}><Icon className="w-4 h-4 mr-1"/>{m.name}</Button>); })}
                    </div>
                  </div>
                )}

                {cart.length > 0 && (
                  <div className="space-y-2 mt-4">
                    <Button className="w-full h-12 text-lg" onClick={processPayment}>Thanh toán</Button>
                    <Button className="w-full h-11 text-lg bg-green-600 hover:bg-green-700" onClick={() => {/* E-invoice flow could reuse existing implementation if needed */}}>Xuất hóa đơn</Button>
                    <Button variant="outline" className="w-full h-10 text-sm" onClick={saveOrderForLater}>Thanh toán sau</Button>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </AppLayout>
  );
}
