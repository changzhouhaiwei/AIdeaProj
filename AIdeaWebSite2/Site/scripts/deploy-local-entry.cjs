'use strict';

/**
 * 本地一键：安装依赖 → 拉取 RSS → 构建 → 后台预览并打开浏览器。
 * - 用 Node 直接运行：位于 Site/scripts，工作目录会切到 Site。
 * - 用 pkg 打成 DeployLocal.exe：请把 exe 放在与 Site 文件夹同级（即 AIdeaWebSite2 目录），仍依赖本机已安装的 npm（PATH）。
 */
const { spawnSync, spawn } = require('node:child_process');
const path = require('node:path');
const fs = require('node:fs');

function resolveSiteRoot() {
	if (process.pkg) {
		return path.join(path.dirname(process.execPath), 'Site');
	}
	return path.join(__dirname, '..');
}

const siteRoot = resolveSiteRoot();
const pkgJson = path.join(siteRoot, 'package.json');

if (!fs.existsSync(pkgJson)) {
	// eslint-disable-next-line no-console
	console.error(
		'[DeployLocal] 未找到 Site\\package.json。\n' +
			'请将 DeployLocal.exe 放在与「Site」文件夹同级的目录（例如 …\\AIdeaWebSite2\\DeployLocal.exe）。',
	);
	process.exit(1);
}

process.chdir(siteRoot);

function run(label, command, args) {
	// eslint-disable-next-line no-console
	console.log(`[DeployLocal] ${label}…`);
	const r = spawnSync(command, args, {
		stdio: 'inherit',
		shell: true,
		cwd: siteRoot,
		env: { ...process.env, FORCE_COLOR: '0' },
	});
	const code = r.status === null ? 1 : r.status;
	if (code !== 0) {
		// eslint-disable-next-line no-console
		console.error(`[DeployLocal] 步骤失败（退出码 ${code}）：${label}`);
		process.exit(code);
	}
}

function ensureNpmOnPath() {
	const which = spawnSync('where', ['npm'], { shell: true, encoding: 'utf8' });
	if (which.status !== 0 || !which.stdout?.trim()) {
		// eslint-disable-next-line no-console
		console.error('[DeployLocal] 未检测到 npm。请先安装 Node.js LTS，并确保「npm」在系统 PATH 中。');
		process.exit(1);
	}
}

ensureNpmOnPath();

run('安装依赖', 'npm', ['install']);
run('拉取 RSS', 'npm', ['run', 'fetch:daily']);
run('构建静态站', 'npm', ['run', 'build']);

const port = process.env.DEPLOY_LOCAL_PORT || '4173';
const url = `http://127.0.0.1:${port}/`;

// eslint-disable-next-line no-console
console.log(`[DeployLocal] 启动预览 ${url}（关闭该预览窗口即停止服务）…`);

const child = spawn('npm', ['run', 'preview', '--', '--host', '127.0.0.1', '--port', port], {
	detached: true,
	stdio: 'ignore',
	shell: true,
	cwd: siteRoot,
});
child.unref();

setTimeout(() => {
	spawnSync('cmd', ['/c', 'start', '', url], { stdio: 'ignore', shell: true });
}, 2200);
