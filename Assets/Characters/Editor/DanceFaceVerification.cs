using System;using System.Collections;using System.IO;using System.Linq;using System.Reflection;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace Staff.Characters.Editor
{
 [InitializeOnLoad] public static class DanceFaceVerification
 {
  const string Flag="Staff.FaceVerify";static IEnumerator routine;static int last=-1,checks,combinations,deformed;
  static DanceFaceVerification(){EditorApplication.update+=Poll;}
  public static void RunBatch(){DanceFaceSetup.BuildBatch();EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;}
  static void Poll(){if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying||Time.frameCount<10||last==Time.frameCount)return;last=Time.frameCount;try{routine??=Run();if(!routine.MoveNext())SessionState.SetBool(Flag,false);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
  static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);checks++;Debug.Log("FACE TEST PASS: "+msg);}
  static IEnumerator Run()
  {
   var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();var d=p.GetComponent<AdventureDance>();var s=p.GetComponent<CharacterSwitcher>();
   p.enabled=false;s.enabled=false;Application.runInBackground=true;
   var faces=Resources.Load<DanceFaceCatalog>("StaffDanceFaces");var dances=Resources.Load<DanceCatalog>("StaffDanceCatalog");
   var late=typeof(AdventureDance).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
   void Settle(){p.Respawn();for(int i=0;i<60;i++){Physics.SyncTransforms();p.Simulate(new AdventurePlayer.Command(),1f/60);p.Animator.Update(1f/60);}}
   for(int character=1;character<s.Count;character++)
   {
    s.Select(character);Settle();var model=faces.characters.First(e=>e.avatar==p.Animator.avatar);var data=JsonUtility.FromJson<DanceFacePlayback.Data>(model.samples.text);
    var renderers=p.Animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
    Check(renderers.Sum(r=>r.sharedMesh.blendShapeCount)>0,"Imported facial geometry / "+s.CurrentName);
    var original=renderers.Select(r=>Enumerable.Range(0,r.sharedMesh.blendShapeCount).Select(r.GetBlendShapeWeight).ToArray()).ToArray();
    for(int dance=0;dance<dances.dances.Length;dance++)
    {
     var entry=dances.dances[dance];var clip=data.clips.First(c=>c.id==entry.id);
     Check(d.StartDance(dance),"Dance starts / "+s.CurrentName+" / "+entry.id);
     Check(d.FacialBindingCount>=clip.tracks.Length,"All source facial tracks bound / "+s.CurrentName+" / "+entry.id);
     if(clip.tracks.Length>0)
     {
      var track=clip.tracks.OrderByDescending(t=>t.weights.Max()-t.weights.Min()).First();int peak=Array.IndexOf(track.weights,track.weights.Max());
      float phase=Mathf.Min(.999f,peak/(entry.clip.length*clip.fps));p.Animator.Play("Base Layer.Dance_"+entry.id,0,phase);p.Animator.Update(0);late.Invoke(d,null);
      float frame=Mathf.Repeat(p.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime,1)*entry.clip.length*clip.fps;int a=Mathf.Min((int)frame,track.weights.Length-1),b=Mathf.Min(a+1,track.weights.Length-1);
      float expected=Mathf.Lerp(track.weights[a],track.weights[b],frame-a);
      bool matched=renderers.Any(r=>Enumerable.Range(0,r.sharedMesh.blendShapeCount).Any(i=>DanceFacePlayback.ShapeName(r.sharedMesh.GetBlendShapeName(i))==track.name&&Mathf.Abs(r.GetBlendShapeWeight(i)-expected)<.02f));
      Check(matched,"Expression follows Animator phase / "+s.CurrentName+" / "+entry.id);
      if(dance==0)
      {
       var r=renderers.First(r=>r.sharedMesh.blendShapeCount>0);var posed=new Mesh();var neutral=new Mesh();r.BakeMesh(posed);
       for(int i=0;i<r.sharedMesh.blendShapeCount;i++)r.SetBlendShapeWeight(i,0);r.BakeMesh(neutral);late.Invoke(d,null);
       var av=posed.vertices;var bv=neutral.vertices;float delta=Enumerable.Range(0,av.Length).Max(i=>(av[i]-bv[i]).sqrMagnitude);
       Check(delta>1e-10f,"Expression deforms rendered vertices / "+s.CurrentName);deformed++;UnityEngine.Object.DestroyImmediate(posed);UnityEngine.Object.DestroyImmediate(neutral);
      }
     }
     // Restart/switch must not leave a smile or closed eye on the next animation.
     d.StopDance();
     Check(renderers.Select((r,ri)=>Enumerable.Range(0,r.sharedMesh.blendShapeCount).All(i=>Mathf.Abs(r.GetBlendShapeWeight(i)-original[ri][i])<.001f)).All(v=>v),"Stop restores original face / "+s.CurrentName+" / "+entry.id);
     combinations++;
    }
   }
   s.Select(8);Settle();d.StartDance(0);p.Animator.Play("Base Layer.Dance_bbbb",0,.5f);p.Animator.Update(0);late.Invoke(d,null);p.Simulate(new AdventurePlayer.Command{move=Vector2.up},1f/60);Check(!d.IsDancing&&d.FacialBindingCount==0,"Movement cancels body and face together");
   Settle();d.StartDance(0);s.Select(4);Check(!d.IsDancing&&d.FacialBindingCount==0,"Character switch clears old face");
   s.Select(0);Settle();Check(d.StartDance(0)&&d.FacialBindingCount==0,"Model without facial shapes dances normally");d.StopDance();
   Directory.CreateDirectory("Library/StaffFace");File.WriteAllText("Library/StaffFace/result.json","{\"pass\":true,\"checks\":"+checks+",\"combinations\":"+combinations+",\"deformedCharacters\":"+deformed+"}");EditorApplication.Exit(0);yield break;
  }
 }
}
