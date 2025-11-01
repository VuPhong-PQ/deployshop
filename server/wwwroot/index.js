// server/index.ts
import express2 from "express";

// server/routes.ts
import { createServer } from "http";
import { WebSocketServer, WebSocket } from "ws";

// shared/schema.ts
import { sql, relations } from "drizzle-orm";
import { pgTable, text, integer, decimal, timestamp, boolean, jsonb, uuid } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
var users = pgTable("users", {
  id: uuid("id").primaryKey().default(sql`gen_random_uuid()`),
  username: text("username").notNull().unique(),
  password: text("password").notNull(),
  fullName: text("full_name").notNull(),
  email: text("email"),
  role: text("role").notNull().default("cashier"),
  // owner, manager, cashier
  avatar: text("avatar"),
  isActive: boolean("is_active").notNull().default(true),
  storeId: uuid("store_id"),
  createdAt: timestamp("created_at").notNull().default(sql`now()`),
  updatedAt: timestamp("updated_at").notNull().default(sql`now()`)
});
var stores = pgTable("stores", {
  id: uuid("id").primaryKey().default(sql`gen_random_uuid()`),
  name: text("name").notNull(),
  address: text("address").notNull(),
  phone: text("phone"),
  email: text("email"),
  taxCode: text("tax_code"),
  isActive: boolean("is_active").notNull().default(true),
  createdAt: timestamp("created_at").notNull().default(sql`now()`),
  updatedAt: timestamp("updated_at").notNull().default(sql`now()`)
});
var categories = pgTable("categories", {
  id: uuid("id").primaryKey().default(sql`gen_random_uuid()`),
  name: text("name").notNull(),
  description: text("description"),
  image: text("image"),
  isActive: boolean("is_active").notNull().default(true),
  storeId: uuid("store_id").notNull(),
  createdAt: timestamp("created_at").notNull().default(sql`now()`)
});
var products = pgTable("products", {
  id: uuid("id").primaryKey().default(sql`gen_random_uuid()`),
  name: text("name").notNull(),
  description: text("description"),
  sku: text("sku").notNull(),
  barcode: text("barcode"),
  price: decimal("price", { precision: 12, scale: 2 }).notNull(),
  costPrice: decimal("cost_price", { precision: 12, scale: 2 }),
  image: text("image"),
  categoryId: uuid("category_id"),
  storeId: uuid("store_id").notNull(),
  isActive: boolean("is_active").notNull().default(true),
  stockQuantity: integer("stock_quantity").notNull().default(0),
  minStockLevel: integer("min_stock_level").notNull().default(5),
  unit: text("unit").notNull().default("pcs"),
  // pcs, kg, liter, etc.
  createdAt: timestamp("created_at").notNull().default(sql`now()`),
  updatedAt: timestamp("updated_at").notNull().default(sql`now()`)
});
var customers = pgTable("customers", {
  id: uuid("id").primaryKey().default(sql`gen_random_uuid()`),
  name: text("name").notNull(),
  email: text("email"),
  phone: text("phone"),
  address: text("address"),
  dateOfBirth: timestamp("date_of_birth"),
  customerType: text("customer_type", { enum: ["regular", "premium", "vip"] }).notNull().default("regular"),
  loyaltyPoints: integer("loyalty_points").notNull().default(0),
  totalSpent: decimal("total_spent", { precision: 12, scale: 2 }).notNull().default("0"),
  storeId: uuid("store_id").notNull(),
  isActive: boolean("is_active").notNull().default(true),
  createdAt: timestamp("created_at").notNull().default(sql`now()`),
  updatedAt: timestamp("updated_at").notNull().default(sql`now()`)
});
var orders = pgTable("orders", {
  id: uuid("id").primaryKey().default(sql`gen_random_uuid()`),
  orderNumber: text("order_number").notNull().unique(),
  customerId: uuid("customer_id"),
  cashierId: uuid("cashier_id").notNull(),
  storeId: uuid("store_id").notNull(),
  subtotal: decimal("subtotal", { precision: 12, scale: 2 }).notNull(),
  taxAmount: decimal("tax_amount", { precision: 12, scale: 2 }).notNull().default("0"),
  discountAmount: decimal("discount_amount", { precision: 12, scale: 2 }).notNull().default("0"),
  total: decimal("total", { precision: 12, scale: 2 }).notNull(),
  paymentMethod: text("payment_method").notNull(),
  // cash, card, qr, ewallet, transfer
  paymentStatus: text("payment_status").notNull().default("completed"),
  // pending, completed, failed
  status: text("status").notNull().default("completed"),
  // pending, processing, completed, cancelled
  notes: text("notes"),
  metadata: jsonb("metadata"),
  // For additional payment info, receipt URLs, etc.
  createdAt: timestamp("created_at").notNull().default(sql`now()`),
  updatedAt: timestamp("updated_at").notNull().default(sql`now()`)
});
var orderItems = pgTable("order_items", {
  id: uuid("id").primaryKey().default(sql`gen_random_uuid()`),
  orderId: uuid("order_id").notNull(),
  productId: uuid("product_id").notNull(),
  quantity: integer("quantity").notNull(),
  unitPrice: decimal("unit_price", { precision: 12, scale: 2 }).notNull(),
  totalPrice: decimal("total_price", { precision: 12, scale: 2 }).notNull(),
  createdAt: timestamp("created_at").notNull().default(sql`now()`)
});
var promotions = pgTable("promotions", {
  id: uuid("id").primaryKey().default(sql`gen_random_uuid()`),
  name: text("name").notNull(),
  description: text("description"),
  type: text("type").notNull(),
  // percentage, fixed_amount, buy_x_get_y
  value: decimal("value", { precision: 12, scale: 2 }).notNull(),
  minOrderAmount: decimal("min_order_amount", { precision: 12, scale: 2 }),
  maxDiscountAmount: decimal("max_discount_amount", { precision: 12, scale: 2 }),
  startDate: timestamp("start_date").notNull(),
  endDate: timestamp("end_date").notNull(),
  isActive: boolean("is_active").notNull().default(true),
  storeId: uuid("store_id").notNull(),
  createdAt: timestamp("created_at").notNull().default(sql`now()`)
});
var inventoryMovements = pgTable("inventory_movements", {
  id: uuid("id").primaryKey().default(sql`gen_random_uuid()`),
  productId: uuid("product_id").notNull(),
  type: text("type").notNull(),
  // sale, purchase, adjustment, return
  quantity: integer("quantity").notNull(),
  previousStock: integer("previous_stock").notNull(),
  newStock: integer("new_stock").notNull(),
  reason: text("reason"),
  userId: uuid("user_id").notNull(),
  storeId: uuid("store_id").notNull(),
  createdAt: timestamp("created_at").notNull().default(sql`now()`)
});
var usersRelations = relations(users, ({ one, many }) => ({
  store: one(stores, { fields: [users.storeId], references: [stores.id] }),
  orders: many(orders),
  inventoryMovements: many(inventoryMovements)
}));
var storesRelations = relations(stores, ({ many }) => ({
  users: many(users),
  categories: many(categories),
  products: many(products),
  customers: many(customers),
  orders: many(orders),
  promotions: many(promotions),
  inventoryMovements: many(inventoryMovements)
}));
var categoriesRelations = relations(categories, ({ one, many }) => ({
  store: one(stores, { fields: [categories.storeId], references: [stores.id] }),
  products: many(products)
}));
var productsRelations = relations(products, ({ one, many }) => ({
  category: one(categories, { fields: [products.categoryId], references: [categories.id] }),
  store: one(stores, { fields: [products.storeId], references: [stores.id] }),
  orderItems: many(orderItems),
  inventoryMovements: many(inventoryMovements)
}));
var customersRelations = relations(customers, ({ one, many }) => ({
  store: one(stores, { fields: [customers.storeId], references: [stores.id] }),
  orders: many(orders)
}));
var ordersRelations = relations(orders, ({ one, many }) => ({
  customer: one(customers, { fields: [orders.customerId], references: [customers.id] }),
  cashier: one(users, { fields: [orders.cashierId], references: [users.id] }),
  store: one(stores, { fields: [orders.storeId], references: [stores.id] }),
  items: many(orderItems)
}));
var orderItemsRelations = relations(orderItems, ({ one }) => ({
  order: one(orders, { fields: [orderItems.orderId], references: [orders.id] }),
  product: one(products, { fields: [orderItems.productId], references: [products.id] })
}));
var promotionsRelations = relations(promotions, ({ one }) => ({
  store: one(stores, { fields: [promotions.storeId], references: [stores.id] })
}));
var inventoryMovementsRelations = relations(inventoryMovements, ({ one }) => ({
  product: one(products, { fields: [inventoryMovements.productId], references: [products.id] }),
  user: one(users, { fields: [inventoryMovements.userId], references: [users.id] }),
  store: one(stores, { fields: [inventoryMovements.storeId], references: [stores.id] })
}));
var insertUserSchema2 = createInsertSchema(users).omit({
  id: true,
  createdAt: true,
  updatedAt: true
});
var insertStoreSchema = createInsertSchema(stores).omit({
  id: true,
  createdAt: true,
  updatedAt: true
});
var insertCategorySchema = createInsertSchema(categories).omit({
  id: true,
  createdAt: true
});
var insertProductSchema = createInsertSchema(products).omit({
  id: true,
  createdAt: true,
  updatedAt: true
});
var insertCustomerSchema = createInsertSchema(customers).omit({
  id: true,
  createdAt: true,
  updatedAt: true
});
var insertOrderSchema = createInsertSchema(orders).omit({
  id: true,
  createdAt: true,
  updatedAt: true
});
var insertOrderItemSchema = createInsertSchema(orderItems).omit({
  id: true,
  createdAt: true
});
var insertPromotionSchema = createInsertSchema(promotions).omit({
  id: true,
  createdAt: true
});
var insertInventoryMovementSchema = createInsertSchema(inventoryMovements).omit({
  id: true,
  createdAt: true
});

// server/db.ts
var db = null;

// server/storage.ts
import { eq } from "drizzle-orm";
var MemStorage = class {
  async getUser(id) {
    const [user] = await db.select().from(users).where(eq(users.id, id));
    return user || void 0;
  }
  async getUserByUsername(username) {
    const [user] = await db.select().from(users).where(eq(users.username, username));
    return user || void 0;
  }
  async createUser(insertUser) {
    const [user] = await db.insert(users).values(insertUser).returning();
    return user;
  }
  // Dashboard methods
  async getDashboardMetrics(storeId) {
    const lowStockProducts = await this.getLowStockProducts(storeId);
    return {
      todayRevenue: "15.750.000\u20AB",
      todayGrowth: "+12.5%",
      monthRevenue: "480.250.000\u20AB",
      monthGrowth: "+8.3%",
      ordersCount: 2847,
      ordersGrowth: "+15.2%",
      newCustomers: 1234,
      customersGrowth: "+5.7%",
      lowStockItems: lowStockProducts.length
    };
  }
  async getRevenueChartData(storeId, days) {
    const data = [];
    for (let i = days - 1; i >= 0; i--) {
      const date = /* @__PURE__ */ new Date();
      date.setDate(date.getDate() - i);
      data.push({
        date: date.toISOString().split("T")[0],
        revenue: Math.floor(Math.random() * 5e6) + 1e6,
        orders: Math.floor(Math.random() * 50) + 10
      });
    }
    return data;
  }
  async getTopProducts(storeId, limit) {
    return [
      { name: "iPhone 15 Pro", sales: 45, revenue: "900.000.000\u20AB" },
      { name: "MacBook Air M2", sales: 23, revenue: "690.000.000\u20AB" },
      { name: "AirPods Pro", sales: 67, revenue: "402.000.000\u20AB" },
      { name: "iPad Air", sales: 34, revenue: "340.000.000\u20AB" },
      { name: "Apple Watch", sales: 28, revenue: "280.000.000\u20AB" }
    ].slice(0, limit);
  }
  async getRecentOrders(storeId, limit) {
    return [
      {
        id: "1",
        orderNumber: "ORD-001",
        customer: "Nguy\u1EC5n V\u0103n An",
        total: "15.000.000\u20AB",
        status: "completed",
        createdAt: /* @__PURE__ */ new Date()
      },
      {
        id: "2",
        orderNumber: "ORD-002",
        customer: "Tr\u1EA7n Th\u1ECB B\xECnh",
        total: "8.500.000\u20AB",
        status: "processing",
        createdAt: new Date(Date.now() - 36e5)
      }
    ].slice(0, limit);
  }
  async getLowStockProducts(storeId) {
    return [
      { id: "1", name: "iPhone 15 Pro", current: 3, minimum: 5, status: "low" },
      { id: "2", name: "AirPods Pro", current: 1, minimum: 10, status: "critical" }
    ];
  }
  // Products
  async getProducts(storeId) {
    return [
      {
        id: "550e8400-e29b-41d4-a716-446655440010",
        name: "iPhone 15 Pro",
        description: "Smartphone cao c\u1EA5p v\u1EDBi chip A17 Pro",
        sku: "IP15P-128GB",
        barcode: "123456789",
        price: "25000000",
        costPrice: "22000000",
        image: "https://images.unsplash.com/photo-1592750475338-74b7b21085ab?w=200&h=200&fit=crop",
        categoryId: "550e8400-e29b-41d4-a716-446655440003",
        storeId,
        isActive: true,
        stockQuantity: 15,
        minStockLevel: 10,
        unit: "chi\u1EBFc",
        createdAt: /* @__PURE__ */ new Date(),
        updatedAt: /* @__PURE__ */ new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440011",
        name: "AirPods Pro 2",
        description: "Tai nghe kh\xF4ng d\xE2y ch\u1ED1ng \u1ED3n",
        sku: "APP2-WHITE",
        barcode: "987654321",
        price: "6000000",
        costPrice: "5200000",
        image: "https://images.unsplash.com/photo-1606220945770-b5b6c2c55bf1?w=200&h=200&fit=crop",
        categoryId: "550e8400-e29b-41d4-a716-446655440005",
        storeId,
        isActive: true,
        stockQuantity: 25,
        minStockLevel: 15,
        unit: "c\u1EB7p",
        createdAt: /* @__PURE__ */ new Date(),
        updatedAt: /* @__PURE__ */ new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440013",
        name: 'iPad Air 11"',
        description: "M\xE1y t\xEDnh b\u1EA3ng v\u1EDBi chip M2",
        sku: "IPA11-256GB",
        barcode: "135792468",
        price: "15000000",
        costPrice: "13000000",
        image: "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=200&h=200&fit=crop",
        categoryId: "550e8400-e29b-41d4-a716-446655440003",
        storeId,
        isActive: true,
        stockQuantity: 3,
        minStockLevel: 5,
        unit: "chi\u1EBFc",
        createdAt: /* @__PURE__ */ new Date(),
        updatedAt: /* @__PURE__ */ new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440012",
        name: "MacBook Air M2",
        description: "Laptop Apple v\u1EDBi chip M2",
        sku: "MBA-M2-256GB",
        barcode: "456789123",
        price: "30000000",
        costPrice: "27000000",
        image: "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=200&h=200&fit=crop",
        categoryId: "550e8400-e29b-41d4-a716-446655440004",
        storeId,
        isActive: true,
        stockQuantity: 8,
        minStockLevel: 3,
        unit: "chi\u1EBFc",
        createdAt: /* @__PURE__ */ new Date(),
        updatedAt: /* @__PURE__ */ new Date()
      }
    ];
  }
  async createProduct(insertProduct) {
    const [product] = await db.insert(products).values(insertProduct).returning();
    return product;
  }
  async updateProduct(id, updates) {
    const [product] = await db.update(products).set(updates).where(eq(products.id, id)).returning();
    return product || void 0;
  }
  async deleteProduct(id) {
    await db.delete(products).where(eq(products.id, id));
  }
  // Customers
  async getCustomers(storeId) {
    return [
      {
        id: "1",
        name: "Nguy\u1EC5n V\u0103n An",
        email: "nguyenvanan@email.com",
        phone: "0901234567",
        address: "123 L\xEA L\u1EE3i, Q1, TP.HCM",
        dateOfBirth: /* @__PURE__ */ new Date("1990-05-15"),
        customerType: "vip",
        loyaltyPoints: 1500,
        totalSpent: "5000000",
        storeId,
        isActive: true,
        createdAt: /* @__PURE__ */ new Date(),
        updatedAt: /* @__PURE__ */ new Date()
      },
      {
        id: "2",
        name: "Tr\u1EA7n Th\u1ECB B\xECnh",
        email: "tranthibinh@email.com",
        phone: "0912345678",
        address: "456 Nguy\u1EC5n Hu\u1EC7, Q1, TP.HCM",
        dateOfBirth: /* @__PURE__ */ new Date("1985-12-20"),
        customerType: "premium",
        loyaltyPoints: 800,
        totalSpent: "3200000",
        storeId,
        isActive: true,
        createdAt: /* @__PURE__ */ new Date(),
        updatedAt: /* @__PURE__ */ new Date()
      },
      {
        id: "3",
        name: "L\xEA V\u0103n C\u01B0\u1EDDng",
        email: "levancuong@email.com",
        phone: "0923456789",
        address: "789 Tr\u1EA7n H\u01B0ng \u0110\u1EA1o, Q5, TP.HCM",
        dateOfBirth: /* @__PURE__ */ new Date("1992-03-10"),
        customerType: "regular",
        loyaltyPoints: 250,
        totalSpent: "1200000",
        storeId,
        isActive: true,
        createdAt: /* @__PURE__ */ new Date(),
        updatedAt: /* @__PURE__ */ new Date()
      }
    ];
  }
  async createCustomer(insertCustomer) {
    const [customer] = await db.insert(customers).values(insertCustomer).returning();
    return customer;
  }
  // Orders
  async getOrders(storeId) {
    return [
      {
        id: "550e8400-e29b-41d4-a716-446655440020",
        orderNumber: "ORD-2025-001",
        customerId: "1",
        cashierId: "550e8400-e29b-41d4-a716-446655440001",
        storeId,
        subtotal: "25000000",
        taxAmount: "2500000",
        discountAmount: "1000000",
        total: "26500000",
        paymentMethod: "cash",
        paymentStatus: "paid",
        status: "completed",
        notes: "Kh\xE1ch h\xE0ng VIP - gi\u1EA3m gi\xE1 \u0111\u1EB7c bi\u1EC7t",
        metadata: { receiptUrl: null },
        createdAt: /* @__PURE__ */ new Date(),
        updatedAt: /* @__PURE__ */ new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440021",
        orderNumber: "ORD-2025-002",
        customerId: "2",
        cashierId: "550e8400-e29b-41d4-a716-446655440001",
        storeId,
        subtotal: "12000000",
        taxAmount: "1200000",
        discountAmount: "0",
        total: "13200000",
        paymentMethod: "card",
        paymentStatus: "paid",
        status: "completed",
        notes: "",
        metadata: { receiptUrl: null },
        createdAt: /* @__PURE__ */ new Date(),
        updatedAt: /* @__PURE__ */ new Date()
      }
    ];
  }
  async createOrder(insertOrder, items) {
    const [order] = await db.insert(orders).values(insertOrder).returning();
    if (items && items.length > 0) {
      await db.insert(orderItems).values(
        items.map((item) => ({ ...item, orderId: order.id }))
      );
    }
    return order;
  }
  // Categories
  async getCategories(storeId) {
    return [
      {
        id: "550e8400-e29b-41d4-a716-446655440003",
        name: "\u0110i\u1EC7n tho\u1EA1i & Tablet",
        description: "Smartphone, m\xE1y t\xEDnh b\u1EA3ng",
        image: null,
        storeId,
        isActive: true,
        createdAt: /* @__PURE__ */ new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440004",
        name: "Laptop & PC",
        description: "M\xE1y t\xEDnh x\xE1ch tay v\xE0 \u0111\u1EC3 b\xE0n",
        image: null,
        storeId,
        isActive: true,
        createdAt: /* @__PURE__ */ new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440005",
        name: "Ph\u1EE5 ki\u1EC7n",
        description: "Tai nghe, s\u1EA1c, \u1ED1p l\u01B0ng",
        image: null,
        storeId,
        isActive: true,
        createdAt: /* @__PURE__ */ new Date()
      }
    ];
  }
  // Inventory
  async getInventoryMovements(storeId) {
    return [
      {
        id: "550e8400-e29b-41d4-a716-446655440030",
        productId: "550e8400-e29b-41d4-a716-446655440010",
        quantity: 20,
        type: "in",
        reason: "Nh\u1EADp h\xE0ng t\u1EEB nh\xE0 cung c\u1EA5p",
        previousStock: 0,
        newStock: 20,
        userId: "550e8400-e29b-41d4-a716-446655440001",
        storeId,
        createdAt: /* @__PURE__ */ new Date()
      },
      {
        id: "550e8400-e29b-41d4-a716-446655440031",
        productId: "550e8400-e29b-41d4-a716-446655440011",
        quantity: -2,
        type: "out",
        reason: "B\xE1n h\xE0ng",
        previousStock: 27,
        newStock: 25,
        userId: "550e8400-e29b-41d4-a716-446655440001",
        storeId,
        createdAt: /* @__PURE__ */ new Date()
      }
    ];
  }
  async adjustInventory(productId, quantity, reason, userId, storeId) {
    const [product] = await db.select().from(products).where(eq(products.id, productId));
    const previousStock = product?.stockQuantity || 0;
    const newStock = previousStock + quantity;
    await db.update(products).set({ stockQuantity: newStock }).where(eq(products.id, productId));
    const [movement] = await db.insert(inventoryMovements).values({
      productId,
      quantity,
      type: quantity > 0 ? "in" : "out",
      reason,
      previousStock,
      newStock,
      userId,
      storeId
    }).returning();
    return movement;
  }
  // Reports
  async getSalesSummary(storeId, startDate, endDate) {
    const orders2 = await this.db.select({
      id: ordersTable.id,
      total: ordersTable.total,
      createdAt: ordersTable.createdAt
    }).from(ordersTable);
    const totalRevenue = orders2.reduce((sum, order) => sum + parseFloat(order.total), 0);
    const totalOrders = orders2.length;
    const averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
    return {
      totalRevenue: totalRevenue.toLocaleString("vi-VN") + "\u20AB",
      totalOrders,
      averageOrderValue: averageOrderValue.toLocaleString("vi-VN") + "\u20AB",
      period: `${startDate || "T\u1EA5t c\u1EA3"} - ${endDate || "hi\u1EC7n t\u1EA1i"}`
    };
  }
  async getProductPerformance(storeId, startDate, endDate) {
    const topProducts = [
      { name: "iPhone 15 Pro", totalSold: 15, revenue: "375.000.000\u20AB", profit: "45.000.000\u20AB" },
      { name: "MacBook Air M2", totalSold: 8, revenue: "240.000.000\u20AB", profit: "24.000.000\u20AB" },
      { name: "AirPods Pro 2", totalSold: 25, revenue: "150.000.000\u20AB", profit: "20.000.000\u20AB" },
      { name: 'iPad Air 11"', totalSold: 3, revenue: "45.000.000\u20AB", profit: "6.000.000\u20AB" }
    ];
    return {
      topProducts,
      totalProductsSold: topProducts.reduce((sum, p) => sum + p.totalSold, 0),
      mostPopularProduct: topProducts[0].name,
      totalCategories: 4
    };
  }
  async getCustomerAnalytics(storeId, startDate, endDate) {
    const customers2 = await this.db.select().from(customersTable);
    return {
      totalCustomers: customers2.length,
      newCustomers: 3,
      returningCustomers: customers2.length - 3,
      averageOrdersPerCustomer: "2.4",
      topCustomers: [
        { name: "Nguy\u1EC5n V\u0103n An", orders: 8, totalSpent: "25.000.000\u20AB" },
        { name: "Tr\u1EA7n Th\u1ECB B\xEDch", orders: 6, totalSpent: "18.500.000\u20AB" },
        { name: "L\xEA Ho\xE0ng Nam", orders: 4, totalSpent: "12.300.000\u20AB" }
      ]
    };
  }
  async getProfitAnalysis(storeId, startDate, endDate) {
    return {
      totalProfit: "95.000.000\u20AB",
      profitMargin: "18.5%",
      costOfGoodsSold: "420.000.000\u20AB",
      grossProfit: "115.000.000\u20AB",
      operatingExpenses: "20.000.000\u20AB",
      monthlyTrend: [
        { month: "07/2025", profit: "78.000.000\u20AB", margin: "16.2%" },
        { month: "08/2025", profit: "95.000.000\u20AB", margin: "18.5%" }
      ],
      topProfitableProducts: [
        { name: "iPhone 15 Pro", profit: "45.000.000\u20AB", margin: "12%" },
        { name: "MacBook Air M2", profit: "24.000.000\u20AB", margin: "10%" },
        { name: "AirPods Pro 2", profit: "20.000.000\u20AB", margin: "13.3%" }
      ]
    };
  }
  async getSalesReport(storeId, startDate, endDate) {
    return {
      totalRevenue: "125.500.000\u20AB",
      totalOrders: 45,
      averageOrderValue: "2.788.889\u20AB",
      topProducts: [
        { name: "iPhone 15 Pro", quantity: 12, revenue: "300.000.000\u20AB" },
        { name: "MacBook Air M2", revenue: "180.000.000\u20AB" },
        { name: "AirPods Pro", quantity: 25, revenue: "150.000.000\u20AB" }
      ],
      dailySales: [
        { date: "2025-08-20", revenue: "8.500.000\u20AB", orders: 3 },
        { date: "2025-08-21", revenue: "12.200.000\u20AB", orders: 5 },
        { date: "2025-08-22", revenue: "15.800.000\u20AB", orders: 7 }
      ]
    };
  }
  // Staff Management
  async getStaff() {
    const users2 = await this.db.select().from(usersTable);
    return users2.map((user) => ({
      ...user,
      role: user.role || "staff",
      status: user.isActive ? "active" : "inactive",
      workSchedule: user.workSchedule || "9:00-18:00",
      permissions: this.getRolePermissions(user.role || "staff")
    }));
  }
  async createStaff(staffData) {
    const staff = await this.db.insert(usersTable).values({
      id: `staff-${Date.now()}`,
      ...staffData,
      createdAt: /* @__PURE__ */ new Date(),
      updatedAt: /* @__PURE__ */ new Date()
    }).returning();
    return staff[0];
  }
  async updateStaff(id, updates) {
    const staff = await this.db.update(usersTable).set({ ...updates, updatedAt: /* @__PURE__ */ new Date() }).where(eq(usersTable.id, id)).returning();
    return staff[0];
  }
  async deleteStaff(id) {
    await this.db.delete(usersTable).where(eq(usersTable.id, id));
  }
  async getRoles() {
    return [
      {
        id: "admin",
        name: "Qu\u1EA3n tr\u1ECB vi\xEAn",
        description: "To\xE0n quy\u1EC1n qu\u1EA3n l\xFD h\u1EC7 th\u1ED1ng",
        permissions: ["all"],
        color: "red"
      },
      {
        id: "manager",
        name: "Qu\u1EA3n l\xFD c\u1EEDa h\xE0ng",
        description: "Qu\u1EA3n l\xFD c\u1EEDa h\xE0ng v\xE0 nh\xE2n vi\xEAn",
        permissions: ["manage_staff", "manage_products", "manage_orders", "view_reports"],
        color: "blue"
      },
      {
        id: "cashier",
        name: "Thu ng\xE2n",
        description: "X\u1EED l\xFD b\xE1n h\xE0ng v\xE0 thanh to\xE1n",
        permissions: ["create_orders", "manage_customers", "view_products"],
        color: "green"
      },
      {
        id: "staff",
        name: "Nh\xE2n vi\xEAn",
        description: "Quy\u1EC1n truy c\u1EADp c\u01A1 b\u1EA3n",
        permissions: ["view_products", "create_orders"],
        color: "gray"
      },
      {
        id: "inventory",
        name: "Th\u1EE7 kho",
        description: "Qu\u1EA3n l\xFD kho v\xE0 ki\u1EC3m h\xE0ng",
        permissions: ["manage_inventory", "view_products", "manage_stock"],
        color: "orange"
      }
    ];
  }
  async getStaffGroups() {
    return [
      {
        id: "management",
        name: "Ban qu\u1EA3n l\xFD",
        description: "Qu\u1EA3n tr\u1ECB vi\xEAn v\xE0 qu\u1EA3n l\xFD c\u1EEDa h\xE0ng",
        memberCount: 2
      },
      {
        id: "sales",
        name: "B\xE1n h\xE0ng",
        description: "Nh\xE2n vi\xEAn b\xE1n h\xE0ng v\xE0 thu ng\xE2n",
        memberCount: 5
      },
      {
        id: "warehouse",
        name: "Kho v\u1EADn",
        description: "Nh\xE2n vi\xEAn kho v\xE0 logistics",
        memberCount: 2
      },
      {
        id: "parttime",
        name: "B\xE1n th\u1EDDi gian",
        description: "Nh\xE2n vi\xEAn l\xE0m b\xE1n th\u1EDDi gian",
        memberCount: 3
      }
    ];
  }
  getRolePermissions(role) {
    const roleMap = {
      admin: ["all"],
      manager: ["manage_staff", "manage_products", "manage_orders", "view_reports"],
      cashier: ["create_orders", "manage_customers", "view_products"],
      staff: ["view_products", "create_orders"],
      inventory: ["manage_inventory", "view_products", "manage_stock"]
    };
    return roleMap[role] || roleMap.staff;
  }
  // Category Management
  async createCategory(categoryData) {
    const category = await this.db.insert(categoriesTable).values({
      id: categoryData.id || `cat-${Date.now()}`,
      ...categoryData,
      createdAt: /* @__PURE__ */ new Date(),
      updatedAt: /* @__PURE__ */ new Date()
    }).returning();
    return category[0];
  }
  async updateCategory(id, updates) {
    const category = await this.db.update(categoriesTable).set({ ...updates, updatedAt: /* @__PURE__ */ new Date() }).where(eq(categoriesTable.id, id)).returning();
    return category[0];
  }
  async deleteCategory(id) {
    await this.db.delete(categoriesTable).where(eq(categoriesTable.id, id));
  }
};
var storage = new MemStorage();

// server/routes.ts
import { z } from "zod";
async function registerRoutes(app2) {
  const httpServer = createServer(app2);
  const wss = new WebSocketServer({ server: httpServer, path: "/ws" });
  const clients = /* @__PURE__ */ new Set();
  wss.on("connection", (ws) => {
    clients.add(ws);
    console.log("Client connected to WebSocket");
    ws.on("close", () => {
      clients.delete(ws);
      console.log("Client disconnected from WebSocket");
    });
  });
  const broadcast = (data) => {
    clients.forEach((client) => {
      if (client.readyState === WebSocket.OPEN) {
        client.send(JSON.stringify(data));
      }
    });
  };
  app2.get("/api/dashboard/metrics", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const metrics = await storage.getDashboardMetrics(storeId);
      res.json(metrics);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch dashboard metrics" });
    }
  });
  app2.get("/api/dashboard/revenue-chart", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const days = parseInt(req.query.days) || 7;
      const data = await storage.getRevenueChartData(storeId, days);
      res.json(data);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch revenue chart data" });
    }
  });
  app2.get("/api/dashboard/top-products", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const limit = parseInt(req.query.limit) || 5;
      const products2 = await storage.getTopProducts(storeId, limit);
      res.json(products2);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch top products" });
    }
  });
  app2.get("/api/dashboard/recent-orders", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const limit = parseInt(req.query.limit) || 10;
      const orders2 = await storage.getRecentOrders(storeId, limit);
      res.json(orders2);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch recent orders" });
    }
  });
  app2.get("/api/dashboard/low-stock", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const products2 = await storage.getLowStockProducts(storeId);
      res.json(products2);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch low stock products" });
    }
  });
  app2.get("/api/reports/sales-summary", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const { startDate, endDate } = req.query;
      const summary = await storage.getSalesSummary(storeId, startDate, endDate);
      res.json(summary);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch sales summary" });
    }
  });
  app2.get("/api/reports/product-performance", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const { startDate, endDate } = req.query;
      const performance = await storage.getProductPerformance(storeId, startDate, endDate);
      res.json(performance);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch product performance" });
    }
  });
  app2.get("/api/reports/customer-analytics", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const { startDate, endDate } = req.query;
      const analytics = await storage.getCustomerAnalytics(storeId, startDate, endDate);
      res.json(analytics);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch customer analytics" });
    }
  });
  app2.get("/api/reports/profit-analysis", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const { startDate, endDate } = req.query;
      const analysis = await storage.getProfitAnalysis(storeId, startDate, endDate);
      res.json(analysis);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch profit analysis" });
    }
  });
  app2.get("/api/products", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const products2 = await storage.getProducts(storeId);
      res.json(products2);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch products" });
    }
  });
  app2.post("/api/products", async (req, res) => {
    try {
      const productData = insertProductSchema.parse(req.body);
      const product = await storage.createProduct(productData);
      broadcast({ type: "product_created", data: product });
      res.json(product);
    } catch (error) {
      if (error instanceof z.ZodError) {
        res.status(400).json({ error: "Invalid product data", details: error.errors });
      } else {
        res.status(500).json({ error: "Failed to create product" });
      }
    }
  });
  app2.put("/api/products/:id", async (req, res) => {
    try {
      const { id } = req.params;
      const updates = req.body;
      const product = await storage.updateProduct(id, updates);
      if (!product) {
        return res.status(404).json({ error: "Product not found" });
      }
      broadcast({ type: "product_updated", data: product });
      res.json(product);
    } catch (error) {
      res.status(500).json({ error: "Failed to update product" });
    }
  });
  app2.delete("/api/products/:id", async (req, res) => {
    try {
      const { id } = req.params;
      await storage.deleteProduct(id);
      broadcast({ type: "product_deleted", data: { id } });
      res.json({ success: true });
    } catch (error) {
      res.status(500).json({ error: "Failed to delete product" });
    }
  });
  app2.get("/api/customers", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const customers2 = await storage.getCustomers(storeId);
      res.json(customers2);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch customers" });
    }
  });
  app2.post("/api/customers", async (req, res) => {
    try {
      const customerData = insertCustomerSchema.parse(req.body);
      const customer = await storage.createCustomer(customerData);
      broadcast({ type: "customer_created", data: customer });
      res.json(customer);
    } catch (error) {
      if (error instanceof z.ZodError) {
        res.status(400).json({ error: "Invalid customer data", details: error.errors });
      } else {
        res.status(500).json({ error: "Failed to create customer" });
      }
    }
  });
  app2.get("/api/staff", async (req, res) => {
    try {
      const staff = await storage.getStaff();
      res.json(staff);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch staff" });
    }
  });
  app2.post("/api/staff", async (req, res) => {
    try {
      const result = insertUserSchema.safeParse(req.body);
      if (!result.success) {
        return res.status(400).json({ error: "Invalid staff data", details: result.error });
      }
      const staff = await storage.createStaff(result.data);
      broadcast({ type: "staff_created", data: staff });
      res.status(201).json(staff);
    } catch (error) {
      res.status(500).json({ error: "Failed to create staff" });
    }
  });
  app2.put("/api/staff/:id", async (req, res) => {
    try {
      const { id } = req.params;
      const staff = await storage.updateStaff(id, req.body);
      broadcast({ type: "staff_updated", data: staff });
      res.json(staff);
    } catch (error) {
      res.status(500).json({ error: "Failed to update staff" });
    }
  });
  app2.delete("/api/staff/:id", async (req, res) => {
    try {
      const { id } = req.params;
      await storage.deleteStaff(id);
      broadcast({ type: "staff_deleted", data: { id } });
      res.json({ success: true });
    } catch (error) {
      res.status(500).json({ error: "Failed to delete staff" });
    }
  });
  app2.get("/api/roles", async (req, res) => {
    try {
      const roles = await storage.getRoles();
      res.json(roles);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch roles" });
    }
  });
  app2.get("/api/staff-groups", async (req, res) => {
    try {
      const groups = await storage.getStaffGroups();
      res.json(groups);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch staff groups" });
    }
  });
  app2.get("/api/orders", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const orders2 = await storage.getOrders(storeId);
      res.json(orders2);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch orders" });
    }
  });
  app2.post("/api/orders", async (req, res) => {
    try {
      const orderData = insertOrderSchema.parse(req.body);
      const order = await storage.createOrder(orderData, req.body.items);
      broadcast({ type: "order_created", data: order });
      broadcast({ type: "metrics_updated" });
      res.json(order);
    } catch (error) {
      console.error("Order creation error:", error);
      if (error instanceof z.ZodError) {
        res.status(400).json({ error: "Invalid order data", details: error.errors });
      } else {
        res.status(500).json({ error: "Failed to create order", details: error.message });
      }
    }
  });
  app2.get("/api/categories", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const categories3 = await storage.getCategories(storeId);
      res.json(categories3);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch categories" });
    }
  });
  app2.post("/api/categories", async (req, res) => {
    try {
      const categoryData = req.body;
      const category = await storage.createCategory(categoryData);
      res.json(category);
    } catch (error) {
      res.status(500).json({ error: "Failed to create category" });
    }
  });
  app2.put("/api/categories/:id", async (req, res) => {
    try {
      const { id } = req.params;
      const updates = req.body;
      const category = await storage.updateCategory(id, updates);
      res.json(category);
    } catch (error) {
      res.status(500).json({ error: "Failed to update category" });
    }
  });
  app2.delete("/api/categories/:id", async (req, res) => {
    try {
      const { id } = req.params;
      await storage.deleteCategory(id);
      res.json({ success: true });
    } catch (error) {
      res.status(500).json({ error: "Failed to delete category" });
    }
  });
  app2.get("/api/inventory/movements", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const movements = await storage.getInventoryMovements(storeId);
      res.json(movements);
    } catch (error) {
      res.status(500).json({ error: "Failed to fetch inventory movements" });
    }
  });
  app2.post("/api/inventory/adjust", async (req, res) => {
    try {
      const { productId, quantity, reason, userId, storeId } = req.body;
      const movement = await storage.adjustInventory(productId, quantity, reason, userId, storeId);
      broadcast({ type: "inventory_updated", data: movement });
      res.json(movement);
    } catch (error) {
      res.status(500).json({ error: "Failed to adjust inventory" });
    }
  });
  app2.get("/api/reports/sales", async (req, res) => {
    try {
      const storeId = req.headers["x-store-id"] || "";
      const { startDate, endDate } = req.query;
      const report = await storage.getSalesReport(storeId, startDate, endDate);
      res.json(report);
    } catch (error) {
      res.status(500).json({ error: "Failed to generate sales report" });
    }
  });
  return httpServer;
}

// server/vite.ts
import express from "express";
import fs from "fs";
import path2 from "path";
import { createServer as createViteServer, createLogger } from "vite";

// vite.config.ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";
import runtimeErrorOverlay from "@replit/vite-plugin-runtime-error-modal";
var vite_config_default = defineConfig({
  base: process.env.NODE_ENV === "production" ? "/" : "/",
  plugins: [
    react(),
    runtimeErrorOverlay(),
    ...process.env.NODE_ENV !== "production" && process.env.REPL_ID !== void 0 ? [
      await import("@replit/vite-plugin-cartographer").then(
        (m) => m.cartographer()
      )
    ] : []
  ],
  resolve: {
    alias: {
      "@": path.resolve(import.meta.dirname, "client", "src"),
      "@shared": path.resolve(import.meta.dirname, "shared"),
      "@assets": path.resolve(import.meta.dirname, "attached_assets")
    }
  },
  root: path.resolve(import.meta.dirname, "client"),
  build: {
    outDir: "../dist",
    emptyOutDir: true,
    rollupOptions: {
      input: {
        main: path.resolve(import.meta.dirname, "client/index.html")
      }
    },
    // Copy 404.html for GitHub Pages SPA routing
    copyPublicDir: true
  },
  publicDir: path.resolve(import.meta.dirname, "client/public"),
  server: {
    fs: {
      strict: true,
      deny: ["**/.*"]
    },
    proxy: {
      "/api": {
        target: "http://101.53.9.76:5273",
        changeOrigin: true,
        secure: false
      }
    }
  }
});

// server/vite.ts
import { nanoid } from "nanoid";
var viteLogger = createLogger();
function log(message, source = "express") {
  const formattedTime = (/* @__PURE__ */ new Date()).toLocaleTimeString("en-US", {
    hour: "numeric",
    minute: "2-digit",
    second: "2-digit",
    hour12: true
  });
  console.log(`${formattedTime} [${source}] ${message}`);
}
async function setupVite(app2, server) {
  const serverOptions = {
    middlewareMode: true,
    hmr: { server },
    allowedHosts: true
  };
  const vite = await createViteServer({
    ...vite_config_default,
    configFile: false,
    customLogger: {
      ...viteLogger,
      error: (msg, options) => {
        viteLogger.error(msg, options);
        process.exit(1);
      }
    },
    server: serverOptions,
    appType: "custom"
  });
  app2.use(vite.middlewares);
  app2.use("*", async (req, res, next) => {
    const url = req.originalUrl;
    try {
      const clientTemplate = path2.resolve(
        import.meta.dirname,
        "..",
        "client",
        "index.html"
      );
      let template = await fs.promises.readFile(clientTemplate, "utf-8");
      template = template.replace(
        `src="/src/main.tsx"`,
        `src="/src/main.tsx?v=${nanoid()}"`
      );
      const page = await vite.transformIndexHtml(url, template);
      res.status(200).set({ "Content-Type": "text/html" }).end(page);
    } catch (e) {
      vite.ssrFixStacktrace(e);
      next(e);
    }
  });
}
function serveStatic(app2) {
  const distPath = path2.resolve(import.meta.dirname, "public");
  if (!fs.existsSync(distPath)) {
    throw new Error(
      `Could not find the build directory: ${distPath}, make sure to build the client first`
    );
  }
  app2.use(express.static(distPath));
  app2.use("*", (_req, res) => {
    res.sendFile(path2.resolve(distPath, "index.html"));
  });
}

// server/index.ts
var app = express2();
app.use(express2.json());
app.use(express2.urlencoded({ extended: false }));
app.use((req, res, next) => {
  const start = Date.now();
  const path3 = req.path;
  let capturedJsonResponse = void 0;
  const originalResJson = res.json;
  res.json = function(bodyJson, ...args) {
    capturedJsonResponse = bodyJson;
    return originalResJson.apply(res, [bodyJson, ...args]);
  };
  res.on("finish", () => {
    const duration = Date.now() - start;
    if (path3.startsWith("/api")) {
      let logLine = `${req.method} ${path3} ${res.statusCode} in ${duration}ms`;
      if (capturedJsonResponse) {
        logLine += ` :: ${JSON.stringify(capturedJsonResponse)}`;
      }
      if (logLine.length > 80) {
        logLine = logLine.slice(0, 79) + "\u2026";
      }
      log(logLine);
    }
  });
  next();
});
(async () => {
  const server = await registerRoutes(app);
  app.use((err, _req, res, _next) => {
    const status = err.status || err.statusCode || 500;
    const message = err.message || "Internal Server Error";
    res.status(status).json({ message });
    throw err;
  });
  if (app.get("env") === "development") {
    await setupVite(app, server);
  } else {
    serveStatic(app);
  }
  const port = parseInt(process.env.PORT || "5000", 10);
  server.listen({
    port,
    host: "0.0.0.0",
    reusePort: true
  }, () => {
    log(`serving on port ${port}`);
  });
})();
