export type TranslateStrategy = "auto" | "lingva" | "mymemory" | "gtx";

export type TranslateOptions = {
  enabled: boolean;
  myMemoryEmail?: string;
  /** 无尾部斜杠，如 https://lingva.ml */
  lingvaBaseUrl?: string;
  /** auto：依次尝试 Lingva → MyMemory → Google(gtx) */
  strategy?: TranslateStrategy;
};

const CHUNK_MAX = 320;

function timeoutSignal(ms: number): AbortSignal {
  const c = new AbortController();
  setTimeout(() => c.abort(), ms);
  return c.signal;
}

/** 多为中文、几乎无拉丁字母时不走英译中，避免乱译 */
export function shouldSkipEnToZh(text: string): boolean {
  const t = text.trim();
  if (t.length === 0) {
    return true;
  }
  let latin = 0;
  let cjk = 0;
  for (const ch of t) {
    if (/[a-zA-Z]/.test(ch)) {
      latin++;
    }
    if (/[\u4e00-\u9fff]/.test(ch)) {
      cjk++;
    }
  }
  if (latin < 2 && cjk >= 2) {
    return true;
  }
  if (latin === 0 && t.length > 0) {
    return true;
  }
  return false;
}

/** 在不超过 maxLen 的边界上尽量从空白或标点断开 */
function smartChunks(s: string, maxLen: number): string[] {
  const t = s.trim();
  if (!t) {
    return [];
  }
  if (t.length <= maxLen) {
    return [t];
  }
  const out: string[] = [];
  let rest = t;
  while (rest.length > 0) {
    if (rest.length <= maxLen) {
      out.push(rest);
      break;
    }
    let cut = rest.lastIndexOf(" ", maxLen);
    if (cut < Math.floor(maxLen * 0.45)) {
      cut = rest.lastIndexOf("\n", maxLen);
    }
    if (cut < Math.floor(maxLen * 0.45)) {
      cut = maxLen;
    }
    const piece = rest.slice(0, cut).trimEnd();
    if (piece.length > 0) {
      out.push(piece);
    }
    rest = rest.slice(cut).trimStart();
  }
  return out.length ? out : [t.slice(0, maxLen)];
}

function parseGtx(data: unknown): string {
  if (!Array.isArray(data) || data.length === 0) {
    return "";
  }
  const head = data[0];
  if (typeof head === "string") {
    return head;
  }
  if (!Array.isArray(head)) {
    return "";
  }
  const pieces: string[] = [];
  for (const seg of head) {
    if (Array.isArray(seg) && typeof seg[0] === "string" && seg[0].length > 0) {
      pieces.push(seg[0]);
    }
  }
  return pieces.join("");
}

function normalizeLingvaBase(url: string): string {
  return url.trim().replace(/\/+$/, "");
}

async function tryLingva(
  chunk: string,
  base: string,
  signal: AbortSignal
): Promise<string | undefined> {
  const b = normalizeLingvaBase(base);
  if (!b) {
    return undefined;
  }
  const path = `${b}/api/v1/en/zh/${encodeURIComponent(chunk)}`;
  const res = await fetch(path, { signal });
  if (!res.ok) {
    return undefined;
  }
  const j = (await res.json()) as { translation?: string };
  const t = j.translation?.trim();
  return t || undefined;
}

async function tryMyMemory(
  chunk: string,
  opts: TranslateOptions,
  signal: AbortSignal
): Promise<string | undefined> {
  let url = `https://api.mymemory.translated.net/get?q=${encodeURIComponent(chunk)}&langpair=en|zh-CN`;
  const de = opts.myMemoryEmail?.trim();
  if (de) {
    url += `&de=${encodeURIComponent(de)}`;
  }
  const res = await fetch(url, { signal });
  if (!res.ok) {
    return undefined;
  }
  const j = (await res.json()) as {
    responseData?: { translatedText?: string; match?: string | number };
    quotaFinished?: boolean;
  };
  if (j.quotaFinished) {
    return undefined;
  }
  const t = j.responseData?.translatedText?.trim();
  if (!t) {
    return undefined;
  }
  const m = j.responseData?.match;
  if (m !== undefined && m !== "") {
    const score = typeof m === "string" ? parseFloat(m) : Number(m);
    if (!Number.isNaN(score) && score < 0.28) {
      return undefined;
    }
  }
  if (t.toLowerCase() === chunk.trim().toLowerCase()) {
    return undefined;
  }
  return t;
}

async function tryGtx(chunk: string, signal: AbortSignal): Promise<string | undefined> {
  const url =
    "https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=zh-CN&dt=t&q=" +
    encodeURIComponent(chunk);
  const res = await fetch(url, { signal });
  if (!res.ok) {
    return undefined;
  }
  const j: unknown = await res.json();
  const t = parseGtx(j).trim();
  if (!t || t.toLowerCase() === chunk.trim().toLowerCase()) {
    return undefined;
  }
  return t;
}

type Provider = "lingva" | "mymemory" | "gtx";

function providersForStrategy(strategy: TranslateStrategy): Provider[] {
  if (strategy === "lingva") {
    return ["lingva"];
  }
  if (strategy === "mymemory") {
    return ["mymemory"];
  }
  if (strategy === "gtx") {
    return ["gtx"];
  }
  return ["lingva", "mymemory", "gtx"];
}

async function translateChunk(
  chunk: string,
  opts: TranslateOptions,
  signal: AbortSignal
): Promise<string | undefined> {
  const strategy = opts.strategy ?? "auto";
  const base = opts.lingvaBaseUrl ?? "https://lingva.ml";
  const chain = providersForStrategy(strategy);

  for (const p of chain) {
    let r: string | undefined;
    if (p === "lingva") {
      r = await tryLingva(chunk, base, signal);
    } else if (p === "mymemory") {
      r = await tryMyMemory(chunk, opts, signal);
    } else {
      r = await tryGtx(chunk, signal);
    }
    if (r) {
      return r;
    }
  }
  return undefined;
}

/** 英→简中；分块请求，多引擎回退；失败返回 undefined */
export async function translateEnToZh(
  text: string,
  opts: TranslateOptions
): Promise<string | undefined> {
  if (!opts.enabled) {
    return undefined;
  }
  const raw = text.trim();
  if (!raw) {
    return undefined;
  }
  if (shouldSkipEnToZh(raw)) {
    return undefined;
  }
  const normalized = raw.replace(/\s+/g, " ").trim();
  const chunks = smartChunks(normalized, CHUNK_MAX);
  if (!chunks.length) {
    return undefined;
  }

  const signal = timeoutSignal(45_000);
  const parts: string[] = [];

  try {
    for (const ch of chunks) {
      const piece = await translateChunk(ch, opts, signal);
      if (!piece) {
        return undefined;
      }
      parts.push(piece);
    }
  } catch {
    return undefined;
  }

  const joined = parts.join(" ").trim();
  return joined.trim() || undefined;
}
