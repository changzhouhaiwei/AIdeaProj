# tileAd1（方块连线可玩广告）

已接入 `framework/` 通用壳。开发改 `game/main.js` + `config.json`，发布跑打包。

```bash
# 从仓库根目录
npm run pack:tileAd1
# 产出：ads/tileAd1/dist/playable.html
```

- 契约说明：[`../../framework/docs/playable-contract.md`](../../framework/docs/playable-contract.md)
- 资源内嵌：`python ads/tileAd1/embed_resources.py`（写入 `game/main.js` 的 `BASE64_IMAGES`）
- 旧单体 `tile_playable_ad_version2.html` 仅作迁移对照；交付以 `dist/playable.html` 为准。
