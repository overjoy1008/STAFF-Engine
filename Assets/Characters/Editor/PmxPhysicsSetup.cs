using System;using System.IO;using UnityEngine;using UnityEditor;
namespace Staff.Characters.Editor {
 public static class PmxPhysicsSetup {
  [MenuItem("Tools/Characters/Apply Original PMX Physics (3 Characters)")]
  public static void Apply(){foreach(var slug in new[]{"JuFufu","TheHerta","Iuno"}){
   string path="Assets/Characters/Prefabs/Imported/"+slug+".prefab";var go=PrefabUtility.LoadPrefabContents(path);
   try{var component=go.GetComponent<PmxPhysics>()??go.AddComponent<PmxPhysics>();component.source=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ThirdParty/StaffPmxPhysics/Data/"+slug+".json");if(!component.source)throw new Exception("Missing PMX profile "+slug);component.PhysicsEnabled=true;PrefabUtility.SaveAsPrefabAsset(go,path);}finally{PrefabUtility.UnloadPrefabContents(go);}
  }AssetDatabase.SaveAssets();Debug.Log("Original PMX physics attached to JuFufu, TheHerta and Iuno.");}
 }
 [InitializeOnLoad] public static class PmxPhysicsDeployment {
  static PmxPhysicsDeployment(){EditorApplication.update+=Poll;}
  static void Poll(){const string folder="Library/StaffPmxPhysics";if(!File.Exists(folder+"/apply-request.txt")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(folder+"/apply-request.txt");try{PmxPhysicsSetup.Apply();File.WriteAllText(folder+"/deploy-result.txt","PASS");}catch(Exception e){File.WriteAllText(folder+"/deploy-error.txt",e.ToString());Debug.LogException(e);}}
 }
}
