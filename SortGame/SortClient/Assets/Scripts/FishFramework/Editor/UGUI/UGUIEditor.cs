using I2.Loc;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FishFramework
{
    public class UGUIEditor : MonoBehaviour
    {
        /// <summary>  
        /// 第一次创建UI元素时，没有canvas、EventSystem所有要生成，Canvas作为父节点  
        /// 之后再空的位置上建UI元素会自动添加到Canvas下  
        /// 在非UI树下的GameObject上新建UI元素也会 自动添加到Canvas下（默认在UI树下）  
        /// 添加到指定的UI元素下  
        /// </summary>  
        // 如果第一次创建UI元素 可能没有 Canvas、EventSystem对象！  
        private static GameObject FindCanvas()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas)
                return canvas.gameObject;

            var obj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

            var canvasScaler = obj.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(640, 1280);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0;

            var raycaster = obj.GetComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.All;

            return obj;
        }

        [MenuItem("GameObject/UI/Text - Custom &q")]
        private static void CreateText()
        {
            GameObject txtObj = new GameObject("Text", typeof(TextMeshProUGUI))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            txtObj.transform.InitParentAndLocalTransform(!Selection.activeTransform ? FindCanvas().transform : Selection.activeTransform);
            txtObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            txtObj.GetComponent<RectTransform>().localScale = Vector3.one;
            txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 30);
            var text = txtObj.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CABRes/Fonts/Arial-Unicode-Bold-RSDF.asset");
            text.raycastTarget = false;
            text.richText = false;
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(txtObj, "Create " + txtObj.name);

            Selection.activeGameObject = txtObj;
        }

        [MenuItem("GameObject/UI/TextLocalize - Custom &w")]
        private static void CreateTextLocalize()
        {
            GameObject txtObj = new GameObject("Text", typeof(TextMeshProUGUI))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            txtObj.transform.InitParentAndLocalTransform(!Selection.activeTransform ? FindCanvas().transform : Selection.activeTransform);
            txtObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            txtObj.GetComponent<RectTransform>().localScale = Vector3.one;
            txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 30);
            var text = txtObj.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CABRes/Fonts/AdobeHeitiStd-RSDF.asset");
            text.raycastTarget = false;
            text.richText = false;
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(txtObj, "Create " + txtObj.name);

            AddLocalize(txtObj);

            Selection.activeGameObject = txtObj;
        }


        [MenuItem("GameObject/UI/Image - Custom &1")]
        private static void CreateImage()
        {
            GameObject imgObj = new GameObject("Image", typeof(Image))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            if (!Selection.activeTransform || !Selection.activeTransform.GetComponent<RectTransform>())
                imgObj.transform.InitParentAndLocalTransform(FindCanvas().transform);
            else
                imgObj.transform.InitParentAndLocalTransform(Selection.activeTransform);
            imgObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            imgObj.GetComponent<RectTransform>().localScale = Vector3.one;
            imgObj.GetComponent<Image>().raycastTarget = false;
            imgObj.GetComponent<Image>().fillCenter = false;

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(imgObj, "Create " + imgObj.name);

            Selection.activeGameObject = imgObj;
        }

        [MenuItem("GameObject/UI/Raw Image - Custom")]
        private static void CreateRawImage()
        {
            GameObject imgObj = new GameObject("RawImage", typeof(RawImage))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            if (!Selection.activeTransform || !Selection.activeTransform.GetComponent<RectTransform>())
                imgObj.transform.InitParentAndLocalTransform(FindCanvas().transform);
            else
                imgObj.transform.InitParentAndLocalTransform(Selection.activeTransform);
            imgObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            imgObj.GetComponent<RectTransform>().localScale = Vector3.one;
            imgObj.GetComponent<RawImage>().raycastTarget = false;

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(imgObj, "Create " + imgObj.name);

            Selection.activeGameObject = imgObj;
        }

        [MenuItem("GameObject/UI/Button - Custom &2")]
        private static void CreateButton()
        {
            GameObject btnObj = new GameObject("Button", typeof(Image), typeof(TButton))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            if (!Selection.activeTransform || !Selection.activeTransform.GetComponent<RectTransform>())
                btnObj.transform.InitParentAndLocalTransform(FindCanvas().transform);
            else
                btnObj.transform.InitParentAndLocalTransform(Selection.activeTransform);
            btnObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            btnObj.GetComponent<RectTransform>().localScale = Vector3.one;
            var image = btnObj.GetComponent<Image>();
            var button = btnObj.GetComponent<TButton>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            GameObject txtObj = new GameObject("Text", typeof(TextMeshProUGUI))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            txtObj.transform.InitParentAndLocalTransform(btnObj.transform);
            txtObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            txtObj.GetComponent<RectTransform>().localScale = Vector3.one;
            txtObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            txtObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            txtObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            var text = txtObj.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CABRes/Fonts/AdobeHeitiStd-RSDF.asset");
            text.raycastTarget = false;
            text.richText = false;
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(txtObj, "Create " + txtObj.name);

            Selection.activeGameObject = btnObj;
        }

        [MenuItem("GameObject/UI/Empty Button - Custom")]
        private static void CreateEmptyButton()
        {
            GameObject btnObj = new GameObject("EmptyButton", typeof(Image), typeof(TButton))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            if (!Selection.activeTransform || !Selection.activeTransform.GetComponent<RectTransform>())
                btnObj.transform.InitParentAndLocalTransform(FindCanvas().transform);
            else
                btnObj.transform.InitParentAndLocalTransform(Selection.activeTransform);
            btnObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            btnObj.GetComponent<RectTransform>().localScale = Vector3.one;
            // btnObj.GetComponent<TButton>().clickDoTween = false;
            // btnObj.GetComponent<TButton>().audioId = string.Empty;

            var image = btnObj.GetComponent<Image>();
            Color color = image.color;
            color.a = 0f;
            image.color = color;
            var button = btnObj.GetComponent<TButton>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(btnObj, "Create " + btnObj.name);

            Selection.activeGameObject = btnObj;
        }

        [MenuItem("GameObject/UI/Toggle - Custom")]
        private static void CreateToggle()
        {
            GameObject tgObj = new GameObject("Toggle", typeof(Image), typeof(TToggle))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            if (!Selection.activeTransform || !Selection.activeTransform.GetComponent<RectTransform>())
                tgObj.transform.SetParent(FindCanvas().transform);
            else
                tgObj.transform.SetParent(Selection.activeTransform);
            tgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
            tgObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            tgObj.GetComponent<RectTransform>().localScale = Vector3.one;
            var imgBackground = tgObj.GetComponent<Image>();
            imgBackground.raycastTarget = true;
            var toggle = tgObj.GetComponent<TToggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.isOn = false;
            toggle.targetGraphic = imgBackground;

            GameObject checkmark = new GameObject("Checkmark", typeof(Image));
            checkmark.transform.SetParent(tgObj.transform);
            checkmark.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
            checkmark.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            checkmark.GetComponent<RectTransform>().localScale = Vector3.one;
            var imgCheckmark = checkmark.GetComponent<Image>();
            imgCheckmark.raycastTarget = false;
            toggle.graphic = imgCheckmark;

            GameObject txtObj = new GameObject("Label", typeof(TextMeshProUGUI))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            txtObj.transform.InitParentAndLocalTransform(tgObj.transform);
            txtObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            txtObj.GetComponent<RectTransform>().localScale = Vector3.one;
            txtObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            txtObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            txtObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            var text = txtObj.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CABRes/Fonts/AdobeHeitiStd-RSDF.asset");
            text.raycastTarget = false;
            text.richText = false;
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.text = "toggle";
            toggle.ToggleLabel = text;

            Selection.activeGameObject = tgObj;
        }

        [MenuItem("GameObject/UI/Scroll View - Custom")]
        private static void CreateScrollView()
        {
            GameObject scrollObj = new GameObject("ScrollView", typeof(Image), typeof(ScrollRect))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            if (!Selection.activeTransform || !Selection.activeTransform.GetComponent<RectTransform>())
                scrollObj.transform.InitParentAndLocalTransform(FindCanvas().transform);
            else
                scrollObj.transform.InitParentAndLocalTransform(Selection.activeTransform);
            scrollObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 200);
            scrollObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            scrollObj.GetComponent<RectTransform>().localScale = Vector3.one;
            Color color = scrollObj.GetComponent<Image>().color;
            color.a = 0f;
            scrollObj.GetComponent<Image>().color = color;
            var scrollView = scrollObj.GetComponent<ScrollRect>();

            GameObject viewportObj = new GameObject("Viewport", typeof(Image), typeof(RectMask2D))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            viewportObj.transform.InitParentAndLocalTransform(scrollObj.transform);
            viewportObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            viewportObj.GetComponent<RectTransform>().localScale = Vector3.one;
            viewportObj.GetComponent<RectTransform>().pivot = Vector2.up;
            viewportObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            viewportObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            viewportObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
            color = viewportObj.GetComponent<Image>().color;
            color.a = 0f;
            viewportObj.GetComponent<Image>().color = color;
            scrollView.viewport = viewportObj.GetComponent<RectTransform>();

            GameObject contentObj = new GameObject("Content", typeof(RectTransform))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            contentObj.transform.InitParentAndLocalTransform(viewportObj.transform);
            contentObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            contentObj.GetComponent<RectTransform>().localScale = Vector3.one;
            contentObj.GetComponent<RectTransform>().pivot = Vector2.up;
            contentObj.GetComponent<RectTransform>().anchorMin = Vector2.up;
            contentObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            contentObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 200);
            scrollView.content = contentObj.GetComponent<RectTransform>();

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(scrollObj, "Create " + scrollObj.name);

            Selection.activeGameObject = scrollObj;
        }

        private static void AddLocalize(GameObject txtObj)
        {
            // var localize = txtObj.AddComponent<Localize>();
            // switch (System.Environment.MachineName)
            // {
            //     case "DESKTOP-VH4CVD8":
            //         localize.Source = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/CABRes/ScriptableObject/I2Languages_Logic_ZHJ.asset") as ILanguageSource;
            //         break;
            //     case "DESKTOP-JG5BBPR":
            //         localize.Source = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/CABRes/ScriptableObject/I2Languages_Logic_YH.asset") as ILanguageSource;
            //         break;
            //     case "DESKTOP-AB9VPHA":
            //         localize.Source = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/CABRes/ScriptableObject/I2Languages_Logic_WHW.asset") as ILanguageSource;
            //         break;
            //     case "DESKTOP-6DKTO9Q":
            //         // 姜海良
            //         localize.Source = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/CABRes/ScriptableObject/I2Languages_Logic_JHL.asset") as ILanguageSource;
            //         break;
            // }
        }
    }
}