const fs = require('fs');

console.log('🔥 LOẠI BỎ TRIỆT ĐỂ CUSTOMER KHỎI DỰ ÁN...');

const salesFile = 'c:/shop/client/src/pages/sales.tsx';
let content = fs.readFileSync(salesFile, 'utf8');

console.log('📝 Đang xóa hoàn toàn customer imports...');
// Fix import statement to remove Customer
content = content.replace(
  /import type \{ Product, Customer \} from "@\/types\/backend-types";/,
  'import type { Product } from "@/types/backend-types";'
);

console.log('📝 Đang xóa tất cả customer state và variables...');
// Remove all customer-related state variables
const customerStatePatterns = [
  /\s*const \[selectedCustomer, setSelectedCustomer\] = useState<Customer \| null>\(null\);/g,
  /\s*\/\/ Customer search state\s*/g,
  /\s*const \[customerSearchTerm, setCustomerSearchTerm\] = useState\(""\);/g,
  /\s*const \[showCustomerDropdown, setShowCustomerDropdown\] = useState\(false\);/g,
  /\s*\/\/ State for quick customer creation\s*/g,
  /\s*const \[showQuickCustomerForm, setShowQuickCustomerForm\] = useState\(false\);/g,
  /\s*const \[quickCustomerData, setQuickCustomerData\] = useState\(\{[\s\S]*?\}\);/g
];

customerStatePatterns.forEach(pattern => {
  content = content.replace(pattern, '');
});

console.log('📝 Đang xóa customer API query...');
// Remove customer query completely
content = content.replace(
  /\s*\/\/ Fetch products and customers[\s\S]*?const \{ data: customers = \[\] \} = useQuery<Customer\[\]>\(\{[\s\S]*?\}\);/,
  ''
);

console.log('📝 Đang xóa customer dependency...');
// Remove selectedCustomer from dependencies
content = content.replace(/, selectedCustomer\]/g, ']');

console.log('📝 Đang xóa customer logic trong loadOrderForEdit...');
// Remove customer logic from loadOrderForEdit
content = content.replace(
  /\s*\/\/ Set customer if available[\s\S]*?if \(orderDetail\.customer\) \{[\s\S]*?setSelectedCustomer\(orderDetail\.customer\);[\s\S]*?\}/,
  ''
);

console.log('📝 Đang tìm và xóa customer UI components...');
// Search for customer UI sections and mark them for manual removal
const lines = content.split('\n');
let inCustomerUI = false;
let braceCount = 0;
let startLine = -1;
const customerUIRanges = [];

for (let i = 0; i < lines.length; i++) {
  const line = lines[i];
  
  // Detect customer UI sections
  if (line.includes('Khách hàng') || line.includes('khách hàng') || 
      line.includes('selectedCustomer') || line.includes('customerSearchTerm') ||
      line.includes('showCustomerDropdown') || line.includes('quickCustomer')) {
    
    if (!inCustomerUI) {
      inCustomerUI = true;
      startLine = i;
      braceCount = 0;
    }
  }
  
  if (inCustomerUI) {
    // Count braces to find the end of the section
    braceCount += (line.match(/\{/g) || []).length;
    braceCount -= (line.match(/\}/g) || []).length;
    
    if (braceCount <= 0 && line.trim().endsWith('>')) {
      customerUIRanges.push({ start: startLine, end: i });
      inCustomerUI = false;
    }
  }
}

console.log(`📝 Tìm thấy ${customerUIRanges.length} customer UI sections để xóa`);

// Remove customer UI sections from bottom to top to preserve line numbers
for (let i = customerUIRanges.length - 1; i >= 0; i--) {
  const range = customerUIRanges[i];
  lines.splice(range.start, range.end - range.start + 1);
  console.log(`   Xóa lines ${range.start}-${range.end}`);
}

content = lines.join('\n');

console.log('💾 Đang lưu file...');
fs.writeFileSync(salesFile, content);

console.log('✅ ĐÃ LOẠI BỎ CUSTOMER! Kiểm tra lại...');

// Quick verification
const finalContent = fs.readFileSync(salesFile, 'utf8');
const customerMatches = finalContent.match(/customer|Customer/gi);
if (customerMatches) {
  console.log(`⚠️ Vẫn còn ${customerMatches.length} customer references`);
  customerMatches.forEach((match, index) => {
    if (index < 5) console.log(`   ${match}`);
  });
} else {
  console.log('✅ ĐÃ LOẠI BỎ HOÀN TOÀN CUSTOMER!');
}