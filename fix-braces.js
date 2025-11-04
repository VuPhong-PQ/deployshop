import fs from 'fs';

// Read the file
const content = fs.readFileSync('c:/shop/client/src/pages/sales.tsx', 'utf8');
const lines = content.split('\n');

let openBraces = 0;
let result = [];

for (let i = 0; i < lines.length; i++) {
  const line = lines[i];
  
  // Count opening braces
  const openCount = (line.match(/\{/g) || []).length;
  // Count closing braces  
  const closeCount = (line.match(/\}/g) || []).length;
  
  openBraces += openCount - closeCount;
  result.push(line);
  
  console.log(`Line ${i + 1}: ${openBraces} open braces`);
  
  // If we reach negative braces, there's an extra closing brace
  if (openBraces < 0) {
    console.log(`ERROR: Extra closing brace at line ${i + 1}: ${line}`);
  }
}

console.log(`Final open braces: ${openBraces}`);

if (openBraces > 0) {
  console.log(`Need to add ${openBraces} closing braces`);
} else if (openBraces < 0) {
  console.log(`Need to remove ${Math.abs(openBraces)} closing braces`);
}