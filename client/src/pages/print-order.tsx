import { useEffect, useState } from "react";
import { useRoute } from "wouter";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Printer, ArrowLeft } from "lucide-react";
import { apiRequest } from "@/lib/queryClient";
import { useLocation } from "wouter";

interface PrintConfig {
  id: number;
  printerName: string;
  paperSize: string;
  printCopies: number;
  autoPrintBill: boolean;
  autoPrintOnOrder: boolean;
  printBarcode: boolean;
  printLogo: boolean;
}

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
  items: {
    productId: number;
    productName: string;
    quantity: number;
    price: number;
    totalPrice: number;
  }[];
}

export default function PrintOrder() {
  const [, params] = useRoute("/print-order/:orderId");
  const [, navigate] = useLocation();
  const [isPrinting, setIsPrinting] = useState(false);
  
  const orderId = params?.orderId ? parseInt(params.orderId) : null;

  // Fetch print configuration
  const { data: printConfig } = useQuery<PrintConfig>({
    queryKey: ['/api/printconfig'],
    queryFn: () => apiRequest('/api/printconfig', { method: 'GET' }),
  });

  // Fetch order details
  const { data: orderDetail, isLoading } = useQuery<OrderDetail>({
    queryKey: ['/api/orders', orderId],
    queryFn: () => apiRequest(`/api/orders/${orderId}`, { method: 'GET' }),
    enabled: !!orderId,
  });

  // Check if it's a POS printer
  const isPOSPrinter = printConfig?.printerName?.includes("POS") || 
                      printConfig?.printerName?.includes("80") || false;

  // Format payment method
  const formatPaymentMethod = (method?: string) => {
    switch (method) {
      case 'cash': return 'Tiền mặt';
      case 'card': return 'Thẻ ngân hàng';
      case 'qr': return 'QR Code';
      case 'ewallet': return 'Ví điện tử';
      default: return 'Tiền mặt';
    }
  };

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
      {/* POS Printer specific styles */}
      {isPOSPrinter && (
        <style>{`
          @media print {
            @page {
              size: 80mm auto;
              margin: 0;
            }
            * {
              box-sizing: border-box;
            }
            body {
              margin: 0;
              padding: 0;
              font-family: "Courier New", monospace;
              font-size: 9px;
              line-height: 1.1;
              width: 80mm;
              max-width: 80mm;
              overflow-x: hidden;
            }
            body * {
              visibility: hidden;
            }
            .print-content, .print-content * {
              visibility: visible;
            }
            /* Force all green colors to black for thermal printing */
            .text-green-600,
            .text-green-700,
            .text-green-800,
            .text-green-500,
            .text-green-900,
            [class*="text-green"] {
              color: #000 !important;
            }
            /* Force all gray colors to black for thermal printing */
            .text-gray-500,
            .text-gray-600,
            .text-gray-700,
            .text-gray-800,
            [class*="text-gray"] {
              color: #000 !important;
            }
            .bg-green-50,
            .bg-green-100,
            .bg-green-200,
            [class*="bg-green"] {
              background-color: transparent !important;
            }
            .border-green-200,
            .border-green-400,
            .border-green-600,
            [class*="border-green"] {
              border-color: #000 !important;
            }
            /* Force ALL text to black for thermal printing */
            * {
              color: #000 !important;
              background-color: transparent !important;
            }
            /* Ensure strong contrast for thermal printing */
            p, span, div, label, input, button, a {
              color: #000 !important;
            }
            .print-content {
              position: absolute;
              left: 0;
              top: 0;
              width: 80mm;
              max-width: 80mm;
              padding: 2mm;
              background: white;
              box-sizing: border-box;
              font-size: 9px;
              overflow-x: hidden;
              word-wrap: break-word;
            }
            .no-print {
              display: none !important;
            }
            .store-info {
              text-align: center;
              font-size: 10px;
              margin-bottom: 2mm;
              border-bottom: 1px dashed #000;
              padding-bottom: 2mm;
              width: 100%;
              max-width: 76mm;
            }
            .store-info .store-name {
              font-weight: bold;
              font-size: 12px;
              margin-bottom: 1mm;
              word-wrap: break-word;
            }
            .store-info div {
              font-size: 8px;
              margin: 0.5mm 0;
              word-wrap: break-word;
            }
            .order-header {
              text-align: center;
              font-weight: bold;
              font-size: 11px;
              margin: 2mm 0;
              word-wrap: break-word;
            }
            .order-details {
              font-size: 8px;
              margin-bottom: 2mm;
              border-bottom: 1px dashed #666;
              padding-bottom: 2mm;
              width: 100%;
              max-width: 76mm;
            }
            .order-details div {
              margin: 0.5mm 0;
              word-wrap: break-word;
            }
            .item-list {
              margin: 2mm 0;
            }
            .item {
              margin-bottom: 2mm;
              font-size: 8px;
              border-bottom: 1px dotted #ccc;
              padding-bottom: 1mm;
              width: 100%;
              max-width: 76mm;
            }
            .item-name {
              font-weight: bold;
              margin-bottom: 0.5mm;
              word-wrap: break-word;
              overflow-wrap: break-word;
              max-width: 76mm;
              white-space: normal;
            }
            .item-line {
              display: flex;
              justify-content: space-between;
              font-size: 7px;
              width: 100%;
              max-width: 76mm;
              flex-wrap: nowrap;
            }
            .totals {
              border-top: 1px dashed #000;
              padding-top: 2mm;
              margin-top: 2mm;
              width: 100%;
              max-width: 76mm;
            }
            .total-line {
              display: flex;
              justify-content: space-between;
              font-size: 8px;
              margin: 0.5mm 0;
              width: 100%;
              max-width: 76mm;
            }
            .grand-total {
              display: flex;
              justify-content: space-between;
              font-weight: bold;
              font-size: 10px;
              border-top: 2px solid #000;
              border-bottom: 2px solid #000;
              width: 100%;
              max-width: 76mm;
              padding: 1mm 0;
              margin: 2mm 0;
            }
            .footer {
              text-align: center;
              font-size: 8px;
              margin-top: 3mm;
              border-top: 1px dashed #000;
              padding-top: 2mm;
              width: 100%;
              max-width: 76mm;
              word-wrap: break-word;
              font-weight: bold;
              color: #000 !important;
            }
            .payment-status, .order-status {
              font-weight: bold;
              color: #000 !important;
              background: none !important;
              border: 1px solid #000 !important;
              padding: 1mm;
              margin: 0.5mm 0;
              display: inline-block;
              font-size: 7px;
            }
            .bank-info {
              font-weight: bold;
              color: #000 !important;
              font-size: 7px;
              text-align: center;
              margin: 1mm 0;
            }
          }
        `}</style>
      )}

      {/* A4 Printer styles */}
      {!isPOSPrinter && (
        <style>{`
          @media print {
            @page {
              size: A4;
              margin: 15mm;
            }
            body {
              margin: 0;
              padding: 0;
              font-family: Arial, sans-serif;
              font-size: 12px;
              line-height: 1.4;
            }
            body * {
              visibility: hidden;
            }
            .print-content, .print-content * {
              visibility: visible;
            }
            .print-content {
              position: absolute;
              left: 0;
              top: 0;
              width: 100%;
              background: white;
            }
            .no-print {
              display: none !important;
            }
            .payment-status, .order-status {
              font-weight: bold;
              color: #000 !important;
              background: none !important;
              border: 1px solid #000 !important;
              padding: 2mm;
              margin: 1mm;
              display: inline-block;
              font-size: 11px;
            }
            .bank-info {
              font-weight: bold;
              color: #000 !important;
              font-size: 11px;
              text-align: center;
              margin: 3mm 0;
            }
            .footer {
              color: #000 !important;
              font-weight: bold;
            }
            /* Force all green colors to black for thermal printing */
            .text-green-600,
            .text-green-700,
            .text-green-800,
            .text-green-500,
            .text-green-900,
            [class*="text-green"] {
              color: #000 !important;
            }
            /* Force all gray colors to black for thermal printing */
            .text-gray-500,
            .text-gray-600,
            .text-gray-700,
            .text-gray-800,
            [class*="text-gray"] {
              color: #000 !important;
            }
            .bg-green-50,
            .bg-green-100,
            .bg-green-200,
            [class*="bg-green"] {
              background-color: transparent !important;
            }
            .border-green-200,
            .border-green-400,
            .border-green-600,
            [class*="border-green"] {
              border-color: #000 !important;
            }
            /* Force ALL text to black for thermal printing */
            * {
              color: #000 !important;
              background-color: transparent !important;
            }
            /* Ensure strong contrast for thermal printing */
            p, span, div, label, input, button, a {
              color: #000 !important;
            }
          }
        `}</style>
      )}

      {/* Screen preview styles */}
      <style>{`
        @media screen {
          .print-content {
            max-width: ${isPOSPrinter ? '80mm' : '800px'};
            width: ${isPOSPrinter ? '80mm' : 'auto'};
            margin: 0 auto;
            background: white;
            box-shadow: 0 0 10px rgba(0,0,0,0.1);
            padding: 20px;
            overflow-x: ${isPOSPrinter ? 'hidden' : 'auto'};
            word-wrap: break-word;
          }
          ${isPOSPrinter ? `
          .print-content * {
            max-width: 100%;
            word-wrap: break-word;
            overflow-wrap: break-word;
          }
          .print-content .item-name {
            white-space: normal;
            line-height: 1.2;
          }
          .print-content .item-line {
            font-size: 0.9em;
          }
          ` : ''}
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
              <h1 className="text-2xl font-bold">In đơn hàng #{orderDetail.orderId}</h1>
              <span className="text-sm text-gray-500">
                ({isPOSPrinter ? 'POS-80C' : 'A4'})
              </span>
            </div>
            <Button
              onClick={handlePrint}
              disabled={isPrinting}
              className="flex items-center gap-2"
            >
              <Printer className="h-4 w-4" />
              {isPrinting ? 'Đang in...' : 'In đơn hàng'}
            </Button>
          </div>
        </div>

        {/* Print content */}
        <div className="print-content p-8">
          {/* Store info */}
          <div className="store-info">
            <div className="store-name">Pinkwish Shop</div>
            <div>Đ/c: Phú quốc - An giang</div>
            <div>MST: 0123456789</div>
            <div>ĐT: 0907999841</div>
            <div>Email: pwshop@gmail.com</div>
          </div>

          {/* Order header */}
          <div className="order-header">
            Đơn hàng #{orderDetail.orderId}
          </div>

          {/* Order details */}
          <div className="order-details">
            <div>Khách hàng: {orderDetail.customerName || orderDetail.customer?.hoTen || 'Khách lẻ'}</div>
            <div>Ngày: {new Date(orderDetail.createdAt).toLocaleDateString('vi-VN')}</div>
            <div>Giờ: {new Date(orderDetail.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</div>
            <div>Thanh toán: {formatPaymentMethod(orderDetail.paymentMethod)}</div>
            <div>Thu ngân: Admin</div>
            
            {/* Trạng thái thanh toán và đơn hàng */}
            <div style={{ marginTop: '2mm' }}>
              <span className="payment-status">
                {formatPaymentStatus(orderDetail.paymentStatus)}
              </span>
              <span style={{ margin: '0 2mm' }}></span>
              <span className="order-status">
                {formatOrderStatus(orderDetail.status)}
              </span>
            </div>
          </div>

          {/* Items */}
          {isPOSPrinter ? (
            /* POS format - simple list */
            <div className="item-list">
              {orderDetail.items.map((item, index) => (
                <div key={index} className="item">
                  <div className="item-name">{item.productName}</div>
                  <div className="item-line">
                    <span>{item.quantity} x {(item.totalPrice / item.quantity).toLocaleString('vi-VN')}₫</span>
                    <span>{item.totalPrice.toLocaleString('vi-VN')}₫</span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            /* A4 format - table */
            <table className="w-full border-collapse border border-gray-300 my-4">
              <thead>
                <tr className="bg-gray-100">
                  <th className="border border-gray-300 px-3 py-2 text-left">Sản phẩm</th>
                  <th className="border border-gray-300 px-3 py-2 text-center">SL</th>
                  <th className="border border-gray-300 px-3 py-2 text-right">Đơn giá</th>
                  <th className="border border-gray-300 px-3 py-2 text-right">Thành tiền</th>
                </tr>
              </thead>
              <tbody>
                {orderDetail.items.map((item, index) => (
                  <tr key={index}>
                    <td className="border border-gray-300 px-3 py-2">{item.productName}</td>
                    <td className="border border-gray-300 px-3 py-2 text-center">{item.quantity}</td>
                    <td className="border border-gray-300 px-3 py-2 text-right">
                      {(item.totalPrice / item.quantity).toLocaleString('vi-VN')}₫
                    </td>
                    <td className="border border-gray-300 px-3 py-2 text-right">
                      {item.totalPrice.toLocaleString('vi-VN')}₫
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {/* Totals */}
          <div className="totals">
            <div className="total-line">
              <span>Tạm tính:</span>
              <span>{orderDetail.subTotal.toLocaleString('vi-VN')}₫</span>
            </div>
            {orderDetail.taxAmount > 0 && (
              <div className="total-line">
                <span>VAT 10%:</span>
                <span>{orderDetail.taxAmount.toLocaleString('vi-VN')}₫</span>
              </div>
            )}
            {orderDetail.discountAmount > 0 && (
              <div className="total-line">
                <span>Giảm giá:</span>
                <span>-{orderDetail.discountAmount.toLocaleString('vi-VN')}₫</span>
              </div>
            )}
            <div className="grand-total">
              <span>TỔNG CỘNG:</span>
              <span>{orderDetail.totalAmount.toLocaleString('vi-VN')}₫</span>
            </div>
          </div>

          {/* Footer */}
          <div className="footer">
            {/* Thông tin ngân hàng cho thanh toán QR/chuyển khoản */}
            {(orderDetail.paymentMethod === 'qr' || orderDetail.paymentMethod === 'QR Code' || orderDetail.paymentMethod?.toLowerCase().includes('qr')) && (
              <div className="bank-info">
                <div>Số TK: 8811192753</div>
                <div>Ngân hàng TMCP Đầu tư và Phát triển Việt Nam</div>
                <div>Tên chủ TK: HO KINH DOANH PINK WISH SHOP</div>
              </div>
            )}
            <div style={{ marginTop: '2mm', fontSize: '9px', fontWeight: 'bold', color: '#000' }}>
              Cảm ơn - Hẹn gặp lại!
            </div>
          </div>
        </div>
      </div>
    </>
  );
}