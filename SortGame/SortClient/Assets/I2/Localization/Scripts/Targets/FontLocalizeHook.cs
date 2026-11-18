using System;
using UnityEngine;

namespace I2.Loc
{
    /// <summary>
    /// I2 程序集内的轻量钩子，供外部（Assembly-CSharp）订阅 TMP 本地化完成事件，
    /// 避免 I2 直接依赖游戏侧类型。
    /// </summary>
    public static class FontLocalizeHook
    {
        /// <summary>参数为刚完成本地化的 TMP 目标（TextMeshProUGUI / TextMeshPro）。</summary>
        public static Action<Component> OnTmpLocalized;

        public static void NotifyTmpLocalized(Component tmpTarget)
        {
            if (tmpTarget == null)
                return;
            var handler = OnTmpLocalized;
            if (handler != null)
                handler(tmpTarget);
        }
    }
}
