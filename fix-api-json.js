import fs from 'fs';

// Read the file
let content = fs.readFileSync('client/src/pages/sales.tsx', 'utf8');

// Replace FormData API calls with JSON
content = content.replace(
  /return await apiRequest\(`\/api\/orders\/\$\{orderId\}`, \{ method: 'PUT', body: formData \}\);/g,
  `const jsonData = formDataToJson(formData);
        console.log('Cập nhật đơn hàng pending:', orderId, jsonData);
        return await apiRequest(\`/api/orders/\${orderId}\`, { 
          method: 'PUT', 
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(jsonData) 
        });`
);

content = content.replace(
  /return await apiRequest\('\/api\/orders', \{ method: 'POST', body: formData \}\);/g,
  `const jsonData = formDataToJson(formData);
        console.log('Gửi đơn hàng chờ thanh toán lên backend:', jsonData);
        return await apiRequest('/api/orders', { 
          method: 'POST', 
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(jsonData) 
        });`
);

// Also fix the complete order mutation
content = content.replace(
  /return await apiRequest\(`\/api\/orders\/\$\{orderId\}\/complete`, \{ method: 'PUT', body: formData \}\);/g,
  `const jsonData = formDataToJson(formData);
      console.log('Hoàn thành đơn hàng:', orderId, jsonData);
      return await apiRequest(\`/api/orders/\${orderId}/complete\`, { 
        method: 'PUT', 
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(jsonData) 
      });`
);

// Write back the file
fs.writeFileSync('client/src/pages/sales.tsx', content);

console.log('Updated API calls to use JSON instead of FormData');