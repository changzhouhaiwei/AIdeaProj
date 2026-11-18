namespace xasset.editor
{
    /// <summary>
    ///     脚本运行模式
    /// </summary>
    public enum ScriptPlayMode
    {
        /// <summary>
        ///     预加载模式，需要先打包，不触发版本更新
        /// </summary>
        Preload,

        /// <summary>
        ///     增量模式，需要先打包，可以在编辑器调试真机热更加载逻辑。
        /// </summary>
        Increment
    }
}