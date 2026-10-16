using System;using System.IO;using System.Linq;using System.Collections;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using Staff.Shared;using Staff.Subway;using Staff.Traversal;
namespace Staff.Rendering.Editor {
[InitializeOnLoad] public static class TangentActionCapture {
 static TangentActionCapture(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(File.Exists("Library/ModeDemo/actions.request")){File.Delete("Library/ModeDemo/actions.request");SessionState.SetInt("StaffActionCapture",1);if(!EditorApplication.isPlaying){EditorApplication.EnterPlaymode();return;}}if(EditorApplication.isPlaying&&SessionState.GetInt("StaffActionCapture",0)>0){int n=SessionState.GetInt("StaffActionCapture",0);SessionState.SetInt("StaffActionCapture",0);if(n==1)SessionState.SetBool("StaffActionBackground",Application.runInBackground);Application.runInBackground=true;Pending=n;SceneManager.sceneLoaded+=Loaded;SceneManager.LoadScene(SceneTravel.City);}}
 static int Pending;
 static void Loaded(Scene s,LoadSceneMode m){if(s.path!=SceneTravel.City)return;SceneManager.sceneLoaded-=Loaded;var r=new GameObject("Temporary tangent action capture").AddComponent<TangentActionRecorder>();r.cancel=Pending==2;}
}
[DefaultExecutionOrder(150)] public sealed class TangentActionRecorder:MonoBehaviour {
 public bool cancel;TangentTraversal ability;int rate;bool wasEnabled;SharedGameplay rig;
 MethodInfo late=typeof(TangentTraversal).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
 void LateUpdate(){if(ability&&!ability.enabled)late.Invoke(ability,null);}
 IEnumerator Start(){rate=Time.captureFramerate;Time.captureFramerate=60;rig=FindFirstObjectByType<SharedGameplay>();ability=rig.player.GetComponent<TangentTraversal>();wasEnabled=ability.enabled;ability.enabled=false;rig.player.InputBlocked=false;rig.player.CaptureInput();var camera=rig.cameraController.GetComponent<Camera>();camera.GetComponent<AbstractEnvironment>().SetMode(AbstractEnvironment.EnvironmentMode.Real);
 string name=cancel?"tangent-jump-cancel":"tangent-train";string folder="Library/ModeDemo/"+name;Directory.CreateDirectory(folder);int frame=0;
 try{
 for(int i=0;i<45;i++)yield return new WaitForEndOfFrame();var train=FindFirstObjectByType<SubwayConsist>();Vector3 target=Vector3.zero;bool found=false;
 foreach(var c in train.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).OrderBy(c=>Vector3.Distance(c.bounds.center,rig.player.transform.position))){foreach(var p in new[]{c.bounds.center,c.bounds.center+Vector3.up*c.bounds.extents.y*.8f}){var screen=camera.WorldToScreenPoint(p);if(screen.z<=0||screen.x<20||screen.x>Screen.width-20||screen.y<20||screen.y>Screen.height-20)continue;if(!ability.Aim(new Ray(camera.transform.position,p-camera.transform.position)))continue;var anchor=(Collider)typeof(TangentTraversal).GetField("anchor",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ability);if(anchor&&anchor.transform.IsChildOf(train.transform)){target=ability.TargetPoint;found=true;break;}}if(found)break;}
 if(!found)throw new Exception("No visible train target from spawn");
 ability.BeginScreenAim(camera);var projected=camera.WorldToScreenPoint(target);ability.MoveScreenAim(((Vector2)projected-ability.AimScreenPosition)/ability.aimSensitivity);
 if(!ability.HasTarget)throw new Exception("Screen aim failed");
 for(int i=0;i<60;i++){yield return new WaitForEndOfFrame();Save(folder,frame++);}
 if(!ability.EndScreenAim())throw new Exception("Train launch failed");bool jumped=false,arrived=false;float flying=0;float jumpY=0,maxY=0;
 for(int i=0;i<240;i++){
 if(ability.State==TangentTraversal.Phase.Flying)flying+=Time.deltaTime;
 if(cancel&&!jumped&&flying>.45f){jumpY=rig.player.transform.position.y;if(!ability.CancelWithJump())throw new Exception("Jump cancel failed");jumped=true;}
 if(jumped)maxY=Mathf.Max(maxY,rig.player.transform.position.y);
 if(ability.CompletedHops>0)arrived=true;
 yield return new WaitForEndOfFrame();Save(folder,frame++);
 }
 if(cancel&&(!jumped||maxY<jumpY+.3f))throw new Exception("Jump did not gain height");if(!cancel&&!arrived)throw new Exception("Train arrival did not complete; final="+ability.State);
 File.WriteAllText(folder+"-result.txt","PASS "+name+" frames="+frame+" target="+target+" arrived="+arrived+" jumped="+jumped+" rise="+(maxY-jumpY));
 if(!cancel)SessionState.SetInt("StaffActionCapture",2);
 }finally{ability.Cancel();ability.enabled=wasEnabled;Time.captureFramerate=rate;if(cancel)Application.runInBackground=SessionState.GetBool("StaffActionBackground",false);Destroy(gameObject);}}
 void Save(string folder,int n){var tex=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(folder+"/"+n.ToString("D4")+".png",tex.EncodeToPNG());}finally{Destroy(tex);}}
}
}
