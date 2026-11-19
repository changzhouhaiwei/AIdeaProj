using System;
using UnityEngine;
using XSortGameCore;

namespace FishXSortPlay
{
    public abstract class XSortEffect : MonoBehaviour
    {
        [SerializeField] XSortEffectType effectType;
        public XSortEffectType EffectType => effectType;

        protected XSortActor linkedXSort;

        public virtual void Init() { }

        public abstract void OnCreated(XSortActor XSortBehavior);
        public abstract void OnDisabled(XSortActor XSortBehavior);

        public abstract void Clear();

        public virtual void OnStateChanged(bool state) { }
        public virtual bool BeforeXSortSubmitted() { return true; }
        public virtual void OnXSortSubmitted() { }
        public virtual void OnAnyXSortSubmitted(bool bFromHintItem = false) { }

        public virtual void OnSortingOrderChanged(int order)
        {

        }

        /// <summary>
        /// HighLight 等全局着色时同步效果层（Unknown 等 overlay）。
        /// </summary>
        public virtual void ApplyTintColor(Color color)
        {
        }

        public virtual bool IsClickAllowed()
        {
            return true;
        }

        public virtual bool IsShuffleAllowed()
        {
            return true;
        }

        public virtual bool IsSubmitClearEffect()
        {
            return false;
        }

        public void DisableEffect()
        {
            if (linkedXSort != null)
            {
                linkedXSort.DisableEffect();
            }
        }

        public void ApplyEffect(XSortActor XSortBehavior)
        {
            GameObject effectObject = Instantiate(gameObject);
            effectObject.transform.SetParent(XSortBehavior.VisualsTransform);
            effectObject.transform.ResetLocal();

            XSortEffect XSortEffect = effectObject.GetComponent<XSortEffect>();
            XSortEffect.linkedXSort = XSortBehavior;

            XSortBehavior.ApplyEffect(XSortEffect);
        }

        public virtual void Unload()
        {

        }

        /// <summary>
        /// 获取效果的序列化数据，用于保存状态
        /// </summary>
        /// <returns>效果的JSON数据</returns>
        public virtual string GetEffectData()
        {
            return "";
        }

        /// <summary>
        /// 从序列化数据恢复效果状态
        /// </summary>
        /// <param name="data">效果的JSON数据</param>
        public virtual void SetEffectData(string data)
        {
            // 默认实现为空，子类可以重写
        }
    }
}
