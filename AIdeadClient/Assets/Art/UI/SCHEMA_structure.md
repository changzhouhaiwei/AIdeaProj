# ui_structure.json

用户/Agent 输入的 UI 真源。描述「有什么」，不必精确到像素。

## 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `screenId` | string | 目录名，如 `MainMenu` |
| `canvas` | `{ width, height }` | 设计分辨率 |
| `nodes` | array | 节点列表 |

### node

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | string | 唯一 id |
| `type` | string | `Root` / `Panel` / `Image` / `Button` / `Text` / `Icon` |
| `parent` | string \| null | 父节点 id；根为 `null` 或 `"root"` 的 parent 为空 |
| `text` | string? | 文案（Button / Text） |
| `hint` | object? | 粗略区域：`{ x, y, w, h }` 设计像素，左上原点 |
| `spriteHint` | string? | 期望 Parts 文件名，如 `btn_primary.png` |
| `nineSliceHint` | bool? | 是否倾向九宫格 |

坐标：`hint` 与后续 `layout.json` 均使用 **左上为 (0,0)、Y 向下**。
