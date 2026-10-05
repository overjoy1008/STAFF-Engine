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
 var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();p.enabled=false;Camera.main.GetComponent<AdventureCamera>().enabled=false;
 var s=p.GetComponent<CharacterSwitcher>();s.enabled=false;typeof(CharacterSwitcher).GetMethod("OnEnable",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(s,null);
 Check(!ShaderUtil.ShaderHasError(Shader.Find("STAFF/Character Toon")),"Shader compilation");
 Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;var keyboard=InputSystem.AddDevice<Keyboard>();
 void Press(Key key,bool accept=true){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));typeof(InputSystem).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});s.HandleInput(accept);InputSystem.QueueStateEvent(keyboard,new KeyboardState());typeof(InputSystem).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});}
 void CheckState(bool expected){Check(s.ToonEnabled==expected,"Toggle state "+s.CurrentName);foreach(var r in p.Animator.GetComponentsInChildren<Renderer>()){var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);Check(b.GetFloat("_ToonEnabled")== (expected?1:0),"Renderer state "+r.name);}}
 s.Select(0);CheckState(true);Press(Key.H,false);yield return null;CheckState(true);Press(Key.H);yield return null;CheckState(false);
 Check(p.Animator.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).All(m=>m.shader.name!="STAFF/Character Toon"),"Robot original shader restored");
 Press(Key.C);yield return null;Check(s.PickerOpen,"C picker binding");p.GetComponent<CharacterPicker>().Choose(1);Check(s.Index==1,"Picker selection");CheckState(false);Press(Key.F8);yield return null;Check(s.EasterEggMode,"F8 binding");CheckState(false);Press(Key.F8);yield return null;CheckState(false);
 for(int i=0;i<s.Count;i++){s.Select(i);yield return null;if(s.ToonEnabled){Press(Key.H);yield return null;}CheckState(false);var off=Capture(p,s.CurrentName+"-smooth");Press(Key.H);yield return null;CheckState(true);var on=Capture(p,s.CurrentName+"-toon");int changed=off.Where((c,n)=>Math.Abs(c.r-on[n].r)+Math.Abs(c.g-on[n].g)+Math.Abs(c.b-on[n].b)>8).Count();Check(changed>100,"Visible shading difference "+s.CurrentName+" pixels="+changed);Press(Key.H);yield return null;}
 s.Select(0);CheckState(false);Press(Key.H);yield return null;CheckState(true);InputSystem.RemoveDevice(keyboard);
 File.WriteAllText("Library/StaffToon/result.txt","PASS: shader compile; H binding; ignored input; C/F8 state persistence; robot restoration; all 11 characters and robot rendered in both modes.");EditorApplication.Exit(0);
 }
 static Color32[] Capture(AdventurePlayer p,string name){
 var obj=new GameObject("Toon verification camera");var cam=obj.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.16f,.19f);cam.orthographic=true;cam.orthographicSize=1.35f;
 var target=p.transform.position+Vector3.up*1.1f;obj.transform.position=target+p.Visual.forward*4+p.Visual.right*1;obj.transform.LookAt(target);
 var rt=new RenderTexture(480,640,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(480,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,480,640),0,0);image.Apply();File.WriteAllBytes("Library/StaffToon/"+name+".png",image.EncodeToPNG());var pixels=image.GetPixels32();RenderTexture.active=previous;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);return pixels;
 }
}}
