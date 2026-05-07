import { authFetch } from "@/lib/authFetch";
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
import { Search, Plus, Minus, Trash2, ShoppingCart, CreditCard, Banknote, QrCode, Smartphone, AlertTriangle, FileText, Send, Printer, Tag, Camera, ChevronLeft, ChevronRight, Clock, DollarSign, Euro, ChevronDown } from "lucide-react";
import { cn, normalizeSearchText } from "@/lib/utils";
import type { Product, Customer } from "@/types/backend-types";

const API_BASE = import.meta.env.VITE_API_BASE_URL || (import.meta.env.VITE_API_BASE_URL||'http://localhost:5273');
import { useCartDiscount, useApplyDiscount, type Discount, type DiscountCalculationResponse } from "@/hooks/useDiscount";
import { useAuth } from "@/contexts/auth-context";
import { BarcodeScanner } from "@/components/BarcodeScanner";
import { DiscountSelector, type DiscountRule } from "@/components/DiscountSelector";

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

// Icon mapping function
const getPaymentIcon = (id: string) => {
  switch (id) {
    case 'cash': return Banknote;
    case 'card': return CreditCard;
    case 'qr': return QrCode;
    case 'ewallet': return Smartphone;
    case 'banktransfer': return CreditCard;
    case 'foreignusd': return DollarSign;
    case 'foreigneur': return Euro;
    default: return Banknote;
  }
};

// Color mapping function
const getPaymentColor = (id: string) => {
  switch (id) {
    case 'cash': return 'bg-green-500';
    case 'card': return 'bg-blue-500';
    case 'qr': return 'bg-purple-500';
    case 'ewallet': return 'bg-orange-500';
    case 'banktransfer': return 'bg-indigo-500';
    case 'foreignusd': return 'bg-emerald-500';
    case 'foreigneur': return 'bg-yellow-500';
    default: return 'bg-gray-500';
  }
};

export default function Sales() {
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
  const [selectedCurrency, setSelectedCurrency] = useState<string>("USD");
  const [showPayment, setShowPayment] = useState(false);
  
  // Split payment states
  const [isSplitPayment, setIsSplitPayment] = useState(false);
  const [splitPayments, setSplitPayments] = useState<SplitPaymentEntry[]>([]);
  const [splitInputAmounts, setSplitInputAmounts] = useState<Record<string, string>>({});
  const [pendingOrderToReopen, setPendingOrderToReopen] = useState<any>(null);
  const [currentReopenedOrder, setCurrentReopenedOrder] = useState<any>(null);
  const [checkLocalStorage, setCheckLocalStorage] = useState(0); // Counter to trigger localStorage check
  const [qrCodeData, setQrCodeData] = useState<any>(null);
  const [showQRCode, setShowQRCode] = useState(false);
  const [activeProductTab, setActiveProductTab] = useState("all"); // "all" hoặc "featured"
  const [showCameraScanner, setShowCameraScanner] = useState(false);
  
  // Pagination states
  const [allProductsPage, setAllProductsPage] = useState(1);
  const [featuredProductsPage, setFeaturedProductsPage] = useState(1);
  const PRODUCTS_PER_PAGE = 10;
  
  // Discount states
  const [selectedDiscount, setSelectedDiscount] = useState<DiscountRule | Discount | null>(null);
  const [selectedDiscountAmount, setSelectedDiscountAmount] = useState(0);
  
  // Customer search state
  const [customerSearchTerm, setCustomerSearchTerm] = useState("");
  const [showCustomerDropdown, setShowCustomerDropdown] = useState(false);

  // Invoice notes state
  const [invoiceNotes, setInvoiceNotes] = useState("");

  // State for quick customer creation
  const [showQuickCustomerForm, setShowQuickCustomerForm] = useState(false);
  const [quickCustomerData, setQuickCustomerData] = useState({
    hoTen: "",
    soDienThoai: "",
    email: "",
    diaChi: "",
    hangKhachHang: "Thuong"
  });

  // Auto-focus barcode input on keypress
  useEffect(() => {
    const handleGlobalKeyPress = (e: KeyboardEvent) => {
      // Only auto-focus if not already typing in an input field
      const activeElement = document.activeElement;
      const isInputActive = activeElement?.tagName === 'INPUT' || 
                           activeElement?.tagName === 'TEXTAREA' ||
                           activeElement?.getAttribute('contenteditable') === 'true';
      
      // If typing a digit or letter and not in an input, focus barcode scanner
      if (!isInputActive && /^[a-zA-Z0-9]$/.test(e.key) && barcodeInputRef) {
        e.preventDefault();
        barcodeInputRef.focus();
        setBarcodeInput(e.key);
      }
    };

    document.addEventListener('keypress', handleGlobalKeyPress);
    return () => {
      document.removeEventListener('keypress', handleGlobalKeyPress);
      if (barcodeTimerRef.current) {
        clearTimeout(barcodeTimerRef.current);
      }
    };
  }, [barcodeInputRef]);

  // Load available stores when component mounts
  useEffect(() => {
    if (!availableStores || availableStores.length === 0) {
      loadAvailableStores();
    }
  }, [availableStores, loadAvailableStores]);

  // Check for storeId in URL params và validate permissions
  useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search);
    const storeId = urlParams.get('storeId');
    
  // debug logs removed for production cleanliness
    
    if (storeId && parseInt(storeId) !== currentStore?.storeId) {
      // Kiểm tra xem user có quyền truy cập store này không
      const targetStore = availableStores?.find(store => store.storeId === parseInt(storeId));
  // debug: target store found (log removed)
      
      if (targetStore) {
        // Chỉ switch nếu store này nằm trong danh sách availableStores (đã được filter theo permissions)
  // debug: switching to authorized store (log removed)
        switchStore(parseInt(storeId));
        // Remove storeId from URL after switching
        const newUrl = window.location.pathname;
        window.history.replaceState({}, '', newUrl);
      } else {
        // Store không có trong availableStores - user không có quyền
        console.warn('Sales page - User không có quyền truy cập store:', storeId);
        toast({
          title: "Không có quyền truy cập",
          description: "Bạn không có quyền truy cập cửa hàng này.",
          variant: "destructive",
        });
        // Remove storeId from URL
        const newUrl = window.location.pathname;
        window.history.replaceState({}, '', newUrl);
      }
    }
  }, [currentStore?.storeId, availableStores, switchStore]);
  
  // State for order detail popup
  const [showOrderDetail, setShowOrderDetail] = useState(false);
  const [orderDetailData, setOrderDetailData] = useState<any>(null);
  
  // State for e-invoice
  const [showEInvoiceForm, setShowEInvoiceForm] = useState(false);
  const [isCreateOrderWithEInvoice, setIsCreateOrderWithEInvoice] = useState(false);
  const [eInvoiceData, setEInvoiceData] = useState({
    buyerTaxCode: "",
    buyerName: "",
    buyerAddress: "",
    buyerPhone: "",
    buyerEmail: "",
    notes: ""
  });
  
  // State for discount management
  const [discountCalculation, setDiscountCalculation] = useState<DiscountCalculationResponse | null>(null);
  const [isCalculatingDiscount, setIsCalculatingDiscount] = useState(false);
  
  // State for manual discount input
  const [manualDiscountType, setManualDiscountType] = useState<'percentage' | 'fixed' | 'none'>('none');
  const [manualDiscountValue, setManualDiscountValue] = useState<string>('');
  const [manualDiscountAmount, setManualDiscountAmount] = useState<number>(0);
  const [showManualDiscount, setShowManualDiscount] = useState(false);
  
  // Initialize discount management
  const { availableDiscounts, isLoading: isLoadingDiscounts, calculateDiscountForCart } = useCartDiscount(
    cart.map(item => ({
      productId: item.productId,
      quantity: item.quantity,
      price: item.price,
      totalPrice: item.totalPrice,
    }))
  );
  
  const { applyDiscount } = useApplyDiscount();

  // Fetch payment methods configuration from backend
  const { data: paymentConfig, refetch: refetchPaymentConfig } = useQuery<PaymentConfig>({
    queryKey: ["/api/PaymentMethodConfig/enabled"],
    queryFn: async () => {
      const res = await apiRequest("/api/PaymentMethodConfig/enabled", { method: "GET" });
  // Payment config fetched - debug log removed
      return res;
    },
    staleTime: 0, // Always refetch to get latest config
    gcTime: 0, // Don't cache (replaced cacheTime)
  });

  // Fetch QR settings configuration
  const { data: qrSettings } = useQuery({
    queryKey: ["/api/QRSettings"],
    queryFn: async () => {
      const res = await apiRequest("/api/QRSettings", { method: "GET" });
      return res;
    },
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  // Fetch print configuration
  const { data: printConfig } = useQuery({
    queryKey: ["/api/PrintConfig"],
    queryFn: async () => {
      const res = await apiRequest("/api/PrintConfig", { method: "GET" });
      return res;
    },
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  // Set default payment method based on config
  useEffect(() => {
    if (paymentConfig?.defaultMethod && selectedPayment === "cash") {
      setSelectedPayment(paymentConfig.defaultMethod);
    }
  }, [paymentConfig]);

  // Listen for payment config changes from settings page
  useEffect(() => {
    const handlePaymentConfigChange = () => {
      // Payment config changed - debug log removed
      refetchPaymentConfig();
    };

    window.addEventListener('paymentConfigChanged', handlePaymentConfigChange);
    
    return () => {
      window.removeEventListener('paymentConfigChanged', handlePaymentConfigChange);
    };
  }, [refetchPaymentConfig]);

  // Get available payment methods from config
  const availablePaymentMethods: PaymentMethod[] = paymentConfig?.paymentMethods?.map((method: any) => ({
    ...method,
    icon: getPaymentIcon(method.id),
    color: getPaymentColor(method.id)
  })) || [
    // Fallback to cash only if no config
    { id: 'cash', name: 'Tiền mặt', icon: Banknote, color: 'bg-green-500', enabled: true }
  ];

  // Debug log for payment methods
  useEffect(() => {
    // Available payment methods updated - debug log removed
  }, [availablePaymentMethods]);

  // Generate QR URL based on settings
  const generateQRUrl = (amount: number, orderId?: number) => {
    if (!qrSettings?.isEnabled || !qrSettings?.bankCode || !qrSettings?.bankAccountNumber) {
      return null;
    }
    
    const template = qrSettings.qrTemplate || "compact";
    const accountName = encodeURIComponent(qrSettings.bankAccountHolder || "");
    
    // Sử dụng orderId nếu có, để tạo mô tả "thanh toan chuyen khoan don hang [mã]"
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

  // Auto-generate QR code when QR payment is selected and cart has items
  useEffect(() => {
    if (selectedPayment === 'qr' && cart.length > 0) {
      const total = cart.reduce((sum, item) => sum + item.totalPrice, 0);
      // Không truyền description để sử dụng mặc định, sẽ được cập nhật với orderId sau khi tạo đơn hàng
      generateQRMutation.mutate({
        amount: total
      });
    } else {
      setShowQRCode(false);
      setQrCodeData(null);
    }
  }, [selectedPayment, cart, selectedCustomer]);

  // Utility function để dispatch event và debug
  const dispatchReportsUpdate = (source: string) => {
    // dispatch event (debug log removed)
    window.dispatchEvent(new CustomEvent('newOrderCreated'));
  };

  // Check localStorage when location changes (when navigating to this page)
  useEffect(() => {
    if (location === '/sales') {
      // navigated to sales page - debug log removed
      setCheckLocalStorage(prev => prev + 1);
    }
  }, [location]);

  // Also check localStorage immediately when component mounts
  useEffect(() => {
    // Sales component mounted - debug log removed
    setCheckLocalStorage(prev => prev + 1);
  }, []);

  // Check for order to reopen from localStorage
  useEffect(() => {
    const reopenOrderData = localStorage.getItem('reopenOrder');
  // Checking for reopen order data - debug log removed
    if (reopenOrderData) {
      try {
        const orderDetail = JSON.parse(reopenOrderData);
  // Parsed order detail - debug log removed
        loadOrderIntoCart(orderDetail);
        // Clear the data after loading
        localStorage.removeItem('reopenOrder');
      } catch (error) {
        console.error('Error parsing reopen order data:', error);
        localStorage.removeItem('reopenOrder');
      }
    }
  }, [checkLocalStorage]); // Add checkLocalStorage as dependency

  // Listen for focus events to check localStorage when user returns to tab
  useEffect(() => {
    const handleFocus = () => {
      // Window focused - debug log removed
      setCheckLocalStorage(prev => prev + 1); // Trigger localStorage check
    };

    const handleVisibilityChange = () => {
      if (!document.hidden) {
  // Tab became visible - debug log removed
        setCheckLocalStorage(prev => prev + 1); // Trigger localStorage check
      }
    };

    // Listen for custom event from reopen order actions
    const handleReopenOrderSet = () => {
      // Reopen order event received - debug log removed
      setCheckLocalStorage(prev => prev + 1); // Trigger localStorage check
    };

    window.addEventListener('focus', handleFocus);
    document.addEventListener('visibilitychange', handleVisibilityChange);
    window.addEventListener('reopenOrderSet', handleReopenOrderSet);

    return () => {
      window.removeEventListener('focus', handleFocus);
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      window.removeEventListener('reopenOrderSet', handleReopenOrderSet);
    };
  }, []);

  // Function to load pending order into cart
  const loadOrderIntoCart = (orderDetail: any) => {
    // Loading order into cart - debug log removed
    
    // Store the current reopened order info
    setCurrentReopenedOrder(orderDetail);
    
    const cartItems: CartItem[] = orderDetail.items.map((item: any, index: number) => ({
      productId: item.productId || 0,
      name: item.productName,
      barcode: null,
      categoryId: null,
      productGroupId: null,
      price: item.price,
      costPrice: null,
      stockQuantity: 100, // Default value
      minStockLevel: 0,
      unit: null,
      imageUrl: null,
      description: '',
      quantity: item.quantity,
      totalPrice: item.totalPrice,
      cartItemId: `cart-${Date.now()}-${index}`,
    }));
    
  // Cart items created - debug log removed
  setCart(cartItems);
    
    // Set customer if available
    if (orderDetail.customer) {
      setSelectedCustomer(orderDetail.customer);
    }
    
    // Set payment method if available
    if (orderDetail.paymentMethod) {
      setSelectedPayment(orderDetail.paymentMethod);
    }
    
    // Không cần thông báo ở đây nữa vì đã có thông báo khi bấm "Mở lại đơn hàng"
    // toast({
    //   title: "Đã tải lại đơn hàng",
    //   description: `Đơn hàng #${orderDetail.orderId} đã được tải vào giỏ hàng`,
    // });
  };

  // Fetch products and customers
  const { data: products = [], isLoading: productsLoading, error: productsError } = useQuery<Product[]>({
    queryKey: ['products-sales', currentStore?.storeId], // Fixed: removed Date.now()
    queryFn: async () => {
      try {
        // Tạo tham số URL để lấy tất cả sản phẩm
        const params = new URLSearchParams({
          pageSize: '9999', // Lấy tất cả sản phẩm (số lớn để không bị giới hạn)
          page: '1'
        });
        
        if (currentStore?.storeId) {
          params.append('storeId', currentStore.storeId.toString());
        }
        
  const url = `${API_BASE}/api/products?${params.toString()}`;
  // PRODUCTS QUERY - starting fetch (debug log removed)
        
        const response = await authFetch(url);
        
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
  // PRODUCTS QUERY - success (debug log removed)
        
        // Xử lý cả 2 trường hợp: response trực tiếp là array hoặc có thuộc tính products/Products
        if (Array.isArray(result)) {
          return result;
        } else if (result.products) {
          return result.products;
        } else if (result.Products) {
          return result.Products;
        } else {
          console.warn('PRODUCTS QUERY - Unexpected response format:', result);
          return [];
        }
      } catch (error) {
        console.error('PRODUCTS QUERY - Error:', error);
        throw error;
      }
    },
    enabled: !!currentStore?.storeId, // Only when we have storeId
  });

  // Product debug logs removed to reduce console noise

  // Fetch featured products
  const { data: featuredProducts = [], isLoading: featuredLoading } = useQuery<Product[]>({
    queryKey: ['/api/products/featured', currentStore?.storeId],
    enabled: !!currentStore?.storeId, // Enable when store is available
    queryFn: async () => {
      try {
        // Tạo tham số URL để lấy tất cả sản phẩm hay bán
        const params = new URLSearchParams({
          pageSize: '9999', // Lấy tất cả sản phẩm hay bán
          page: '1'
        });
        
        if (currentStore?.storeId) {
          params.append('storeId', currentStore.storeId.toString());
        }
        
  const url = `/api/products/featured?${params.toString()}`;
  // FEATURED PRODUCTS QUERY - fetching URL (debug log removed)
        
        const response = await apiRequest(url, {
          method: 'GET'
        });
        
  // FEATURED PRODUCTS QUERY - success (debug log removed)
        
        // Xử lý nhiều format response khác nhau
        if (response && Array.isArray(response.products)) {
          return response.products;
        } else if (Array.isArray(response)) {
          return response;
        } else if (response && response.Products && Array.isArray(response.Products)) {
          return response.Products;
        } else {
          console.warn('FEATURED PRODUCTS QUERY - Unexpected response format:', response);
          return [];
        }
      } catch (error) {
        console.error('FEATURED PRODUCTS QUERY - Error:', error);
        throw error;
      }
    },
  });

  const { data: customers = [] } = useQuery<Customer[]>({
    queryKey: ['/api/customers', currentStore?.storeId],
    queryFn: async () => {
      const storeParam = currentStore?.storeId ? `?storeId=${currentStore.storeId}` : '';
      const rawCustomers = await apiRequest(`/api/customers${storeParam}`, { method: 'GET' });
      return rawCustomers;
    },
    select: (rawCustomers: any[]) => rawCustomers.map((c) => ({
      customerId: c.customerId,
      hoTen: c.hoTen || '',
      soDienThoai: c.soDienThoai || '',
      email: c.email || '',
      diaChi: c.diaChi || '',
      hangKhachHang: c.hangKhachHang || '',
      // Mapped fields for compatibility
      id: c.customerId?.toString(),
      name: c.hoTen || '',
      phone: c.soDienThoai || '',
      address: c.diaChi || '',
      customerType: c.hangKhachHang === 'VIP' ? 'vip' : 
                   c.hangKhachHang === 'Premium' ? 'premium' : 'regular',
    })),
    enabled: !!currentStore?.storeId,
  });

  // Fetch store info
  const { data: storeInfo } = useQuery<StoreInfo | null>({
    queryKey: ["/api/StoreInfo"],
    queryFn: async () => {
      const res = await apiRequest("/api/StoreInfo", { method: "GET" });
      if (res.status === 404) return null;
      return typeof res === "string" ? JSON.parse(res) : res;
    },
  });

  // Fetch e-invoice config
  const { data: eInvoiceConfig } = useQuery({
    queryKey: ["/api/EInvoice/config"],
    queryFn: async () => {
      return await apiRequest("/api/EInvoice/config", { method: "GET" });
    },
  });

  // Create e-invoice mutation
  const createEInvoiceMutation = useMutation({
    mutationFn: async (data: { orderId: number; buyerInfo: any }) => {
      return await apiRequest('/api/EInvoice/create-from-order', { 
        method: 'POST', 
        body: JSON.stringify({
          orderId: data.orderId,
          buyerTaxCode: data.buyerInfo.buyerTaxCode,
          buyerName: data.buyerInfo.buyerName,
          buyerAddress: data.buyerInfo.buyerAddress,
          buyerPhone: data.buyerInfo.buyerPhone,
          buyerEmail: data.buyerInfo.buyerEmail,
          notes: data.buyerInfo.notes
        }),
        headers: {
          'Content-Type': 'application/json'
        }
      });
    },
    onSuccess: (response) => {
      toast({
        title: "Thành công",
        description: "Hóa đơn điện tử đã được tạo thành công",
      });
      setShowEInvoiceForm(false);
      setEInvoiceData({
        buyerTaxCode: "",
        buyerName: "",
        buyerAddress: "",
        buyerPhone: "",
        buyerEmail: "",
        notes: ""
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Không thể tạo hóa đơn điện tử",
        variant: "destructive",
      });
    },
  });

  // Create order mutation
  const createOrderMutation = useMutation({
    mutationFn: async (formData: FormData) => {
  // Sending order data to backend (debug log removed)
      return await apiRequest('/api/orders', { method: 'POST', body: formData });
    },
    onSuccess: async (response) => {
      // Apply discount if selected
      if (selectedDiscount && response?.orderId) {
        try {
          await applyDiscount(response.orderId, selectedDiscount.id);
          // Discount applied successfully - debug log removed
        } catch (error) {
          console.error('Failed to apply discount:', error);
          // Still show success for order creation even if discount fails
          toast({
            title: "Cảnh báo", 
            description: "Đơn hàng đã tạo thành công nhưng không thể áp dụng giảm giá",
            variant: "destructive",
          });
        }
      }
      
      // For manual discount, we'll store it in order notes/description for now
      // (since it's ad-hoc and doesn't need to be tracked like predefined discounts)
      if (manualDiscountAmount > 0 && response?.orderId) {
        // Manual discount applied - debug log removed
      }
      
      // Process loyalty points for the order
      if (response?.orderId) {
        try {
          const loyaltyResponse = await apiRequest(`/api/LoyaltyProcess/process-order/${response.orderId}`, { 
            method: 'POST' 
          });
          // Loyalty points processed - debug log removed
        } catch (error) {
          console.error('Failed to process loyalty points:', error);
          // Don't show error to user since order creation was successful
        }
      }
      
      // Toast notification removed
      
      // Tạo object order detail để hiển thị trong popup
      const orderDetail = {
        orderId: response?.orderId,
        customerName: selectedCustomer?.name || "Khách lẻ",
        createdAt: new Date().toISOString(),
        totalAmount: total, // Use final total with discount
        subtotal: subtotal,
        discountAmount: totalDiscountAmount, // Use total discount amount (includes manual)
        discountName: selectedDiscount?.name || (manualDiscountAmount > 0 ? 'Giảm giá thủ công' : null),
        manualDiscountAmount: manualDiscountAmount,
        discountType: selectedDiscount ? 
          (selectedDiscount.type === 'PercentageTotal' ? `${selectedDiscount.value}% tổng bill` :
           selectedDiscount.type === 'FixedAmountTotal' ? `${selectedDiscount.value.toLocaleString('vi-VN')}₫ tổng bill` :
           `${selectedDiscount.value.toLocaleString('vi-VN')}₫ từng món`) :
          (manualDiscountAmount > 0 ? 
            `${manualDiscountType === 'percentage' ? manualDiscountValue + '%' : manualDiscountValue + '₫'} tổng bill` 
            : null),
        items: cart.map(item => ({
          productName: item.name,
          quantity: item.quantity,
          price: item.price,
          totalPrice: item.totalPrice,
          unit: item.unit || ''
        })),
        taxAmount: taxAmount,
        paymentMethod: getPaymentMethodDisplay(),
        splitPaymentDetails: isSplitPayment ? splitPayments : null,
        paymentStatus: 'paid',
        status: 'completed',
        cashierName: 'Admin',
        notes: invoiceNotes.trim() || null // Thêm notes từ form
      };
      
      // Hiển thị popup chi tiết hóa đơn
      setOrderDetailData(orderDetail);
      setShowOrderDetail(true);
      
      // Removed auto print functionality
      
      // Clear cart và state
      setCart([]);
      setSelectedCustomer(null);
      setShowPayment(false);
      setSelectedDiscount(null);
      setDiscountCalculation(null);
      clearManualDiscount(); // Clear manual discount state
      setInvoiceNotes(""); // Clear invoice notes
      setIsSplitPayment(false); // Clear split payment
      setSplitPayments([]);
      setSplitInputAmounts({});
      
      // Refetch tất cả dữ liệu liên quan
      queryClient.invalidateQueries({ queryKey: ['/api/orders'] });
      queryClient.invalidateQueries({ queryKey: ['/api/notifications'] });
      queryClient.invalidateQueries({ queryKey: ['/api/notifications/count'] });
      
      // Dispatch event để cập nhật reports real-time
      window.dispatchEvent(new CustomEvent('newOrderCreated'));
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể tạo đơn hàng. Vui lòng thử lại.",
        variant: "destructive",
      });
    }
  });

  // Save order for later mutation (for pending orders)
  const saveOrderForLaterMutation = useMutation({
    mutationFn: async (formData: FormData) => {
      // Sending pending order to backend (debug log removed)
      return await apiRequest('/api/orders', { method: 'POST', body: formData });
    },
    onSuccess: () => {
      // Thông báo ngay lập tức với thông tin chi tiết hơn
      const customerName = selectedCustomer?.name || "Khách vãng lai";
      const orderTotal = total.toLocaleString('vi-VN');
      
      toast({
        title: "📋 Đơn hàng đã được lưu!",
        description: (
          <div>
            <p>Khách hàng: <strong>{customerName}</strong></p>
            <p>Tổng tiền: <strong>{orderTotal}₫</strong></p>
            <p>Trạng thái: <em>Chờ thanh toán</em></p>
            <p className="text-xs mt-1 text-blue-600">
              💡 Có thể thanh toán sau từ trang "Lịch sử hóa đơn"
            </p>
          </div>
        ),
        duration: 5000,
      });
      
      // Phát âm thanh thông báo
      playNotificationSound();
      
      // Clear cart và navigate
      setCart([]);
      setSelectedCustomer(null);
      setCustomerSearchTerm("");
      setShowPayment(false);
      setInvoiceNotes(""); // Clear invoice notes
      
      // Refetch danh sách đơn hàng và notifications
      queryClient.invalidateQueries({ queryKey: ['/api/orders'] });
      queryClient.invalidateQueries({ queryKey: ['/api/notifications'] });
      queryClient.invalidateQueries({ queryKey: ['/api/notifications/count'] });
      
      // Dispatch event để cập nhật reports real-time
      window.dispatchEvent(new CustomEvent('newOrderCreated'));
      
      navigate('/orders');
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể lưu đơn hàng. Vui lòng thử lại.",
        variant: "destructive",
      });
    }
  });

  // Complete order mutation (for reopened orders)
  const completeOrderMutation = useMutation({
    mutationFn: async ({ orderId, formData }: { orderId: number, formData: FormData }) => {
      // Updating order (complete) - debug log removed
      return await apiRequest(`/api/orders/${orderId}/complete`, { method: 'PUT', body: formData });
    },
    onSuccess: () => {
      // Thông báo ngay lập tức với âm thanh
      toast({
        title: "Thanh toán thành công! 🎉",
        description: `Đơn hàng #${currentReopenedOrder?.orderId} của ${selectedCustomer?.name || currentReopenedOrder?.customerName || "khách vãng lai"} đã được thanh toán`,
      });
      
      // Phát âm thanh thông báo
      playNotificationSound();
      
      // Tạo object order detail để hiển thị trong popup
      const orderDetail = {
        orderId: currentReopenedOrder?.orderId,
        customerName: selectedCustomer?.name || currentReopenedOrder?.customerName || "Khách lẻ",
        createdAt: currentReopenedOrder?.createdAt || new Date().toISOString(),
        totalAmount: cart.reduce((sum, item) => sum + item.totalPrice, 0),
        items: cart.map(item => ({
          productName: item.name,
          quantity: item.quantity,
          price: item.price,
          totalPrice: item.totalPrice,
          unit: item.unit || ''
        })),
        taxAmount: taxAmount,
        paymentMethod: getPaymentMethodDisplay(),
        splitPaymentDetails: isSplitPayment ? splitPayments : null,
        paymentStatus: 'paid',
        status: 'completed',
        cashierName: 'Admin',
        notes: invoiceNotes.trim() || null // Thêm notes từ form
      };
      
      // Hiển thị popup chi tiết hóa đơn
      setOrderDetailData(orderDetail);
      setShowOrderDetail(true);
      
      // Process loyalty points for the completed order
      if (currentReopenedOrder?.orderId) {
        (async () => {
            try {
              const loyaltyResponse = await apiRequest(`/api/LoyaltyProcess/process-order/${currentReopenedOrder.orderId}`, { 
                method: 'POST' 
              });
              // Loyalty points processed for completed order - debug log removed
            } catch (error) {
              console.error('Failed to process loyalty points for completed order:', error);
            }
        })();
      }
      
      // Clear state
      setCart([]);
      setSelectedCustomer(null);
      setShowPayment(false);
      setCurrentReopenedOrder(null); // Clear reopened order
      setInvoiceNotes(""); // Clear invoice notes
      setIsSplitPayment(false); // Clear split payment
      setSplitPayments([]);
      setSplitInputAmounts({});
      
      // Refetch tất cả dữ liệu liên quan
      queryClient.invalidateQueries({ queryKey: ['/api/orders'] });
      queryClient.invalidateQueries({ queryKey: ['/api/notifications'] });
      queryClient.invalidateQueries({ queryKey: ['/api/notifications/count'] });
      
      // Dispatch event để cập nhật reports real-time
      window.dispatchEvent(new CustomEvent('newOrderCreated'));
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể thanh toán đơn hàng. Vui lòng thử lại.",
        variant: "destructive",
      });
    }
  });

  // Generate QR Code mutation
  const generateQRMutation = useMutation({
    mutationFn: async ({ amount, orderId, description }: { amount: number, orderId?: string, description?: string }) => {
      // Tạo URL với orderId để có format "thanh toan chuyen khoan don hang [mã]"
      let url = `/api/QRSettings/generate-url?amount=${amount}`;
      
      if (orderId) {
        url += `&orderId=${encodeURIComponent(orderId)}`;
      } else if (description) {
        url += `&description=${encodeURIComponent(description)}`;
      }
      
      return await apiRequest(url, { method: "GET" });
    },
    onSuccess: (data) => {
      setQrCodeData(data);
      setShowQRCode(true);
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi tạo QR",
        description: error.message || "Không thể tạo mã QR. Vui lòng kiểm tra cấu hình trong Settings > QR Code.",
        variant: "destructive",
      });
    }
  });

  // Create customer mutation
  const createCustomerMutation = useMutation({
    mutationFn: async (customerData: any) => {
      const storeParam = currentStore?.storeId ? `?storeId=${currentStore.storeId}` : '';
      
      // Format data to match backend API expectations
      const requestData = {
        hoTen: customerData.hoTen,
        soDienThoai: customerData.soDienThoai,
        email: customerData.email || '',
        diaChi: customerData.diaChi || '',
        hangKhachHang: customerData.hangKhachHang,
        storeId: currentStore?.storeId || 'store-1',
        customerType: customerData.hangKhachHang === 'VIP' ? 'vip' : 
                     customerData.hangKhachHang === 'Premium' ? 'premium' : 'regular'
      };
      
  // Creating customer - debug log removed
      
      return await apiRequest(`/api/customers${storeParam}`, { 
        method: 'POST', 
        body: JSON.stringify(requestData),
        headers: {
          'Content-Type': 'application/json'
        }
      });
    },
    onSuccess: (response) => {
      // Map the response to the expected format
      const newCustomer = {
        customerId: response.customerId,
        hoTen: response.hoTen || quickCustomerData.hoTen,
        soDienThoai: response.soDienThoai || quickCustomerData.soDienThoai,
        email: response.email || quickCustomerData.email,
        diaChi: response.diaChi || quickCustomerData.diaChi,
        hangKhachHang: response.hangKhachHang || quickCustomerData.hangKhachHang,
        // Mapped fields for compatibility
        id: response.customerId?.toString(),
        name: response.hoTen || quickCustomerData.hoTen,
        phone: response.soDienThoai || quickCustomerData.soDienThoai,
        address: response.diaChi || quickCustomerData.diaChi,
        customerType: (response.hangKhachHang || quickCustomerData.hangKhachHang) === 'VIP' ? 'vip' : 
                     (response.hangKhachHang || quickCustomerData.hangKhachHang) === 'Premium' ? 'premium' : 'regular',
      };

      // Set as selected customer
      setSelectedCustomer(newCustomer);
      setCustomerSearchTerm(newCustomer.name);
      
      // Clear form and close
      setQuickCustomerData({
        hoTen: "",
        soDienThoai: "",
        email: "",
        diaChi: "",
        hangKhachHang: "Thuong"
      });
      setShowQuickCustomerForm(false);
      setShowCustomerDropdown(false);
      
      // Refresh customers list
      queryClient.invalidateQueries({ queryKey: ['/api/customers'] });
      
      toast({
        title: "Thành công! 🎉",
        description: `Đã tạo khách hàng ${newCustomer.name} và chọn làm khách hàng cho đơn hàng này`,
      });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi tạo khách hàng",
        description: error.message || "Không thể tạo khách hàng mới. Vui lòng thử lại.",
        variant: "destructive",
      });
    }
  });

  // Filter products based on search with Vietnamese diacritics support
  const filteredProducts = (products || []).filter(product => {
    const searchNormalized = normalizeSearchText(searchTerm);
    const productNameNormalized = normalizeSearchText(product.name || '');
    const productBarcodeNormalized = normalizeSearchText(product.barcode || '');
    
    return productNameNormalized.includes(searchNormalized) ||
           productBarcodeNormalized.includes(searchNormalized);
  });

  // Pagination logic for all products
  const totalAllProductsPages = Math.ceil(filteredProducts.length / PRODUCTS_PER_PAGE);
  const startAllProductsIndex = (allProductsPage - 1) * PRODUCTS_PER_PAGE;
  const endAllProductsIndex = startAllProductsIndex + PRODUCTS_PER_PAGE;
  const paginatedAllProducts = filteredProducts.slice(startAllProductsIndex, endAllProductsIndex);

  // Pagination logic for featured products
  const totalFeaturedProductsPages = Math.ceil((featuredProducts || []).length / PRODUCTS_PER_PAGE);
  const startFeaturedProductsIndex = (featuredProductsPage - 1) * PRODUCTS_PER_PAGE;
  const endFeaturedProductsIndex = startFeaturedProductsIndex + PRODUCTS_PER_PAGE;
  const paginatedFeaturedProducts = (featuredProducts || []).slice(startFeaturedProductsIndex, endFeaturedProductsIndex);

  // Reset page when search changes
  useEffect(() => {
    setAllProductsPage(1);
  }, [searchTerm]);

  // Reset page when switching tabs
  useEffect(() => {
    if (activeProductTab === 'all') {
      setAllProductsPage(1);
    } else {
      setFeaturedProductsPage(1);
    }
  }, [activeProductTab]);

  // Close customer dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as Element;
      if (!target.closest('.customer-search-container')) {
        setShowCustomerDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  // Filter customers based on search term
  const filteredCustomers = (customers || []).filter(customer => {
    const searchNormalized = normalizeSearchText(customerSearchTerm);
    const nameNormalized = normalizeSearchText(customer.name || '');
    const phoneNormalized = normalizeSearchText(customer.phone || '');
    
    return nameNormalized.includes(searchNormalized) ||
           phoneNormalized.includes(searchNormalized);
  });

  // Pagination Component
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
            
            {/* Show page numbers - simplified for mobile */}
            {totalPages <= 5 ? (
              // Show all pages if 5 or fewer
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
              // Show simplified pagination for many pages
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

  // Lấy thông tin thuế VAT từ API
  const { data: taxConfig } = useQuery<any>({
    queryKey: ["/api/TaxConfig"],
    queryFn: async () => {
      const res = await apiRequest("/api/TaxConfig", { method: "GET" });
      return typeof res === "string" ? JSON.parse(res) : res;
    },
  });

  const subtotal = cart.reduce((sum, item) => sum + item.totalPrice, 0);
  
  // Kiểm tra xem thuế VAT có được bật hay không - hỗ trợ cả PascalCase và camelCase
  const isVATEnabled = Boolean(
    taxConfig?.EnableVAT || 
    taxConfig?.enableVAT || 
    false
  );
  
  // Lấy tax rate - hỗ trợ cả hai format
  const taxRate = Number(
    taxConfig?.VATRate || 
    taxConfig?.vatRate || 
    10
  );
  
  // Lấy tax label - hỗ trợ cả hai format  
  const taxLabel = String(
    taxConfig?.VATLabel || 
    taxConfig?.vatLabel || 
    "VAT"
  );
  
  // Chỉ tính thuế nếu VAT được bật
  const taxAmount = isVATEnabled ? subtotal * (taxRate / 100) : 0;
  
  // Calculate discount amount
  const cartDiscountAmount = discountCalculation?.canApply ? discountCalculation.discountAmount : 0;
  const totalDiscountAmount = Math.max(cartDiscountAmount, manualDiscountAmount, selectedDiscountAmount);
  
  // Calculate final total with discount
  const total = subtotal + taxAmount - totalDiscountAmount;

  // Add product to cart
  const addToCart = (product: Product) => {
    // Adding product to cart - debug log removed
    
    // Kiểm tra tồn kho trước khi thêm vào giỏ hàng
    if (product.stockQuantity <= 0) {
      toast({
        title: "Hết hàng",
        description: "Vui lòng nhập thêm hàng sau đó quay lại bán",
        variant: "destructive",
      });
      return;
    }
    
    // Clear reopened order if user manually adds products
    if (currentReopenedOrder) {
      setCurrentReopenedOrder(null);
    }
    
    // Luôn thêm một dòng mới, không gộp số lượng
    const newItem: CartItem = {
      ...product,
      cartItemId: `${Date.now()}-${Math.random()}`,
      quantity: 1,
      totalPrice: Number(product.price)
    };
    setCart([...cart, newItem]);
  };

  // Handle barcode scan
  const handleBarcodeSubmit = (barcode: string) => {
    // Scanning barcode - debug log removed
    
    if (!barcode.trim()) {
      return;
    }
    
    // Normalize barcode for search (remove spaces, lowercase)
  const normalizedBarcode = barcode.trim().toLowerCase().replace(/\s+/g, '');
    
    // Combine all products and featured products for comprehensive search
    const allProductsForSearch = [
      ...(products || []),
      ...(featuredProducts || [])
    ];
    
    // Remove duplicates based on product ID
    const uniqueProducts = allProductsForSearch.filter((product, index, self) => 
      index === self.findIndex((p) => p.productId === product.productId)
    );
    
    // Product debug logs removed (counts and barcode listing)
    
    // Special-case debug removed for SP002322
    
    // Find product by barcode with flexible matching - now searching in combined array
    const product = uniqueProducts.find(p => {
      if (!p.barcode) return false;
      
      // Normalize product barcode too
      const normalizedProductBarcode = p.barcode.trim().toLowerCase().replace(/\s+/g, '');
      
  // Comparing normalized barcodes (debug log removed)
      
      // Try exact match first
      if (normalizedProductBarcode === normalizedBarcode) {
        // Exact match found (debug log removed)
        return true;
      }
      
      // Try partial match (contains)
      if (normalizedProductBarcode.includes(normalizedBarcode) || normalizedBarcode.includes(normalizedProductBarcode)) {
        // Partial match found (debug log removed)
        return true;
      }
      
      return false;
    });
    
  // Found product - debug log removed
    
    if (product) {
      addToCart(product);
      
      // Play success sound (using existing notification sound)
      playNotificationSound();
      
      toast({
        title: "✅ Quét thành công",
        description: `${product.name} đã được thêm vào giỏ hàng`,
        duration: 2000,
      });
      setBarcodeInput(""); // Clear barcode input
      
      // Focus back to barcode input for next scan
      if (barcodeInputRef) {
        setTimeout(() => barcodeInputRef.focus(), 100);
      }
    } else {
      // No product found for barcode - debug logs removed
      
      // Try to refresh products data first
      queryClient.invalidateQueries({ queryKey: ['products-sales'] });
      queryClient.invalidateQueries({ queryKey: ['/api/products/featured'] });
      
      // Wait and try again after refresh
      setTimeout(() => {
        // Re-fetch products from query cache
        const refreshedProducts = queryClient.getQueryData(['products-sales', currentStore?.storeId]) as Product[] || [];
        const refreshedFeatured = queryClient.getQueryData(['/api/products/featured', currentStore?.storeId]) as Product[] || [];
        
        // Combine refreshed data
        const allRefreshedProducts = [
          ...refreshedProducts,
          ...refreshedFeatured
        ];
        
        // Remove duplicates
        const uniqueRefreshedProducts = allRefreshedProducts.filter((product, index, self) => 
          index === self.findIndex((p) => p.productId === product.productId)
        );
        
  // After refresh - debug logs removed
        
        // Try to find the product again with refreshed data
        const productAfterRefresh = uniqueRefreshedProducts.find(p => {
          if (!p.barcode) return false;
          
          const normalizedProductBarcode = p.barcode.trim().toLowerCase().replace(/\s+/g, '');
          // Re-checking normalized barcodes - debug log removed
          
          return normalizedProductBarcode === normalizedBarcode || 
                 normalizedProductBarcode.includes(normalizedBarcode) || 
                 normalizedBarcode.includes(normalizedProductBarcode);
        });
        
        if (productAfterRefresh) {
          // Found product after refresh - debug log removed
          addToCart(productAfterRefresh);
          playNotificationSound();
          toast({
            title: "✅ Quét thành công (sau refresh)",
            description: `${productAfterRefresh.name} đã được thêm vào giỏ hàng`,
            duration: 2000,
          });
          setBarcodeInput("");
          if (barcodeInputRef) {
            setTimeout(() => barcodeInputRef.focus(), 100);
          }
          return;
        }
        
  // Still not found after refresh - debug log removed
        
        // Check if we can find any similar products to suggest
        const similarProducts = (products || []).filter(p => 
          p.barcode && p.barcode.toLowerCase().includes(normalizedBarcode.substring(0, 3))
        );
        
  // Similar products found - debug log removed
        
        toast({
          title: "❌ Không tìm thấy sản phẩm",
          description: (
            <div>
              <p>Không có sản phẩm nào có mã vạch: {barcode}</p>
              <p className="text-xs mt-1">Kiểm tra console để xem chi tiết hoặc thêm sản phẩm mới.</p>
              {similarProducts.length > 0 && (
                <p className="text-xs mt-1 text-blue-600">
                  Tìm thấy {similarProducts.length} sản phẩm tương tự. Kiểm tra console.
                </p>
              )}
            </div>
          ),
          variant: "destructive",
          duration: 10000,
          action: (
            <div className="flex gap-2">
              <button
                onClick={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  
                  // Navigate to products page to add new product with pre-filled barcode
                  const params = new URLSearchParams({ barcode: barcode });
                  navigate(`/products?${params.toString()}`);
                }}
                className="bg-white text-red-600 px-2 py-1 rounded text-xs hover:bg-gray-100"
              >
                Thêm sản phẩm
              </button>
              {similarProducts.length > 0 && (
                <button
                  onClick={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    
                    // Show similar products and auto-add if only one result (debug logs removed)
                    if (similarProducts.length === 1) {
                      addToCart(similarProducts[0]);
                      playNotificationSound();
                      toast({
                        title: "✅ Thêm sản phẩm tương tự",
                        description: `${similarProducts[0].name} đã được thêm vào giỏ hàng`,
                        duration: 2000,
                      });
                      setBarcodeInput("");
                    }
                  }}
                  className="bg-blue-500 text-white px-2 py-1 rounded text-xs hover:bg-blue-600"
                >
                  Sản phẩm tương tự
                </button>
              )}
            </div>
          ),
        });
        
        // Clear invalid barcode after showing error
        setTimeout(() => {
          setBarcodeInput("");
          if (barcodeInputRef) {
            barcodeInputRef.focus();
          }
        }, 2000);
      }, 1500); // Wait a bit longer for refresh to complete
    }
  };

  // Handle camera scan
  const handleCameraScan = (code: string) => {
    // Camera scanned - debug log removed
    setShowCameraScanner(false);
    
    // Use the same logic as barcode input
    handleBarcodeSubmit(code);
  };

  // Handle barcode input change with auto-submit timer
  const handleBarcodeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setBarcodeInput(value);
    
    // Clear any existing timer
    if (barcodeTimerRef.current) {
      clearTimeout(barcodeTimerRef.current);
    }
    
    // Auto-submit after 500ms of no typing (typical barcode scanner behavior)
    if (value.length >= 3) { // Minimum barcode length
      barcodeTimerRef.current = setTimeout(() => {
        handleBarcodeSubmit(value);
      }, 500);
    }
  };

  // Handle barcode input keydown for immediate scanning
  const handleBarcodeKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      if (barcodeTimerRef.current) {
        clearTimeout(barcodeTimerRef.current);
      }
      handleBarcodeSubmit(barcodeInput);
    }
  };

  // Update quantity
  const updateQuantity = (productId: string, newQuantity: number) => {
    if (newQuantity <= 0) {
      removeFromCart(productId);
      return;
    }
    
    // Tìm sản phẩm trong giỏ hàng để kiểm tra tồn kho
    const cartItem = cart.find(item => item.cartItemId === productId);
    if (cartItem && newQuantity > cartItem.stockQuantity) {
      toast({
        title: "Không đủ hàng",
        description: `Chỉ còn ${cartItem.stockQuantity} sản phẩm trong kho`,
        variant: "destructive",
      });
      return;
    }
    
    setCart(cart.map(item => 
      item.cartItemId === productId 
        ? { 
            ...item, 
            quantity: newQuantity, 
            totalPrice: Number(item.price) * newQuantity 
          }
        : item
    ));
  };

  // Remove from cart
  const removeFromCart = (productId: string) => {
    setCart(cart.filter(item => item.cartItemId !== productId));
  };

  // Clear cart
  const clearCart = () => {
    setCart([]);
    setCurrentReopenedOrder(null); // Also clear reopened order
    setSelectedDiscount(null); // Clear selected discount
    setDiscountCalculation(null); // Clear discount calculation
    clearManualDiscount(); // Clear manual discount
    setInvoiceNotes(""); // Clear invoice notes
  };

  // Handle discount selection
  const handleDiscountSelect = async (discountId: string) => {
    if (!discountId || discountId === 'none') {
      setSelectedDiscount(null);
      setDiscountCalculation(null);
      return;
    }

    const discount = availableDiscounts.find(d => d.id.toString() === discountId);
    if (!discount) return;

    setSelectedDiscount(discount);
    setIsCalculatingDiscount(true);

    try {
      const orderTotal = subtotal + taxAmount;
      const calculation = await calculateDiscountForCart(discount.id, orderTotal);
      setDiscountCalculation(calculation);
      
      if (calculation && !calculation.canApply) {
        toast({
          title: "Không thể áp dụng giảm giá",
          description: calculation.message || "Đơn hàng không đủ điều kiện",
          variant: "destructive",
        });
        setSelectedDiscount(null);
        setDiscountCalculation(null);
      }
    } catch (error) {
      console.error('Error calculating discount:', error);
      toast({
        title: "Lỗi tính giảm giá",
        description: "Không thể tính toán giảm giá",
        variant: "destructive",
      });
      setSelectedDiscount(null);
      setDiscountCalculation(null);
    } finally {
      setIsCalculatingDiscount(false);
    }
  };

  // Handle manual discount calculation
  const calculateManualDiscount = () => {
    if (manualDiscountType === 'none' || !manualDiscountValue || Number(manualDiscountValue) <= 0) {
      setManualDiscountAmount(0);
      return;
    }

    const value = Number(manualDiscountValue);
    let discountAmount = 0;
    const totalBeforeDiscount = subtotal + taxAmount;

    if (manualDiscountType === 'percentage') {
      if (value > 100) {
        toast({
          title: "Giá trị không hợp lệ",
          description: "Phần trăm giảm giá không được vượt quá 100%",
          variant: "destructive",
        });
        return;
      }
      
      discountAmount = totalBeforeDiscount * (value / 100);
    } else if (manualDiscountType === 'fixed') {
      if (value > totalBeforeDiscount) {
        toast({
          title: "Giá trị không hợp lệ", 
          description: `Số tiền giảm giá không được vượt quá ${totalBeforeDiscount.toLocaleString('vi-VN')}₫`,
          variant: "destructive",
        });
        return;
      }
      
      discountAmount = value;
    }

    setManualDiscountAmount(discountAmount);
  };

  // Handle manual discount input change
  const handleManualDiscountChange = (value: string) => {
    setManualDiscountValue(value);
    // Reset manual discount amount when changing input
    setManualDiscountAmount(0);
  };

  // Apply manual discount
  const applyManualDiscount = () => {
    if (manualDiscountAmount > 0) {
      calculateManualDiscount();
    }
  };

  // Clear manual discount
  const clearManualDiscount = () => {
    setManualDiscountType('none');
    setManualDiscountValue('');
    setManualDiscountAmount(0);
    setShowManualDiscount(false);
    // Also clear selected discount
    setSelectedDiscount(null);
    setDiscountCalculation(null);
  };

  // ========== SPLIT PAYMENT HELPERS ==========
  
  // Toggle split payment mode
  const toggleSplitPayment = () => {
    if (isSplitPayment) {
      // Turning off split payment - reset
      setIsSplitPayment(false);
      setSplitPayments([]);
      setSplitInputAmounts({});
    } else {
      // Turning on split payment - add first method with full amount
      setIsSplitPayment(true);
      const firstMethod = availablePaymentMethods[0];
      if (firstMethod) {
        setSplitPayments([{
          method: firstMethod.id,
          methodName: firstMethod.name,
          amount: total
        }]);
        setSplitInputAmounts({ [firstMethod.id]: total.toString() });
      }
    }
  };

  // Add a payment method to split
  const addSplitPaymentMethod = (methodId: string) => {
    const method = availablePaymentMethods.find(m => m.id === methodId);
    if (!method) return;
    
    // Don't add if already exists
    if (splitPayments.find(sp => sp.method === methodId)) {
      toast({ title: "Đã có", description: `${method.name} đã được thêm vào danh sách thanh toán`, variant: "destructive" });
      return;
    }
    
    setSplitPayments(prev => [...prev, { method: methodId, methodName: method.name, amount: 0 }]);
    setSplitInputAmounts(prev => ({ ...prev, [methodId]: '0' }));
  };

  // Remove a payment method from split
  const removeSplitPaymentMethod = (methodId: string) => {
    setSplitPayments(prev => prev.filter(sp => sp.method !== methodId));
    setSplitInputAmounts(prev => {
      const copy = { ...prev };
      delete copy[methodId];
      return copy;
    });
  };

  // Update amount for a split payment method
  const updateSplitAmount = (methodId: string, value: string) => {
    const numValue = parseFloat(value) || 0;
    setSplitInputAmounts(prev => ({ ...prev, [methodId]: value }));
    setSplitPayments(prev => prev.map(sp => 
      sp.method === methodId ? { ...sp, amount: numValue } : sp
    ));
  };

  // Auto-fill remaining for the last empty method
  const autoFillSplitRemaining = (methodId: string) => {
    const otherTotal = splitPayments
      .filter(sp => sp.method !== methodId)
      .reduce((sum, sp) => sum + sp.amount, 0);
    const remaining = Math.max(0, total - otherTotal);
    updateSplitAmount(methodId, remaining.toString());
  };

  // Calculate total split amount
  const totalSplitAmount = splitPayments.reduce((sum, sp) => sum + sp.amount, 0);
  const splitRemaining = total - totalSplitAmount;
  const isSplitValid = isSplitPayment ? Math.abs(splitRemaining) < 1 && splitPayments.length >= 2 && splitPayments.every(sp => sp.amount > 0) : true;

  // Get the payment method display name(s) for the order
  const getPaymentMethodDisplay = () => {
    if (isSplitPayment && splitPayments.length > 0) {
      return splitPayments.map(sp => sp.methodName).join(' + ');
    }
    return availablePaymentMethods.find(m => m.id === selectedPayment)?.name || selectedPayment;
  };

  // Get split payment JSON for backend
  const getSplitPaymentJSON = () => {
    if (isSplitPayment && splitPayments.length > 0) {
      return JSON.stringify(splitPayments);
    }
    return null;
  };

  // Get primary payment method for backend compatibility
  const getPrimaryPaymentMethod = () => {
    if (isSplitPayment && splitPayments.length > 0) {
      // Send "split" to indicate this is a split payment order
      return "split";
    }
    return selectedPayment;
  };

  // Process payment
  const processPayment = () => {
    if (cart.length === 0) {
      toast({
        title: "Giỏ hàng trống",
        description: "Vui lòng thêm sản phẩm vào giỏ hàng",
        variant: "destructive",
      });
      return;
    }

    // Validate split payment
    if (isSplitPayment) {
      if (splitPayments.length < 2) {
        toast({
          title: "Thanh toán chia nhỏ",
          description: "Vui lòng chọn ít nhất 2 phương thức thanh toán",
          variant: "destructive",
        });
        return;
      }
      if (Math.abs(splitRemaining) >= 1) {
        toast({
          title: "Số tiền không khớp",
          description: `Tổng số tiền phân bổ (${totalSplitAmount.toLocaleString('vi-VN')}₫) chưa khớp với tổng đơn hàng (${total.toLocaleString('vi-VN')}₫). Còn thiếu: ${splitRemaining.toLocaleString('vi-VN')}₫`,
          variant: "destructive",
        });
        return;
      }
      if (splitPayments.some(sp => sp.amount <= 0)) {
        toast({
          title: "Số tiền không hợp lệ",
          description: "Mỗi phương thức thanh toán phải có số tiền > 0",
          variant: "destructive",
        });
        return;
      }
    }

    // Kiểm tra xem có đang mở lại đơn hàng không
    if (currentReopenedOrder) {
      // Nếu đang mở lại đơn hàng, cập nhật đơn hàng hiện tại
      completeReopenedOrder();
    } else {
      // Nếu không, tạo đơn hàng mới như bình thường
      createNewOrder();
    }
  };

  // Complete reopened order (update existing order)
  const completeReopenedOrder = () => {
    // Completing reopened order - debug log removed
    const formData = new FormData();
    formData.append('paymentMethod', getPrimaryPaymentMethod());
    formData.append('paymentStatus', 'paid');
    formData.append('status', 'completed');
    
    // Split payment details
    const splitJSON = getSplitPaymentJSON();
    if (splitJSON) {
      formData.append('splitPaymentDetails', splitJSON);
    }
    
    // Thêm currency nếu chọn ngoại tệ
    if (selectedPayment === 'banktransfer') {
      formData.append('currency', selectedCurrency);
    }
    
    if (invoiceNotes.trim()) {
      formData.append('notes', invoiceNotes);
    }

    // Sử dụng mutation để cập nhật đơn hàng
    completeOrderMutation.mutate({ orderId: currentReopenedOrder.orderId, formData });
  };

  // Create new order
  const createNewOrder = () => {
    // Creating new order - debug log removed
    // Tạo form-data đúng chuẩn cho backend
    const formData = new FormData();
    formData.append('orderNumber', `ORD${Date.now()}`);
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
    
    // Split payment details
    const splitJSON = getSplitPaymentJSON();
    if (splitJSON) {
      formData.append('splitPaymentDetails', splitJSON);
    }
    
    // Thêm currency nếu chọn ngoại tệ
    if (selectedPayment === 'banktransfer') {
      formData.append('currency', selectedCurrency);
    }
    
    if (invoiceNotes.trim()) {
      formData.append('notes', invoiceNotes);
    }
    // Gửi từng item dưới dạng items[0].productId, items[0].productName, ...
    cart.forEach((item, idx) => {
      // Luôn lấy đúng productId, không để undefined
      const productId = item.productId?.toString() || "";
      formData.append(`items[${idx}].productId`, productId);
      formData.append(`items[${idx}].productName`, item.name || "");
      formData.append(`items[${idx}].quantity`, item.quantity?.toString() || "1");
      formData.append(`items[${idx}].unitPrice`, item.price?.toString() || "0");
      formData.append(`items[${idx}].totalPrice`, item.totalPrice?.toString() || "0");
    });
  // FormData prepared for submission (debug log removed)
    createOrderMutation.mutate(formData);
  };

  // Handle e-invoice creation
  const handleCreateEInvoice = () => {
    if (!orderDetailData?.orderId) {
      toast({
        title: "Lỗi",
        description: "Không tìm thấy thông tin đơn hàng",
        variant: "destructive",
      });
      return;
    }

    // Pre-fill form with customer data if available
    if (selectedCustomer) {
      setEInvoiceData({
        buyerTaxCode: "",
        buyerName: selectedCustomer.name || "",
        buyerAddress: selectedCustomer.address || "",
        buyerPhone: selectedCustomer.phone || "",
        buyerEmail: selectedCustomer.email || "",
        notes: invoiceNotes || "" // Pre-fill with invoice notes
      });
    } else {
      // Pre-fill notes even without customer
      setEInvoiceData(prev => ({
        ...prev,
        notes: invoiceNotes || ""
      }));
    }

    setIsCreateOrderWithEInvoice(false);
    setShowEInvoiceForm(true);
  };

  const submitEInvoice = () => {
    if (isCreateOrderWithEInvoice) {
      // Create order with e-invoice
      handleEInvoiceSubmit();
    } else {
      // Create e-invoice for existing order
      if (!orderDetailData?.orderId) {
        toast({
          title: "Lỗi",
          description: "Không tìm thấy thông tin đơn hàng",
          variant: "destructive",
        });
        return;
      }

      createEInvoiceMutation.mutate({
        orderId: orderDetailData.orderId,
        buyerInfo: eInvoiceData
      });
    }
  };

  // Handle payment with e-invoice
  const processPaymentWithEInvoice = () => {
    if (cart.length === 0) {
      toast({
        title: "Lỗi",
        description: "Giỏ hàng trống",
        variant: "destructive",
      });
      return;
    }

    // Pre-fill invoice data with customer info if available
    if (selectedCustomer) {
      setEInvoiceData({
        buyerTaxCode: "",
        buyerName: selectedCustomer.name || "",
        buyerAddress: selectedCustomer.address || "",
        buyerPhone: selectedCustomer.phone || "",
        buyerEmail: selectedCustomer.email || "",
        notes: ""
      });
    }

    // Set flag to indicate this is for creating new order with e-invoice
    setIsCreateOrderWithEInvoice(true);
    setShowEInvoiceForm(true);
  };

  // Modified function to handle e-invoice submission with order creation
  const handleEInvoiceSubmit = async () => {
    try {
  // Creating order with e-invoice - debug logs removed
      
      // First create the order
      const formData = new FormData();
      formData.append('customerId', selectedCustomer?.id || '0');
      formData.append('cashierId', user?.staffId?.toString() || "1");
      formData.append('storeId', currentStore?.storeId?.toString() || "");
      formData.append('staffId', user?.staffId?.toString() || "1");
      formData.append('subtotal', subtotal.toString());
      formData.append('taxAmount', taxAmount.toString());
      formData.append('discountAmount', totalDiscountAmount.toString());
      formData.append('total', total.toString());
      formData.append('paymentMethod', getPrimaryPaymentMethod());
      formData.append('paymentStatus', "completed");
      formData.append('status', "completed");
      
      // Split payment details
      const splitJSON = getSplitPaymentJSON();
      if (splitJSON) {
        formData.append('splitPaymentDetails', splitJSON);
      }
      
      // Thêm currency nếu chọn ngoại tệ
      if (selectedPayment === 'banktransfer') {
        formData.append('currency', selectedCurrency);
      }
      
      cart.forEach((item, idx) => {
        const productId = item.productId?.toString() || "";
        formData.append(`items[${idx}].productId`, productId);
        formData.append(`items[${idx}].productName`, item.name || "");
        formData.append(`items[${idx}].quantity`, item.quantity?.toString() || "1");
        formData.append(`items[${idx}].unitPrice`, item.price?.toString() || "0");
        formData.append(`items[${idx}].totalPrice`, item.totalPrice?.toString() || "0");
      });

  // Order FormData prepared (debug log removed)

      // Create order and get order ID
      const orderResponse = await apiRequest('/api/orders', { 
        method: 'POST', 
        body: formData 
      });

  // Order response received (debug log removed)

      if (orderResponse && orderResponse.orderId) {
  // Creating e-invoice for order - debug log removed
        
        // Then create e-invoice for the order
        const eInvoicePayload = {
          orderId: orderResponse.orderId,
          buyerInfo: eInvoiceData
        };
        
  // E-invoice payload prepared (debug log removed)
        
        await createEInvoiceMutation.mutateAsync({
          orderId: orderResponse.orderId,
          buyerInfo: eInvoiceData
        });

        // Clear cart and show success
        setCart([]);
        setSelectedCustomer(null);
        setCheckLocalStorage(prev => prev + 1);
        
        toast({
          title: "Thành công! 🎉",
          description: "Đơn hàng và hóa đơn điện tử đã được tạo thành công",
        });

        // Show order detail
        setOrderDetailData(orderResponse);
        setShowOrderDetail(true);
        setShowEInvoiceForm(false);
      } else {
        console.error('Order response invalid:', orderResponse);
        throw new Error('Không thể tạo đơn hàng');
      }
    } catch (error: any) {
      console.error('Error in handleEInvoiceSubmit:', error);
      toast({
        title: "Lỗi",
        description: error.message || "Có lỗi xảy ra khi tạo đơn hàng và hóa đơn",
        variant: "destructive",
      });
    }
  };

  // Save order for later payment
  const saveOrderForLater = () => {
    if (cart.length === 0) {
      toast({
        title: "Giỏ hàng trống",
        description: "Vui lòng thêm sản phẩm vào giỏ hàng trước khi lưu",
        variant: "destructive",
      });
      return;
    }

    // Show confirmation for better UX
    const customerName = selectedCustomer?.name || "Khách vãng lai";
    const orderTotal = total.toLocaleString('vi-VN');
    
    // Tạo form-data cho đơn hàng chờ thanh toán
    const formData = new FormData();
    formData.append('orderNumber', `PENDING${Date.now()}`);
    formData.append('customerId', selectedCustomer?.id || '0');
    formData.append('cashierId', user?.staffId?.toString() || "1");
    formData.append('storeId', currentStore?.storeId?.toString() || "");
    formData.append('staffId', user?.staffId?.toString() || "1");
    formData.append('subtotal', subtotal.toString());
    formData.append('taxAmount', taxAmount.toString());
    formData.append('discountAmount', totalDiscountAmount.toString());
    formData.append('total', total.toString());
    formData.append('paymentMethod', getPrimaryPaymentMethod());
    formData.append('paymentStatus', "pending");
    formData.append('status', "pending");
    
    // Tạo notes với thông tin đơn hàng và ghi chú tùy chọn
    let orderNotes = `Đơn hàng chờ thanh toán cho ${customerName} - Tổng: ${orderTotal}₫`;
    if (invoiceNotes.trim()) {
      orderNotes += `\n\nGhi chú: ${invoiceNotes}`;
    }
    formData.append('notes', orderNotes);
    
    // Gửi từng item
    cart.forEach((item, idx) => {
      const productId = item.productId?.toString() || "";
      formData.append(`items[${idx}].productId`, productId);
      formData.append(`items[${idx}].productName`, item.name || "");
      formData.append(`items[${idx}].quantity`, item.quantity?.toString() || "1");
      formData.append(`items[${idx}].unitPrice`, item.price?.toString() || "0");
      formData.append(`items[${idx}].totalPrice`, item.totalPrice?.toString() || "0");
    });
    
  // FormData for pending order prepared (debug log removed)
    
    // Show loading toast
    toast({
      title: "Đang lưu đơn hàng...",
      description: `Lưu đơn hàng ${orderTotal}₫ cho ${customerName}`,
      duration: 2000,
    });
    
    saveOrderForLaterMutation.mutate(formData);
  };

  return (
    <AppLayout title="Bán hàng">
      <div className="flex flex-col lg:grid lg:grid-cols-[70%_30%] lg:gap-4" data-testid="sales-page">
        {/* Products Section */}
        <div className="order-1 lg:order-1 min-h-[60vh] lg:min-h-[calc(100vh-120px)]">
          <Card className="h-full">
            <CardContent className="p-4 lg:p-6 flex flex-col h-full overflow-hidden">
              <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between mb-4 lg:mb-6 gap-4 flex-shrink-0">
                <div className="flex items-center gap-4">
                  <h2 className="text-lg lg:text-xl font-semibold">Sản phẩm</h2>
                  <Button
                    variant="outline"
                    onClick={() => navigate('/orders')}
                    className="text-xs lg:text-sm"
                    size="sm"
                  >
                    Xem lịch sử hóa đơn
                  </Button>
                </div>
                
                <div className="flex flex-col gap-2 w-full lg:w-auto">
                  <div className="relative w-full">
                    <Input
                      placeholder="Tìm kiếm sản phẩm (có thể gõ không dấu)..."
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="pl-10 text-sm"
                      data-testid="input-product-search"
                    />
                    <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
                  </div>
                  
                  {/* Barcode Scanner Input */}
                  <div className="relative w-full">
                    <Input
                      ref={setBarcodeInputRef}
                      placeholder="Quét mã vạch hoặc nhập mã vạch..."
                      value={barcodeInput}
                      onChange={handleBarcodeChange}
                      onKeyDown={handleBarcodeKeyDown}
                      className="pl-10 pr-20 bg-yellow-50 border-yellow-200 focus:border-yellow-400 text-sm"
                      data-testid="input-barcode-scanner"
                      autoComplete="off"
                      title="Quét mã vạch để tự động thêm sản phẩm vào giỏ hàng"
                    />
                    <Tag className="absolute left-3 top-3 h-4 w-4 text-yellow-600" />
                    
                    {/* Camera Scanner Button */}
                    <Button
                      size="sm"
                      variant="ghost"
                      className="absolute right-12 top-1 h-8 p-1 text-yellow-600 hover:text-yellow-700 hover:bg-yellow-100"
                      onClick={() => setShowCameraScanner(true)}
                      title="Mở camera để quét mã vạch"
                    >
                      <Camera className="w-4 h-4" />
                    </Button>
                    
                    {barcodeInput && (
                      <Button
                        size="sm"
                        className="absolute right-1 top-1 h-8 text-xs px-2"
                        onClick={() => handleBarcodeSubmit(barcodeInput)}
                      >
                        Quét
                      </Button>
                    )}
                  </div>
                  
                  <div className="text-xs text-yellow-600 text-center lg:hidden">
                    📱 Quét mã vạch tự động thêm vào giỏ hàng
                  </div>
                  
                  {/* Refresh Products Button */}
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      // Manual refresh products triggered (debug log removed)
                      queryClient.invalidateQueries({ queryKey: ['products-sales'] });
                      toast({
                        title: "🔄 Đang cập nhật",
                        description: "Đang tải lại danh sách sản phẩm...",
                        duration: 2000,
                      });
                    }}
                    className="self-start h-8 lg:h-10 text-xs"
                    title="Tải lại danh sách sản phẩm nếu vừa thêm sản phẩm mới"
                  >
                    🔄 Refresh
                  </Button>
                </div>
              </div>

              <Tabs value={activeProductTab} onValueChange={setActiveProductTab} className="w-full flex flex-col flex-1 min-h-0">
                <TabsList className="grid w-full grid-cols-2 flex-shrink-0 mb-3">
                  <TabsTrigger value="all" className="text-sm">
                    Tất cả sản phẩm 
                    {products.length > 0 && (
                      <span className="ml-1 text-xs bg-blue-100 text-blue-600 px-1 rounded">
                        {products.length}
                      </span>
                    )}
                  </TabsTrigger>
                  <TabsTrigger value="featured" className="text-sm">
                    Sản phẩm hay bán
                    {featuredProducts.length > 0 && (
                      <span className="ml-1 text-xs bg-yellow-100 text-yellow-600 px-1 rounded">
                        {featuredProducts.length}
                      </span>
                    )}
                  </TabsTrigger>
                </TabsList>

                <TabsContent value="all" className="flex-1 overflow-hidden">
                  <div className="h-full flex flex-col">
                    {/* Loading state */}
                    {productsLoading && (
                      <div className="flex justify-center items-center py-8">
                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                        <span className="ml-2 text-sm text-gray-600">Đang tải sản phẩm...</span>
                      </div>
                    )}

                    {/* Search results info */}
                    {!productsLoading && searchTerm && (
                      <div className="px-3 py-2 bg-blue-50 border-b text-sm text-blue-700">
                        {filteredProducts.length > 0 ? (
                          <>Tìm thấy <strong>{filteredProducts.length}</strong> sản phẩm cho "{searchTerm}"</>
                        ) : (
                          <>Không tìm thấy sản phẩm nào cho "{searchTerm}"</>
                        )}
                      </div>
                    )}

                    <div className="flex-1 overflow-y-auto">
                      {!productsLoading && paginatedAllProducts.length === 0 ? (
                        <div className="flex flex-col items-center justify-center py-12 text-gray-500">
                          <div className="text-4xl mb-4">📦</div>
                          <div className="text-lg font-medium mb-2">
                            {searchTerm ? 'Không tìm thấy sản phẩm' : 'Chưa có sản phẩm nào'}
                          </div>
                          <div className="text-sm text-center">
                            {searchTerm ? (
                              <>Thử tìm kiếm với từ khóa khác hoặc <button 
                                onClick={() => setSearchTerm('')} 
                                className="text-blue-600 underline"
                              >
                                xóa bộ lọc
                              </button></>
                            ) : (
                              <>Hãy thêm sản phẩm mới trong trang <button 
                                onClick={() => navigate('/products')} 
                                className="text-blue-600 underline"
                              >
                                Quản lý sản phẩm
                              </button></>
                            )}
                          </div>
                        </div>
                      ) : (
                        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4 gap-3 p-3">
                          {paginatedAllProducts.map((product) => {
                          const stockQty = product.stockQuantity || 0;
                          const minStock = product.minStockLevel || 0;
                          
                          let stockStatus = { label: '', color: '' };
                          if (stockQty === 0) {
                            stockStatus = { label: 'Hết hàng', color: 'bg-red-500' };
                          } else if (stockQty <= minStock) {
                            stockStatus = { label: 'Sắp hết', color: 'bg-orange-500' };
                          } else {
                            stockStatus = { label: 'Còn hàng', color: 'bg-green-500' };
                          }
                          const key = product.productId;
                          const isOutOfStock = stockQty <= 0;
                          
                          return (
                            <div
                              key={key}
                              className={cn(
                                "cursor-pointer hover:shadow-md transition-shadow",
                                isOutOfStock && "opacity-60 cursor-not-allowed"
                              )}
                              onClick={() => {
                                if (!isOutOfStock) {
                                  addToCart(product);
                                }
                              }}
                              data-testid={`product-card-${key}`}
                            >
                              <Card className="h-full">
                                <CardContent className="p-3 lg:p-4 h-full flex flex-col">
                                  <div className="relative mb-3">
                                    <div className="w-full h-24 sm:h-28 lg:h-32 bg-gray-100 flex items-center justify-center overflow-hidden rounded-lg">
                                      <img
                                        src={
                                          product.imageUrl && product.imageUrl !== ""
                                            ? (product.imageUrl.startsWith("http") ? product.imageUrl : `${API_BASE}${product.imageUrl}`)
                                            : "https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=200&h=150&fit=crop"
                                        }
                                        alt={product.name || 'Sản phẩm'}
                                        className="max-w-full max-h-full object-contain"
                                        style={{ width: '100%', height: '100%' }}
                                      />
                                      <Badge
                                        className={`absolute top-1 right-1 text-white text-xs ${stockStatus.color}`}
                                        data-testid={`stock-status-${key}`}
                                      >
                                        {stockStatus.label}
                                      </Badge>
                                      {stockQty <= minStock && (
                                        <AlertTriangle className="absolute top-1 left-1 w-4 h-4 text-orange-500" />
                                      )}
                                    </div>
                                  </div>
                                  <div className="flex-1 flex flex-col justify-between">
                                    <div className="mb-2">
                                      <h3 className="font-medium text-xs sm:text-sm mb-1 line-clamp-2 leading-tight">{product.name || 'Tên sản phẩm'}</h3>
                                      <p className="text-sm sm:text-base lg:text-lg font-bold text-primary">{Number(product.price || 0).toLocaleString('vi-VN')}₫</p>
                                      <div className="text-xs text-gray-500 space-y-0.5">
                                        <p>Tồn: {product.stockQuantity || 0} {product.unit || ''}</p>
                                        <p>Tối thiểu: {product.minStockLevel || 0}</p>
                                      </div>
                                    </div>
                                    <Button
                                      className="w-full mt-1 h-8 text-xs"
                                      size="sm"
                                      disabled={isOutOfStock}
                                      onClick={(e) => {
                                        e.stopPropagation();
                                        if (!isOutOfStock) {
                                          addToCart(product);
                                        }
                                      }}
                                    >
                                      <Plus className="w-3 h-3 mr-1" />
                                      {isOutOfStock ? "Hết hàng" : "Thêm vào hóa đơn"}
                                    </Button>
                                  </div>
                                </CardContent>
                              </Card>
                            </div>
                          );
                        })}
                        </div>
                      )}
                    </div>
                    
                    {/* Pagination for all products */}
                    <div className="flex-shrink-0 p-3">
                      <PaginationComponent
                        currentPage={allProductsPage}
                        totalPages={totalAllProductsPages}
                        onPageChange={setAllProductsPage}
                        totalItems={filteredProducts.length}
                        itemsPerPage={PRODUCTS_PER_PAGE}
                      />
                    </div>
                  </div>
                </TabsContent>

                <TabsContent value="featured" className="flex-1 overflow-hidden">
                  <div className="h-full flex flex-col">
                    {featuredLoading && (
                      <div className="flex justify-center py-8">
                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
                      </div>
                    )}

                    <div className="flex-1 overflow-y-auto">
                      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4 gap-3 p-3">
                        {paginatedFeaturedProducts.map((product: Product) => {
                          const stockQty = product.stockQuantity || 0;
                          const minStock = product.minStockLevel || 0;
                          
                          let stockStatus = { label: '', color: '' };
                          if (stockQty === 0) {
                            stockStatus = { label: 'Hết hàng', color: 'bg-red-500' };
                          } else if (stockQty <= minStock) {
                            stockStatus = { label: 'Sắp hết', color: 'bg-orange-500' };
                          } else {
                            stockStatus = { label: 'Còn hàng', color: 'bg-green-500' };
                          }
                          const key = product.productId;
                          const isOutOfStock = stockQty <= 0;
                          
                          return (
                            <div
                              key={key}
                              className={cn(
                                "cursor-pointer hover:shadow-md transition-shadow",
                                isOutOfStock && "opacity-60 cursor-not-allowed"
                              )}
                              onClick={() => {
                                if (!isOutOfStock) {
                                  addToCart(product);
                                }
                              }}
                              data-testid={`featured-product-card-${key}`}
                            >
                              <Card className="border-yellow-200 bg-yellow-50 h-full">
                                <CardContent className="p-3 lg:p-4 h-full flex flex-col">
                                  <div className="relative mb-3">
                                    <div className="w-full h-24 sm:h-28 lg:h-32 bg-gray-100 flex items-center justify-center overflow-hidden rounded-lg">
                                      <img
                                        src={
                                          product.imageUrl && product.imageUrl !== ""
                                            ? (product.imageUrl.startsWith("http") ? product.imageUrl : `${API_BASE}${product.imageUrl}`)
                                            : "https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=200&h=150&fit=crop"
                                        }
                                        alt={product.name || 'Sản phẩm'}
                                        className="max-w-full max-h-full object-contain"
                                        style={{ width: '100%', height: '100%' }}
                                      />
                                      <Badge className="absolute top-1 left-1 bg-yellow-500 text-yellow-900 text-xs">
                                        ⭐ Hay bán
                                      </Badge>
                                      <Badge
                                        className={`absolute top-1 right-1 text-white text-xs ${stockStatus.color}`}
                                        data-testid={`stock-status-${key}`}
                                      >
                                        {stockStatus.label}
                                      </Badge>
                                      {stockQty <= minStock && (
                                        <AlertTriangle className="absolute top-8 left-1 w-4 h-4 text-orange-500" />
                                      )}
                                    </div>
                                  </div>
                                  <div className="flex-1 flex flex-col justify-between">
                                    <div className="mb-2">
                                      <h3 className="font-medium text-xs sm:text-sm mb-1 line-clamp-2 leading-tight">{product.name || 'Tên sản phẩm'}</h3>
                                      <p className="text-sm sm:text-base lg:text-lg font-bold text-primary">{Number(product.price || 0).toLocaleString('vi-VN')}₫</p>
                                      <div className="text-xs text-gray-500 space-y-0.5">
                                        <p>Tồn: {product.stockQuantity || 0} {product.unit || ''}</p>
                                        <p>Tối thiểu: {product.minStockLevel || 0}</p>
                                      </div>
                                    </div>
                                    <Button
                                      className="w-full mt-1 h-8 text-xs"
                                      size="sm"
                                      disabled={isOutOfStock}
                                      onClick={(e) => {
                                        e.stopPropagation();
                                        if (!isOutOfStock) {
                                          addToCart(product);
                                        }
                                      }}
                                    >
                                      <Plus className="w-3 h-3 mr-1" />
                                      {isOutOfStock ? "Hết hàng" : "Thêm vào hóa đơn"}
                                    </Button>
                                  </div>
                                </CardContent>
                              </Card>
                            </div>
                          );
                        })}
                      </div>
                    </div>

                    {/* Show pagination only if there are featured products */}
                    <div className="flex-shrink-0 p-3">
                      {(featuredProducts || []).length > 0 && (
                        <PaginationComponent
                          currentPage={featuredProductsPage}
                          totalPages={totalFeaturedProductsPages}
                          onPageChange={setFeaturedProductsPage}
                          totalItems={(featuredProducts || []).length}
                          itemsPerPage={PRODUCTS_PER_PAGE}
                        />
                      )}
                    </div>

                    {(featuredProducts || []).length === 0 && !featuredLoading && (
                      <div className="flex-1 flex items-center justify-center">
                        <div className="text-center py-12 text-gray-500">
                          <div className="text-lg mb-2">📦</div>
                          <div className="text-sm">
                            Chưa có sản phẩm hay bán nào được chọn.
                          </div>
                          <div className="text-xs mt-1">
                            Hãy vào trang Sản phẩm để đánh dấu các sản phẩm hay bán.
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                </TabsContent>
              </Tabs>
            </CardContent>
          </Card>
        </div>

        {/* Cart Section */}
        <div className="order-2 lg:order-2 min-h-[40vh] lg:min-h-[calc(100vh-120px)]">
          <Card className="h-full lg:sticky lg:top-0 lg:max-h-[calc(100vh-120px)] lg:max-w-[480px]">
            <CardContent className="p-4 lg:p-6 flex flex-col h-full overflow-hidden">
              <div className="flex items-center justify-between mb-4 flex-shrink-0">
                <h2 className="text-lg lg:text-xl font-semibold flex items-center">
                  <ShoppingCart className="w-4 h-4 lg:w-5 lg:h-5 mr-2" />
                  Giỏ hàng ({cart.length})
                </h2>
                {cart.length > 0 && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={clearCart}
                    data-testid="button-clear-cart"
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                )}
              </div>

              {/* Scrollable Content Container */}
              <div className="flex-1 overflow-y-auto overflow-x-hidden min-h-0 space-y-4">

              {/* Thông báo đơn hàng được mở lại */}
              {currentReopenedOrder && (
                <div className="mb-4 p-3 bg-orange-50 border border-orange-200 rounded-lg">
                  <div className="flex items-center gap-2 text-orange-800">
                    <AlertTriangle className="h-4 w-4" />
                    <span className="text-sm font-medium">
                      Đang thanh toán đơn hàng #{currentReopenedOrder.orderId}
                    </span>
                  </div>
                  <p className="text-xs text-orange-600 mt-1">
                    Bấm "Thanh toán" để hoàn tất đơn hàng này
                  </p>
                </div>
              )}

              {/* Reopened Order Notification */}
              {currentReopenedOrder && (
                <div className="mb-4 p-3 bg-orange-50 border border-orange-200 rounded-lg">
                  <div className="flex items-center gap-2 text-orange-700">
                    <AlertTriangle className="w-4 h-4" />
                    <span className="text-sm font-medium">
                      Đang thanh toán đơn hàng #{currentReopenedOrder.orderId}
                    </span>
                  </div>
                  <p className="text-xs text-orange-600 mt-1">
                    Bấm "Thanh toán" để hoàn thành đơn hàng này
                  </p>
                </div>
              )}

              {/* Customer Selection with Search */}
              <div className="mb-4 relative customer-search-container">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Khách hàng
                </label>
                <div className="relative">
                  <Input
                    placeholder="Tìm kiếm khách hàng hoặc nhập tên mới..."
                    value={customerSearchTerm}
                    onChange={(e) => {
                      setCustomerSearchTerm(e.target.value);
                      setShowCustomerDropdown(true);
                      // If search is cleared, also clear selected customer
                      if (!e.target.value) {
                        setSelectedCustomer(null);
                      }
                    }}
                    onFocus={() => setShowCustomerDropdown(true)}
                    className="w-full"
                  />
                  <Search className="absolute right-3 top-3 h-4 w-4 text-gray-400" />
                  
                  {/* Customer Dropdown */}
                  {showCustomerDropdown && (
                    <div className="absolute z-50 w-full mt-1 bg-white border border-gray-200 rounded-md shadow-lg max-h-60 overflow-y-auto">
                      {/* Walk-in customer option */}
                      <div
                        className="px-4 py-2 hover:bg-gray-100 cursor-pointer border-b"
                        onClick={() => {
                          setSelectedCustomer(null);
                          setCustomerSearchTerm("Khách vãng lai");
                          setShowCustomerDropdown(false);
                        }}
                      >
                        <div className="font-medium text-gray-900">Khách vãng lai</div>
                        <div className="text-sm text-gray-500">Không cần thông tin khách hàng</div>
                      </div>
                      
                      {/* Create new customer option */}
                      {customerSearchTerm && customerSearchTerm !== "Khách vãng lai" && 
                       !filteredCustomers.some(c => c.name?.toLowerCase() === customerSearchTerm.toLowerCase()) && (
                        <div
                          className="px-4 py-2 hover:bg-blue-50 cursor-pointer border-b bg-blue-25"
                          onClick={() => {
                            setQuickCustomerData({
                              ...quickCustomerData,
                              hoTen: customerSearchTerm
                            });
                            setShowQuickCustomerForm(true);
                            setShowCustomerDropdown(false);
                          }}
                        >
                          <div className="font-medium text-blue-600">+ Tạo khách hàng mới: "{customerSearchTerm}"</div>
                          <div className="text-sm text-blue-500">Tạo khách hàng mới với tên này</div>
                        </div>
                      )}
                      
                      {/* Quick create button */}
                      <div
                        className="px-4 py-2 hover:bg-green-50 cursor-pointer border-b bg-green-25"
                        onClick={() => {
                          setShowQuickCustomerForm(true);
                          setShowCustomerDropdown(false);
                        }}
                      >
                        <div className="font-medium text-green-600">➕ Tạo khách hàng mới</div>
                        <div className="text-sm text-green-500">Điền thông tin đầy đủ để tích điểm</div>
                      </div>
                      
                      {/* Existing customers */}
                      {filteredCustomers.map((customer) => (
                        <div
                          key={customer.id}
                          className="px-4 py-2 hover:bg-gray-100 cursor-pointer"
                          onClick={() => {
                            setSelectedCustomer(customer);
                            setCustomerSearchTerm(customer.name || '');
                            setShowCustomerDropdown(false);
                          }}
                        >
                          <div className="font-medium text-gray-900">{customer.name}</div>
                          <div className="text-sm text-gray-500">
                            {customer.phone} {customer.address && `• ${customer.address}`}
                          </div>
                        </div>
                      ))}
                      
                      {/* No results */}
                      {filteredCustomers.length === 0 && customerSearchTerm && customerSearchTerm !== "Khách vãng lai" && (
                        <div className="px-4 py-2 text-gray-500 text-sm">
                          Không tìm thấy khách hàng nào
                        </div>
                      )}
                    </div>
                  )}
                </div>
                
                {/* Selected customer info */}
                {selectedCustomer && selectedCustomer.id !== 'walk-in' && (
                  <div className="mt-2 p-2 bg-blue-50 rounded-md text-sm">
                    <div className="font-medium text-blue-900">{selectedCustomer.name}</div>
                    {selectedCustomer.phone && (
                      <div className="text-blue-700">📞 {selectedCustomer.phone}</div>
                    )}
                    {selectedCustomer.address && (
                      <div className="text-blue-700">📍 {selectedCustomer.address}</div>
                    )}
                  </div>
                )}
              </div>

              {/* Quick Customer Creation Form */}
              {showQuickCustomerForm && (
                <div className="mb-4 p-4 bg-green-50 border border-green-200 rounded-lg">
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="font-medium text-green-800">Tạo khách hàng mới</h3>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        setShowQuickCustomerForm(false);
                        setQuickCustomerData({
                          hoTen: "",
                          soDienThoai: "",
                          email: "",
                          diaChi: "",
                          hangKhachHang: "Thuong"
                        });
                      }}
                      className="h-6 w-6 p-0 text-green-600"
                    >
                      ✕
                    </Button>
                  </div>
                  
                  <div className="space-y-3">
                    <div>
                      <label className="block text-sm font-medium text-green-700 mb-1">
                        Tên khách hàng *
                      </label>
                      <Input
                        value={quickCustomerData.hoTen}
                        onChange={(e) => setQuickCustomerData({...quickCustomerData, hoTen: e.target.value})}
                        placeholder="Nhập tên khách hàng"
                        className="text-sm"
                        required
                      />
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-green-700 mb-1">
                        Số điện thoại *
                      </label>
                      <Input
                        value={quickCustomerData.soDienThoai}
                        onChange={(e) => setQuickCustomerData({...quickCustomerData, soDienThoai: e.target.value})}
                        placeholder="Nhập số điện thoại"
                        className="text-sm"
                        required
                      />
                    </div>
                    
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                      <div>
                        <label className="block text-sm font-medium text-green-700 mb-1">
                          Email
                        </label>
                        <Input
                          type="email"
                          value={quickCustomerData.email}
                          onChange={(e) => setQuickCustomerData({...quickCustomerData, email: e.target.value})}
                          placeholder="Nhập email"
                          className="text-sm"
                        />
                      </div>
                      
                      <div>
                        <label className="block text-sm font-medium text-green-700 mb-1">
                          Hạng khách hàng
                        </label>
                        <Select
                          value={quickCustomerData.hangKhachHang}
                          onValueChange={(value) => setQuickCustomerData({...quickCustomerData, hangKhachHang: value})}
                        >
                          <SelectTrigger className="text-sm">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Thuong">Thường</SelectItem>
                            <SelectItem value="Premium">Premium</SelectItem>
                            <SelectItem value="VIP">VIP</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-green-700 mb-1">
                        Địa chỉ
                      </label>
                      <Input
                        value={quickCustomerData.diaChi}
                        onChange={(e) => setQuickCustomerData({...quickCustomerData, diaChi: e.target.value})}
                        placeholder="Nhập địa chỉ"
                        className="text-sm"
                      />
                    </div>
                    
                    <div className="flex gap-2 pt-2">
                      <Button
                        onClick={() => {
                          if (!quickCustomerData.hoTen.trim() || !quickCustomerData.soDienThoai.trim()) {
                            toast({
                              title: "Thiếu thông tin",
                              description: "Vui lòng nhập tên và số điện thoại khách hàng",
                              variant: "destructive",
                            });
                            return;
                          }
                          createCustomerMutation.mutate(quickCustomerData);
                        }}
                        disabled={createCustomerMutation.isPending}
                        className="flex-1 bg-green-600 hover:bg-green-700 text-white"
                        size="sm"
                      >
                        {createCustomerMutation.isPending ? "Đang tạo..." : "Tạo & Chọn khách hàng"}
                      </Button>
                      
                      <Button
                        variant="outline"
                        onClick={() => {
                          setShowQuickCustomerForm(false);
                          setQuickCustomerData({
                            hoTen: "",
                            soDienThoai: "",
                            email: "",
                            diaChi: "",
                            hangKhachHang: "Thuong"
                          });
                        }}
                        className="px-4"
                        size="sm"
                      >
                        Hủy
                      </Button>
                    </div>
                  </div>
                </div>
              )}

              {/* Cart Items */}
              <div className="space-y-3" style={{visibility: 'visible', display: 'block'}}>
                {cart.length === 0 ? (
                  <div className="text-center py-8 text-gray-500">
                    <ShoppingCart className="w-12 h-12 mx-auto mb-2 opacity-50" />
                    <p>Giỏ hàng trống</p>
                  </div>
                ) : (
                  <>
                    {/* Header cho giỏ hàng */}
                    <div className="flex items-center p-2 bg-gray-50 rounded-lg border text-xs font-medium text-gray-700">
                      <div className="w-8 text-center">STT</div>
                      <div className="flex-1 px-2 min-w-0">Tên hàng</div>
                      <div className="w-10 text-center text-xs">ĐVT</div>
                      <div className="w-20 text-center">Số lượng</div>
                      <div className="w-16 text-center text-xs">Thành tiền</div>
                      <div className="w-8"></div> {/* Space cho nút xóa */}
                    </div>
                    
                    {/* Cart render debug removed */}
                    {cart.map((item, index) => (
                      <div 
                        key={item.cartItemId} 
                        className="flex items-start p-3 bg-white rounded-lg border-2 border-blue-200 mb-2" 
                        data-testid={`cart-item-${item.productId}`}
                        style={{visibility: 'visible', display: 'flex', minHeight: '80px', opacity: '1'}}
                      >
                        {/* Cột STT */}
                        <div className="w-8 text-center flex items-start pt-1">
                          <span className="text-sm font-medium text-gray-600">{index + 1}</span>
                        </div>
                        
                        {/* Tên hàng và giá */}
                        <div className="flex-1 min-w-0 px-2">
                          <p className="font-medium text-sm break-words leading-tight" style={{ 
                            wordBreak: 'break-word',
                            overflowWrap: 'break-word',
                            hyphens: 'auto'
                          }}>
                            {item.name || 'Tên sản phẩm không xác định'}
                          </p>
                          <p className="text-primary font-semibold text-xs mt-1">{Number(item.price || 0).toLocaleString('vi-VN')}₫</p>
                        </div>
                        
                        {/* Đơn vị tính */}
                        <div className="w-10 text-center flex items-start pt-1">
                          {item.unit ? (
                            <span className="text-xs text-gray-600 bg-gray-100 px-1 py-0.5 rounded text-center block">
                              {item.unit}
                            </span>
                          ) : (
                            <span className="text-xs text-gray-400">-</span>
                          )}
                        </div>
                        
                        {/* Số lượng */}
                        <div className="w-20 flex items-start justify-center space-x-1 pt-1">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => updateQuantity(item.cartItemId, item.quantity - 1)}
                            data-testid={`button-decrease-${item.productId}`}
                            className="h-6 w-6 p-0"
                          >
                            <Minus className="w-3 h-3" />
                          </Button>
                          <span className="w-6 text-center text-sm font-medium" data-testid={`quantity-${item.productId}`}>{item.quantity}</span>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => updateQuantity(item.cartItemId, item.quantity + 1)}
                            data-testid={`button-increase-${item.productId}`}
                            className="h-6 w-6 p-0"
                          >
                            <Plus className="w-3 h-3" />
                          </Button>
                        </div>
                        
                        {/* Thành tiền */}
                        <div className="w-16 text-center flex items-start pt-1">
                          <span className="text-xs font-semibold text-primary w-full" data-testid={`item-total-${item.productId}`}>
                            {Number(item.totalPrice).toLocaleString('vi-VN')}₫
                          </span>
                        </div>
                        
                        {/* Nút xóa */}
                        <div className="w-8 flex justify-center items-start pt-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => removeFromCart(item.cartItemId)}
                            data-testid={`button-remove-${item.productId}`}
                            className="h-6 w-6 p-0 text-red-500 hover:text-red-700"
                          >
                            <Trash2 className="w-3 h-3" />
                          </Button>
                        </div>
                      </div>
                    ))}
                  </>
                )}
              </div>

              {/* Order Summary */}
              {cart.length > 0 && (
                <div className="space-y-2 mb-4">
                  <Separator />
                  <div className="flex justify-between">
                    <span>Tạm tính:</span>
                    <span data-testid="subtotal">{subtotal.toLocaleString('vi-VN')}₫</span>
                  </div>

                  {/* Discount Selector */}
                  <DiscountSelector 
                    cart={cart.map(item => ({
                      productId: item.productId || 0,
                      quantity: item.quantity,
                      totalPrice: item.totalPrice,
                      categoryId: item.categoryId
                    }))}
                    subtotal={subtotal}
                    onDiscountSelect={(discount, amount) => {
                      setSelectedDiscount(discount);
                      setSelectedDiscountAmount(amount);
                    }}
                    selectedDiscount={selectedDiscount}
                  />

                  {/* Manual Discount Input */}
                  <div className="space-y-2">
                    <div className="flex items-center gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setShowManualDiscount(!showManualDiscount)}
                        className="text-xs"
                      >
                        {showManualDiscount ? 'Ẩn' : 'Nhập'} giảm giá
                      </Button>
                    </div>
                    
                    {showManualDiscount && (
                      <div className="space-y-2 p-3 border rounded-lg bg-gray-50">
                          {/* Discount Type Selection */}
                          <div className="grid grid-cols-3 gap-1">
                            <Button
                              variant={manualDiscountType === 'percentage' ? 'default' : 'outline'}
                              size="sm"
                              onClick={() => setManualDiscountType('percentage')}
                              className="text-xs"
                            >
                              %
                            </Button>
                            <Button
                              variant={manualDiscountType === 'fixed' ? 'default' : 'outline'}
                              size="sm"
                              onClick={() => setManualDiscountType('fixed')}
                              className="text-xs"
                            >
                              Tiền
                            </Button>
                            <Button
                              variant={manualDiscountType === 'none' ? 'default' : 'outline'}
                              size="sm"
                              onClick={() => clearManualDiscount()}
                              className="text-xs"
                            >
                              Không
                            </Button>
                          </div>
                          
                          {manualDiscountType !== 'none' && (
                            <>
                              {/* Value Input */}
                              <div className="flex gap-2">
                                <Input
                                  type="number"
                                  placeholder={manualDiscountType === 'percentage' ? 'Nhập % giảm tổng bill' : 'Nhập số tiền giảm tổng bill'}
                                  value={manualDiscountValue}
                                  onChange={(e) => handleManualDiscountChange(e.target.value)}
                                  className="text-sm"
                                  min="0"
                                  max={manualDiscountType === 'percentage' ? '100' : undefined}
                                />
                                <Button
                                  size="sm"
                                  onClick={calculateManualDiscount}
                                  disabled={!manualDiscountValue || Number(manualDiscountValue) <= 0}
                                  className="text-xs"
                                >
                                  Áp dụng
                                </Button>
                              </div>
                              
                              {/* Show calculated discount amount */}
                              {manualDiscountAmount > 0 && (
                                <div className="flex justify-between text-green-600 text-sm print:text-black">
                                  <span>Giảm thủ công:</span>
                                  <span>-{manualDiscountAmount.toLocaleString('vi-VN')}₫</span>
                                </div>
                              )}
                              
                              {/* Clear manual discount */}
                              {manualDiscountAmount > 0 && (
                                <Button
                                  variant="outline"
                                  size="sm"
                                  onClick={clearManualDiscount}
                                  className="text-xs w-full"
                                >
                                  Xóa giảm giá thủ công
                                </Button>
                              )}
                            </>
                          )}
                        </div>
                      )}
                    </div>
                  
                  {/* Chỉ hiển thị thuế khi được bật */}
                  {isVATEnabled && (
                    <div className="flex justify-between">
                      <span>{taxLabel} ({taxRate}%):</span>
                      <span data-testid="tax">{taxAmount.toLocaleString('vi-VN')}₫</span>
                    </div>
                  )}
                  
                  {/* Display discount breakdown */}
                  {selectedDiscountAmount > 0 && (
                    <div className="flex justify-between text-green-600 print:text-black">
                      <span>Giảm giá ({selectedDiscount?.name}):</span>
                      <span>-{selectedDiscountAmount.toLocaleString('vi-VN')}₫</span>
                    </div>
                  )}
                  
                  {manualDiscountAmount > 0 && (
                    <div className="flex justify-between text-green-600 print:text-black">
                      <span>Giảm thủ công:</span>
                      <span>-{manualDiscountAmount.toLocaleString('vi-VN')}₫</span>
                    </div>
                  )}
                  
                  {cartDiscountAmount > 0 && selectedDiscountAmount === 0 && manualDiscountAmount === 0 && (
                    <div className="flex justify-between text-green-600 print:text-black">
                      <span>Giảm giá khách hàng:</span>
                      <span>-{cartDiscountAmount.toLocaleString('vi-VN')}₫</span>
                    </div>
                  )}
                  
                  {totalDiscountAmount > 0 && (
                    <div className="flex justify-between text-green-600 print:text-black font-semibold">
                      <span>Tổng giảm giá:</span>
                      <span data-testid="total-discount">-{totalDiscountAmount.toLocaleString('vi-VN')}₫</span>
                    </div>
                  )}
                  
                  <Separator />
                  <div className="flex justify-between text-lg font-bold">
                    <span>Tổng cộng:</span>
                    <span data-testid="total">{total.toLocaleString('vi-VN')}₫</span>
                  </div>
                </div>
              )}

              {/* Invoice Notes */}
              {cart.length > 0 && (
                <div className="space-y-3">
                  <p className="font-medium">Ghi chú hóa đơn:</p>
                  <textarea
                    placeholder="Nhập ghi chú cho hóa đơn (tùy chọn)..."
                    value={invoiceNotes}
                    onChange={(e) => setInvoiceNotes(e.target.value)}
                    className="w-full p-3 border rounded-lg resize-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    rows={3}
                    maxLength={500}
                    data-testid="invoice-notes"
                  />
                  <div className="text-xs text-gray-500 text-right">
                    {invoiceNotes.length}/500 ký tự
                  </div>
                </div>
              )}

              {/* Payment Methods */}
              {cart.length > 0 && (
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <p className="font-medium">Phương thức thanh toán:</p>
                    <Button
                      variant={isSplitPayment ? "default" : "outline"}
                      size="sm"
                      onClick={toggleSplitPayment}
                      className={cn("text-xs h-7", isSplitPayment && "bg-orange-500 hover:bg-orange-600")}
                    >
                      {isSplitPayment ? "✂️ Đang chia nhỏ" : "✂️ Chia thanh toán"}
                    </Button>
                  </div>

                  {/* Normal single payment mode */}
                  {!isSplitPayment && (
                    <>
                      <div className="grid grid-cols-2 lg:grid-cols-2 gap-2">
                        {availablePaymentMethods.map((method) => {
                          const Icon = method.icon;
                          return (
                            <Button
                              key={method.id}
                              variant={selectedPayment === method.id ? "default" : "outline"}
                              size="sm"
                              onClick={() => setSelectedPayment(method.id)}
                              className="h-12 text-xs lg:text-sm"
                              data-testid={`payment-${method.id}`}
                            >
                              <Icon className="w-4 h-4 mr-1 lg:mr-2" />
                              <span className="hidden sm:inline lg:inline">{method.name}</span>
                              <span className="sm:hidden lg:hidden">{method.name.split(' ')[0]}</span>
                            </Button>
                          );
                        })}
                      </div>
                      
                      {/* Currency Selection for Foreign Currency */}
                      {selectedPayment === 'banktransfer' && (
                        <div className="space-y-2">
                          <p className="font-medium text-sm">Loại ngoại tệ:</p>
                          <Select value={selectedCurrency} onValueChange={setSelectedCurrency}>
                            <SelectTrigger className="w-full">
                              <SelectValue placeholder="Chọn loại ngoại tệ" />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="USD">USD - Đô la Mỹ</SelectItem>
                              <SelectItem value="EUR">EUR - Euro</SelectItem>
                            </SelectContent>
                          </Select>
                        </div>
                      )}
                    </>
                  )}

                  {/* Split payment mode */}
                  {isSplitPayment && (
                    <div className="space-y-3">
                      {/* Added split payment methods */}
                      <div className="space-y-2">
                        {splitPayments.map((sp, idx) => {
                          const method = availablePaymentMethods.find(m => m.id === sp.method);
                          const Icon = method?.icon || Banknote;
                          return (
                            <div key={sp.method} className="flex items-center gap-2 p-2 bg-gray-50 rounded-lg border">
                              <div className="flex items-center gap-1 min-w-[100px]">
                                <Icon className="w-4 h-4 text-blue-600 shrink-0" />
                                <span className="text-xs font-medium truncate">{sp.methodName}</span>
                              </div>
                              <div className="flex-1 relative">
                                <Input
                                  type="number"
                                  value={splitInputAmounts[sp.method] || ''}
                                  onChange={(e) => updateSplitAmount(sp.method, e.target.value)}
                                  placeholder="Nhập số tiền"
                                  className="h-9 text-sm pr-8"
                                  min={0}
                                  max={total}
                                />
                                <span className="absolute right-2 top-1/2 -translate-y-1/2 text-xs text-gray-400">₫</span>
                              </div>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => autoFillSplitRemaining(sp.method)}
                                className="h-9 px-2 text-xs text-blue-600 hover:text-blue-800 hover:bg-blue-50 shrink-0"
                                title="Tự động điền số tiền còn lại"
                              >
                                Còn lại
                              </Button>
                              {splitPayments.length > 1 && (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => removeSplitPaymentMethod(sp.method)}
                                  className="h-9 w-9 p-0 text-red-500 hover:text-red-700 hover:bg-red-50 shrink-0"
                                >
                                  <Trash2 className="w-4 h-4" />
                                </Button>
                              )}
                            </div>
                          );
                        })}
                      </div>

                      {/* Add more payment methods */}
                      <div>
                        <p className="text-xs text-gray-500 mb-1">Thêm phương thức:</p>
                        <div className="flex flex-wrap gap-1">
                          {availablePaymentMethods
                            .filter(m => !splitPayments.find(sp => sp.method === m.id))
                            .map((method) => {
                              const Icon = method.icon;
                              return (
                                <Button
                                  key={method.id}
                                  variant="outline"
                                  size="sm"
                                  onClick={() => addSplitPaymentMethod(method.id)}
                                  className="h-8 text-xs border-dashed"
                                >
                                  <Plus className="w-3 h-3 mr-1" />
                                  <Icon className="w-3 h-3 mr-1" />
                                  {method.name}
                                </Button>
                              );
                          })}
                        </div>
                      </div>

                      {/* Split summary */}
                      <div className={cn(
                        "p-3 rounded-lg border text-sm",
                        Math.abs(splitRemaining) < 1 
                          ? "bg-green-50 border-green-200" 
                          : "bg-orange-50 border-orange-200"
                      )}>
                        <div className="flex justify-between">
                          <span className="text-gray-600">Tổng đơn hàng:</span>
                          <span className="font-semibold">{total.toLocaleString('vi-VN')}₫</span>
                        </div>
                        <div className="flex justify-between">
                          <span className="text-gray-600">Đã phân bổ:</span>
                          <span className="font-semibold">{totalSplitAmount.toLocaleString('vi-VN')}₫</span>
                        </div>
                        <Separator className="my-1" />
                        <div className="flex justify-between">
                          <span className={cn(
                            "font-medium",
                            Math.abs(splitRemaining) < 1 ? "text-green-600" : "text-orange-600"
                          )}>
                            {Math.abs(splitRemaining) < 1 ? "✅ Đã khớp" : "Còn thiếu:"}
                          </span>
                          {Math.abs(splitRemaining) >= 1 && (
                            <span className="font-bold text-orange-600">{splitRemaining.toLocaleString('vi-VN')}₫</span>
                          )}
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              )}

              {/* QR Code Display */}
              {selectedPayment === 'qr' && showQRCode && qrCodeData && (
                <div className="space-y-3 border rounded-lg p-4 bg-purple-50">
                  <div className="text-center">
                    <h3 className="font-semibold text-purple-800 mb-2">Quét mã QR để thanh toán</h3>
                    <p className="text-sm text-purple-600 mb-3">
                      Số tiền: <span className="font-bold">{total.toLocaleString('vi-VN')}₫</span>
                    </p>
                    
                    {qrCodeData.qrImageUrl && (
                      <div className="flex justify-center mb-3">
                        <img 
                          src={qrCodeData.qrImageUrl} 
                          alt="QR Code thanh toán" 
                          className="w-72 h-72 border-2 border-purple-300 rounded-lg shadow-lg max-w-full"
                          onError={(e) => {
                            e.currentTarget.src = "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjg4IiBoZWlnaHQ9IjI4OCIgdmlld0JveD0iMCAwIDI4OCAyODgiIGZpbGw9Im5vbmUiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+CjxyZWN0IHdpZHRoPSIyODgiIGhlaWdodD0iMjg4IiBmaWxsPSIjRjNGNEY2Ii8+Cjx0ZXh0IHg9IjE0NCIgeT0iMTQ0IiBmb250LWZhbWlseT0iQXJpYWwiIGZvbnQtc2l6ZT0iMTgiIGZpbGw9IiM2QjczODAiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGR5PSIwLjNlbSI+UVIgTG9hZCBFcnJvcjwvdGV4dD4KPHN2Zz4=";
                          }}
                        />
                      </div>
                    )}
                    
                    <div className="text-xs text-gray-600 space-y-1 print:text-black">
                      {qrCodeData.bankName && <p><span className="font-medium">Ngân hàng:</span> {qrCodeData.bankName}</p>}
                      {qrCodeData.accountNumber && <p><span className="font-medium">Số TK:</span> {qrCodeData.accountNumber}</p>}
                      {qrCodeData.accountHolder && <p><span className="font-medium">Chủ TK:</span> {qrCodeData.accountHolder}</p>}
                      {qrCodeData.description && (
                        <p className="mt-2 p-2 bg-green-100 rounded print:bg-white print:border print:border-black">
                          <span className="font-medium text-green-800 print:text-black">Nội dung CK:</span> 
                          <span className="text-green-700 print:text-black"> {qrCodeData.description}</span>
                          {qrCodeData.description.includes('don hang') && (
                            <span className="block text-xs text-green-600 mt-1 print:text-black">
                              ✓ QR đã cập nhật với mã đơn hàng
                            </span>
                          )}
                        </p>
                      )}
                    </div>
                  </div>
                </div>
              )}

              {/* QR Code không khả dụng */}
              {selectedPayment === 'qr' && !qrSettings?.isEnabled && (
                <div className="space-y-3 border rounded-lg p-4 bg-orange-50">
                  <div className="text-center text-orange-600">
                    <p className="text-sm font-medium">QR Code chưa được cấu hình</p>
                    <p className="text-xs">Vui lòng vào Settings &gt; QR Code để cấu hình</p>
                  </div>
                </div>
              )}

              {/* Action Buttons */}
              <div className="space-y-3 mt-4">
                <Button
                  className="w-full h-12 text-lg font-semibold"
                  onClick={processPayment}
                  disabled={cart.length === 0 || createOrderMutation.isPending || (isSplitPayment && !isSplitValid)}
                  data-testid="button-process-payment"
                >
                  {createOrderMutation.isPending ? "Đang xử lý..." : 
                    isSplitPayment ? `💳 Thanh toán (${splitPayments.length} phương thức)` : "💳 Thanh toán ngay"}
                </Button>
                
                <Button
                  className="w-full h-11 text-lg bg-green-600 hover:bg-green-700"
                  onClick={processPaymentWithEInvoice}
                  disabled={cart.length === 0 || createOrderMutation.isPending || createEInvoiceMutation.isPending}
                  data-testid="button-payment-with-einvoice"
                >
                  <FileText className="w-5 h-5 mr-2" />
                  {createOrderMutation.isPending || createEInvoiceMutation.isPending ? "Đang xử lý..." : "Xuất hóa đơn"}
                </Button>
                
                {/* Save for later section */}
                <div className="pt-2 border-t border-gray-200">
                  <div className="text-xs text-gray-600 mb-2 text-center">
                    Hoặc lưu đơn hàng để thanh toán sau
                  </div>
                  <Button
                    variant="outline"
                    className="w-full h-10 text-sm border-orange-200 text-orange-700 hover:bg-orange-50 hover:border-orange-300"
                    onClick={saveOrderForLater}
                    disabled={cart.length === 0 || saveOrderForLaterMutation.isPending || createOrderMutation.isPending}
                    data-testid="button-save-for-later"
                  >
                    <Clock className="w-4 h-4 mr-2" />
                    {saveOrderForLaterMutation.isPending ? "Đang lưu..." : "💾 Lưu đơn hàng"}
                  </Button>
                  {cart.length > 0 && (
                    <div className="text-xs text-gray-500 mt-1 text-center">
                      Đơn hàng sẽ được lưu với trạng thái "Chờ thanh toán"
                    </div>
                  )}
                </div>
              </div>
              </div> {/* Close scrollable content container */}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Modal chi tiết hóa đơn sau thanh toán */}
      {showOrderDetail && orderDetailData && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4 print:bg-white print:p-0">
          <div
            className="bg-white rounded-lg shadow-lg p-6 w-full max-w-md sm:max-w-lg md:max-w-xl relative max-h-[90vh] overflow-y-auto print:w-[80mm] print:max-w-[80mm] print:min-w-[80mm] print:rounded-none print:shadow-none print:p-2 print:overflow-visible print:max-h-none print:relative print:block print:no-break"
            style={{ 
              width: 'min(90vw, 450px)', 
              fontSize: '14px'
            }}
          >
            <button
              className="absolute top-2 right-2 text-gray-500 hover:text-black print:hidden"
              onClick={() => setShowOrderDetail(false)}
            >
              Đóng
            </button>
            
            {/* Thông tin cửa hàng in đầu bill */}
            <div className="text-center border-b pb-2 mb-2 print:pl-4">
              <div className="font-bold text-lg print:text-sm">{storeInfo?.name || "[Tên cửa hàng]"}</div>
              {storeInfo?.address && <div className="text-sm print:text-xs">Đ/c: {storeInfo.address}</div>}
              {storeInfo?.taxCode && <div className="text-sm print:text-xs">MST: {storeInfo.taxCode}</div>}
              {storeInfo?.phone && <div className="text-sm print:text-xs">ĐT: {storeInfo.phone}</div>}
              {storeInfo?.email && <div className="text-sm print:text-xs">Email: {storeInfo.email}</div>}
            </div>
            
            <h2 className="text-xl font-bold mb-2 print:text-sm print:mb-1 print:pl-4">Đơn hàng #{orderDetailData.orderId}</h2>
            <div className="print:text-xs print:pl-4">Khách hàng: {orderDetailData.customerName || "Khách lẻ"}</div>
            <div className="print:text-xs print:pl-4">Ngày tạo: {new Date(orderDetailData.createdAt).toLocaleDateString('vi-VN')}</div>
            <div className="print:text-xs print:pl-4">Giờ tạo: {new Date(orderDetailData.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</div>
            
            {/* Trạng thái đơn hàng */}
            <div className="flex gap-2 my-2 print:my-1 print:pl-4">
              <Badge className="bg-green-100 text-green-800 border-green-200 print:text-xs print:px-1 print:py-0 print:bg-white print:text-black print:border-black">Đã thanh toán</Badge>
              <Badge className="bg-blue-100 text-blue-800 border-blue-200 print:text-xs print:px-1 print:py-0 print:bg-white print:text-black print:border-black">Hoàn thành</Badge>
            </div>
            
            {/* Thông tin bổ sung */}
            <div className="print:text-xs print:pl-4">Hình thức thanh toán: <b>{orderDetailData.paymentMethod || "Tiền mặt"}</b></div>
            {/* Chi tiết thanh toán chia nhỏ */}
            {orderDetailData.splitPaymentDetails && orderDetailData.splitPaymentDetails.length > 0 && (
              <div className="mt-1 ml-4 print:ml-2 text-sm print:text-xs">
                {(Array.isArray(orderDetailData.splitPaymentDetails) 
                  ? orderDetailData.splitPaymentDetails 
                  : JSON.parse(orderDetailData.splitPaymentDetails)
                ).map((sp: any, idx: number) => (
                  <div key={idx} className="flex justify-between max-w-[250px] print:max-w-full">
                    <span className="text-gray-600 print:text-black">• {sp.methodName}:</span>
                    <span className="font-medium">{Number(sp.amount).toLocaleString('vi-VN')}₫</span>
                  </div>
                ))}
              </div>
            )}
            <div className="print:text-xs print:pl-4">Thu Ngân: <b>{orderDetailData.cashierName || "Admin"}</b></div>
            
            <div className="mt-4 print:no-break">
              <table className="w-full border print:no-break text-sm print:text-xs">
                <thead>
                  <tr className="bg-gray-50 print:bg-white">
                    <th className="border px-1 py-1 text-center print:px-1 print:py-0.5 w-8">STT</th>
                    <th className="border px-2 py-1 print:px-1 print:py-0.5">Tên hàng</th>
                    <th className="border px-1 py-1 text-center print:px-1 print:py-0.5 w-12">ĐVT</th>
                    <th className="border px-1 py-1 text-center print:px-1 print:py-0.5 w-12">SL</th>
                    <th className="border px-2 py-1 text-center print:px-1 print:py-0.5">Đơn giá</th>
                    <th className="border px-2 py-1 text-center print:px-1 print:py-0.5">Thành tiền</th>
                  </tr>
                </thead>
                <tbody>
                  {orderDetailData.items?.map((item: any, idx: number) => (
                    <tr key={idx}>
                      <td className="border px-1 py-1 text-center print:px-1 print:py-0.5">{idx + 1}</td>
                      <td className="border px-2 py-1 print:px-1 print:py-0.5">{item.productName}</td>
                      <td className="border px-1 py-1 text-center print:px-1 print:py-0.5">{item.unit || '-'}</td>
                      <td className="border px-1 py-1 text-center print:px-1 print:py-0.5">{item.quantity}</td>
                      <td className="border px-2 py-1 text-right print:px-1 print:py-0.5">{Number(item.price).toLocaleString('vi-VN')}₫</td>
                      <td className="border px-2 py-1 text-right print:px-1 print:py-0.5">{Number(item.totalPrice).toLocaleString('vi-VN')}₫</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {/* Tính lại tạm tính và VAT từ items */}
              <div className="mt-2 text-right">
                {(() => {
                  const subtotal = orderDetailData.subtotal || orderDetailData.items?.reduce((sum: number, item: any) => sum + (Number(item.totalPrice) || 0), 0) || 0;
                  const taxAmount = Number(orderDetailData.taxAmount) || 0;
                  const discountAmount = Number(orderDetailData.discountAmount) || 0;
                  return (
                    <>
                      <div>Tạm tính: <b>{subtotal.toLocaleString('vi-VN')}₫</b></div>
                      {discountAmount > 0 && (
                        <div className="text-green-600 print:text-black">
                          Giảm giá {(() => {
                            if (orderDetailData.discountName && orderDetailData.discountName !== 'Giảm giá thủ công') {
                              return `(${orderDetailData.discountName})`;
                            } else if (orderDetailData.discountType) {
                              return `(${orderDetailData.discountType})`;
                            } else {
                              return '(Giảm giá thủ công)';
                            }
                          })()}: 
                          <b> -{discountAmount.toLocaleString('vi-VN')}₫</b>
                        </div>
                      )}
                      {taxAmount > 0 && (
                        <div>VAT 10%: <b>{taxAmount.toLocaleString('vi-VN')}₫</b></div>
                      )}
                    </>
                  );
                })()}
              </div>
            </div>
            
            <div className="mt-4 text-right font-bold text-lg border-t pt-2 print:no-break">
              Tổng cộng: {Number(orderDetailData.totalAmount).toLocaleString('vi-VN')}₫
            </div>
            
            {/* Ghi chú hóa đơn */}
            {orderDetailData.notes && (
              <div className="mt-4 print:mt-2 print:no-break border-t pt-2">
                <div className="text-sm font-medium mb-1 print:text-xs">Ghi chú:</div>
                <div className="text-sm text-gray-700 print:text-xs print:text-black">{orderDetailData.notes}</div>
              </div>
            )}
            
            {/* QR Code cho thanh toán QR - Đặt sau tổng cộng */}
            {(orderDetailData.paymentMethod === 'qr' || orderDetailData.paymentMethod === 'QR Code' || orderDetailData.paymentMethod?.toLowerCase().includes('qr')) && (
              <div className="mt-4 text-center print:mt-2 print:border-0 print:p-0 print:bg-white border rounded-lg p-4 bg-gradient-to-br from-purple-50 to-blue-50 border-purple-200 print:no-break">
                <h4 className="font-semibold text-purple-800 mb-3 text-base print:text-black print:text-sm print:mb-1 print:font-bold">Mã QR Thanh toán</h4>
                
                {generateQRUrl(orderDetailData.totalAmount, orderDetailData.orderId) ? (
                  <>
                    <div className="flex justify-center mb-4 print:mb-1">
                      <div className="p-2 bg-white rounded-xl shadow-lg border-2 border-purple-200 w-full max-w-full print:p-0 print:shadow-none print:border-0 print:rounded-none print:bg-transparent">
                        <img 
                          src={generateQRUrl(orderDetailData.totalAmount, orderDetailData.orderId) || ""}
                          alt="QR Code thanh toán" 
                          className="w-full h-auto object-contain mx-auto print:w-full print:h-auto"
                          style={{ 
                            width: '100%', 
                            height: 'auto', 
                            minWidth: '200px', 
                            minHeight: '200px', 
                            maxWidth: '300px', 
                            display: 'block' 
                          }}
                          onError={(e) => {
                            e.currentTarget.src = "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjUwIiBoZWlnaHQ9IjI1MCIgdmlld0JveD0iMCAwIDI1MCAyNTAiIGZpbGw9Im5vbmUiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+CjxyZWN0IHdpZHRoPSIyNTAiIGhlaWdodD0iMjUwIiBmaWxsPSIjRjNGNEY2Ii8+Cjx0ZXh0IHg9IjEyNSIgeT0iMTI1IiBmb250LWZhbWlseT0iQXJpYWwiIGZvbnQtc2l6ZT0iMTQiIGZpbGw9IiM2QjczODAiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGR5PSIwLjNlbSI+UVIgRXJyb3I8L3RleHQ+Cjwvc3ZnPg==";
                          }}
                        />
                      </div>
                    </div>
                  </>
                ) : (
                  <div className="text-center text-orange-600 py-6">
                    <div className="w-full h-48 mx-auto bg-orange-100 rounded-xl flex items-center justify-center mb-3">
                      <span className="text-orange-500 text-sm font-medium">QR không khả dụng</span>
                    </div>
                    <p className="text-sm font-medium">QR Code chưa được cấu hình</p>
                    <p className="text-xs">Vui lòng vào Settings &gt; QR Code để cấu hình</p>
                  </div>
                )}
              </div>
            )}
            
            <div className="mt-6 text-center font-semibold text-gray-700">
              Cảm ơn - Hẹn gặp lại
            </div>
            
            {/* Auto print status indicator removed */}
            
            <div className="mt-4 print:hidden">
              {/* Other Actions */}
              <div className="flex flex-col gap-3 w-full">
                {/* Hàng trên: Xuất hóa đơn và In đơn hàng */}
                <div className="flex gap-3">
                  {eInvoiceConfig?.isEnabled && (
                    <Button 
                      onClick={handleCreateEInvoice} 
                      variant="outline"
                      className="text-blue-600 border-blue-600 hover:bg-blue-50 flex-1"
                    >
                      <FileText className="w-4 h-4 mr-2" />
                      Xuất hóa đơn điện tử
                    </Button>
                  )}
                  <Button 
                    onClick={() => window.print()} 
                    className="bg-green-600 hover:bg-green-700 text-white flex-1"
                  >
                    <Printer className="w-4 h-4 mr-2" />
                    In đơn hàng
                  </Button>
                </div>
                
                {/* Hàng dưới: Đóng */}
                <div className="flex justify-center">
                  <Button 
                    onClick={() => setShowOrderDetail(false)}
                    variant="outline"
                    className="px-8"
                  >
                    Đóng
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* E-Invoice Form Modal */}
      {showEInvoiceForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <h3 className="text-lg font-semibold mb-4 flex items-center">
                <FileText className="w-5 h-5 mr-2 text-blue-600" />
                {isCreateOrderWithEInvoice ? "Thanh toán & Xuất hóa đơn điện tử" : "Tạo hóa đơn điện tử"}
              </h3>
              
              <div className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Mã số thuế người mua
                    </label>
                    <Input
                      value={eInvoiceData.buyerTaxCode}
                      onChange={(e) => setEInvoiceData(prev => ({ ...prev, buyerTaxCode: e.target.value }))}
                      placeholder="Nhập mã số thuế"
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Tên người mua *
                    </label>
                    <Input
                      value={eInvoiceData.buyerName}
                      onChange={(e) => setEInvoiceData(prev => ({ ...prev, buyerName: e.target.value }))}
                      placeholder="Nhập tên người mua"
                      required
                    />
                  </div>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Địa chỉ người mua
                  </label>
                  <Input
                    value={eInvoiceData.buyerAddress}
                    onChange={(e) => setEInvoiceData(prev => ({ ...prev, buyerAddress: e.target.value }))}
                    placeholder="Nhập địa chỉ"
                  />
                </div>
                
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Số điện thoại
                    </label>
                    <Input
                      value={eInvoiceData.buyerPhone}
                      onChange={(e) => setEInvoiceData(prev => ({ ...prev, buyerPhone: e.target.value }))}
                      placeholder="Nhập số điện thoại"
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Email
                    </label>
                    <Input
                      type="email"
                      value={eInvoiceData.buyerEmail}
                      onChange={(e) => setEInvoiceData(prev => ({ ...prev, buyerEmail: e.target.value }))}
                      placeholder="Nhập email"
                    />
                  </div>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Ghi chú
                  </label>
                  <textarea
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    rows={3}
                    value={eInvoiceData.notes}
                    onChange={(e) => setEInvoiceData(prev => ({ ...prev, notes: e.target.value }))}
                    placeholder="Nhập ghi chú (tùy chọn)"
                  />
                </div>
              </div>
              
              <div className="mt-6 flex justify-end gap-3">
                <Button 
                  onClick={() => {
                    setShowEInvoiceForm(false);
                    setIsCreateOrderWithEInvoice(false);
                  }} 
                  variant="outline"
                  disabled={createEInvoiceMutation.isPending}
                >
                  Hủy
                </Button>
                <Button 
                  onClick={submitEInvoice}
                  disabled={createEInvoiceMutation.isPending || !eInvoiceData.buyerName}
                  className="bg-blue-600 hover:bg-blue-700"
                >
                  <Send className="w-4 h-4 mr-2" />
                  {createEInvoiceMutation.isPending ? "Đang xử lý..." : 
                    (isCreateOrderWithEInvoice ? "Thanh toán & Xuất hóa đơn" : "Tạo hóa đơn")}
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Camera Barcode Scanner */}
      <BarcodeScanner
        isOpen={showCameraScanner}
        onScan={handleCameraScan}
        onClose={() => setShowCameraScanner(false)}
      />
    </AppLayout>
  );
}

