import type { DailyFeeds, DailyPayload, FeedItem } from '../types/daily';

export type { DailyFeeds, DailyPayload, FeedItem } from '../types/daily';

export const fallbackDailyDate = '2026-05-13';

function item(i: number, title: string, host: string): FeedItem {
	return {
		title: `${title} · 条目 ${i + 1}`,
		url: `https://${host}/?n=${i}`,
		source: host,
	};
}

function list(prefix: string, host: string, n: number): FeedItem[] {
	return Array.from({ length: n }, (_, i) => item(i, prefix, host));
}

function splitList(cnPrefix: string, intlPrefix: string, n: number): { cn: FeedItem[]; intl: FeedItem[] } {
	return {
		cn: list(cnPrefix, 'cn-media.example', n),
		intl: list(intlPrefix, 'intl-media.example', n),
	};
}

/** 浏览器拉不到 daily.json 或 JSON 无效时使用 */
export const fallbackFeeds: DailyFeeds = {
	nba: splitList('NBA（国内媒体）', 'NBA（国外媒体）', 10),
	stocks: {
		cn: list('A股焦点', 'finance-cn.example', 10),
		intl: list('国际市场', 'finance-intl.example', 10),
	},
	ai: {
		cn: list('国内 AI 编程', 'ai-cn.example', 10),
		intl: list('海外 AI 编程', 'ai-intl.example', 10),
	},
	play: splitList('Play 游戏（国内）', 'Play 游戏（国外）', 10),
	world: splitList('国际形势（国内媒体）', '国际形势（国外媒体）', 10),
};

function isFeedItem(v: unknown): v is FeedItem {
	if (!v || typeof v !== 'object') return false;
	const o = v as Record<string, unknown>;
	return typeof o.title === 'string' && typeof o.url === 'string';
}

function isFeedItemArray(x: unknown): x is FeedItem[] {
	return Array.isArray(x) && x.every((i) => isFeedItem(i));
}

function isCnIntlBlock(v: unknown): v is { cn: FeedItem[]; intl: FeedItem[] } {
	if (!v || typeof v !== 'object' || Array.isArray(v)) return false;
	const o = v as Record<string, unknown>;
	return isFeedItemArray(o.cn) && isFeedItemArray(o.intl);
}

export function isDailyPayload(v: unknown): v is DailyPayload {
	if (!v || typeof v !== 'object') return false;
	const o = v as Record<string, unknown>;
	if (typeof o.dailyDate !== 'string' || !o.dailyDate) return false;
	const feeds = o.feeds;
	if (!feeds || typeof feeds !== 'object' || Array.isArray(feeds)) return false;
	const f = feeds as Record<string, unknown>;
	if (!isCnIntlBlock(f.nba) || !isCnIntlBlock(f.play) || !isCnIntlBlock(f.world)) return false;
	const stocks = f.stocks;
	const ai = f.ai;
	if (!stocks || typeof stocks !== 'object' || Array.isArray(stocks)) return false;
	const s = stocks as Record<string, unknown>;
	if (!isFeedItemArray(s.cn) || !isFeedItemArray(s.intl)) return false;
	if (!ai || typeof ai !== 'object' || Array.isArray(ai)) return false;
	const a = ai as Record<string, unknown>;
	if (!isFeedItemArray(a.cn) || !isFeedItemArray(a.intl)) return false;
	return true;
}

export type TabId = 'nba' | 'stocks' | 'ai' | 'play' | 'world';

export const tabs: { id: TabId; label: string; hint: string }[] = [
	{ id: 'nba', label: 'NBA', hint: '国内 10 · 国外 10' },
	{ id: 'stocks', label: '股票', hint: 'A股 10 · 国际 10' },
	{ id: 'ai', label: 'AI 编程', hint: '国内 10 · 国际 10' },
	{ id: 'play', label: 'Play 游戏', hint: '国内 10 · 国外 10' },
	{ id: 'world', label: '国际形势', hint: '国内 10 · 国外 10' },
];
