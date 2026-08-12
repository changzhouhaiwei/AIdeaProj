# Settings 冒烟验收

从你提供的样式参考 `ref_style.png` 解读信息架构后，按本地闭环生成（画风为工程锁定的徽章土豆风，非原图木纹厚涂）。

## 解读到的结构

1. 居中弹层底板 + 顶栏标题牌「设置」+ 右上关闭  
2. 四个方形开关：音乐 / 音效 / 语音 / 震动  
3. 六行列表：语言、主题、关于我们、评分、怎么玩、客服支持（左图标 + 文案 + 右箭头）  
4. 底栏：隐私政策 | 服务条款  

## 产物

| 产物 | 路径 |
|------|------|
| 样式参考 | `ref_style.png` |
| 结构 | `ui_structure.json` |
| 效果图 | `mockup.png` |
| 布局 | `layout.json` |
| 图素 | `Parts/*.png`（面板、标题牌、关闭、开关底、行底、图标） |
| Prefab | Unity 生成 → `Prefabs/UI_Settings.prefab` |

## Unity 步骤

1. 菜单 **Art/UI/Build Prefab From Layout**  
2. 选择 `Assets/Art/UI/Settings/layout.json`  
3. 打开 `Prefabs/UI_Settings.prefab` 预览；文案由 TMP 显示  
