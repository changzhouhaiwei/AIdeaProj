# AIdeaDesktop (Unity) — 火车窗动态壁纸

从 `IdeaProj/AIdeaDesktop`（pygame）移植到 Unity 2022.3。

## 资源

全部走 **Resources** + **场景挂载**（不用 StreamingAssets）：

```
Assets/Resources/Wallpaper/
  foreground.png      # Sprite
  background.png      # Sprite
  config.json         # TextAsset（speed / fps）
  Chars/
    layout.json                 # 淡入1.4s / 停留30s / 淡出1.4s；居中站姿锚点
    stand_a.png … stand_j.png   # 窗前站姿角色（随机切换池）
    char_a.png … char_j.png     # 旧坐姿资源（保留，未进当前池）
```

`SampleScene` 里有 `WallpaperApp` 组件，Inspector 已挂好前景/背景/角色引用；引用为空时会回退 `Resources.Load`。

## 用法

1. 打开 `DesktopGame` 工程，进 Play → 预览（1280×720）
   - 角色在车厢中央靠窗站立，约 1.4s 淡入 → 30s → 1.4s 淡出，再纯随机换下一张
   - `←` / `→` 调速 · 空格暂停 · Esc 退出 Play
2. **一键打包**（Unity 菜单栏）：
   - `AIdeaDesktop` → `一键打包 Windows 桌面版` → 输出 `DesktopGame/Builds/Windows64/AIdeaDesktop.exe`
   - `一键打包并运行（桌面壁纸）` / `（窗口预览）` → 打完直接启动
   - `打开输出目录` → 定位产物
3. 出包后桌面模式：勾选 `Desktop Mode`，或命令行 `--desktop`
   - Standalone 会把 Unity 窗口挂到 WorkerW（图标后方）
   - 退出：`Ctrl+Shift+Q`

## 脚本

| 文件 | 作用 |
|------|------|
| `WallpaperApp.cs` | 主循环：滚动背景 + 车厢前景 |
| `CharacterCycle.cs` | 窗前站姿角色淡入/停留/淡出（纯随机，避免连抽同一张） |
| `DesktopWinHost.cs` | WorkerW 嵌入（仅 Windows Standalone） |
| `WallpaperConfig.cs` | 读 Resources 里的 config |
| `Editor/DesktopBuildMenu.cs` | 菜单一键打 Win64 桌面包 |
