using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Staff.Characters;
namespace Staff.AnimationLab.Editor {
public static class AnimationLabSetup {
 const string Root="Assets/StaffAnimationLab/";
 public const string ScenePath=Root+"Scenes/Animation Test.unity";
 [MenuItem("Tools/Animation Lab/Open Test Scene")]
 public static void Open(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);}
 public static void BuildBatch(){Build();EditorApplication.Exit(0);}
 public static void Build(){
  Directory.CreateDirectory(Root+"Scenes");Directory.CreateDirectory(Root+"Materials");AssetDatabase.Refresh();
  var entries=new List<AnimationLabCatalog.Entry>();
  for(int n=1;n<=2;n++) {
   var path=$"Assets/ThirdParty/QuaterniusAnimations/UAL{n}_Standard.fbx";
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
   importer.bakeAxisConversion=true;importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
   importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationCompression=ModelImporterAnimationCompression.Off;
   importer.SaveAndReimport();
   var clips=importer.defaultClipAnimations;
   foreach(var c in clips){c.name=c.name.Split('|').Last();c.loopTime=c.name.EndsWith("_Loop");c.loopPose=c.loopTime;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;c.keepOriginalOrientation=true;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;}
   importer.clipAnimations=clips;importer.SaveAndReimport();
   var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
   if(!avatar||!avatar.isHuman||!avatar.isValid)throw new Exception("Invalid humanoid rig: "+path);
   foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")&&c.name!="A_TPose")) {
    if(!clip.humanMotion)throw new Exception("Not humanoid: "+clip.name);
    entries.Add(new AnimationLabCatalog.Entry{title=Title(clip.name),category="Quaternius",subcategory=Category(clip.name),context=Category(clip.name),source="Quaternius / Universal Library "+(n==1?"One":"Two"),clip=clip});
   }
  }
  foreach(var name in new[]{"idle","walk","jog"}){
   var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/GenshinFanLocomotion/"+name+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
   entries.Add(new AnimationLabCatalog.Entry{title="Genshin · "+Title(name),category="Genshin",source="Hotaru7MMD / fan recreation",clip=clip});
  }
  foreach(var path in AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/Characters/Animation/Humanoid"}).Select(AssetDatabase.GUIDToAssetPath)){
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);entries.Add(new AnimationLabCatalog.Entry{title="Unity · "+Title(clip.name),category="Unity",source="Unity Starter Assets",clip=clip});
  }
  MotionLibrarySetup.AppendImported(entries);
  var roster=Resources.Load<CharacterRosterCatalog>("StaffCharacterRoster");
  var catalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>(Root+"Catalog.asset");
  if(!catalog){catalog=ScriptableObject.CreateInstance<AnimationLabCatalog>();AssetDatabase.CreateAsset(catalog,Root+"Catalog.asset");}
  catalog.entries=entries.OrderBy(e=>e.category).ThenBy(e=>e.title).ToArray();catalog.characters=roster.characters;EditorUtility.SetDirty(catalog);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var env=new GameObject("Environment").transform;var lighting=new GameObject("Lighting").transform;var cameras=new GameObject("Cameras").transform;
  Material Mat(string name,Color color){var p=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.25f);EditorUtility.SetDirty(m);return m;}
  var floor=Mat("Slate",new Color(.09f,.12f,.16f));var rim=Mat("Teal",new Color(.08f,.45f,.44f));var pale=Mat("Pale",new Color(.5f,.6f,.65f));
  GameObject Prop(string name,Vector3 pos,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube){var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(env);go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
  Prop("Studio floor",new Vector3(0,-.12f,0),new Vector3(30,.2f,30),floor);
  Prop("Stage rim",new Vector3(0,-.035f,0),new Vector3(3.5f,.025f,3.5f),rim,PrimitiveType.Cylinder);
  Prop("Stage",new Vector3(0,-.005f,0),new Vector3(3.4f,.005f,3.4f),floor,PrimitiveType.Cylinder);
  var seat=Prop("Seat reference",new Vector3(0,.42f,-.25f),new Vector3(.7f,.08f,.6f),pale);
  var wall=Prop("Ledge reference",new Vector3(0,.5f,.6f),new Vector3(1.5f,1,1),pale);
  var water=Prop("Water reference",new Vector3(0,.55f,0),new Vector3(3,.015f,3),rim);
  // A water plane is a height reference; keep it behind the character to expose the limbs.
  water.transform.position=new Vector3(0,.35f,1.7f);water.transform.localScale=new Vector3(3,.015f,1);
  void Light(string name,Vector3 angles,float intensity,Color color){var go=new GameObject(name);go.transform.SetParent(lighting);go.transform.rotation=Quaternion.Euler(angles);var l=go.AddComponent<UnityEngine.Light>();l.type=LightType.Directional;l.intensity=intensity;l.color=color;l.shadows=LightShadows.Soft;}
  Light("Key",new Vector3(40,-30,0),1.3f,new Color(1,.92f,.85f));Light("Fill",new Vector3(25,140,0),.7f,new Color(.65f,.8f,1));
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.55f,.65f);RenderSettings.fog=false;
  var cameraGO=new GameObject("Main Camera");cameraGO.transform.SetParent(cameras);cameraGO.tag="MainCamera";
  var camera=cameraGO.AddComponent<Camera>();cameraGO.AddComponent<AudioListener>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.075f);camera.fieldOfView=34;camera.nearClipPlane=.03f;
  camera.transform.position=new Vector3(1.5f,1.9f,4.5f);camera.transform.LookAt(Vector3.up);
  var lab=new GameObject("Animation Preview").AddComponent<AnimationLabPlayer>();lab.catalog=catalog;lab.viewCamera=camera;lab.seat=seat;lab.wall=wall;lab.water=water;
  seat.SetActive(false);wall.SetActive(false);water.SetActive(false);
  EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
  File.WriteAllLines(Root+"Inventory.tsv",new[]{"Title\tCategory\tSource\tClip"}.Concat(catalog.entries.Select(e=>$"{e.title}\t{e.category}\t{e.source}\t{e.clip.name}")));
  Debug.Log("ANIMATION LAB BUILT: "+entries.Count+" clips, "+roster.characters.Length+" characters");
 }
 static string Title(string n){if(n=="ClimbUp_1m")return "Ledge climb";return Regex.Replace(n.Replace("_Loop","").Replace("--"," ").Replace('_',' '),@"\d+","").Trim();}
 static string Category(string n){
  if(n.StartsWith("Sitting"))return "Sit";if(n.StartsWith("Swim"))return "Swim";if(n.StartsWith("Climb"))return "Climb";
  if(n.Contains("Sword")||n.Contains("Melee")||n.Contains("Punch")||n.Contains("Shield"))return "Combat";
  if(n.StartsWith("Pistol")||n.StartsWith("Spell")||n.Contains("Throw"))return "Ranged";
  if(n.Contains("Farm")||n.Contains("Tree")||n.Contains("Fixing")||n.Contains("PickUp")||n.Contains("Chest")||n=="Interact"||n=="Consume"||n=="Driving_Loop")return "Interact";
  if(n.StartsWith("Hit")||n.StartsWith("Death"))return "Reaction";
  if(n.Contains("Jump")||n.Contains("Slide")||n=="Roll"||n=="LayToIdle")return "Parkour";
  if(n.Contains("Walk")||n.Contains("Jog")||n.Contains("Sprint")||n.Contains("Crouch")||n.Contains("Push"))return "Move";
  return "Idle / Emote";
 }
}
}
