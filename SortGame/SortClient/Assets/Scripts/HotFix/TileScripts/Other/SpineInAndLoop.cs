using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

public class SpineInAndLoop : MonoBehaviour
{

    [SerializeField] SkeletonAnimation spine;
    [SerializeField] string animIn;
    [SerializeField] string animLoop;


    void PlayAnim()
    {
        // spine.AnimationState.ClearTrack(0); // 清理当前轨道
        // spine.Skeleton.SetToSetupPose();    // 还原为初始姿势
        spine.Update(0);                    // 立即刷新

        spine.AnimationState.SetAnimation(0, animIn, false);
        spine.AnimationState.AddAnimation(0, animLoop, true, 0f);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnEnable()
    {
        PlayAnim();
    }
}
