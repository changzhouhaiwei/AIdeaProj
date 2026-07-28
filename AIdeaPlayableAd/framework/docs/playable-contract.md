# Playable 游戏契约

游戏只负责玩法；渠道 CTA / MRAID / 打包由 `framework/` 处理。

## 最小 API

```js
// 资源与场景就绪（可开始交互）
Playable.ready();

// 达成结束条件（收集完成 / 超时等）
Playable.end('complete' | 'timeout');

// CTA 唯一出口（不要直接 window.open 商店链接）
Playable.openStore();
```

## 启动门闩

Phaser（或其它引擎）应在可播放时再创建：

```js
Playable.whenPlayable(function () {
  game = new Phaser.Game(config);
});
```

默认 `config.waitForViewable !== false` 时，会等 MRAID viewable（预览环境约 800ms 后 fail-open）。

## 配置

每个广告目录提供 `config.json`，打包时注入为 `window.__PLAYABLE_CONFIG__` / `Playable.config`。

常用字段：

| 字段 | 说明 |
|---|---|
| `iosStoreUrl` / `androidStoreUrl` | 商店链接 |
| `timeoutMs` | 超时弹窗毫秒（游戏可读） |
| `waitForViewable` | 是否等可视再 PLAY |
| `maxSizeBytes` | 体积预算（默认 5MB） |
| `endcard` | EndCard 元数据（文案 key、是否可关等） |

## 可选钩子

```js
Playable.onViewable = function () {};
Playable.onPause = function () {};
Playable.onResume = function () {};
Playable.onTrack = function (name, payload) {}; // analytics
Playable.audio.whenUnlocked(fn);               // 首触解锁音频
Playable.onEndCard('show'|'hide', fn);         // EndCard 契约监听
```

## 目录约定

```
ads/<name>/
  config.json
  game/main.js
  vendor/phaser.min.js   # 或其它引擎
  assets/
  dist/playable.html     # pack 产出
```

打包：

```bash
node framework/build/pack.mjs --ad ads/<name>
```
