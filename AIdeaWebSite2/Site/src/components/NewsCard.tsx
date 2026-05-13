import type { FeedItem } from '../data/daily';

export function NewsCard({ item, rank }: { item: FeedItem; rank: number }) {
	return (
		<li className="group relative flex gap-3 rounded-xl border border-white/[0.06] bg-[var(--color-surface)]/80 px-3 py-2.5 shadow-sm backdrop-blur-sm transition hover:border-amber-400/25 hover:bg-[var(--color-surface-2)]/90">
			<span className="font-mono text-xs leading-6 text-zinc-500 tabular-nums group-hover:text-amber-400/80">
				{String(rank).padStart(2, '0')}
			</span>
			<div className="min-w-0 flex-1">
				<a
					href={item.url}
					target="_blank"
					rel="noreferrer noopener"
					className="block font-medium leading-snug text-zinc-100 no-underline decoration-transparent underline-offset-2 hover:text-white"
				>
					{item.title}
				</a>
				{item.source ? (
					<p className="mt-0.5 truncate text-xs text-zinc-500">{item.source}</p>
				) : null}
			</div>
		</li>
	);
}
