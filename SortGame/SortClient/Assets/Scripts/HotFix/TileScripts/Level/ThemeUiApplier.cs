using System.Collections.Generic;
using FishFramework;
using Spine.Unity;
using TileGameCore;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FishTilePlay
{
    /// <summary>
    /// 按 tilebaseId 加载独立 ThemePack；空引用不改预制体默认。
    /// </summary>
    public static class ThemeUiApplier
    {
        public const int DefaultTileBaseId = 0;
        public const float ClickVfxHoldSeconds = 0.3f;
        public const string PackFolder = "Assets/CABRes/LevelScriptableObject/Themes";
        const string PackPathFormat = PackFolder + "/{0}.asset";
        const string ClickVfxSlot = "themeClickVfx";

        static readonly Dictionary<int, TileBaseThemePack> packs = new Dictionary<int, TileBaseThemePack>();

        public static int CurrentTileBaseId => LevelDirector.LoadedLevelTheme;

        public static TileBaseThemePack Get() => Get(CurrentTileBaseId);

        public static void ReleaseCachedPacks()
        {
            packs.Clear();
        }

        public static TileBaseThemePack Get(int tilebaseId)
        {
            if (packs.TryGetValue(tilebaseId, out TileBaseThemePack cached))
            {
                return cached;
            }

            TileBaseThemePack pack = UILoadHelper.LoadAsset<TileBaseThemePack>(
                string.Format(PackPathFormat, tilebaseId));
            packs[tilebaseId] = pack;
            return pack;
        }

        /// <summary>
        /// 有 button 才返回 buttonClickVfx；点击时与 buttonClickSound 用同一 tilebaseId。
        /// </summary>
        public static GameObject PairedClickVfx(int tilebaseId)
        {
            TileBaseThemePack pack = Get(tilebaseId);
            if (pack == null || pack.button == null)
            {
                return null;
            }

            return pack.buttonClickVfx;
        }

        public static void ApplySkeleton(SkeletonGraphic spine, SkeletonDataAsset asset)
        {
            if (spine == null || asset == null)
            {
                return;
            }

            if (spine.skeletonDataAsset == asset && spine.IsValid)
            {
                return;
            }

            spine.skeletonDataAsset = asset;
            spine.initialSkinName = string.Empty;
            spine.Initialize(true);
        }

        public static void ApplyButtonThemeStyle(
            Transform buttonRoot,
            Sprite sprite,
            Material appearMaterial = null,
            bool playAppear = true)
        {
            if (buttonRoot == null)
            {
                return;
            }

            Transform style = buttonRoot.Find("themeStyle");
            if (style == null)
            {
                return;
            }

            bool show = sprite != null;
            style.gameObject.SetActive(show);
            if (!show)
            {
                ClearButtonAppear(style);
                Image hiddenImage = style.GetComponent<Image>();
                if (hiddenImage != null)
                {
                    // 清掉上一主题残留图，避免之后误打开时闪出旧按钮
                    hiddenImage.sprite = null;
                }

                return;
            }

            Image image = style.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.SetNativeSize();
            }

            ApplyButtonAppear(style, appearMaterial, playAppear);
        }

        /// <summary>
        /// 结算 Result02 真正显示后再播主题按钮溶解，避免在隐藏阶段播完。
        /// </summary>
        public static void PlayButtonThemeAppear(Transform buttonRoot)
        {
            if (buttonRoot == null)
            {
                return;
            }

            Transform style = buttonRoot.Find("themeStyle");
            if (style == null)
            {
                return;
            }

            var appear = style.GetComponent<UIDissolveAppear>();
            // 无主题溶解材质 = 普通关；切勿强行打开 themeStyle（否则会把上一主题残留图重新亮出来）
            if (appear == null || !appear.HasThemeAppearMaterial)
            {
                return;
            }

            // 父级 Result02 Animator 可能短暂关掉过 themeStyle
            if (!style.gameObject.activeSelf)
            {
                style.gameObject.SetActive(true);
            }

            appear.Play();
        }

        static void ApplyButtonAppear(Transform style, Material appearMaterial, bool playAppear)
        {
            if (style == null)
            {
                return;
            }

            var appear = style.GetComponent<UIDissolveAppear>();
            if (appearMaterial == null)
            {
                if (appear != null)
                {
                    appear.ClearThemeMaterial();
                }
                else
                {
                    var image = style.GetComponent<Image>();
                    if (image != null)
                    {
                        image.material = null;
                    }
                }

                return;
            }

            if (appear == null)
            {
                appear = style.gameObject.AddComponent<UIDissolveAppear>();
            }

            appear.ApplyThemeMaterial(appearMaterial, playAppear);
        }

        static void ClearButtonAppear(Transform style)
        {
            if (style == null)
            {
                return;
            }

            var appear = style.GetComponent<UIDissolveAppear>();
            if (appear != null)
            {
                appear.ClearThemeMaterial();
                return;
            }

            var image = style.GetComponent<Image>();
            if (image != null)
            {
                image.material = null;
            }
        }

        public static void PlayClickVfx(Transform buttonRoot, GameObject prefab)
        {
            if (buttonRoot == null || prefab == null)
            {
                return;
            }

            Transform existing = buttonRoot.Find(ClickVfxSlot);
            if (existing != null)
            {
                Object.Destroy(existing.gameObject);
            }

            // ice_btn_broken_effect 预制体 localScale 是 0（给 adAwardButton 入场动画用），
            // 直接 Instantiate(prefab, parent) 会带着 0 缩放 + playOnAwake 在错误位置喷粒子。
            GameObject inst = Object.Instantiate(prefab);
            inst.name = ClickVfxSlot;
            inst.SetActive(false);
            inst.transform.SetParent(buttonRoot, false);
            SnapClickVfxToButton(inst.transform);
            inst.SetActive(true);

            ParticleSystem[] particles = inst.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles[i].Clear(true);
                particles[i].Play(true);
            }

            Object.Destroy(inst, 2f);
            PlaySound(Get(TileBaseThemeResolver.ResolveCurrentLevelTileBaseId())?.buttonClickSound);
        }

        public static void PlaySound(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AudioController.PlaySound(clip);
        }

        public static void StopSound(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AudioController.StopSound(clip);
        }

        public static bool IsDefaultWinEffect(GameObject prefab)
        {
            return prefab == null || prefab == Get(DefaultTileBaseId)?.winEffect;
        }

        public static void HideClickVfx(Transform buttonRoot, GameObject prefab)
        {
            if (buttonRoot == null)
            {
                return;
            }

            Transform vfx = buttonRoot.Find(ClickVfxSlot);
            if (vfx != null)
            {
                vfx.gameObject.SetActive(false);
            }
        }

        static void SnapClickVfxToButton(Transform t)
        {
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            if (t is RectTransform rt)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition3D = Vector3.zero;
            }
            else
            {
                t.localPosition = Vector3.zero;
            }

            t.SetAsLastSibling();
        }

    }
}
