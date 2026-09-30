import { createProxyMiddleware } from 'http-proxy-middleware';

export default function (app) {
  app.use(
    '/api',
    createProxyMiddleware({
  target: process.env.REACT_APP_API_BASE_URL || (import.meta.env.VITE_API_BASE_URL||'http://101.53.9.75:5273'),
      changeOrigin: true,
    })
  );
}
