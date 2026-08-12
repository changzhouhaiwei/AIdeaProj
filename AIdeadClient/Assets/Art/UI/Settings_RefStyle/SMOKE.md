# Settings_RefStyle — 绿幕全流程（测试）

## 流程

1. 参考 `ref_style.png`（木纹截图风格，非项目徽章风）  
2. 零件出图到 `Raw_Green/`：默认 **绿幕 #00FF00**；绿色开关钮用 **品红幕 #FF00FF**（避免抠掉按钮本体）  
3. `python Tools/UI/chroma_green.py … --key green|magenta` → `Parts/` 真透明 + 紧裁  
4. `python Tools/UI/resize_parts_to_layout.py …/layout.json` → Parts 缩到 layout 设计尺寸（`SetNativeSize` 才正确）  
5. `layout.json`（列表行无 `row_bg`，减少透明层）  
6. Unity：**Art/UI/Build Prefab From Layout** → `Prefabs/UI_Settings_RefStyle.prefab`

## 目录

- `Raw_Green/` 生图原片（带幕）  
- `Parts/` chroma 后交付  
- `mockup.png` 整屏参考（已去绿幕）  
