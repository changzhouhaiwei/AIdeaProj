#!/usr/bin/env node
/**
 * Pack framework shell + runtime + game into a single playable HTML.
 *
 * Usage:
 *   node framework/build/pack.mjs --ad ads/tileAd1
 *   node framework/build/pack.mjs --ad ads/tileAd1 --out ads/tileAd1/dist/playable.html
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { spawnSync } from 'child_process';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const FRAMEWORK_ROOT = path.resolve(__dirname, '..');
const REPO_ROOT = path.resolve(FRAMEWORK_ROOT, '..');

function read(p) {
  return fs.readFileSync(p, 'utf8');
}

function scriptTag(code, marker) {
  return `<script>/*${marker}*/\n${code}\n</script>`;
}

function parseArgs(argv) {
  const out = { ad: null, out: null, skipSize: false, warnOnly: false };
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--ad') out.ad = argv[++i];
    else if (a === '--out') out.out = argv[++i];
    else if (a === '--skip-size') out.skipSize = true;
    else if (a === '--warn-only') out.warnOnly = true;
  }
  return out;
}

function resolveAdDir(adArg) {
  const p = path.isAbsolute(adArg) ? adArg : path.resolve(REPO_ROOT, adArg);
  if (!fs.existsSync(p)) throw new Error('Ad directory not found: ' + p);
  return p;
}

function loadConfig(adDir) {
  const cfgPath = path.join(adDir, 'config.json');
  if (!fs.existsSync(cfgPath)) throw new Error('Missing config.json in ' + adDir);
  return JSON.parse(read(cfgPath));
}

function main() {
  const opts = parseArgs(process.argv);
  if (!opts.ad) {
    console.error('Usage: node pack.mjs --ad ads/tileAd1 [--out path] [--skip-size] [--warn-only]');
    process.exit(2);
  }

  const adDir = resolveAdDir(opts.ad);
  const config = loadConfig(adDir);
  const title = config.title || 'Playable';
  const bg = (config.theme && config.theme.background) || '#1a1a2e';

  const shell = {
    platform: read(path.join(FRAMEWORK_ROOT, 'shell/platform-shims.js')),
    store: read(path.join(FRAMEWORK_ROOT, 'shell/store-redirect.js')),
    sdk: read(path.join(FRAMEWORK_ROOT, 'shell/sdk-bridge.js')),
    css: read(path.join(FRAMEWORK_ROOT, 'shell/touch-viewport.css')).replace(
      /var\(--playable-bg,\s*#[0-9a-fA-F]+\)/,
      bg
    ),
    gamestart: read(path.join(FRAMEWORK_ROOT, 'shell/gamestart-fallback.js')),
    openstore: read(path.join(FRAMEWORK_ROOT, 'shell/openstore-shim.js')),
    fetchEmbed: read(path.join(FRAMEWORK_ROOT, 'shell/fetch-embed.js'))
  };

  const runtimeFiles = [
    'playable-bootstrap.js',
    'analytics.js',
    'audio-gate.js',
    'cta.js',
    'endcard-api.js',
    'lifecycle.js'
  ];
  const runtime = runtimeFiles
    .map((f) => read(path.join(FRAMEWORK_ROOT, 'runtime', f)))
    .join('\n');

  const vendorPhaser = path.join(adDir, 'vendor/phaser.min.js');
  const gameMain = path.join(adDir, 'game/main.js');
  if (!fs.existsSync(vendorPhaser)) throw new Error('Missing ' + vendorPhaser);
  if (!fs.existsSync(gameMain)) throw new Error('Missing ' + gameMain);

  const phaser = read(vendorPhaser);
  const game = read(gameMain);

  const configLiteral = JSON.stringify(config, null, 2);

  const html = `<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<script type="text/javascript" src="mraid.js"></script>
${scriptTag(shell.platform, 'playable-platform-shims')}
<meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, minimum-scale=1.0, user-scalable=no">
<title>${title}</title>
<style>
${shell.css}
</style>
${scriptTag('window.__PLAYABLE_CONFIG__ = ' + configLiteral + ';', 'playable-config')}
${scriptTag(runtime, 'playable-runtime')}
${scriptTag(shell.store, 'playable-store-redirect')}
</head>
<body>
    <div id="game-container"></div>
${scriptTag(phaser, 'phaser')}
${scriptTag(game, 'game')}
${scriptTag(shell.sdk, 'playable-sdk-bridge')}
${scriptTag(shell.gamestart, 'playable-gamestart-fallback')}
${scriptTag(shell.fetchEmbed, 'playable-fetch-embed')}
${scriptTag(shell.openstore, 'playable-openstore-shim')}
</body>
</html>
`;

  const outPath = path.resolve(
    opts.out || path.join(adDir, 'dist', config.outputName || 'playable.html')
  );
  fs.mkdirSync(path.dirname(outPath), { recursive: true });
  fs.writeFileSync(outPath, html, 'utf8');
  console.log('Packed ->', outPath, '(' + fs.statSync(outPath).size + ' bytes)');

  if (!opts.skipSize) {
    const max = config.maxSizeBytes || 5 * 1024 * 1024;
    const checker = path.join(FRAMEWORK_ROOT, 'build/size-check.js');
    const args = [checker, outPath, '--max', String(max)];
    if (opts.warnOnly || config.sizeWarnOnly) args.push('--warn-only');
    const r = spawnSync(process.execPath, args, { stdio: 'inherit' });
    if (r.status && r.status !== 0) process.exit(r.status);
  }
}

main();
