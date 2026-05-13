import type { TabId } from '../data/daily';

export function TabBar({
	active,
	onChange,
	tabs,
}: {
	active: TabId;
	onChange: (id: TabId) => void;
	tabs: readonly { id: TabId; label: string; hint: string }[];
}) {
	return (
		<nav
			className="sticky top-0 z-20 -mx-4 border-b border-white/[0.06] bg-[#07080c]/75 px-4 py-3 backdrop-blur-md sm:-mx-6 sm:px-6"
			aria-label="栏目"
		>
			<div className="flex gap-1 overflow-x-auto pb-0.5">
				{tabs.map((t) => {
					const on = t.id === active;
					return (
						<button
							key={t.id}
							type="button"
							onClick={() => onChange(t.id)}
							className={`shrink-0 rounded-full px-4 py-2 text-left text-sm font-medium transition ${
								on
									? 'bg-amber-400/15 text-amber-100 ring-1 ring-amber-400/35'
									: 'text-zinc-400 hover:bg-white/[0.05] hover:text-zinc-200'
							}`}
							aria-current={on ? 'page' : undefined}
						>
							<span className="block">{t.label}</span>
							<span className="mt-0.5 block text-[10px] font-normal leading-none text-zinc-500">{t.hint}</span>
						</button>
					);
				})}
			</div>
		</nav>
	);
}
