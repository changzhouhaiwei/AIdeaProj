---
name: game-ui-pipeline
description: >-
  Local Cursor pipeline for AIdeadClient game UI: structure → mockup →
  layout.json + Parts PNGs → Unity uGUI Prefab. Use when the user asks for
  游戏UI、主菜单、HUD、切图拼UI、ui_structure、layout.json、Build Prefab From Layout,
  or potato-badge UI chrome generation.
---

# 游戏 UI 本地闭环（structure → Prefab）

## 何时使用

用户给出一套 UI 结构、要出效果图、识别图素并拼成 Unity UI 时，**先读本 Skill**，再读 `potato-badge-art`（画风锁）。

契约与目录：`Assets/Art/UI/README.md`、`SCHEMA_structure.md`、`SCHEMA_layout.md`。

## 目录

```text
Assets/Art/UI/<ScreenId>/
  ui_structure.json
  mockup.png
  layout.json
  Parts/*.png
  Prefabs/UI_<ScreenId>.prefab
```

## 五步流程（按屏执行）

### 1. 结构入库

- 用户贴 YAML/JSON，或直接改 `ui_structure.json`
- 保证每个可交互/可渲染节点有稳定 `id`、`type`、`parent`、可选 `hint` / `spriteHint`
- `screenId` 与文件夹名一致

### 2. 出整屏效果图

用 `GenerateImage` 写到 `Assets/Art/UI/<ScreenId>/mockup.png`。

参考图（若可用）：

- `reference_image_paths`: `Assets/Art/Role/role_potato.png`

提示词骨架：

```text
Mobile game main menu mockup, portrait 9:16, centered card panel, potato badge mascot logo on top,
three stacked rounded buttons with empty label areas (no readable text glyphs),
flat badge emblem UI chrome, thick uniform pure-black outlines, flat limited 3-5 colors,
cream or soft farm field background behind the card, clean silhouette, no neon, no purple cyber,
no photorealism, no anime thick shading.
Style lock (mandatory): flat badge emblem / logo glyph art like Brotato potato icons,
thick uniform pure-black outlines (single outline only, no double stroke, no cream halo),
flat limited 3-5 color palette, tiny grain texture optional,
blocky geometric UI panels and buttons readable at small size.
```

注意：整屏可带装饰，**尽量少生成可读文字**（文案由 TMP 负责）。

### 3. 写 layout.json

- 以 `ui_structure.json` 为真源复制节点 id/父子
- `Read` `mockup.png` 校对各控件大致位置，填 `rect`（左上原点）
- 字段必须符合 `SCHEMA_layout.md`（`JsonUtility` 友好：平面 `anchorMinX` 等，勿用嵌套数组）
- 为每个有图节点填 `sprite`
- **九宫格默认关闭**：一律 `useNineSlice: 0`；**禁止擅自开启**。仅当用户明确要求「九宫格 / nine-slice」时才设 `1` 并填 border
- Text 节点可不挂 sprite

### 4. 出 Parts（色幕抠图）

**优先色幕生图再 chroma**（比假透明白底/棋盘格可靠）。入口：`Tools/UI/chroma_key.py`（旧名 `chroma_green.py` 仍可用，默认 green）。

#### 选幕规则（先判颜色）

| 幕 | Hex | 何时用 |
|----|-----|--------|
| **绿幕** `green` | `#00FF00` | 零件本身**不是**大面积绿（金框、木纹、蓝/棕按钮） |
| **品红幕** `magenta` | `#FF00FF` | 零件含**绿色填充**（绿按钮、绿开关）——绿幕会挖空本体 |
| **红幕** `red` | `#FF0000` | 绿/品红都与主体冲突时；或截图背景偏红 |
| **蓝幕** `blue` | `#0000FF` | 暖色主体（金/木/橙）且绿幕易脏边 |
| **青幕** `cyan` | `#00FFFF` | 备用 |
| **auto** | 四角采样 | **截图直切**、不确定幕色：采四角多数色，再分类/custom |
| **custom** | 四角采样色 | 羊皮纸/米色底等非纯色幕，用距离抠 |

```bash
# 生图零件（推荐）
python Tools/UI/chroma_key.py Raw_Chroma/gs_xxx.png --out Parts/xxx.png --key magenta

# 截图直切（羊皮纸/未知底）
python Tools/UI/chroma_key.py shot.png --out Parts/xxx.png --key auto

# 旧入口（默认 green）
python Tools/UI/chroma_green.py Raw_Green/gs_xxx.png --out Parts/xxx.png
```

流程：

1. 生到 `Raw_Chroma/`（或 `Raw_Green/`），背景写死对应纯色幕（禁止棋盘格/白底）  
2. 主体含绿 → **必须品红幕**，且中心必须是**实心填充**（禁止空心框，品红会变成透明洞）  
3. `chroma_key.py --key …` → `Parts/<name>.png`  
4. **按 layout 缩到设计像素**（否则 Unity `SetNativeSize` 暴大）：  
   `python Tools/UI/resize_parts_to_layout.py Assets/Art/UI/<ScreenId>/layout.json`  
5. 列表行尽量不挂 `row_bg`；通用件放 `Common/Parts/`，layout 写 `"sprite": "Common/btn_close.png"`

也可按槽位单独 GenerateImage；文件名与 `layout.sprite` 一致，写入 `Parts/`：

| 槽位示例 | 提示要点 |
|----------|----------|
| `panel_bg.png` | 圆角卡牌底板，透明底，无文字（勿默认九宫格） |
| `btn_primary.png` | 主按钮空壳，粗黑描边，透明底，无文字 |
| `btn_secondary.png` | 次按钮空壳 |
| `logo_potato.png` | 小徽章土豆 Logo，透明底 |

UI 组件 style lock：

```text
Style lock: flat badge UI chrome, thick uniform pure-black single outline,
flat 3-5 colors, transparent background, no text, no cream fringe on edges,
centered, game UI sprite. Do not assume nine-slice.
```

占位裁切（可选）：

```bash
python Tools/UI/crop_from_layout.py Assets/Art/UI/<ScreenId>/layout.json
```

### 5. 拼 Prefab

Agent **不**依赖 Unity MCP。保证 JSON/PNG 齐套后提示用户：

1. Unity 菜单 **Art/UI/Build Prefab From Layout**
2. 选择该屏 `layout.json`
3. 打开生成的 `Prefabs/UI_<ScreenId>.prefab` 验收

业务按钮事件不在本管线绑定。

## 与角色管线的关系

- 角色分层：`potato-badge-art` + `Art/Role/Parts`
- UI：本 Skill + `Art/UI/<ScreenId>`
- 画风同一把锁；UI 允许「徽章风面板/按钮」，角色 Skill 里「no UI chrome」仅约束角色立绘

## 验收清单

- [ ] structure / layout 节点 id 对齐
- [ ] Parts 透明底干净，文件名匹配 layout
- [ ] Prefab Hierarchy 与 structure 一致
- [ ] 未擅自开九宫格（仅用户点名的节点可为 Sliced）
- [ ] 文案来自 TMP，不烙在 PNG 上
