using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

namespace TileGameCore
{
    [CustomEditor(typeof(InitializerInitModule))]
    public class InitializerInitModuleEditor : InitModuleEditor
    {
        public override void OnCreated()
        {
            GameObject canvasPrefab = EditorUtils.GetAsset<GameObject>("Core System Messages Canvas");
            if (canvasPrefab != null)
            {
                serializedObject.Update();
                serializedObject.FindProperty("systemMessagesPrefab").objectReferenceValue = canvasPrefab;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif