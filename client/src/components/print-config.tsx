import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiRequest } from "@/lib/queryClient";

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

interface PrintStyleProps {
  children: React.ReactNode;
}

export function PrintStyleProvider({ children }: PrintStyleProps) {
  const [printStyles, setPrintStyles] = useState<string>("");

  // Fetch print configuration
  const { data: printConfig } = useQuery<PrintConfig>({
    queryKey: ['/api/printconfig'],
    queryFn: () => apiRequest('/api/printconfig', { method: 'GET' }),
  });

  useEffect(() => {
    if (printConfig?.printerName) {
      // Generate styles based on printer type
      const styles = generatePrintStyles(printConfig.printerName);
      setPrintStyles(styles);
    }
  }, [printConfig]);

  return (
    <>
      {printStyles && (
        <style dangerouslySetInnerHTML={{ __html: printStyles }} />
      )}
      {children}
    </>
  );
}

function generatePrintStyles(printerName: string): string {
  const isPOS = printerName.includes("POS") || printerName.includes("80");
  
  if (isPOS) {
    // POS thermal printer styles (80mm)
    return `
      @media print {
        @page {
          size: 80mm auto;
          margin: 0;
        }
        body {
          margin: 0;
          padding: 0;
          font-family: "Courier New", monospace;
          font-size: 9px;
          line-height: 1.2;
          width: 80mm;
        }
        .print-area {
          width: 80mm;
          padding: 2mm;
          font-size: 9px;
        }
        .pos-format {
          display: block !important;
        }
        .a4-format {
          display: none !important;
        }
        .store-header {
          text-align: center;
          font-size: 10px;
          margin-bottom: 3mm;
          border-bottom: 1px dashed #000;
          padding-bottom: 2mm;
        }
        .order-info {
          font-size: 8px;
          margin-bottom: 3mm;
          border-bottom: 1px dashed #666;
          padding-bottom: 2mm;
        }
        .product-item {
          font-size: 8px;
          margin-bottom: 2mm;
          border-bottom: 1px dotted #666;
          padding-bottom: 1mm;
        }
        .product-name {
          font-weight: bold;
          margin-bottom: 1mm;
        }
        .product-details {
          display: flex;
          justify-content: space-between;
          font-size: 7px;
        }
        .totals {
          border-top: 1px dashed #000;
          padding-top: 2mm;
          margin-top: 3mm;
        }
        .total-line {
          display: flex;
          justify-content: space-between;
          font-size: 8px;
          margin-bottom: 1mm;
        }
        .grand-total {
          display: flex;
          justify-content: space-between;
          font-weight: bold;
          font-size: 10px;
          border-top: 2px solid #000;
          border-bottom: 2px solid #000;
          padding: 2mm 0;
          margin: 2mm 0;
        }
        .footer {
          text-align: center;
          font-size: 7px;
          margin-top: 3mm;
          border-top: 1px dashed #000;
          padding-top: 2mm;
        }
      }
    `;
  } else {
    // Standard A4 printer styles
    return `
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
        .print-area {
          width: 100%;
          max-width: 210mm;
          padding: 0;
        }
        .pos-format {
          display: none !important;
        }
        .a4-format {
          display: block !important;
        }
        .store-header {
          text-align: center;
          font-size: 14px;
          margin-bottom: 5mm;
          border-bottom: 2px solid #000;
          padding-bottom: 3mm;
        }
        .order-info {
          font-size: 11px;
          margin-bottom: 5mm;
        }
        .products-table {
          width: 100%;
          border-collapse: collapse;
          font-size: 11px;
          margin-bottom: 5mm;
        }
        .products-table th,
        .products-table td {
          border: 1px solid #000;
          padding: 3mm;
          text-align: left;
        }
        .products-table th {
          background-color: #f0f0f0;
          font-weight: bold;
        }
        .totals {
          text-align: right;
          font-size: 12px;
        }
        .total-line {
          margin-bottom: 2mm;
        }
        .grand-total {
          font-weight: bold;
          font-size: 14px;
          border-top: 2px solid #000;
          padding-top: 2mm;
          margin-top: 3mm;
        }
        .footer {
          text-align: center;
          font-size: 10px;
          margin-top: 10mm;
        }
      }
    `;
  }
}