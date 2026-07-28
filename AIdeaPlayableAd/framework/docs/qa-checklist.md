# 可玩广告通用真机 QA 清单

用于任意 `ads/<name>/dist/playable.html` 的渠道与体验回归。玩法专项项可追加在各广告目录。

## 1. 设备矩阵

- Android 中端（建议 60Hz）：1 台
- iPhone 高 DPR：1 台
- 桌面 Chrome `file://` 预览：1 次（校验 CTA 兜底）

## 2. 壳层 / CTA

- [ ] 开局可交互（MRAID 预览或真机 viewable 后进入 PLAY）
- [ ] EndCard（complete / timeout）出现后棋盘操作锁定
- [ ] CTA 按钮触发商店 / SDK `install`（非 `file://` 坏链）
- [ ] 横竖屏切换后 UI 不崩、可继续或正确重建

## 3. 触控与视口

- [ ] 页面不可双指缩放 / 双击放大
- [ ] 拖动手势不带动整页滚动
- [ ] 安全区 / 刘海屏下 CTA 可点

## 4. 体积与加载

- [ ] 单文件体积在 `config.maxSizeBytes` 内（默认 5MB）
- [ ] 首屏资源加载无明显白屏超时（建议 < 3s 可点）

## 5. 结果模板

- 广告名 / 构建产物：
- 机型与系统：
- CTA 结论：
- 横竖屏结论：
- 性能结论：
- 是否通过：通过 / 不通过
- 备注：
