namespace cfg.Game
{
    /// <summary>
    /// Luban 导表后 Theme 可能是 int 或 string。统一收成表达式给 ThemeExprParser。
    /// 不要写回 Theme，避免导成 string 后编译失败。
    /// </summary>
    public sealed partial class GameLevelConfig
    {
        public string ThemeExpr { get; private set; }

        partial void PostInit()
        {
            ThemeExpr = Theme != null ? Theme.ToString() : "";
        }
    }
}
