# AIdeaWebSite2 · 每日热榜站点

与仓库内其他 Astro 示例无关；本目录为 **Vite + React + TypeScript + Tailwind CSS** 静态站，界面与数据层分离，便于日后用脚本或 CI 覆盖「每日榜单」数据。

## 一键本地部署（Windows）

- **双击 `AIdeaWebSite2/DeployLocal.exe`**（若目录里没有该文件，在 `AIdeaWebSite2` 下双击 **`BuildDeployLocalExe.cmd`** 用本机 .NET SDK 生成自包含 exe，体积约数十 MB，默认被 `.gitignore` 忽略）。
- **或双击 `DeployLocal.cmd` / `DeployLocal.vbs`**：不依赖 .NET，仅依赖 **Node.js（含 npm）**。
- 以上入口均会执行：`npm install` → `fetch:daily` → `build`，再启动预览并尝试打开浏览器。请保持 **`Site` 与这些文件同级**（均在 `AIdeaWebSite2` 根目录）。

说明：`DeployLocal.exe` 只是启动器，**构建与预览仍依赖本机已安装的 Node.js**（`node` / `npm` 在 PATH）。

## 本地开发

```bash
cd AIdeaWebSite2/Site
npm install
npm run dev
```

## 生产构建

```bash
npm run build
```

产物在 `dist/`。若站点部署在 GitHub Pages 的**项目页**（非 `username.github.io` 根域），构建时需带子路径前缀，例如：

```bash
set VITE_BASE=/你的仓库名/
npm run build
```

Linux/macOS：

```bash
VITE_BASE=/你的仓库名/ npm run build
```

仓库根目录下的 GitHub Actions 工作流会在构建时自动设置 `VITE_BASE`。

## 数据源（已实现管线）

整体思路：**在 Node 里拉 RSS（无浏览器 CORS）→ 写入 `public/daily.json` → 构建复制到 `dist` → 前端 `fetch` 同域读取**；请求失败或 JSON 无效时使用 `src/data/daily.ts` 内置占位。栏目与条数与仓库 **`1.md`** 一致（NBA / Play / 国际形势 均为「国内 10 + 国外 10」等）。

1. **本地 / CI 拉取**（开源依赖 [rss-parser](https://www.npmjs.com/package/rss-parser)）  
   ```bash
   npm run fetch:daily
   ```  
   源地址在 `scripts/fetch-daily.mjs` 的 `SOURCES` 中按**数组顺序**配置：脚本会依次请求，**去重后凑满条数**，避免单点（如 Google、BBC）超时导致整栏为空。

2. **一次拉取并构建**  
   ```bash
   npm run build:online
   ```

3. **合规与稳定性**  
   控制频率、遵守 robots 与各站 ToS。脚本在某一源失败时会尝试沿用**上一份** `public/daily.json` 中对应栏目，减少 CI 因单点封禁整站变空。

GitHub Actions 已在 `npm run build` 前执行 `npm run fetch:daily`。

## 开源栈

- [Vite](https://vitejs.dev/)（MIT）
- [React](https://react.dev/)（MIT）
- [TypeScript](https://www.typescriptlang.org/)（Apache-2.0）
- [Tailwind CSS](https://tailwindcss.com/)（MIT）
- [rss-parser](https://www.npmjs.com/package/rss-parser)（MIT，仅用于 `npm run fetch:daily`）
