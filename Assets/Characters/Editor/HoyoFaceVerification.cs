using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace Staff.Characters.Editor {
 [InitializeOnLoad] public static class HoyoFaceVerification {
 const string Flag="Staff.HoyoFaces";static HoyoFaceVerification(){EditorApplication.update+=Tick;}
 [MenuItem("STAFF/Subway/Capture Hoyo Faces")]
 static void Run(){SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;}
 static void Tick(){
 if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying)return;Application.runInBackground=true;
 if(Time.frameCount<20)return;SessionState.SetBool(Flag,false);
 try{
 var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();var roster=p.GetComponent<CharacterSwitcher>();
 var field=typeof(CharacterSwitcher).GetField("shading",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);if(field.GetValue(roster)==null)field.SetValue(roster,new CharacterToonToggle());
 var obj=new GameObject("Face QA Camera");var cam=obj.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.targetTexture=null;cam.orthographic=true;cam.orthographicSize=.27f;cam.nearClipPlane=.03f;
 string dir="Library/StaffHoyoFaces";Directory.CreateDirectory(dir);
 var rt=new RenderTexture(768,768,24);var old=RenderTexture.active;
 try{
 foreach(var name in new[]{"TheHerta","Citlali","Iuno","Aemeath"}){
  for(int i=1;i<roster.Count;i++)if(roster.NameAt(i)==name){roster.Select(i);break;}
  p.Animator.Update(.1f);var head=p.Animator.GetBoneTransform(HumanBodyBones.Head);
  var target=head.position+Vector3.up*.05f;cam.transform.position=target+p.Visual.forward*1.4f;cam.transform.LookAt(target);
  while(roster.ShaderMode!=CharacterShaderMode.HoyoToon)roster.ToggleToon();
  for(int m=0;m<4;m++){
   cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var tex=new Texture2D(768,768,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();
   File.WriteAllBytes(dir+"/"+name+"-"+roster.ShaderMode+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);roster.ToggleToon();
  }
 }
 if(ShaderUtil.ShaderHasError(Shader.Find("HoyoToon/STAFF MMD")))throw new Exception("Hoyo shader compile error");
 File.WriteAllText(dir+"/result.txt","PASS: four characters / four modes rendered; Hoyo shader compiled.");
 }finally{RenderTexture.active=old;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
 }catch(Exception e){Debug.LogException(e);}finally{Application.runInBackground=false;EditorApplication.isPlaying=false;}
 }
 }
}
