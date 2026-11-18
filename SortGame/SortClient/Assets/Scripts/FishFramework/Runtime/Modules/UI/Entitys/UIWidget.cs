using UnityEngine;

namespace FishFramework
{
    public abstract class UIWidget : UIView
    {
        protected UIView parentView;
        protected string widgetId => viewId;
        public bool Visible { private set; get; }
        public UIWidgetBehaviour widgetBehaviour => (UIWidgetBehaviour)viewBehaviour;

        protected internal void Create(string _widgetId, UIView _parentView, Transform parentTf)
        {
            parentView = _parentView;
            base.Create(_widgetId, parentTf);
            Open();
        }

        protected internal void Create(string _widgetId, UIView _parentView, Transform parentTf, UIWidgetBehaviour _widgetBehaviour)
        {
            parentView = _parentView;
            base.Create(_widgetId, parentTf, _widgetBehaviour);
            Open();
        }

        protected void DestroySelf()
        {
            parentView.DestroyWidget(widgetId);
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
            gameObject.SetActive(visible);
        }

        protected override void OnInternalCreated()
        {
            Visible = true;
        }

        protected override void OnInternalDestroying()
        {
            Visible = false;
            parentView = null;
            base.OnInternalDestroying();
        }
    }
}