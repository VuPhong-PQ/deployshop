import fs from 'fs';

console.log('Replacing apiRequest with direct fetch...');

let content = fs.readFileSync('client/src/pages/sales.tsx', 'utf8');

// Replace the entire mutation function
const oldMutation = `mutationFn: async ({ orderId, formData }: { orderId?: number, formData: FormData }) => {
      if (orderId) {`;

const newMutation = `mutationFn: async ({ orderId, formData }: { orderId?: number, formData: FormData }) => {
      const jsonData = formDataToJson(formData);
      console.log('=== MUTATION DEBUG ===');
      console.log('Has orderId:', !!orderId);
      console.log('JSON Data:', JSON.stringify(jsonData, null, 2));
      
      // Use direct fetch instead of apiRequest to avoid header issues
      const url = orderId ? \`/api/orders/\${orderId}\` : '/api/orders';
      const method = orderId ? 'PUT' : 'POST';
      const fullUrl = \`http://101.53.9.76:5273\${url}\`;
      
      const currentStore = JSON.parse(localStorage.getItem('currentStore') || 'null');
      const storeId = currentStore?.storeId;
      
      const headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      };
      
      if (storeId) {
        headers['X-Store-Id'] = storeId.toString();
      }
      
      console.log('Direct fetch - URL:', fullUrl);
      console.log('Direct fetch - Method:', method);
      console.log('Direct fetch - Headers:', headers);
      console.log('Direct fetch - Body:', JSON.stringify(jsonData));
      
      const response = await fetch(fullUrl, {
        method,
        headers,
        body: JSON.stringify(jsonData),
        credentials: 'include'
      });
      
      console.log('Direct fetch - Response status:', response.status);
      console.log('Direct fetch - Response ok:', response.ok);
      
      if (!response.ok) {
        const errorText = await response.text();
        console.error('Direct fetch - Error:', errorText);
        throw new Error(\`\${response.status}: \${errorText}\`);
      }
      
      const result = await response.json();
      console.log('Direct fetch - Success result:', result);
      return result;
      
      if (orderId) {`;

if (content.includes(oldMutation)) {
  content = content.replace(oldMutation, newMutation);
  console.log('✅ Replaced mutation with direct fetch');
} else {
  console.log('❌ Could not find mutation pattern');
}

fs.writeFileSync('client/src/pages/sales.tsx', content);
console.log('✅ File updated with direct fetch implementation');