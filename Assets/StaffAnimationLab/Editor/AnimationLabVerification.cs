using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Staff.AnimationLab.Editor {
[InitializeOnLoad] public static class AnimationLabVerification {
 static IEnumerator routine;static int last=-1;
 static AnimationLabVerification(){EditorApplication.update+=Poll;}
 public static void RunBatch(){EditorSceneManager.OpenScene(AnimationLabSetup.ScenePath);SessionState.SetBool("Staff.AnimationLab.Verify",true);EditorApplication.isPlaying=true;}
 static void Poll(){if(!SessionState.GetBool("Staff.AnimationLab.Verify",false)||!EditorApplication.isPlaying||Time.frameCount<8||last==Time.frameCount)return;last=Time.frameCount;
 try{routine??=Verify();if(!routine.MoveNext()){SessionState.SetBool("Staff.AnimationLab.Verify",false);EditorApplication.Exit(0);}}
 catch(Exception e){Debug.LogException(e);SessionState.SetBool("Staff.AnimationLab.Verify",false);EditorApplication.Exit(1);}}
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static IEnumerator Verify(){
  var lab=UnityEngine.Object.FindFirstObjectByType<AnimationLabPlayer>();Check(lab&&lab.Animator,"Lab initialized");lab.enabled=false;
  Check(lab.catalog.entries.Count(e=>e.source.StartsWith("Quaternius"))==84,"Expected new animation inventory");
  foreach(var cat in new[]{"Sit","Climb","Swim"})Check(lab.catalog.entries.Any(e=>e.category==cat),"Missing "+cat);
  int combinations=0;
  for(int c=0;c<lab.catalog.characters.Length;c++){
   lab.SelectCharacter(c);Check(lab.Animator.isHuman&&lab.Animator.avatar.isValid,"Invalid character");
   for(int i=0;i<lab.catalog.entries.Length;i++){
    lab.SelectClip(i);Check(lab.Current.clip.humanMotion&&lab.Current.clip.length>0,"Invalid clip");
    lab.Seek(lab.Current.clip.length*.27);var hand=lab.Animator.GetBoneTransform(HumanBodyBones.RightHand);Check(hand,"Missing hand");
    lab.Seek(lab.Current.clip.length*.71);
    foreach(var b in new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftFoot,HumanBodyBones.RightHand}){
     var p=lab.Animator.GetBoneTransform(b).position;Check(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z),"Invalid pose "+lab.Current.title);
     Check(p.magnitude<30,"Exploded pose "+lab.Current.title);
    }
    Check(Math.Abs(lab.TimePosition-lab.Current.clip.length*.71)<.001,"Scrub failed");
    lab.TogglePause();Check(!lab.Playing,"Pause failed");combinations++;
   }
   Debug.Log("LAB CHARACTER PASS: "+lab.catalog.characters[c].name);yield return null;
  }
  lab.SelectCharacter(Array.FindIndex(lab.catalog.characters,c=>c.name=="Nicole"));lab.enabled=true;
  Directory.CreateDirectory("Library/StaffAnimationLab");
  foreach(var clipName in new[]{"Sitting_Idle_Loop","ClimbUp_1m","Swim_Fwd_Loop","Sword_Attack"}){
   lab.SelectClip(Array.FindIndex(lab.catalog.entries,e=>e.clip.name==clipName));lab.Seek(lab.Current.clip.length*.5);lab.TogglePause();
   yield return null;yield return null;
   // Camera capture checks retargeting visually, while ScreenCapture includes the UI.
   var rt=new RenderTexture(1280,900,24);lab.viewCamera.targetTexture=rt;lab.viewCamera.Render();
   var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,900),0,0);tex.Apply();
   File.WriteAllBytes("Library/StaffAnimationLab/"+clipName+".png",tex.EncodeToPNG());RenderTexture.active=old;lab.viewCamera.targetTexture=null;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);
  }
  ScreenCapture.CaptureScreenshot("Library/StaffAnimationLab/interface.png");
  for(int i=0;i<12;i++)yield return null;
  File.WriteAllText("Library/StaffAnimationLab/verification.txt","PASS: "+lab.catalog.entries.Length+" clips / "+lab.catalog.characters.Length+" characters / "+combinations+" sampled combinations. Humanoid rigs, finite poses, scrub, pause and required categories checked.");
  Debug.Log("ANIMATION LAB VERIFICATION PASS: "+combinations+" combinations");
 }
}
}
