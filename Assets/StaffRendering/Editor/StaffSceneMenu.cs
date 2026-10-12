using UnityEditor;
using UnityEditor.SceneManagement;
public static class StaffSceneMenu {
 [MenuItem("STAFF/Scenes/Nishita Comparison")]
 public static void OpenNishita()=>Open("Assets/StaffNishita/Scenes/Nishita Comparison.unity");
 [MenuItem("STAFF/Scenes/Subway")]
 public static void OpenSubway()=>Open("Assets/Scenes/STAFF_Subway_Reference.unity");
 [MenuItem("STAFF/Scenes/MathSpace")]
 public static void OpenMath()=>Open("Assets/Scenes/SampleScene.unity");
 static void Open(string path){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(path);}
}
