export interface DiscountSummaryReport {
  totalDiscountAmount: number;
  totalDiscountApplications: number;
  uniqueOrdersWithDiscount: number;
  averageDiscountPerOrder: number;
  discountsByType: DiscountTypeReport[];
  topDiscounts: TopDiscountReport[];
  dailyDiscounts: DailyDiscountReport[];
}

export interface DiscountTypeReport {
  type: number;
  typeName: string;
  count: number;
  totalAmount: number;
  averageAmount: number;
}

export interface TopDiscountReport {
  discountId: number;
  discountName: string;
  usageCount: number;
  totalAmount: number;
  averageAmount: number;
}

export interface DailyDiscountReport {
  date: string;
  count: number;
  totalAmount: number;
  uniqueOrders: number;
}

export interface DiscountOrdersResponse {
  orders: DiscountedOrderSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DiscountedOrderSummary {
  orderId: number;
  orderNumber: string;
  customerName: string;
  orderTotal: number;
  totalDiscountAmount: number;
  discountCount: number;
  orderDate: string;
  paymentStatus: string;
  paymentMethod: string;
  discountDetails: OrderDiscountDetail[];
}

export interface OrderDiscountDetail {
  discountName: string;
  discountType: number;
  discountTypeName: string;
  discountValue: number;
  discountAmount: number;
  appliedAt: string;
}