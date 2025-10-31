import { 
  users, products, customers, orders, orderItems, categories, 
  inventoryMovements, promotions, stores,
  type User, type InsertUser, type Product, type InsertProduct,
  type Customer, type InsertCustomer, type Order, type InsertOrder,
  type OrderItem, type InsertOrderItem, type Category,
  type InventoryMovement, type InsertInventoryMovement
} from "@shared/schema";
import { db } from "./db";
import { eq, desc, sql, and, lte, gte } from "drizzle-orm";

// Storage interface
export interface IStorage {
  // Users
  getUser(id: string): Promise<User | undefined>;
  getUserByUsername(username: string): Promise<User | undefined>;
  createUser(insertUser: InsertUser): Promise<User>;
  
  // Dashboard
  getDashboardMetrics(storeId: string): Promise<any>;
  getRevenueChartData(storeId: string, days: number): Promise<any[]>;
  getTopProducts(storeId: string, limit: number): Promise<any[]>;
  getRecentOrders(storeId: string, limit: number): Promise<any[]>;
  getLowStockProducts(storeId: string): Promise<any[]>;
  
  // Products
  getProducts(storeId: string): Promise<Product[]>;
  createProduct(insertProduct: InsertProduct): Promise<Product>;
  updateProduct(id: string, updates: Partial<Product>): Promise<Product | undefined>;
  deleteProduct(id: string): Promise<void>;
  
  // Customers
  getCustomers(storeId: string): Promise<Customer[]>;
  createCustomer(insertCustomer: InsertCustomer): Promise<Customer>;
  
  // Orders
  getOrders(storeId: string): Promise<Order[]>;
  createOrder(insertOrder: InsertOrder, items: InsertOrderItem[]): Promise<Order>;
  
  // Categories
  getCategories(storeId: string): Promise<Category[]>;
  
  // Inventory
  getInventoryMovements(storeId: string): Promise<InventoryMovement[]>;
  adjustInventory(productId: string, quantity: number, reason: string, userId: string, storeId: string): Promise<InventoryMovement>;
  
  // Reports
  getSalesReport(storeId: string, startDate: string, endDate: string): Promise<any>;
}

export class MemStorage implements IStorage {
  async getUser(id: string): Promise<User | undefined> {
    const [user] = await db.select().from(users).where(eq(users.id, id));
    return user || undefined;
  }

  async getUserByUsername(username: string): Promise<User | undefined> {
    const [user] = await db.select().from(users).where(eq(users.username, username));
    return user || undefined;
  }

  async createUser(insertUser: InsertUser): Promise<User> {
    const [user] = await db
      .insert(users)
      .values(insertUser)
      .returning();
    return user;
  }
  
  // Dashboard methods
  async getDashboardMetrics(storeId: string): Promise<any> {
    // Mock data for now - in real implementation would query database
    const lowStockProducts = await this.getLowStockProducts(storeId);
    
    return {
      todayRevenue: "15.750.000₫",
      todayGrowth: "+12.5%",
      monthRevenue: "480.250.000₫",
      monthGrowth: "+8.3%",
      ordersCount: 2847,
      ordersGrowth: "+15.2%",
      newCustomers: 1234,
      customersGrowth: "+5.7%",
      lowStockItems: lowStockProducts.length
    };
  }

  async getRevenueChartData(storeId: string, days: number): Promise<any[]> {
    // Mock revenue chart data
    const data = [];
    for (let i = days - 1; i >= 0; i--) {
      const date = new Date();
      date.setDate(date.getDate() - i);
      data.push({
        date: date.toISOString().split('T')[0],
        revenue: Math.floor(Math.random() * 5000000) + 1000000,
        orders: Math.floor(Math.random() * 50) + 10
      });
    }
    return data;
  }

  async getTopProducts(storeId: string, limit: number): Promise<any[]> {
    // Mock top products data
    return [
      { name: "iPhone 15 Pro", sales: 45, revenue: "900.000.000₫" },
      { name: "MacBook Air M2", sales: 23, revenue: "690.000.000₫" },
      { name: "AirPods Pro", sales: 67, revenue: "402.000.000₫" },
      { name: "iPad Air", sales: 34, revenue: "340.000.000₫" },
      { name: "Apple Watch", sales: 28, revenue: "280.000.000₫" }
    ].slice(0, limit);
  }

  async getRecentOrders(storeId: string, limit: number): Promise<any[]> {
    // Mock recent orders data
    return [
      {
        id: "1",
        orderNumber: "ORD-001",
        customer: "Nguyễn Văn An",
        total: "15.000.000₫",
        status: "completed",
        createdAt: new Date()
      },
      {
        id: "2", 
        orderNumber: "ORD-002",
        customer: "Trần Thị Bình",
        total: "8.500.000₫",
        status: "processing",
        createdAt: new Date(Date.now() - 3600000)
      }
    ].slice(0, limit);
  }

  async getLowStockProducts(storeId: string): Promise<any[]> {
    // Mock low stock products
    return [
      { id: "1", name: "iPhone 15 Pro", current: 3, minimum: 5, status: "low" },
      { id: "2", name: "AirPods Pro", current: 1, minimum: 10, status: "critical" }
    ];
  }

  // Products
  async getProducts(storeId: string): Promise<Product[]> {
    // Mock products for demo
    return [
        {
          id: "550e8400-e29b-41d4-a716-446655440010",
          name: "iPhone 15 Pro",
          description: "Smartphone cao cấp với chip A17 Pro",
          sku: "IP15P-128GB",
          barcode: "123456789",
          price: "25000000",
          costPrice: "22000000", 
          image: "https://images.unsplash.com/photo-1592750475338-74b7b21085ab?w=200&h=200&fit=crop",
          categoryId: "550e8400-e29b-41d4-a716-446655440003",
          storeId: storeId,
          isActive: true,
          stockQuantity: 15,
          minStockLevel: 10,
          unit: "chiếc",
          createdAt: new Date(),
          updatedAt: new Date()
        },
        {
          id: "550e8400-e29b-41d4-a716-446655440011",
          name: "AirPods Pro 2",
          description: "Tai nghe không dây chống ồn",
          sku: "APP2-WHITE",
          barcode: "987654321",
          price: "6000000",
          costPrice: "5200000",
          image: "https://images.unsplash.com/photo-1606220945770-b5b6c2c55bf1?w=200&h=200&fit=crop",
          categoryId: "550e8400-e29b-41d4-a716-446655440005",
          storeId: storeId,
          isActive: true,
          stockQuantity: 25,
          minStockLevel: 15,
          unit: "cặp",
          createdAt: new Date(),
          updatedAt: new Date()
        },
        {
          id: "550e8400-e29b-41d4-a716-446655440013",
          name: "iPad Air 11\"",
          description: "Máy tính bảng với chip M2",
          sku: "IPA11-256GB",
          barcode: "135792468",
          price: "15000000",
          costPrice: "13000000",
          image: "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=200&h=200&fit=crop",
          categoryId: "550e8400-e29b-41d4-a716-446655440003",
          storeId: storeId,
          isActive: true,
          stockQuantity: 3,
          minStockLevel: 5,
          unit: "chiếc",
          createdAt: new Date(),
          updatedAt: new Date()
        },
        {
          id: "550e8400-e29b-41d4-a716-446655440012",
          name: "MacBook Air M2",
          description: "Laptop Apple với chip M2",
          sku: "MBA-M2-256GB",
          barcode: "456789123",
          price: "30000000",
          costPrice: "27000000",
          image: "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=200&h=200&fit=crop",
          categoryId: "550e8400-e29b-41d4-a716-446655440004",
          storeId: storeId,
          isActive: true,
          stockQuantity: 8,
          minStockLevel: 3,
          unit: "chiếc",
          createdAt: new Date(),
          updatedAt: new Date()
        }
    ];
  }

  async createProduct(insertProduct: InsertProduct): Promise<Product> {
    const [product] = await db
      .insert(products)
      .values(insertProduct)
      .returning();
    return product;
  }

  async updateProduct(id: string, updates: Partial<Product>): Promise<Product | undefined> {
    const [product] = await db
      .update(products)
      .set(updates)
      .where(eq(products.id, id))
      .returning();
    return product || undefined;
  }

  async deleteProduct(id: string): Promise<void> {
    await db.delete(products).where(eq(products.id, id));
  }

  // Customers
  async getCustomers(storeId: string): Promise<Customer[]> {
    // Mock customers for demo
    return [
      {
        id: "1",
        name: "Nguyễn Văn An",
        email: "nguyenvanan@email.com",
        phone: "0901234567",
        address: "123 Lê Lợi, Q1, TP.HCM",
        dateOfBirth: new Date("1990-05-15"),
        customerType: "vip",
        loyaltyPoints: 1500,
        totalSpent: "5000000",
        storeId: storeId,
        isActive: true,
        createdAt: new Date(),
        updatedAt: new Date()
      },
      {
        id: "2",
        name: "Trần Thị Bình",
        email: "tranthibinh@email.com", 
        phone: "0912345678",
        address: "456 Nguyễn Huệ, Q1, TP.HCM",
        dateOfBirth: new Date("1985-12-20"),
        customerType: "premium",
        loyaltyPoints: 800,
        totalSpent: "3200000",
        storeId: storeId,
        isActive: true,
        createdAt: new Date(),
        updatedAt: new Date()
      },
      {
        id: "3",
        name: "Lê Văn Cường",
        email: "levancuong@email.com",
        phone: "0923456789", 
        address: "789 Trần Hưng Đạo, Q5, TP.HCM",
        dateOfBirth: new Date("1992-03-10"),
        customerType: "regular",
        loyaltyPoints: 250,
        totalSpent: "1200000",
        storeId: storeId,
        isActive: true,
        createdAt: new Date(),
        updatedAt: new Date()
      }
    ];
  }

  async createCustomer(insertCustomer: InsertCustomer): Promise<Customer> {
    const [customer] = await db
      .insert(customers)
      .values(insertCustomer)
      .returning();
    return customer;
  }

  // Orders
  async getOrders(storeId: string): Promise<Order[]> {
    // Mock orders for demo
    return [
      {
        id: "550e8400-e29b-41d4-a716-446655440020",
        orderNumber: "ORD-2025-001",
        customerId: "1",
        cashierId: "550e8400-e29b-41d4-a716-446655440001",
        storeId: storeId,
        subtotal: "25000000",
        taxAmount: "2500000",
        discountAmount: "1000000",
        total: "26500000",
        paymentMethod: "cash",
        paymentStatus: "paid",
        status: "completed",
        notes: "Khách hàng VIP - giảm giá đặc biệt",
        metadata: { receiptUrl: null },
        createdAt: new Date(),
        updatedAt: new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440021", 
        orderNumber: "ORD-2025-002",
        customerId: "2",
        cashierId: "550e8400-e29b-41d4-a716-446655440001",
        storeId: storeId,
        subtotal: "12000000",
        taxAmount: "1200000",
        discountAmount: "0",
        total: "13200000",
        paymentMethod: "card",
        paymentStatus: "paid",
        status: "completed",
        notes: "",
        metadata: { receiptUrl: null },
        createdAt: new Date(),
        updatedAt: new Date()
      }
    ];
  }

  async createOrder(insertOrder: InsertOrder, items: InsertOrderItem[]): Promise<Order> {
    const [order] = await db
      .insert(orders)
      .values(insertOrder)
      .returning();
      
    if (items && items.length > 0) {
      await db.insert(orderItems).values(
        items.map(item => ({ ...item, orderId: order.id }))
      );
    }
    
    return order;
  }

  // Categories
  async getCategories(storeId: string): Promise<Category[]> {
    // Mock categories for demo
    return [
      {
        id: "550e8400-e29b-41d4-a716-446655440003",
        name: "Điện thoại & Tablet", 
        description: "Smartphone, máy tính bảng",
        image: null,
        storeId: storeId,
        isActive: true,
        createdAt: new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440004",
        name: "Laptop & PC",
        description: "Máy tính xách tay và để bàn",
        image: null,
        storeId: storeId, 
        isActive: true,
        createdAt: new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440005",
        name: "Phụ kiện",
        description: "Tai nghe, sạc, ốp lưng",
        image: null,
        storeId: storeId,
        isActive: true,
        createdAt: new Date()
      }
    ];
  }

  // Inventory
  async getInventoryMovements(storeId: string): Promise<InventoryMovement[]> {
    // Mock inventory movements for demo
    return [
      {
        id: "550e8400-e29b-41d4-a716-446655440030",
        productId: "550e8400-e29b-41d4-a716-446655440010",
        quantity: 20,
        type: "in",
        reason: "Nhập hàng từ nhà cung cấp",
        previousStock: 0,
        newStock: 20,
        userId: "550e8400-e29b-41d4-a716-446655440001",
        storeId: storeId,
        createdAt: new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440031", 
        productId: "550e8400-e29b-41d4-a716-446655440011",
        quantity: -2,
        type: "out",
        reason: "Bán hàng",
        previousStock: 27,
        newStock: 25,
        userId: "550e8400-e29b-41d4-a716-446655440001",
        storeId: storeId,
        createdAt: new Date()
      }
    ];
  }

  async adjustInventory(productId: string, quantity: number, reason: string, userId: string, storeId: string): Promise<InventoryMovement> {
    // Get current product stock
    const [product] = await db.select().from(products).where(eq(products.id, productId));
    const previousStock = product?.stockQuantity || 0;
    const newStock = previousStock + quantity;
    
    // Update product stock
    await db
      .update(products)
      .set({ stockQuantity: newStock })
      .where(eq(products.id, productId));
    
    // Create inventory movement record
    const [movement] = await db
      .insert(inventoryMovements)
      .values({
        productId,
        quantity,
        type: quantity > 0 ? "in" : "out",
        reason,
        previousStock,
        newStock,
        userId,
        storeId
      })
      .returning();
    return movement;
  }

  // Reports
  async getSalesSummary(storeId: string, startDate?: string, endDate?: string): Promise<any> {
    const orders = await this.db.select({
      id: ordersTable.id,
      total: ordersTable.total,
      createdAt: ordersTable.createdAt
    })
    .from(ordersTable);
    
    const totalRevenue = orders.reduce((sum, order) => sum + parseFloat(order.total), 0);
    const totalOrders = orders.length;
    const averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
    
    return {
      totalRevenue: totalRevenue.toLocaleString('vi-VN') + "₫",
      totalOrders,
      averageOrderValue: averageOrderValue.toLocaleString('vi-VN') + "₫",
      period: `${startDate || 'Tất cả'} - ${endDate || 'hiện tại'}`
    };
  }

  async getProductPerformance(storeId: string, startDate?: string, endDate?: string): Promise<any> {
    // Get top performing products from order items
    const topProducts = [
      { name: "iPhone 15 Pro", totalSold: 15, revenue: "375.000.000₫", profit: "45.000.000₫" },
      { name: "MacBook Air M2", totalSold: 8, revenue: "240.000.000₫", profit: "24.000.000₫" },
      { name: "AirPods Pro 2", totalSold: 25, revenue: "150.000.000₫", profit: "20.000.000₫" },
      { name: "iPad Air 11\"", totalSold: 3, revenue: "45.000.000₫", profit: "6.000.000₫" }
    ];

    return {
      topProducts,
      totalProductsSold: topProducts.reduce((sum, p) => sum + p.totalSold, 0),
      mostPopularProduct: topProducts[0].name,
      totalCategories: 4
    };
  }

  async getCustomerAnalytics(storeId: string, startDate?: string, endDate?: string): Promise<any> {
    const customers = await this.db.select().from(customersTable);
    
    return {
      totalCustomers: customers.length,
      newCustomers: 3,
      returningCustomers: customers.length - 3,
      averageOrdersPerCustomer: "2.4",
      topCustomers: [
        { name: "Nguyễn Văn An", orders: 8, totalSpent: "25.000.000₫" },
        { name: "Trần Thị Bích", orders: 6, totalSpent: "18.500.000₫" },
        { name: "Lê Hoàng Nam", orders: 4, totalSpent: "12.300.000₫" }
      ]
    };
  }

  async getProfitAnalysis(storeId: string, startDate?: string, endDate?: string): Promise<any> {
    return {
      totalProfit: "95.000.000₫",
      profitMargin: "18.5%",
      costOfGoodsSold: "420.000.000₫",
      grossProfit: "115.000.000₫",
      operatingExpenses: "20.000.000₫",
      monthlyTrend: [
        { month: "07/2025", profit: "78.000.000₫", margin: "16.2%" },
        { month: "08/2025", profit: "95.000.000₫", margin: "18.5%" }
      ],
      topProfitableProducts: [
        { name: "iPhone 15 Pro", profit: "45.000.000₫", margin: "12%" },
        { name: "MacBook Air M2", profit: "24.000.000₫", margin: "10%" },
        { name: "AirPods Pro 2", profit: "20.000.000₫", margin: "13.3%" }
      ]
    };
  }

  async getSalesReport(storeId: string, startDate: string, endDate: string): Promise<any> {
    // Mock sales report data
    return {
      totalRevenue: "125.500.000₫",
      totalOrders: 45,
      averageOrderValue: "2.788.889₫",
      topProducts: [
        { name: "iPhone 15 Pro", quantity: 12, revenue: "300.000.000₫" },
        { name: "MacBook Air M2", revenue: "180.000.000₫" },
        { name: "AirPods Pro", quantity: 25, revenue: "150.000.000₫" }
      ],
      dailySales: [
        { date: "2025-08-20", revenue: "8.500.000₫", orders: 3 },
        { date: "2025-08-21", revenue: "12.200.000₫", orders: 5 },
        { date: "2025-08-22", revenue: "15.800.000₫", orders: 7 }
      ]
    };
  }

  // Staff Management
  async getStaff(): Promise<any[]> {
    const users = await this.db.select().from(usersTable);
    return users.map(user => ({
      ...user,
      role: user.role || "staff",
      status: user.isActive ? "active" : "inactive",
      workSchedule: user.workSchedule || "9:00-18:00",
      permissions: this.getRolePermissions(user.role || "staff")
    }));
  }

  async createStaff(staffData: any): Promise<any> {
    const staff = await this.db.insert(usersTable).values({
      id: `staff-${Date.now()}`,
      ...staffData,
      createdAt: new Date(),
      updatedAt: new Date()
    }).returning();
    return staff[0];
  }

  async updateStaff(id: string, updates: any): Promise<any> {
    const staff = await this.db.update(usersTable)
      .set({ ...updates, updatedAt: new Date() })
      .where(eq(usersTable.id, id))
      .returning();
    return staff[0];
  }

  async deleteStaff(id: string): Promise<void> {
    await this.db.delete(usersTable).where(eq(usersTable.id, id));
  }

  async getRoles(): Promise<any[]> {
    return [
      {
        id: "admin",
        name: "Quản trị viên",
        description: "Toàn quyền quản lý hệ thống",
        permissions: ["all"],
        color: "red"
      },
      {
        id: "manager",
        name: "Quản lý cửa hàng",
        description: "Quản lý cửa hàng và nhân viên",
        permissions: ["manage_staff", "manage_products", "manage_orders", "view_reports"],
        color: "blue"
      },
      {
        id: "cashier",
        name: "Thu ngân",
        description: "Xử lý bán hàng và thanh toán",
        permissions: ["create_orders", "manage_customers", "view_products"],
        color: "green"
      },
      {
        id: "staff",
        name: "Nhân viên",
        description: "Quyền truy cập cơ bản",
        permissions: ["view_products", "create_orders"],
        color: "gray"
      },
      {
        id: "inventory",
        name: "Thủ kho",
        description: "Quản lý kho và kiểm hàng",
        permissions: ["manage_inventory", "view_products", "manage_stock"],
        color: "orange"
      }
    ];
  }

  async getStaffGroups(): Promise<any[]> {
    return [
      {
        id: "management",
        name: "Ban quản lý",
        description: "Quản trị viên và quản lý cửa hàng",
        memberCount: 2
      },
      {
        id: "sales",
        name: "Bán hàng",
        description: "Nhân viên bán hàng và thu ngân",
        memberCount: 5
      },
      {
        id: "warehouse",
        name: "Kho vận",
        description: "Nhân viên kho và logistics",
        memberCount: 2
      },
      {
        id: "parttime",
        name: "Bán thời gian",
        description: "Nhân viên làm bán thời gian",
        memberCount: 3
      }
    ];
  }

  private getRolePermissions(role: string): string[] {
    const roleMap: { [key: string]: string[] } = {
      admin: ["all"],
      manager: ["manage_staff", "manage_products", "manage_orders", "view_reports"],
      cashier: ["create_orders", "manage_customers", "view_products"],
      staff: ["view_products", "create_orders"],
      inventory: ["manage_inventory", "view_products", "manage_stock"]
    };
    return roleMap[role] || roleMap.staff;
  }

  // Category Management
  async createCategory(categoryData: any): Promise<any> {
    const category = await this.db.insert(categoriesTable).values({
      id: categoryData.id || `cat-${Date.now()}`,
      ...categoryData,
      createdAt: new Date(),
      updatedAt: new Date()
    }).returning();
    return category[0];
  }

  async updateCategory(id: string, updates: any): Promise<any> {
    const category = await this.db.update(categoriesTable)
      .set({ ...updates, updatedAt: new Date() })
      .where(eq(categoriesTable.id, id))
      .returning();
    return category[0];
  }

  async deleteCategory(id: string): Promise<void> {
    await this.db.delete(categoriesTable).where(eq(categoriesTable.id, id));
  }
}

// Create storage instance
export const storage = new MemStorage();