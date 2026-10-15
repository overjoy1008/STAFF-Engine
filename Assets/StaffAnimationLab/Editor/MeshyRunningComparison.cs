using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
namespace Staff.AnimationLab.Editor {
public static class MeshyRunningComparison {
 const string Out="/Users/gradient/Documents/GitHub/STAFF/3D Studio/Animation Lab/Meshy/RunningComparison";
 [MenuItem("Tools/Animation Lab/Compare Meshy Running")]
 public static void Run(){
  Directory.CreateDirectory(Out);var lines=new List<string>();
  var catalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset");
  foreach(var character in new[]{"TheHerta","Kafka_NoCoat"})foreach(var source in new[]{"MeshyMotions","KafkaLocomotion20"}){
   var path="Assets/ThirdParty/MeshyAnimations/"+source+".fbx";
   var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name=="Running");
   var imp=(ModelImporter)AssetImporter.GetAtPath(path);
   File.WriteAllText(Out+"/"+source+"-humanoid.json",JsonUtility.ToJson(imp.humanDescription,true));
   var scene=EditorSceneManager.NewPreviewScene();PlayableGraph graph=default;GameObject go=null;
   try{
    go=UnityEngine.Object.Instantiate(catalog.characters.First(c=>c.name==character));SceneManager.MoveGameObjectToScene(go,scene);
    foreach(var behaviour in go.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
    var anim=go.GetComponentInChildren<Animator>();anim.runtimeAnimatorController=null;anim.applyRootMotion=false;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;anim.Rebind();
    graph=PlayableGraph.Create("Running comparison");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"Character",anim);output.SetSourcePlayable(playable);graph.Play();
    var cameraGo=new GameObject("Comparison Camera");SceneManager.MoveGameObjectToScene(cameraGo,scene);var camera=cameraGo.AddComponent<Camera>();camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.19f);camera.orthographic=true;camera.orthographicSize=1.15f;camera.nearClipPlane=.01f;camera.farClipPlane=20;
    foreach(var angle in new[]{new Vector3(35,-25,0),new Vector3(20,140,0)}){var l=new GameObject("Comparison Light");SceneManager.MoveGameObjectToScene(l,scene);l.transform.rotation=Quaternion.Euler(angle);l.AddComponent<Light>().type=LightType.Directional;l.GetComponent<Light>().intensity=1.2f;}
    foreach(var f in new[]{.13f,.47f,.79f}){
     playable.SetTime(clip.length*f);graph.Evaluate(0);
     foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.LeftFoot}){var t=anim.GetBoneTransform(bone);lines.Add(character+","+source+","+f+","+bone+","+t.position.ToString("F5")+","+t.rotation.eulerAngles.ToString("F2"));}
     camera.transform.position=new Vector3(2.3f,1.45f,4);camera.transform.LookAt(new Vector3(0,.9f,0));
     var rt=new RenderTexture(700,800,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
     var tex=new Texture2D(700,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,700,800),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+character+"-"+source+"-"+f.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png",tex.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
    }
    lines.Add("CLIP,"+source+","+clip.length+","+clip.frameRate);
   }finally{if(graph.IsValid())graph.Destroy();if(go)UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
  }
  File.WriteAllLines(Out+"/comparison.csv",lines);File.WriteAllText(Out+"/done.txt","Completed old/new Running comparison on TheHerta and Kafka_NoCoat.");
 }
}
}
