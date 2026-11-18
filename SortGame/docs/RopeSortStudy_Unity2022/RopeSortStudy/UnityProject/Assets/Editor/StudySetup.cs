using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class StudySetup {
 public static void SmokeTest(){
  var demo=new GameObject("Smoke").AddComponent<RopeSortDemo>();
  var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
  typeof(RopeSortDemo).GetMethod("Start",flags).Invoke(demo,null);
  foreach(int i in new[]{0,50528,50529,101053,101054,101702}){
   typeof(RopeSortDemo).GetMethod("Load",flags).Invoke(demo,new object[]{i});
   if(typeof(RopeSortDemo).GetField("state",flags).GetValue(demo)==null)throw new System.Exception("Failed to load "+i);
  }
  Object.DestroyImmediate(demo.gameObject);Debug.Log("ROPE_STUDY_RUNTIME_LOAD_OK 6 boundary samples");
 }
 [MenuItem("Rope Sort/Create Study Scene")]
 public static void CreateScene(){
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var camera=new GameObject("Camera").AddComponent<Camera>();camera.backgroundColor=new Color(.08f,.1f,.15f);camera.clearFlags=CameraClearFlags.SolidColor;
  new GameObject("Rope Sort Study").AddComponent<RopeSortDemo>();
  System.IO.Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/Study.unity");
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Study.unity",true)};
  Debug.Log("ROPE_STUDY_SETUP_OK");
 }
}
