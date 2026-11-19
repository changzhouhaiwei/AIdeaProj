using System;
using FishFramework;
using TMPro;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 主界面 / 结算下一关按钮：按难度把 TMP 材质切到 Material_green / _red / _purple；
    /// 金币关（XSort_btn_coinreward）用 orange。
    /// 挂 IAfterLocalizationFontApply，避免语言切换把颜色改回去。
    /// </summary>
    public static class PlayBtnDifficultyFont
    {
        const string MikadoMatFmt = "Assets/CABRes/Fonts/MIKADO-BLACK SDF Material_{0}.mat";
        const string ArialMatFmt = "Assets/CABRes/Fonts/Arial-Unicode-Bold-RSDF3 btn_{0}.mat";
        const string ArialPurpleFallback = "Assets/CABRes/Fonts/Arial-Unicode-Bold-RSDF3 btn_puple 1.mat";

        static Material sMikadoGreen;
        static Material sMikadoRed;
        static Material sMikadoPurple;
        static Material sMikadoOrange;
        static Material sArialGreen;
        static Material sArialRed;
        static Material sArialPurple;
        static Material sArialOrange;

        public static void Apply(TMP_Text tmp, int difficulty, bool restoreOriginal = false, bool isCoinLevel = false)
        {
            if (tmp == null)
                return;

            var binder = tmp.GetComponent<PlayBtnDifficultyFontBinder>();
            if (binder == null)
                binder = tmp.gameObject.AddComponent<PlayBtnDifficultyFontBinder>();

            binder.Configure(tmp, difficulty, restoreOriginal, isCoinLevel);
            binder.ApplyNow();
        }

        internal static void ApplyConfigured(TMP_Text tmp, int difficulty, bool restoreOriginal, bool isCoinLevel)
        {
            if (tmp == null)
                return;

            if (restoreOriginal)
            {
                LocalizationFontSwitcher.ApplyTo(tmp);
                return;
            }

            var mat = LoadColorMaterial(tmp, difficulty, isCoinLevel);
            if (mat == null)
                return;

            if (tmp.fontSharedMaterial != mat)
                tmp.fontSharedMaterial = mat;

            // 描边颜色在 Material_* 上；未开 OUTLINE_ON 时换材质看起来完全没变。
            if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth) && mat.GetFloat(ShaderUtilities.ID_OutlineWidth) > 0f)
                mat.EnableKeyword("OUTLINE_ON");

            tmp.extraPadding = true;
            tmp.UpdateMeshPadding();
            tmp.SetMaterialDirty();
            tmp.SetVerticesDirty();
            tmp.havePropertiesChanged = true;
            tmp.ForceMeshUpdate(true, true);
        }

        static string ColorKey(int difficulty, bool isCoinLevel)
        {
            if (isCoinLevel)
                return "orange";
            if (difficulty == 1)
                return "red";
            if (difficulty == 2)
                return "purple";
            return "green";
        }

        static Material LoadColorMaterial(TMP_Text tmp, int difficulty, bool isCoinLevel)
        {
            string color = ColorKey(difficulty, isCoinLevel);
            bool useArial = IsArialFont(tmp);

            if (useArial)
            {
                if (color == "red")
                    return LoadCached(ref sArialRed, string.Format(ArialMatFmt, color));
                if (color == "purple")
                {
                    var purple = LoadCached(ref sArialPurple, string.Format(ArialMatFmt, color));
                    if (purple != null)
                        return purple;
                    return LoadCached(ref sArialPurple, ArialPurpleFallback);
                }
                if (color == "orange")
                    return LoadCached(ref sArialOrange, string.Format(ArialMatFmt, color));

                return LoadCached(ref sArialGreen, string.Format(ArialMatFmt, color));
            }

            if (color == "red")
                return LoadCached(ref sMikadoRed, string.Format(MikadoMatFmt, color));
            if (color == "purple")
                return LoadCached(ref sMikadoPurple, string.Format(MikadoMatFmt, color));
            if (color == "orange")
                return LoadCached(ref sMikadoOrange, string.Format(MikadoMatFmt, color));
            return LoadCached(ref sMikadoGreen, string.Format(MikadoMatFmt, color));
        }

        static bool IsArialFont(TMP_Text tmp)
        {
            if (tmp == null)
                return false;
            if (tmp.font != null && tmp.font.name.IndexOf("Arial", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            var mat = tmp.fontSharedMaterial;
            return mat != null && mat.name.IndexOf("Arial", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static Material LoadCached(ref Material cached, string path)
        {
            if (cached != null)
                return cached;
            cached = UILoadHelper.LoadAsset<Material>(path);
            return cached;
        }
    }
}
