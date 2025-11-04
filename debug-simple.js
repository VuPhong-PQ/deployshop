import fs from 'fs';

console.log('Fixing API mutation debug...');

// Read the file
let content = fs.readFileSync('client/src/pages/sales.tsx', 'utf8');

// Find and replace the mutation function
const oldPattern = `mutationFn: async ({ orderId, formData }: { orderId?: number, formData: FormData }) => {
      const jsonData = formDataToJson(formData);
      
      if (orderId) {`;

const newCode = `mutationFn: async ({ orderId, formData }: { orderId?: number, formData: FormData }) => {
      const jsonData = formDataToJson(formData);
      
      console.log('=== MUTATION DEBUG ===');
      console.log('Has orderId:', !!orderId);
      console.log('JSON Data:', JSON.stringify(jsonData, null, 2));
      
      const headers = { 
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      };
      
      if (orderId) {`;

if (content.includes(oldPattern)) {
  content = content.replace(oldPattern, newCode);
  console.log('✅ Added debug logging to mutation');
} else {
  console.log('❌ Pattern not found');
}

// Write back
fs.writeFileSync('client/src/pages/sales.tsx', content);
console.log('✅ File updated');