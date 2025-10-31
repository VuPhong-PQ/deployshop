import { useState, useEffect } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { Checkbox } from "@/components/ui/checkbox";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useToast } from "@/hooks/use-toast";
import { apiRequest } from "@/lib/queryClient";
import { queryClient } from "@/lib/queryClient";
import {
  Users,
  UserPlus,
  Settings,
  Search,
  Filter,
  MoreVertical,
  Edit,
  Trash2,
  Clock,
  Shield,
  UserCheck,
  UserX,
  Calendar,
  Mail,
  Phone,
  MapPin,
  Award,
  User,
  Package
} from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

const staffFormSchema = z.object({
  fullName: z.string().min(1, "Họ tên là bắt buộc"),
  username: z.string().min(1, "Tên đăng nhập là bắt buộc"),
  password: z.string().optional(),
  email: z.string().email("Email không hợp lệ").optional(),
  phoneNumber: z.string().optional(),
  roleId: z.number().min(1, "Vai trò là bắt buộc"),
  isActive: z.boolean().default(true),
  notes: z.string().optional(),
  assignedStores: z.array(z.number()).optional()
}).refine((data) => {
  // For new staff, password is required and must be at least 6 chars
  // For editing staff, password is optional but if provided must be at least 6 chars
  if (data.password && data.password.length > 0) {
    return data.password.length >= 6;
  }
  return true;
}, {
  message: "Mật khẩu tối thiểu 6 ký tự",
  path: ["password"]
});

const roleFormSchema = z.object({
  roleName: z.string().min(1, "Tên vai trò là bắt buộc").max(50, "Tên vai trò tối đa 50 ký tự"),
  description: z.string().max(200, "Mô tả tối đa 200 ký tự").optional(),
  permissionIds: z.array(z.number()).optional()
});

type StaffFormData = z.infer<typeof staffFormSchema>;
type RoleFormData = z.infer<typeof roleFormSchema>;

const getRoleBadgeColor = (roleName: string) => {
  const colors: { [key: string]: string } = {
    "Admin": "bg-red-100 text-red-800",
    "Quản lý": "bg-blue-100 text-blue-800",
    "Thu ngân": "bg-green-100 text-green-800",
    staff: "bg-gray-100 text-gray-800",
    inventory: "bg-orange-100 text-orange-800"
  };
  return colors[roleName] || colors.staff;
};

const getRoleIcon = (role: string) => {
  switch (role) {
    case 'admin': return <Shield className="w-4 h-4" />;
    case 'manager': return <Settings className="w-4 h-4" />;
    case 'cashier': return <UserCheck className="w-4 h-4" />;
    case 'inventory': return <Package className="w-4 h-4" />;
    default: return <Users className="w-4 h-4" />;
  }
};

export default function Staff() {
  const { toast } = useToast();
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedRole, setSelectedRole] = useState<string>("all");
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [editingStaff, setEditingStaff] = useState<any | null>(null);
  const [isConfigDialogOpen, setIsConfigDialogOpen] = useState(false);
  const [selectedConfigRole, setSelectedConfigRole] = useState<any | null>(null);
  
  // Role management state
  const [isRoleDialogOpen, setIsRoleDialogOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<any | null>(null);

  // Fetch data
  const { data: staff = [], isLoading: staffLoading } = useQuery<any[]>({
    queryKey: ['/api/staff'],
    queryFn: async () => {
      const response = await fetch('http://101.53.9.76:5273/api/staff');
      if (!response.ok) throw new Error('Failed to fetch staff');
      return response.json();
    },
  });

  const { data: roles = [], isLoading: rolesLoading } = useQuery<any[]>({
    queryKey: ['/api/role'],
    queryFn: async () => {
      const response = await fetch('http://101.53.9.76:5273/api/role');
      if (!response.ok) throw new Error('Failed to fetch roles');
      return response.json();
    },
  });

  const { data: permissions = [], isLoading: permissionsLoading } = useQuery<any[]>({
    queryKey: ['/api/permission'],
    queryFn: async () => {
      const response = await fetch('http://101.53.9.76:5273/api/permission');
      if (!response.ok) throw new Error('Failed to fetch permissions');
      return response.json();
    },
  });

  // Fetch stores for assignment
  const { data: stores = [], isLoading: storesLoading } = useQuery<any[]>({
    queryKey: ['/api/stores'],
    queryFn: async () => {
      const response = await fetch('http://101.53.9.76:5273/api/stores');
      if (!response.ok) throw new Error('Failed to fetch stores');
      return response.json();
    },
  });

  // Fetch staff-store assignments
  const { data: staffStoreAssignments = [], isLoading: assignmentsLoading } = useQuery<any[]>({
    queryKey: ['/api/staffstores/staff-assignments'],
    queryFn: async () => {
      const response = await fetch('http://101.53.9.76:5273/api/staffstores/staff-assignments');
      if (!response.ok) throw new Error('Failed to fetch staff assignments');
      return response.json();
    },
  });

  const isLoading = staffLoading || rolesLoading || permissionsLoading || storesLoading || assignmentsLoading;

  // Helper function to handle store assignments
  const handleStoreAssignments = async (staffId: number, assignedStores: number[] = []) => {
    try {
      // First, get current assignments
      const currentAssignments = staffStoreAssignments.find((s: any) => s.staffId === staffId)?.assignedStores || [];
      const currentStoreIds = currentAssignments.map((s: any) => s.storeId);
      
      // Find stores to add and remove
      const storesToAdd = assignedStores.filter((storeId: number) => !currentStoreIds.includes(storeId));
      const storesToRemove = currentStoreIds.filter((storeId: number) => !assignedStores.includes(storeId));
      
      // Add new store assignments
      for (const storeId of storesToAdd) {
        const response = await fetch('http://101.53.9.76:5273/api/staffstores/assign', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ staffId, storeId }),
          credentials: 'include'
        });
        
        if (!response.ok) {
          console.error(`Failed to assign store ${storeId} to staff ${staffId}`);
        }
      }
      
      // Remove old store assignments
      for (const storeId of storesToRemove) {
        const response = await fetch(`http://101.53.9.76:5273/api/staffstores/unassign?staffId=${staffId}&storeId=${storeId}`, {
          method: 'DELETE',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include'
        });
        
        if (!response.ok) {
          console.error(`Failed to unassign store ${storeId} from staff ${staffId}`);
        }
      }
      
      // Refresh assignments data
      queryClient.invalidateQueries({ queryKey: ['/api/staffstores/staff-assignments'] });
      
    } catch (error) {
      console.error('Error handling store assignments:', error);
      throw error;
    }
  };

  // Form
  const form = useForm<StaffFormData>({
    resolver: zodResolver(staffFormSchema),
    defaultValues: {
      fullName: "",
      username: "",
      password: "",
      email: "",
      phoneNumber: "",
      roleId: 0,
      isActive: true,
      notes: "",
    },
  });

  // Role form
  const roleForm = useForm<RoleFormData>({
    resolver: zodResolver(roleFormSchema),
    defaultValues: {
      roleName: "",
      description: "",
      permissionIds: [],
    },
  });

  // Mutations
  const addStaffMutation = useMutation({
    mutationFn: async (staffData: StaffFormData) => {
      // Separate store assignments from staff data
      const { assignedStores, ...staffInfo } = staffData;
      
      const response = await fetch('http://101.53.9.76:5273/api/staff', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(staffInfo)
      });
      
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to create staff: ${response.status} - ${errorText}`);
      }
      
      let staffResult = null;
      // Handle 204 No Content response
      if (response.status === 204) {
        // For new staff, we need to get the ID somehow. Let's refetch staff list
        await queryClient.invalidateQueries({ queryKey: ['/api/staff'] });
        // We'll handle store assignments after getting the new staff ID
        return { assignedStores };
      } else {
        staffResult = await response.json();
        return { ...staffResult, assignedStores };
      }
    },
    onSuccess: async (result) => {
      try {
        // Refresh staff data first
        await queryClient.invalidateQueries({ queryKey: ['/api/staff'] });
        
        // If we have store assignments, handle them
        if (result?.assignedStores && result.assignedStores.length > 0) {
          // Find the newly created staff ID
          const updatedStaff = queryClient.getQueryData<any[]>(['/api/staff']);
          if (updatedStaff) {
            const newStaff = updatedStaff[updatedStaff.length - 1]; // Assuming newest is last
            if (newStaff?.staffId) {
              await handleStoreAssignments(newStaff.staffId, result.assignedStores);
            }
          }
        }
        
        toast({
          title: "Thành công",
          description: "Nhân viên đã được thêm thành công",
        });
        setIsAddDialogOpen(false);
        form.reset();
      } catch (error) {
        console.error('Error in post-creation steps:', error);
        toast({
          title: "Cảnh báo", 
          description: "Nhân viên đã được tạo nhưng có lỗi khi phân quyền cửa hàng",
          variant: "destructive"
        });
      }
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể thêm nhân viên",
        variant: "destructive",
      });
    }
  });

  const updateStaffMutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: Partial<StaffFormData> }) => {
      console.log('Updating staff with ID:', id);
      console.log('Data being sent:', data);
      
      // Separate store assignments from staff data
      const { assignedStores, ...staffInfo } = data;
      
      const response = await fetch(`http://101.53.9.76:5273/api/staff/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(staffInfo)
      });
      
      console.log('Response status:', response.status);
      
      if (!response.ok) {
        const errorText = await response.text();
        console.error('Update failed:', response.status, errorText);
        throw new Error(`Failed to update staff: ${response.status} - ${errorText}`);
      }
      
      // Return both staff update result and store assignments
      let result = null;
      if (response.status !== 204) {
        result = await response.json();
      }
      
      return { staffResult: result, assignedStores, staffId: id };
    },
    onSuccess: async (result) => {
      try {
        // Handle store assignments if provided
        if (result?.assignedStores !== undefined) {
          await handleStoreAssignments(result.staffId, result.assignedStores);
        }
        
        toast({
          title: "Thành công",
          description: "Thông tin nhân viên đã được cập nhật",
        });
        queryClient.invalidateQueries({ queryKey: ['/api/staff'] });
        setEditingStaff(null);
        setIsAddDialogOpen(false);
        form.reset();
      } catch (error) {
        console.error('Error in update steps:', error);
        toast({
          title: "Cảnh báo",
          description: "Thông tin nhân viên đã được cập nhật nhưng có lỗi khi phân quyền cửa hàng", 
          variant: "destructive"
        });
      }
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể cập nhật nhân viên",
        variant: "destructive",
      });
    }
  });

  const deleteStaffMutation = useMutation({
    mutationFn: async (id: number) => {
      const response = await fetch(`http://101.53.9.76:5273/api/staff/${id}`, {
        method: 'DELETE'
      });
      if (!response.ok) throw new Error('Failed to delete staff');
      
      // Handle different response types
      if (response.status === 204) {
        // Hard delete - no content
        return { hardDelete: true };
      } else if (response.status === 200) {
        // Soft delete - staff deactivated
        const data = await response.json();
        return { hardDelete: false, ...data };
      }
      return {};
    },
    onSuccess: (data) => {
      if (data?.hardDelete) {
        toast({
          title: "Thành công",
          description: "Nhân viên đã được xóa hoàn toàn",
        });
      } else {
        toast({
          title: "Thành công", 
          description: data?.message || "Nhân viên đã được đánh dấu không hoạt động",
          variant: "default",
        });
      }
      queryClient.invalidateQueries({ queryKey: ['/api/staff'] });
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể xóa nhân viên",
        variant: "destructive",
      });
    }
  });

  // Role management mutations
  const addRoleMutation = useMutation({
    mutationFn: async (roleData: RoleFormData) => {
      const response = await fetch('http://101.53.9.76:5273/api/role', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(roleData)
      });
      
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to create role: ${response.status} - ${errorText}`);
      }
      
      return response.json();
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Vai trò đã được thêm thành công",
      });
      queryClient.invalidateQueries({ queryKey: ['/api/role'] });
      setIsRoleDialogOpen(false);
      roleForm.reset();
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Không thể thêm vai trò",
        variant: "destructive",
      });
    }
  });

  const updateRoleMutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: Partial<RoleFormData> }) => {
      const response = await fetch(`http://101.53.9.76:5273/api/role/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      });
      
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update role: ${response.status} - ${errorText}`);
      }
      
      return response.status === 204 ? null : response.json();
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Vai trò đã được cập nhật thành công",
      });
      queryClient.invalidateQueries({ queryKey: ['/api/role'] });
      setIsRoleDialogOpen(false);
      setEditingRole(null);
      roleForm.reset();
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Không thể cập nhật vai trò",
        variant: "destructive",
      });
    }
  });

  const deleteRoleMutation = useMutation({
    mutationFn: async (id: number) => {
      const response = await fetch(`http://101.53.9.76:5273/api/role/${id}`, {
        method: 'DELETE'
      });
      
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || `Failed to delete role: ${response.status}`);
      }
    },
    onSuccess: () => {
      toast({
        title: "Thành công",
        description: "Vai trò đã được xóa thành công",
      });
      queryClient.invalidateQueries({ queryKey: ['/api/role'] });
    },
    onError: (error: any) => {
      toast({
        title: "Lỗi",
        description: error.message || "Không thể xóa vai trò",
        variant: "destructive",
      });
    }
  });

  // Permission management functions
  const togglePermission = useMutation({
    mutationFn: async ({ roleId, permissionId, action }: { roleId: number, permissionId: number, action: 'add' | 'remove' }) => {
      const url = action === 'add' 
        ? 'http://101.53.9.76:5273/api/role/assign-permission' 
        : 'http://101.53.9.76:5273/api/role/remove-permission';
      
      const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ roleId, permissionId })
      });
      
      if (!response.ok) {
        const errorText = await response.text();
        console.error('API Error:', response.status, errorText);
        throw new Error(`Failed to update permission: ${response.status}`);
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
        description: "Quyền hạn đã được cập nhật",
      });
      // Invalidate queries to refresh data
      queryClient.invalidateQueries({ queryKey: ['/api/role'] });
      queryClient.invalidateQueries({ queryKey: ['/api/permission'] });
      
      // Update selectedConfigRole with fresh data
      if (selectedConfigRole) {
        setTimeout(() => {
          const roles = queryClient.getQueryData(['/api/role']) as any[];
          const updatedRole = roles?.find(r => r.roleId === selectedConfigRole.roleId);
          if (updatedRole) {
            setSelectedConfigRole(updatedRole);
          }
        }, 100);
      }
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể cập nhật quyền hạn",
        variant: "destructive",
      });
    }
  });

  const handlePermissionToggle = (permissionId: number, hasPermission: boolean) => {
    if (!selectedConfigRole) {
      console.error('No role selected for permission toggle');
      return;
    }
    
    const requestData = {
      roleId: selectedConfigRole.roleId,
      permissionId,
      action: (hasPermission ? 'remove' : 'add') as 'add' | 'remove'
    };
    
    console.log('Toggling permission:', requestData);
    console.log('Selected role:', selectedConfigRole);
    
    togglePermission.mutate(requestData);
  };

  const openConfigDialog = (role: any) => {
    console.log('Opening config dialog for role:', role);
    setSelectedConfigRole(role);
    setIsConfigDialogOpen(true);
  };

  // Auto-update selectedConfigRole when roles data changes
  useEffect(() => {
    if (selectedConfigRole && roles.length > 0) {
      const updatedRole = roles.find((r: any) => r.roleId === selectedConfigRole.roleId);
      if (updatedRole) {
        setSelectedConfigRole(updatedRole);
      }
    }
  }, [roles, selectedConfigRole?.roleId]);

  // Select/Deselect all permissions
  const selectAllPermissions = useMutation({
    mutationFn: async () => {
      if (!selectedConfigRole) throw new Error('No role selected');
      
      const currentPermissionIds = selectedConfigRole.permissions?.map((p: any) => p.permissionId) || [];
      const allPermissionIds = permissions.map((p: any) => p.permissionId);
      const toAssign = allPermissionIds.filter(id => !currentPermissionIds.includes(id));
      
      // Assign missing permissions
      for (const permissionId of toAssign) {
        const response = await fetch('http://101.53.9.76:5273/api/role/assign-permission', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ roleId: selectedConfigRole.roleId, permissionId })
        });
        if (!response.ok) throw new Error('Failed to assign permission');
      }
      
      return { assigned: toAssign.length };
    },
    onSuccess: (data) => {
      toast({
        title: "Thành công",
        description: `Đã chọn tất cả quyền hạn (${data.assigned} quyền mới)`,
      });
      queryClient.invalidateQueries({ queryKey: ['/api/role'] });
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể chọn tất cả quyền hạn",
        variant: "destructive",
      });
    }
  });

  const deselectAllPermissions = useMutation({
    mutationFn: async () => {
      if (!selectedConfigRole) throw new Error('No role selected');
      
      const currentPermissionIds = selectedConfigRole.permissions?.map((p: any) => p.permissionId) || [];
      
      // Remove all permissions
      for (const permissionId of currentPermissionIds) {
        const response = await fetch('http://101.53.9.76:5273/api/role/remove-permission', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ roleId: selectedConfigRole.roleId, permissionId })
        });
        if (!response.ok) throw new Error('Failed to remove permission');
      }
      
      return { removed: currentPermissionIds.length };
    },
    onSuccess: (data) => {
      toast({
        title: "Thành công",
        description: `Đã hủy tất cả quyền hạn (${data.removed} quyền)`,
      });
      queryClient.invalidateQueries({ queryKey: ['/api/role'] });
    },
    onError: () => {
      toast({
        title: "Lỗi",
        description: "Không thể hủy tất cả quyền hạn",
        variant: "destructive",
      });
    }
  });

  // Filter staff
  const filteredStaff = staff.filter((member: any) => {
    const matchesSearch = member.fullName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         member.email?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         member.username?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         member.phoneNumber?.includes(searchTerm);
    const matchesRole = selectedRole === "all" || member.roleId?.toString() === selectedRole;
    return matchesSearch && matchesRole;
  });

  const onSubmit = (data: StaffFormData) => {
    console.log('onSubmit called with data:', data);
    console.log('editingStaff:', editingStaff);
    
    // For new staff, password is required
    if (!editingStaff && (!data.password || data.password.length < 6)) {
      toast({
        title: "Lỗi",
        description: "Mật khẩu là bắt buộc và tối thiểu 6 ký tự khi tạo nhân viên mới",
        variant: "destructive",
      });
      return;
    }

    // For updating staff, only send password if it's provided
    if (editingStaff) {
      const updateData = { ...data };
      if (!data.password || data.password.length === 0) {
        delete updateData.password;
      }
      console.log('Calling updateStaffMutation with:', { id: editingStaff.staffId, data: updateData });
      updateStaffMutation.mutate({ id: editingStaff.staffId, data: updateData });
    } else {
      console.log('Calling addStaffMutation with:', data);
      addStaffMutation.mutate(data);
    }
  };

  const handleEditStaff = (member: any) => {
    setEditingStaff(member);
    
    // Get current store assignments for this staff
    const staffAssignment = staffStoreAssignments.find((s: any) => s.staffId === member.staffId);
    const assignedStoreIds = staffAssignment?.assignedStores?.map((store: any) => store.storeId) || [];
    
    form.reset({
      fullName: member.fullName || "",
      username: member.username || "",
      password: "", // Don't populate password for editing
      email: member.email || "",
      phoneNumber: member.phoneNumber || "",
      roleId: member.roleId || 0,
      isActive: member.isActive ?? true,
      notes: member.notes || "",
      assignedStores: assignedStoreIds
    });
    setIsAddDialogOpen(true);
  };

  const handleDeleteStaff = (id: number) => {
    if (confirm("Bạn có chắc muốn xóa nhân viên này?")) {
      deleteStaffMutation.mutate(id);
    }
  };

  // Role management functions
  const onRoleSubmit = (data: RoleFormData) => {
    console.log('onRoleSubmit called with data:', data);
    console.log('editingRole:', editingRole);
    
    if (editingRole) {
      updateRoleMutation.mutate({ id: editingRole.roleId, data });
    } else {
      addRoleMutation.mutate(data);
    }
  };

  const handleDeleteRole = (id: number) => {
    if (confirm("Bạn có chắc muốn xóa vai trò này? Thao tác này không thể hoàn tác.")) {
      deleteRoleMutation.mutate(id);
    }
  };

  // Staff statistics
  const totalStaff = staff.length;
  const activeStaff = staff.filter((member: any) => member.isActive).length;
  const staffByRole = roles.map((role: any) => ({
    ...role,
    count: staff.filter((s: any) => s.roleId === role.roleId).length
  }));

  return (
    <AppLayout title="Quản lý nhân viên">
      <div className="space-y-6" data-testid="staff-page">
        {/* Staff Statistics Overview */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center">
                <div className="p-3 bg-blue-100 rounded-lg">
                  <Users className="w-6 h-6 text-blue-600" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">Tổng nhân viên</p>
                  <p className="text-2xl font-bold text-gray-900">{totalStaff}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center">
                <div className="p-3 bg-green-100 rounded-lg">
                  <UserCheck className="w-6 h-6 text-green-600" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">Đang hoạt động</p>
                  <p className="text-2xl font-bold text-gray-900">{activeStaff}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center">
                <div className="p-3 bg-orange-100 rounded-lg">
                  <Shield className="w-6 h-6 text-orange-600" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">Vai trò</p>
                  <p className="text-2xl font-bold text-gray-900">{roles.length}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        <Tabs defaultValue="list" className="space-y-6">
          <div className="flex items-center justify-between">
            <TabsList>
              <TabsTrigger value="list">Danh sách nhân viên</TabsTrigger>
              <TabsTrigger value="roles">Vai trò & Quyền hạn</TabsTrigger>
              <TabsTrigger value="schedule">Lịch làm việc</TabsTrigger>
            </TabsList>

            <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
              <DialogTrigger asChild>
                <Button 
                  onClick={() => {
                    setEditingStaff(null);
                    form.reset();
                  }}
                  data-testid="button-add-staff"
                >
                  <UserPlus className="w-4 h-4 mr-2" />
                  Thêm nhân viên
                </Button>
              </DialogTrigger>
              <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
                <DialogHeader>
                  <DialogTitle>
                    {editingStaff ? "Chỉnh sửa nhân viên" : "Thêm nhân viên mới"}
                  </DialogTitle>
                </DialogHeader>
                
                <Form {...form}>
                  <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <FormField
                        control={form.control}
                        name="fullName"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Họ và tên *</FormLabel>
                            <FormControl>
                              <Input {...field} data-testid="input-staff-name" />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />

                      <FormField
                        control={form.control}
                        name="username"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Tên đăng nhập *</FormLabel>
                            <FormControl>
                              <Input {...field} data-testid="input-staff-username" />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />

                      <FormField
                        control={form.control}
                        name="password"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>
                              {editingStaff ? "Mật khẩu mới (để trống nếu không đổi)" : "Mật khẩu *"}
                            </FormLabel>
                            <FormControl>
                              <Input 
                                {...field} 
                                type="password" 
                                placeholder={editingStaff ? "Nhập mật khẩu mới..." : "Tối thiểu 6 ký tự"}
                                data-testid="input-staff-password" 
                              />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />

                      <FormField
                        control={form.control}
                        name="email"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Email</FormLabel>
                            <FormControl>
                              <Input {...field} type="email" data-testid="input-staff-email" />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />

                      <FormField
                        control={form.control}
                        name="phoneNumber"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Số điện thoại</FormLabel>
                            <FormControl>
                              <Input {...field} data-testid="input-staff-phone" />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />

                      <FormField
                        control={form.control}
                        name="roleId"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Vai trò *</FormLabel>
                            <Select onValueChange={(value) => field.onChange(parseInt(value))} value={field.value?.toString() || ""}>
                              <FormControl>
                                <SelectTrigger data-testid="select-staff-role">
                                  <SelectValue placeholder="Chọn vai trò" />
                                </SelectTrigger>
                              </FormControl>
                              <SelectContent>
                                {roles.map((role: any) => (
                                  <SelectItem key={role.roleId} value={role.roleId.toString()}>
                                    {role.roleName}
                                  </SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                            <FormMessage />
                          </FormItem>
                        )}
                      />

                      {/* Phân quyền cửa hàng */}
                      <FormField
                        control={form.control}
                        name="assignedStores"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Cửa hàng được phép truy cập</FormLabel>
                            <FormControl>
                              <div className="space-y-2">
                                {stores.map((store: any) => (
                                  <div key={store.storeId} className="flex items-center space-x-2">
                                    <Checkbox
                                      id={`store-${store.storeId}`}
                                      checked={field.value?.includes(store.storeId) || false}
                                      onCheckedChange={(checked) => {
                                        const currentStores = field.value || [];
                                        if (checked) {
                                          field.onChange([...currentStores, store.storeId]);
                                        } else {
                                          field.onChange(currentStores.filter((id: number) => id !== store.storeId));
                                        }
                                      }}
                                      data-testid={`checkbox-store-${store.storeId}`}
                                    />
                                    <Label 
                                      htmlFor={`store-${store.storeId}`}
                                      className="text-sm font-normal cursor-pointer flex-1"
                                    >
                                      <div>
                                        <div className="font-medium">{store.name}</div>
                                        <div className="text-xs text-muted-foreground">{store.address}</div>
                                        <div className="text-xs text-muted-foreground">Quản lý: {store.manager}</div>
                                      </div>
                                    </Label>
                                  </div>
                                ))}
                                {stores.length === 0 && (
                                  <p className="text-sm text-muted-foreground">Không có cửa hàng nào</p>
                                )}
                              </div>
                            </FormControl>
                            <FormMessage />
                            <p className="text-xs text-muted-foreground">
                              Chọn các cửa hàng mà nhân viên này có quyền truy cập. Admin có quyền truy cập tất cả cửa hàng.
                            </p>
                          </FormItem>
                        )}
                      />

                      <FormField
                        control={form.control}
                        name="isActive"
                        render={({ field }) => (
                          <FormItem className="flex items-center justify-between rounded-lg border p-4">
                            <div className="space-y-0.5">
                              <FormLabel className="text-base">Trạng thái hoạt động</FormLabel>
                            </div>
                            <FormControl>
                              <Switch
                                checked={field.value}
                                onCheckedChange={field.onChange}
                                data-testid="switch-staff-active"
                              />
                            </FormControl>
                          </FormItem>
                        )}
                      />
                    </div>

                    <FormField
                      control={form.control}
                      name="notes"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Ghi chú</FormLabel>
                          <FormControl>
                            <Textarea {...field} rows={3} data-testid="textarea-staff-notes" />
                          </FormControl>
                          <FormMessage />
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
                        disabled={addStaffMutation.isPending || updateStaffMutation.isPending}
                        data-testid="button-save-staff"
                      >
                        {addStaffMutation.isPending || updateStaffMutation.isPending 
                          ? "Đang lưu..." 
                          : (editingStaff ? "Cập nhật" : "Thêm nhân viên")
                        }
                      </Button>
                    </div>
                  </form>
                </Form>
              </DialogContent>
            </Dialog>
          </div>

          <TabsContent value="list" className="space-y-6">
            {/* Filters */}
            <Card>
              <CardContent className="p-4">
                <div className="flex flex-col md:flex-row gap-4">
                  <div className="relative flex-1">
                    <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      placeholder="Tìm nhân viên..."
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="pl-9"
                      data-testid="input-search-staff"
                    />
                  </div>
                  <Select value={selectedRole} onValueChange={setSelectedRole}>
                    <SelectTrigger className="w-full md:w-48" data-testid="select-filter-role">
                      <SelectValue placeholder="Lọc theo vai trò" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">Tất cả vai trò</SelectItem>
                      {roles.map((role: any) => (
                        <SelectItem key={role.roleId} value={role.roleId.toString()}>
                          {role.roleName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </CardContent>
            </Card>

            {/* Staff List */}
            {isLoading ? (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {[...Array(6)].map((_, i) => (
                  <Card key={i} className="animate-pulse">
                    <CardContent className="p-6">
                      <div className="flex items-center space-x-4">
                        <div className="w-12 h-12 bg-gray-200 rounded-full"></div>
                        <div className="space-y-2 flex-1">
                          <div className="h-4 bg-gray-200 rounded w-3/4"></div>
                          <div className="h-3 bg-gray-200 rounded w-1/2"></div>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            ) : filteredStaff.length === 0 ? (
              <Card>
                <CardContent className="p-12 text-center">
                  <Users className="w-16 h-16 mx-auto mb-4 text-gray-400" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">
                    Không tìm thấy nhân viên
                  </h3>
                  <p className="text-gray-500 mb-4">
                    {searchTerm || selectedRole !== "all" 
                      ? "Thử thay đổi bộ lọc tìm kiếm"
                      : "Bắt đầu bằng cách thêm nhân viên đầu tiên"
                    }
                  </p>
                  <Button onClick={() => setIsAddDialogOpen(true)}>
                    <UserPlus className="w-4 h-4 mr-2" />
                    Thêm nhân viên
                  </Button>
                </CardContent>
              </Card>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {filteredStaff.map((member: any) => (
                  <Card key={member.staffId} className="hover:shadow-md transition-shadow" data-testid={`staff-card-${member.staffId}`}>
                    <CardContent className="p-6">
                      <div className="flex items-start justify-between mb-4">
                        <div className="flex items-center space-x-3">
                          <Avatar>
                            <AvatarImage src={member.avatar} />
                            <AvatarFallback>
                              {member.fullName?.split(' ').map((n: string) => n[0]).join('').toUpperCase() || 'NV'}
                            </AvatarFallback>
                          </Avatar>
                          <div>
                            <h3 className="font-semibold text-gray-900">{member.fullName}</h3>
                            <p className="text-sm text-gray-500">{member.email || member.username}</p>
                          </div>
                        </div>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" className="h-8 w-8 p-0">
                              <MoreVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent>
                            <DropdownMenuItem onClick={() => handleEditStaff(member)}>
                              <Edit className="mr-2 h-4 w-4" />
                              Chỉnh sửa
                            </DropdownMenuItem>
                            <DropdownMenuItem 
                              onClick={() => handleDeleteStaff(member.staffId)}
                              className="text-red-600"
                            >
                              <Trash2 className="mr-2 h-4 w-4" />
                              Xóa
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>

                      <div className="space-y-3">
                        <div className="flex items-center justify-between">
                          <Badge className={getRoleBadgeColor(member.roleName || 'N/A')} variant="outline">
                            {getRoleIcon(member.roleName || 'N/A')}
                            <span className="ml-1">{member.roleName || 'N/A'}</span>
                          </Badge>
                          <Badge variant={member.isActive ? 'default' : 'secondary'}>
                            {member.isActive ? 'Hoạt động' : 'Không hoạt động'}
                          </Badge>
                        </div>

                        {/* Hiển thị cửa hàng được phân quyền */}
                        {(() => {
                          const staffAssignment = staffStoreAssignments.find((s: any) => s.staffId === member.staffId);
                          const assignedStores = staffAssignment?.assignedStores || [];
                          
                          if (staffAssignment?.isAdmin) {
                            return (
                              <div className="flex items-center text-sm">
                                <Package className="w-4 h-4 mr-2 text-blue-500" />
                                <span className="text-blue-600 font-medium">Tất cả cửa hàng (Admin)</span>
                              </div>
                            );
                          }
                          
                          if (assignedStores.length > 0) {
                            return (
                              <div className="space-y-1">
                                <div className="flex items-center text-sm">
                                  <Package className="w-4 h-4 mr-2 text-green-500" />
                                  <span className="text-green-600 font-medium">Cửa hàng được phân quyền:</span>
                                </div>
                                <div className="flex flex-wrap gap-1">
                                  {assignedStores.map((store: any) => (
                                    <Badge key={store.storeId} variant="outline" className="text-xs">
                                      {store.name}
                                    </Badge>
                                  ))}
                                </div>
                              </div>
                            );
                          }
                          
                          return (
                            <div className="flex items-center text-sm">
                              <Package className="w-4 h-4 mr-2 text-gray-400" />
                              <span className="text-gray-500">Chưa được phân quyền cửa hàng nào</span>
                            </div>
                          );
                        })()}

                        <div className="space-y-2 text-sm text-gray-600">
                          <div className="flex items-center">
                            <User className="w-4 h-4 mr-2" />
                            {member.username}
                          </div>
                          {member.phoneNumber && (
                            <div className="flex items-center">
                              <Phone className="w-4 h-4 mr-2" />
                              {member.phoneNumber}
                            </div>
                          )}
                          <div className="flex items-center">
                            <Clock className="w-4 h-4 mr-2" />
                            Tạo: {new Date(member.createdAt).toLocaleDateString('vi-VN')}
                          </div>
                          {member.notes && (
                            <div className="flex items-start">
                              <Award className="w-4 h-4 mr-2 mt-0.5" />
                              <span className="text-xs">{member.notes}</span>
                            </div>
                          )}
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </TabsContent>

          <TabsContent value="roles" className="space-y-6">
            {/* Header with Add Role button */}
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-lg font-semibold">Quản lý vai trò & quyền hạn</h3>
                <p className="text-sm text-gray-600">Tạo và quản lý các vai trò cùng quyền hạn tương ứng</p>
              </div>
              <Button 
                onClick={() => {
                  setEditingRole(null);
                  roleForm.reset();
                  setIsRoleDialogOpen(true);
                }}
              >
                <UserPlus className="w-4 h-4 mr-2" />
                Thêm vai trò
              </Button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {roles.map((role: any) => {
                const staffCount = staff.filter((s: any) => s.roleId === role.roleId).length;
                return (
                  <Card key={role.roleId} className="hover:shadow-md transition-shadow">
                    <CardHeader>
                      <CardTitle className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          {getRoleIcon(role.roleName)}
                          {role.roleName}
                        </div>
                        <Badge variant="outline" className="ml-auto">
                          {staffCount} NV
                        </Badge>
                      </CardTitle>
                    </CardHeader>
                    <CardContent>
                      <p className="text-sm text-gray-600 mb-4">{role.description || 'Không có mô tả'}</p>
                      <div className="space-y-2">
                        <h4 className="font-medium text-sm">Quyền hạn:</h4>
                        <div className="flex flex-wrap gap-1">
                          {role.permissions && role.permissions.length > 0 ? (
                            role.permissions.map((permission: any) => (
                              <Badge key={permission.permissionId} variant="outline" className="text-xs">
                                {permission.permissionName}
                              </Badge>
                            ))
                          ) : (
                            <span className="text-xs text-gray-400">Chưa có quyền hạn</span>
                          )}
                        </div>
                      </div>
                      <div className="mt-4 pt-4 border-t">
                        <div className="flex items-center justify-between">
                          <p className="text-sm text-gray-500">
                            Trạng thái: <span className={`font-medium ${staffCount > 0 ? 'text-green-600' : 'text-gray-400'}`}>
                              {staffCount > 0 ? 'Đang sử dụng' : 'Chưa có NV'}
                            </span>
                          </p>
                          <div className="flex gap-2">
                            <Button 
                              variant="outline" 
                              size="sm"
                              onClick={() => {
                                setEditingRole(role);
                                roleForm.reset({
                                  roleName: role.roleName,
                                  description: role.description || "",
                                  permissionIds: role.permissions ? role.permissions.map((p: any) => p.permissionId) : []
                                });
                                setIsRoleDialogOpen(true);
                              }}
                            >
                              <Edit className="w-3 h-3 mr-1" />
                              Sửa
                            </Button>
                            <Button 
                              variant="outline" 
                              size="sm"
                              onClick={() => handleDeleteRole(role.roleId)}
                              className="text-red-600 hover:text-red-700"
                            >
                              <Trash2 className="w-3 h-3 mr-1" />
                              Xóa
                            </Button>
                          </div>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          </TabsContent>

          <TabsContent value="schedule" className="space-y-6">
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
              {/* Schedule Overview */}
              <Card className="lg:col-span-2">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Calendar className="w-5 h-5" />
                    Lịch làm việc hôm nay
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {staff.filter((member: any) => member.isActive).slice(0, 5).map((member: any) => (
                      <div key={member.id} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                        <div className="flex items-center space-x-3">
                          <Avatar className="w-8 h-8">
                            <AvatarFallback className="text-xs">
                              {member.fullName?.split(' ').map((n: string) => n[0]).join('').toUpperCase() || 'NV'}
                            </AvatarFallback>
                          </Avatar>
                          <div>
                            <p className="font-medium text-sm">{member.fullName}</p>
                            <p className="text-xs text-gray-500">{roles.find((r: any) => r.id === member.role)?.name}</p>
                          </div>
                        </div>
                        <div className="text-right">
                          <p className="text-sm font-medium">{member.workSchedule || '9:00-18:00'}</p>
                          <Badge variant="outline" className="text-xs">
                            <Clock className="w-3 h-3 mr-1" />
                            Đang làm việc
                          </Badge>
                        </div>
                      </div>
                    ))}
                    {staff.filter((member: any) => member.isActive).length === 0 && (
                      <div className="text-center py-8">
                        <Calendar className="w-12 h-12 mx-auto mb-3 text-gray-400" />
                        <p className="text-gray-500">Chưa có nhân viên làm việc hôm nay</p>
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>

              {/* Quick Stats */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Thống kê nhanh</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-3">
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-gray-600">Đang làm việc</span>
                      <Badge variant="default">{activeStaff} người</Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-gray-600">Nghỉ phép</span>
                      <Badge variant="secondary">0 người</Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-gray-600">Ca sáng</span>
                      <Badge variant="outline">{Math.floor(activeStaff * 0.6)} người</Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-gray-600">Ca chiều</span>
                      <Badge variant="outline">{Math.ceil(activeStaff * 0.4)} người</Badge>
                    </div>
                  </div>
                  
                  <div className="pt-4 border-t">
                    <Button className="w-full" variant="outline">
                      <Calendar className="w-4 h-4 mr-2" />
                      Xem lịch đầy đủ
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>
        </Tabs>
      </div>

      {/* Permission Configuration Dialog */}
      <Dialog open={isConfigDialogOpen} onOpenChange={setIsConfigDialogOpen}>
        <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Settings className="w-5 h-5" />
              Cấu hình quyền hạn - {selectedConfigRole?.roleName}
            </DialogTitle>
          </DialogHeader>
          
          {/* Bulk Actions */}
          <div className="flex items-center justify-between pb-4 border-b">
            <div className="text-sm text-gray-600">
              Đã chọn: {selectedConfigRole?.permissions?.length || 0} / {permissions.length} quyền
            </div>
            <div className="flex gap-2">
              <Button 
                variant="outline" 
                size="sm"
                onClick={() => selectAllPermissions.mutate()}
                disabled={selectAllPermissions.isPending}
              >
                Chọn tất cả
              </Button>
              <Button 
                variant="outline" 
                size="sm"
                onClick={() => deselectAllPermissions.mutate()}
                disabled={deselectAllPermissions.isPending}
              >
                Hủy tất cả
              </Button>
            </div>
          </div>
          
          <div className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {permissions.map((permission: any) => {
                const hasPermission = selectedConfigRole?.permissions?.some(
                  (p: any) => p.permissionId === permission.permissionId
                );
                
                return (
                  <div key={`permission-${permission.permissionId}`} className="flex items-start space-x-3 p-3 border rounded-lg hover:bg-gray-50">
                    <Checkbox
                      id={`permission-${permission.permissionId}`}
                      checked={hasPermission}
                      onCheckedChange={() => handlePermissionToggle(permission.permissionId, hasPermission)}
                      className="mt-1"
                      aria-describedby={`permission-desc-${permission.permissionId}`}
                    />
                    <div className="flex-1 min-w-0">
                      <label 
                        htmlFor={`permission-${permission.permissionId}`}
                        className="block text-sm font-medium text-gray-900 cursor-pointer"
                      >
                        {permission.permissionName}
                      </label>
                      <p 
                        id={`permission-desc-${permission.permissionId}`}
                        className="text-xs text-gray-500 mt-1"
                      >
                        {permission.description}
                      </p>
                      <Badge variant="outline" className="text-xs mt-1">
                        {permission.category}
                      </Badge>
                    </div>
                  </div>
                );
              })}
            </div>
            
            {permissions.length === 0 && (
              <div className="text-center py-8 text-gray-500">
                <Shield className="w-12 h-12 mx-auto mb-3 opacity-50" />
                <p>Không có quyền hạn nào được tìm thấy</p>
              </div>
            )}
          </div>
          
          <div className="flex justify-end items-center pt-4 border-t">
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setIsConfigDialogOpen(false)}>
                Đóng
              </Button>
              <Button 
                onClick={() => {
                  toast({
                    title: "Thành công",
                    description: "Cấu hình quyền hạn đã được lưu",
                  });
                  setIsConfigDialogOpen(false);
                }}
              >
                Lưu thay đổi
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Role Management Dialog */}
      <Dialog open={isRoleDialogOpen} onOpenChange={setIsRoleDialogOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>
              {editingRole ? "Chỉnh sửa vai trò" : "Thêm vai trò mới"}
            </DialogTitle>
          </DialogHeader>
          
          <Form {...roleForm}>
            <form onSubmit={roleForm.handleSubmit(onRoleSubmit)} className="space-y-4">
              <div className="grid grid-cols-1 gap-4">
                <FormField
                  control={roleForm.control}
                  name="roleName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Tên vai trò *</FormLabel>
                      <FormControl>
                        <Input placeholder="Nhập tên vai trò" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={roleForm.control}
                  name="description"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Mô tả</FormLabel>
                      <FormControl>
                        <Textarea 
                          placeholder="Nhập mô tả vai trò" 
                          rows={3}
                          {...field} 
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={roleForm.control}
                  name="permissionIds"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Quyền hạn</FormLabel>
                      <FormControl>
                        <div className="border rounded-lg p-4 max-h-60 overflow-y-auto">
                          {permissions.length === 0 ? (
                            <p className="text-sm text-gray-500">Đang tải quyền hạn...</p>
                          ) : (
                            <div className="grid grid-cols-1 gap-3">
                              {permissions.map((permission: any) => (
                                <div key={permission.permissionId} className="flex items-start space-x-3">
                                  <Checkbox
                                    id={`role-permission-${permission.permissionId}`}
                                    checked={field.value?.includes(permission.permissionId) || false}
                                    onCheckedChange={(checked) => {
                                      const currentValue = field.value || [];
                                      if (checked) {
                                        field.onChange([...currentValue, permission.permissionId]);
                                      } else {
                                        field.onChange(currentValue.filter((id: number) => id !== permission.permissionId));
                                      }
                                    }}
                                  />
                                  <div className="flex-1">
                                    <label 
                                      htmlFor={`role-permission-${permission.permissionId}`}
                                      className="block text-sm font-medium text-gray-900 cursor-pointer"
                                    >
                                      {permission.permissionName}
                                    </label>
                                    {permission.description && (
                                      <p className="text-xs text-gray-500 mt-1">
                                        {permission.description}
                                      </p>
                                    )}
                                    {permission.category && (
                                      <Badge variant="outline" className="text-xs mt-1">
                                        {permission.category}
                                      </Badge>
                                    )}
                                  </div>
                                </div>
                              ))}
                            </div>
                          )}
                        </div>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
              
              <div className="flex justify-end items-center pt-4 border-t">
                <div className="flex gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => {
                      setIsRoleDialogOpen(false);
                      setEditingRole(null);
                      roleForm.reset();
                    }}
                  >
                    Hủy
                  </Button>
                  <Button
                    type="submit"
                    disabled={addRoleMutation.isPending || updateRoleMutation.isPending}
                  >
                    {addRoleMutation.isPending || updateRoleMutation.isPending
                      ? "Đang lưu..." 
                      : (editingRole ? "Cập nhật" : "Thêm vai trò")
                    }
                  </Button>
                </div>
              </div>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </AppLayout>
  );
}