using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Staff.City.Editor {
[InitializeOnLoad] public static class CityPlayVerification {
 const string Work="Library/StaffMathCity",Key="STAFF.City.PlayCheck";
 static double deadline;
 static CityPlayVerification(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
 static void Changed(PlayModeStateChange s){
  if(!SessionState.GetBool(Key,false))return;
  if(s==PlayModeStateChange.EnteredPlayMode)deadline=EditorApplication.timeSinceStartup+3;
  if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);File.WriteAllText(Work+"/play-returned.txt","Returned to editable scene after Play mode verification.");}
 }
 static void Tick(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(File.Exists(Work+"/play-request.txt")&&!EditorApplication.isPlayingOrWillChangePlaymode){
   File.Delete(Work+"/play-request.txt");if(SceneManager.GetActiveScene().path!="Assets/Scenes/STAFF_City_Reference.unity"){File.WriteAllText(Work+"/play-validation.txt","FAIL: Open city first.");return;}
   SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;
  }
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||deadline==0||EditorApplication.timeSinceStartup<deadline)return;
  deadline=0;
  try {var c=Camera.main;if(!c||!(c.GetComponent<Staff.Characters.AdventureCamera>()?c.GetComponent<Staff.Characters.AdventureCamera>().enabled:c.GetComponent<CityExploreCamera>()&&c.GetComponent<CityExploreCamera>().enabled))throw new Exception("Navigation camera missing or disabled.");MathCityBuilder.Verify();MathCityBuilder.CaptureSaved();File.WriteAllText(Work+"/play-validation.txt","PASS: Actual Play mode entered; city camera and Input System navigation component active; all materials and scripts valid; three camera renders captured in Play mode.");}
  catch(Exception e){File.WriteAllText(Work+"/play-validation.txt","FAIL: "+e);}
  finally{EditorApplication.isPlaying=false;}
 }
}
}
