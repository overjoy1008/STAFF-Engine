using System.IO;using System.Linq;using System.Collections;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using Staff.Shared;using Staff.Subway;
namespace Staff.Rendering.Editor {
[InitializeOnLoad] public static class SpawnViewCapture {
 const string Request="Library/ModeDemo/spawn-smooth.request";
 static SpawnViewCapture(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(File.Exists(Request)){File.Delete(Request);SessionState.SetBool("StaffSpawnCapture",true);if(!EditorApplication.isPlaying){EditorApplication.EnterPlaymode();return;}}
 if(!EditorApplication.isPlaying||!SessionState.GetBool("StaffSpawnCapture",false))return;SessionState.SetBool("StaffSpawnCapture",false);SessionState.SetBool("StaffSpawnBackground",Application.runInBackground);Application.runInBackground=true;SceneManager.sceneLoaded+=Loaded;SceneManager.LoadScene(SceneTravel.City);}
 static void Loaded(Scene scene,LoadSceneMode mode){if(scene.path!=SceneTravel.City)return;SceneManager.sceneLoaded-=Loaded;new GameObject("Temporary spawn capture").AddComponent<SpawnViewRecorder>();}
}
[DefaultExecutionOrder(-32000)] public sealed class SpawnViewRecorder:MonoBehaviour {
 AbstractEnvironment recordingMode;bool stepping;float progress;System.Reflection.FieldInfo startField=typeof(AbstractEnvironment).GetField("entryStart",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
 void Update(){if(stepping&&recordingMode)startField.SetValue(recordingMode,Time.unscaledTime-recordingMode.entryDuration*progress);}
 IEnumerator Start(){var rig=FindFirstObjectByType<SharedGameplay>();var mode=rig.cameraController.GetComponent<AbstractEnvironment>();bool blocked=rig.player.InputBlocked;int rate=Time.captureFramerate;Time.captureFramerate=60;rig.player.InputBlocked=true;Directory.CreateDirectory("Library/ModeDemo/spawn-smooth");
 try{for(int i=0;i<48;i++)yield return new WaitForEndOfFrame();mode.SetMode(AbstractEnvironment.EnvironmentMode.Real);yield return new WaitForEndOfFrame();Save("docs/MathCity/City-Street.png");int frame=0;
 foreach(var target in new[]{AbstractEnvironment.EnvironmentMode.Abstract,AbstractEnvironment.EnvironmentMode.Imaginary,AbstractEnvironment.EnvironmentMode.Real}){
 for(int i=0;i<30;i++){yield return new WaitForEndOfFrame();Save("Library/ModeDemo/spawn-smooth/"+(frame++).ToString("D4")+".png");}
 mode.BeginTransition(target,rig.player.transform.position);recordingMode=mode;stepping=true;progress=0;
 for(int i=0;i<76;i++){progress=i/75f;yield return null;yield return new WaitForEndOfFrame();Save("Library/ModeDemo/spawn-smooth/"+(frame++).ToString("D4")+".png");}stepping=false;mode.SetMode(target); }
 File.WriteAllText("Library/ModeDemo/spawn-smooth-result.txt","PASS actual Game view, authored fresh city spawn, frame-stepped runtime I transitions, 60 fps, 318 frames\nCamera "+rig.cameraController.transform.position+" / "+rig.cameraController.transform.eulerAngles);
 }finally{stepping=false;Application.runInBackground=SessionState.GetBool("StaffSpawnBackground",false);Time.captureFramerate=rate;rig.player.InputBlocked=blocked;mode.SetMode(AbstractEnvironment.EnvironmentMode.Real);Destroy(gameObject);}}
 void Save(string path){var tex=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(path,tex.EncodeToPNG());}finally{Destroy(tex);}}
}
}
