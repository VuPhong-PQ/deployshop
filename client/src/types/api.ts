// Types for backend API responses
export interface ApiCustomer {
  customerId: number;
  hoTen: string | null;
  soDienThoai: string | null;
  email: string | null;
  diaChi: string | null;
  hangKhachHang: "Thuong" | "Premium" | "VIP" | "Platinum";
  storeId: number | null;
  tierId: number | null;
  loyaltyPoints: number;
  dateOfBirth: string | null; // ISO date string
  totalSpent: number;
  isActive: boolean;
  createdAt: string; // ISO date string
  updatedAt: string; // ISO date string
  customerTier?: CustomerTier;
}

export interface CustomerTier {
  tierId: number;
  tierName: string;
  minSpent: number;
  minPoints: number;
  pointsMultiplier: number;
  discountPercentage: number;
  description: string | null;
  tierColor: string;
  isActive: boolean;
  createdAt: string;
}

export interface LoyaltyConfig {
  loyaltyConfigId: number;
  isEnabled: boolean;
  pointsPerCurrency: number;
  minOrderAmountForPoints: number;
  maxPointsPerOrder: number | null;
  pointExpiryDays: number;
  allowPointRedemption: boolean;
  pointValue: number;
  maxRedemptionPercentage: number;
  happyHourEnabled: boolean;
  happyHourStartTime: string; // TimeSpan as string "HH:mm:ss"
  happyHourEndTime: string;
  happyHourMultiplier: number;
  weekendBonusEnabled: boolean;
  weekendMultiplier: number;
  birthdayBonusEnabled: boolean;
  birthdayMultiplier: number;
  birthdayValidDays: number;
  createdAt: string;
  updatedAt: string | null;
  createdBy: number | null;
}

export interface LoyaltyTransaction {
  transactionId: number;
  customerId: number;
  orderId: number | null;
  transactionType: "EARN" | "REDEEM" | "EXPIRE" | "ADJUST";
  points: number;
  pointsBalance: number;
  reason: string | null;
  expiryDate: string | null;
  processedAt: string;
  processedBy: number | null;
  customer?: ApiCustomer;
  order?: any; // Order type would be defined elsewhere
  processedByStaff?: any; // Staff type would be defined elsewhere
}

// Form types for frontend components
export interface CustomerFormData {
  name: string;
  phone: string;
  email?: string | null;
  address?: string | null;
  storeId: string;
  dateOfBirth?: Date | null;
  customerType?: "regular" | "premium" | "vip";
  loyaltyPoints?: number;
  totalSpent?: string;
  isActive?: boolean;
  hangKhachHang?: "Thuong" | "Premium" | "VIP" | "Platinum";
}

// Frontend Customer type (mapped from API)
export interface Customer {
  id: string;
  name: string;
  phone: string;
  email: string | null;
  address: string | null;
  customerType: "regular" | "premium" | "vip";
  loyaltyPoints: number;
  totalSpent: string;
  storeId: string;
  hangKhachHang: "Thuong" | "Premium" | "VIP" | "Platinum";
  dateOfBirth: string | null;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  // Add tier information
  tierId?: number | null;
  customerTier?: CustomerTier | null;
}

// API Store type
export interface ApiStore {
  storeId: number;
  name: string;
  address: string | null;
  phone: string | null;
  email: string | null;
  taxCode: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

// Order and Product types
export interface Order {
  orderId: number;
  orderNumber?: string;
  customerId?: number;
  storeId?: number;
  status: "pending" | "processing" | "completed" | "cancelled";
  totalAmount: string;
  subTotal?: string;
  taxAmount?: string;
  discountAmount?: string;
  paymentMethod?: "cash" | "card" | "other";
  createdAt: string;
  updatedAt?: string;
  items?: OrderItem[];
  customer?: ApiCustomer;
  store?: ApiStore;
}

export interface OrderItem {
  orderItemId: number;
  orderId: number;
  productId: number;
  quantity: number;
  price: string;
  totalPrice?: string;
  productName?: string;
  product?: Product;
}

export interface Product {
  productId: number;
  name: string;
  sku?: string;
  description?: string;
  price: string;
  cost?: string;
  stock?: number;
  imageUrl?: string;
  isActive?: boolean;
  categoryId?: number;
  storeId?: number;
  createdAt?: string;
  updatedAt?: string;
}

// Customer detail with orders and transactions
export interface CustomerDetailData {
  customerId: number;
  hoTen: string;
  soDienThoai: string;
  email?: string;
  diaChi?: string;
  hangKhachHang: string;
  loyaltyPoints: number;
  totalSpent: string;
  orders: Order[];
  loyaltyTransactions: LoyaltyTransaction[];
}