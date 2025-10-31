// Test Route Preservation
console.log("=== ROUTE PRESERVATION DEBUG ===");

// Check localStorage on page load
window.addEventListener('load', () => {
  console.log("🔄 Page loaded");
  console.log("📍 Current URL:", window.location.pathname);
  console.log("💾 Intended route in localStorage:", localStorage.getItem("intendedRoute"));
  console.log("👤 User in localStorage:", !!localStorage.getItem("user"));
  console.log("🏪 Store in localStorage:", !!localStorage.getItem("currentStore"));
});

// Listen for route changes
let lastLocation = window.location.pathname;
setInterval(() => {
  if (window.location.pathname !== lastLocation) {
    console.log("🔄 Route changed:", lastLocation, "→", window.location.pathname);
    lastLocation = window.location.pathname;
  }
}, 100);

// Override setLocation to add logging
const originalSetItem = localStorage.setItem;
localStorage.setItem = function(key, value) {
  if (key === 'intendedRoute') {
    console.log("💾 Setting intended route:", value);
  }
  return originalSetItem.call(this, key, value);
};

const originalRemoveItem = localStorage.removeItem;
localStorage.removeItem = function(key) {
  if (key === 'intendedRoute') {
    console.log("🗑️ Removing intended route");
  }
  return originalRemoveItem.call(this, key);
};