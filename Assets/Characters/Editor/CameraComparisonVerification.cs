using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Cinemachine;
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Staff.Characters.Editor
{
    [InitializeOnLoad] public static class CameraComparisonVerification
    {
        const string Flag="Staff.CameraCompareVerify";
        static IEnumerator routine; static int last=-1, checks;
        static CameraComparisonVerification(){EditorApplication.update+=Poll;}
        public static void RunBatch()
        {EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); SessionState.SetBool(Flag,true); EditorApplication.isPlaying=true;}
        static void Poll()
        {
            if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying||Time.frameCount<10||last==Time.frameCount)return;
            last=Time.frameCount;
            try {routine??=Verify(); if(!routine.MoveNext())SessionState.SetBool(Flag,false);}
            catch(Exception e){Debug.LogException(e);Directory.CreateDirectory("Library/StaffCamera");File.WriteAllText("Library/StaffCamera/error.txt",e.ToString());EditorApplication.Exit(1);}
        }
        static void Check(bool condition,string message)
        {if(!condition)throw new Exception(message);checks++;Debug.Log("CAMERA TEST PASS: "+message);}
        static void Capture(Camera camera,string filename)
        {
            Directory.CreateDirectory("Library/StaffCamera");
            var target=new RenderTexture(1280,720,24);var old=camera.targetTexture;var active=RenderTexture.active;
            try {camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Library/StaffCamera/"+filename,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
            finally{camera.targetTexture=old;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
        static IEnumerator Verify()
        {
            Application.runInBackground=true;
            var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();
            var cam=Camera.main; var c=cam.GetComponent<AdventureCamera>();var d=p.GetComponent<AdventureDance>();
            p.enabled=false;var switcher=p.GetComponent<CharacterSwitcher>();switcher.enabled=false;
            p.Respawn();for(int i=0;i<60;i++){p.Simulate(new AdventurePlayer.Command(),1f/60);yield return null;}
            Check(c.Version==AdventureCamera.CameraVersion.Wafflus,"Cinemachine starts by default");
            c.SetVersion(AdventureCamera.CameraVersion.Current);
            var shortcut=(InputAction)typeof(AdventureCamera).GetField("toggleVersion",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);
            Check(shortcut.enabled&&shortcut.bindings[0].path=="<Keyboard>/f6","F6 shortcut enabled");
            c.AddLook(new Vector2(100,40),Vector2.zero,120,false,1f/60);c.Simulate(1f/60);
            float yaw=c.Yaw,pitch=c.Pitch,distance=c.Distance,fov=cam.fieldOfView;
            Check(d.StartDance(0),"Start dance before camera comparison");
            Capture(cam,"current.png");
            c.ToggleVersion();yield return null;
            Capture(cam,"wafflus.png");
            Check(c.Version==AdventureCamera.CameraVersion.Wafflus,"Toggle activates Wafflus");
            Check(d.IsDancing,"Switch preserves dance");
            Check(cam.GetComponent<CinemachineBrain>()&&c.WafflusRig.Pov&&c.WafflusRig.Body,"Actual Cinemachine pipeline is active");
            Check(Mathf.Abs(cam.fieldOfView-60)<.01f,"Wafflus FOV is 60");
            Check(Mathf.Abs(c.WafflusRig.ZoomTarget-6)<.01f,"Wafflus initial distance is 6");
            Check(c.WafflusRig.Pov.m_VerticalAxis.m_MinValue==-90&&c.WafflusRig.Pov.m_VerticalAxis.m_MaxValue==90,"Wafflus vertical limits");
            float before=c.Yaw;
            for(int i=0;i<10;i++){c.AddLook(new Vector2(20,2),Vector2.zero,0,false,Time.deltaTime);yield return null;}
            Check(Mathf.Abs(Mathf.DeltaAngle(before,c.Yaw))>.1f,"Cinemachine POV responds to mouse input");
            Check(d.IsDancing,"Wafflus mouse input preserves dance");
            c.AddLook(Vector2.zero,Vector2.zero,120000,false,Time.deltaTime);
            float deadline = Time.time + 2; while(Time.time < deadline) yield return null;
            Check(c.WafflusRig.ZoomTarget==1&&Mathf.Abs(c.WafflusRig.Body.m_CameraDistance-1)<.1f,"Wafflus zoom clamps and eases to minimum: target="+c.WafflusRig.ZoomTarget+" body="+c.WafflusRig.Body.m_CameraDistance);
            Check(d.IsDancing,"Wafflus zoom preserves dance");
            c.AddLook(Vector2.zero,Vector2.zero,-120000,false,Time.deltaTime);
            deadline = Time.time + 2; while(Time.time < deadline) yield return null;
            Check(c.WafflusRig.ZoomTarget==6&&Mathf.Abs(c.WafflusRig.Body.m_CameraDistance-6)<.1f,"Wafflus zoom clamps and eases to maximum");
            c.NotifyMovement(new AdventurePlayer.Command{move=Vector2.right},true,5);
            Check(c.WafflusRig.Pov.m_HorizontalRecentering.m_enabled,"Sideways movement enables recentering");
            c.NotifyMovement(new AdventurePlayer.Command{move=Vector2.up},true,5);
            Check(!c.WafflusRig.Pov.m_HorizontalRecentering.m_enabled,"Forward movement disables recentering");
            c.NotifyMovement(new AdventurePlayer.Command{move=Vector2.right},false,5);
            Check(!c.WafflusRig.Pov.m_HorizontalRecentering.m_enabled,"Airborne disables recentering");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name="Camera collision verification wall";
            var rotation=Quaternion.Euler(c.Pitch,c.Yaw,0);
            var origin=p.transform.position+Vector3.up*1.25f;
            wall.transform.SetPositionAndRotation(origin+rotation*Vector3.back*3,rotation);
            wall.transform.localScale=new Vector3(4,4,.3f);Physics.SyncTransforms();
            for(int i=0;i<10;i++)yield return null;
            Check(c.Distance<3&&c.Distance>.1f,"Cinemachine pulls in before a wall");
            UnityEngine.Object.DestroyImmediate(wall);
            deadline=Time.time+2;while(Time.time<deadline)yield return null;
            Check(c.Distance>5.5f,"Cinemachine restores distance after wall clears");
            float wyaw=c.Yaw,wpitch=c.Pitch;
            c.SetVersion(AdventureCamera.CameraVersion.Current);yield return null;
            Check(c.Version==AdventureCamera.CameraVersion.Current,"Toggle restores current camera");
            Check(Mathf.Abs(c.Yaw-yaw)<.01f&&Mathf.Abs(c.Pitch-pitch)<.01f&&Mathf.Abs(c.Distance-distance)<.1f,"Current orbit and zoom are restored");
            Check(Mathf.Abs(cam.fieldOfView-fov)<.01f,"Current FOV restored");
            Check(!cam.GetComponent<CinemachineBrain>().enabled,"Current camera has no competing Brain");
            Check(d.IsDancing,"Return toggle preserves dance");
            c.ToggleVersion();yield return null;
            Check(Mathf.Abs(Mathf.DeltaAngle(wyaw,c.Yaw))<.01f&&Mathf.Abs(wpitch-c.Pitch)<.01f,"Wafflus view is restored");
            p.Simulate(new AdventurePlayer.Command{move=Vector2.up},1f/60);
            Check(!d.IsDancing,"Movement still cancels dance in Wafflus mode");
            p.Respawn();for(int i=0;i<60;i++){p.Simulate(new AdventurePlayer.Command(),1f/60);yield return null;}
            Check(d.StartDance(1),"Can dance after settling");
            p.Simulate(new AdventurePlayer.Command{jump=true},1f/60);
            Check(!d.IsDancing&&!p.Grounded,"Jump still cancels dance in Wafflus mode");
            c.SetVersion(AdventureCamera.CameraVersion.Current);
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            void Keys(params Key[] keys)
            {InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));typeof(InputSystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});keyboard.MakeCurrent();}
            foreach(var mode in new[]{AdventureCamera.CameraVersion.Wafflus,AdventureCamera.CameraVersion.Current})
            {
                c.SetVersion(mode);p.Respawn();
                for(int i=0;i<60;i++){p.Simulate(new AdventurePlayer.Command(),1f/60);yield return null;}
                Check(d.StartDance(0),"Dance starts for cursor hold / "+mode);
                c.AddLook(new Vector2(40,10),Vector2.zero,0,false,Time.deltaTime);yield return null;
                Keys(Key.LeftAlt,Key.RightAlt);c.CursorHold.Update(true,true);
                Check(Cursor.visible&&Cursor.lockState==CursorLockMode.None,"Option releases cursor / "+mode);
                float beforeYaw=c.Yaw,beforePitch=c.Pitch;
                if(mode==AdventureCamera.CameraVersion.Wafflus)c.NotifyMovement(new AdventurePlayer.Command{move=Vector2.right},true,5);
                for(int i=0;i<10;i++){Keys(Key.LeftAlt,Key.RightAlt);c.CursorHold.Update(c.CursorHold.OptionHeld,true);c.AddLook(new Vector2(300,200),Vector2.one,0,true,Time.deltaTime);yield return null;}
                Check(Mathf.Abs(Mathf.DeltaAngle(beforeYaw,c.Yaw))<.01f&&Mathf.Abs(c.Pitch-beforePitch)<.01f,"Option freezes look, momentum and recenter / "+mode+" before="+beforeYaw+","+beforePitch+" after="+c.Yaw+","+c.Pitch+" held="+c.CursorHold.OptionHeld);
                Check(d.IsDancing,"Option preserves dancing / "+mode);
                Keys(Key.RightAlt);c.CursorHold.Update(c.CursorHold.OptionHeld,true);
                Check(c.CursorHold.OptionHeld&&Cursor.visible,"One Option remains held / "+mode);
                Keys();c.CursorHold.Update(false,true);
                Check(!Cursor.visible,"Option release requests capture / "+mode);
                c.AddLook(new Vector2(50000,-50000),Vector2.zero,0,false,Time.deltaTime);
                Check(Mathf.Abs(Mathf.DeltaAngle(beforeYaw,c.Yaw))<.01f&&Mathf.Abs(c.Pitch-beforePitch)<.01f,"Relock delta cannot jump camera / "+mode);
                if(mode==AdventureCamera.CameraVersion.Wafflus)c.NotifyMovement(new AdventurePlayer.Command{move=Vector2.up},true,5);
                yield return null;
                for(int i=0;i<10;i++){Keys();c.AddLook(new Vector2(20,0),Vector2.zero,0,false,Time.deltaTime);yield return null;}
                Check(Mathf.Abs(Mathf.DeltaAngle(beforeYaw,c.Yaw))>.1f,"Relative mouse continues from preserved view / "+mode+" before="+beforeYaw+" after="+c.Yaw+" paused="+c.CursorHold.LookPaused+" held="+c.CursorHold.OptionHeld);
                Keys(Key.LeftAlt);c.CursorHold.Update(true,true);Keys();c.CursorHold.Update(false,false);
                Check(Cursor.visible&&Cursor.lockState==CursorLockMode.None,"Uncaptured input does not relock / "+mode);
                c.ToggleVersion();Check(c.Version!=mode,"F6 switches to the other remaining mode / "+mode);
                c.ToggleVersion();Check(c.Version==mode,"F6 cycles back with only two modes / "+mode);
            }
            InputSystem.RemoveDevice(keyboard);
            Check(Enum.GetValues(typeof(AdventureCamera.CameraVersion)).Length==2,"Only two camera modes remain");
            Check(Type.GetType("Staff.Characters.MouseMovementCamera, Assembly-CSharp")==null,"MouseMovement implementation removed");
            c.SetVersion(AdventureCamera.CameraVersion.Current);
            Directory.CreateDirectory("Library/StaffCamera");
            File.WriteAllText("Library/StaffCamera/result.json","{\"pass\":true,\"checks\":"+checks+",\"cinemachine\":\"2.8.4\"}");
            EditorApplication.Exit(0);yield break;
        }
    }
}
