---
name: sort-feature-conventions
description: >-
  SortGame 增加功能、改界面、改多语言或配置表时的工程约定。新控件放在预制体上，不要用代码生成 Button、Image 这类组件。改多语言或配表时，SortProfile 的 Excel、生成的 json 和代码必须一起改。Use when adding a feature or UI, editing prefabs, or changing localization, I2 text, Luban tables, config json, or SortProfile xlsx.
---

# Sort 功能约定

## 界面组件放预制体

如果增加功能，优先在预制体上放组件，尽量不要用代码生成 Button、Image 这类组件。

- 改 `SortClient/Assets/CABRes/Prefabs` 里已有预制体，或新建预制体再挂组件。脚本用 `[SerializeField]` 引用预制体上的 Button、Image、TextMeshProUGUI。
- 不要 `AddComponent<Button>()`、`AddComponent<Image>()`，也不要运行时 `new GameObject` 再挂这些 UI 组件。
- 需要多个相同控件时，实例化已经配好组件的预制体。
- 玩法逻辑、数据和动画驱动写代码。可见控件本身放在预制体上。

## 多语言和配置表

如果改动了多语言或者配置表，除了 json 和代码，`SortProfile` 里的 Excel 也要对应修改。三处必须一致，不能只改生成物。

| 内容 | Excel（源） | 代码 | 运行时数据 |
| --- | --- | --- | --- |
| 配置表 | `SortProfile/*.xlsx` | `SortClient/Assets/Scripts/HotFix/GameConfig` | `SortClient/Assets/CABRes/Configs/*.json` |
| 多语言 | `SortProfile/LocalizationConfig.xlsx` | 引用文案 key 的脚本 | `SortClient/Assets/Resources/LocalizationConfig.csv`，并导入 I2 |

现有表：`WinStreak.xlsx`、`SevenDaySign.xlsx`、`Settings.xlsx`、`UserInfo.xlsx`、`LocalizationConfig.xlsx`。新表还要改 `__tables__.xlsx` 和 `__beans__.xlsx`。

- 配表在 Unity 里打开 `Assets/Luban.asset` 点生成。代码写到 `GameConfig`，json 写到 `CABRes/Configs`。
- 语言包用菜单 `游戏工具/一键导出语言包`（`SortClient/Tools/Localization`），从 `LocalizationConfig.xlsx` 导出 csv 并 Replace 进 I2。
- `__beans__.xlsx` 的 `*fields` 表头必须保持 `H1:L1` 合并，`__enums__.xlsx` 保持 `G1:K1` 合并，否则 Luban 读不出字段。
