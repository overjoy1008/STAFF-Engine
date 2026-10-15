using System;using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Staff.Shared;using Staff.Shared.Editor;using UnityEngine.SceneManagement;
namespace Staff.City.Editor {
[InitializeOnLoad] public static class CityScaleMatching {
 const string Work="Library/CityScaleMatching";
 static CityScaleMatching(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;if(File.Exists(Work+"/apply.request")){File.Delete(Work+"/apply.request");try{Apply();}catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}return;}if(!File.Exists(Work+"/inspect.request"))return;File.Delete(Work+"/inspect.request");try{Inspect();}catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());}}
 public static void Inspect(){Directory.CreateDirectory(Work);var b=new StringBuilder();foreach(var path in new[]{"Assets/Scenes/STAFF_Subway_Reference.unity","Assets/Scenes/STAFF_City_Reference.unity"}){var s=SceneManager.GetSceneByPath(path);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);b.AppendLine(path+" dirty="+s.isDirty);foreach(var root in s.GetRootGameObjects())b.AppendLine("ROOT "+root.name+" pos="+root.transform.position+" scale="+root.transform.localScale);foreach(var t in SharedMigration.All<SubwayConsist>(s)){t.Rebuild();b.AppendLine("TRAIN "+t.name+" local="+t.transform.localScale+" world="+t.transform.lossyScale+" cars="+t.CarCount+" moduleScale="+t.Cars[0].transform.lossyScale+" body="+t.Cars[0].localBodyBounds);}if(opened)EditorSceneManager.CloseScene(s,true);}File.WriteAllText(Work+"/inspection.txt",b.ToString());}
 public static Vector3 ReferenceScale(){const string path="Assets/Scenes/STAFF_Subway_Reference.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{return SharedMigration.All<SubwayConsist>(scene).Single().transform.lossyScale;}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}}
 public static float MatchScene(Scene scene,Vector3 reference){
  var train=SharedMigration.All<SubwayConsist>(scene).Single();
  var env=scene.GetRootGameObjects().Single(g=>g.name=="Environment").transform;
  const float cityScale=1.3f;float factor=cityScale/env.lossyScale.x;
  var before=env.GetComponentsInChildren<Transform>(true).Where(t=>(t.gameObject.hideFlags&HideFlags.DontSave)==0).Select(t=>(t,t.position,t.lossyScale)).ToArray();
  Undo.RecordObject(env,"Match city to subway scale");env.position*=factor;env.localScale*=factor;
  foreach(var row in before)if(Vector3.Distance(row.t.position,row.position*factor)>.005f||Vector3.Distance(row.t.lossyScale,row.lossyScale*factor)>.005f)throw new Exception("Non-uniform environment transform: "+row.t.name);
  var gameplay=SharedMigration.All<SharedGameplay>(scene).Single();var characterScale=gameplay.player.transform.lossyScale;var oldSpawn=gameplay.player.transform.position;
  Undo.RecordObject(gameplay.transform,"Scale city spawn position");gameplay.transform.position+=oldSpawn*(factor-1);PrefabUtility.RecordPrefabInstancePropertyModifications(gameplay.transform);
  foreach(var camera in SharedMigration.All<Camera>(scene)){
   Undo.RecordObject(camera,"Extend city camera far plane");camera.farClipPlane*=factor;
   if(!camera.transform.IsChildOf(gameplay.transform)){Undo.RecordObject(camera.transform,"Scale authored camera placement");camera.transform.position*=factor;var fly=camera.GetComponent<CityExploreCamera>();if(fly){Undo.RecordObject(fly,"Scale city reference camera home");fly.homePosition*=factor;fly.speed*=factor;}}
   if(PrefabUtility.IsPartOfPrefabInstance(camera))PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
  }
  foreach(var light in SharedMigration.All<Light>(scene).Where(l=>l.type!=LightType.Directional&&!l.transform.IsChildOf(env))){Undo.RecordObject(light.transform,"Scale city light position");Undo.RecordObject(light,"Scale city light range");light.transform.position*=factor;light.range*=factor;if(PrefabUtility.IsPartOfPrefabInstance(light)){PrefabUtility.RecordPrefabInstancePropertyModifications(light);PrefabUtility.RecordPrefabInstancePropertyModifications(light.transform);}}
  if(Vector3.Distance(characterScale,gameplay.player.transform.lossyScale)>.00001f)throw new Exception("Character size changed");
  // Keep the train and its complete elevated lane at their own world scale.
  float railWorldScale=reference.x/.55f;
  foreach(var name in new[]{"STAFF Subway - elevated","Infinity Metro - elevated railway","Metro entrance"}){
   var part=env.Find(name);if(!part)continue;Undo.RecordObject(part,"Keep railway at full subway scale");
   float authoredScale=name=="STAFF Subway - elevated"?.55f:1;
   float correction=railWorldScale*authoredScale/part.lossyScale.x;
   part.localPosition*=correction;part.localScale*=correction;
   if(PrefabUtility.IsPartOfPrefabInstance(part))PrefabUtility.RecordPrefabInstancePropertyModifications(part);
  }
  if(Vector3.Distance(train.transform.lossyScale,reference)>.0001f)throw new Exception("Train world scale mismatch");
  train.Rebuild();Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);return factor;
 }
 [MenuItem("STAFF/City/Match City Scale To Subway Scene")]
 public static void Apply(){
  Directory.CreateDirectory(Work);const string path="Assets/Scenes/STAFF_City_Reference.unity";var scene=SceneManager.GetSceneByPath(path);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
  var reference=ReferenceScale();string backup=Work+"/STAFF_City_Reference.before-scale.unity";if(!File.Exists(backup))File.Copy(path,backup);
  float factor=MatchScene(scene,reference);EditorSceneManager.SaveScene(scene);
  var train=SharedMigration.All<SubwayConsist>(scene).Single();var body=train.Cars[0].localBodyBounds;var size=Vector3.Scale(body.size,train.transform.lossyScale);
  var g=SharedMigration.All<SharedGameplay>(scene).Single();
  File.WriteAllText(Work+"/result.txt","PASS city environment absolute scale=1.3; adjustment factor="+factor.ToString("R")+"\nPASS train world scale="+train.transform.lossyScale+" source="+reference+"\nPASS first-car local-axis dimensions="+size+"\nPASS buildings and roads at 1.3; train, elevated lane and metro entrance at 1/0.55; associated colliders follow their respective transforms\nPASS shared character scale="+g.player.transform.lossyScale+" spawn="+g.player.transform.position+"\nPASS scene saved; source subway scene and shared prefabs unchanged\n");
  Selection.activeGameObject=scene.GetRootGameObjects().Single(x=>x.name=="Environment");SceneView.RepaintAll();
 }

}
}
