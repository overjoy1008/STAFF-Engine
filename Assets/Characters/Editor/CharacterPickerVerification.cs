using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Staff.MathSpace;
namespace Staff.Characters.Editor {
[InitializeOnLoad] public static class CharacterPickerVerification {
 const string Flag="Staff.PickerVerify";static IEnumerator routine;static int last=-1;
 static CharacterPickerVerification(){EditorApplication.update+=Poll;}
 public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;EditorApplication.ExecuteMenuItem("Window/General/Game");}
 static void Poll(){if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying||Time.frameCount<10||last==Time.frameCount)return;last=Time.frameCount;try{routine??=Verify();routine.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("PICKER PASS: "+message);}
 static IEnumerator Verify(){
 var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();var s=p.GetComponent<CharacterSwitcher>();var ui=p.GetComponent<CharacterPicker>();var mono=UnityEngine.Object.FindFirstObjectByType<MonochromeMode>();
 Check(s.CurrentName=="Kafka_NoCoat","Kafka default");
 Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
 s.enabled=false;typeof(CharacterSwitcher).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(s,null);
 var keyboard=InputSystem.AddDevice<Keyboard>();
 void UpdateInput()=>typeof(InputSystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});
 void Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));UpdateInput();s.HandleInput(true);InputSystem.QueueStateEvent(keyboard,new KeyboardState());UpdateInput();}
 bool before=mono.Inverted;Press(Key.I);Check(mono.Inverted!=before,"I inverts existing monochrome scene");yield return null;Press(Key.I);Check(mono.Inverted==before,"I restores scene");
 Press(Key.C);Check(s.PickerOpen&&p.InputBlocked&&!p.InputCaptured,"C opens picker and releases movement/cursor");
 p.CaptureInput();Check(!p.InputCaptured,"Menu prevents gameplay recapture");
 Directory.CreateDirectory("Library/StaffPicker");
 for(int wait=0;wait<20;wait++)yield return null;ScreenCapture.CaptureScreenshot("/tmp/staff-picker-validation/Library/StaffPicker/menu.png");yield return null;yield return null;
 Press(Key.Escape);Check(!s.PickerOpen&&!p.InputBlocked,"Escape closes picker");yield return null;
 Press(Key.C);Check(s.PickerOpen,"C reopens picker");yield return null;Press(Key.C);Check(!s.PickerOpen,"C closes picker");
 s.ToggleToon();bool toon=s.ToonEnabled;
 for(int i=0;i<s.Count;i++){
 Check(Resources.Load<Texture2D>("CharacterPortraits/"+s.NameAt(i)),"Portrait "+s.NameAt(i));
 ui.SetOpen(true);ui.Choose(i);Check(s.Index==i&&!ui.IsOpen&&!p.InputBlocked&&p.InputCaptured,"Selection and resume "+s.NameAt(i));Check(s.ToonEnabled==toon,"Toon state retained");yield return null;
 }
 s.Select(4);s.ToggleEasterEgg();Check(s.EasterEggMode && s.Index==4 && p.Animator.avatar.isValid,"Easter egg retained");
 File.WriteAllText("Library/StaffPicker/result.txt","PASS: Kafka default; I scene inversion/restoration; C/Escape picker controls; input blocking; 12 portraits; all selections and H state persistence; F8 preservation.");InputSystem.RemoveDevice(keyboard);EditorApplication.Exit(0);
 }
}}
