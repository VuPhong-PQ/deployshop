import { useRef, useCallback } from 'react';

export const useNotificationSound = () => {
  const audioRef = useRef<HTMLAudioElement | null>(null);

  // Táº¡o Ã¢m thanh thÃ´ng bÃ¡o sá»­ dá»¥ng Web Audio API
  const createNotificationSound = useCallback(() => {
    // Táº¡o Ã¢m thanh báº±ng Web Audio API (khÃ´ng cáº§n file Ã¢m thanh)
    const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    
    // Táº¡o oscillator cho Ã¢m thanh chuÃ´ng
    const oscillator1 = audioContext.createOscillator();
    const oscillator2 = audioContext.createOscillator();
    const gainNode = audioContext.createGain();
    
    // Káº¿t ná»‘i cÃ¡c node
    oscillator1.connect(gainNode);
    oscillator2.connect(gainNode);
    gainNode.connect(audioContext.destination);
    
    // Cáº¥u hÃ¬nh Ã¢m thanh chuÃ´ng (hai táº§n sá»‘ Ä‘á»ƒ táº¡o Ã¢m hÃ i hÃ²a)
    oscillator1.frequency.setValueAtTime(800, audioContext.currentTime); // Note cao
    oscillator2.frequency.setValueAtTime(600, audioContext.currentTime); // Note tháº¥p hÆ¡n
    
    oscillator1.type = 'sine';
    oscillator2.type = 'sine';
    
    // Cáº¥u hÃ¬nh envelope (fade in/out)
    gainNode.gain.setValueAtTime(0, audioContext.currentTime);
    gainNode.gain.linearRampToValueAtTime(0.3, audioContext.currentTime + 0.1);
    gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.8);
    
    // PhÃ¡t Ã¢m thanh
    oscillator1.start(audioContext.currentTime);
    oscillator2.start(audioContext.currentTime);
    
    // Dá»«ng sau 0.8 giÃ¢y
    oscillator1.stop(audioContext.currentTime + 0.8);
    oscillator2.stop(audioContext.currentTime + 0.8);
    
    return audioContext;
  }, []);

  const playNotificationSound = useCallback(() => {
    try {
      createNotificationSound();
    } catch (error) {
      // Fallback: sá»­ dá»¥ng HTML5 audio vá»›i data URL
      try {
        if (!audioRef.current) {
          // Táº¡o Ã¢m thanh ngáº¯n báº±ng data URL
          const audio = new Audio();
          audio.src = 'data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBjiMzvHLfiMHImfA7dWPOAgSU6Xf7bhgGQQ5jdT1zm4jBiVuv+zZjDoKF2q+6d2OOQgUUqnh5rZgGQU+ksz1zG4jBiFov+vYjTkIElOp5Oy1YRoFPJLM9Mxv'; 
          audioRef.current = audio;
        }
        audioRef.current.volume = 0.3;
        audioRef.current.play().catch(() => {
          // Ignore errors if user hasn't interacted with page yet
        });
      } catch (fallbackError) {
      }
    }
  }, [createNotificationSound]);

  return { playNotificationSound };
};
