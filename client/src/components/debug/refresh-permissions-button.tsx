import React from 'react';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/contexts/auth-context';
import { RefreshCw } from 'lucide-react';

export function RefreshPermissionsButton() {
  const { refreshPermissions } = useAuth();

  const handleRefresh = async () => {
    await refreshPermissions();
    // Reload page to see updated menu
    window.location.reload();
  };

  return (
    <Button 
      onClick={handleRefresh} 
      variant="outline" 
      size="sm"
      className="gap-2"
    >
      <RefreshCw className="h-4 w-4" />
      Refresh Permissions
    </Button>
  );
}