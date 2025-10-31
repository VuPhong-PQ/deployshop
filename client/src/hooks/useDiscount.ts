import { useQuery } from '@tanstack/react-query';

export interface Discount {
  id: number;
  name: string;
  description?: string;
  discountType: 'PercentageTotal' | 'FixedAmountItem' | 'FixedAmountTotal';
  value: number;
  minOrderAmount?: number;
  maxDiscountAmount?: number;
  isActive: boolean;
  startDate?: string;
  endDate?: string;
  usageLimit?: number;
  usageCount: number;
  applicableProductIds?: number[];
  applicableCategoryIds?: number[];
}

export interface DiscountCalculationRequest {
  discountId: number;
  orderItems: Array<{
    productId: number;
    quantity: number;
    price: number;
    totalPrice: number;
  }>;
  orderTotal: number;
}

export interface DiscountCalculationResponse {
  canApply: boolean;
  discountAmount: number;
  message?: string;
  finalTotal: number;
}

// Hook để lấy danh sách giảm giá có thể áp dụng
export const useAvailableDiscounts = () => {
  return useQuery({
    queryKey: ['discounts', 'available'],
    queryFn: async (): Promise<Discount[]> => {
      const response = await fetch('http://101.53.9.76:5273/api/discounts?status=active');
      if (!response.ok) {
        throw new Error('Failed to fetch available discounts');
      }
      return response.json();
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

// Hook để tính toán giảm giá
export const useDiscountCalculation = () => {
  const calculateDiscount = async (request: DiscountCalculationRequest): Promise<DiscountCalculationResponse> => {
    const response = await fetch('http://101.53.9.76:5273/api/order-discounts/calculate', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to calculate discount');
    }

    return response.json();
  };

  return { calculateDiscount };
};

// Hook để áp dụng giảm giá cho đơn hàng
export const useApplyDiscount = () => {
  const applyDiscount = async (orderId: number, discountId: number): Promise<void> => {
    const response = await fetch(`http://101.53.9.76:5273/api/orders/${orderId}/discounts/apply`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ discountId }),
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(error || 'Failed to apply discount');
    }
  };

  return { applyDiscount };
};

// Hook tích hợp quản lý giảm giá cho cart
export const useCartDiscount = (cart: Array<{ productId: number; quantity: number; price: number; totalPrice: number }>) => {
  const { data: availableDiscounts, isLoading } = useAvailableDiscounts();
  const { calculateDiscount } = useDiscountCalculation();

  const getApplicableDiscounts = () => {
    if (!availableDiscounts) return [];
    
    const now = new Date();
    return availableDiscounts.filter(discount => {
      // Kiểm tra active
      if (!discount.isActive) return false;
      
      // Kiểm tra thời gian
      if (discount.startDate && new Date(discount.startDate) > now) return false;
      if (discount.endDate && new Date(discount.endDate) < now) return false;
      
      // Kiểm tra usage limit
      if (discount.usageLimit && discount.usageCount >= discount.usageLimit) return false;
      
      return true;
    });
  };

  const calculateDiscountForCart = async (discountId: number, orderTotal: number) => {
    if (cart.length === 0) return null;

    const request: DiscountCalculationRequest = {
      discountId,
      orderItems: cart.map(item => ({
        productId: item.productId,
        quantity: item.quantity,
        price: item.price,
        totalPrice: item.totalPrice,
      })),
      orderTotal,
    };

    try {
      return await calculateDiscount(request);
    } catch (error) {
      console.error('Error calculating discount:', error);
      return null;
    }
  };

  return {
    availableDiscounts: getApplicableDiscounts(),
    isLoading,
    calculateDiscountForCart,
  };
};