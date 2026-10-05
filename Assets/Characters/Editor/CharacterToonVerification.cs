using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Staff.Characters.Editor {
[InitializeOnLoad] public static class CharacterToonVerification {
 const string Flag="Staff.ToonVerify";
 static CharacterToonVerification(){EditorApplication.update+=Poll;}
 public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;}
 static IEnumerator routine; static int lastFrame=-1;
 static void Poll(){if(!Application.isBatchMode||!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying||Time.frameCount<=5||Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;try{if(routine==null)routine=Verify();routine.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("TOON PASS: "+message);}
 static IEnumerator Verify(){
 Directory.CreateDirectory("Library/StaffToon");
 var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();p.enabled=false;
 Camera.main.GetComponent<AdventureCamera>().enabled=false;var s=p.GetComponent<CharacterSwitcher>();s.enabled=false;typeof(CharacterSwitcher).GetMethod("OnEnable",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(s,null);
 Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;var keyboard=InputSystem.AddDevice<Keyboard>();
 void PressH(){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.H));typeof(InputSystem).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});s.HandleInput(true);InputSystem.QueueStateEvent(keyboard,new KeyboardState());typeof(InputSystem).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});}
 foreach(var name in new[]{"STAFF/Character Toon","HoyoToon/STAFF MMD"})Check(!ShaderUtil.ShaderHasError(Shader.Find(name)),"Shader compilation "+name);
 for(int i=0;i<s.Count;i++){
 s.Select(i);yield return null;
 for(int mode=0;mode<4;mode++){
 Check((int)s.ShaderMode==mode,"H mode cycle "+s.CurrentName+" / "+s.ShaderLabel);
 foreach(var r in p.Animator.GetComponentsInChildren<Renderer>()){
 var props=new MaterialPropertyBlock();r.GetPropertyBlock(props);
 foreach(var m in r.sharedMaterials){Check((m.shader.name=="HoyoToon/STAFF MMD")== (mode<2),"Hoyo material / "+s.CurrentName);if(mode==2)Check(m.shader.name=="STAFF/Character Toon"&&props.GetFloat("_ToonEnabled")==1,"CharacterToon two-tone restored / "+s.CurrentName);if(mode==3)Check(props.GetFloat("_ToonEnabled")==0,"Smooth / "+s.CurrentName);}
 }
 var pixels=Capture(p,s.CurrentName+"-"+s.ShaderMode);
 if(mode<2){foreach(var r in p.Animator.GetComponentsInChildren<Renderer>()){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);Check(block.GetFloat("_StaffTwoTone")== (mode==1?1:0),"HSR preset state / "+s.CurrentName);foreach(var m in r.sharedMaterials)Check(m.GetFloat("_StaffNeutralLighting")==1&&m.GetColor("_PostShadowTint")==Color.white,"Neutral lighting and tint / "+s.CurrentName);}}
PressH();yield return null;
 }
 }
 InputSystem.RemoveDevice(keyboard);File.WriteAllText("Library/StaffToon/result.txt","PASS: 12 characters x 4 shader modes; H key cycles; CharacterToon two-tone restored; shader compilation and captures.");EditorApplication.Exit(0);
 }
 static Color32[] Capture(AdventurePlayer p,string name){
 var obj=new GameObject("Toon verification camera");var cam=obj.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.16f,.19f);cam.orthographic=true;cam.orthographicSize=1.35f;
 var target=p.transform.position+Vector3.up*1.1f;obj.transform.position=target+p.Visual.forward*4+p.Visual.right*1;obj.transform.LookAt(target);
 var rt=new RenderTexture(480,640,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(480,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,480,640),0,0);image.Apply();File.WriteAllBytes("Library/StaffToon/"+name+".png",image.EncodeToPNG());var pixels=image.GetPixels32();RenderTexture.active=previous;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);return pixels;
 }
}}
