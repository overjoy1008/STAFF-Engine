using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Staff.Characters;
namespace Staff.Subway.Editor {
 [InitializeOnLoad] public static class SubwayPlayVerification {
  const string Key="Staff.Subway.PlayVerification";
  static SubwayPlayVerification(){EditorApplication.update+=Tick;}
  [MenuItem("STAFF/Subway/Verify Play Mode")]
  static void Run(){if(SceneManager.GetActiveScene().name!="STAFF_Subway_Reference")throw new Exception("Open the subway scene first.");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
  static void Tick(){
   if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.frameCount<15)return;
   SessionState.SetBool(Key,false);var report=new StringBuilder();
   void Check(bool ok,string detail){if(!ok)throw new Exception(detail);report.AppendLine("PASS: "+detail);}
   try{
    var c=Camera.main;var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();var cam=c.GetComponent<AdventureCamera>();
    Check(p&&cam&&cam.Target==p.transform,"SampleScene player and follow camera linked");
    Check(p.Animator&&p.Animator.isHuman&&p.Animator.avatar.isValid,"Valid humanoid avatar and locomotion animator");
    Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.enabled)==1,"One active audio listener");
    var input=new SerializedObject(p).FindProperty("inputActions").objectReferenceValue as UnityEngine.InputSystem.InputActionAsset;
    Check(input&&input.FindAction("Move")!=null&&input.FindAction("Jump")!=null,"Original input actions assigned");
    p.enabled=false; // Deterministic commands exercise the same movement path as Update.
    void Step(AdventurePlayer.Command cmd,int count){for(int i=0;i<count;i++){Physics.SyncTransforms();p.Simulate(cmd,1f/60);p.Animator.Update(1f/60);cam.Simulate(1f/60);}}
    var idle=new AdventurePlayer.Command();Step(idle,60);
    Check(p.Grounded&&Mathf.Abs(p.transform.position.y)<.15f,"Grounded on station platform");
    var start=p.transform.position;Step(new AdventurePlayer.Command{move=Vector2.up},60);
    float run=Vector3.Distance(start,p.transform.position);Check(run>7.1f&&run<8.1f,"Run distance: "+run.ToString("F2")+"m in 1s");
    Step(idle,30);start=p.transform.position;Step(new AdventurePlayer.Command{jump=true},1);float apex=0;
    for(int i=0;i<90;i++){Step(idle,1);apex=Mathf.Max(apex,p.transform.position.y-start.y);}
    Check(apex>1&&apex<1.4f&&p.Grounded,"Jump and landing; apex "+apex.ToString("F2")+"m");
    Step(new AdventurePlayer.Command{move=Vector2.up,sprint=true},1);Check(p.IsDashing&&p.HorizontalSpeed>17,"Original Shift dash starts");Step(idle,60);
    var cc=p.GetComponent<CharacterController>();
    void Place(Vector3 pos){cc.enabled=false;p.transform.position=pos;cc.enabled=true;Physics.SyncTransforms();cam.Snap();Step(idle,30);}
    Place(new Vector3(0,.03f,-4));Step(new AdventurePlayer.Command{move=Vector2.left},60);
    Check(p.transform.position.x>-2.05f&&p.transform.position.x<-1.7f,"Column blocks movement at x="+p.transform.position.x.ToString("F2"));
    Place(new Vector3(0,.03f,0));Step(new AdventurePlayer.Command{move=Vector2.right,sprint=true},90);
    Check(p.transform.position.x<3.2f&&p.transform.position.x>2.8f,"Platform edge blocks movement and dash");
    var switcher=p.GetComponent<CharacterSwitcher>();int old=switcher.Index;Check(switcher.Count>2,"Original character roster loaded: "+switcher.Count);
    switcher.Select(old==1?2:1);Check(p.Animator&&p.Animator.isHuman,"Character switching retains humanoid animator");switcher.Select(old);
    Place(new Vector3(0,.03f,-4));Step(idle,60);cam.Snap();
    var r=c.GetComponent<SubwayPlanarReflection>();Check(r&&r.enabled,"Floor reflection migrated to gameplay camera");
    var rt=new RenderTexture(1600,900,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var img=new Texture2D(1600,900,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1600,900),0,0);img.Apply();File.WriteAllBytes("Library/StaffSubway/gameplay-view.png",img.EncodeToPNG());c.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(img);UnityEngine.Object.DestroyImmediate(rt);
    Check(r.floorMaterial.GetTexture("_ReflectionTex")!=null,"Live station reflection rendered");
   }catch(Exception e){report.AppendLine("FAIL: "+e);Debug.LogException(e);}
   finally{File.WriteAllText("Library/StaffSubway/play-validation.txt",report.ToString());EditorApplication.isPlaying=false;}
  }
 }
}
