using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Staff.Nishita.Editor
{
 // Explicit file request only; never enters Play automatically on an ordinary import.
 [InitializeOnLoad] public static class NishitaPreviewVerification
 {
  const string Folder="Library/StaffNishita/", Key="StaffNishitaPreviewStage";
  static NishitaPreviewVerification(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
  static void Log(string message,string stack,LogType type){if(SessionState.GetInt(Key,0)>0&&(type==LogType.Exception||type==LogType.Error))SessionState.SetBool(Key+"Error",true);}
  static void Tick(){
   if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   int stage=SessionState.GetInt(Key,0);
   if(stage==0){
    if(!File.Exists(Folder+"preview-request.txt"))return;File.Delete(Folder+"preview-request.txt");
    if(EditorApplication.isPlayingOrWillChangePlaymode||EditorSceneManager.GetActiveScene().path!=NishitaSceneBuilder.ScenePath){File.WriteAllText(Folder+"preview-result.txt","SKIPPED: intended scene must be active and stopped.");return;}
    var atmosphere=UnityEngine.Object.FindFirstObjectByType<NishitaAtmosphere>();SessionState.SetFloat(Key+"SolarStart",atmosphere?atmosphere.timeOfDay:0);
    File.Delete(Folder+"GameView.png");File.Delete(Folder+"preview-result.txt");SessionState.SetBool(Key+"Error",false);SessionState.SetBool(Key+"Armed",false);SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"Time",(float)EditorApplication.timeSinceStartup);
    var game=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");if(game!=null)EditorWindow.GetWindow(game).Focus();
    SessionState.SetBool(Key+"Background",Application.runInBackground);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;return;
   }
   if(EditorApplication.isPlaying){Application.runInBackground=true;EditorApplication.isPaused=false;EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();}
   if(stage==1&&EditorApplication.isPlaying&&!SessionState.GetBool(Key+"Armed",false)){
    var a=UnityEngine.Object.FindFirstObjectByType<NishitaAtmosphere>();var c=UnityEngine.Object.FindFirstObjectByType<NishitaComparison>();
    if(a){a.useDayCycle=true;a.animate=true;a.timeOfDay=17.5f;a.Tick(true);SessionState.SetFloat(Key+"SolarStart",a.timeOfDay);}if(c)c.exposure=-2;
    SessionState.SetBool(Key+"Armed",true);SessionState.SetFloat(Key+"Time",(float)EditorApplication.timeSinceStartup);
   }
   double elapsed=EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"Time",0);
   if(stage==1 && EditorApplication.isPlaying && elapsed>5){
    EditorApplication.isPaused=false;
    ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+"GameView.png"));SessionState.SetInt(Key,2);return;
   }
   if(stage==2 && File.Exists(Folder+"GameView.png")){
    var atmosphere=UnityEngine.Object.FindFirstObjectByType<NishitaAtmosphere>();bool advanced=atmosphere&&atmosphere.Ready&&Mathf.Repeat(atmosphere.timeOfDay-SessionState.GetFloat(Key+"SolarStart",0),24)>.001f;
    File.WriteAllText(Folder+"solar-play.txt",atmosphere?"ready="+atmosphere.Ready+"; advanced="+advanced+"; time="+atmosphere.timeOfDay+"; rebuilds="+atmosphere.RebuildCount:"Missing atmosphere");
    if(!advanced)SessionState.SetBool(Key+"Error",true);
    File.WriteAllText(Folder+"preview-result.txt",SessionState.GetBool(Key+"Error",false)?"FAIL: runtime errors occurred.":"PASS: Play Mode and full Game View captured.");SessionState.SetInt(Key,0);Application.runInBackground=SessionState.GetBool(Key+"Background",false);EditorApplication.isPlaying=false;EditorApplication.isPaused=false;return;
   }
   if(elapsed>45){File.WriteAllText(Folder+"preview-result.txt","FAIL: preview timed out.");SessionState.SetInt(Key,0);if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;}
  }
 }
}
