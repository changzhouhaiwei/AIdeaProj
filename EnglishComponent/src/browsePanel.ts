import * as vscode from "vscode";
import type { VocabularyEntry } from "./types";

const viewType = "englishComponent.browse";

let panel: vscode.WebviewPanel | undefined;

export type BrowseLoadEntries = () => Promise<VocabularyEntry[]>;
export type BrowseDeleteEntry = (
  text: string,
  savedAt: string
) => Promise<{ ok: boolean; error?: string }>;

let deleteEntryRef: BrowseDeleteEntry | undefined;

function getNonce(): string {
  let t = "";
  const c = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  for (let i = 0; i < 32; i++) {
    t += c.charAt(Math.floor(Math.random() * c.length));
  }
  return t;
}

function buildHtml(webview: vscode.Webview, nonce: string): string {
  const csp = [
    `default-src 'none'`,
    `style-src ${webview.cspSource} 'unsafe-inline'`,
    `script-src 'nonce-${nonce}'`,
  ].join("; ");

  return `<!DOCTYPE html>
<html lang="zh-CN">
<head>
  <meta charset="UTF-8" />
  <meta http-equiv="Content-Security-Policy" content="${csp}">
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>词汇浏览</title>
  <style>
    body {
      margin: 0;
      padding: 12px 16px 24px;
      font-family: var(--vscode-font-family);
      font-size: var(--vscode-font-size);
      color: var(--vscode-foreground);
      background: var(--vscode-editor-background);
    }
    h1 { font-size: 1.1rem; font-weight: 600; margin: 0 0 12px; }
    .toolbar { display: flex; gap: 8px; align-items: center; margin-bottom: 12px; flex-wrap: wrap; }
    button {
      background: var(--vscode-button-background);
      color: var(--vscode-button-foreground);
      border: none;
      padding: 6px 12px;
      cursor: pointer;
      border-radius: 2px;
      font-family: inherit;
      font-size: inherit;
    }
    button:hover { background: var(--vscode-button-hoverBackground); }
    .table-wrap {
      overflow-x: auto;
      width: 100%;
      max-width: 100%;
      -webkit-overflow-scrolling: touch;
    }
    table {
      width: 100%;
      min-width: 560px;
      border-collapse: collapse;
      table-layout: auto;
    }
    th, td {
      text-align: left;
      padding: 8px 10px;
      border-bottom: 1px solid var(--vscode-panel-border);
      vertical-align: middle;
    }
    th { color: var(--vscode-descriptionForeground); font-weight: 600; }
    th.col-op { width: 6.5rem; min-width: 6.5rem; text-align: center; }
    td.col-op {
      text-align: center;
      position: sticky;
      right: 0;
      z-index: 4;
      background: var(--vscode-editor-background);
      box-shadow: -6px 0 10px rgba(0,0,0,0.12);
    }
    th.col-op {
      position: sticky;
      right: 0;
      z-index: 5;
      background: var(--vscode-editor-background);
    }
    tr:hover td:not(.col-op) { background: var(--vscode-list-hoverBackground); }
    tr:hover td.col-op {
      background: var(--vscode-list-hoverBackground);
      z-index: 6;
    }
    button.btn-del {
      background: var(--vscode-inputValidation-errorBackground);
      color: var(--vscode-errorForeground);
      min-width: 4.5rem;
      min-height: 32px;
      padding: 8px 14px;
      font-size: 0.9rem;
      line-height: 1.2;
      cursor: pointer;
      pointer-events: auto;
      position: relative;
      z-index: 8;
      -webkit-user-select: none;
      user-select: none;
    }
    button.btn-del:hover {
      filter: brightness(1.08);
    }
    button.btn-del:active {
      transform: scale(0.98);
    }
    .hint { opacity: 0.85; font-size: 0.9em; }
    .en { white-space: pre-wrap; word-break: break-word; }
    .zh { color: var(--vscode-textLink-foreground); white-space: pre-wrap; word-break: break-word; }
    .meta { font-size: 0.85em; color: var(--vscode-descriptionForeground); }
    .empty { padding: 24px; text-align: center; opacity: 0.8; }
  </style>
</head>
<body>
  <h1>不懂的英文 · 浏览</h1>
  <div class="toolbar">
    <button type="button" id="btn-refresh">刷新</button>
    <span class="hint" id="count"></span>
  </div>
  <div id="root"></div>
  <script nonce="${nonce}">
    const vscode = acquireVsCodeApi();

    function render(entries) {
      const root = document.getElementById('root');
      const count = document.getElementById('count');
      if (!root || !count) return;
      count.textContent = entries.length ? '共 ' + entries.length + ' 条' : '暂无条目';
      root.textContent = '';

      if (!entries.length) {
        const empty = document.createElement('div');
        empty.className = 'empty';
        empty.textContent = '还没有保存过词汇。在编辑器中选中英文右键「保存不懂的英文」，或使用命令「从剪贴板保存不懂的英文」。';
        root.appendChild(empty);
        return;
      }

      const wrap = document.createElement('div');
      wrap.className = 'table-wrap';

      const table = document.createElement('table');
      const thead = document.createElement('thead');
      const hr = document.createElement('tr');
      ['英文', '中文', '保存时间 / 来源', '操作'].forEach((label, i) => {
        const th = document.createElement('th');
        th.textContent = label;
        if (i === 3) th.className = 'col-op';
        hr.appendChild(th);
      });
      thead.appendChild(hr);
      table.appendChild(thead);

      const tbody = document.createElement('tbody');
      const ordered = entries.slice().reverse();
      for (const e of ordered) {
        const tr = document.createElement('tr');

        const tdEn = document.createElement('td');
        tdEn.className = 'en';
        tdEn.textContent = e.text || '';
        tr.appendChild(tdEn);

        const tdZh = document.createElement('td');
        tdZh.className = 'zh';
        tdZh.textContent = (e.translationZh && e.translationZh.trim()) ? e.translationZh : '—';
        tr.appendChild(tdZh);

        const tdMeta = document.createElement('td');
        tdMeta.className = 'meta';
        tdMeta.appendChild(document.createTextNode(e.savedAt || ''));
        if (e.source) {
          tdMeta.appendChild(document.createElement('br'));
          const src = document.createElement('span');
          src.textContent = e.source;
          tdMeta.appendChild(src);
        }
        tr.appendChild(tdMeta);

        const tdOp = document.createElement('td');
        tdOp.className = 'col-op';
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn-del';
        btn.textContent = '删除';
        btn.addEventListener('click', (ev) => {
          ev.preventDefault();
          ev.stopPropagation();
          vscode.postMessage({
            type: 'delete',
            text: e.text,
            savedAt: e.savedAt ?? ''
          });
        });
        tdOp.appendChild(btn);
        tr.appendChild(tdOp);

        tbody.appendChild(tr);
      }
      table.appendChild(tbody);
      wrap.appendChild(table);
      root.appendChild(wrap);
    }

    window.addEventListener('message', (event) => {
      const m = event.data;
      if (m && m.type === 'data' && Array.isArray(m.entries)) {
        render(m.entries);
      }
    });

    document.getElementById('btn-refresh').addEventListener('click', () => {
      vscode.postMessage({ type: 'refresh' });
    });
  </script>
</body>
</html>`;
}

export function showOrRevealBrowsePanel(
  loadEntries: BrowseLoadEntries,
  deleteEntry: BrowseDeleteEntry
): void {
  deleteEntryRef = deleteEntry;

  const send = async (p: vscode.WebviewPanel) => {
    const entries = await loadEntries();
    p.webview.postMessage({ type: "data", entries });
  };

  if (panel) {
    panel.reveal(undefined, true);
    void send(panel);
    return;
  }

  const nonce = getNonce();
  panel = vscode.window.createWebviewPanel(
    viewType,
    "不懂的英文 · 浏览",
    vscode.ViewColumn.One,
    {
      enableScripts: true,
      retainContextWhenHidden: true,
    }
  );

  panel.webview.html = buildHtml(panel.webview, nonce);

  panel.webview.onDidReceiveMessage(
    async (msg: {
      type?: string;
      text?: string;
      savedAt?: string;
    }) => {
      if (!panel) {
        return;
      }
      if (msg?.type === "refresh") {
        await send(panel);
        return;
      }
      if (
        msg?.type === "delete" &&
        typeof msg.text === "string" &&
        typeof msg.savedAt === "string"
      ) {
        const del = deleteEntryRef;
        if (del) {
          await del(msg.text, msg.savedAt);
        }
        await send(panel);
      }
    }
  );

  panel.onDidDispose(() => {
    panel = undefined;
  });

  void send(panel);
}

export async function refreshBrowsePanelIfOpen(
  loadEntries: BrowseLoadEntries
): Promise<void> {
  if (!panel) {
    return;
  }
  const entries = await loadEntries();
  panel.webview.postMessage({ type: "data", entries });
}
