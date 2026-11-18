using System;
using UnityEngine;
using TileGameCore;

namespace FishTilePlay
{
    public abstract class TileEffect : MonoBehaviour
    {
        [SerializeField] TileEffectType effectType;
        public TileEffectType EffectType => effectType;

        protected TileActor linkedTile;

        public virtual void Init() { }

        public abstract void OnCreated(TileActor tileBehavior);
        public abstract void OnDisabled(TileActor tileBehavior);

        public abstract void Clear();

        public virtual void OnStateChanged(bool state) { }
        public virtual bool BeforeTileSubmitted() { return true; }
        public virtual void OnTileSubmitted() { }
        public virtual void OnAnyTileSubmitted(bool bFromHintItem = false) { }

        public virtual void OnSortingOrderChanged(int order)
        {

        }

        /// <summary>
        /// HighLight 等全局着色时同步效果层（Unknown/GoldenTree 等 overlay）。
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
            if (linkedTile != null)
            {
                linkedTile.DisableEffect();
            }
        }

        public void ApplyEffect(TileActor tileBehavior)
        {
            GameObject effectObject = Instantiate(gameObject);
            effectObject.transform.SetParent(tileBehavior.VisualsTransform);
            effectObject.transform.ResetLocal();

            TileEffect tileEffect = effectObject.GetComponent<TileEffect>();
            tileEffect.linkedTile = tileBehavior;

            tileBehavior.ApplyEffect(tileEffect);
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
