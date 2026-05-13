export type FeedItem = {
	title: string;
	url: string;
	source?: string;
};

export type DailyFeeds = {
	nba: { cn: FeedItem[]; intl: FeedItem[] };
	stocks: { cn: FeedItem[]; intl: FeedItem[] };
	ai: { cn: FeedItem[]; intl: FeedItem[] };
	play: { cn: FeedItem[]; intl: FeedItem[] };
	world: { cn: FeedItem[]; intl: FeedItem[] };
};

export type DailyPayload = {
	dailyDate: string;
	feeds: DailyFeeds;
};
