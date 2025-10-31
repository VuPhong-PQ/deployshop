import { useRef, useCallback } from 'react';

export const useNotificationSound = () => {
  const audioRef = useRef<HTMLAudioElement | null>(null);

  // Tạo âm thanh thông báo sử dụng Web Audio API
  const createNotificationSound = useCallback(() => {
    // Tạo âm thanh bằng Web Audio API (không cần file âm thanh)
    const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    
    // Tạo oscillator cho âm thanh chuông
    const oscillator1 = audioContext.createOscillator();
    const oscillator2 = audioContext.createOscillator();
    const gainNode = audioContext.createGain();
    
    // Kết nối các node
    oscillator1.connect(gainNode);
    oscillator2.connect(gainNode);
    gainNode.connect(audioContext.destination);
    
    // Cấu hình âm thanh chuông (hai tần số để tạo âm hài hòa)
    oscillator1.frequency.setValueAtTime(800, audioContext.currentTime); // Note cao
    oscillator2.frequency.setValueAtTime(600, audioContext.currentTime); // Note thấp hơn
    
    oscillator1.type = 'sine';
    oscillator2.type = 'sine';
    
    // Cấu hình envelope (fade in/out)
    gainNode.gain.setValueAtTime(0, audioContext.currentTime);
    gainNode.gain.linearRampToValueAtTime(0.3, audioContext.currentTime + 0.1);
    gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.8);
    
    // Phát âm thanh
    oscillator1.start(audioContext.currentTime);
    oscillator2.start(audioContext.currentTime);
    
    // Dừng sau 0.8 giây
    oscillator1.stop(audioContext.currentTime + 0.8);
    oscillator2.stop(audioContext.currentTime + 0.8);
    
    return audioContext;
  }, []);

  const playNotificationSound = useCallback(() => {
    try {
      createNotificationSound();
    } catch (error) {
      console.log('Không thể phát âm thanh:', error);
      // Fallback: sử dụng HTML5 audio với data URL
      try {
        if (!audioRef.current) {
          // Tạo âm thanh ngắn bằng data URL
          const audio = new Audio();
          audio.src = 'data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBjiMzvHLfiMHImfA7dWPOAgSU6Xf7bhgGQQ5jdT1zm4jBiVuv+zZjDoKF2q+6d2OOQgUUqnh5rZgGQU+ksz1zG4jBiFov+vYjTkIElOp5Oy1YRoFPJLM9Mxv'; 
          audioRef.current = audio;
        }
        audioRef.current.volume = 0.3;
        audioRef.current.play().catch(() => {
          // Ignore errors if user hasn't interacted with page yet
        });
      } catch (fallbackError) {
        console.log('Fallback audio cũng thất bại:', fallbackError);
      }
    }
  }, [createNotificationSound]);

  return { playNotificationSound };
};