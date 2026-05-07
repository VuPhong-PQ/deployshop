import { createProxyMiddleware } from 'http-proxy-middleware';

export default function (app) {
  app.use(
    '/api',
    createProxyMiddleware({
  target: process.env.REACT_APP_API_BASE_URL || (import.meta.env.VITE_API_BASE_URL||'http://localhost:5273'),
      changeOrigin: true,
    })
  );
}
