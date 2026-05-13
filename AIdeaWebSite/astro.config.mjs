// @ts-check

import mdx from '@astrojs/mdx';
import sitemap from '@astrojs/sitemap';
import { defineConfig, fontProviders } from 'astro/config';

/** GitHub Actions 里会注入；本地开发可不改。带子路径时保留尾部 `/`，便于 `new URL('rss.xml', site)` 等解析正确 */
let site = process.env.PUBLIC_ASTRO_SITE ?? 'https://example.com';
if (!site.endsWith('/')) site = `${site}/`;
const baseRaw = (process.env.PUBLIC_ASTRO_BASE ?? '').trim();
const base =
	baseRaw && baseRaw !== '/'
		? `${baseRaw.startsWith('/') ? baseRaw : `/${baseRaw}`}`.replace(/\/+$/, '') + '/'
		: undefined;

// https://astro.build/config
export default defineConfig({
	site,
	...(base ? { base } : {}),
	integrations: [mdx(), sitemap()],
	fonts: [
		{
			provider: fontProviders.local(),
			name: 'Atkinson',
			cssVariable: '--font-atkinson',
			fallbacks: ['sans-serif'],
			options: {
				variants: [
					{
						src: ['./src/assets/fonts/atkinson-regular.woff'],
						weight: 400,
						style: 'normal',
						display: 'swap',
					},
					{
						src: ['./src/assets/fonts/atkinson-bold.woff'],
						weight: 700,
						style: 'normal',
						display: 'swap',
					},
				],
			},
		},
	],
});
