// Backend API response types matching C# models
export interface OrderItem {
  orderItemId: number;
  orderId: number;
  productId: number;
  productName: string | null;
  quantity: number;
  price: number;
  totalPrice: number;
}

// Inventory types
export interface InventoryTransaction {
  transactionId: number;
  productId: number;
  productName: string;
  productCode: string;
  staffId: number;
  staffName: string;
  type: 'IN' | 'OUT';
  typeName: string;
  quantity: number;
  unitPrice: number;
  totalValue: number;
  reason: string;
  notes?: string;
  orderId?: number;
  supplierId?: number;
  supplierName?: string;
  referenceNumber?: string;
  transactionDate: string;
  createdAt: string;
  stockBefore: number;
  stockAfter: number;
}

export interface InventoryTransactionResponse {
  data: InventoryTransaction[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface InventorySummary {
  productId: number;
  productName: string;
  productCode: string;
  currentStock: number;
  totalInbound: number;
  totalOutbound: number;
  totalInboundValue: number;
  totalOutboundValue: number;
  lastTransaction?: string;
}

export interface CreateInboundTransactionDto {
  productId: number;
  quantity: number;
  unitPrice: number;
  reason: string;
  notes?: string;
  supplierId?: number;
  supplierName?: string;
  referenceNumber?: string;
  transactionDate?: string;
}

export interface CreateOutboundTransactionDto {
  productId: number;
  quantity: number;
  reason: string;
  notes?: string;
  orderId?: number;
  referenceNumber?: string;
  transactionDate?: string;
}

export interface Product {
  productId: number;
  name: string | null;
  barcode: string | null;
  categoryId: number | null;
  productGroupId: number | null;
  price: number;
  costPrice: number | null;
  stockQuantity: number;
  minStockLevel: number;
  unit: string | null;
  imageUrl: string | null;
  description: string | null;
  isFeatured: boolean; // Sản phẩm hay bán
}

export interface Customer {
  customerId: number;
  hoTen: string | null;
  soDienThoai: string | null;
  email: string | null;
  diaChi: string | null;
  hangKhachHang: string | null;
  // Mapped fields for frontend compatibility
  id?: string;
  name?: string;
  phone?: string;
  address?: string;
  customerType?: string;
}

export interface Order {
  orderId: number;
  customerId: number | null;
  orderDate: string;
  totalAmount: number;
  discountAmount: number | null;
  taxAmount: number | null;
  paymentMethod: string | null;
  status: string | null;
  notes: string | null;
}

export interface OrderItem {
  orderItemId: number;
  orderId: number;
  productId: number;
  productName: string | null;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}