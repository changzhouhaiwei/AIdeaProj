import { useEffect, useMemo, useState } from 'react';
import { Column } from './components/Column';
import { TabBar } from './components/TabBar';
import {
	fallbackDailyDate,
	fallbackFeeds,
	isDailyPayload,
	tabs,
	type DailyFeeds,
	type TabId,
} from './data/daily';

function dailyJsonHref(): string {
	const base = import.meta.env.BASE_URL;
	const normalized = base.endsWith('/') ? base : `${base}/`;
	return `${normalized}daily.json`;
}

export function App() {
	const [tab, setTab] = useState<TabId>('nba');
	const [feeds, setFeeds] = useState<DailyFeeds>(fallbackFeeds);
	const [dailyDate, setDailyDate] = useState(fallbackDailyDate);
	const [source, setSource] = useState<'remote' | 'fallback'>('fallback');

	useEffect(() => {
		let cancelled = false;
		const ac = new AbortController();
		void (async () => {
			try {
				const r = await fetch(dailyJsonHref(), { cache: 'no-store', signal: ac.signal });
				if (!r.ok) throw new Error(String(r.status));
				const json: unknown = await r.json();
				if (cancelled) return;
				if (isDailyPayload(json)) {
					setFeeds(json.feeds);
					setDailyDate(json.dailyDate);
					setSource('remote');
				}
			} catch {
				if (!cancelled) setSource('fallback');
			}
		})();
		return () => {
			cancelled = true;
			ac.abort();
		};
	}, []);

	const body = useMemo(() => {
		switch (tab) {
			case 'nba':
				return (
					<div className="grid gap-8 lg:grid-cols-2">
						<Column title="国内媒体" sub={`数据日期 ${dailyDate}`} items={feeds.nba.cn} />
						<Column title="国外媒体" items={feeds.nba.intl} />
					</div>
				);
			case 'stocks':
				return (
					<div className="grid gap-8 lg:grid-cols-2">
						<Column title="A股" items={feeds.stocks.cn} />
						<Column title="国际" items={feeds.stocks.intl} />
					</div>
				);
			case 'ai':
				return (
					<div className="grid gap-8 lg:grid-cols-2">
						<Column title="国内" items={feeds.ai.cn} />
						<Column title="国际" items={feeds.ai.intl} />
					</div>
				);
			case 'play':
				return (
					<div className="grid gap-8 lg:grid-cols-2">
						<Column title="国内" sub={`数据日期 ${dailyDate}`} items={feeds.play.cn} />
						<Column title="国外" items={feeds.play.intl} />
					</div>
				);
			case 'world':
				return (
					<div className="grid gap-8 lg:grid-cols-2">
						<Column title="国内媒体" sub={`数据日期 ${dailyDate}`} items={feeds.world.cn} />
						<Column title="国外媒体" items={feeds.world.intl} />
					</div>
				);
			default:
				return null;
		}
	}, [tab, dailyDate, feeds]);

	return (
		<div className="mx-auto flex min-h-dvh max-w-6xl flex-col px-4 pb-16 pt-8 sm:px-6">
			<header className="mb-6 flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
				<div>
					<p className="font-mono text-[11px] uppercase tracking-[0.2em] text-amber-400/80">Daily digest</p>
					<h1 className="mt-1 text-2xl font-semibold tracking-tight text-white sm:text-3xl">每日热榜聚合</h1>
					<p className="mt-2 max-w-xl text-sm leading-relaxed text-zinc-400">
						数据来自构建或定时任务生成的{' '}
						<code className="rounded bg-white/[0.06] px-1 py-0.5 font-mono text-xs text-amber-100/90">daily.json</code>
						（由 <code className="rounded bg-white/[0.06] px-1 font-mono text-xs">npm run fetch:daily</code> 写入{' '}
						<code className="rounded bg-white/[0.06] px-1 font-mono text-xs">public/</code>
						）。栏目条数与需求见仓库 <code className="rounded bg-white/[0.06] px-1 font-mono text-xs">1.md</code>
						。拉取失败时自动使用内置占位。
					</p>
				</div>
				<div className="rounded-lg border border-white/[0.08] bg-white/[0.03] px-3 py-2 text-right">
					<p className="text-[10px] uppercase tracking-wider text-zinc-500">Snapshot</p>
					<p className="font-mono text-sm text-zinc-200">{dailyDate}</p>
					<p className="mt-1 text-[10px] text-zinc-500">{source === 'remote' ? 'daily.json' : '内置占位'}</p>
				</div>
			</header>

			<TabBar active={tab} onChange={setTab} tabs={tabs} />

			<main className="mt-8 flex-1">{body}</main>

			<footer className="mt-16 border-t border-white/[0.06] pt-6 text-center text-xs text-zinc-500">
				仅聚合公开链接标题；请控制抓取频率并遵守各站点服务条款与 robots 规则。
			</footer>
		</div>
	);
}
