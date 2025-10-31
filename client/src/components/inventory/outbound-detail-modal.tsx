import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Calendar, Package, Receipt, TrendingDown, User, Hash } from "lucide-react";
import { InventoryTransaction } from "@/types/backend-types";

interface OutboundDetailModalProps {
  transaction: InventoryTransaction | null;
  isOpen: boolean;
  onClose: () => void;
}

export function OutboundDetailModal({
  transaction,
  isOpen,
  onClose,
}: OutboundDetailModalProps) {
  if (!transaction || transaction.type !== "OUT") {
    return null;
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', { 
      style: 'currency', 
      currency: 'VND' 
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('vi-VN');
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <TrendingDown className="w-5 h-5 text-red-600" />
            Chi tiết xuất kho - Đơn hàng bán ra
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          {/* Thông tin giao dịch */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm text-gray-600">
                <Hash className="w-4 h-4" />
                <span>Mã giao dịch:</span>
              </div>
              <p className="font-semibold">#{transaction.transactionId}</p>
            </div>
            
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm text-gray-600">
                <Calendar className="w-4 h-4" />
                <span>Thời gian:</span>
              </div>
              <p className="font-semibold">{formatDate(transaction.transactionDate)}</p>
            </div>
          </div>

          <Separator />

          {/* Thông tin sản phẩm */}
          <div className="space-y-4">
            <h3 className="font-semibold flex items-center gap-2">
              <Package className="w-5 h-5" />
              Thông tin sản phẩm
            </h3>
            
            <div className="bg-gray-50 p-4 rounded-lg">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-sm text-gray-600">Tên sản phẩm:</p>
                  <p className="font-semibold text-lg">{transaction.productName}</p>
                  {transaction.productCode && (
                    <>
                      <p className="text-sm text-gray-600 mt-2">Mã sản phẩm:</p>
                      <p className="text-sm font-mono">{transaction.productCode}</p>
                    </>
                  )}
                </div>
                
                <div className="text-right">
                  <Badge variant="destructive" className="mb-2">
                    {transaction.typeName}
                  </Badge>
                  <div className="space-y-1">
                    <p className="text-sm text-gray-600">Số lượng bán:</p>
                    <p className="text-2xl font-bold text-red-600">
                      -{Math.abs(transaction.quantity)}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <Separator />

          {/* Thông tin giá và tiền */}
          <div className="space-y-4">
            <h3 className="font-semibold flex items-center gap-2">
              <Receipt className="w-5 h-5" />
              Thông tin bán hàng
            </h3>
            
            <div className="grid grid-cols-3 gap-4">
              <div className="text-center p-3 bg-blue-50 rounded-lg">
                <p className="text-sm text-gray-600">Giá bán / đơn vị</p>
                <p className="font-bold text-lg text-blue-600">
                  {formatCurrency(transaction.unitPrice)}
                </p>
              </div>
              
              <div className="text-center p-3 bg-orange-50 rounded-lg">
                <p className="text-sm text-gray-600">Số lượng</p>
                <p className="font-bold text-lg text-orange-600">
                  {Math.abs(transaction.quantity)}
                </p>
              </div>
              
              <div className="text-center p-3 bg-green-50 rounded-lg">
                <p className="text-sm text-gray-600">Tổng tiền</p>
                <p className="font-bold text-lg text-green-600">
                  {formatCurrency(Math.abs(transaction.totalValue))}
                </p>
              </div>
            </div>
          </div>

          <Separator />

          {/* Thông tin tồn kho */}
          <div className="space-y-4">
            <h3 className="font-semibold">Thay đổi tồn kho</h3>
            
            <div className="flex items-center justify-center gap-4 p-4 bg-gray-50 rounded-lg">
              <div className="text-center">
                <p className="text-sm text-gray-600">Trước khi bán</p>
                <p className="font-bold text-lg">{transaction.stockBefore}</p>
              </div>
              
              <div className="text-2xl text-gray-400">→</div>
              
              <div className="text-center">
                <p className="text-sm text-gray-600">Sau khi bán</p>
                <p className="font-bold text-lg">{transaction.stockAfter}</p>
              </div>
            </div>
          </div>

          {/* Thông tin nhân viên và đơn hàng */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm text-gray-600">
                <User className="w-4 h-4" />
                <span>Nhân viên bán:</span>
              </div>
              <p className="font-semibold">{transaction.staffName}</p>
            </div>
            
            {transaction.orderId && (
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-sm text-gray-600">
                  <Receipt className="w-4 h-4" />
                  <span>Mã đơn hàng:</span>
                </div>
                <p className="font-semibold">#{transaction.orderId}</p>
              </div>
            )}
          </div>

          {/* Ghi chú nếu có */}
          {(transaction.reason || transaction.notes) && (
            <>
              <Separator />
              <div className="space-y-2">
                <h3 className="font-semibold">Ghi chú</h3>
                {transaction.reason && (
                  <p><span className="font-medium">Lý do:</span> {transaction.reason}</p>
                )}
                {transaction.notes && (
                  <p><span className="font-medium">Chi tiết:</span> {transaction.notes}</p>
                )}
              </div>
            </>
          )}

          {/* Buttons */}
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="outline" onClick={onClose}>
              Đóng
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}