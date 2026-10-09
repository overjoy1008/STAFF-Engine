using System;using System.IO;using UnityEngine;using UnityEditor;
namespace Staff.Characters.Editor {
 public static class PmxPhysicsSetup {
  const string DataFolder="Assets/ThirdParty/StaffPmxPhysics/Data";
  [MenuItem("Tools/Characters/Apply Original PMX Physics (Imported Characters)")]
  public static void Apply(){
   var profiles=new System.Collections.Generic.List<string>();
   foreach(var guid in AssetDatabase.FindAssets("t:TextAsset",new[]{DataFolder})){
    var path=AssetDatabase.GUIDToAssetPath(guid);if(Path.GetExtension(path)==".json")profiles.Add(path);
   }
   profiles.Sort(StringComparer.Ordinal);
   if(profiles.Count==0)throw new Exception("No PMX physics profiles in "+DataFolder);
   foreach(var profilePath in profiles){
    string slug=Path.GetFileNameWithoutExtension(profilePath),path="Assets/Characters/Prefabs/Imported/"+slug+".prefab";
    var source=AssetDatabase.LoadAssetAtPath<TextAsset>(profilePath);
    if(!AssetDatabase.LoadAssetAtPath<GameObject>(path))throw new Exception("Missing imported prefab "+slug);
    var go=PrefabUtility.LoadPrefabContents(path);
    try{
     var component=go.GetComponent<PmxPhysics>()??go.AddComponent<PmxPhysics>();
     component.source=source;
     // New components use runtime defaults; retain existing user parameters.
     PrefabUtility.SaveAsPrefabAsset(go,path);
    }finally{PrefabUtility.UnloadPrefabContents(go);}
   }
   AssetDatabase.SaveAssets();Debug.Log("Original PMX physics attached to "+profiles.Count+" imported characters.");
  }
 }
 [InitializeOnLoad] public static class PmxPhysicsDeployment {
  static PmxPhysicsDeployment(){EditorApplication.update+=Poll;}
  static void Poll(){const string folder="Library/StaffPmxPhysics";if(!File.Exists(folder+"/apply-request.txt")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(folder+"/apply-request.txt");try{PmxPhysicsSetup.Apply();File.WriteAllText(folder+"/deploy-result.txt","PASS");}catch(Exception e){File.WriteAllText(folder+"/deploy-error.txt",e.ToString());Debug.LogException(e);}}
 }
}
