using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace Staff.Shared {
// SharedGameplay calls this once per frame. Scene paths also work in a player build.
public static class SceneTravel {
 public const string City="Assets/Scenes/STAFF_City_Reference.unity",Subway="Assets/Scenes/STAFF_Subway_Reference.unity";
 static bool loading;
 public static bool IsLoading=>loading;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){loading=false;}
 public static string Destination(string current)=>current==City?Subway:City;
 public static void HandleInput(SharedGameplay rig){
  if(!Application.isPlaying||!Application.isFocused||loading||!rig.player||rig.player.InputBlocked||Keyboard.current==null)return;
  if(rig.gameObject.scene!=SceneManager.GetActiveScene())return;
  if(Keyboard.current.f6Key.wasPressedThisFrame)Switch(rig.gameObject.scene.path);
 }
 public static bool Switch(string current){
  if(loading)return false;string path=Destination(current);
  if(!Application.CanStreamedLevelBeLoaded(path)){Debug.LogError("Scene is missing from Build Settings: "+path);return false;}
  loading=true;
  try{var operation=SceneManager.LoadSceneAsync(path,LoadSceneMode.Single);if(operation==null){loading=false;return false;}operation.completed+=_=>loading=false;return true;}
  catch(System.Exception e){loading=false;Debug.LogException(e);return false;}
 }
}
}
