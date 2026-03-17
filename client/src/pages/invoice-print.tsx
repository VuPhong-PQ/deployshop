import { useEffect, useState } from "react";
import { useRoute } from "wouter";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";
import { apiRequest } from "@/lib/queryClient";
import { useLocation } from "wouter";

interface OrderDetail {
  orderId: number;
  customerId?: number;
  customerName?: string;
  createdAt: string;
  totalAmount: number;
  subTotal: number;
  taxAmount: number;
  discountAmount: number;
  paymentMethod?: string;
  paymentStatus?: string;
  status?: string;
  orderNumber?: string;
  cashierId?: string;
  storeId?: string;
  notes?: string;
  customer?: {
    customerId: number;
    hoTen: string;
    soDienThoai?: string;
    email?: string;
    diaChi?: string;
  };
  splitPaymentDetails?: string | null;
  items: {
    productId: number;
    productName: string;
    quantity: number;
    price: number;
    totalPrice: number;
  }[];
}

interface SplitPaymentEntry {
  method: string;
  methodName: string;
  amount: number;
}

export default function InvoicePrint() {
  const [, params] = useRoute("/invoice-print/:orderId");
  const [, navigate] = useLocation();
  const [isPrinting, setIsPrinting] = useState(false);
  
  const orderId = params?.orderId ? parseInt(params.orderId) : null;

  // Fetch order details
  const { data: orderDetail, isLoading } = useQuery<OrderDetail>({
    queryKey: ['/api/orders', orderId],
    queryFn: () => apiRequest(`/api/orders/${orderId}`, { method: 'GET' }),
    enabled: !!orderId,
  });

  // Auto-print if requested via query param - chỉ sau khi data đã load
  useEffect(() => {
    if (!orderDetail) return; // Chỉ auto-print khi có data
    
    try {
      const sp = new URLSearchParams(window.location.search);
      if (sp.get('autoPrint') === '1') {
        setIsPrinting(true);
        // Đợi lâu hơn để đảm bảo CSS đã load
        setTimeout(() => {
          window.print();
          setTimeout(() => {
            setIsPrinting(false);
            try { window.close(); } catch (e) { /* ignore */ }
          }, 1000);
        }, 1500); // Tăng delay từ 500ms lên 1500ms
      }
    } catch (err) {
      // ignore
    }
  }, [orderDetail]); // Dependency vào orderDetail

  // Format payment method
  const formatPaymentMethod = (method?: string) => {
    switch (method) {
      case 'cash': return 'Tiền mặt';
      case 'card': return 'Thẻ ngân hàng';
      case 'qr': return 'QR Code';
      case 'ewallet': return 'Ví điện tử';
      case 'banktransfer': return 'Chuyển khoản';
      case 'foreignusd': return 'Ngoại tệ USD';
      case 'foreigneur': return 'Ngoại tệ EUR';
      case 'split': return 'Thanh toán chia nhỏ';
      default: return 'Tiền mặt';
    }
  };

  // Parse split payment details
  const parseSplitPayments = (): SplitPaymentEntry[] | null => {
    if (!orderDetail?.splitPaymentDetails) return null;
    try {
      const splits = JSON.parse(orderDetail.splitPaymentDetails) as SplitPaymentEntry[];
      if (Array.isArray(splits) && splits.length > 0) return splits;
      return null;
    } catch {
      return null;
    }
  };

  const splitPayments = parseSplitPayments();

  // Format payment status
  const formatPaymentStatus = (status?: string) => {
    switch (status) {
      case 'paid': return 'Đã thanh toán';
      case 'pending': return 'Chờ thanh toán';
      case 'failed': return 'Thanh toán thất bại';
      default: return 'Đã thanh toán';
    }
  };

  // Format order status
  const formatOrderStatus = (status?: string) => {
    switch (status) {
      case 'completed': return 'Hoàn thành';
      case 'pending': return 'Đang xử lý';
      case 'cancelled': return 'Đã hủy';
      default: return 'Hoàn thành';
    }
  };

  // Handle print
  const handlePrint = () => {
    setIsPrinting(true);
    window.print();
    setTimeout(() => setIsPrinting(false), 1000);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Đang tải chi tiết đơn hàng...</p>
        </div>
      </div>
    );
  }

  if (!orderDetail) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <p className="text-gray-600">Không tìm thấy đơn hàng</p>
          <Button 
            onClick={() => navigate('/orders')} 
            className="mt-4"
            variant="outline"
          >
            Quay lại danh sách đơn hàng
          </Button>
        </div>
      </div>
    );
  }

  return (
    <>
      {/* Print-specific styles */}
      <style>{`
        @media print {
          @page {
            size: 80mm auto;
            margin: 2mm;
          }
          
          /* Reset và ẩn tất cả */
          * {
            visibility: hidden !important;
          }
          
          html, body {
            width: 76mm !important;
            height: auto !important;
            overflow: visible !important;
            font-size: 11px !important;
            line-height: 1.2 !important;
            font-family: Arial, sans-serif !important;
          }
          
          /* Chỉ hiện invoice content và children của nó */
          .invoice-content,
          .invoice-content * {
            visibility: visible !important;
            color: #000 !important;
            background: white !important;
            border-color: #000 !important;
          }
          
          .invoice-content {
            position: absolute !important;
            left: 0 !important;
            top: 0 !important;
            width: 76mm !important;
            max-width: 76mm !important;
            margin: 0 !important;
            padding: 2mm !important;
            font-size: 10px !important;
          }
          
          /* Typography cho 80mm */
          .invoice-content h2 {
            font-size: 12px !important;
            margin: 2mm 0 !important;
          }
          
          .invoice-content .text-xl {
            font-size: 12px !important;
          }
          
          .invoice-content .text-lg {
            font-size: 11px !important;
          }
          
          .invoice-content .text-sm {
            font-size: 9px !important;
          }
          
          .invoice-content .text-xs {
            font-size: 8px !important;
          }
          
          /* Table styling */
          .invoice-content table {
            width: 100% !important;
            border-collapse: collapse !important;
            margin: 1mm 0 !important;
          }
          
          .invoice-content th,
          .invoice-content td {
            padding: 1mm !important;
            border: 1px solid #000 !important;
            font-size: 8px !important;
          }
          
          /* Hide gradients and colors for print */
          .invoice-content .bg-gradient-to-br,
          .invoice-content .from-purple-50,
          .invoice-content .to-blue-50,
          .invoice-content .bg-purple-50,
          .invoice-content .bg-green-100,
          .invoice-content .bg-blue-100,
          .invoice-content .bg-gray-50 {
            background: white !important;
          }
          
          .invoice-content .text-purple-800,
          .invoice-content .text-green-800,
          .invoice-content .text-green-700,
          .invoice-content .text-blue-800 {
            color: #000 !important;
          }
          
          /* Hide screen elements */
          .no-print {
            display: none !important;
            visibility: hidden !important;
          }
        }
        
        /* Screen styles - Đơn giản, vuông */
        @media screen {
          .invoice-content {
            max-width: 400px;
            width: 400px;
            margin: 0 auto;
            background: white;
            box-shadow: 0 0 5px rgba(0, 0, 0, 0.2);
            padding: 16px;
            border: 1px solid #000;
            min-height: fit-content;
            font-size: 14px;
          }
        }
      `}</style>

      <div className="min-h-screen bg-gray-50">
        {/* Header - Hidden when printing */}
        <div className="no-print bg-white shadow-sm border-b p-4">
          <div className="max-w-4xl mx-auto flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Button
                variant="outline"
                onClick={() => window.history.back()}
                className="flex items-center gap-2"
              >
                <ArrowLeft className="h-4 w-4" />
                Quay lại
              </Button>
              <h1 className="text-2xl font-bold">In hóa đơn #{orderDetail?.orderId}</h1>
            </div>
            <div className="flex items-center gap-2">
              <Button
                onClick={handlePrint}
                disabled={isPrinting}
                className="flex items-center gap-2 bg-green-600 hover:bg-green-700"
              >
                {isPrinting ? 'Đang in...' : 'In ngay'}
              </Button>
              <Button
                variant="outline"
                onClick={() => window.close()}
                className="flex items-center gap-2"
              >
                Đóng
              </Button>
            </div>
          </div>
        </div>

        {/* Invoice content */}
        <div className="invoice-content p-4">
          {/* Store header - Đơn giản */}
          <div className="text-center mb-3">
            <div className="font-bold text-lg">Pinkwish Shop</div>
            <div className="text-sm">Đ/c: Tổ 2, Dương Bào, Đặc Khu Phú Quốc, An Giang</div>
            <div className="text-sm">ĐT: 0773491130</div>
            <div className="text-sm">Email: ruby7080@gmail.com</div>
          </div>
          <div style={{ borderTop: '1px solid #000', margin: '8px 0' }}></div>

          {/* Invoice title */}
          <h2 className="text-xl font-bold mb-2">Đơn hàng #{orderDetail.orderId}</h2>
          
          {/* Order info - Theo style sales */}
          <div className="mb-3 text-sm">
            <div>Khách hàng: {orderDetail.customerName || orderDetail.customer?.hoTen || 'Khách lẻ'}</div>
            <div>Ngày tạo: {new Date(orderDetail.createdAt).toLocaleDateString('vi-VN')}</div>
            <div>Giờ tạo: {new Date(orderDetail.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</div>
            
            {/* Status badges */}
            <div className="flex gap-2 my-2">
              <span className="inline-flex px-2 py-1 text-xs font-medium bg-green-100 text-green-800 rounded border border-green-200">
                {formatPaymentStatus(orderDetail.paymentStatus)}
              </span>
              <span className="inline-flex px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 rounded border border-blue-200">
                {formatOrderStatus(orderDetail.status)}
              </span>
            </div>
            
            <div>Hình thức thanh toán: <strong>{formatPaymentMethod(orderDetail.paymentMethod)}</strong></div>
            
            {/* Split payment details */}
            {splitPayments && splitPayments.length > 0 && (
              <div className="mt-2 mb-1" style={{ border: '1px solid #ccc', borderRadius: '4px', padding: '6px 8px', background: '#f8f9ff' }}>
                <div className="font-bold text-xs mb-1" style={{ color: '#4338ca' }}>Chi tiết chia bill:</div>
                {splitPayments.map((sp, idx) => (
                  <div key={idx} className="flex justify-between text-xs" style={{ padding: '2px 0' }}>
                    <span>{sp.methodName || formatPaymentMethod(sp.method)}</span>
                    <strong>{Number(sp.amount).toLocaleString('vi-VN')}₫</strong>
                  </div>
                ))}
                <div className="flex justify-between text-xs font-bold" style={{ borderTop: '1px dashed #ccc', marginTop: '4px', paddingTop: '4px' }}>
                  <span>Tổng:</span>
                  <span>{splitPayments.reduce((s, p) => s + Number(p.amount), 0).toLocaleString('vi-VN')}₫</span>
                </div>
              </div>
            )}
            
            <div>Thu Ngân: <strong>Admin</strong></div>
          </div>

          {/* Products table - Đơn giản, vuông */}
          <div className="mb-4">
            <table className="w-full border-collapse">
              <thead>
                <tr>
                  <th className="px-2 py-2 text-left text-xs font-medium border border-black">STT</th>
                  <th className="px-2 py-2 text-left text-xs font-medium border border-black">Tên hàng</th>
                  <th className="px-2 py-2 text-center text-xs font-medium border border-black">ĐVT</th>
                  <th className="px-2 py-2 text-center text-xs font-medium border border-black">SL</th>
                  <th className="px-2 py-2 text-right text-xs font-medium border border-black">Đơn giá</th>
                  <th className="px-2 py-2 text-right text-xs font-medium border border-black">Thành tiền</th>
                </tr>
              </thead>
              <tbody>
                {orderDetail.items.map((item, index) => (
                  <tr key={index}>
                    <td className="px-2 py-2 text-xs border border-black">{index + 1}</td>
                    <td className="px-2 py-2 text-xs border border-black">{item.productName}</td>
                    <td className="px-2 py-2 text-center text-xs border border-black">gói</td>
                    <td className="px-2 py-2 text-center text-xs border border-black">{item.quantity}</td>
                    <td className="px-2 py-2 text-right text-xs border border-black">
                      {(item.totalPrice / item.quantity).toLocaleString('vi-VN')}₫
                    </td>
                    <td className="px-2 py-2 text-right text-xs font-medium border border-black">
                      {item.totalPrice.toLocaleString('vi-VN')}₫
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Summary totals - Đơn giản */}
          <div style={{ borderTop: '1px solid #000', paddingTop: '8px', marginTop: '8px' }}>
            <div className="text-right text-sm">
              <div>Tạm tính: <strong>{(orderDetail.subTotal || orderDetail.totalAmount).toLocaleString('vi-VN')}₫</strong></div>
              {orderDetail.discountAmount > 0 && (
                <div>Giảm giá: <strong>-{orderDetail.discountAmount.toLocaleString('vi-VN')}₫</strong></div>
              )}
              {orderDetail.taxAmount > 0 && (
                <div>VAT 10%: <strong>{orderDetail.taxAmount.toLocaleString('vi-VN')}₫</strong></div>
              )}
            </div>
            
            <div className="text-right font-bold text-lg" style={{ borderTop: '1px solid #000', paddingTop: '4px', marginTop: '4px' }}>
              Tổng cộng: {orderDetail.totalAmount.toLocaleString('vi-VN')}₫
            </div>
          </div>

          {/* QR Code cho thanh toán QR - Đơn giản */}
          {(orderDetail.paymentMethod === 'qr' || orderDetail.paymentMethod === 'QR Code' || orderDetail.paymentMethod?.toLowerCase().includes('qr')) && (
            <div className="mt-4 text-center" style={{ border: '1px solid #000', padding: '8px' }}>
              <div className="font-bold mb-2">THÔNG TIN CHUYỂN KHOẢN</div>
              <div className="text-sm">
                <div>Số TK: 8811192753</div>
                <div>Ngân hàng: Ngân hàng TMCP Đầu tư và Phát triển Việt Nam</div>
                <div>Chủ TK: HO KINH DOANH PINK WISH SHOP</div>
                <div className="mt-2">
                  <strong>Nội dung CK: Thanh toan don hang #{orderDetail.orderId}</strong>
                </div>
              </div>
            </div>
          )}
          
          {/* Footer message */}
          <div className="mt-4 text-center text-sm font-bold">
            ★ Cảm ơn quý khách - Hẹn gặp lại! ★
          </div>
        </div>
      </div>
    </>
  );
}