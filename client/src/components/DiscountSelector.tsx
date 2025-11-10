import React, { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { apiRequest } from '@/lib/queryClient';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Tag, X } from 'lucide-react';

export interface DiscountRule {
  discountId: number;
  name: string;
  description: string;
  type: number; // 0=PercentageItem, 1=PercentageTotal, 2=FixedAmountItem, 3=FixedAmountTotal
  value: number;
  isActive: boolean;
  categoryId?: number;
  productId?: number;
  minimumPurchaseAmount?: number;
}

interface CartItem {
  productId: number;
  quantity: number;
  totalPrice: number;
  categoryId?: number;
}

interface DiscountSelectorProps {
  cart: CartItem[];
  subtotal: number;
  onDiscountSelect: (discount: DiscountRule | null, discountAmount: number) => void;
  selectedDiscount: DiscountRule | null;
  manualDiscountAmount?: number;
}

export function DiscountSelector({ cart, subtotal, onDiscountSelect, selectedDiscount, manualDiscountAmount = 0 }: DiscountSelectorProps) {
  const [showDiscountSelect, setShowDiscountSelect] = useState(false);

  // Fetch available discount rules
  const { data: discountRules = [], isLoading } = useQuery<DiscountRule[]>({
    queryKey: ['/api/discounts'],
    queryFn: async () => {
      const response = await apiRequest('/api/discounts', { method: 'GET' });
      const data = typeof response === 'string' ? JSON.parse(response) : response;
      return Array.isArray(data) ? data : [];
    },
    enabled: showDiscountSelect
  });

  // Calculate discount amount for a specific rule
  const calculateDiscountAmount = (rule: DiscountRule): number => {
    if (!rule.isActive) return 0;

    // Check minimum purchase amount
    if (rule.minimumPurchaseAmount && subtotal < rule.minimumPurchaseAmount) {
      return 0;
    }

    let discountAmount = 0;

    switch (rule.type) {
      case 1: // PercentageTotal
        discountAmount = subtotal * rule.value; // value is already in decimal (0.03 = 3%)
        break;
      
      case 3: // FixedAmountTotal  
        discountAmount = Math.min(rule.value, subtotal);
        break;
      
      case 0: // PercentageItem
        // Apply to specific category or product
        if (rule.categoryId) {
          const categoryItems = cart.filter(item => item.categoryId === rule.categoryId);
          const categoryTotal = categoryItems.reduce((sum, item) => sum + item.totalPrice, 0);
          discountAmount = categoryTotal * rule.value;
        } else if (rule.productId) {
          const productItems = cart.filter(item => item.productId === rule.productId);
          const productTotal = productItems.reduce((sum, item) => sum + item.totalPrice, 0);
          discountAmount = productTotal * rule.value;
        } else {
          // Apply to all items if no specific category/product
          discountAmount = subtotal * rule.value;
        }
        break;
      
      case 2: // FixedAmountItem
        // Apply to specific category or product
        if (rule.categoryId) {
          const categoryItems = cart.filter(item => item.categoryId === rule.categoryId);
          if (categoryItems.length > 0) {
            discountAmount = Math.min(rule.value, categoryItems.reduce((sum, item) => sum + item.totalPrice, 0));
          }
        } else if (rule.productId) {
          const productItems = cart.filter(item => item.productId === rule.productId);
          if (productItems.length > 0) {
            discountAmount = Math.min(rule.value, productItems.reduce((sum, item) => sum + item.totalPrice, 0));
          }
        } else {
          // Apply to total if no specific category/product
          discountAmount = Math.min(rule.value, subtotal);
        }
        break;
    }

    return Math.max(0, discountAmount);
  };

  // Filter applicable discount rules
  const applicableDiscounts = discountRules.filter(rule => {
    const discountAmount = calculateDiscountAmount(rule);
    return discountAmount > 0;
  });

  // Handle discount selection
  const handleDiscountSelect = (discountId: string) => {
    if (discountId === "none") {
      onDiscountSelect(null, 0);
      return;
    }

    const rule = discountRules.find(r => r.discountId.toString() === discountId);
    if (rule) {
      const discountAmount = calculateDiscountAmount(rule);
      onDiscountSelect(rule, discountAmount);
    }
  };

  // Clear selected discount
  const clearDiscount = () => {
    onDiscountSelect(null, 0);
  };

  const formatDiscountType = (type: number) => {
    switch (type) {
      case 1: return 'Giảm % tổng bill';
      case 3: return 'Giảm tiền tổng bill';
      case 0: return 'Giảm % theo mặt hàng';
      case 2: return 'Giảm tiền theo mặt hàng';
      default: return `Loại ${type}`;
    }
  };

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowDiscountSelect(!showDiscountSelect)}
          className="text-xs flex items-center gap-1"
        >
          <Tag className="w-3 h-3" />
          {showDiscountSelect ? 'Ẩn' : 'Chọn'} giảm giá
        </Button>
        
        {selectedDiscount && (
          <Button
            variant="ghost"
            size="sm"
            onClick={clearDiscount}
            className="text-xs text-red-500 hover:text-red-700"
          >
            <X className="w-3 h-3" />
          </Button>
        )}
      </div>

      {showDiscountSelect && (
        <div className="space-y-2 p-3 border rounded-lg bg-gray-50">
          <div className="text-xs text-gray-600 mb-2">
            Chọn loại giảm giá đã thiết lập:
          </div>
          
          {manualDiscountAmount > 0 && (
            <div className="text-xs text-orange-600 mb-2 p-2 bg-orange-50 border border-orange-200 rounded">
              ⚠️ Đã áp dụng giảm giá thủ công. Chỉ có thể chọn thêm giảm giá theo mặt hàng.
            </div>
          )}
          
          {isLoading ? (
            <div className="text-xs text-gray-500">Đang tải...</div>
          ) : applicableDiscounts.length > 0 ? (
            <Select 
              value={selectedDiscount?.discountId.toString() || "none"} 
              onValueChange={handleDiscountSelect}
            >
              <SelectTrigger className="text-xs h-8">
                <SelectValue placeholder="Chọn giảm giá..." />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none" className="text-xs">
                  Không áp dụng giảm giá
                </SelectItem>
                {applicableDiscounts.map((rule) => {
                  const discountAmount = calculateDiscountAmount(rule);
                  return (
                    <SelectItem key={rule.discountId} value={rule.discountId.toString()} className="text-xs">
                      <div className="flex flex-col gap-1">
                        <div className="font-medium">{rule.name}</div>
                        <div className="text-gray-500">
                          {formatDiscountType(rule.type)} - 
                          {(rule.type === 0 || rule.type === 1) ? ` ${(rule.value * 100).toFixed(1)}%` : ` ${rule.value.toLocaleString('vi-VN')}₫`}
                          {discountAmount > 0 && (
                            <span className="text-green-600 ml-1">
                              (-{discountAmount.toLocaleString('vi-VN')}₫)
                            </span>
                          )}
                        </div>
                        {rule.minimumPurchaseAmount && (
                          <div className="text-xs text-orange-600">
                            Tối thiểu: {rule.minimumPurchaseAmount.toLocaleString('vi-VN')}₫
                          </div>
                        )}
                      </div>
                    </SelectItem>
                  );
                })}
              </SelectContent>
            </Select>
          ) : (
            <div className="text-xs text-gray-500">
              Không có giảm giá nào áp dụng được cho đơn hàng này
            </div>
          )}
        </div>
      )}

      {/* Display selected discount */}
      {selectedDiscount && (
        <div className="flex items-center justify-between text-sm p-2 bg-green-50 border border-green-200 rounded">
          <div className="flex items-center gap-2">
            <Badge variant="secondary" className="text-xs">
              {formatDiscountType(selectedDiscount.type)}
            </Badge>
            <span className="text-green-700 font-medium">{selectedDiscount.name}</span>
          </div>
          <span className="text-green-600 font-semibold">
            -{calculateDiscountAmount(selectedDiscount).toLocaleString('vi-VN')}₫
          </span>
        </div>
      )}
    </div>
  );
}