import { Textarea } from "@/components/ui/textarea";
// Hàm upload ảnh lên server, trả về url
async function uploadImage(file) {
  const formData = new FormData();
  formData.append("file", file);
  const res = await fetch("http://101.53.9.76:5273/api/upload/image", {
    method: "POST",
    body: formData,
  });
  if (!res.ok) throw new Error("Upload ảnh thất bại");
  const data = await res.json();
  return data.url.startsWith("/") ? `http://101.53.9.76:5273${data.url}` : data.url;
}

// Hàm AI tìm kiếm và tải hình ảnh tự động
async function searchAndDownloadImage(productName) {
  const res = await fetch("http://101.53.9.76:5273/api/products/search-image", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ productName }),
  });
  if (!res.ok) throw new Error("Tìm kiếm hình ảnh thất bại");
  const data = await res.json();
  return data.imageUrl.startsWith("/") ? `http://101.53.9.76:5273${data.imageUrl}` : data.imageUrl;
}
import React, { useState } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { useToast } from "@/hooks/use-toast";
import { queryClient, apiRequest } from "@/lib/queryClient";
import { cn, normalizeSearchText } from "@/lib/utils";
import { Plus, Search, Edit, Trash2, Package, AlertTriangle, Download, Upload } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Switch } from "@/components/ui/switch";
import { 
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import { insertProductSchema } from "@shared/schema";
import { z } from "zod";
import type { Product, Category } from "@shared/schema";

const productFormSchema = z.object({
  name: z.string().min(1, "Tên sản phẩm là bắt buộc"),
  description: z.string().optional().default(""),
  barcode: z.string().optional().default(""),
  price: z.number().min(0, "Giá bán là bắt buộc"),
  costPrice: z.number().min(0).optional().default(0),
  image: z.string().optional(),
  productGroupId: z.string().min(1, "Nhóm sản phẩm là bắt buộc"),
  storeId: z.string().default("550e8400-e29b-41d4-a716-446655440002"),
  isActive: z.boolean().default(true),
  isFeatured: z.boolean().default(false), // Sản phẩm hay bán
  stockQuantity: z.number().min(0, "Số lượng tồn kho phải >= 0").default(0),
  minStockLevel: z.number().min(0, "Mức tồn kho tối thiểu phải >= 0").default(5),
  unit: z.string().default("chiếc"),
});

type ProductFormData = z.infer<typeof productFormSchema>;

export default function Products() {
  const { toast } = useToast();
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<string>("all");
  const [selectedGroup, setSelectedGroup] = useState<string>("all");
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [isImportDialogOpen, setIsImportDialogOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [importFile, setImportFile] = useState<File | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(20);
  const [isSearchingImage, setIsSearchingImage] = useState(false);
  const [availableImages, setAvailableImages] = useState<string[]>([]);
  const [showImageSelector, setShowImageSelector] = useState(false);

  // Fetch products with pagination
  const { data: productsResponse, isLoading } = useQuery({
    queryKey: ['/api/products', currentPage, pageSize, searchTerm, selectedGroup],
    queryFn: async () => {
      const params = new URLSearchParams({
        page: currentPage.toString(),
        pageSize: pageSize.toString()
      });
      
      const response = await fetch(`http://101.53.9.76:5273/api/products?${params}`);
      if (!response.ok) {
        throw new Error('Failed to fetch products');
      }
      return response.json();
    }
  });
  
  const products = productsResponse?.products || productsResponse?.Products || [];
  const totalCount = productsResponse?.totalCount || productsResponse?.TotalCount || 0;
  const totalPages = productsResponse?.totalPages || productsResponse?.TotalPages || 0;

  // Debug logs
  console.log('Products data:', products);
  if (Array.isArray(products) && products.length > 0) {
    console.log('Sample product:', products[0]);
  }
  console.log('Products count:', products.length);
  console.log('isLoading:', isLoading);

  // const { data: categories = [] } = useQuery<Category[]>({
  //   queryKey: ['/api/categories'],
  // });
  

  // Thay đổi endpoint lấy nhóm sản phẩm
  const { data: productGroups = [] } = useQuery<any[]>({
    queryKey: ['/api/productgroups'],
  });
  // Map productGroups to Select options format for dropdowns
  const productGroupOptions = productGroups
    .filter(g => g.productGroupId || g.ProductGroupId)
    .map(g => ({
      label: g.name,
      value: String(g.productGroupId ?? g.ProductGroupId),
    }));
  console.log('productGroups:', productGroups);

  // Form for adding/editing products
  const form = useForm<ProductFormData>({
    resolver: zodResolver(productFormSchema),
    defaultValues: {
      name: "",
      description: "",
      barcode: "",
      price: 0,
      costPrice: 0,
      productGroupId: "",
      storeId: "550e8400-e29b-41d4-a716-446655440002",
      stockQuantity: 0,
      minStockLevel: 5,
      unit: "chiếc",
      image: "",
      isFeatured: false
    },
  });

  // Add product mutation
  const addProductMutation = useMutation({
    mutationFn: async (productData: ProductFormData) => {
      const formattedData = {
        name: productData.name,
        description: productData.description || null,
        barcode: productData.barcode || null,
        price: Number(productData.price),
        costPrice: productData.costPrice !== undefined ? Number(productData.costPrice) : null,
        productGroupId: productData.productGroupId ? parseInt(productData.productGroupId) : null,
        stockQuantity: Number(productData.stockQuantity),
        minStockLevel: Number(productData.minStockLevel),
        unit: productData.unit || "chiếc",
        imageUrl: productData.image || null,
        isFeatured: Boolean(productData.isFeatured),
      };
      
      console.log('Sending product data:', formattedData);
      
      const response = await fetch('http://101.53.9.76:5273/api/products', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formattedData)
      });
      
      if (!response.ok) {
        const errorText = await response.text();
        console.error('API Error Response:', errorText);
        throw new Error(`HTTP ${response.status}: ${errorText}`);
      }
      
      // Handle 204 No Content response
      if (response.status === 204) {
        return {};
      }
      return response.json();
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Sản phẩm đã được thêm thành công",
      });
      // Force refresh both products and categories
      queryClient.invalidateQueries({ queryKey: ['/api/products'] });
      queryClient.refetchQueries({ queryKey: ['/api/products'] });
      setIsAddDialogOpen(false);
      form.reset();
    },
    onError: (error: any) => {
      console.error('Add product error:', error);
      toast({
        title: "Lỗi",
        description: `Không thể thêm sản phẩm. ${error?.message || ''}`,
        variant: "destructive",
      });
    }
  });

  // Edit product mutation
  const editProductMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string; data: Partial<ProductFormData> }) => {
      // Map lại dữ liệu cho đúng backend
      const mappedData: any = {
        productId: parseInt(id),
        name: data.name,
        barcode: data.barcode,
        categoryId: data.categoryId ? parseInt(data.categoryId as string) : null,
        price: data.price !== undefined ? Number(data.price) : 0,
        costPrice: data.costPrice !== undefined ? Number(data.costPrice) : 0,
        stockQuantity: data.stockQuantity !== undefined ? Number(data.stockQuantity) : 0,
        minStockLevel: data.minStockLevel !== undefined ? Number(data.minStockLevel) : 0,
        unit: data.unit,
        description: data.description,
        isFeatured: data.isFeatured !== undefined ? Boolean(data.isFeatured) : false,
      };
      
      // Chỉ gửi imageUrl khi có giá trị mới (không rỗng và không undefined)
      if (data.image && data.image.trim() !== '') {
        mappedData.imageUrl = data.image;
      }
      
      try {
        const response = await fetch(`http://101.53.9.76:5273/api/products/${id}`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(mappedData)
        });
        
        if (!response.ok) {
          const errorText = await response.text();
          throw new Error(`HTTP ${response.status}: ${errorText}`);
        }
        
        // Nếu response là 204 No Content thì trả về null, vẫn coi là thành công
        if (response.status === 204) {
          return null;
        }
        
        const result = await response.json();
        return result;
      } catch (error) {
        console.error('API Request failed:', error);
        throw error;
      }
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Sản phẩm đã được cập nhật",
      });
      queryClient.invalidateQueries({ queryKey: ['/api/products'] });
      setTimeout(() => {
        queryClient.refetchQueries({ queryKey: ['/api/products'] });
      }, 300);
      setIsAddDialogOpen(false);
      setEditingProduct(null);
      form.reset();
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể cập nhật sản phẩm",
        variant: "destructive",
      });
    }
  });

  // Delete product mutation
  const deleteProductMutation = useMutation({
    mutationFn: async (id: string) => {
      const response = await fetch(`http://101.53.9.76:5273/api/products/${id}`, {
        method: 'DELETE'
      });
      
      if (!response.ok) {
        throw new Error('Failed to delete product');
      }
      
      // Handle 204 No Content response
      if (response.status === 204) {
        return {};
      }
      return response.json();
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Sản phẩm đã được xóa",
      });
      queryClient.invalidateQueries({ queryKey: ['/api/products'] });
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể xóa sản phẩm",
        variant: "destructive",
      });
    }
  });

  // Export all products mutation
  const exportAllProductsMutation = useMutation({
    mutationFn: async () => {
      const response = await fetch('http://101.53.9.76:5273/api/products/export');
      if (!response.ok) {
        throw new Error('Failed to export products');
      }
      return response.blob();
    },
    onSuccess: (blob) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Products_Export_${new Date().toISOString().slice(0, 10)}.xlsx`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      toast({
        title: "Thành công",
        description: "Dữ liệu sản phẩm đã được xuất thành công",
      });
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể xuất dữ liệu sản phẩm",
        variant: "destructive",
      });
    }
  });

  // Export template mutation
  const exportTemplateMutation = useMutation({
    mutationFn: async () => {
      const response = await fetch('http://101.53.9.76:5273/api/products/export-template');
      if (!response.ok) {
        throw new Error('Failed to export template');
      }
      return response.blob();
    },
    onSuccess: (blob) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Products_Template_${new Date().toISOString().slice(0, 10)}.xlsx`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      toast({
        title: "Thành công",
        description: "Template Excel đã được tải xuống",
      });
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể tải xuống template Excel",
        variant: "destructive",
      });
    }
  });

  // Import from Excel mutation
  const importFromExcelMutation = useMutation({
    mutationFn: async (file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      const response = await fetch('http://101.53.9.76:5273/api/products/import-excel', {
        method: 'POST',
        body: formData,
      });
      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to import Excel file');
      }
      return response.json();
    },
    onSuccess: (result) => {
      const message = result.SkippedCount > 0 
        ? `Import hoàn tất! Thêm mới: ${result.SuccessCount}, Bỏ qua (trùng lặp): ${result.SkippedCount}, Lỗi: ${result.ErrorCount}`
        : `Import thành công ${result.SuccessCount} sản phẩm!`;
      
      toast({
        title: "Import hoàn tất",
        description: message,
        variant: result.ErrorCount > 0 ? "destructive" : "default",
      });
      
      // Hiển thị chi tiết lỗi nếu có
      if (result.Errors && result.Errors.length > 0) {
        console.error('Import errors:', result.Errors);
      }
      
      queryClient.invalidateQueries({ queryKey: ['/api/products'] });
      setIsImportDialogOpen(false);
      setImportFile(null);
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi import",
        description: error.message || "Không thể import file Excel",
        variant: "destructive",
      });
    }
  });

  // Reset page when filters change
  React.useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm, selectedGroup]);

  // Debug products
  console.log('Products data:', products);
  console.log('Products count:', products.length);
  console.log('Current page:', currentPage);
  console.log('Total pages:', totalPages);

  // Handle AI image search
  const handleAIImageSearch = async () => {
    const productName = form.getValues("name");
    if (!productName?.trim()) {
      toast({
        title: "Lỗi",
        description: "Vui lòng nhập tên sản phẩm trước khi tìm kiếm hình ảnh",
        variant: "destructive",
      });
      return;
    }

    setIsSearchingImage(true);
    try {
      // Lấy thêm metadata để cải thiện tìm kiếm
      const productGroupId = form.getValues("productGroupId");
      const description = form.getValues("description");
      const unit = form.getValues("unit");
      
      // Tìm tên nhóm sản phẩm
      const selectedGroup = productGroups.find(g => 
        String(g.productGroupId ?? g.ProductGroupId) === productGroupId
      );
      
      // Tìm nhiều hình ảnh để người dùng lựa chọn
      const response = await fetch('http://101.53.9.76:5273/api/products/search-images', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
          productName: productName,
          productGroupName: selectedGroup?.name || null,
          description: description || null,
          unit: unit || null,
          limit: 6 // Lấy 6 hình để người dùng chọn
        })
      });
      
      if (!response.ok) throw new Error('Tìm kiếm thất bại');
      
      const data = await response.json();
      const images = data.images || [];
      
      if (images.length === 0) {
        throw new Error('Không tìm thấy hình ảnh phù hợp');
      }
      
      if (images.length === 1) {
        // Nếu chỉ có 1 hình, tự động chọn
        form.setValue("image", images[0]);
        toast({
          title: "Thành công",
          description: "Đã tìm và chọn hình ảnh tự động cho sản phẩm",
        });
      } else {
        // Nếu có nhiều hình, cho phép người dùng chọn
        setAvailableImages(images);
        setShowImageSelector(true);
      }
    } catch (error) {
      toast({
        title: "Lỗi",
        description: error.message || "Không thể tìm kiếm hình ảnh phù hợp. Vui lòng thử lại sau.",
        variant: "destructive",
      });
    } finally {
      setIsSearchingImage(false);
    }
  };

  const handleImageSelect = (imageUrl: string) => {
    form.setValue("image", imageUrl);
    setShowImageSelector(false);
    setAvailableImages([]);
    toast({
      title: "Thành công", 
      description: "Đã chọn hình ảnh cho sản phẩm",
    });
  };

  // Handle form submission
  const onSubmit = (data: ProductFormData) => {
    if (editingProduct) {
      // Đảm bảo lấy đúng id (string hoặc số đều được, backend nhận uuid hoặc int)
      const productId = editingProduct.id || editingProduct.productId || editingProduct._id;
      if (!productId) {
        toast({ title: 'Lỗi', description: 'Không xác định được ID sản phẩm để cập nhật', variant: 'destructive' });
        return;
      }
      editProductMutation.mutate({ id: String(productId), data });
    } else {
      // Loại bỏ categoryId nếu không cần thiết, ép productGroupId về số
      const { categoryId, ...rest } = data;
      const payload = { ...rest, productGroupId: Number(data.productGroupId) };
      addProductMutation.mutate(payload);
    }
  };

  // Handle edit product
  const handleEditProduct = (product: Product) => {
    setEditingProduct(product);
    form.reset({
      name: product.name,
      description: product.description || "",
      barcode: product.barcode || "",
      price: Number(product.price),
      costPrice: product.costPrice ? Number(product.costPrice) : 0,
      categoryId: product.categoryId ? String(product.categoryId) : "",
      storeId: product.storeId,
      stockQuantity: Number(product.stockQuantity),
      minStockLevel: Number(product.minStockLevel),
      unit: product.unit,
      image: product.image || "",
      isFeatured: Boolean(product.isFeatured),
    });
    setIsAddDialogOpen(true);
  };

  // Handle delete product
  const handleDeleteProduct = (id: string) => {
    if (confirm("Bạn có chắc chắn muốn xóa sản phẩm này?")) {
      deleteProductMutation.mutate(id);
    }
  };

  // Get stock status
  const getStockStatus = (product: Product) => {
    if (product.stockQuantity === 0) {
      return { label: "Hết hàng", color: "bg-red-500" };
    } else if (product.stockQuantity <= product.minStockLevel) {
      return { label: "Sắp hết", color: "bg-orange-500" };
    } else {
      return { label: "Còn hàng", color: "bg-green-500" };
    }
  };

  return (
    <AppLayout title="Sản phẩm">
      <div data-testid="products-page">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center space-x-4">
            <div className="relative w-80">
              <Input
                placeholder="Tìm kiếm sản phẩm (có thể gõ không dấu)..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
                data-testid="input-product-search"
              />
              <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
            </div>
            <Select value={selectedCategory} onValueChange={setSelectedCategory}>
              <SelectTrigger className="w-48" data-testid="select-category-filter">
                <SelectValue placeholder="Tất cả danh mục" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả danh mục</SelectItem>
                {productGroupOptions.map((option) => (
  <SelectItem key={option.value} value={option.value}>
    {option.label}
  </SelectItem>
))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex items-center space-x-2">
            {/* Export All Products Button */}
            <Button
              variant="outline"
              onClick={() => exportAllProductsMutation.mutate()}
              disabled={exportAllProductsMutation.isPending}
              data-testid="button-export-all-products"
            >
              <Download className="w-4 h-4 mr-2" />
              {exportAllProductsMutation.isPending ? "Đang xuất..." : "Xuất dữ liệu"}
            </Button>
            
            {/* Export Template Button */}
            <Button
              variant="outline"
              onClick={() => exportTemplateMutation.mutate()}
              disabled={exportTemplateMutation.isPending}
              data-testid="button-export-template"
            >
              <Download className="w-4 h-4 mr-2" />
              {exportTemplateMutation.isPending ? "Đang xuất..." : "Xuất Template"}
            </Button>

            {/* Import Excel Button */}
            <Dialog open={isImportDialogOpen} onOpenChange={setIsImportDialogOpen}>
              <DialogTrigger asChild>
                <Button variant="outline" data-testid="button-import-excel">
                  <Upload className="w-4 h-4 mr-2" />
                  Nhập từ Excel
                </Button>
              </DialogTrigger>
              <DialogContent className="max-w-md">
                <DialogHeader>
                  <DialogTitle>Nhập sản phẩm từ Excel</DialogTitle>
                </DialogHeader>
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium mb-2">Chọn file Excel</label>
                    <Input
                      type="file"
                      accept=".xlsx,.xls"
                      onChange={(e) => setImportFile(e.target.files?.[0] || null)}
                      data-testid="input-excel-file"
                    />
                  </div>
                  <div className="text-sm text-gray-600">
                    <p>Lưu ý:</p>
                    <ul className="list-disc list-inside space-y-1">
                      <li>Chỉ hỗ trợ file .xlsx và .xls</li>
                      <li>Tải template mẫu để biết định dạng chính xác</li>
                      <li>Các trường có dấu (*) là bắt buộc</li>
                      <li><strong>Sản phẩm trùng tên hoặc mã vạch sẽ bị bỏ qua</strong></li>
                      <li>Chỉ thêm mới sản phẩm chưa tồn tại</li>
                    </ul>
                  </div>
                  <div className="flex justify-end space-x-2">
                    <Button variant="outline" onClick={() => setIsImportDialogOpen(false)}>
                      Hủy
                    </Button>
                    <Button
                      onClick={() => {
                        if (importFile) {
                          importFromExcelMutation.mutate(importFile);
                        }
                      }}
                      disabled={!importFile || importFromExcelMutation.isPending}
                      data-testid="button-confirm-import"
                    >
                      {importFromExcelMutation.isPending ? "Đang nhập..." : "Nhập dữ liệu"}
                    </Button>
                  </div>
                </div>
              </DialogContent>
            </Dialog>

            {/* Add Product Button */}
            <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
            <DialogTrigger asChild>
              <Button
                onClick={() => {
                  setEditingProduct(null);
                  form.reset();
                }}
                data-testid="button-add-product"
              >
                <Plus className="w-4 h-4 mr-2" />
                Thêm sản phẩm
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
              <DialogHeader>
                <DialogTitle>
                  {editingProduct ? "Chỉnh sửa sản phẩm" : "Thêm sản phẩm mới"}
                </DialogTitle>
              </DialogHeader>
              <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="name"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Tên sản phẩm *</FormLabel>
                          <FormControl>
                            <Input {...field} placeholder="Tên sản phẩm" data-testid="input-product-name" />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="barcode"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Mã vạch</FormLabel>
                          <FormControl>
                            <Input {...field} placeholder="Mã vạch" data-testid="input-product-barcode" />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="productGroupId"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Nhóm sản phẩm</FormLabel>
                          <Select onValueChange={val => field.onChange(String(val))} value={field.value ? String(field.value) : ""}>
                            <FormControl>
                              <SelectTrigger data-testid="select-product-group">
                                <SelectValue placeholder="Chọn nhóm sản phẩm" />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent>
                              {productGroupOptions.length === 0 ? (
                                <div className="px-4 py-2 text-gray-500">Không có nhóm sản phẩm nào</div>
                              ) : (
                                productGroupOptions.map((option) => (
                                  <SelectItem key={option.value} value={option.value}>
                                    {option.label}
                                  </SelectItem>
                                ))
                              )}
                            </SelectContent>
                          </Select>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="price"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Giá bán *</FormLabel>
                          <FormControl>
                            <Input {...field} type="number" placeholder="0" data-testid="input-product-price" onChange={e => field.onChange(e.target.value === '' ? '' : Number(e.target.value))} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="costPrice"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Giá vốn</FormLabel>
                          <FormControl>
                            <Input {...field} type="number" placeholder="0" data-testid="input-product-costPrice" onChange={e => field.onChange(e.target.value === '' ? '' : Number(e.target.value))} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="stockQuantity"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Số lượng tồn kho</FormLabel>
                          <FormControl>
                            <Input {...field} type="number" placeholder="0" data-testid="input-product-stockQuantity" onChange={e => field.onChange(e.target.value === '' ? '' : Number(e.target.value))} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="minStockLevel"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Mức tồn kho tối thiểu</FormLabel>
                          <FormControl>
                            <Input {...field} type="number" placeholder="0" data-testid="input-product-minStockLevel" onChange={e => field.onChange(e.target.value === '' ? '' : Number(e.target.value))} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="unit"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Đơn vị</FormLabel>
                          <FormControl>
                            <Input {...field} placeholder="Đơn vị" data-testid="input-product-unit" />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    {/* Upload hình ảnh */}
                    <FormField
                      control={form.control}
                      name="image"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Hình ảnh</FormLabel>
                          <FormControl>
                            <div className="space-y-2">
                              <div className="flex gap-2">
                                <Input
                                  type="file"
                                  accept="image/*"
                                  onChange={async (e) => {
                                    const file = e.target.files?.[0];
                                    if (file) {
                                      try {
                                        const url = await uploadImage(file);
                                        field.onChange(url);
                                        toast({ title: "Tải ảnh thành công", description: "Ảnh đã được upload." });
                                      } catch {
                                        toast({ title: "Lỗi", description: "Tải ảnh thất bại", variant: "destructive" });
                                      }
                                    }
                                  }}
                                  className="flex-1"
                                />
                                <Button
                                  type="button"
                                  variant="outline"
                                  onClick={handleAIImageSearch}
                                  disabled={isSearchingImage}
                                  className="whitespace-nowrap"
                                >
                                  {isSearchingImage ? "Đang tìm..." : "🤖 AI Tìm ảnh"}
                                </Button>
                              </div>
                              {typeof field.value === "string" && field.value && (
                                <img src={field.value} alt="preview" style={{ maxWidth: 120, marginTop: 8 }} />
                              )}
                            </div>
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="description"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Mô tả</FormLabel>
                          <FormControl>
                            <Textarea {...field} placeholder="Mô tả sản phẩm" data-testid="input-product-description" />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    
                    {/* Checkbox sản phẩm hay bán */}
                    <FormField
                      control={form.control}
                      name="isFeatured"
                      render={({ field }) => (
                        <FormItem className="flex items-center justify-between rounded-lg border p-3 shadow-sm">
                          <div className="space-y-0.5">
                            <FormLabel className="text-base">
                              Sản phẩm hay bán
                            </FormLabel>
                            <div className="text-sm text-muted-foreground">
                              Hiển thị sản phẩm này trong tab "Sản phẩm hay bán" tại trang bán hàng
                            </div>
                          </div>
                          <FormControl>
                            <Switch
                              checked={field.value}
                              onCheckedChange={field.onChange}
                              data-testid="switch-product-featured"
                            />
                          </FormControl>
                        </FormItem>
                      )}
                    />
                  </div>
                  <div className="flex justify-end space-x-2">
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => setIsAddDialogOpen(false)}
                      data-testid="button-cancel"
                    >
                      Hủy
                    </Button>
                    <Button
                      type="button"
                      disabled={addProductMutation.isPending || editProductMutation.isPending}
                      data-testid="button-save-product"
                      onClick={() => {
                        const formData = form.getValues();
                        form.trigger().then(isValid => {
                          if (isValid) {
                            onSubmit(formData);
                          }
                        });
                      }}
                    >
                      {addProductMutation.isPending || editProductMutation.isPending 
                        ? "Đang lưu..." 
                        : (editingProduct ? "Cập nhật" : "Thêm")
                      }
                    </Button>
                  </div>
                </form>
              </Form>
            </DialogContent>
          </Dialog>

          {/* AI Image Selector Dialog */}
          <Dialog open={showImageSelector} onOpenChange={setShowImageSelector}>
            <DialogContent className="max-w-4xl">
              <DialogHeader>
                <DialogTitle>Chọn hình ảnh cho sản phẩm</DialogTitle>
              </DialogHeader>
              <div className="grid grid-cols-2 md:grid-cols-3 gap-4 max-h-[60vh] overflow-y-auto">
                {availableImages.map((imageUrl, index) => (
                  <div
                    key={index}
                    className="relative cursor-pointer rounded-lg overflow-hidden hover:ring-2 hover:ring-blue-500 transition-all"
                    onClick={() => handleImageSelect(imageUrl)}
                  >
                    <img 
                      src={imageUrl} 
                      alt={`Option ${index + 1}`}
                      className="w-full h-32 object-cover"
                      onError={(e) => {
                        e.target.style.display = 'none';
                      }}
                    />
                    <div className="absolute inset-0 bg-black bg-opacity-0 hover:bg-opacity-20 transition-all flex items-center justify-center">
                      <div className="hidden hover:block bg-white text-black px-2 py-1 rounded text-sm">
                        Chọn ảnh này
                      </div>
                    </div>
                  </div>
                ))}
              </div>
              <div className="flex justify-end space-x-2">
                <Button 
                  variant="outline" 
                  onClick={() => {
                    setShowImageSelector(false);
                    setAvailableImages([]);
                  }}
                >
                  Hủy
                </Button>
              </div>
            </DialogContent>
          </Dialog>
        </div>
        </div>

        {/* Products Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {isLoading ? (
            // Loading skeleton
            [...Array(8)].map((_, i) => (
              <Card key={i} className="animate-pulse">
                <CardContent className="p-4">
                  <div className="w-full h-48 bg-gray-200 rounded-lg mb-4"></div>
                  <div className="h-4 bg-gray-200 rounded mb-2"></div>
                  <div className="h-4 bg-gray-200 rounded w-3/4"></div>
                </CardContent>
              </Card>
            ))
          ) : products.length === 0 ? (
            <Card className="col-span-full">
              <CardContent className="p-12 text-center">
                <Package className="w-16 h-16 mx-auto mb-4 text-gray-400" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">
                  Không tìm thấy sản phẩm
                </h3>
                <p className="text-gray-500">
                  {searchTerm || selectedCategory !== "all" 
                    ? "Thử thay đổi bộ lọc tìm kiếm"
                    : "Bắt đầu bằng cách thêm sản phẩm đầu tiên"
                  }
                </p>
              </CardContent>
            </Card>
          ) : (
            products.map((product) => {
              const stockStatus = getStockStatus(product);
              // Ưu tiên imageUrl, sau đó đến image, cuối cùng là ảnh mặc định
              let imageUrl = product.imageUrl || product.image || "https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=300&h=200&fit=crop";
              if (imageUrl && typeof imageUrl === "string" && !imageUrl.startsWith("http")) {
                imageUrl = `http://101.53.9.76:5273${imageUrl}`;
              }
              const key = product.productId || product.id;
              return (
                <Card key={key} className="overflow-hidden" data-testid={`product-card-${key}`}> 
                  <CardContent className="p-0">
                    <div className="relative">
                      <div className="w-full h-48 bg-gray-100 flex items-center justify-center overflow-hidden">
                        <img
                          src={imageUrl}
                          alt={product.name}
                          className="max-w-full max-h-full object-contain"
                          style={{ width: '100%', height: '100%' }}
                        />
                      </div>
                      <Badge
                        className={`absolute top-2 right-2 text-white ${stockStatus.color}`}
                        data-testid={`stock-status-${key}`}
                      >
                        {stockStatus.label}
                      </Badge>
                      {product.stockQuantity <= product.minStockLevel && (
                        <AlertTriangle className="absolute top-2 left-2 w-5 h-5 text-orange-500" />
                      )}
                    </div>
                    <div className="p-4">
                      <h3 className="font-semibold text-lg mb-1 line-clamp-2" data-testid={`product-name-${key}`}>
                        {product.name}
                      </h3>
                      {/* Đã xóa hiển thị SKU */}
                      <div className="flex items-center justify-between mb-3">
                        <div>
                          <p className="text-2xl font-bold text-primary" data-testid={`product-price-${key}`}>
                            {parseInt(product.price).toLocaleString('vi-VN')}₫
                          </p>
                          {product.costPrice && (
                            <p className="text-sm text-gray-500">
                              Vốn: {parseInt(product.costPrice).toLocaleString('vi-VN')}₫
                            </p>
                          )}
                        </div>
                        <div className="text-right">
                          <p className="text-sm font-medium" data-testid={`product-stock-${key}`}>
                            Tồn: {product.stockQuantity} {product.unit}
                          </p>
                          <p className="text-xs text-gray-500">
                            Tối thiểu: {product.minStockLevel}
                          </p>
                        </div>
                      </div>
                      <div className="flex space-x-2">
                        <Button
                          variant="outline"
                          size="sm"
                          className="flex-1"
                          onClick={() => handleEditProduct(product)}
                          data-testid={`button-edit-${key}`}
                        >
                          <Edit className="w-4 h-4 mr-2" />
                          Sửa
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          className="text-red-600 hover:text-red-700"
                          onClick={() => handleDeleteProduct(key)}
                          data-testid={`button-delete-${key}`}
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              );
            })
          )}
        </div>

        {/* Pagination Controls */}
        {totalPages > 1 && (
          <div className="mt-8 flex flex-col items-center space-y-4">
            <div className="text-sm text-gray-600">
              Hiển thị trang {currentPage} / {totalPages} ({totalCount} sản phẩm)
            </div>
            
            <Pagination>
              <PaginationContent>
                <PaginationItem>
                  <PaginationPrevious 
                    onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                    className={currentPage === 1 ? "pointer-events-none opacity-50" : "cursor-pointer"}
                  />
                </PaginationItem>
                
                {/* Page numbers */}
                {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                  const pageNumber = Math.max(1, Math.min(totalPages, currentPage - 2 + i));
                  return (
                    <PaginationItem key={pageNumber}>
                      <PaginationLink 
                        onClick={() => setCurrentPage(pageNumber)}
                        isActive={currentPage === pageNumber}
                        className="cursor-pointer"
                      >
                        {pageNumber}
                      </PaginationLink>
                    </PaginationItem>
                  );
                })}
                
                {totalPages > 5 && currentPage < totalPages - 2 && (
                  <PaginationItem>
                    <PaginationEllipsis />
                  </PaginationItem>
                )}
                
                <PaginationItem>
                  <PaginationNext 
                    onClick={() => setCurrentPage(Math.min(totalPages, currentPage + 1))}
                    className={currentPage === totalPages ? "pointer-events-none opacity-50" : "cursor-pointer"}
                  />
                </PaginationItem>
              </PaginationContent>
            </Pagination>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
