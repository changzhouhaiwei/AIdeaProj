using Sirenix.OdinInspector;

namespace Luban.Editor
{
    [LabelText("一键导出语言包(I2)")]
    public class LubanAfterGenI2Localization : IAfterGen
    {
        public void Process()
        {
            UnityEngine.Debug.Log("[Luban] 生成完成，开始执行「游戏工具/一键导出语言包」。");
            I2LocalizationOneClickImporter.ExportSheetAndImportReplace();
        }
    }
}
