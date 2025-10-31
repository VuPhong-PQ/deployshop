import { useState } from "react";
import { Sidebar } from "./sidebar";
import { Header } from "./header";
import { NotificationModal } from "@/components/ui/notification-modal";
import { useWebSocket } from "@/hooks/use-websocket";

interface AppLayoutProps {
  children: React.ReactNode;
  title: string;
}

export function AppLayout({ children, title }: AppLayoutProps) {
  const [showNotifications, setShowNotifications] = useState(false);
  const { isConnected } = useWebSocket();

  return (
    <div className="flex h-screen bg-gray-50 relative">
      <Sidebar />
      
      <main className="flex-1 flex flex-col overflow-hidden transition-all duration-300 ease-in-out">
        <Header 
          title={title}
          onToggleNotifications={() => setShowNotifications(!showNotifications)}
          isWebSocketConnected={isConnected}
        />
        
        <div className="flex-1 overflow-auto p-6">
          {children}
        </div>
      </main>

      <NotificationModal 
        show={showNotifications}
        onClose={() => setShowNotifications(false)}
      />
    </div>
  );
}
