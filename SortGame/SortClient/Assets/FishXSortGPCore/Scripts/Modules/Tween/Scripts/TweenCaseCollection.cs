using System.Collections.Generic;
using DG.Tweening;

namespace XSortGameCore
{
    public class TweenCaseCollection
    {
        private List<TweenClip> _list = new List<TweenClip>();
        private int _completedCount;
        private SimpleCallback _onComplete;

        public void AddTween(TweenClip tc)
        {
            _list.Add(tc);
            tc.OnComplete(() =>
            {
                _completedCount++;
                if (_completedCount >= _list.Count) _onComplete?.Invoke();
            });
        }

        public bool IsComplete() => _completedCount >= _list.Count && _list.Count > 0;

        public void Kill()
        {
            foreach (var t in _list) t?.Kill();
        }

        public void OnComplete(SimpleCallback cb) => _onComplete += cb;
    }
}
