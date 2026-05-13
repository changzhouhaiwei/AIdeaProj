import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

const base = (process.env.VITE_BASE ?? '/').replace(/\/?$/, '/') as `/${string}`;

export default defineConfig({
	plugins: [react(), tailwindcss()],
	base,
	server: {
		// 避免只绑 IPv4 时，部分环境下访问 localhost 异常；并打印 Network 地址便于局域网调试
		host: true,
		port: 5173,
		strictPort: false,
	},
	preview: {
		host: true,
		port: 4173,
		strictPort: false,
	},
});
