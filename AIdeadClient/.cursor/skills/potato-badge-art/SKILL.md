---
name: potato-badge-art
description: >-
  Generates and edits AIdeadClient / 土豆围城 art in the locked badge emblem style
  (Brotato-like flat glyph). Use when creating or revising characters, enemies,
  weapons, items, UI icons, role sprites, GenerateImage prompts, cutouts, or
  when the user mentions 画风、立绘、角色图、徽章风、土豆风、出图、切图.
---

# 土豆徽章画风（工程锁定）

## 权威参考（先读再画）

1. `Assets/Art/Role/role_potato.png` — 成品锁定样张  
2. `Assets/Art/Role/Parts/_preview_assembled.png` — 分层装配预览  
3. `Assets/Art/Role/Prefabs/Role_Potato.prefab` — 游戏内装配结构  

与参考不一致时，**改图就参考，不另起画风**。

## 风格锁（Style Lock）

| 项 | 规格 |
|----|------|
| 类型 | 扁平徽章 / Logo 符号风，可读性优先 |
| 描边 | 粗、均匀、纯黑；单层 |
| 上色 | 平涂；每主体 3–5 色 |
| 纹理 | 可选极轻颗粒；禁止厚涂光影 |
| 五官 | 两黑圆点眼为主；可无嘴巴 |
| 肢体 | 细黑臂杆 + 小圆关节手 |
| 武器 | 块状黑色剪影，几何清晰 |
| 构图 | 角色居中；背景透明或纯色占位（交付必须透明） |
| 透视 | 正面或轻微 3/4；不要复杂透视 |

## GenerateImage 提示词模板

英文提示词末尾**固定追加** style lock 段：

```text
Style lock (mandatory): flat badge emblem / logo glyph art like Brotato potato icons,
thick uniform pure-black outlines (single outline only, no double stroke, no cream halo),
flat limited 3-5 color palette, tiny grain texture optional, two solid black dot eyes,
no mouth unless requested, thin black stick arms with small circular hand joints,
blocky black silhouette weapons, centered character, clean silhouette readable at tiny size,
no realistic rendering, no anime cel shading, no neon glow, no purple cyber aesthetic,
no text, no UI chrome.
```

角色变体时只改主体物种/颜色/武器词，**不要改 style lock**。

### 角色出图示例骨架

```text
Badge emblem game character, [SUBJECT: e.g. apple warrior / potato king],
[SIGNATURE PROP: e.g. dual pistols / crown], front view, transparent or solid cream placeholder background.
+ (append Style lock block above)
```

用 `GenerateImage` 时传入参考图路径（若可用）：

- `reference_image_paths`: `Assets/Art/Role/role_potato.png`

## 切图与分层约定

需要朝向/举枪时，拆成：

| 层 | 说明 |
|----|------|
| body | 躯干+标志性头饰；眼洞填平 |
| eye | 单眼模板（左右复用） |
| arm_l/r | 细臂杆 |
| hand_l/r | 圆关节（旋转锚点） |
| gun_l/r | 武器剪影 |
| limb_l/r | 可选整肢（简单旋转） |

导出目录：`Assets/Art/Role/Parts/`（或对应角色子目录）  
锚点写进 `parts_meta.json`（canvas 坐标 + hand pivot）。

去底规则：

- 硬切透明；外缘脏奶油/灰边删掉  
- 描边外缘收成纯黑  
- 禁止软边光晕「第二圈描边」

## 预制体约定

角色 Visual 层级对齐 Asset：

```text
Role_XXX / Visual
  Body
  Aim_L / Limb_L / (Arm, Gun, Hand)
  Aim_R / Limb_R / (Arm, Gun, Hand)
  Eye_L, Eye_R
```

重建土豆样板菜单：`Art/Role/Create Role_Potato Prefab`

## 验收清单

- [ ] 缩到 64px 仍能认出是谁  
- [ ] 只有单层黑描边，无浅色描边光晕  
- [ ] 颜色 ≤5，且与参考同属一类扁平感  
- [ ] 透明底干净（夹角/弧面无奶油渣）  
- [ ] 若要进战斗：眼睛与枪可分层，手心有锚点  
