using System.Collections;

using UnityEngine;

using FishFramework;



public class Main : MonoBehaviour

{

        [SerializeField] private GameObject bgNode;



        private void Start()

        {

                bgNode.SetActiveEx(true);

                StartCoroutine(Initialize());

        }



        private IEnumerator Initialize()

        {

                yield return new WaitForSeconds(0.5f);

                GPSDKMgr.Instance.HideSplashScreen();


                // xasset 加载场景前必须先初始化 Resource，否则 Scene.LoadAsync 找不到资源
                var gameModule = Object.FindObjectOfType<GameModule>();

                if (gameModule != null)
                {
                        yield return gameModule.StartCoroutine(gameModule.Initialize());
                }

                xasset.Scene.LoadAsync($"Assets/CABRes/Scenes/LoginScene.unity");

        }

}

