#!/usr/bin/env node
/**
 * 数据源管线（推荐）：
 * 1. 在 Node 里请求 RSS / 自建 RSSHub，不受浏览器 CORS 限制。
 * 2. 汇总为 public/daily.json；构建时复制到 dist，前端 fetch 同域读取。
 * 3. 定时：本机任务计划程序 / GitHub Actions cron 在 build 前执行本脚本。
 *
 * 合规：控制频率、遵守 robots 与各站 ToS；商业或大规模请换官方 API 或购买数据。
 */
import Parser from 'rss-parser';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, '..');
const outFile = join(root, 'public', 'daily.json');

const parser = new Parser({
	timeout: 25000,
	maxRedirects: 5,
	headers: {
		'User-Agent':
			'Mozilla/5.0 (compatible; AIdeaWebSite2-daily/1.0; personal RSS reader)',
		Accept: 'application/rss+xml, application/xml, text/xml, */*;q=0.8',
	},
});

/**
 * @typedef {{ title: string, url: string, source?: string }} FeedItem
 */

function host(url) {
	try {
		return new URL(url).hostname.replace(/^www\./, '');
	} catch {
		return undefined;
	}
}

/**
 * @param {string} url
 * @param {number} limit
 * @returns {Promise<FeedItem[]>}
 */
async function takeFromFeed(url, limit) {
	try {
		const feed = await parser.parseURL(url);
		const items = feed.items ?? [];
		/** @type {FeedItem[]} */
		const out = [];
		for (const it of items) {
			if (out.length >= limit) break;
			const title = (it.title ?? '').replace(/\s+/g, ' ').trim();
			const guid = it.guid;
			const guidStr =
				typeof guid === 'string' ? guid : guid && typeof guid === 'object' && '#text' in guid ? String(guid['#text']) : '';
			const link = (it.link ?? guidStr).trim();
			if (!title || !link) continue;
			out.push({
				title,
				url: link,
				source: feed.title ? String(feed.title) : host(link),
			});
		}
		return out;
	} catch (e) {
		const msg = e instanceof Error ? e.message : String(e);
		console.warn('[fetch-daily] 源失败', url, msg);
		return [];
	}
}

/**
 * 按 URL 顺序依次拉取，去重后凑满 limit（前面源失败时自动用后面的）。
 * @param {string[]} urls
 * @param {number} limit
 * @returns {Promise<FeedItem[]>}
 */
async function takeFromFeedChain(urls, limit) {
	/** @type {FeedItem[]} */
	const out = [];
	const seen = new Set();
	for (const url of urls) {
		if (out.length >= limit) break;
		const need = limit - out.length;
		const batch = await takeFromFeed(url, need + 20);
		for (const item of batch) {
			if (out.length >= limit) break;
			let key = item.url.trim();
			try {
				const u = new URL(key);
				u.hash = '';
				key = u.toString();
			} catch {
				key = key.slice(0, 200);
			}
			if (seen.has(key)) continue;
			seen.add(key);
			out.push(item);
		}
	}
	return out;
}

/**
 * 多源兜底；栏目与仓库 `1.md` 对齐：NBA / Play / 国际形势 均为「国内 10 + 国外 10」。
 * @type {Record<string, { urls: string[], limit: number }>}
 */
const SOURCES = {
	nbaCn: {
		urls: [
			'https://www.chinanews.com.cn/rss/sports.xml',
			'https://rss.sina.com.cn/roll/sports/hot_roll.xml',
			'https://news.google.com/rss/search?q=NBA&hl=zh-CN&gl=CN&ceid=CN:zh-Hans',
		],
		limit: 10,
	},
	nbaIntl: {
		urls: ['https://www.espn.com/espn/rss/nba/news', 'https://www.nba.com/news/rss.xml'],
		limit: 10,
	},
	stocksCn: {
		urls: [
			'https://rss.sina.com.cn/roll/finance/hot_roll.xml',
			'https://www.chinanews.com.cn/rss/finance.xml',
			'https://news.google.com/rss/search?q=A%E8%82%A1&hl=zh-CN&gl=CN&ceid=CN:zh-Hans',
		],
		limit: 10,
	},
	stocksIntl: {
		urls: [
			'https://finance.yahoo.com/news/rssindex',
			'https://www.marketwatch.com/rss/topstories',
			'https://news.google.com/rss/search?q=global+stock+market&hl=en-US&gl=US&ceid=US:en',
		],
		limit: 10,
	},
	aiCn: {
		urls: [
			'https://www.oschina.net/news/rss',
			'https://www.ithome.com/rss/',
			'https://www.solidot.org/index.rss',
			'https://news.google.com/rss/search?q=AI+%E7%BC%96%E7%A8%8B&hl=zh-CN&gl=CN&ceid=CN:zh-Hans',
		],
		limit: 10,
	},
	aiIntl: {
		urls: [
			'https://hnrss.org/newest?q=AI&count=30',
			'https://hnrss.org/newest?q=LLM&count=30',
			'https://www.theverge.com/rss/ai-artificial-intelligence/index.xml',
		],
		limit: 10,
	},
	playCn: {
		urls: [
			'https://news.google.com/rss/search?q=Google+Play+%E6%B8%B8%E6%88%8F&hl=zh-CN&gl=CN&ceid=CN:zh-Hans',
			'https://www.ithome.com/rss/',
		],
		limit: 10,
	},
	playIntl: {
		urls: [
			'https://www.theverge.com/rss/games/index.xml',
			'https://www.androidpolice.com/feed/',
			'https://www.polygon.com/rss/index.xml',
		],
		limit: 10,
	},
	worldCn: {
		urls: [
			'https://www.chinanews.com.cn/rss/world.xml',
			'https://www.chinadaily.com.cn/rss/china_rss.xml',
		],
		limit: 10,
	},
	worldIntl: {
		urls: [
			'https://news.yahoo.com/rss/world',
			'https://feeds.bbci.co.uk/news/world/rss.xml',
			'https://www.chinadaily.com.cn/rss/world_rss.xml',
		],
		limit: 10,
	},
};

/**
 * 读取上一版 daily.json 中的分栏数据；兼容旧版「单数组」结构。
 * @param {Record<string, unknown> | undefined} feeds
 * @param {'nba'|'play'|'world'} key
 */
function readSplitPrev(feeds, key) {
	const v = feeds?.[key];
	if (v && typeof v === 'object' && !Array.isArray(v)) {
		const o = /** @type {Record<string, unknown>} */ (v);
		const cn = Array.isArray(o.cn) ? o.cn : undefined;
		const intl = Array.isArray(o.intl) ? o.intl : undefined;
		if (cn || intl) return { cn, intl };
	}
	if (Array.isArray(v)) {
		if (key === 'world' && v.length > 10) {
			return { cn: v.slice(0, 10), intl: v.slice(10, 20) };
		}
		return { cn: undefined, intl: v };
	}
	return { cn: undefined, intl: undefined };
}

/**
 * @param {FeedItem[]} next
 * @param {FeedItem[] | undefined} prev
 * @param {number} limit
 */
function useOrPrev(next, prev, limit) {
	const base = next.length > 0 ? next : prev ?? [];
	return base.slice(0, limit);
}

async function main() {
	/** @type {{ feeds?: Record<string, unknown> } | null} */
	let prevRoot = null;
	try {
		prevRoot = JSON.parse(readFileSync(outFile, 'utf8'));
	} catch {
		prevRoot = null;
	}
	const p = prevRoot?.feeds ?? {};
	const prevNba = readSplitPrev(p, 'nba');
	const prevPlay = readSplitPrev(p, 'play');
	const prevWorld = readSplitPrev(p, 'world');
	const ps = p.stocks && typeof p.stocks === 'object' ? p.stocks : {};
	const pcn = Array.isArray(ps.cn) ? ps.cn : undefined;
	const pintl = Array.isArray(ps.intl) ? ps.intl : undefined;
	const pa = p.ai && typeof p.ai === 'object' ? p.ai : {};
	const pacn = Array.isArray(pa.cn) ? pa.cn : undefined;
	const paintl = Array.isArray(pa.intl) ? pa.intl : undefined;

	const [
		nbaCn,
		nbaIntl,
		stocksCn,
		stocksIntl,
		aiCn,
		aiIntl,
		playCn,
		playIntl,
		worldCn,
		worldIntl,
	] = await Promise.all([
		takeFromFeedChain(SOURCES.nbaCn.urls, SOURCES.nbaCn.limit),
		takeFromFeedChain(SOURCES.nbaIntl.urls, SOURCES.nbaIntl.limit),
		takeFromFeedChain(SOURCES.stocksCn.urls, SOURCES.stocksCn.limit),
		takeFromFeedChain(SOURCES.stocksIntl.urls, SOURCES.stocksIntl.limit),
		takeFromFeedChain(SOURCES.aiCn.urls, SOURCES.aiCn.limit),
		takeFromFeedChain(SOURCES.aiIntl.urls, SOURCES.aiIntl.limit),
		takeFromFeedChain(SOURCES.playCn.urls, SOURCES.playCn.limit),
		takeFromFeedChain(SOURCES.playIntl.urls, SOURCES.playIntl.limit),
		takeFromFeedChain(SOURCES.worldCn.urls, SOURCES.worldCn.limit),
		takeFromFeedChain(SOURCES.worldIntl.urls, SOURCES.worldIntl.limit),
	]);

	const dailyDate = new Date().toISOString().slice(0, 10);
	const feeds = {
		nba: {
			cn: useOrPrev(nbaCn, prevNba.cn, SOURCES.nbaCn.limit),
			intl: useOrPrev(nbaIntl, prevNba.intl, SOURCES.nbaIntl.limit),
		},
		stocks: {
			cn: useOrPrev(stocksCn, pcn, SOURCES.stocksCn.limit),
			intl: useOrPrev(stocksIntl, pintl, SOURCES.stocksIntl.limit),
		},
		ai: {
			cn: useOrPrev(aiCn, pacn, SOURCES.aiCn.limit),
			intl: useOrPrev(aiIntl, paintl, SOURCES.aiIntl.limit),
		},
		play: {
			cn: useOrPrev(playCn, prevPlay.cn, SOURCES.playCn.limit),
			intl: useOrPrev(playIntl, prevPlay.intl, SOURCES.playIntl.limit),
		},
		world: {
			cn: useOrPrev(worldCn, prevWorld.cn, SOURCES.worldCn.limit),
			intl: useOrPrev(worldIntl, prevWorld.intl, SOURCES.worldIntl.limit),
		},
	};

	mkdirSync(dirname(outFile), { recursive: true });
	writeFileSync(outFile, JSON.stringify({ dailyDate, feeds }, null, 2), 'utf8');
	console.log('[fetch-daily] 已写入', outFile);
	console.log(
		'条数:',
		`nba.cn=${nbaCn.length}, nba.intl=${nbaIntl.length}, stocks.cn=${stocksCn.length}, stocks.intl=${stocksIntl.length}, ai.cn=${aiCn.length}, ai.intl=${aiIntl.length}, play.cn=${playCn.length}, play.intl=${playIntl.length}, world.cn=${worldCn.length}, world.intl=${worldIntl.length}`,
	);
}

main().catch((e) => {
	console.error(e);
	process.exitCode = 1;
});
