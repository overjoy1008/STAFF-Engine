using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Staff.Characters.Editor
{
 public static class PreserveCharacterEasterEgg
 {
  public static void RunBatch()
  {
   const string folder="Assets/ThirdParty/StaffCharacterEasterEgg/";
   const string prefabs="Assets/Characters/Prefabs/EasterEgg/";
   Directory.CreateDirectory(folder);Directory.CreateDirectory(prefabs);AssetDatabase.Refresh();
   var manifest=JsonUtility.FromJson<CharacterRosterSetup.Manifest>(File.ReadAllText("Assets/ThirdParty/StaffCharacters/manifest.json"));
   foreach(var entry in manifest.characters)
   {
    string path=folder+entry.slug+".fbx",output=prefabs+entry.slug+".prefab";
    if(AssetDatabase.LoadAssetAtPath<GameObject>(output))continue;
    if(!AssetDatabase.CopyAsset("Assets/ThirdParty/StaffCharacters/"+entry.slug+"/"+entry.slug+".fbx",path))throw new Exception("Copy failed: "+entry.slug);
    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
    var assets=AssetDatabase.LoadAllAssetsAtPath(path);var avatar=assets.OfType<Avatar>().First();var meshes=assets.OfType<Mesh>().ToDictionary(m=>m.name);
    var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Prefabs/Imported/"+entry.slug+".prefab"));
    PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
    instance.name=entry.slug+"_EasterEgg";instance.GetComponent<Animator>().avatar=avatar;
    foreach(var mesh in instance.GetComponentsInChildren<SkinnedMeshRenderer>())mesh.sharedMesh=meshes[mesh.sharedMesh.name];
    PrefabUtility.SaveAsPrefabAsset(instance,output);UnityEngine.Object.DestroyImmediate(instance);
   }
   AssetDatabase.SaveAssets();Debug.Log("EASTER EGG PRESERVED: original backwards / raised-arms avatars");
  }
 }
}
