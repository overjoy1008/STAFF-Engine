using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Staff.Characters.Editor
{
 [InitializeOnLoad]
 public static class CharacterRosterVerification
 {
  const string Flag="Staff.CharacterRosterVerification";
  static CharacterRosterVerification(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode && SessionState.GetBool(Flag,false)&&EditorApplication.isPlaying&&Time.frameCount>5)Verify();}
  public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Flag,true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("ROSTER TEST PASS: "+message);}
  static void Verify()
  {
   SessionState.SetBool(Flag,false);
   try
   {
    Directory.CreateDirectory("Library/StaffCharacters");
    var player=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();var roster=player.GetComponent<CharacterSwitcher>();var camera=Camera.main.GetComponent<AdventureCamera>();
    player.enabled=false;camera.enabled=false;roster.enabled=true;
    Check(roster.CurrentName=="Kafka_NoCoat","Kafka default");roster.Select(0);
    Check(roster.Count==12,"Robot plus 11 characters");Check(roster.Index==0,"Robot is selectable");
    var keyboard=InputSystem.AddDevice<Keyboard>();
    try {InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));InputSystem.Update();roster.HandleInput(true);Check(roster.PickerOpen,"Real C binding opens picker");player.GetComponent<CharacterPicker>().Choose(1);Check(roster.Index==1,"Picker selects character");InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
      InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F8));InputSystem.Update();roster.HandleInput(true);Check(roster.EasterEggMode,"Real F8 binding activates easter egg");
      InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
      InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F8));InputSystem.Update();roster.HandleInput(true);Check(!roster.EasterEggMode,"Real F8 binding restores normal mode");
      InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();}
    finally{InputSystem.RemoveDevice(keyboard);}
    const float dt=1f/60;
    void Step(AdventurePlayer.Command cmd,int count){for(int j=0;j<count;j++){Physics.SyncTransforms();player.Simulate(cmd,dt);player.Animator.Update(dt);}}
    var output=new List<string>();
    for(int i=1;i<roster.Count;i++)
    {
     player.Respawn();var before=player.transform.position;roster.Select(i);
     Check(player.transform.position==before,"Switch preserves player position: "+roster.CurrentName);
     var animator=player.Animator;Check(animator.isHuman&&animator.avatar.isValid,"Valid avatar: "+roster.CurrentName);
     Step(new AdventurePlayer.Command(),60);
     Check(animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name.Contains("Idle")&&c.weight>.9f),"Idle clip: "+roster.CurrentName);
     var transforms=animator.GetComponentsInChildren<Transform>();var rotations=transforms.Select(t=>t.localRotation).ToArray();Step(new AdventurePlayer.Command(),24);
     Check(transforms.Where((t,n)=>Quaternion.Angle(t.localRotation,rotations[n])>.05f).Any(),"Breathing pose changes: "+roster.CurrentName);
     var head=animator.GetBoneTransform(HumanBodyBones.Head);
     var eye=(animator.GetBoneTransform(HumanBodyBones.LeftEye).position+animator.GetBoneTransform(HumanBodyBones.RightEye).position)*.5f;
     float facing=Vector3.Dot(eye-head.position,player.Visual.forward);
     float leftHand=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position.y-animator.GetBoneTransform(HumanBodyBones.LeftHand).position.y;
     float rightHand=animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position.y-animator.GetBoneTransform(HumanBodyBones.RightHand).position.y;
     Capture(player,roster.CurrentName+"-idle");
     Check(facing>.005f,"Face points forward: "+roster.CurrentName+" "+facing);
     Check(leftHand>.12f&&rightHand>.12f,"Idle arms below shoulders: "+roster.CurrentName+" "+leftHand+" / "+rightHand);
     before=player.transform.position;Step(new AdventurePlayer.Command{move=Vector2.up,walk=true},60);
     float walk=Vector3.Distance(before,player.transform.position);
     Check(walk>1.3f&&walk<2.1f,"Walking displacement: "+roster.CurrentName);
     Check(animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name.Contains("Walk_N")&&c.weight>.8f),"Walk animation: "+roster.CurrentName);
     Capture(player,roster.CurrentName+"-walk");
     before=player.transform.position;Step(new AdventurePlayer.Command{move=Vector2.up},60);
     Check(Vector3.Distance(before,player.transform.position)>7,"Running displacement: "+roster.CurrentName);
     Check(animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name.Contains("Run_N")&&c.weight>.8f),"Run animation: "+roster.CurrentName);
     Capture(player,roster.CurrentName+"-run");
     Step(new AdventurePlayer.Command(),30);float floor=player.transform.position.y;Step(new AdventurePlayer.Command{jump=true},1);float apex=floor;
     for(int j=0;j<100;j++){Step(new AdventurePlayer.Command(),1);apex=Mathf.Max(apex,player.transform.position.y);}
     Check(apex-floor>1&&player.Grounded,"Jump and landing preserved: "+roster.CurrentName);
     output.Add(roster.CurrentName+": Humanoid / breathing / walk / run / jump PASS");
    }
    roster.ToggleEasterEgg();Check(roster.EasterEggMode,"Easter egg activates");
    var oldAnimator=player.Animator;roster.ToggleEasterEgg();Check(!roster.EasterEggMode&&player.Animator!=oldAnimator,"Easter egg restores corrected character");
    roster.Cycle();Check(roster.Index==0&&player.Animator.gameObject.activeInHierarchy,"Cycle returns to silver robot");
    File.WriteAllText("Library/StaffCharacters/verification.txt",string.Join("\n",output)+"\nC key and return to robot PASS");
    Debug.Log("CHARACTER ROSTER VERIFICATION PASS");Finish(0);
   }
   catch(Exception e){Debug.LogException(e);File.WriteAllText("Library/StaffCharacters/verification-error.txt",e.ToString());Finish(1);}
  }
  static void Finish(int code){if(Application.isBatchMode)EditorApplication.Exit(code);else EditorApplication.isPlaying=false;}
  static void Capture(AdventurePlayer player,string name)
  {
   var obj=new GameObject("Character Verification Camera");var camera=obj.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.16f,.19f);camera.fieldOfView=32;
   var target=player.transform.position+Vector3.up*.9f;obj.transform.position=target+player.Visual.forward*3.7f+player.Visual.right*1.2f+Vector3.up*.1f;obj.transform.LookAt(target);
   var rt=new RenderTexture(512,640,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(512,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,512,640),0,0);image.Apply();File.WriteAllBytes("Library/StaffCharacters/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);
  }
 }
}
