// DEBUG BARCODE SCANNER - Simplified version
console.log('🔧 Loading debug barcode scanner...');

let debugBuffer = '';
let debugTimeout = null;

// Remove existing listeners
document.removeEventListener('keydown', window.debugBarcodeHandler);

// Simple debug handler
window.debugBarcodeHandler = function(event) {
    console.log('🎹 Key Event:', {
        key: event.key,
        code: event.code,
        timeStamp: event.timeStamp,
        target: event.target?.tagName,
        buffer: debugBuffer
    });
    
    if (event.key === 'Enter') {
        if (debugBuffer.length > 0) {
            console.log('🔍 BARCODE DETECTED:', debugBuffer);
            
            // Try to trigger product search
            const searchInput = document.querySelector('input[placeholder*="Tìm kiếm"]');
            if (searchInput) {
                searchInput.value = debugBuffer;
                searchInput.dispatchEvent(new Event('input', { bubbles: true }));
                console.log('📝 Updated search input with:', debugBuffer);
            }
            
            debugBuffer = '';
        }
        return;
    }
    
    if (event.key.length === 1) {
        debugBuffer += event.key;
        
        clearTimeout(debugTimeout);
        debugTimeout = setTimeout(() => {
            if (debugBuffer.length > 0) {
                console.log('⏰ Timeout - processing buffer:', debugBuffer);
                
                // Try to trigger product search
                const searchInput = document.querySelector('input[placeholder*="Tìm kiếm"]');
                if (searchInput) {
                    searchInput.value = debugBuffer;
                    searchInput.dispatchEvent(new Event('input', { bubbles: true }));
                    console.log('📝 Updated search input with:', debugBuffer);
                }
                
                debugBuffer = '';
            }
        }, 200);
    }
};

// Add listener
document.addEventListener('keydown', window.debugBarcodeHandler);

console.log('✅ Debug barcode scanner loaded. Quét mã vạch ngay!');

// Test function
window.testBarcodeScan = function(barcode = '89900000011') {
    console.log('🧪 Testing barcode scan:', barcode);
    
    barcode.split('').forEach((char, index) => {
        setTimeout(() => {
            document.dispatchEvent(new KeyboardEvent('keydown', {
                key: char,
                code: `Digit${char}`,
                bubbles: true
            }));
        }, index * 30);
    });
    
    setTimeout(() => {
        document.dispatchEvent(new KeyboardEvent('keydown', {
            key: 'Enter',
            code: 'Enter',
            bubbles: true
        }));
    }, barcode.length * 30 + 100);
};

console.log('💡 Run testBarcodeScan() to test manually');