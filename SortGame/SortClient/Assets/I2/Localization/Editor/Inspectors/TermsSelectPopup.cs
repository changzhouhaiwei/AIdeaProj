using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace I2.Loc
{
    public class TermsSelectPopup : PopupWindowContent
    {
        const string SearchControlName = "TermsSelectPopup_Search";
        const float WindowWidth = 320f;
        const float WindowHeight = 400f;
        const float ItemHeight = 18f;

        readonly string[] mAllTerms;
        readonly string mCurrentTerm;
        readonly Action<string> mOnSelected;

        string mSearch = "";
        Vector2 mScroll;
        List<string> mFilteredTerms = new List<string>();
        int mHighlightedIndex;

        public TermsSelectPopup(string[] terms, string currentTerm, Action<string> onSelected)
        {
            mAllTerms = terms;
            mCurrentTerm = currentTerm;
            mOnSelected = onSelected;
            UpdateFilter();
        }

        public static void Show(Rect activatorRect, string[] terms, string currentTerm, Action<string> onSelected)
        {
            PopupWindow.Show(activatorRect, new TermsSelectPopup(terms, currentTerm, onSelected));
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(WindowWidth, WindowHeight);
        }

        public override void OnOpen()
        {
            EditorGUI.FocusTextInControl(SearchControlName);
            ScrollToHighlighted();
        }

        public override void OnGUI(Rect rect)
        {
            HandleKeyboard();

            GUILayout.Space(4);
            GUI.SetNextControlName(SearchControlName);
            EditorGUI.BeginChangeCheck();
            mSearch = GUILayout.TextField(mSearch, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField);
            if (EditorGUI.EndChangeCheck())
                UpdateFilter();

            GUILayout.Space(2);
            mScroll = EditorGUILayout.BeginScrollView(mScroll);
            for (int i = 0; i < mFilteredTerms.Count; i++)
            {
                var term = mFilteredTerms[i];
                var isCurrent = term == mCurrentTerm;
                var isHighlighted = i == mHighlightedIndex;

                if (isCurrent)
                    GUI.contentColor = new Color(0.3f, 0.7f, 1f);
                else if (isHighlighted)
                    GUI.backgroundColor = new Color(0.3f, 0.5f, 0.85f, 0.5f);

                if (GUILayout.Button(term, isHighlighted ? EditorStyles.whiteLabel : EditorStyles.label, GUILayout.Height(ItemHeight)))
                    SelectTerm(term);

                GUI.contentColor = Color.white;
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
        }

        void HandleKeyboard()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown)
                return;

            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    editorWindow.Close();
                    e.Use();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (mHighlightedIndex >= 0 && mHighlightedIndex < mFilteredTerms.Count)
                    {
                        SelectTerm(mFilteredTerms[mHighlightedIndex]);
                        e.Use();
                    }
                    break;
                case KeyCode.UpArrow:
                    mHighlightedIndex = Mathf.Max(0, mHighlightedIndex - 1);
                    ScrollToHighlighted();
                    e.Use();
                    editorWindow.Repaint();
                    break;
                case KeyCode.DownArrow:
                    mHighlightedIndex = Mathf.Min(mFilteredTerms.Count - 1, mHighlightedIndex + 1);
                    ScrollToHighlighted();
                    e.Use();
                    editorWindow.Repaint();
                    break;
            }
        }

        void ScrollToHighlighted()
        {
            if (mHighlightedIndex < 0)
                return;
            float y = mHighlightedIndex * ItemHeight;
            if (y < mScroll.y)
                mScroll.y = y;
            else if (y + ItemHeight > mScroll.y + WindowHeight - 40f)
                mScroll.y = y + ItemHeight - (WindowHeight - 40f);
        }

        void SelectTerm(string term)
        {
            mOnSelected?.Invoke(term);
            editorWindow.Close();
        }

        void UpdateFilter()
        {
            mFilteredTerms.Clear();
            var filter = mSearch.Trim();
            var hasFilter = filter.Length > 0;

            for (int i = 0; i < mAllTerms.Length; i++)
            {
                var term = mAllTerms[i];
                if (string.IsNullOrEmpty(term))
                    continue;

                if (hasFilter && term.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                mFilteredTerms.Add(term);
            }

            mHighlightedIndex = 0;
            for (int i = 0; i < mFilteredTerms.Count; i++)
            {
                if (mFilteredTerms[i] == mCurrentTerm)
                {
                    mHighlightedIndex = i;
                    break;
                }
            }
        }
    }
}
