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
 foreach(var name in new[]{"STAFF/Character Toon","HoyoToon/Genshin/Character","HoyoToon/Star Rail/Character","HoyoToon/Zenless Zone Zero/Character","HoyoToon/Wuthering Waves/Character"})Check(!ShaderUtil.ShaderHasError(Shader.Find(name)),"Shader compilation "+name);
 for(int i=0;i<s.Count;i++){
 s.Select(i);var expressions=p.GetComponent<AdventureExpressions>();if(expressions){expressions.RefreshModel();expressions.Release();}yield return null;
 var overlays=p.Animator.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Expression cheek blush").ToDictionary(r=>r,r=>r.sharedMaterials);
 if(i>0)Check(overlays.Count>0,"Expression overlays created before shader switch / "+s.CurrentName);
 for(int mode=0;mode<CharacterToonToggle.ModeCount;mode++){
 Check((int)s.ShaderMode==mode,"H mode cycle "+s.CurrentName+" / "+s.ShaderLabel);
 foreach(var r in p.Animator.GetComponentsInChildren<Renderer>()){
 if(overlays.TryGetValue(r,out var originalOverlay)){Check(r.sharedMaterials.SequenceEqual(originalOverlay)&&r.sharedMaterials.All(m=>m.shader.name=="STAFF/ExpressionCheekBlush"),"Expression shader and material preserved / "+s.CurrentName+" / "+s.ShaderMode);continue;}
 var props=new MaterialPropertyBlock();r.GetPropertyBlock(props);
 foreach(var m in r.sharedMaterials){Check(m.shader.name==CharacterToonToggle.ShaderName(s.ShaderMode),"Actual shader / "+s.CurrentName+" / "+s.ShaderMode);Check(props.GetFloat("_ToonEnabled")== (mode==1?1:0),"CharacterToon band state / "+s.CurrentName);Check(props.GetFloat("_StaffTwoTone")==0&&props.GetFloat("_StaffNeutralLighting")== (mode<2?0:1)&&Mathf.Approximately(props.GetFloat("_StaffExposure"),mode<2?1:.8f),"Legacy brightness/neutral light without Hoyo two-tone / "+s.CurrentName);}

 }
 if(i>0 && mode<2){
 var frame=p.Animator.GetComponent<CharacterFaceLighting>();Check(frame!=null,"Face frame exists / "+s.CurrentName);frame.Refresh();int faces=0;
 foreach(var r in p.Animator.GetComponentsInChildren<Renderer>())for(int slot=0;slot<r.sharedMaterials.Length;slot++){
 var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,slot);if(block.GetFloat("_StaffFaceEnabled")<.5f)continue;faces++;
 var center=(Vector3)block.GetVector("_StaffFaceCenter");var radius=block.GetVector("_StaffFaceRadius");
 Check(Vector3.Distance(center,p.Animator.GetBoneTransform(HumanBodyBones.Head).position)<.4f && radius.x>.02f && radius.x<.3f && radius.y>.02f && radius.y<.3f,"Face frame scale / "+s.CurrentName);
 Check(block.GetFloat("_StaffTwoTone")==0&&block.GetFloat("_ToonEnabled")== (mode==1?1:0),"Face slot mode / "+s.CurrentName);
 }Check(faces>0,"Face material bound / "+s.CurrentName);
 }
 var pixels=Capture(p,s.CurrentName+"-"+s.ShaderMode);
 Check(pixels.Any(c=>c.r>80||c.g>80||c.b>80),"Rendered visible pixels / "+s.CurrentName+" / "+s.ShaderMode);
 Check(pixels.Count(c=>c.r>220&&c.b>220&&c.g<40)<pixels.Length/100,"No error shader pixels / "+s.CurrentName+" / "+s.ShaderMode);
 var activeShader=Shader.Find(CharacterToonToggle.ShaderName(s.ShaderMode));Check(!ShaderUtil.ShaderHasError(activeShader),"Rendered shader compilation / "+s.ShaderMode);

PressH();yield return null;
 }
 }
 InputSystem.RemoveDevice(keyboard);File.WriteAllText("Library/StaffToon/result.txt","PASS: roster x 6 shader modes; actual H input cycles in the specified order; game-specific shader identity; legacy brightness/neutral lighting without Hoyo two-tone; compilation and captures; expression overlay identity preserved.");EditorApplication.Exit(0);
 }
 static Color32[] Capture(AdventurePlayer p,string name){
 var obj=new GameObject("Toon verification camera");var cam=obj.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.16f,.19f);cam.orthographic=true;cam.orthographicSize=1.35f;
 var target=p.transform.position+Vector3.up*1.1f;obj.transform.position=target+p.Visual.forward*4+p.Visual.right*1;obj.transform.LookAt(target);
 var rt=new RenderTexture(480,640,24);cam.targetTexture=rt;Staff.Rendering.StaffCameraCapture.Render(cam);var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(480,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,480,640),0,0);image.Apply();File.WriteAllBytes("Library/StaffToon/"+name+".png",image.EncodeToPNG());var pixels=image.GetPixels32();RenderTexture.active=previous;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);return pixels;
 }
}}
