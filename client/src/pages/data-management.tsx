import React, { useState, useEffect } from 'react';
import { AppLayout } from '@/components/layout/app-layout';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Progress } from '@/components/ui/progress';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { 
  Database, 
  Download, 
  Upload, 
  Trash2, 
  AlertTriangle, 
  Shield, 
  FileText,
  HardDrive,
  Clock,
  CheckCircle,
  Loader2,
  FolderDown
} from 'lucide-react';
import { useToast } from '@/hooks/use-toast';
import { dataManagementApi, type DatabaseInfo, type BackupResult, type BackupFile, type BackupHistoryItem } from '@/lib/data-management-api';

// API Base URL for backup settings
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://101.53.9.76:5273';
const BACKUP_SETTINGS_API = `${API_BASE_URL}/api/BackupSettings`;

const DataManagement: React.FC = () => {
  const { toast } = useToast();
  const [databaseInfo, setDatabaseInfo] = useState<DatabaseInfo | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [backupProgress, setBackupProgress] = useState(0);
  const [restoreProgress, setRestoreProgress] = useState(0);
  const [customBackupPath, setCustomBackupPath] = useState('');
  const [restoreFilePath, setRestoreFilePath] = useState('');
  const [salesDataConfirmation, setSalesDataConfirmation] = useState('');
  const [showRestoreDialog, setShowRestoreDialog] = useState(false);
  const [backupFiles, setBackupFiles] = useState<BackupFile[]>([]);
  const [selectedBackupFile, setSelectedBackupFile] = useState('');
  const [uploadedFile, setUploadedFile] = useState<File | null>(null);
  const [backupHistory, setBackupHistory] = useState<BackupHistoryItem[]>([]);
  const [backupMode, setBackupMode] = useState<'server' | 'download'>('server'); // New state for backup mode
  
  // Backup Settings state
  const [backupSettings, setBackupSettings] = useState({
    backupTime: '13:00',
    isEnabled: true,
    notes: ''
  });
  const [isUpdatingSettings, setIsUpdatingSettings] = useState(false);
  const [isTestingBackup, setIsTestingBackup] = useState(false);

  const [isTestingConnection, setIsTestingConnection] = useState(false);

  // Load database info on component mount
  useEffect(() => {
    loadDatabaseInfo();
    loadBackupFiles();
    loadBackupHistory();
    loadBackupSettings();
  }, []);

  // Test API connection
  const handleTestConnection = async () => {
    setIsTestingConnection(true);
    try {
      const result = await dataManagementApi.testConnection();
      toast({
        variant: result.success ? "default" : "destructive",
        title: result.success ? "Kết nối thành công" : "Lỗi kết nối",
        description: result.message
      });
    } catch (error: any) {
      toast({
        variant: "destructive",
        title: "Lỗi kiểm tra kết nối",
        description: error.message || 'Không thể kiểm tra kết nối đến server'
      });
    } finally {
      setIsTestingConnection(false);
    }
  };

  const loadDatabaseInfo = async () => {
    try {
      const data = await dataManagementApi.getDatabaseInfo();
      setDatabaseInfo(data);
    } catch (error: any) {
      console.error('Error loading database info:', error);
      toast({
        variant: "destructive",
        title: "Lỗi",
        description: error.message || 'Lỗi khi tải thông tin database'
      });
    }
  };

  const loadBackupFiles = async () => {
    try {
      const files = await dataManagementApi.getBackupFiles();
      setBackupFiles(files);
    } catch (error: any) {
      console.error('Error loading backup files:', error);
      // Không hiển thị toast error cho việc load backup files vì có thể folder chưa tồn tại
    }
  };

  const loadBackupHistory = async () => {
    try {
      const history = await dataManagementApi.getBackupHistory();
      setBackupHistory(history);
    } catch (error: any) {
      console.error('Error loading backup history:', error);
      // Không hiển thị toast error cho việc load backup history
    }
  };

  // Load backup settings
  const loadBackupSettings = async () => {
    try {
      const response = await fetch(`${BACKUP_SETTINGS_API}`);
      if (response.ok) {
        const settings = await response.json();
        // Convert TimeSpan to HH:mm format
        const hours = Math.floor(settings.backupTime.totalSeconds / 3600);
        const minutes = Math.floor((settings.backupTime.totalSeconds % 3600) / 60);
        const timeString = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
        
        setBackupSettings({
          backupTime: timeString,
          isEnabled: settings.isEnabled,
          notes: settings.notes || ''
        });
      }
    } catch (error) {
      console.error('Error loading backup settings:', error);
    }
  };

  // Update backup settings
  const handleUpdateBackupSettings = async () => {
    setIsUpdatingSettings(true);
    try {
      const response = await fetch(`${BACKUP_SETTINGS_API}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(backupSettings)
      });

      if (response.ok) {
        toast({
          title: "Thành công",
          description: "Cài đặt backup đã được cập nhật",
        });
      } else {
        throw new Error('Failed to update backup settings');
      }
    } catch (error: any) {
      console.error('Error updating backup settings:', error);
      toast({
        variant: "destructive",
        title: "Lỗi",
        description: error.message || 'Lỗi khi cập nhật cài đặt backup'
      });
    } finally {
      setIsUpdatingSettings(false);
    }
  };

  // Test backup
  const handleTestBackup = async () => {
    setIsTestingBackup(true);
    try {
      const response = await fetch(`${BACKUP_SETTINGS_API}/test`, {
        method: 'POST'
      });

      if (response.ok) {
        toast({
          title: "Thành công",
          description: "Backup thử nghiệm đã hoàn thành",
        });
        // Reload backup history to show the new backup
        loadBackupHistory();
      } else {
        throw new Error('Failed to run test backup');
      }
    } catch (error: any) {
      console.error('Error running test backup:', error);
      toast({
        variant: "destructive",
        title: "Lỗi",
        description: error.message || 'Lỗi khi chạy backup thử nghiệm'
      });
    } finally {
      setIsTestingBackup(false);
    }
  };

  const handleCreateBackup = async () => {
    setIsLoading(true);
    setBackupProgress(0);
    
    try {
      if (backupMode === 'download') {
        // Download backup directly to user's computer
        await dataManagementApi.backupAndDownload();
        setBackupProgress(100);
        toast({
          title: "Thành công",
          description: "Sao lưu database và download về máy thành công!"
        });
      } else {
        // Save backup on server
        const result = await dataManagementApi.backupDatabase({
          backupPath: customBackupPath.trim() || undefined
        });
        setBackupProgress(100);
        toast({
          title: "Thành công",
          description: `Sao lưu database thành công! File: ${result.fileName}`
        });
      }
      
      setCustomBackupPath('');
      await loadDatabaseInfo();
      await loadBackupHistory(); // Reload backup history
    } catch (error: any) {
      console.error('Error creating backup:', error);
      toast({
        variant: "destructive",
        title: "Lỗi sao lưu",
        description: error.message || 'Không thể tạo backup. Vui lòng kiểm tra kết nối server và thử lại.'
      });
    } finally {
      setIsLoading(false);
      setBackupProgress(0);
    }
  };

  const handleDownloadBackup = async (fileName: string) => {
    setIsLoading(true);
    try {
      await dataManagementApi.downloadBackupFile(fileName);
      toast({
        title: "Thành công",
        description: `Đã download file ${fileName} về máy thành công!`
      });
    } catch (error: any) {
      console.error('Error downloading backup:', error);
      toast({
        variant: "destructive",
        title: "Lỗi download",
        description: error.message || 'Không thể download file backup. Vui lòng kiểm tra kết nối mạng và thử lại.'
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleFileUpload = async (file: File) => {
    if (!file) return;

    setIsLoading(true);
    try {
      const result = await dataManagementApi.uploadBackupFile(file);
      toast({
        title: "Thành công",
        description: `Upload file ${result.originalName} thành công!`
      });
      setUploadedFile(file);
      setRestoreFilePath(result.filePath);
      await loadBackupFiles(); // Reload backup files list
    } catch (error: any) {
      console.error('Error uploading file:', error);
      toast({
        variant: "destructive",
        title: "Lỗi",
        description: error.message || 'Lỗi khi upload file backup'
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleRestoreDatabase = async () => {
    if (!restoreFilePath.trim()) {
      toast({
        variant: "destructive",
        title: "Lỗi",
        description: 'Vui lòng chọn file backup hoặc upload file mới'
      });
      return;
    }

    setIsLoading(true);
    setRestoreProgress(0);
    
    try {
      const result = await dataManagementApi.restoreDatabase({
        backupFilePath: restoreFilePath
      });
      
      setRestoreProgress(100);
      toast({
        title: "Thành công",
        description: 'Phục hồi database thành công!'
      });
      setRestoreFilePath('');
      setSelectedBackupFile('');
      setUploadedFile(null);
      setShowRestoreDialog(false);
      await loadDatabaseInfo();
    } catch (error: any) {
      console.error('Error restoring database:', error);
      toast({
        variant: "destructive",
        title: "Lỗi",
        description: error.message || 'Lỗi khi phục hồi database'
      });
    } finally {
      setIsLoading(false);
      setRestoreProgress(0);
    }
  };

  const handleDeleteSalesData = async () => {
    if (salesDataConfirmation !== 'DELETE SALES DATA') {
      toast({
        variant: "destructive",
        title: "Lỗi",
        description: 'Vui lòng nhập chính xác "DELETE SALES DATA"'
      });
      return;
    }

    setIsLoading(true);
    
    try {
      const result = await dataManagementApi.deleteSalesData({
        confirmationText: salesDataConfirmation
      });
      
      toast({
        title: "Thành công",
        description: 'Xóa dữ liệu bán hàng thành công!'
      });
      setSalesDataConfirmation('');
      await loadDatabaseInfo();
    } catch (error: any) {
      console.error('Error deleting sales data:', error);
      toast({
        variant: "destructive",
        title: "Lỗi",
        description: error.message || 'Lỗi khi xóa dữ liệu bán hàng'
      });
    } finally {
      setIsLoading(false);
    }
  };

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <AppLayout title="Quản Lý Dữ Liệu">
      <div className="container mx-auto p-6">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-3xl font-bold text-gray-900">Quản Lý Dữ Liệu</h1>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={handleTestConnection}
              disabled={isTestingConnection}
              className="flex items-center gap-2"
            >
              {isTestingConnection ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Shield className="h-4 w-4" />
              )}
              Kiểm tra kết nối
            </Button>
            <Badge variant="outline">
              <Shield className="h-4 w-4 mr-1" />
              Cấp quyền
            </Badge>
          </div>
        </div>

      {/* Database Info Card */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Database className="h-5 w-5 text-blue-600" />
            Thông Tin Database
          </CardTitle>
        </CardHeader>
        <CardContent>
          {databaseInfo ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="flex items-center gap-3">
                <Database className="h-8 w-8 text-blue-600" />
                <div>
                  <p className="text-sm text-gray-600">Tên Database</p>
                  <p className="font-semibold">{databaseInfo.databaseName}</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <HardDrive className="h-8 w-8 text-green-600" />
                <div>
                  <p className="text-sm text-gray-600">Kích Thước</p>
                  <p className="font-semibold">{databaseInfo.sizeMB} MB</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <Database className="h-8 w-8 text-purple-600" />
                <div>
                  <p className="text-sm text-gray-600">Server</p>
                  <p className="font-semibold">{databaseInfo.serverName}</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <Clock className="h-8 w-8 text-orange-600" />
                <div>
                  <p className="text-sm text-gray-600">Backup Cuối</p>
                  <p className="font-semibold">{databaseInfo.lastBackup || 'Chưa có'}</p>
                </div>
              </div>
            </div>
          ) : (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
              <span className="ml-2 text-gray-600">Đang tải thông tin...</span>
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Backup Database Card */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Download className="h-5 w-5 text-green-600" />
              Sao Lưu Dữ Liệu
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Backup Mode Selection */}
            <div className="space-y-3">
              <Label>Chọn phương thức sao lưu:</Label>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <label className={`flex items-center space-x-3 p-3 border rounded-lg cursor-pointer transition-colors ${
                  backupMode === 'server' ? 'border-blue-500 bg-blue-50' : 'border-gray-300 hover:border-gray-400'
                }`}>
                  <input
                    type="radio"
                    name="backupMode"
                    value="server"
                    checked={backupMode === 'server'}
                    onChange={(e) => setBackupMode(e.target.value as 'server' | 'download')}
                    className="h-4 w-4 text-blue-600"
                  />
                  <div className="flex items-center gap-2">
                    <Database className="h-4 w-4 text-blue-600" />
                    <div>
                      <div className="font-medium">Lưu trên Server</div>
                      <div className="text-xs text-gray-500">Lưu file backup trên server</div>
                    </div>
                  </div>
                </label>
                
                <label className={`flex items-center space-x-3 p-3 border rounded-lg cursor-pointer transition-colors ${
                  backupMode === 'download' ? 'border-green-500 bg-green-50' : 'border-gray-300 hover:border-gray-400'
                }`}>
                  <input
                    type="radio"
                    name="backupMode"
                    value="download"
                    checked={backupMode === 'download'}
                    onChange={(e) => setBackupMode(e.target.value as 'server' | 'download')}
                    className="h-4 w-4 text-green-600"
                  />
                  <div className="flex items-center gap-2">
                    <FolderDown className="h-4 w-4 text-green-600" />
                    <div>
                      <div className="font-medium">Download về máy</div>
                      <div className="text-xs text-gray-500">Tải trực tiếp về máy local</div>
                    </div>
                  </div>
                </label>
              </div>
            </div>

            {/* Custom backup path - only show for server mode */}
            {backupMode === 'server' && (
              <div className="space-y-2">
                <Label>Đường dẫn sao lưu (tùy chọn)</Label>
                <Input
                  placeholder="Để trống sẽ dùng đường dẫn mặc định"
                  value={customBackupPath}
                  onChange={(e) => setCustomBackupPath(e.target.value)}
                  disabled={isLoading}
                />
                <p className="text-sm text-gray-600">
                  Mặc định: C:\temp\RetailPoint_backup_[timestamp].bak
                </p>
              </div>
            )}

            {/* Info for download mode */}
            {backupMode === 'download' && (
              <Alert>
                <FolderDown className="h-4 w-4" />
                <AlertDescription>
                  <div className="space-y-1">
                    <p>File backup sẽ được tải trực tiếp về thư mục Downloads của bạn.</p>
                    <p className="text-xs text-gray-600">
                      <strong>Lưu ý:</strong> Nếu gặp lỗi, hãy thử click "Kiểm tra kết nối" trước khi download.
                    </p>
                  </div>
                </AlertDescription>
              </Alert>
            )}

            {backupProgress > 0 && (
              <div className="space-y-2">
                <Label>Tiến trình sao lưu:</Label>
                <Progress value={backupProgress} className="w-full" />
              </div>
            )}

            <Button
              onClick={handleCreateBackup}
              disabled={isLoading}
              className="w-full"
            >
              {isLoading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Đang sao lưu...
                </>
              ) : (
                <>
                  {backupMode === 'download' ? (
                    <FolderDown className="h-4 w-4 mr-2" />
                  ) : (
                    <Download className="h-4 w-4 mr-2" />
                  )}
                  {backupMode === 'download' ? 'Tạo Backup & Download' : 'Tạo Backup'}
                </>
              )}
            </Button>
          </CardContent>
        </Card>

        {/* Restore Database Card */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Upload className="h-5 w-5 text-blue-600" />
              Phục Hồi Dữ Liệu
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <Alert>
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>
                <strong>Cảnh báo:</strong> Phục hồi sẽ ghi đè toàn bộ dữ liệu hiện tại!
              </AlertDescription>
            </Alert>

            <Dialog open={showRestoreDialog} onOpenChange={setShowRestoreDialog}>
              <DialogTrigger asChild>
                <Button variant="outline" className="w-full" disabled={isLoading}>
                  <Upload className="h-4 w-4 mr-2" />
                  Phục Hồi Database
                </Button>
              </DialogTrigger>
              <DialogContent className="max-w-md">
                <DialogHeader>
                  <DialogTitle>Phục Hồi Database</DialogTitle>
                </DialogHeader>
                <div className="space-y-4">
                  <Alert>
                    <AlertTriangle className="h-4 w-4" />
                    <AlertDescription>
                      Phục hồi sẽ ghi đè toàn bộ dữ liệu hiện tại!
                    </AlertDescription>
                  </Alert>

                  {/* Chọn từ backup files đã có */}
                  {backupFiles.length > 0 && (
                    <div className="space-y-2">
                      <Label>Chọn từ backup files đã upload:</Label>
                      <Select 
                        value={selectedBackupFile} 
                        onValueChange={(value) => {
                          setSelectedBackupFile(value);
                          setRestoreFilePath(value);
                          setUploadedFile(null);
                        }}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Chọn file backup..." />
                        </SelectTrigger>
                        <SelectContent>
                          {backupFiles.map((file) => (
                            <SelectItem key={file.fileName} value={file.filePath}>
                              <div className="flex flex-col">
                                <span className="font-medium">{file.fileName}</span>
                                <span className="text-xs text-gray-500">
                                  {formatFileSize(file.size)} - {new Date(file.lastModified).toLocaleString()}
                                </span>
                              </div>
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  )}

                  <div className="flex items-center gap-2">
                    <div className="flex-1 border-t border-gray-300"></div>
                    <span className="text-sm text-gray-500">HOẶC</span>
                    <div className="flex-1 border-t border-gray-300"></div>
                  </div>

                  {/* Upload file mới */}
                  <div className="space-y-2">
                    <Label>Upload file backup mới:</Label>
                    <div className="flex items-center justify-center w-full">
                      <label className="flex flex-col items-center justify-center w-full h-32 border-2 border-gray-300 border-dashed rounded-lg cursor-pointer bg-gray-50 hover:bg-gray-100">
                        <div className="flex flex-col items-center justify-center pt-5 pb-6">
                          <Upload className="w-8 h-8 mb-2 text-gray-500" />
                          <p className="mb-2 text-sm text-gray-500">
                            <span className="font-semibold">Click để chọn</span> hoặc kéo thả file
                          </p>
                          <p className="text-xs text-gray-500">.bak hoặc .sql files</p>
                        </div>
                        <input
                          type="file"
                          className="hidden"
                          accept=".bak,.sql"
                          onChange={(e) => {
                            const file = e.target.files?.[0];
                            if (file) {
                              handleFileUpload(file);
                              setSelectedBackupFile('');
                            }
                          }}
                          disabled={isLoading}
                        />
                      </label>
                    </div>
                    
                    {uploadedFile && (
                      <div className="p-3 bg-green-50 border border-green-200 rounded-md">
                        <p className="text-sm text-green-800">
                          ✓ Đã upload: {uploadedFile.name}
                        </p>
                      </div>
                    )}
                  </div>

                  {restoreProgress > 0 && (
                    <div className="space-y-2">
                      <Label>Tiến trình phục hồi:</Label>
                      <Progress value={restoreProgress} className="w-full" />
                    </div>
                  )}
                </div>

                <div className="flex justify-end gap-2 mt-6">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setShowRestoreDialog(false);
                      setRestoreFilePath('');
                      setSelectedBackupFile('');
                      setUploadedFile(null);
                    }}
                    disabled={isLoading}
                  >
                    Hủy
                  </Button>
                  <Button
                    onClick={handleRestoreDatabase}
                    disabled={isLoading || !restoreFilePath.trim()}
                  >
                    {isLoading ? (
                      <>
                        <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                        Đang phục hồi...
                      </>
                    ) : (
                      <>
                        <Upload className="h-4 w-4 mr-2" />
                        Phục Hồi
                      </>
                    )}
                  </Button>
                </div>
              </DialogContent>
            </Dialog>
          </CardContent>
        </Card>
      </div>

      {/* Backup History Section */}
      <div className="mt-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-5 w-5 text-orange-600" />
              Lịch Sử Backup
            </CardTitle>
          </CardHeader>
          <CardContent>
            {backupHistory.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left p-2">Thời gian</th>
                      <th className="text-left p-2">Loại</th>
                      <th className="text-left p-2">Tên file</th>
                      <th className="text-left p-2">Kích thước</th>
                      <th className="text-left p-2">Trạng thái</th>
                      <th className="text-left p-2">Ghi chú</th>
                      <th className="text-left p-2">Thao tác</th>
                    </tr>
                  </thead>
                  <tbody>
                    {backupHistory.map((item) => (
                      <tr key={item.id} className="border-b hover:bg-gray-50">
                        <td className="p-2">
                          {new Date(item.backupDate).toLocaleString('vi-VN')}
                        </td>
                        <td className="p-2">
                          <Badge variant={item.backupType === 'Auto' ? 'default' : 'outline'}>
                            {item.backupType === 'Auto' ? 'Tự động' : 'Thủ công'}
                          </Badge>
                        </td>
                        <td className="p-2 font-mono text-xs">{item.fileName}</td>
                        <td className="p-2">{item.fileSizeMB.toFixed(2)} MB</td>
                        <td className="p-2">
                          <Badge variant={item.status === 'Success' ? 'default' : 'destructive'}>
                            {item.status === 'Success' ? 'Thành công' : 'Thất bại'}
                          </Badge>
                        </td>
                        <td className="p-2 text-xs text-gray-600">{item.note}</td>
                        <td className="p-2">
                          {item.status === 'Success' && (
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => handleDownloadBackup(item.fileName)}
                              disabled={isLoading}
                              className="h-8 px-2 text-xs"
                            >
                              {isLoading ? (
                                <Loader2 className="h-3 w-3 mr-1 animate-spin" />
                              ) : (
                                <FolderDown className="h-3 w-3 mr-1" />
                              )}
                              Download
                            </Button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="text-center py-8 text-gray-500">
                <Clock className="h-12 w-12 mx-auto mb-2 opacity-50" />
                <p>Chưa có lịch sử backup nào</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Backup Settings Section */}
      <div className="mt-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-5 w-5 text-blue-600" />
              Cài Đặt Backup Tự Động
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="backup-time">Thời gian backup hàng ngày</Label>
                <Input
                  id="backup-time"
                  type="time"
                  value={backupSettings.backupTime}
                  onChange={(e) => setBackupSettings({
                    ...backupSettings,
                    backupTime: e.target.value
                  })}
                />
                <p className="text-xs text-gray-500">
                  Hệ thống sẽ tự động backup dữ liệu vào thời gian này mỗi ngày
                </p>
              </div>
              
              <div className="space-y-2">
                <Label>Trạng thái</Label>
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="backup-enabled"
                    checked={backupSettings.isEnabled}
                    onChange={(e) => setBackupSettings({
                      ...backupSettings,
                      isEnabled: e.target.checked
                    })}
                    className="rounded"
                  />
                  <Label htmlFor="backup-enabled">
                    {backupSettings.isEnabled ? 'Đang bật' : 'Đang tắt'}
                  </Label>
                </div>
              </div>
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="backup-notes">Ghi chú (tùy chọn)</Label>
              <Textarea
                id="backup-notes"
                placeholder="Nhập ghi chú về cài đặt backup..."
                value={backupSettings.notes}
                onChange={(e) => setBackupSettings({
                  ...backupSettings,
                  notes: e.target.value
                })}
                rows={2}
              />
            </div>
            
            <div className="flex gap-2">
              <Button 
                onClick={handleUpdateBackupSettings}
                disabled={isUpdatingSettings}
                className="flex items-center gap-2"
              >
                {isUpdatingSettings && <Loader2 className="h-4 w-4 animate-spin" />}
                Lưu cài đặt
              </Button>
              
              <Button 
                variant="outline"
                onClick={handleTestBackup}
                disabled={isTestingBackup}
                className="flex items-center gap-2"
              >
                {isTestingBackup && <Loader2 className="h-4 w-4 animate-spin" />}
                Chạy thử backup
              </Button>
            </div>
            
            <Alert>
              <Shield className="h-4 w-4" />
              <AlertDescription>
                Backup tự động sẽ chạy vào thời gian đã cài đặt mỗi ngày. 
                Bạn có thể tắt tính năng này bằng cách bỏ chọn "Đang bật".
              </AlertDescription>
            </Alert>
          </CardContent>
        </Card>
      </div>

      {/* Data Deletion Section */}
      <div className="mt-6 max-w-md mx-auto">
        {/* Delete Sales Data Card */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Trash2 className="h-5 w-5 text-yellow-600" />
              Xóa Dữ Liệu Bán Hàng
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <Alert>
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>
                Xóa tất cả đơn hàng, chi tiết đơn hàng và dữ liệu thanh toán
              </AlertDescription>
            </Alert>

            <div className="space-y-2">
              <Label>Nhập "DELETE SALES DATA" để xác nhận:</Label>
              <Input
                placeholder="DELETE SALES DATA"
                value={salesDataConfirmation}
                onChange={(e) => setSalesDataConfirmation(e.target.value)}
                disabled={isLoading}
              />
            </div>

            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button
                  variant="destructive"
                  disabled={isLoading || salesDataConfirmation !== 'DELETE SALES DATA'}
                  className="w-full"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  Xóa Dữ Liệu Bán Hàng
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>Bạn có chắc chắn?</AlertDialogTitle>
                  <AlertDialogDescription>
                    Thao tác này sẽ xóa vĩnh viễn tất cả dữ liệu bán hàng và không thể hoàn tác.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Hủy</AlertDialogCancel>
                  <AlertDialogAction onClick={handleDeleteSalesData}>
                    Xóa
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          </CardContent>
        </Card>
      </div>
    </div>
    </AppLayout>
  );
};

export default DataManagement;