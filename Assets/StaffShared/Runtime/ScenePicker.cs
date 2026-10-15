using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Staff.Characters;
namespace Staff.Shared {
public sealed class ScenePicker:MonoBehaviour {
 public static bool IsOpen {get;private set;}
 bool open,wasCaptured,wasBlocked; AdventurePlayer player; Vector2 scroll; CursorLockMode priorLock; bool priorVisible;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){var go=new GameObject("UI - Scene picker");DontDestroyOnLoad(go);go.AddComponent<ScenePicker>();}
 void Update(){if(!Application.isFocused||Keyboard.current==null||SceneTravel.IsLoading)return;if(Keyboard.current.nKey.wasPressedThisFrame){if(open)Close();else Open();}else if(open&&Keyboard.current.escapeKey.wasPressedThisFrame)Close();}
 void Open(){player=Object.FindFirstObjectByType<AdventurePlayer>();if(player&&player.InputBlocked)return;priorLock=Cursor.lockState;priorVisible=Cursor.visible;wasCaptured=player&&player.InputCaptured;wasBlocked=player&&player.InputBlocked;if(player){player.GetComponent<Staff.Traversal.TangentTraversal>()?.Cancel();player.ReleaseInput();player.InputBlocked=true;}Cursor.lockState=CursorLockMode.None;Cursor.visible=true;IsOpen=open=true;}
 void Close(){IsOpen=open=false;if(player){player.InputBlocked=wasBlocked;if(wasCaptured)player.CaptureInput();}else{Cursor.lockState=priorLock;Cursor.visible=priorVisible;}}
 void OnDestroy(){if(open)Close();}
 void OnGUI(){if(!open)return;float w=Mathf.Min(660,Screen.width-32),h=Mathf.Min(650,Screen.height-32);GUILayout.BeginArea(new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h),GUI.skin.box);GUILayout.Label("Scenes    N / Esc: Close");scroll=GUILayout.BeginScrollView(scroll);
 for(int i=0;i<SceneManager.sceneCountInBuildSettings;i++){string path=SceneUtility.GetScenePathByBuildIndex(i),key=Path.GetFileNameWithoutExtension(path);if(key=="Nishita Comparison")continue;GUILayout.BeginHorizontal(GUI.skin.box);var image=Resources.Load<Texture2D>("SceneThumbnails/"+key);if(image)GUILayout.Label(image,GUILayout.Width(220),GUILayout.Height(124));else GUILayout.Box("Scene preview",GUILayout.Width(220),GUILayout.Height(124));GUILayout.BeginVertical();GUILayout.Space(28);GUILayout.Label(key=="Animation Test"?"Animation Lab":key.Replace("STAFF_", "").Replace("_Reference", "").Replace("_"," "));bool active=SceneManager.GetActiveScene().path==path;GUI.enabled=!active;if(GUILayout.Button(active?"Current scene":"Open",GUILayout.Height(36))){Close();SceneTravel.Load(path);}GUI.enabled=true;GUILayout.EndVertical();GUILayout.EndHorizontal();}
 GUILayout.EndScrollView();GUILayout.EndArea();}
}
}
