import { createProxyMiddleware } from 'http-proxy-middleware';

export default function (app) {
  app.use(
    '/api',
    createProxyMiddleware({
      target: 'http://101.53.9.76:5273',
      changeOrigin: true,
    })
  );
}
