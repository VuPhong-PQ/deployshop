import React from "react";

interface VietQRModalProps {
  open: boolean;
  onClose: () => void;
  qrImage?: string;
  bankName?: string;
  bankAccount?: string;
  accountHolder?: string;
  amount?: string;
  description?: string;
}

export const VietQRModal: React.FC<VietQRModalProps> = ({ open, onClose, qrImage, bankName, bankAccount, accountHolder, amount, description }) => {
  if (!open) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-40">
      <div className="bg-white rounded-lg shadow-lg p-6 w-full max-w-sm relative">
        <button className="absolute top-2 right-2 text-gray-500 hover:text-black" onClick={onClose}>&times;</button>
        <h2 className="text-lg font-semibold mb-2">Mã QR thanh toán VietQR</h2>
        {qrImage ? (
          <img src={qrImage} alt="VietQR" className="mx-auto mb-4 w-48 h-48 object-contain border" />
        ) : (
          <div className="w-48 h-48 flex items-center justify-center bg-gray-100 mb-4">Không có QR</div>
        )}
        <div className="mb-2 print:text-black"><b>Ngân hàng:</b> {bankName || "-"}</div>
        <div className="mb-2 print:text-black"><b>Số tài khoản:</b> {bankAccount || "-"}</div>
        <div className="mb-2 print:text-black"><b>Chủ tài khoản:</b> {accountHolder || "-"}</div>
        {amount && <div className="mb-2 print:text-black"><b>Số tiền:</b> {amount}</div>}
        {description && <div className="mb-2 print:text-black"><b>Nội dung:</b> {description}</div>}
      </div>
    </div>
  );
};
