using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace FishFramework
{
    [CustomEditor(typeof(TToggle))]
    [CanEditMultipleObjects]
    public class TToggleEditor : ToggleEditor
    {
        private SerializedProperty m_Audio;
        private SerializedProperty m_AudioCancle;
        private SerializedProperty m_ToggleLabel;
        private SerializedProperty m_NoSelectColor;
        private SerializedProperty m_SelectColor;

        private SerializedProperty m_NoSelectNode;
        private SerializedProperty m_SelectNode;
        private SerializedProperty m_ClickDoTween;
        private SerializedProperty m_DownScale;
        private SerializedProperty m_UpScale;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Audio = serializedObject.FindProperty("AudioIdOne");
            m_AudioCancle = serializedObject.FindProperty("AudioCancle");
            m_ToggleLabel = serializedObject.FindProperty("_ToggleLabel");
            m_NoSelectColor = serializedObject.FindProperty("_NoSelectColor");
            m_SelectColor = serializedObject.FindProperty("_SelectColor");

            m_NoSelectNode = serializedObject.FindProperty("_NoSelectNode");
            m_SelectNode = serializedObject.FindProperty("_SelectNode");
            m_ClickDoTween = serializedObject.FindProperty("clickDoTween");
            m_DownScale = serializedObject.FindProperty("downScale");
            m_UpScale = serializedObject.FindProperty("upScale");
        }

        /// <summary>
        ///   <para>See Editor.OnInspectorGUI.</para>
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Audio);
            EditorGUILayout.PropertyField(m_AudioCancle);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_ClickDoTween);
            if (m_ClickDoTween != null && m_ClickDoTween.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_DownScale, new GUIContent("Down Scale"));
                EditorGUILayout.PropertyField(m_UpScale, new GUIContent("Up Scale"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_NoSelectColor);
            EditorGUILayout.PropertyField(m_SelectColor);
            EditorGUILayout.PropertyField(m_ToggleLabel);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_NoSelectNode);
            EditorGUILayout.PropertyField(m_SelectNode);
            serializedObject.ApplyModifiedProperties();
        }
    }
}