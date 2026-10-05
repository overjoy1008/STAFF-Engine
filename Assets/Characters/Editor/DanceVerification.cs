using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Staff.Characters.Editor
{
    [InitializeOnLoad] public static class DanceVerification
    {
        const string Flag="Staff.DanceVerify";
        static IEnumerator routine;static int last=-1;
        static DanceVerification(){EditorApplication.update+=Poll;}
        public static void RunBatch()
        {DanceSetup.BuildBatch();EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;}
        static void Poll()
        {
            if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying||Time.frameCount<10||last==Time.frameCount)return;
            last=Time.frameCount;
            try {routine??=Verify();if(!routine.MoveNext())SessionState.SetBool(Flag,false);}
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Library/StaffDance/verify-error.txt",e.ToString());EditorApplication.Exit(1);}
        }
        static int checks;
        static void Check(bool condition,string message)
        {if(!condition)throw new Exception(message);checks++;Debug.Log("DANCE TEST PASS: "+message);}
        static IEnumerator Verify()
        {
            var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();var d=p.GetComponent<AdventureDance>();
            var s=p.GetComponent<CharacterSwitcher>();var camera=Camera.main.GetComponent<AdventureCamera>();
            Check(d && d.Count==8,"Dance library attached automatically to existing player");
            p.enabled=false;camera.enabled=false;s.enabled=false;d.enabled=false;
            typeof(AdventureDance).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);
            const float dt=1f/60;
            void Step(AdventurePlayer.Command cmd,int frames=1)
            {for(int n=0;n<frames;n++){Physics.SyncTransforms();p.Simulate(cmd,dt);p.Animator.Update(dt);}}
            void Settle(){p.Respawn();Step(new AdventurePlayer.Command(),60);}
            Settle();
            Application.runInBackground=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            void UpdateInput()=>typeof(InputSystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});
            void Press(Key key,bool accept=true)
            {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));UpdateInput();d.HandleInput(accept);InputSystem.QueueStateEvent(keyboard,new KeyboardState());UpdateInput();}
            var catalog=Resources.Load<DanceCatalog>("StaffDanceCatalog");
            for(int i=0;i<8;i++)
            {
                Press(Key.Digit1+i);p.Animator.Update(0);p.Animator.Update(.2f);

                Check(d.ActiveIndex==i && p.Animator.GetCurrentAnimatorStateInfo(0).IsName("Dance_"+catalog.dances[i].id),"Top-row key starts "+catalog.dances[i].id);
                Check(d.Music.clip==catalog.dances[i].music,"Corresponding audio "+catalog.dances[i].id);
                Step(new AdventurePlayer.Command(),20);Check(d.IsDancing,"Idle does not cancel dance");
                camera.AddLook(new Vector2(300,30),new Vector2(.3f,.1f),120,true,dt);camera.Simulate(dt);Step(new AdventurePlayer.Command());Check(d.IsDancing,"Mouse/stick/zoom/recenter preserves "+catalog.dances[i].id);
                d.StopDance();Press(Key.Numpad1+i);Check(d.ActiveIndex==i,"Numpad key starts "+catalog.dances[i].id);
            }
            d.StopDance();Press(Key.Digit1,false);Check(!d.IsDancing,"Uncaptured number input is ignored");
            p.InputBlocked=true;Press(Key.Digit1);Check(!d.IsDancing,"Picker blocks number input");p.InputBlocked=false;
            Settle();Press(Key.Digit1);Step(new AdventurePlayer.Command{move=Vector2.up});Check(!d.IsDancing && !d.Music.isPlaying,"Movement cancels dance and music immediately");
            Press(Key.Digit2);Check(!d.IsDancing,"Dance cannot start while moving");Step(new AdventurePlayer.Command(),60);
            Press(Key.Digit1);Step(new AdventurePlayer.Command{jump=true});Check(!d.IsDancing && !p.Grounded,"Jump cancels dance and executes jump");Press(Key.Digit1);Check(!d.IsDancing,"Airborne number input is rejected");
            Settle();Press(Key.Digit1);Step(new AdventurePlayer.Command{sprint=true});Check(!d.IsDancing && p.IsDashing,"Dash cancels dance immediately");
            Settle();Press(Key.Digit1);p.ReleaseInput();Check(!d.IsDancing,"Cursor release cancels dance");
            int combinations=0;
            for(int character=0;character<s.Count;character++)
            {
                s.Select(character);Settle();
                for(int dance=0;dance<8;dance++)
                {
                    Check(d.StartDance(dance),"Start on "+s.CurrentName+" / "+dance);p.Animator.Update(.3f);
                    var bones=new[]{HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.Spine}.Select(p.Animator.GetBoneTransform).ToArray();
                    p.Animator.Play("Base Layer.Dance_"+catalog.dances[dance].id,0,.22f);p.Animator.Update(0);
                    var pose=bones.Select(b=>b.localRotation).ToArray();
                    p.Animator.Play("Base Layer.Dance_"+catalog.dances[dance].id,0,.64f);p.Animator.Update(0);
                    Check(Vector3.Distance(p.Animator.GetBoneTransform(HumanBodyBones.Hips).position,p.transform.position)<3,"Pelvis stays near player "+s.CurrentName+" / "+dance+" hips="+p.Animator.GetBoneTransform(HumanBodyBones.Hips).position+" player="+p.transform.position);
                    Check(bones.Where((b,n)=>Quaternion.Angle(pose[n],b.localRotation)>.1f).Any(),"Retargeted pose animates "+s.CurrentName+" / "+dance);
                    Step(new AdventurePlayer.Command(),5);Check(d.IsDancing,"No root movement cancellation "+s.CurrentName+" / "+dance);
                    d.StopDance();p.Animator.Update(.2f);combinations++;
                }
            }
            foreach(int character in new[]{4,8,9})
            {
                s.Select(character);Settle();d.StartDance(2);p.Animator.Play("Base Layer.Dance_gowild",0,.5f);p.Animator.Update(0);
                camera.AddLook(Vector2.zero,Vector2.zero,0,true,dt);camera.Snap();Capture(p,"Library/StaffDance/"+s.CurrentName+"-gowild.png");d.StopDance();
            }
            s.Select(8);Settle();Press(Key.Digit1);s.Select(4);Check(!d.IsDancing,"Character replacement stops previous dance");
            Step(new AdventurePlayer.Command(),15);Check(p.Animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Character replacement restores locomotion");
            Settle();Press(Key.Digit1);p.Animator.Update(.3f);
            Directory.CreateDirectory("Library/StaffDance");
            File.WriteAllText("Library/StaffDance/verify-result.json",JsonUtility.ToJson(new Result{pass=true,checks=checks,combinations=combinations,characters=s.Count,dances=8,cameraDoesNotCancel=true,movementCancels=true,jumpCancels=true,dashCancels=true},true));
            InputSystem.RemoveDevice(keyboard);EditorApplication.Exit(0);yield break;
        }
        static void Capture(AdventurePlayer player,string path)
        {
            var obj=new GameObject("Dance Verification Camera");var camera=obj.AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.16f,.19f);camera.fieldOfView=32;
            var target=player.transform.position+Vector3.up*.9f;obj.transform.position=target+player.Visual.forward*3.7f+player.Visual.right*1.2f+Vector3.up*.1f;obj.transform.LookAt(target);
            Debug.Log("DANCE BOUNDS "+player.Animator.GetBoneTransform(HumanBodyBones.Hips).position+" player="+player.transform.position);
            var rt=new RenderTexture(1280,720,24);var previous=camera.targetTexture;var active=RenderTexture.active;
            try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
            finally {camera.targetTexture=previous;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
        }
        [Serializable] class Result {public bool pass,cameraDoesNotCancel,movementCancels,jumpCancels,dashCancels;public int checks,combinations,characters,dances;}
    }
}
