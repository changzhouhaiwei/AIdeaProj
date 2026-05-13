import * as fs from "fs/promises";
import * as path from "path";
import * as vscode from "vscode";
import {
  refreshBrowsePanelIfOpen,
  showOrRevealBrowsePanel,
} from "./browsePanel";
import { registerVocabularySidebar } from "./sidebarTree";
import {
  translateEnToZh,
  shouldSkipEnToZh,
  type TranslateStrategy,
} from "./translate";
import type { VocabularyEntry, VocabularyFile } from "./types";

const MAX_TEXT_LEN = 2000;

function normalizeKey(text: string): string {
  return text.trim().toLowerCase();
}

function workspaceFolderForDocument(
  documentUri: vscode.Uri | undefined
): vscode.WorkspaceFolder | undefined {
  if (documentUri) {
    const w = vscode.workspace.getWorkspaceFolder(documentUri);
    if (w) {
      return w;
    }
  }
  return vscode.workspace.workspaceFolders?.[0];
}

/** 默认：全局目录；可选工作区或自定义路径。 */
function resolveVocabularyUri(
  context: vscode.ExtensionContext,
  documentUri: vscode.Uri | undefined
): vscode.Uri | undefined {
  const cfg = vscode.workspace.getConfiguration("englishComponent");
  const configured = (cfg.get<string>("vocabularyFile") ?? "").trim();
  const useWorkspace = cfg.get<boolean>("useWorkspaceStorage") === true;
  const folder = workspaceFolderForDocument(documentUri);

  if (configured) {
    if (path.isAbsolute(configured)) {
      return vscode.Uri.file(configured);
    }
    if (folder) {
      return vscode.Uri.joinPath(folder.uri, configured);
    }
    vscode.window.showWarningMessage(
      "已设置相对路径 vocabularyFile，但未打开工作区文件夹，无法解析路径。"
    );
    return undefined;
  }

  if (useWorkspace) {
    if (!folder) {
      vscode.window.showWarningMessage(
        "已启用「按工作区保存」，但未打开工作区文件夹。"
      );
      return undefined;
    }
    return vscode.Uri.joinPath(
      folder.uri,
      "EnglishComponent",
      "vocabulary.json"
    );
  }

  return vscode.Uri.joinPath(context.globalStorageUri, "vocabulary.json");
}

async function readVocabulary(uri: vscode.Uri): Promise<VocabularyFile> {
  try {
    const raw = await fs.readFile(uri.fsPath, "utf8");
    const parsed = JSON.parse(raw) as Partial<VocabularyFile>;
    if (
      parsed &&
      parsed.version === 1 &&
      Array.isArray(parsed.entries)
    ) {
      return { version: 1, entries: parsed.entries };
    }
  } catch (e: unknown) {
    const code = (e as NodeJS.ErrnoException)?.code;
    if (code !== "ENOENT") {
      throw e;
    }
  }
  return { version: 1, entries: [] };
}

async function writeVocabulary(
  uri: vscode.Uri,
  data: VocabularyFile
): Promise<void> {
  await fs.mkdir(path.dirname(uri.fsPath), { recursive: true });
  await fs.writeFile(uri.fsPath, JSON.stringify(data, null, 2), "utf8");
}

function resolveDocUriForVocab(): vscode.Uri | undefined {
  return (
    vscode.window.activeTextEditor?.document.uri ??
    vscode.workspace.workspaceFolders?.[0]?.uri
  );
}

function makeLoadEntries(
  context: vscode.ExtensionContext,
  documentUri: vscode.Uri | undefined
): () => Promise<VocabularyEntry[]> {
  return async () => {
    const uri = resolveVocabularyUri(context, documentUri);
    if (!uri) {
      return [];
    }
    try {
      const d = await readVocabulary(uri);
      return d.entries;
    } catch {
      return [];
    }
  };
}

async function saveWordEntry(
  context: vscode.ExtensionContext,
  documentUri: vscode.Uri | undefined,
  selected: string,
  source: string | undefined
): Promise<void> {
  if (!selected) {
    vscode.window.showInformationMessage("没有可保存的文本。");
    return;
  }
  if (selected.length > MAX_TEXT_LEN) {
    vscode.window.showWarningMessage(
      `文本过长（超过 ${MAX_TEXT_LEN} 字符），请缩短后再试。`
    );
    return;
  }

  const vocabUri = resolveVocabularyUri(context, documentUri);
  if (!vocabUri) {
    return;
  }

  let data: VocabularyFile;
  try {
    data = await readVocabulary(vocabUri);
  } catch (e) {
    vscode.window.showErrorMessage(
      `读取词汇文件失败：${e instanceof Error ? e.message : String(e)}`
    );
    return;
  }

  const key = normalizeKey(selected);
  if (data.entries.some((e) => normalizeKey(e.text) === key)) {
    vscode.window.showInformationMessage(
      `已在词汇表中：「${selected.slice(0, 80)}${selected.length > 80 ? "…" : ""}」`
    );
    return;
  }

  const cfg = vscode.workspace.getConfiguration("englishComponent");
  const translateOn = cfg.get<boolean>("translateOnSave") !== false;
  const email = (cfg.get<string>("myMemoryContactEmail") ?? "").trim();
  let translationZh: string | undefined;
  if (translateOn && !shouldSkipEnToZh(selected)) {
    const lingvaUrl = (cfg.get<string>("lingvaUrl") ?? "https://lingva.ml").trim();
    const strategy = (cfg.get<string>("translateStrategy") ??
      "auto") as TranslateStrategy;
    await vscode.window.withProgress(
      {
        location: vscode.ProgressLocation.Notification,
        title: "正在翻译…",
        cancellable: false,
      },
      async () => {
        translationZh = await translateEnToZh(selected, {
          enabled: true,
          myMemoryEmail: email || undefined,
          lingvaBaseUrl: lingvaUrl || "https://lingva.ml",
          strategy,
        });
      }
    );
    if (!translationZh) {
      vscode.window.showWarningMessage(
        "自动翻译未返回结果，仍将保存英文（可稍后在浏览窗口查看或更换翻译策略）。"
      );
    }
  } else if (translateOn && shouldSkipEnToZh(selected)) {
    /* 多为中文，不调用在线翻译 */
  }

  const entry: VocabularyEntry = {
    text: selected,
    translationZh,
    savedAt: new Date().toISOString(),
    source,
  };
  data.entries.push(entry);

  try {
    await writeVocabulary(vocabUri, data);
  } catch (e) {
    vscode.window.showErrorMessage(
      `写入词汇文件失败：${e instanceof Error ? e.message : String(e)}`
    );
    return;
  }

  const useWorkspace = cfg.get<boolean>("useWorkspaceStorage") === true;
  const custom = (cfg.get<string>("vocabularyFile") ?? "").trim();
  const count = data.entries.length;
  const zhHint = translationZh ? ` 译文：${translationZh}` : "";
  const msg =
    custom || useWorkspace
      ? `已保存（共 ${count} 条）${zhHint}\n${vocabUri.fsPath}`
      : `已保存到全局词汇表（共 ${count} 条）${zhHint}\n${vocabUri.fsPath}`;
  vscode.window.showInformationMessage(msg);

  await refreshBrowsePanelIfOpen(makeLoadEntries(context, documentUri));
}

async function deleteWordEntry(
  context: vscode.ExtensionContext,
  documentUri: vscode.Uri | undefined,
  text: string,
  savedAt: string
): Promise<{ ok: boolean; error?: string }> {
  const preview =
    text.length > 72 ? `${text.slice(0, 72)}…` : text;
  const pick = await vscode.window.showWarningMessage(
    `确定删除这条词汇？\n\n${preview}`,
    { modal: true },
    "删除",
    "取消"
  );
  if (pick !== "删除") {
    return { ok: false };
  }

  const vocabUri = resolveVocabularyUri(context, documentUri);
  if (!vocabUri) {
    return { ok: false, error: "无法定位词汇文件" };
  }
  try {
    const data = await readVocabulary(vocabUri);
    const idx = data.entries.findIndex(
      (e) => e.text === text && (e.savedAt ?? "") === savedAt
    );
    if (idx === -1) {
      vscode.window.showWarningMessage("列表中已没有这条记录（可能已被删除）。");
      return { ok: false, error: "not found" };
    }
    data.entries.splice(idx, 1);
    await writeVocabulary(vocabUri, data);
    vscode.window.showInformationMessage("已删除该词条。");
    return { ok: true };
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    vscode.window.showErrorMessage(`删除失败：${msg}`);
    return { ok: false, error: msg };
  }
}

export function activate(context: vscode.ExtensionContext): void {
  const save = vscode.commands.registerCommand(
    "englishComponent.saveSelection",
    async () => {
      const editor = vscode.window.activeTextEditor;
      if (!editor) {
        vscode.window.showInformationMessage("没有活动的编辑器。");
        return;
      }

      const selected = editor.document.getText(editor.selection).trim();
      if (!selected) {
        vscode.window.showInformationMessage("请先选中要保存的英文或短语。");
        return;
      }

      const source =
        editor.document.uri.scheme === "file"
          ? editor.document.uri.fsPath
          : editor.document.uri.toString();

      await saveWordEntry(context, editor.document.uri, selected, source);
    }
  );

  const saveClip = vscode.commands.registerCommand(
    "englishComponent.saveFromClipboard",
    async () => {
      const text = (await vscode.env.clipboard.readText()).trim();
      if (!text) {
        vscode.window.showInformationMessage("剪贴板为空，请先在网页或其它处复制英文。");
        return;
      }
      const docUri = resolveDocUriForVocab();
      const src = "clipboard";
      await saveWordEntry(context, docUri, text, src);
    }
  );

  const openVocab = vscode.commands.registerCommand(
    "englishComponent.openVocabulary",
    async () => {
      const editor = vscode.window.activeTextEditor;
      const vocabUri = resolveVocabularyUri(
        context,
        editor?.document.uri ?? resolveDocUriForVocab()
      );
      if (!vocabUri) {
        return;
      }
      try {
        const doc = await vscode.workspace.openTextDocument(vocabUri);
        await vscode.window.showTextDocument(doc);
      } catch (e) {
        vscode.window.showErrorMessage(
          `无法打开词汇文件：${e instanceof Error ? e.message : String(e)}`
        );
      }
    }
  );

  const browse = vscode.commands.registerCommand(
    "englishComponent.browseVocabulary",
    () => {
      const docUri = resolveDocUriForVocab();
      showOrRevealBrowsePanel(
        makeLoadEntries(context, docUri),
        (text, savedAt) => deleteWordEntry(context, docUri, text, savedAt)
      );
    }
  );

  registerVocabularySidebar(context);
  context.subscriptions.push(save, saveClip, openVocab, browse);
}

export function deactivate(): void {}
