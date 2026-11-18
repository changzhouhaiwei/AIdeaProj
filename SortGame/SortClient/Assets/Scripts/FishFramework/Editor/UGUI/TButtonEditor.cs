using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(TButton))]
public class TButtonEditor : ButtonEditor
{
    private SerializedProperty pressDoTween;
    private SerializedProperty clickDownSc;
    private SerializedProperty clickUpSc;
    private SerializedProperty scaleTarget;
    
    private SerializedProperty audio;

    protected override void OnEnable()
    {
        base.OnEnable();
        pressDoTween = serializedObject.FindProperty("clickDoTween");
        clickDownSc = serializedObject.FindProperty("downScale");
        clickUpSc = serializedObject.FindProperty("upScale");
        scaleTarget = serializedObject.FindProperty("scaleTarget");
        
        audio = serializedObject.FindProperty("audioId");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.PropertyField(pressDoTween);

        TButton button = target as TButton;
        if (button != null && button.clickDoTween)
        {
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(clickDownSc, new GUIContent("Down Scale"));
            EditorGUILayout.PropertyField(clickUpSc, new GUIContent("Up Scale"));
            EditorGUILayout.PropertyField(scaleTarget, new GUIContent("Scale Target",
                "Optional child transform for press scale. Keeps root free for show/hide tweens."));
            EditorGUI.indentLevel = 0;
        }
        
        EditorGUILayout.PropertyField(audio);
        serializedObject.ApplyModifiedProperties();
    }
}
