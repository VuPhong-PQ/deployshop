import { useState } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useToast } from "@/hooks/use-toast";
import { apiRequest } from "@/lib/queryClient";
import { queryClient } from "@/lib/queryClient";
import { normalizeSearchText } from "@/lib/utils";
import {
  Folder,
  FolderPlus,
  Search,
  MoreVertical,
  Edit,
  Trash2,
  Tag,
  Package,
  Grid3X3,
  Eye,
  EyeOff,
  Download,
  Upload
} from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

const productGroupFormSchema = z.object({
  name: z.string().min(1, "Tên nhóm là bắt buộc"),
  description: z.string().optional(),
  isActive: z.boolean().default(true),
  sortOrder: z.number().default(0),
  color: z.string().optional()
});

type ProductGroupFormData = z.infer<typeof productGroupFormSchema>;

const getCategoryColor = (color?: string) => {
  const colors: { [key: string]: string } = {
    blue: "bg-blue-100 text-blue-800 border-blue-200",
    green: "bg-green-100 text-green-800 border-green-200",
    purple: "bg-purple-100 text-purple-800 border-purple-200",
    red: "bg-red-100 text-red-800 border-red-200",
    yellow: "bg-yellow-100 text-yellow-800 border-yellow-200",
    gray: "bg-gray-100 text-gray-800 border-gray-200"
  };
  return colors[color || "gray"] || colors.gray;
};

export default function ProductGroups() {
  const { toast } = useToast();
  const [searchTerm, setSearchTerm] = useState("");
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [isImportDialogOpen, setIsImportDialogOpen] = useState(false);
  const [editingGroup, setEditingGroup] = useState<any | null>(null);
  const [importFile, setImportFile] = useState<File | null>(null);

  // Fetch product groups
  const { data: productGroups = [], isLoading } = useQuery({
    queryKey: ['/api/productgroups'],
  }) as { data: any[]; isLoading: boolean };

  // Fetch products to count items per category
  const { data: products = [] } = useQuery({
    queryKey: ['/api/products'],
  }) as { data: any[] };

  // Form
  const form = useForm<ProductGroupFormData>({
    resolver: zodResolver(productGroupFormSchema),
    defaultValues: {
      name: "",
      description: "",
      isActive: true,
      sortOrder: 0,
      color: "blue"
    },
  });

  // Mutations
  const addProductGroupMutation = useMutation({
    mutationFn: async (groupData: ProductGroupFormData) => {
      const response = await apiRequest('/api/productgroups', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: groupData.name,
          description: groupData.description,
          color: groupData.color,
          order: groupData.sortOrder,
          isVisible: groupData.isActive,
        })
      });
      return typeof response === 'string' ? JSON.parse(response) : response;
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Nhóm sản phẩm đã được thêm thành công",
      });
      queryClient.invalidateQueries({ queryKey: ['/api/productgroups'] });
      setIsAddDialogOpen(false);
      form.reset();
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể thêm nhóm sản phẩm",
        variant: "destructive",
      });
    }
  });

  const updateProductGroupMutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: Partial<ProductGroupFormData> }) => {
      const response = await fetch(`/api/productgroups/${id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: data.name,
          description: data.description,
          color: data.color,
          order: data.sortOrder,
          isVisible: data.isActive,
        }),
      });
      return response.ok;
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Nhóm sản phẩm đã được cập nhật",
      });
      queryClient.invalidateQueries({ queryKey: ['/api/productgroups'] });
      setEditingGroup(null);
      setIsAddDialogOpen(false);
      form.reset();
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể cập nhật nhóm sản phẩm",
        variant: "destructive",
      });
    }
  });

  const deleteProductGroupMutation = useMutation({
    mutationFn: async (id: number) => {
      const response = await fetch(`/api/productgroups/${id}`, {
        method: 'DELETE'
      });
      return response.ok;
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Nhóm sản phẩm đã được xóa",
      });
      queryClient.invalidateQueries({ queryKey: ['/api/productgroups'] });
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể xóa nhóm sản phẩm",
        variant: "destructive",
      });
    }
  });

  // Export template mutation
  const exportTemplateMutation = useMutation({
    mutationFn: async () => {
      const response = await fetch('http://101.53.9.76:5273/api/productgroups/export-template');
      if (!response.ok) {
        throw new Error('Failed to export template');
      }
      return response.blob();
    },
    onSuccess: (blob) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `ProductGroups_Template_${new Date().toISOString().slice(0, 10)}.xlsx`;
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
      const response = await fetch('http://101.53.9.76:5273/api/productgroups/import-excel', {
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
        : `Import thành công ${result.SuccessCount} nhóm sản phẩm!`;
      
      toast({
        title: "Import hoàn tất",
        description: message,
        variant: result.ErrorCount > 0 ? "destructive" : "default",
      });
      
      // Hiển thị chi tiết lỗi nếu có
      if (result.Errors && result.Errors.length > 0) {
        console.error('Import errors:', result.Errors);
      }
      
      queryClient.invalidateQueries({ queryKey: ['/api/productgroups'] });
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

  // Filter categories with Vietnamese diacritics support
  const filteredGroups = productGroups.filter((group: any) => {
    const searchNormalized = normalizeSearchText(searchTerm);
    const groupNameNormalized = normalizeSearchText(group.name || '');
    const groupDescNormalized = normalizeSearchText(group.description || '');
    
    return groupNameNormalized.includes(searchNormalized) ||
           groupDescNormalized.includes(searchNormalized);
  });

  const onSubmit = (data: ProductGroupFormData) => {
    if (editingGroup) {
      const id = (editingGroup.productGroupId ?? editingGroup.ProductGroupId ?? editingGroup.id ?? "").toString();
      if (!id || id === "undefined" || id === "null") {
        toast({ title: "Lỗi", description: "Không xác định được ID nhóm sản phẩm!", variant: "destructive" });
        return;
      }
      updateProductGroupMutation.mutate({ id: Number(id), data });
    } else {
      addProductGroupMutation.mutate(data);
    }
  };

  const handleEditGroup = (group: any) => {
    setEditingGroup(group);
    form.reset({
      name: group.name || "",
      description: group.description || "",
      isActive: group.isVisible ?? true,
      sortOrder: group.order || 0,
      color: group.color || "blue"
    });
    setIsAddDialogOpen(true);
  };

  const handleDeleteGroup = (idOrGroup: any) => {
    let id = idOrGroup;
    if (typeof idOrGroup === 'object' && idOrGroup !== null) {
      id = idOrGroup.productGroupId ?? idOrGroup.ProductGroupId ?? idOrGroup.id;
    }
    const productsInGroup = Array.isArray(products) ? products.filter((p: any) => p.productGroupId === id || p.productGroupId === Number(id)).length : 0;
    if (productsInGroup > 0) {
      toast({
        title: "Không thể xóa",
        description: `Nhóm này còn ${productsInGroup} sản phẩm. Hãy di chuyển sản phẩm trước khi xóa.`,
        variant: "destructive",
      });
      return;
    }
    if (confirm("Bạn có chắc muốn xóa nhóm sản phẩm này?")) {
      deleteProductGroupMutation.mutate(Number(id));
    }
  };

  const getProductCount = (groupId: string) => {
    return Array.isArray(products) ? products.filter((p: any) => (p.productGroupId === groupId || p.productGroupId === Number(groupId))).length : 0;
  };

  return (
    <AppLayout title="Nhóm sản phẩm">
      <div className="space-y-6" data-testid="product-groups-page">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Nhóm sản phẩm</h1>
            <p className="text-gray-600">Quản lý danh mục và phân loại sản phẩm</p>
          </div>

          <div className="flex space-x-2">
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
                  <DialogTitle>Nhập nhóm sản phẩm từ Excel</DialogTitle>
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
                      <li><strong>Nhóm trùng tên sẽ bị bỏ qua</strong></li>
                      <li>Chỉ thêm mới nhóm chưa tồn tại</li>
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

            <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
              <DialogTrigger asChild>
                <Button 
                  onClick={() => {
                    setEditingGroup(null);
                    form.reset();
                  }}
                  data-testid="button-add-group"
                >
                  <FolderPlus className="w-4 h-4 mr-2" />
                  Thêm nhóm mới
                </Button>
              </DialogTrigger>
              <DialogContent className="max-w-md">
                <DialogHeader>
                  <DialogTitle>
                    {editingGroup ? "Chỉnh sửa nhóm sản phẩm" : "Thêm nhóm sản phẩm mới"}
                  </DialogTitle>
                </DialogHeader>
                
                <Form {...form}>
                  <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                    <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Tên nhóm *</FormLabel>
                        <FormControl>
                          <Input {...field} placeholder="Ví dụ: Điện thoại" data-testid="input-category-name" />
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
                          <Textarea {...field} rows={3} placeholder="Mô tả về nhóm sản phẩm" data-testid="textarea-category-description" />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div className="grid grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="color"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Màu sắc</FormLabel>
                          <FormControl>
                            <select {...field} className="w-full px-3 py-2 border border-gray-300 rounded-md" data-testid="select-category-color">
                              <option value="blue">Xanh dương</option>
                              <option value="green">Xanh lá</option>
                              <option value="purple">Tím</option>
                              <option value="red">Đỏ</option>
                              <option value="yellow">Vàng</option>
                              <option value="gray">Xám</option>
                            </select>
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="sortOrder"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Thứ tự</FormLabel>
                          <FormControl>
                            <Input {...field} type="number" onChange={(e) => field.onChange(Number(e.target.value))} data-testid="input-category-order" />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  <FormField
                    control={form.control}
                    name="isActive"
                    render={({ field }) => (
                      <FormItem className="flex items-center justify-between rounded-lg border p-4">
                        <div className="space-y-0.5">
                          <FormLabel className="text-base">Hiển thị</FormLabel>
                          <p className="text-sm text-muted-foreground">Cho phép hiển thị nhóm này</p>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            data-testid="switch-category-active"
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />

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
                      type="submit"
                      disabled={addProductGroupMutation.isPending || updateProductGroupMutation.isPending}
                      data-testid="button-save-group"
                    >
                      {addProductGroupMutation.isPending || updateProductGroupMutation.isPending 
                        ? "Đang lưu..." 
                        : (editingGroup ? "Cập nhật" : "Thêm nhóm")
                      }
                    </Button>
                  </div>
                </form>
              </Form>
            </DialogContent>
          </Dialog>
          </div>
        </div>

        {/* Search */}
        <Card>
          <CardContent className="p-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Tìm nhóm sản phẩm (có thể gõ không dấu)..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-9"
                data-testid="input-search-categories"
              />
            </div>
          </CardContent>
        </Card>

        {/* Product Groups Grid */}
        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {[...Array(8)].map((_, i) => (
              <Card key={i} className="animate-pulse">
                <CardContent className="p-6">
                  <div className="space-y-3">
                    <div className="h-5 bg-gray-200 rounded w-3/4"></div>
                    <div className="h-4 bg-gray-200 rounded w-full"></div>
                    <div className="h-4 bg-gray-200 rounded w-1/2"></div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        ) : filteredGroups.length === 0 ? (
          <Card>
            <CardContent className="p-12 text-center">
              <Folder className="w-16 h-16 mx-auto mb-4 text-gray-400" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">
                {searchTerm ? "Không tìm thấy nhóm sản phẩm" : "Chưa có nhóm sản phẩm"}
              </h3>
              <p className="text-gray-500 mb-4">
                {searchTerm 
                  ? "Thử thay đổi từ khóa tìm kiếm"
                  : "Bắt đầu bằng cách tạo nhóm sản phẩm đầu tiên"
                }
              </p>
              <Button onClick={() => setIsAddDialogOpen(true)}>
                <FolderPlus className="w-4 h-4 mr-2" />
                Thêm nhóm mới
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {filteredGroups.map((group: any) => {
              const groupId = group.ProductGroupId ?? group.productGroupId ?? group.id;
              const productCount = getProductCount(groupId);
              return (
                <Card key={groupId} className="hover:shadow-md transition-shadow" data-testid={`group-card-${groupId}`}>
                  <CardContent className="p-6">
                    <div className="flex items-start justify-between mb-4">
                      <div className="flex items-center space-x-3">
                        <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${getCategoryColor(group.color)}`}>
                          <Folder className="w-5 h-5" />
                        </div>
                        <div className="flex-1">
                          <h3 className="font-semibold text-gray-900 truncate">{group.name}</h3>
                          <div className="flex items-center space-x-2 mt-1">
                            <Badge variant="outline" className="text-xs">
                              <Package className="w-3 h-3 mr-1" />
                              {productCount} sản phẩm
                            </Badge>
                            {group.isVisible ? (
                              <Badge variant="outline" className="text-xs text-green-600">
                                <Eye className="w-3 h-3 mr-1" />
                                Hiển thị
                              </Badge>
                            ) : (
                              <Badge variant="secondary" className="text-xs">
                                <EyeOff className="w-3 h-3 mr-1" />
                                Ẩn
                              </Badge>
                            )}
                          </div>
                        </div>
                      </div>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" className="h-8 w-8 p-0">
                            <MoreVertical className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent>
                          <DropdownMenuItem onClick={() => handleEditGroup(group)}>
                            <Edit className="mr-2 h-4 w-4" />
                            Chỉnh sửa
                          </DropdownMenuItem>
                          <DropdownMenuItem 
                            onClick={() => handleDeleteGroup(group)}
                            className="text-red-600"
                            disabled={productCount > 0}
                          >
                            <Trash2 className="mr-2 h-4 w-4" />
                            Xóa
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>

                    {group.description && (
                      <p className="text-sm text-gray-600 mb-3 line-clamp-2">{group.description}</p>
                    )}

                    <div className="flex items-center justify-between text-sm text-gray-500">
                      <span>Thứ tự: {group.order || 0}</span>
                      <div className="flex items-center space-x-1">
                        <Grid3X3 className="w-3 h-3" />
                        <span>#{typeof groupId === 'string' ? groupId.slice(-4) : String(groupId ?? '').slice(-4)}</span>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}

        {/* Summary Stats */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center space-x-2">
                <Folder className="w-5 h-5 text-blue-600" />
                <div>
                  <p className="text-sm font-medium">Tổng nhóm</p>
                  <p className="text-2xl font-bold">{productGroups.length}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center space-x-2">
                <Eye className="w-5 h-5 text-green-600" />
                <div>
                  <p className="text-sm font-medium">Đang hiển thị</p>
                  <p className="text-2xl font-bold">{productGroups.filter((g: any) => g.isVisible).length}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center space-x-2">
                <Package className="w-5 h-5 text-purple-600" />
                <div>
                  <p className="text-sm font-medium">Tổng sản phẩm</p>
                  <p className="text-2xl font-bold">{Array.isArray(products) ? products.length : 0}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center space-x-2">
                <Tag className="w-5 h-5 text-orange-600" />
                <div>
                  <p className="text-sm font-medium">Nhóm phổ biến</p>
                  <p className="text-lg font-bold">
                    {productGroups.length > 0 
                      ? productGroups.reduce((max: any, g: any) => 
                          getProductCount(g.ProductGroupId ?? g.productGroupId ?? g.id) > getProductCount(max.ProductGroupId ?? max.productGroupId ?? max.id) ? g : max, 
                          productGroups[0]
                        )?.name?.slice(0, 10) + "..."
                      : "N/A"
                    }
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </AppLayout>
  );
}
