// Fix DS9208 Browser Integration - Final Solution
console.clear();
console.log('🔧 DS9208 Final Fix - Addressing Browser Security...');

// 1. Remove ALL existing event listeners
const removeAllListeners = () => {
    // Get all listeners and remove them
    const elements = [document, window, document.body];
    const events = ['keydown', 'keypress', 'keyup'];
    
    elements.forEach(el => {
        events.forEach(event => {
            el.removeEventListener(event, window.simpleBarcodeDetector, true);
            el.removeEventListener(event, window.debugBarcodeHandler, true);
            el.removeEventListener(event, window.exactBarcodeHandler, true);
            el.removeEventListener(event, window.ds9208Handler, true);
        });
    });
    
    console.log('🧹 Removed all existing listeners');
};

removeAllListeners();

// 2. Override browser security restrictions
const enableFullKeyboardAccess = () => {
    // Request keyboard access
    if (navigator.keyboard && navigator.keyboard.getLayoutMap) {
        navigator.keyboard.getLayoutMap().then(() => {
            console.log('⌨️ Keyboard access granted');
        }).catch(e => console.log('⌨️ Keyboard access denied:', e));
    }
    
    // Ensure document focus
    document.body.setAttribute('tabindex', '-1');
    document.body.focus();
    window.focus();
    
    // Click to activate
    document.body.click();
    
    console.log('🎯 Browser focus maximized');
};

enableFullKeyboardAccess();

// 3. Advanced DS9208 Event Interceptor
let scanBuffer = '';
let scanTimeout = null;
let eventCount = 0;
let lastEventTime = 0;

const ultimateDS9208Handler = (event) => {
    eventCount++;
    const now = performance.now();
    const timeDiff = now - lastEventTime;
    lastEventTime = now;
    
    // Log EVERYTHING
    console.log(`🎹 [${eventCount}] EVENT:`, {
        key: event.key,
        code: event.code,
        type: event.type,
        timing: Math.round(timeDiff),
        target: event.target?.tagName,
        isTrusted: event.isTrusted,
        ctrlKey: event.ctrlKey,
        altKey: event.altKey,
        metaKey: event.metaKey
    });
    
    // Handle special cases first
    if (event.key === 'Enter') {
        if (scanBuffer.length > 0) {
            console.log('🎯 DS9208 SCAN COMPLETE:', scanBuffer);
            processBarcodeFromDS9208(scanBuffer);
            scanBuffer = '';
        }
        return true;
    }
    
    // Capture digits and letters
    if (event.key.length === 1) {
        scanBuffer += event.key;
        console.log('📝 Scan buffer:', scanBuffer);
        
        // Clear previous timeout
        clearTimeout(scanTimeout);
        
        // Set timeout for auto-completion
        scanTimeout = setTimeout(() => {
            if (scanBuffer.length >= 5) { // Minimum barcode length
                console.log('⏰ Auto-complete scan:', scanBuffer);
                processBarcodeFromDS9208(scanBuffer);
                scanBuffer = '';
            } else {
                console.log('⏰ Buffer cleared (too short):', scanBuffer);
                scanBuffer = '';
            }
        }, 500);
    }
    
    return true;
};

const processBarcodeFromDS9208 = (barcode) => {
    console.log('🔄 Processing DS9208 barcode:', barcode);
    
    // Direct React state update
    const searchInput = document.querySelector('input[placeholder*="Tìm kiếm"]');
    if (searchInput) {
        console.log('📝 Updating search input...');
        
        // Clear current value
        searchInput.value = '';
        searchInput.focus();
        
        // Set new value with React-compatible method
        const nativeInputValueSetter = Object.getOwnPropertyDescriptor(
            window.HTMLInputElement.prototype,
            'value'
        ).set;
        
        nativeInputValueSetter.call(searchInput, barcode);
        
        // Trigger React events
        const inputEvent = new Event('input', { bubbles: true });
        const changeEvent = new Event('change', { bubbles: true });
        
        searchInput.dispatchEvent(inputEvent);
        searchInput.dispatchEvent(changeEvent);
        
        console.log('✅ Search updated with DS9208 barcode:', barcode);
        
        // Auto-add to cart if only one product found
        setTimeout(() => {
            checkAndAutoAddToCart(barcode);
        }, 500);
    } else {
        console.log('❌ Search input not found');
    }
};

const checkAndAutoAddToCart = (barcode) => {
    // Look for matching products in the displayed list
    const productCards = document.querySelectorAll('[class*="grid"] button:has([class*="Thêm vào hóa đơn"])');
    console.log('🔍 Found product cards:', productCards.length);
    
    if (productCards.length === 1) {
        console.log('🎯 Auto-adding single matching product to cart...');
        productCards[0].click();
    }
};

// 4. Install ultimate listeners with maximum priority
document.addEventListener('keydown', ultimateDS9208Handler, {
    capture: true,
    passive: false
});

document.addEventListener('keypress', ultimateDS9208Handler, {
    capture: true,
    passive: false
});

window.addEventListener('keydown', ultimateDS9208Handler, {
    capture: true,
    passive: false
});

// 5. Additional security bypass attempts
setTimeout(() => {
    // Force focus again
    document.body.focus();
    console.log('🔄 Re-focused document');
}, 1000);

console.log('✅ Ultimate DS9208 handler installed');
console.log('🎯 DS9208 should now work. Try scanning barcode...');
console.log('📊 Will log ALL keyboard events with timing analysis');

// Test function
window.testDS9208 = function() {
    console.log('🧪 Testing DS9208 simulation...');
    const testBarcode = '89900000011';
    
    testBarcode.split('').forEach((char, i) => {
        setTimeout(() => {
            ultimateDS9208Handler({
                key: char,
                code: `Digit${char}`,
                type: 'keydown',
                target: document.body,
                isTrusted: false
            });
        }, i * 30); // 30ms delay between chars (typical scanner speed)
    });
    
    setTimeout(() => {
        ultimateDS9208Handler({
            key: 'Enter',
            code: 'Enter',
            type: 'keydown',
            target: document.body,
            isTrusted: false
        });
    }, testBarcode.length * 30 + 100);
};

console.log('💡 Run testDS9208() to simulate perfect scan');