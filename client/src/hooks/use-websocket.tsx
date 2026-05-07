// WebSocket disabled — server does not support WS, hook returns no-op to avoid console errors
export function useWebSocket() {
  const sendMessage = (_message: any) => {};
  return { isConnected: false, lastMessage: null, sendMessage };
}

