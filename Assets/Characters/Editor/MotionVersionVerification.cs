using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Staff.Characters.Editor {
[InitializeOnLoad] public static class MotionVersionVerification {
static MotionVersionVerification(){EditorApplication.update+=Poll;}
public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool("MotionVerify",true);EditorApplication.isPlaying=true;}
static void Poll(){if(!SessionState.GetBool("MotionVerify",false)||!EditorApplication.isPlaying||Time.frameCount<10)return;SessionState.SetBool("MotionVerify",false);Verify();}
static void Verify() {
try {
 var fan=Resources.Load<AnimatorOverrideController>("StaffGenshinMotion");
 Check(fan,"Override loads");
 foreach(var n in new[]{"Stand--Idle","Locomotion--Walk_N","Locomotion--Run_N"}) Check(fan[n] && fan[n].humanMotion && fan[n].name.StartsWith("GenshinFan_"),"Humanoid replacement "+n);
 foreach(var clip in fan.runtimeAnimatorController.animationClips.Where(c=>!new[]{"Stand--Idle","Locomotion--Walk_N","Locomotion--Run_N"}.Contains(c.name))) Check(fan[clip.name]==clip,"Preserved "+clip.name);

 var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();
 p.enabled=false;
 Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
 var v=p.GetComponent<AdventureMotionVersion>();
 typeof(AdventureMotionVersion).GetMethod("Awake",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(v,null);
 v.ApplyTo(p.Animator);Check(p.Animator.runtimeAnimatorController==fan,"Default Genshin");
 p.Animator.Rebind();p.Animator.SetBool("Grounded",true);p.Animator.SetFloat("Speed",8);p.Animator.Play("Locomotion",0,.35f);p.Animator.Update(0);
 var keyboard=InputSystem.AddDevice<Keyboard>();
 void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));typeof(InputSystem).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});}
 Keys(Key.L);Check(!v.HandleInput(false)&&v.UseGenshin,"Uncaptured ignored");p.InputBlocked=true;Check(!v.HandleInput(true),"Picker ignored");p.InputBlocked=false;
 Check(v.HandleInput(true)&&!v.UseGenshin,"L selects Unity");Check(p.Animator.runtimeAnimatorController==fan.runtimeAnimatorController,"Original restored");
 Check(Mathf.Abs(p.Animator.GetFloat("Speed")-8)<.001f,"Speed preserved");
 Keys();Keys(Key.L);v.HandleInput(true);Check(v.UseGenshin,"L selects Genshin");
 foreach(var name in new[]{"Locomotion","Jump","Fall","Dance_bbbb"}) {
 p.Animator.Play(name,0,.3f);p.Animator.Update(0);var before=p.Animator.GetCurrentAnimatorStateInfo(0);
 Keys();Keys(Key.L);v.HandleInput(true);p.Animator.Update(0);var after=p.Animator.GetCurrentAnimatorStateInfo(0);
 Check(before.fullPathHash==after.fullPathHash && Mathf.Abs(before.normalizedTime-after.normalizedTime)<.01f,"State/time retained "+name);
 }
 typeof(AdventurePlayer).GetField("motionVersion",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(p,v);
 var roster=Resources.Load<CharacterRosterCatalog>("StaffCharacterRoster");
 foreach(var prefab in roster.characters) {
 var model=UnityEngine.Object.Instantiate(prefab);var animator=model.GetComponentInChildren<Animator>();
 p.ReplaceAnimator(animator);Check(animator.runtimeAnimatorController==(v.UseGenshin ? fan : fan.runtimeAnimatorController),"Character retains version "+prefab.name);
 Keys();Keys(Key.L);v.HandleInput(true);Check(animator.runtimeAnimatorController==(v.UseGenshin ? fan : fan.runtimeAnimatorController),"Character toggles "+prefab.name);
 animator.SetFloat("Speed",8);animator.Play("Locomotion");animator.Update(.2f);Check(animator.isHuman,"Humanoid evaluates "+prefab.name);
 }
 InputSystem.RemoveDevice(keyboard);
 Debug.Log("MOTION VERSION VERIFICATION PASS");EditorApplication.Exit(0);
} catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
}
static void Check(bool ok,string label){if(!ok)throw new Exception(label);Debug.Log("MOTION PASS: "+label);}
}}
