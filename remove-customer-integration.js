import fs from 'fs';

console.log('Removing customer integration from orders...');

// Read the file
let content = fs.readFileSync('client/src/pages/sales.tsx', 'utf8');

// 1. Remove customer selection UI - find customer selection section
// We'll replace the customer selection with just a simple "Khách vãng lai"

// 2. In formDataToJson, always set customerId to null
const formDataPattern = /if \(key === 'customerId'\) \{[\s\S]*?\} else if/;
const newCustomerIdLogic = `if (key === 'customerId') {
          // Always set to null - no customer integration for now
          obj[key] = null;
        } else if`;

if (formDataPattern.test(content)) {
  content = content.replace(formDataPattern, newCustomerIdLogic);
  console.log('✅ Fixed customerId to always be null');
}

// 3. In createNewPendingOrder, remove customer logic
const createOrderPattern = /formData\.append\('customerId', selectedCustomer\?\.\w+ \|\| currentReopenedOrder\?\.\w+ \|\| '[^']*'\);/;
const newCustomerIdAppend = `formData.append('customerId', '0'); // No customer integration`;

if (createOrderPattern.test(content)) {
  content = content.replace(createOrderPattern, newCustomerIdAppend);
  console.log('✅ Fixed customerId in createNewPendingOrder');
}

// 4. In updateExistingOrder, remove customer logic
const updateOrderPattern = /formData\.append\('customerId', selectedCustomer\?\.\w+ \|\| currentReopenedOrder\?\.\w+ \|\| '[^']*'\);/;

if (updateOrderPattern.test(content)) {
  content = content.replace(updateOrderPattern, newCustomerIdAppend);
  console.log('✅ Fixed customerId in updateExistingOrder');
}

// 5. Fix customer name display to always be "Khách vãng lai"
const customerNamePatterns = [
  /const customerName = selectedCustomer\?\.\w+ \|\| currentReopenedOrder\?\.\w+ \|\| "[^"]*";/g,
  /selectedCustomer\?\.\w+ \|\| "[^"]*"/g
];

customerNamePatterns.forEach((pattern, index) => {
  if (pattern.test(content)) {
    content = content.replace(pattern, '"Khách vãng lai"');
    console.log(`✅ Fixed customer name pattern ${index + 1}`);
  }
});

// 6. Remove customer search UI (comment out for now)
// We'll just disable the customer selection interface

// Write back the file
fs.writeFileSync('client/src/pages/sales.tsx', content);

console.log('🎉 Customer integration removed from orders!');
console.log('');
console.log('Changes made:');
console.log('- customerId always set to null in JSON');
console.log('- Customer name always "Khách vãng lai"');
console.log('- Removed customer selection logic');
console.log('');
console.log('Now try creating an order without customer complications!');