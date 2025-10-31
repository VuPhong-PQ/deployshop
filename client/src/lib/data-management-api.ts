// Data Management API Types
export interface DatabaseInfo {
  databaseName: string;
  sizeMB: number;
  serverName: string;
  lastBackup: string;
}

export interface BackupRequest {
  backupPath?: string;
}

export interface BackupResult {
  message: string;
  backupPath: string;
  fileName: string;
  timestamp: string;
}

export interface RestoreRequest {
  backupFilePath: string;
}

export interface BackupFile {
  fileName: string;
  filePath: string;
  size: number;
  lastModified: string;
  extension: string;
}

export interface UploadBackupResponse {
  message: string;
  filePath: string;
  fileName: string;
  originalName: string;
  size: number;
}

export interface BackupHistoryItem {
  id: number;
  backupDate: string;
  backupType: string;
  fileName: string;
  fileSizeMB: number;
  status: string;
  note: string;
}

export interface DeleteConfirmation {
  confirmationText: string;
}

export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  message?: string;
  error?: string;
}

// Utility function for retry logic
const retryFetch = async (
  url: string, 
  options: RequestInit, 
  maxRetries: number = 2,
  delay: number = 1000
): Promise<Response> => {
  let lastError: Error;
  
  for (let i = 0; i <= maxRetries; i++) {
    try {
      const response = await fetch(url, options);
      return response;
    } catch (error: any) {
      lastError = error;
      
      // Don't retry on certain errors
      if (error.name === 'AbortError' || i === maxRetries) {
        break;
      }
      
      // Wait before retry
      if (i < maxRetries) {
        await new Promise(resolve => setTimeout(resolve, delay * (i + 1)));
      }
    }
  }
  
  throw lastError!;
};

// Data Management API functions
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://101.53.9.76:5273';
const API_BASE = `${API_BASE_URL}/api/DataManagement`;

export const dataManagementApi = {
  // Test API connection
  testConnection: async (): Promise<{success: boolean, message: string}> => {
    try {
      // Test với endpoint database-info thay vì health
      const response = await fetch(`${API_BASE}/database-info`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'StaffId': localStorage.getItem('staffId') || '1'
        },
        mode: 'cors',
        credentials: 'omit'
      });
      
      if (response.ok) {
        return { success: true, message: 'Kết nối API thành công' };
      } else {
        const contentType = response.headers.get('content-type');
        let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
        
        if (contentType && contentType.includes('application/json')) {
          try {
            const error = await response.json();
            errorMessage = error.message || error.error || errorMessage;
          } catch {
            // Keep the default error message
          }
        }
        
        return { success: false, message: errorMessage };
      }
    } catch (error: any) {
      let errorMessage = 'Lỗi kết nối không xác định';
      
      if (error.name === 'TypeError') {
        if (error.message.includes('fetch')) {
          errorMessage = 'Không thể kết nối đến server. Kiểm tra URL và kết nối mạng.';
        } else if (error.message.includes('CORS')) {
          errorMessage = 'Lỗi CORS. Server cần cấu hình cho phép cross-origin requests.';
        }
      } else {
        errorMessage = error.message;
      }
      
      return { success: false, message: errorMessage };
    }
  },

  // Get database info
  getDatabaseInfo: async (): Promise<DatabaseInfo> => {
    const staffId = localStorage.getItem('staffId');
    const response = await fetch(`${API_BASE}/database-info`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'StaffId': staffId || ''
      }
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Lỗi khi tải thông tin database');
    }

    return response.json();
  },

  // Backup database
  backupDatabase: async (request: BackupRequest): Promise<BackupResult> => {
    const staffId = localStorage.getItem('staffId');
    const response = await fetch(`${API_BASE}/backup`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'StaffId': staffId || ''
      },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Lỗi khi sao lưu database');
    }

    return response.json();
  },

  // Get backup files list
  getBackupFiles: async (): Promise<BackupFile[]> => {
    const staffId = localStorage.getItem('staffId');
    const response = await fetch(`${API_BASE}/backup-files`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'StaffId': staffId || ''
      }
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Lỗi khi lấy danh sách backup files');
    }

    const result = await response.json();
    return result.files;
  },

  // Upload backup file
  uploadBackupFile: async (file: File): Promise<{
    message: string;
    filePath: string;
    fileName: string;
    originalName: string;
    size: number;
  }> => {
    const staffId = localStorage.getItem('staffId');
    const formData = new FormData();
    formData.append('file', file);
    
    const response = await fetch(`${API_BASE}/upload-backup`, {
      method: 'POST',
      headers: {
        'StaffId': staffId || '1'
      },
      body: formData
    });
    
    if (!response.ok) {
      let errorMessage = 'Lỗi khi upload backup file';
      try {
        const error = await response.json();
        errorMessage = error.message || error.error || errorMessage;
      } catch {
        errorMessage = `HTTP ${response.status}: ${response.statusText}`;
      }
      throw new Error(errorMessage);
    }
    
    return response.json();
  },

  // Download backup file
  downloadBackupFile: async (fileName: string): Promise<void> => {
    const staffId = localStorage.getItem('staffId');
    
    try {
      const response = await fetch(`${API_BASE}/download-backup/${encodeURIComponent(fileName)}`, {
        method: 'GET',
        headers: {
          'StaffId': staffId || '1',
          'Accept': 'application/octet-stream, application/json'
        },
        mode: 'cors',
        credentials: 'omit'
      });
      
      if (!response.ok) {
        let errorMessage = 'Lỗi khi download backup file';
        
        // Check if response has content
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          try {
            const error = await response.json();
            errorMessage = error.message || error.error || errorMessage;
          } catch {
            errorMessage = `Server Error ${response.status}: ${response.statusText}`;
          }
        } else {
          errorMessage = `Server Error ${response.status}: ${response.statusText}`;
        }
        
        throw new Error(errorMessage);
      }
      
      // Create blob and download
      const blob = await response.blob();
      
      // Check if blob is valid
      if (blob.size === 0) {
        throw new Error('File backup trống hoặc không tồn tại');
      }
      
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      link.style.display = 'none';
      document.body.appendChild(link);
      link.click();
      
      // Clean up
      setTimeout(() => {
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      }, 100);
      
    } catch (error: any) {
      // Handle network errors
      if (error.name === 'TypeError' && error.message.includes('fetch')) {
        throw new Error('Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng.');
      }
      throw error;
    }
  },

  // Backup and download directly
  backupAndDownload: async (): Promise<void> => {
    const staffId = localStorage.getItem('staffId');
    
    try {
      const response = await retryFetch(`${API_BASE}/backup-and-download`, {
        method: 'POST',
        headers: {
          'StaffId': staffId || '1',
          'Accept': 'application/octet-stream, application/json'
        },
        mode: 'cors',
        credentials: 'omit'
      }, 2, 2000); // 2 retries with 2 second delays
      
      if (!response.ok) {
        let errorMessage = 'Lỗi khi tạo backup và download';
        
        // Check if response has content
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          try {
            const error = await response.json();
            errorMessage = error.message || error.error || errorMessage;
          } catch {
            errorMessage = `Server Error ${response.status}: ${response.statusText}`;
          }
        } else {
          errorMessage = `Server Error ${response.status}: ${response.statusText}`;
        }
        
        throw new Error(errorMessage);
      }
      
      // Extract filename from Content-Disposition header or use default
      let fileName = `backup_${new Date().toISOString().replace(/[:.]/g, '-')}.bak`;
      const contentDisposition = response.headers.get('Content-Disposition');
      if (contentDisposition) {
        const fileNameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
        if (fileNameMatch && fileNameMatch[1]) {
          fileName = fileNameMatch[1].replace(/['"]/g, '');
        }
      }
      
      // Create blob and download
      const blob = await response.blob();
      
      // Check if blob is valid
      if (blob.size === 0) {
        throw new Error('File backup trống hoặc không hợp lệ');
      }
      
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      link.style.display = 'none';
      document.body.appendChild(link);
      link.click();
      
      // Clean up
      setTimeout(() => {
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      }, 100);
      
    } catch (error: any) {
      // Handle network errors
      if (error.name === 'TypeError' && error.message.includes('fetch')) {
        throw new Error('Không thể kết nối đến server sau nhiều lần thử. Vui lòng kiểm tra kết nối mạng.');
      }
      throw error;
    }
  },  // Get backup history
  getBackupHistory: async (): Promise<BackupHistoryItem[]> => {
    const staffId = localStorage.getItem('staffId');
    const response = await fetch(`${API_BASE}/backup-history`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'StaffId': staffId || ''
      }
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Lỗi khi lấy lịch sử backup');
    }

    const result = await response.json();
    return result.history;
  },

  // Restore database
  restoreDatabase: async (request: RestoreRequest): Promise<{ message: string; restoredFrom: string }> => {
    const staffId = localStorage.getItem('staffId');
    const response = await fetch(`${API_BASE}/restore`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'StaffId': staffId || ''
      },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Lỗi khi phục hồi database');
    }

    return response.json();
  },

  // Delete sales data
  deleteSalesData: async (confirmation: DeleteConfirmation): Promise<{ message: string; timestamp: string }> => {
    const staffId = localStorage.getItem('staffId');
    const response = await fetch(`${API_BASE}/sales-data`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        'StaffId': staffId || ''
      },
      body: JSON.stringify(confirmation)
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Lỗi khi xóa dữ liệu bán hàng');
    }

    return response.json();
  }
};

// React Query hooks for data management
export const useDataManagement = () => {
  return {
    getDatabaseInfo: () => dataManagementApi.getDatabaseInfo(),
    backupDatabase: (request: BackupRequest) => dataManagementApi.backupDatabase(request),
    getBackupFiles: () => dataManagementApi.getBackupFiles(),
    getBackupHistory: () => dataManagementApi.getBackupHistory(),
    uploadBackupFile: (file: File) => dataManagementApi.uploadBackupFile(file),
    restoreDatabase: (request: RestoreRequest) => dataManagementApi.restoreDatabase(request),
    deleteSalesData: (confirmation: DeleteConfirmation) => dataManagementApi.deleteSalesData(confirmation)
  };
};