using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.Playables;using UnityEngine.Animations;
namespace Staff.AnimationLab.Editor {
public static class MeshyRetargetDiagnostic {
 const string Out="/Users/gradient/Documents/GitHub/STAFF/3D Studio/Animation Lab/Meshy/RetargetFix";
 const string Control="Assets/ThirdParty/MeshyAnimations/KafkaMeshyControl.fbx";
 [MenuItem("Tools/Animation Lab/Diagnose Meshy Retarget")]
 public static void Run(){
  var imp=(ModelImporter)AssetImporter.GetAtPath(Control);imp.animationType=ModelImporterAnimationType.Generic;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;imp.bakeAxisConversion=true;imp.importAnimation=true;imp.animationCompression=ModelImporterAnimationCompression.Off;imp.SaveAndReimport();
  var takes=imp.defaultClipAnimations;foreach(var c in takes){c.name=c.name.Split('|').Last();c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;c.keepOriginalOrientation=true;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;}imp.clipAnimations=takes;imp.SaveAndReimport();
  Capture("SourceGeneric",AssetDatabase.LoadAssetAtPath<GameObject>(Control),GetClip(Control));
  imp.animationType=ModelImporterAnimationType.Human;imp.SaveAndReimport();
  Capture("SourceHuman",AssetDatabase.LoadAssetAtPath<GameObject>(Control),GetClip(Control));
  var catalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset");var target=catalog.characters.First(c=>c.name=="Kafka_NoCoat");
  Capture("TargetControlAvatar",target,GetClip(Control));
  Capture("TargetCurrent",target,GetClip("Assets/ThirdParty/MeshyAnimations/KafkaLocomotion20.fbx"));
  var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AvatarSetupTool");File.WriteAllLines(Out+"/avatar_methods.txt",type==null?new[]{"missing"}:type.GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Select(m=>m.ToString()));
  File.WriteAllText(Out+"/diagnostic_done.txt","PASS");
 }
 static AnimationClip GetClip(string p)=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().First(c=>c.name=="Running");
 public static void Capture(string name,GameObject prefab,AnimationClip clip,float phase=.47f){
  var scene=EditorSceneManager.NewPreviewScene();PlayableGraph graph=default;GameObject go=null;
  try{
   go=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(go,scene);foreach(var mb in go.GetComponentsInChildren<MonoBehaviour>())mb.enabled=false;
   var anim=go.GetComponentInChildren<Animator>();anim.runtimeAnimatorController=null;anim.applyRootMotion=false;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;anim.Rebind();
   graph=PlayableGraph.Create(name);graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"Motion",anim);output.SetSourcePlayable(p);graph.Play();p.SetTime(clip.length*phase);graph.Evaluate(0);
   var lines=new List<string>();foreach(var t in go.GetComponentsInChildren<Transform>())lines.Add(t.name+","+t.position.ToString("F6")+","+t.rotation.eulerAngles.ToString("F3"));File.WriteAllLines(Out+"/"+name+"-bones.csv",lines);
   if(!name.StartsWith("Target"))foreach(var r in go.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(m=>{if(!m)return m;if(m.shader.name.Contains("Staff")||m.shader.name.Contains("Universal"))return m;var n=new Material(Shader.Find("Universal Render Pipeline/Lit"));if(m.mainTexture)n.SetTexture("_BaseMap",m.mainTexture);n.SetColor("_BaseColor",Color.white);return n;}).ToArray();}
   var bounds=new Bounds(go.transform.position+Vector3.up*.9f,Vector3.zero);foreach(var r in go.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
   var cameraGo=new GameObject("Diagnostic camera");SceneManager.MoveGameObjectToScene(cameraGo,scene);var cam=cameraGo.AddComponent<Camera>();cam.scene=scene;cam.orthographic=true;cam.orthographicSize=Mathf.Max(1.15f,bounds.size.y*.6f);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.13f,.16f,.2f);cam.nearClipPlane=.01f;cam.farClipPlane=30;cam.transform.position=bounds.center+new Vector3(1,0,-4);cam.transform.LookAt(bounds.center);
   foreach(var angle in new[]{new Vector3(35,155,0),new Vector3(20,-40,0)}){var light=new GameObject("Diagnostic light");SceneManager.MoveGameObjectToScene(light,scene);light.transform.rotation=Quaternion.Euler(angle);var l=light.AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.1f;}
   var rt=new RenderTexture(700,800,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(700,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,700,800),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+name+".png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
  }finally{if(graph.IsValid())graph.Destroy();if(go)UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
 }
}
}
