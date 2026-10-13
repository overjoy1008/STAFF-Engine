using System.Linq;using UnityEngine;using UnityEditor;
namespace Staff.Shared.Editor {
[CustomEditor(typeof(SharedGameplay))] public sealed class SharedGameplayEditor:UnityEditor.Editor {
 public override void OnInspectorGUI(){EditorGUILayout.HelpBox("Edit STAFF Gameplay.prefab to update every scene. Scene instance overrides remain local. Use Overrides > Apply for intentional shared changes; keep spawn transforms local.",MessageType.Info);DrawDefaultInspector();if(GUILayout.Button("Open shared gameplay source"))AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(SharedMigration.Gameplay));}
}
[CustomEditor(typeof(SubwayConsist))] public sealed class SubwayConsistEditor:UnityEditor.Editor {
 public override void OnInspectorGUI(){EditorGUILayout.HelpBox("2–8 cars: Rear + Middle × (count − 2) + Front. Door curves, gates, gangways and movement bounds rebuild together. Runtime: set CarCount.",MessageType.Info);DrawDefaultInspector();if(GUILayout.Button("Rebuild consist"))((SubwayConsist)target).Rebuild();}
}
public sealed class SharedModuleRefresh:AssetPostprocessor {
 static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] old){if(!imported.Any(p=>p.StartsWith("Assets/StaffShared/Prefabs/Subway ")&&p.EndsWith(".prefab")))return;EditorApplication.delayCall+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode)return;foreach(var train in Object.FindObjectsByType<SubwayConsist>(FindObjectsSortMode.None))train.Rebuild();};}
}
}
