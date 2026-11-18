using UnityEditor;

namespace FishFramework
{
    [CustomEditor(typeof(UIWidgetBehaviour))]
    public class UIWidgetBehaviourEditor : UIViewBehaviourEditor 
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawOpElementList();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight / 2);
            DrawExpoertButton();

            serializedObject.ApplyModifiedProperties();
        }
    }
}