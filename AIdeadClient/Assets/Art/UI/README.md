# Art/UI — Cursor 本地游戏 UI 闭环

目录约定与验收说明。Agent 操作规范见 `.cursor/skills/game-ui-pipeline/SKILL.md`。

## 目录

```text
Assets/Art/UI/<ScreenId>/
  ui_structure.json   # 真源：层级 / 类型 / 文案 / 区域 hint
  mockup.png          # 整屏效果图（风格参考）
  layout.json         # 像素布局 + sprite 映射（拼 Prefab 真源）
  Parts/              # 透明底切图 PNG
  Prefabs/UI_<ScreenId>.prefab
```

契约说明：

- [SCHEMA_structure.md](SCHEMA_structure.md)
- [SCHEMA_layout.md](SCHEMA_layout.md)

样例屏：`MainMenu/`、`Settings/`（样式参考 → 结构解读 → 徽章风重绘）

## Unity 拼装

1. 确认该屏已有 `layout.json` 与 `Parts/*.png`
2. 菜单 **Art/UI/Build Prefab From Layout**
3. 选择该屏的 `layout.json`（或先在 Project 里选中该文件再点菜单）
4. Prefab 输出到同级 `Prefabs/UI_<ScreenId>.prefab`
5. 拖入场景 Canvas 下（或打开 Prefab 自带 Canvas）预览

## 本地裁切占位

```bash
python Tools/UI/crop_from_layout.py Assets/Art/UI/MainMenu/layout.json
```

默认从同目录 `mockup.png` 按 rect 裁到 `Parts/`（可加 `--chroma` 去浅色底）。

## 主菜单验收清单

样例屏详情见 [MainMenu/SMOKE.md](MainMenu/SMOKE.md)。

- [x] 存在 `mockup.png`（徽章风整屏）
- [x] `layout.json` 节点与 `ui_structure.json` id 对齐
- [x] `Parts/` 至少含底板 + 主按钮等可渲染图素
- [ ] 菜单生成 Prefab 后 Hierarchy 与 structure 一致（Unity 内点 **Art/UI/Build Prefab From Layout**）
- [ ] Play 模式可见按钮与文案（业务绑定另做）
