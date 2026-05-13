# AIdea 博客（Astro Blog 模板）

本目录对应 `1.md` 中的目标：Markdown/MDX 写文、静态站点、可发布到 GitHub Pages。

## 一键部署到 GitHub（推荐：本文件夹单独建仓库）

1. 在 GitHub 新建仓库，把 **`AIdeaWebSite` 作为仓库根目录** 推送（这样 `.github/workflows` 路径才正确）。
2. 仓库 **Settings → Pages**：**Build and deployment** 的 Source 选 **GitHub Actions**（不要选 Deploy from a branch）。
3. 推送到默认分支 **`main` 或 `master`**，工作流会自动构建并发布。
4. 工作流会根据仓库名自动设置子路径：若仓库名为 `用户名.github.io`，站点在根路径；否则为 `https://用户名.github.io/仓库名/`。

本地开发：`npm install` → `npm run dev`（默认 <http://localhost:4321>）。

### 在网站里新建/编辑博客（Decap CMS）

静态站本身没有数据库；已接入开源 **[Decap CMS](https://decapcms.org/)**，在浏览器里编辑 `src/content/blog` 下的 Markdown。

1. **必须在 Git 仓库里使用**（本地代理通过 Git 写文件）。若 `AIdeaWebSite` 单独成仓：在此目录执行 `git init`。若 Git 根目录是上一级 `AIdeaProj`：请把 `public/admin/config.yml` 里的 `folder` 改为 `AIdeaWebSite/src/content/blog`，`media_folder` 改为 `AIdeaWebSite/src/assets`（路径相对 **Git 仓库根**）。
2. 终端一（在 `AIdeaWebSite` 下）：`npm run dev`
3. 终端二（仍在 `AIdeaWebSite` 下）：`npm run cms`
4. 浏览器打开：<http://localhost:4321/admin/>，用「New 博客文章」新建，保存后会生成/更新 `src/content/blog/*.md`。

页脚有「博客后台」链接。线上 GitHub 编辑需配置 OAuth 并关闭 `local_backend`，见 `public/admin/config.yml` 顶部注释与 [Decap + GitHub](https://decapcms.org/docs/github-backend/)。

---

以下为 Astro 官方模板说明（英文）。

# Astro Starter Kit: Blog

```sh
npm create astro@latest -- --template blog
```

> 🧑‍🚀 **Seasoned astronaut?** Delete this file. Have fun!

Features:

- ✅ Minimal styling (make it your own!)
- ✅ 100/100 Lighthouse performance
- ✅ SEO-friendly with canonical URLs and Open Graph data
- ✅ Sitemap support
- ✅ RSS Feed support
- ✅ Markdown & MDX support

## 🚀 Project Structure

Inside of your Astro project, you'll see the following folders and files:

```text
├── public/
├── src/
│   ├── assets/
│   ├── components/
│   ├── content/
│   ├── layouts/
│   └── pages/
├── astro.config.mjs
├── README.md
├── package.json
└── tsconfig.json
```

Astro looks for `.astro` or `.md` files in the `src/pages/` directory. Each page is exposed as a route based on its file name.

There's nothing special about `src/components/`, but that's where we like to put any Astro/React/Vue/Svelte/Preact components.

The `src/content/` directory contains "collections" of related Markdown and MDX documents. Use `getCollection()` to retrieve posts from `src/content/blog/`, and type-check your frontmatter using an optional schema. See [Astro's Content Collections docs](https://docs.astro.build/en/guides/content-collections/) to learn more.

Any static assets, like images, can be placed in the `public/` directory.

## 🧞 Commands

All commands are run from the root of the project, from a terminal:

| Command                   | Action                                           |
| :------------------------ | :----------------------------------------------- |
| `npm install`             | Installs dependencies                            |
| `npm run dev`             | Starts local dev server at `localhost:4321`      |
| `npm run build`           | Build your production site to `./dist/`          |
| `npm run preview`         | Preview your build locally, before deploying     |
| `npm run astro ...`       | Run CLI commands like `astro add`, `astro check` |
| `npm run astro -- --help` | Get help using the Astro CLI                     |

## 👀 Want to learn more?

Check out [our documentation](https://docs.astro.build) or jump into our [Discord server](https://astro.build/chat).

## Credit

This theme is based off of the lovely [Bear Blog](https://github.com/HermanMartinus/bearblog/).
