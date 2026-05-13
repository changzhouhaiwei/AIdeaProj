import type { FeedItem } from '../data/daily';
import { NewsCard } from './NewsCard';

export function Column({
	title,
	sub,
	items,
}: {
	title: string;
	sub?: string;
	items: readonly FeedItem[];
}) {
	return (
		<section className="flex min-w-0 flex-1 flex-col gap-3">
			<header className="flex flex-wrap items-end justify-between gap-2 border-b border-white/[0.07] pb-2">
				<div>
					<h2 className="text-sm font-semibold tracking-wide text-zinc-200">{title}</h2>
					{sub ? <p className="text-xs text-zinc-500">{sub}</p> : null}
				</div>
				<span className="rounded-md border border-white/[0.08] bg-white/[0.03] px-2 py-0.5 font-mono text-[10px] text-zinc-400">
					{items.length} 条
				</span>
			</header>
			<ol className="flex flex-col gap-2">
				{items.map((it, i) => (
					<NewsCard key={`${title}-${i}`} item={it} rank={i + 1} />
				))}
			</ol>
		</section>
	);
}
