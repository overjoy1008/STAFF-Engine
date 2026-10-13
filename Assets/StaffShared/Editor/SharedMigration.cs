using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using Staff.Characters;using Staff.Subway;using Object=UnityEngine.Object;
namespace Staff.Shared.Editor {
[InitializeOnLoad] public static class SharedMigration {
 public const string Root="Assets/StaffShared",Work="Library/StaffShared";
 public const string Gameplay=Root+"/Prefabs/STAFF Gameplay.prefab",Sky=Root+"/Prefabs/STAFF Sky.prefab",Train=Root+"/Prefabs/STAFF Subway.prefab";
 public static readonly string[] Scenes={"Assets/Scenes/STAFF_Subway_Reference.unity","Assets/Scenes/SampleScene.unity","Assets/Scenes/STAFF_City_Reference.unity"};
 static SharedMigration(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;foreach(var action in new[]{"migrate","verify","citytrain"}){string p=Work+"/"+action+".request";if(!File.Exists(p))continue;File.Delete(p);try{if(action=="migrate")Migrate();else if(action=="citytrain")Staff.City.Editor.MathCityBuilder.UpgradeRailway();else Verify();}catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}break;}}
 static T One<T>(Scene s)where T:Component=>All<T>(s).Single();
 public static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
 static GameObject Group(string name,Scene s,Transform parent=null){var g=new GameObject(name);SceneManager.MoveGameObjectToScene(g,s);if(parent)g.transform.SetParent(parent,false);return g;}
 static Bounds BoundsOf(Component c){var rr=c.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static void SetRef(Object o,string name,Object value){var so=new SerializedObject(o);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 [MenuItem("STAFF/Shared/Install Shared Systems In Existing Scenes")]
 public static void Migrate(){
  if(File.Exists(Gameplay))throw new Exception("Shared prefabs already exist. Use Install In Current Scene; migration is intentionally one-time.");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save your open scene before migration.");
  Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Work+"/Backups");AssetDatabase.Refresh();
  foreach(var path in Scenes)File.Copy(path,Work+"/Backups/"+Path.GetFileName(path),true);
  var source=EditorSceneManager.OpenScene(Scenes[0],OpenSceneMode.Single);
  var env=source.GetRootGameObjects().Single(g=>g.name=="Environment");
  var player=One<AdventurePlayer>(source);var camera=One<AdventureCamera>(source);Vector3 subwaySpawn=player.transform.position;
  var context=Group("Scene bindings",source).AddComponent<GameplaySceneContext>();context.environmentRoot=env.transform;
  var mirror=camera.GetComponent<SubwayPlanarReflection>();context.reflectionFloor=mirror?mirror.floorMaterial:null;context.reflectionHeight=mirror?mirror.height:.004f;
  // Keep the authored models, input, roster and camera features together so internal references survive prefab extraction.
  var root=Group("STAFF Gameplay",source);root.transform.position=subwaySpawn;
  var chars=Group("Characters",source,root.transform);var cameras=Group("Cameras",source,root.transform);
  var oldCharacters=player.transform.parent;player.transform.SetParent(chars.transform,true);camera.transform.SetParent(cameras.transform,true);
  if(oldCharacters&&oldCharacters.childCount==0)Object.DestroyImmediate(oldCharacters.gameObject);
  camera.name="Gameplay Camera";camera.GetComponent<Camera>().cullingMask=-1;camera.GetComponent<Camera>().GetUniversalAdditionalCameraData().SetRenderer(0);
  if(!camera.GetComponent<Skybox>())camera.gameObject.AddComponent<Skybox>();
  var mode=camera.GetComponent<AbstractEnvironment>();mode.environmentRoot=null;mode.additionalEnvironmentRoots=Array.Empty<Transform>();mode.abstractSkybox=null;
  if(mirror){mirror.enabled=false;mirror.floorMaterial=null;}
  if(!player.Animator||!player.Visual)throw new Exception("Player model references missing in source.");
  SetRef(player,"followCamera",camera);SetRef(camera,"target",player.transform);SetRef(camera,"facing",player.Visual);
  var shared=root.AddComponent<SharedGameplay>();shared.player=player;shared.cameraController=camera;
  root.transform.position=Vector3.zero;
  PrefabUtility.SaveAsPrefabAssetAndConnect(root,Gameplay,InteractionMode.AutomatedAction);
  root.transform.position=subwaySpawn;PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);shared.BindScene();
  MakeSky(source);
  MakeTrain(source,env);
  Save(source);
  foreach(var path in Scenes.Skip(1)){var s=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);Install(s);Save(s);}
  AssetDatabase.SaveAssets();Verify();File.WriteAllText(Work+"/migration-complete.txt","Shared sky, gameplay and configurable train installed. Original scene backups in Library/StaffShared/Backups.");
  EditorSceneManager.OpenScene(Scenes[2],OpenSceneMode.Single);
 }
 static void MakeSky(Scene s){
  var sky=One<SubwayTimeOfDay>(s);sky.enabled=false;
  var root=Group("STAFF Sky",s);var lighting=s.GetRootGameObjects().Single(g=>g.name=="Lighting");root.transform.SetParent(lighting.transform,true);
  sky.transform.SetParent(root.transform,true);if(sky.skyFill)sky.skyFill.transform.SetParent(root.transform,true);
  var cloud=All<Volume>(s).Single(v=>v.name=="Nishita Volumetric Clouds");cloud.transform.SetParent(root.transform,true);
  sky.cameras=Array.Empty<Camera>();sky.automaticSceneCameras=true;
  var sun=Group("Solar directional light",s,root.transform).AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.cullingMask=-1;
  var rig=root.AddComponent<SharedSkyRig>();rig.sky=sky;rig.sun=sun;rig.rendererData=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/PC_Renderer.asset");rig.skyVolumes=new[]{cloud};
  root.transform.SetParent(null,true);root.transform.localScale=Vector3.one;
  sky.enabled=true;sky.RestorePreviewBindings();
  PrefabUtility.SaveAsPrefabAssetAndConnect(root,Sky,InteractionMode.AutomatedAction);
  root.transform.SetParent(lighting.transform,true);PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
  sky.ApplySky();
 }
 static void MakeTrain(Scene s,GameObject env){
  var oldTransit=env.GetComponent<SubwayTransit>();var oldDoors=env.GetComponent<SubwayDoors>();
  var cars=env.GetComponentsInChildren<SubwayCar>().OrderBy(c=>BoundsOf(c).center.z).ToArray();
  var bounds=cars.Select(BoundsOf).ToArray();var collision=env.transform.Find("Boarding and car collision");
  var colliders=collision.Cast<Transform>().ToArray();
  var models=new Dictionary<string,GameObject>();
  foreach(string kind in new[]{"Rear","Middle","Front"}){
   int index=Array.FindIndex(cars,c=>c.module==kind);var car=cars[index];var b=bounds[index];var pivot=new Vector3(b.center.x,0,b.center.z);
   var moduleRoot=Group("Subway "+kind,s);moduleRoot.transform.position=pivot;
   var visual=Object.Instantiate(car.gameObject,car.transform.parent);visual.transform.SetParent(moduleRoot.transform,true);visual.name="Train body";
   var module=moduleRoot.AddComponent<SubwayDoorModule>();var gates=new List<BoxCollider>();
   foreach(var t in colliders){
    if(t.name=="Platform edge barrier"||t.name=="Gangway floor bridge")continue;
    var box=t.GetComponent<BoxCollider>();if(!box)continue;float z=box.bounds.center.z;
    int nearest=Enumerable.Range(0,cars.Length).OrderBy(i=>Mathf.Abs(bounds[i].center.z-z)).First();if(nearest!=index)continue;
    var copy=Object.Instantiate(t.gameObject,t.parent);copy.name=t.name;copy.transform.SetParent(moduleRoot.transform,true);
    if(oldDoors.doorwayGates.Contains(box))gates.Add(copy.GetComponent<BoxCollider>());
   }
   module.leaves=oldDoors.leaves.Where(l=>l.transform&&l.transform.IsChildOf(car.transform)).Select(l=>new SubwayDoors.Leaf{transform=visual.transform.Find(AnimationUtility.CalculateTransformPath(l.transform,car.transform)),closedLocalPosition=l.closedLocalPosition,x=l.x,y=l.y,z=l.z}).ToArray();
   if(module.leaves.Any(l=>!l.transform)||module.leaves.Length==0)throw new Exception("Door mapping failed "+kind);
   module.gates=gates.ToArray();module.localBodyBounds=new Bounds(b.center-pivot,b.size);moduleRoot.transform.position=Vector3.zero;
   models[kind]=PrefabUtility.SaveAsPrefabAsset(moduleRoot,Root+"/Prefabs/Subway "+kind+".prefab");Object.DestroyImmediate(moduleRoot);
  }
  var trainParent=cars[0].transform.parent;Object.DestroyImmediate(oldTransit);Object.DestroyImmediate(oldDoors);Object.DestroyImmediate(trainParent.gameObject);Object.DestroyImmediate(collision.gameObject);
  var root=Group("STAFF Subway",s);root.transform.position=Vector3.zero;
  var consist=root.AddComponent<SubwayConsist>();consist.rearCar=models["Rear"];consist.middleCar=models["Middle"];consist.frontCar=models["Front"];consist.carSpacing=bounds[1].center.z-bounds[0].center.z;consist.gangwayMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/StaffSubway/Materials/Wall panels.mat");
  var transit=root.GetComponent<SubwayTransit>();transit.travelDistance=420;transit.travelSeconds=16;
  PrefabUtility.SaveAsPrefabAssetAndConnect(root,Train,InteractionMode.AutomatedAction);
  root.transform.position=new Vector3(bounds[0].center.x,0,bounds[0].center.z);root.transform.SetParent(env.transform,true);PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);consist.Rebuild();
 }
 [MenuItem("STAFF/Shared/Install In Current Scene")]
 public static void InstallCurrent(){Install(SceneManager.GetActiveScene());EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());}
 public static void Install(Scene s){
  if(!All<SharedGameplay>(s).Any()){
   var old=All<AdventurePlayer>(s).FirstOrDefault();Vector3 spawn=old?old.transform.position:new Vector3(0,.2f,-8);Quaternion rot=old?old.transform.rotation:Quaternion.identity;
   foreach(var c in All<Camera>(s)){if(c.GetComponent<AdventureCamera>()){Object.DestroyImmediate(c.gameObject);continue;}c.enabled=false;c.tag="Untagged";foreach(var b in c.GetComponents<Behaviour>())b.enabled=false;}
   if(old){var parent=old.transform.parent;Object.DestroyImmediate(old.gameObject);if(parent&&parent.childCount==0)Object.DestroyImmediate(parent.gameObject);}
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Gameplay);var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,s);g.transform.SetPositionAndRotation(spawn,rot);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
   var context=All<GameplaySceneContext>(s).FirstOrDefault();if(!context)context=Group("Scene bindings",s).AddComponent<GameplaySceneContext>();context.environmentRoot=s.GetRootGameObjects().FirstOrDefault(x=>x.name=="Environment")?.transform;context.additionalEnvironmentRoots=s.GetRootGameObjects().Where(x=>x.name=="Coordinates").Select(x=>x.transform).ToArray();g.GetComponent<SharedGameplay>().BindScene();
  }
  if(!All<SharedSkyRig>(s).Any()){
   var lighting=s.GetRootGameObjects().FirstOrDefault(g=>g.name=="Lighting")??Group("Lighting",s);
   foreach(var l in All<Light>(s).Where(l=>l.type==LightType.Directional)){l.enabled=false;}
   var sky=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Sky),s);sky.transform.SetParent(lighting.transform,true);
   var tod=sky.GetComponentInChildren<SubwayTimeOfDay>();tod.enabled=false;tod.enabled=true;
  }
 }
 static void Save(Scene s){foreach(var sky in All<SubwayTimeOfDay>(s))sky.RestorePreviewBindings();EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed "+s.path);}
 [MenuItem("STAFF/Shared/Open Gameplay Prefab")]
 static void OpenGameplay()=>AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(Gameplay));
 [MenuItem("STAFF/Shared/Open Sky Prefab")]
 static void OpenSky()=>AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(Sky));
 public static void Verify(){
  var lines=new List<string>();
  foreach(var path in Scenes){var s=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var g=One<SharedGameplay>(s);var sky=One<SharedSkyRig>(s);
   if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g.gameObject)!=Gameplay||PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sky.gameObject)!=Sky)throw new Exception("Disconnected shared prefab "+path);
   if(All<Camera>(s).Count(c=>c.enabled&&c.gameObject.activeInHierarchy)!=1)throw new Exception("Expected one active camera "+path);
   if(!g.player.Animator||!g.player.Visual||g.cameraController.Target!=g.player.transform)throw new Exception("Broken gameplay reference "+path);
   if(!sky.sky.atmosphere||!sky.sky.agxLut||!sky.sky.moon.surface||!sky.rendererData)throw new Exception("Sky dependencies missing "+path);
   int missing=s.GetRootGameObjects().Sum(root=>root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));if(missing>0)throw new Exception("Missing scripts "+path);
   lines.Add("PASS "+path+": connected shared gameplay / sky; one camera; model and sky references intact.");
   var train=All<SubwayConsist>(s).FirstOrDefault();if(train){int originalCount=train.CarCount;
    foreach(int n in new[]{2,3,4,5,6,7,8}){train.CarCount=n;var cars=train.Cars;if(cars.Length!=n||cars[0].GetComponentInChildren<SubwayCar>().module!="Rear"||cars[n-1].GetComponentInChildren<SubwayCar>().module!="Front")throw new Exception("Consist count failed "+n);var doors=train.GetComponent<SubwayDoors>();if(doors.leaves.Any(l=>!l.transform)||doors.doorwayGates.Any(c=>!c))throw new Exception("Door references broken");doors.Toggle();doors.Advance(2);if(doors.Progress!=1)throw new Exception("Door open failed");doors.Close();doors.Advance(2);if(doors.Progress!=0)throw new Exception("Door close failed");lines.Add("PASS train "+n+" cars: modules / door curves / collision gates.");}
    train.CarCount=1;if(train.CarCount!=2)throw new Exception("Lower clamp failed");train.CarCount=9;if(train.CarCount!=8)throw new Exception("Upper clamp failed");train.CarCount=originalCount;
   }
  }
  File.WriteAllLines(Work+"/validation.txt",lines);Debug.Log("STAFF SHARED VALIDATION PASS");
 }
}
}
