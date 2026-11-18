using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TileGameCore
{
    [System.Serializable]
    public class FloatingCloud
    {
        [SerializeField] Data[] floatingCloudCases;

        private static Dictionary<int, Data> floatingCloudLink = new Dictionary<int, Data>();
        private static List<Animation> activeClouds = new List<Animation>();

        public void Init()
        {
            for (int i = 0; i < floatingCloudCases.Length; i++)
            {
                RegisterCase(floatingCloudCases[i]);
            }

            Currency[] currencies = CurrencyController.Currencies;
            if(!currencies.IsNullOrEmpty())
            {
                foreach (var currency in currencies)
                {
                    Currency.FloatingCloudCase floatingCloudCase = currency.FloatingCloud;
                    if (floatingCloudCase.AddToCloud)
                    {
                        FloatingCloudSettings floatingCloudSettings;

                        if (floatingCloudCase.SpecialPrefab != null)
                        {
                            floatingCloudSettings = new FloatingCloudSettings(currency.CurrencyType.ToString(), floatingCloudCase.SpecialPrefab);
                        }
                        else
                        {
                            floatingCloudSettings = new FloatingCloudSettings(currency.CurrencyType.ToString(), currency.Icon, new Vector2(100, 100));
                        }

                        floatingCloudSettings.SetAudio(floatingCloudCase.AppearAudioClip, floatingCloudCase.CollectAudioClip);

                        RegisterCase(floatingCloudSettings);
                    }
                }
            }
        }

        public static void Clear()
        {
            Unload();

            foreach (Data floatingCloudData in floatingCloudLink.Values)
            {
                floatingCloudData.Destroy();
            }

            floatingCloudLink.Clear();
        }

        public static void Unload()
        {
            for (int i = 0; i < activeClouds.Count; i++)
            {
                activeClouds[i].Clear();
            }

            activeClouds.Clear();
        }

        public static void RegisterCase(FloatingCloudSettings floatingCloudSettings)
        {
            int cloudHash = floatingCloudSettings.Name.GetHashCode();

            if (floatingCloudLink.ContainsKey(cloudHash))
            {
                Debug.LogError($"Cloud {floatingCloudSettings.Name} already registered!");

                return;
            }

            Data floatingCloudCase = new Data(floatingCloudSettings);
            floatingCloudCase.Init();

            floatingCloudLink.Add(cloudHash, floatingCloudCase);
        }

        public static void RegisterCase(Data floatingCloudCase)
        {
            int cloudHash = floatingCloudCase.Name.GetHashCode();

            if (floatingCloudLink.ContainsKey(cloudHash))
            {
                Debug.LogError($"Cloud {floatingCloudCase.Name} already registered!");

                return;
            }

            floatingCloudCase.Init();

            floatingCloudLink.Add(cloudHash, floatingCloudCase);
        }

        public static void SpawnCurrency(string key, RectTransform rectTransform, RectTransform targetTransform, int elementsAmount, string text, SimpleCallback onCurrencyHittedTarget = null)
        {
            SpawnCurrency(key.GetHashCode(), rectTransform, targetTransform, elementsAmount, text, onCurrencyHittedTarget);
        }

        public static void SpawnCurrency(int hash, RectTransform rectTransform, RectTransform targetTransform, int elementsAmount, string text, SimpleCallback onCurrencyHittedTarget = null)
        {
            if (!floatingCloudLink.ContainsKey(hash))
            {
                Debug.LogError($"Cloud with hash {hash} isn't registered!");

                return;
            }

            Animation animation = new Animation(floatingCloudLink[hash], rectTransform, targetTransform, elementsAmount, onCurrencyHittedTarget);
            animation.PlayAnimation();

            activeClouds.Add(animation);
        }

        public static void OnAnimationFinished(Animation animation)
        {
            activeClouds.Remove(animation);
        }

        [System.Serializable]
        public class Data
        {
            [SerializeField] string name;
            public string Name => name;

            [SerializeField] GameObject prefab;
            public GameObject Prefab => prefab;

            [SerializeField] AudioClip appearAudioClip;
            public AudioClip AppearAudioClip => appearAudioClip;

            [SerializeField] AudioClip collectAudioClip;
            public AudioClip CollectAudioClip => collectAudioClip;

            [Space]
            [SerializeField] float cloudRadius;
            public float CloudRadius => cloudRadius;

            private Pool pool;
            public Pool Pool => pool;

            public Data(FloatingCloudSettings settings)
            {
                name = settings.Name;
                prefab = settings.Prefab;

                cloudRadius = settings.CloudRadius;

                appearAudioClip = settings.AppearAudioClip;
                collectAudioClip = settings.CollectAudioClip;
            }

            public void Init()
            {
                pool = new Pool(prefab, "FloatingCloud_" + name);
            }

            public void Destroy()
            {
                PoolManager.DestroyPool(pool);

                pool = null;
            }
        }

        public class Animation
        {
            private Data floatingCloudData;
            private RectTransform rectTransform;
            private RectTransform targetTransform;
            private int elementsAmount;
            private List<RectTransform> elementsList = new List<RectTransform>();
            private SimpleCallback onCurrencyHittedTarget;

            private Transform fakeTargetTransform;

            private TweenCaseCollection tweenCaseCollection;

            public Animation(Data floatingCloudData, RectTransform rectTransform, RectTransform targetTransform, int elementsAmount, SimpleCallback onCurrencyHittedTarget)
            {
                this.floatingCloudData = floatingCloudData;
                this.rectTransform = rectTransform;
                this.targetTransform = targetTransform;
                this.elementsAmount = elementsAmount;
                this.elementsList = new List<RectTransform>();
                this.onCurrencyHittedTarget = onCurrencyHittedTarget;

                GameObject fakeTargetObject = new GameObject("Fake Target");
                fakeTargetTransform = fakeTargetObject.transform;
                fakeTargetTransform.SetParent(targetTransform.parent);
                fakeTargetTransform.position = targetTransform.position;
                fakeTargetTransform.localScale = targetTransform.localScale;
                fakeTargetTransform.localRotation = targetTransform.localRotation;
            }

            public void PlayAnimation()
            {
                RectTransform targetRectTransform = targetTransform;

                tweenCaseCollection = Tween.BeginTweenCaseCollection();

                // Play appear sound
                if (floatingCloudData.AppearAudioClip != null)
                    AudioController.PlaySound(floatingCloudData.AppearAudioClip);

                float cloudRadius = floatingCloudData.CloudRadius;
                Vector3 centerPoint = rectTransform.position;

                int finishedElementsAmount = 0;

                float defaultPitch = 0.9f;
                bool currencyHittedTarget = false;
                for (int i = 0; i < elementsAmount; i++)
                {
                    TweenClip currencyTweenCase = null;
                    GameObject elementObject = floatingCloudData.Pool.GetPooledObject();

                    RectTransform elementRectTransform = (RectTransform)elementObject.transform;

                    elementRectTransform.SetParent(fakeTargetTransform);

                    elementRectTransform.position = centerPoint;
                    elementRectTransform.localRotation = Quaternion.identity;
                    elementRectTransform.localScale = Vector3.one;

                    elementsList.Add(elementRectTransform);

                    Image elementImage = elementObject.GetComponent<Image>();
                    elementImage.color = Color.white.SetAlpha(0);

                    // 加快扩散动画速度
                    float moveTime = Random.Range(0.3f, 0.4f);

                    // 加快淡入速度
                    elementImage.DOFade(1, 0.1f, unscaledTime: true);
                    elementRectTransform.DOAnchoredPosition(elementRectTransform.anchoredPosition + (Random.insideUnitCircle * cloudRadius), moveTime, unscaledTime: true).SetEasing(DG.Tweening.Ease.OutCubic).OnComplete(delegate
                    {
                        // 减少延迟时间
                        tweenCaseCollection.AddTween(Tween.DelayedCall(0.05f, delegate
                        {
                            // 加快缩放动画
                            tweenCaseCollection.AddTween(elementRectTransform.DOTScale(0.3f, 0.25f, unscaledTime: true).SetEasing(DG.Tweening.Ease.InExpo));
                            // 加快飞向目标动画
                            tweenCaseCollection.AddTween(elementRectTransform.DOLocalMove(Vector3.zero, 0.25f, unscaledTime: true).SetEasing(DG.Tweening.Ease.InSine).OnComplete(delegate
                            {
                                if (!currencyHittedTarget)
                                {
                                    if (onCurrencyHittedTarget != null)
                                        onCurrencyHittedTarget.Invoke();

                                    currencyHittedTarget = true;
                                }

                                bool punchTarget = true;
                                if (currencyTweenCase != null)
                                {
                                    if (currencyTweenCase.State < 0.8f)
                                    {
                                        punchTarget = false;
                                    }
                                    else
                                    {
                                        currencyTweenCase.Kill();
                                    }
                                }

                                if (punchTarget)
                                {
                                    // Play collect sound
                                    if (floatingCloudData.CollectAudioClip != null)
                                        AudioController.PlaySound(floatingCloudData.CollectAudioClip, pitch: defaultPitch);

                                    defaultPitch += 0.01f;

                                    // 加快目标缩放动画
                                    currencyTweenCase = targetRectTransform.DOTScale(1.2f, 0.1f, unscaledTime: true).OnComplete(delegate
                                    {
                                        currencyTweenCase = targetRectTransform.DOTScale(1.0f, 0.05f, unscaledTime: true);

                                        tweenCaseCollection.AddTween(currencyTweenCase);
                                    });

                                    tweenCaseCollection.AddTween(currencyTweenCase);
                                }

                                elementObject.transform.SetParent(floatingCloudData.Pool.ObjectsContainer);
                                elementObject.SetActive(false);

                                finishedElementsAmount++;
                                if (finishedElementsAmount >= elementsAmount)
                                {
                                    FloatingCloud.OnAnimationFinished(this);

                                    GameObject.Destroy(fakeTargetTransform.gameObject);
                                }
                            }));
                        }, unscaledTime: true));
                    });
                }

                Tween.EndTweenCaseCollection();
            }

            public void Clear()
            {
                tweenCaseCollection.Kill();

                if(fakeTargetTransform != null)
                    GameObject.Destroy(fakeTargetTransform.gameObject);
            }
        }
    }
}
