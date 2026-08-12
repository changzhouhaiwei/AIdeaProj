# layout.json

拼 Prefab 的布局真源。由 structure + 效果图校对得到。须可被 Unity `JsonUtility` 解析。

## 字段

```json
{
  "screenId": "MainMenu",
  "canvas": { "width": 1080, "height": 1920 },
  "nodes": [
    {
      "id": "btn_start",
      "type": "Button",
      "parent": "root",
      "rect": { "x": 340, "y": 1100, "w": 400, "h": 120 },
      "anchorMinX": 0.5,
      "anchorMinY": 0.5,
      "anchorMaxX": 0.5,
      "anchorMaxY": 0.5,
      "pivotX": 0.5,
      "pivotY": 0.5,
      "sprite": "btn_primary.png",
      "nineSliceL": 32,
      "nineSliceR": 32,
      "nineSliceT": 32,
      "nineSliceB": 32,
      "useNineSlice": 1,
      "text": "开始",
      "fontSize": 48,
      "sortingOrder": 10
    }
  ]
}
```

| 字段 | 说明 |
|------|------|
| `rect` | 左上原点像素框 |
| `anchor*` / `pivot*` | RectTransform 归一化锚点与轴心 |
| `sprite` | `Parts/` 下文件名；纯 Text 可空 |
| `useNineSlice` | `1` 启用九宫格并写 `sprite.border` |
| `nineSliceL/R/T/B` | 边距（像素） |
| `text` / `fontSize` | TMP 文案 |
| `sortingOrder` | 同级绘制顺序（Sibling index 辅助） |

`parent` 指向同文件内另一 `id`；屏幕根节点 `id` 通常为 `root`，`parent` 为空字符串。
