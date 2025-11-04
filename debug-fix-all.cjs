const fs = require('fs');

console.log('🔥 CHUẨN BI SCRIPT CẨN THẬN ĐỂ LOẠI BỎ CUSTOMER VÀ FIX ORDER API...');

const salesFile = 'c:/shop/client/src/pages/sales.tsx';
let content = fs.readFileSync(salesFile, 'utf8');
const originalContent = content;

console.log('📝 Step 1: Loại bỏ Customer import...');
content = content.replace(
  /import type \{ Product, Customer \} from "@\/types\/backend-types";/,
  'import type { Product } from "@/types/backend-types";'
);

console.log('📝 Step 2: Loại bỏ customer state variables...');
content = content.replace(/\s*const \[selectedCustomer, setSelectedCustomer\] = useState<Customer \| null>\(null\);/, '');
content = content.replace(/\s*\/\/ Customer search state\s*/, '');
content = content.replace(/\s*const \[customerSearchTerm, setCustomerSearchTerm\] = useState\(""\);/, '');
content = content.replace(/\s*const \[showCustomerDropdown, setShowCustomerDropdown\] = useState\(false\);/, '');
content = content.replace(/\s*\/\/ State for quick customer creation\s*/, '');
content = content.replace(/\s*const \[showQuickCustomerForm, setShowQuickCustomerForm\] = useState\(false\);/, '');

console.log('📝 Step 3: Loại bỏ customer từ dependencies...');
content = content.replace(/, selectedCustomer\]/g, ']');

console.log('📝 Step 4: Set customerId = null trong formDataToJson...');
content = content.replace(
  /obj\[key\] = \(val === '0' \|\| val === '' \|\| !val\) \? null : parseInt\(val\);/,
  'obj[key] = null; // Always null - no customer integration'
);

console.log('📝 Step 5: Loại bỏ customer API query...');
content = content.replace(
  /\s*\/\/ Fetch products and customers[\s\S]*?const \{ data: customers = \[\] \} = useQuery<Customer\[\]>\(\{[\s\S]*?\}\);/,
  ''
);

console.log('📝 Step 6: Hardcode customerName = "Khách vãng lai"...');
content = content.replace(
  /customerName: selectedCustomer\?\.customerName \|\| "Khách vãng lai"/g,
  'customerName: "Khách vãng lai"'
);

console.log('📝 Step 7: Thay thế saveOrderForLaterMutation với direct fetch...');
const saveOrderPattern = /mutationFn: async \(\{ orderId, formData \}[\s\S]*?return result;[\s\S]*?\}/;
const saveOrderMatch = content.match(saveOrderPattern);
if (saveOrderMatch) {
  const replacement = `mutationFn: async ({ orderId, formData }: { orderId?: number, formData: FormData }) => {
      try {
        console.log('📋 Lưu order cho sau:', { orderId, formData });
        const jsonData = formDataToJson(formData);
        console.log('📄 Converted JSON:', jsonData);
        
        const baseUrl = 'http://101.53.9.76:5273';
        const isUpdate = !!orderId;
        const url = isUpdate ? \`\${baseUrl}/api/orders/\${orderId}\` : \`\${baseUrl}/api/orders\`;
        
        console.log('🚀 Direct fetch - saveOrderForLaterMutation');
        console.log('📤 Method:', isUpdate ? 'PUT' : 'POST');
        console.log('📤 URL:', url);
        console.log('📤 Headers:', { 'Content-Type': 'application/json' });
        console.log('📤 Body:', JSON.stringify(jsonData));
        
        const response = await fetch(url, {
          method: isUpdate ? 'PUT' : 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(jsonData)
        });
        
        console.log('📥 Response status:', response.status);
        console.log('📥 Response headers:', Object.fromEntries(response.headers.entries()));
        
        if (!response.ok) {
          const errorText = await response.text();
          console.error('❌ Response error:', errorText);
          throw new Error(\`HTTP \${response.status}: \${errorText}\`);
        }
        
        const result = await response.json();
        console.log('✅ Save order success:', result);
        return result;
        
      } catch (error) {
        console.error('🔥 Fetch error:', error);
        throw error;
      }
    }`;
  
  content = content.replace(saveOrderPattern, replacement);
  console.log('✅ Đã thay thế saveOrderForLaterMutation');
} else {
  console.log('⚠️ Không tìm thấy saveOrderForLaterMutation pattern');
}

console.log('💾 Đang lưu file...');
fs.writeFileSync(salesFile, content);

// Verification
const changes = originalContent !== content;
console.log(changes ? '✅ FILE ĐÃ ĐƯỢC THAY ĐỔI' : '⚠️ KHÔNG CÓ THAY ĐỔI NÀO');

console.log('🎯 HOÀN THÀNH! File đã được cập nhật cẩn thận.');