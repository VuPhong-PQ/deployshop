import { getVietQRPayload } from "vietqr";
import QRCode from "qrcode";

export interface VietQRParams {
  bank: string; // Mã ngân hàng (ex: VCB)
  account: string; // Số tài khoản
  amount?: number; // Số tiền
  description?: string; // Nội dung chuyển khoản
}

export async function generateVietQR({ bank, account, amount, description }: VietQRParams): Promise<string> {
  // Tạo payload VietQR chuẩn
  const payload = getVietQRPayload({
    bank,
    account,
    amount,
    template: "compact",
    addInfo: description || undefined
  });
  // Tạo QR code base64
  return await QRCode.toDataURL(payload, { width: 300 });
}
