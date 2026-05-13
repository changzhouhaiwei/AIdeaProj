import * as vscode from "vscode";

const VIEW_ID = "englishComponent.sidebar";

function item(
  label: string,
  command: string,
  iconId: string
): vscode.TreeItem {
  const t = new vscode.TreeItem(label, vscode.TreeItemCollapsibleState.None);
  t.command = { command, title: label };
  t.iconPath = new vscode.ThemeIcon(iconId);
  return t;
}

class VocabSidebarProvider implements vscode.TreeDataProvider<vscode.TreeItem> {
  getTreeItem(e: vscode.TreeItem): vscode.TreeItem {
    return e;
  }

  getChildren(): vscode.ProviderResult<vscode.TreeItem[]> {
    return [
      item("浏览词汇（表格）", "englishComponent.browseVocabulary", "table"),
      item("打开词汇 JSON", "englishComponent.openVocabulary", "json"),
      item("从剪贴板保存", "englishComponent.saveFromClipboard", "clipboard"),
    ];
  }
}

export function registerVocabularySidebar(
  context: vscode.ExtensionContext
): vscode.Disposable {
  const provider = new VocabSidebarProvider();
  const tree = vscode.window.registerTreeDataProvider(VIEW_ID, provider);
  context.subscriptions.push(tree);
  return tree;
}
