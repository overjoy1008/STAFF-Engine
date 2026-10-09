using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Staff.Characters.Editor {
 [InitializeOnLoad] public static class ExpressionVerification {
  const string Flag="Staff.ExpressionVerify";static IEnumerator routine;static int last=-1,checks,combinations;
  static ExpressionVerification(){EditorApplication.update+=Poll;}
  public static void RunBatch(){ExpressionSetup.BuildBatch();EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;}
  static void Poll(){if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying||Time.frameCount<10||last==Time.frameCount)return;last=Time.frameCount;try{routine??=Run();if(!routine.MoveNext())SessionState.SetBool(Flag,false);}catch(Exception e){Directory.CreateDirectory("Library/StaffExpressions");File.WriteAllText("Library/StaffExpressions/error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
  static void Check(bool value,string why){if(!value)throw new Exception(why);checks++;}
  static IEnumerator Run(){
   var player=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();var sw=player.GetComponent<CharacterSwitcher>();var e=player.GetComponent<AdventureExpressions>();Check(e,"Automatically attached on scene load");player.enabled=false;sw.enabled=false;Application.runInBackground=true;
   var catalog=Resources.Load<ExpressionCatalog>("StaffExpressionCatalog");var data=JsonUtility.FromJson<AdventureExpressions.Data>(catalog.json.text);
   Directory.CreateDirectory("Library/StaffExpressions");
   for(int ci=1;ci<sw.Count;ci++){
    sw.Select(ci);e.RefreshModel();var entry=Array.Find(catalog.characters,c=>c.avatar==player.Animator.avatar);var c=Array.Find(data.characters,c=>c.character==entry.id);Check(e.Count==23,c.character+" presets");Check(e.BindingCount>0&&e.MissingShapes.Length==0,c.character+" all shapes bind");Check(e.OverlayCount>0,c.character+" blush mesh");
    var renderers=player.Animator.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>!r.name.Contains("Expression cheek")).ToArray();
    for(int pi=0;pi<c.presets.Length;pi++){
     Check(e.Select(pi),"Select "+c.character+" / "+pi);e.Tick(e.TransitionSeconds+1);
     var p=c.presets[pi];foreach(var w in p.weights)Check(renderers.Any(r=>Enumerable.Range(0,r.sharedMesh.blendShapeCount).Any(i=>DanceFacePlayback.ShapeName(r.sharedMesh.GetBlendShapeName(i))==w.name&&Mathf.Abs(r.GetBlendShapeWeight(i)-w.weight)<.001f)),c.character+" "+p.id+" "+w.name);
     Check(Mathf.Abs(e.Blush-p.blush/100)<.001f,"Blush strength "+p.id);combinations++;
    }
    int smile=Array.FindIndex(c.presets,p=>p.id=="smile"),closed=Array.FindIndex(c.presets,p=>p.id=="smile-closed"),shy=Array.FindIndex(c.presets,p=>p.id=="shy");
    var r=renderers.First(x=>Enumerable.Range(0,x.sharedMesh.blendShapeCount).Any(i=>DanceFacePlayback.ShapeName(x.sharedMesh.GetBlendShapeName(i))=="笑い"));int eye=Enumerable.Range(0,r.sharedMesh.blendShapeCount).First(i=>DanceFacePlayback.ShapeName(r.sharedMesh.GetBlendShapeName(i))=="笑い");
    e.Select(0);e.Tick(2);var neutral=new Mesh();r.BakeMesh(neutral);e.Select(closed);e.Tick(e.TransitionSeconds/2);float mid=r.GetBlendShapeWeight(eye);Check(mid>0&&mid<100,"Intermediate eye weight");e.Select(smile);e.Tick(0);Check(Mathf.Abs(r.GetBlendShapeWeight(eye)-mid)<.001f,"Interrupted transition continuity");e.Tick(2);Check(Mathf.Abs(r.GetBlendShapeWeight(eye)-10)<.001f,"Latest transition target");e.Select(closed);e.Tick(2);var posed=new Mesh();r.BakeMesh(posed);Check(neutral.vertices.Zip(posed.vertices,(a,b)=>(a-b).sqrMagnitude).Max()>1e-10f,"Real mesh deforms "+c.character);UnityEngine.Object.DestroyImmediate(neutral);UnityEngine.Object.DestroyImmediate(posed);
    e.Select(shy);e.Tick(2);Capture(player,c.character);e.Release();Check(!e.Manual&&e.Blush==0,"Automatic face restore");yield return null;
   }
   sw.Select(1);e.RefreshModel();e.Select(0);e.Tick(2);
   InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
   void UpdateInput()=>typeof(InputSystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});
   void Press(Key key,bool accept=true){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));UpdateInput();e.HandleInput(accept);InputSystem.QueueStateEvent(keyboard,new KeyboardState());UpdateInput();}
   Press(Key.LeftArrow);Check(e.Index==22,"Left key wraps, actual="+e.Index);Press(Key.RightArrow);Check(e.Index==0,"Right key wraps");Press(Key.RightArrow);Check(e.Index==1,"Right key selects smile");Press(Key.RightArrow,false);Check(e.Index==1,"UI input suppression");Press(Key.Home);Check(e.Index==0,"Home selects neutral");Press(Key.F7);Check(!e.Manual,"F7 returns to dance face");InputSystem.RemoveDevice(keyboard);
   // Body dance keeps running while manual expressions win after its face writer.
   player.Respawn();for(int i=0;i<90;i++){Physics.SyncTransforms();player.Simulate(new AdventurePlayer.Command(),1f/60);player.Animator.Update(1f/60);}
   var dance=player.GetComponent<AdventureDance>();Check(dance.StartDance(0),"Dance starts");e.Select(1);player.Animator.Update(.1f);typeof(AdventureDance).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(dance,null);e.Tick(2);Check(dance.IsDancing&&e.Manual,"Body dance and manual face coexist");e.Release();Check(!e.Manual&&dance.IsDancing,"Automatic return preserves dance");dance.StopDance();
   sw.Select(0);e.RefreshModel();Check(e.Count==0&&!e.Select(1),"Robot gracefully unsupported");
   Check(!ShaderUtil.ShaderHasError(catalog.blushMaterial.shader),"Shader compiles");
   File.WriteAllText("Library/StaffExpressions/result.json","{\"pass\":true,\"checks\":"+checks+",\"characters\":11,\"combinations\":"+combinations+",\"realKeyboard\":true,\"transitions\":true,\"blush\":true,\"danceCoexistence\":true}");EditorApplication.Exit(0);yield break;
  }
  static void Capture(AdventurePlayer p,string name){var head=p.Animator.GetBoneTransform(HumanBodyBones.Head);var obj=new GameObject("Expression verification camera");var cam=obj.AddComponent<Camera>();cam.transform.position=head.position+p.Animator.transform.forward*.85f+Vector3.up*.08f;cam.transform.LookAt(head.position+Vector3.up*.06f);cam.fieldOfView=32;cam.nearClipPlane=.01f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.11f,.16f);var rt=new RenderTexture(512,512,24);cam.targetTexture=rt;cam.Render();var before=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(512,512,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();File.WriteAllBytes("Library/StaffExpressions/"+name+".png",image.EncodeToPNG());RenderTexture.active=before;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
 }
}
