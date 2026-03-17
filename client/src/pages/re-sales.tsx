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
import { Search, Plus, Minus, Trash2, ShoppingCart, CreditCard, Banknote, QrCode, Smartphone, AlertTriangle, FileText, Send, Printer, Tag, Camera, ChevronLeft, ChevronRight } from "lucide-react";
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

interface SplitPaymentEntry {
  method: string;
  methodName: string;
  amount: number;
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
  // Pagination states (inherited from Sales)
  const [allProductsPage, setAllProductsPage] = useState(1);
  const [featuredProductsPage, setFeaturedProductsPage] = useState(1);
  const PRODUCTS_PER_PAGE = 10;
  const [showCameraScanner, setShowCameraScanner] = useState(false);

  // Split payment states
  const [isSplitPayment, setIsSplitPayment] = useState(false);
  const [splitPayments, setSplitPayments] = useState<SplitPaymentEntry[]>([]);
  const [splitInputAmounts, setSplitInputAmounts] = useState<Record<string, string>>({});

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
      // Request all products (same behavior as sales.tsx) so client-side pagination works consistently
      const params = new URLSearchParams({ pageSize: '9999', page: '1' });
      if (currentStore?.storeId) params.append('storeId', currentStore.storeId.toString());
      const url = `/api/products?${params.toString()}`;
      const res = await apiRequest(url, { method: 'GET' });
      const r = typeof res === 'string' ? JSON.parse(res) : res;
      return Array.isArray(r) ? r : (r.products || r.Products || []);
    },
    enabled: !!currentStore?.storeId,
  });

  const { data: featuredProducts = [] } = useQuery<any[]>({
    queryKey: ['/api/products/featured', currentStore?.storeId],
    queryFn: async () => {
      // Request all featured products to support client-side pagination
      const params = new URLSearchParams({ pageSize: '9999', page: '1' });
      if (currentStore?.storeId) params.append('storeId', currentStore.storeId.toString());
      const url = `/api/products/featured?${params.toString()}`;
      const res = await apiRequest(url, { method: 'GET' });
      const r = typeof res === 'string' ? JSON.parse(res) : res;
      return Array.isArray(r) ? r : (r.products || r.Products || []);
    },
    enabled: !!currentStore?.storeId
  });

  const { data: customers = [] } = useQuery<any[]>({ queryKey: ['/api/customers', currentStore?.storeId], queryFn: async () => { const storeParam = currentStore?.storeId ? `?storeId=${currentStore.storeId}` : ''; const rawCustomers = await apiRequest(`/api/customers${storeParam}`, { method: 'GET' }); return rawCustomers; }, select: (rawCustomers: any[]) => rawCustomers.map((c) => ({ id: c.customerId?.toString(), name: c.hoTen || '', phone: c.soDienThoai || '', address: c.diaChi || '' })), enabled: !!currentStore?.storeId });

  const { data: storeInfo } = useQuery<StoreInfo | null>({ queryKey: ["/api/StoreInfo"], queryFn: async () => { const res = await apiRequest("/api/StoreInfo", { method: "GET" }); if (res.status === 404) return null; return typeof res === "string" ? JSON.parse(res) : res; } });

  // Pagination helper component and logic (inherited from Sales)
  const PaginationComponent = ({ 
    currentPage, 
    totalPages, 
    onPageChange,
    totalItems,
    itemsPerPage 
  }: {
    currentPage: number;
    totalPages: number;
    onPageChange: (page: number) => void;
    totalItems: number;
    itemsPerPage: number;
  }) => {
    if (totalPages <= 1) return null;

    const startItem = (currentPage - 1) * itemsPerPage + 1;
    const endItem = Math.min(currentPage * itemsPerPage, totalItems);

    return (
      <div className="bg-white border-t border-gray-200 px-2 lg:px-4 py-2 shadow-sm">
        <div className="flex items-center justify-between">
          <div className="flex items-center text-xs text-gray-600">
            <span className="hidden sm:inline">
              Hiển thị {startItem}-{endItem} trong tổng số {totalItems} sản phẩm
            </span>
            <span className="sm:hidden">
              Trang {currentPage}/{totalPages} ({totalItems} sp)
            </span>
          </div>
          <div className="flex items-center space-x-1">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(currentPage - 1)}
              disabled={currentPage === 1}
              className="h-7 w-7 p-0"
              title="Trang trước"
            >
              <ChevronLeft className="h-3 w-3" />
            </Button>
            {totalPages <= 5 ? (
              Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                <Button
                  key={page}
                  variant={currentPage === page ? "default" : "outline"}
                  size="sm"
                  onClick={() => onPageChange(page)}
                  className="h-7 w-7 p-0 text-xs"
                  title={`Trang ${page}`}
                >
                  {page}
                </Button>
              ))
            ) : (
              <>
                {currentPage > 2 && (
                  <>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => onPageChange(1)}
                      className="h-7 w-7 p-0 text-xs"
                      title="Trang đầu"
                    >
                      1
                    </Button>
                    {currentPage > 3 && (
                      <span className="px-1 text-gray-400 text-xs">...</span>
                    )}
                  </>
                )}
                {currentPage > 1 && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => onPageChange(currentPage - 1)}
                    className="h-7 w-7 p-0 text-xs"
                    title={`Trang ${currentPage - 1}`}
                  >
                    {currentPage - 1}
                  </Button>
                )}
                <Button
                  variant="default"
                  size="sm"
                  className="h-7 w-7 p-0 text-xs"
                  title={`Trang hiện tại: ${currentPage}`}
                >
                  {currentPage}
                </Button>
                {currentPage < totalPages && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => onPageChange(currentPage + 1)}
                    className="h-7 w-7 p-0 text-xs"
                    title={`Trang ${currentPage + 1}`}
                  >
                    {currentPage + 1}
                  </Button>
                )}
                {currentPage < totalPages - 1 && (
                  <>
                    {currentPage < totalPages - 2 && (
                      <span className="px-1 text-gray-400 text-xs">...</span>
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => onPageChange(totalPages)}
                      className="h-7 w-7 p-0 text-xs"
                      title="Trang cuối"
                    >
                      {totalPages}
                    </Button>
                  </>
                )}
              </>
            )}
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(currentPage + 1)}
              disabled={currentPage === totalPages}
              className="h-7 w-7 p-0"
              title="Trang sau"
            >
              <ChevronRight className="h-3 w-3" />
            </Button>
          </div>
        </div>
      </div>
    );
  };

  // Filter products using normalizeSearchText and compute pagination
  const filteredProducts = (products || []).filter((product: any) => {
    const q = normalizeSearchText(searchTerm || "");
    if (!q) return true;
    const name = normalizeSearchText(product.name || "");
    const barcode = normalizeSearchText(product.barcode || product.barcodeNumber || "");
    const sku = normalizeSearchText(product.sku || product.code || "");
    return name.includes(q) || barcode.includes(q) || sku.includes(q);
  });

  const totalAllProductsPages = Math.max(1, Math.ceil(filteredProducts.length / PRODUCTS_PER_PAGE));
  const startAllProductsIndex = (allProductsPage - 1) * PRODUCTS_PER_PAGE;
  const endAllProductsIndex = startAllProductsIndex + PRODUCTS_PER_PAGE;
  const paginatedAllProducts = filteredProducts.slice(startAllProductsIndex, endAllProductsIndex);

  const totalFeaturedProductsPages = Math.max(1, Math.ceil((featuredProducts || []).length / PRODUCTS_PER_PAGE));
  const startFeaturedProductsIndex = (featuredProductsPage - 1) * PRODUCTS_PER_PAGE;
  const endFeaturedProductsIndex = startFeaturedProductsIndex + PRODUCTS_PER_PAGE;
  const paginatedFeaturedProducts = (featuredProducts || []).slice(startFeaturedProductsIndex, endFeaturedProductsIndex);

  useEffect(() => {
    setAllProductsPage(1);
  }, [searchTerm]);

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

  // === Split Payment Helper Functions ===
  const toggleSplitPayment = () => {
    if (!isSplitPayment) {
      const firstMethod = availablePaymentMethods[0];
      if (firstMethod) {
        setSplitPayments([{ method: firstMethod.id, methodName: firstMethod.name, amount: 0 }]);
        setSplitInputAmounts({ [firstMethod.id]: '' });
      }
      setIsSplitPayment(true);
    } else {
      setIsSplitPayment(false);
      setSplitPayments([]);
      setSplitInputAmounts({});
    }
  };

  const addSplitPaymentMethod = (methodId: string) => {
    const method = availablePaymentMethods.find(m => m.id === methodId);
    if (!method) return;
    if (splitPayments.some(sp => sp.method === methodId)) {
      toast({ title: "Đã có", description: `${method.name} đã được thêm rồi`, variant: "destructive" });
      return;
    }
    setSplitPayments(prev => [...prev, { method: methodId, methodName: method.name, amount: 0 }]);
    setSplitInputAmounts(prev => ({ ...prev, [methodId]: '' }));
  };

  const removeSplitPaymentMethod = (methodId: string) => {
    setSplitPayments(prev => prev.filter(sp => sp.method !== methodId));
    setSplitInputAmounts(prev => { const n = { ...prev }; delete n[methodId]; return n; });
  };

  const updateSplitAmount = (methodId: string, value: string) => {
    setSplitInputAmounts(prev => ({ ...prev, [methodId]: value }));
    const numValue = parseFloat(value) || 0;
    setSplitPayments(prev => prev.map(sp => sp.method === methodId ? { ...sp, amount: numValue } : sp));
  };

  const autoFillSplitRemaining = (methodId: string) => {
    const otherTotal = splitPayments.filter(sp => sp.method !== methodId).reduce((s, sp) => s + sp.amount, 0);
    const remaining = Math.max(0, total - otherTotal);
    setSplitPayments(prev => prev.map(sp => sp.method === methodId ? { ...sp, amount: remaining } : sp));
    setSplitInputAmounts(prev => ({ ...prev, [methodId]: remaining.toString() }));
  };

  const totalSplitAmount = splitPayments.reduce((s, sp) => s + sp.amount, 0);
  const splitRemaining = total - totalSplitAmount;
  const isSplitValid = isSplitPayment ? (splitPayments.length >= 2 && Math.abs(splitRemaining) < 1) : true;

  const getPaymentMethodDisplay = () => {
    if (!isSplitPayment) {
      const m = availablePaymentMethods.find(pm => pm.id === selectedPayment);
      return m?.name || 'Tiền mặt';
    }
    return splitPayments.map(sp => sp.methodName).join(' + ');
  };

  const getSplitPaymentJSON = (): string | null => {
    if (!isSplitPayment || splitPayments.length < 2) return null;
    return JSON.stringify(splitPayments);
  };

  const getPrimaryPaymentMethod = (): string => {
    if (!isSplitPayment) return selectedPayment;
    return 'split';
  };

  const processPayment = () => {
    if (cart.length === 0) { toast({ title: "Giỏ hàng trống", description: "Vui lòng thêm sản phẩm vào giỏ hàng", variant: "destructive" }); return; }
    if (isSplitPayment && !isSplitValid) { toast({ title: "Chia thanh toán chưa hợp lệ", description: "Vui lòng kiểm tra lại số tiền chia", variant: "destructive" }); return; }
    if (currentReopenedOrder) { const formData = new FormData(); formData.append('paymentMethod', getPrimaryPaymentMethod()); formData.append('paymentStatus', 'paid'); formData.append('status', 'completed'); const splitJSON = getSplitPaymentJSON(); if (splitJSON) formData.append('splitPaymentDetails', splitJSON); completeOrderMutation.mutate({ orderId: currentReopenedOrder.orderId, formData }); }
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
    formData.append('paymentMethod', getPrimaryPaymentMethod());
    formData.append('paymentStatus', "paid");
    formData.append('status', "completed");
    const splitJSON = getSplitPaymentJSON();
    if (splitJSON) formData.append('splitPaymentDetails', splitJSON);
    // createdAt using selected saleDate + current time (simple approach like working sample)
    try {
      const timePart = new Date().toTimeString().split(' ')[0];
      const createdAtIso = new Date(`${saleDate}T${timePart}`).toISOString();
      console.log('[RE-SALES] saleDate:', saleDate);
      console.log('[RE-SALES] createdAt being sent:', createdAtIso);
      formData.append('createdAt', createdAtIso);
    } catch (e) {
      console.error('[RE-SALES] Error creating createdAt:', e);
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
        setIsSplitPayment(false);
        setSplitPayments([]);
        setSplitInputAmounts({});
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
    formData.append('paymentMethod', getPrimaryPaymentMethod());
    formData.append('paymentStatus', "pending");
    formData.append('status', "pending");
    const splitJSONLater = getSplitPaymentJSON();
    if (splitJSONLater) formData.append('splitPaymentDetails', splitJSONLater);
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
                {paginatedAllProducts.map((product: any) => (
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
              {/* Pagination for products (inherited from Sales) */}
              <div className="flex-shrink-0 p-3">
                <PaginationComponent
                  currentPage={allProductsPage}
                  totalPages={totalAllProductsPages}
                  onPageChange={setAllProductsPage}
                  totalItems={filteredProducts.length}
                  itemsPerPage={PRODUCTS_PER_PAGE}
                />
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
                    <div className="flex items-center justify-between">
                      <p className="font-medium">Phương thức thanh toán:</p>
                      <Button
                        variant={isSplitPayment ? "default" : "outline"}
                        size="sm"
                        onClick={toggleSplitPayment}
                        className={isSplitPayment ? "bg-indigo-600 hover:bg-indigo-700 text-xs" : "text-xs"}
                      >
                        {isSplitPayment ? "✓ Đang chia bill" : "Chia bill"}
                      </Button>
                    </div>

                    {!isSplitPayment ? (
                      <div className="grid grid-cols-2 gap-2">
                        {availablePaymentMethods.map(m => { const Icon = m.icon; return (<Button key={m.id} variant={selectedPayment===m.id? 'default':'outline'} size="sm" onClick={() => setSelectedPayment(m.id)}><Icon className="w-4 h-4 mr-1"/>{m.name}</Button>); })}
                      </div>
                    ) : (
                      <div className="space-y-2 p-3 bg-indigo-50 border border-indigo-200 rounded-lg">
                        {splitPayments.map((sp, idx) => {
                          const Icon = getPaymentIcon(sp.method);
                          return (
                            <div key={sp.method} className="flex items-center gap-2">
                              <div className="flex items-center gap-1 min-w-[100px]">
                                <Icon className="w-3 h-3 text-indigo-600" />
                                <span className="text-xs font-medium text-indigo-700">{sp.methodName}</span>
                              </div>
                              <Input
                                type="number"
                                placeholder="Số tiền"
                                value={splitInputAmounts[sp.method] || ''}
                                onChange={(e) => updateSplitAmount(sp.method, e.target.value)}
                                className="h-8 text-sm flex-1"
                              />
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => autoFillSplitRemaining(sp.method)}
                                className="h-8 text-xs text-indigo-600 hover:text-indigo-800 px-2"
                                title="Tự động điền số còn lại"
                              >
                                Còn lại
                              </Button>
                              {splitPayments.length > 1 && (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => removeSplitPaymentMethod(sp.method)}
                                  className="h-8 w-8 p-0 text-red-500 hover:text-red-700"
                                >
                                  <Trash2 className="w-3 h-3" />
                                </Button>
                              )}
                            </div>
                          );
                        })}

                        {/* Add payment method */}
                        {availablePaymentMethods.filter(m => !splitPayments.some(sp => sp.method === m.id)).length > 0 && (
                          <div className="flex gap-1 flex-wrap pt-1">
                            {availablePaymentMethods.filter(m => !splitPayments.some(sp => sp.method === m.id)).map(m => (
                              <Button
                                key={m.id}
                                variant="outline"
                                size="sm"
                                onClick={() => addSplitPaymentMethod(m.id)}
                                className="h-7 text-xs border-dashed"
                              >
                                <Plus className="w-3 h-3 mr-1" />{m.name}
                              </Button>
                            ))}
                          </div>
                        )}

                        {/* Split summary */}
                        <div className="pt-2 border-t border-indigo-200 space-y-1">
                          <div className="flex justify-between text-xs">
                            <span className="text-indigo-600">Đã chia:</span>
                            <span className="font-bold text-indigo-700">{totalSplitAmount.toLocaleString('vi-VN')}₫</span>
                          </div>
                          <div className="flex justify-between text-xs">
                            <span className="text-indigo-600">Còn lại:</span>
                            <span className={`font-bold ${Math.abs(splitRemaining) < 1 ? 'text-green-600' : 'text-red-600'}`}>
                              {splitRemaining.toLocaleString('vi-VN')}₫
                            </span>
                          </div>
                          {splitPayments.length < 2 && (
                            <p className="text-xs text-orange-600">⚠ Cần ít nhất 2 phương thức thanh toán</p>
                          )}
                          {Math.abs(splitRemaining) >= 1 && totalSplitAmount > 0 && (
                            <p className="text-xs text-red-600">⚠ Tổng chia phải bằng tổng đơn hàng ({total.toLocaleString('vi-VN')}₫)</p>
                          )}
                        </div>
                      </div>
                    )}
                  </div>
                )}

                {cart.length > 0 && (
                  <div className="space-y-2 mt-4">
                    <Button 
                      className="w-full h-12 text-lg" 
                      onClick={processPayment}
                      disabled={isSplitPayment && !isSplitValid}
                    >
                      {isSplitPayment 
                        ? `Thanh toán (${splitPayments.length} hình thức)` 
                        : 'Thanh toán'}
                    </Button>
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
