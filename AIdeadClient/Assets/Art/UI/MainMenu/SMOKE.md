# MainMenu 冒烟验收

本屏已按本地闭环产出：

| 产物 | 路径 |
|------|------|
| 结构 | `ui_structure.json` |
| 效果图 | `mockup.png` |
| 布局 | `layout.json` |
| 图素 | `Parts/panel_bg.png`, `logo_potato.png`, `btn_primary.png`, `btn_secondary.png` |
| Prefab | 需在 Unity 内生成 → `Prefabs/UI_MainMenu.prefab` |

## 步骤

1. 打开工程，等 `Assets/Art/UI` 导入完成  
2. 菜单 **Art/UI/Build Prefab From Layout**  
3. 选择 `Assets/Art/UI/MainMenu/layout.json`  
4. 打开 `Prefabs/UI_MainMenu.prefab`，确认层级：`root → panel_bg → logo / title / btn_*`  
5. 拖入场景 Play：可见底板、Logo、三按钮与 TMP 文案「开始 / 设置 / 退出」

## 可选：裁切脚本自检

不覆盖已有 Parts（应打印 `keep existing`）：

```bash
python Tools/UI/crop_from_layout.py Assets/Art/UI/MainMenu/layout.json
```

写入临时目录验证缩放：

```bash
python Tools/UI/crop_from_layout.py Assets/Art/UI/MainMenu/layout.json --parts Assets/Art/UI/MainMenu/_crop_tmp --overwrite
```
