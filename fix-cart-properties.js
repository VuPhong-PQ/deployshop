import fs from 'fs';

// Read the file
const content = fs.readFileSync('c:/shop/client/src/pages/sales.tsx', 'utf8');

// Fix specific issues
let newContent = content
  .replace(/item\.productId\?\.toString\(\) \|\| ""/g, 'item.productId.toString()')
  .replace(/item\.quantity\?\.toString\(\) \|\| "1"/g, 'item.quantity.toString()')
  .replace(/item\.price\?\.toString\(\) \|\| "0"/g, 'item.price.toString()')
  .replace(/item\.totalPrice\?\.toString\(\) \|\| "0"/g, 'item.totalPrice.toString()');

// Write back
fs.writeFileSync('c:/shop/client/src/pages/sales.tsx', newContent, 'utf8');
console.log('Fixed cart item properties');