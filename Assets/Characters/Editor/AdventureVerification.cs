using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Staff.MathSpace;
using Staff.MathSpace.Editor;

namespace Staff.Characters.Editor
{
    [InitializeOnLoad]
    public static class AdventureVerification
    {
        const string Flag = "Staff.AdventureVerification";
        static AdventureVerification()
        {
            EditorApplication.update += Poll;
        }
        static void Poll()
        {
            if (SessionState.GetBool(Flag, false) && EditorApplication.isPlaying && Time.frameCount > 5)
                Verify();
        }
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Flag, true);
            EditorApplication.isPlaying = true;
        }
        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            Debug.Log("PLAYER TEST PASS: " + message);
        }
        static void Verify()
        {
            SessionState.SetBool(Flag, false);
            try
            {
                Check(Application.isPlaying, "Runs in real Play mode");
                var player = UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();
                var camera = Camera.main.GetComponent<AdventureCamera>();
                player.enabled = false; camera.enabled = true;
                var animator = player.Animator;
                Check(animator && animator.avatar && animator.avatar.isValid, "Valid skeletal animation avatar");
                Check(animator.isHuman, "Starter Assets Human Avatar active");
                Check(animator.runtimeAnimatorController.animationClips.Length >= 4, "Locomotion / jump clips bound");
                VerifyInput();
                const float dt = 1f / 60;
                void Step(AdventurePlayer.Command command, int count)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Physics.SyncTransforms();
                        player.Simulate(command, dt);
                        animator.Update(dt);
                        camera.Simulate(dt);
                    }
                }
                var idle = new AdventurePlayer.Command();
                Step(idle, 60);
                Check(player.Grounded && Mathf.Abs(player.transform.position.y) < .15f, "Grounded on coordinate floor");
                Check(animator.GetCurrentAnimatorClipInfo(0).Any(c => c.clip.name.Contains("Idle") && c.weight > .95f), "Idle clip dominates while stationary");
                var bones = player.GetComponentsInChildren<Transform>();
                var pose = bones.Select(t => t.localRotation).ToArray();
                animator.Update(.4f);
                Check(bones.Where((t, i) => Quaternion.Angle(t.localRotation, pose[i]) > .05f).Any(), "Idle changes skeletal pose");
                Vector3 start = player.transform.position;
                Step(new AdventurePlayer.Command { move = Vector2.up }, 60);
                float run = Vector3.Distance(player.transform.position, start);
                Check(run > 7.1f && run < 8.1f, "Faster running displacement " + run);
                Check(animator.GetCurrentAnimatorClipInfo(0).Any(c => c.clip.name.Contains("Run_N") && c.weight > .8f), "Run clip dominates while running");
                Capture("player-running.png");
                start = player.transform.position;
                var dash = new AdventurePlayer.Command { move = Vector2.up, sprint = true };
                Step(dash, 1);
                Check(player.IsDashing && player.HorizontalSpeed > 17, "Press starts an immediate fast dash");
                Step(dash, 59);
                float sprint = Vector3.Distance(player.transform.position, start);
                Check(sprint > run + 1.5f && sprint < 11, "Burst adds distance then returns to running " + sprint);
                Check(!player.IsDashing && Mathf.Abs(player.HorizontalSpeed - 8) < .02f, "Holding Shift does not sustain sprint");
                for (int i = 0; i < 90; i++) { Step(dash, 1); CheckNoRepeat(player); }
                Step(new AdventurePlayer.Command { move = Vector2.up }, 1);
                Step(dash, 1);
                Check(player.IsDashing, "Release and press again retriggers after cooldown");
                Step(new AdventurePlayer.Command { move = Vector2.up }, 1);
                Step(dash, 1);
                Step(new AdventurePlayer.Command { move = Vector2.up }, 15);
                Check(!player.IsDashing, "Early presses do not extend dash duration");
                Step(dash, 1);
                Check(!player.IsDashing, "Cooldown rejects early re-press without queueing");
                Step(idle, 30);
                start = player.transform.position;
                Step(new AdventurePlayer.Command { move = Vector2.up, walk = true }, 60);
                float walk = Vector3.Distance(player.transform.position, start);
                Check(walk > 1.4f && walk < run * .6f, "Walking is slower " + walk);
                Step(new AdventurePlayer.Command { move = Vector2.one, sprint = true }, 60);
                Check(player.HorizontalSpeed < 8.01f, "Diagonal input is normalized after burst");
                Step(idle, 30);
                float floor = player.transform.position.y;
                Step(new AdventurePlayer.Command { jump = true }, 1);
                Step(new AdventurePlayer.Command { sprint = true }, 1);
                Check(!player.IsDashing, "Airborne dash is rejected");
                float apex = floor;
                bool jumped = false, fell = false;
                for (int i = 0; i < 100; i++)
                {
                    Step(idle, 1); apex = Mathf.Max(apex, player.transform.position.y);
                    jumped |= animator.GetCurrentAnimatorStateInfo(0).IsName("Jump");
                    fell |= animator.GetCurrentAnimatorStateInfo(0).IsName("Fall");
                    if (i == 14) Capture("player-jumping.png");
                }
                Check(apex - floor > 1 && apex - floor < 1.5f, "Jump reaches configured height " + (apex-floor));
                Check(jumped && fell && animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), "Jump -> fall -> locomotion transitions");
                Check(player.Grounded && Mathf.Abs(player.transform.position.y - floor) < .08f, "Lands without falling through floor");
                Step(idle, 60);
                start = player.transform.position;
                Step(new AdventurePlayer.Command { sprint = true }, 1);
                Step(idle, 60);
                Check(Vector3.Distance(start, player.transform.position) > 1 && player.HorizontalSpeed < .01f, "Tap at rest dashes forward then stops");
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = player.transform.position + Vector3.forward * .85f + Vector3.up;
                wall.transform.localScale = new Vector3(3, 3, .2f);
                Physics.SyncTransforms(); start = player.transform.position;
                Step(dash, 30);
                Check(player.transform.position.z - start.z < .55f, "Dash respects walls without tunnelling");
                UnityEngine.Object.DestroyImmediate(wall); Physics.SyncTransforms(); Step(idle, 60);
                Step(dash, 1); player.ReleaseInput();
                Check(!player.IsDashing && player.HorizontalSpeed == 0, "Escape/focus release cancels burst");
                Step(idle, 60);
                float referenceDistance = 0;
                foreach (int fps in new[] { 30, 60, 120 })
                {
                    player.Respawn(); Step(idle, 60); start = player.transform.position;
                    for (int i=0; i<fps; i++) { Physics.SyncTransforms(); player.Simulate(dash, 1f/fps); animator.Update(1f/fps); }
                    float travelled = Vector3.Distance(start, player.transform.position);
                    if (referenceDistance > 0) Check(Mathf.Abs(travelled-referenceDistance) < .2f, "Dash distance consistent at " + fps + " fps");
                    referenceDistance = travelled;
                }
                player.Respawn(); Step(idle, 60);
                camera.WafflusRig.SetView(90, 0, 6);
                start = player.transform.position;
                Step(new AdventurePlayer.Command { move = Vector2.up }, 60);
                var delta = player.transform.position - start;
                Check(delta.x > 7.1f && Mathf.Abs(delta.z) < .1f, "W follows rotated camera, not world Z");
                Check(Vector3.Dot(player.Visual.forward, Vector3.right) > .98f, "Character faces movement direction");
                Step(idle, 30);
                camera.AddLook(new Vector2(0,100000), Vector2.zero, 0, false, dt);
                Check(camera.Pitch >= -90 && camera.Pitch <= 90, "Vertical orbit is clamped");
                camera.AddLook(new Vector2(0,-100000), Vector2.zero, 0, false, dt);
                Check(camera.Pitch <= 90, "Upper orbit limit");
                camera.WafflusRig.SetView(camera.Yaw, 16, 6);
                camera.Snap();
                var pivot = camera.FocusPoint;
                var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = "Temporary camera collision verification";
                obstacle.transform.position = pivot - camera.transform.forward * 2.5f;
                obstacle.transform.localScale = Vector3.one;
                Physics.SyncTransforms(); camera.Simulate(dt);
                Check(camera.Distance < 2.5f, "Camera pulls in before an obstacle");
                UnityEngine.Object.DestroyImmediate(obstacle); Physics.SyncTransforms();
                for (int i=0;i<90;i++) camera.Simulate(dt);
                Check(camera.Distance > 4.7f, "Camera recovers after obstacle clears");
                player.Respawn(); player.Visual.rotation = Quaternion.identity;
                camera.AddLook(Vector2.zero, Vector2.zero, 0, true, dt);
                Step(idle, 60); camera.Snap();
                var mode = UnityEngine.Object.FindFirstObjectByType<MonochromeMode>();
                mode.SetInverted(false); Capture("player-normal.png");
                mode.SetInverted(true); Capture("player-inverted.png");
                camera.AddLook(new Vector2(1500,0), Vector2.zero, 0, false, dt);
                camera.Snap(); Capture("player-front.png");
                CartesianSpaceSetup.Validate();
                File.WriteAllText("Library/StaffMathSpace/player-verification.txt", "PASS: Play mode; Shift/right mouse input; immediate dash; return to run; no held-key repeats; tap at rest; cooldown; no air dash; wall collision; focus cancellation; 30/60/120 fps; animated humanoid; walking; jump/fall/landing; camera-relative movement; camera collision; inversion and math space.\n");
                Debug.Log("ALL PLAYER TESTS PASSED");
                EditorApplication.Exit(0);
            }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }
        static void Capture(string filename)
        {
            CartesianSpaceSetup.Capture();
            File.Copy("Library/StaffMathSpace/camera-preview-infinite.png", "Library/StaffMathSpace/" + filename, true);
        }
        static void CheckNoRepeat(AdventurePlayer player)
        {
            if (player.IsDashing || player.HorizontalSpeed > 8.01f) throw new Exception("Held dash repeated or kept sprint speed");
        }
        static void VerifyInput()
        {
            var previousSettings = InputSystem.settings;
            var testSettings = UnityEngine.Object.Instantiate(previousSettings);
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testSettings;
            var asset = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Characters/Input/Adventure.inputactions"));
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                asset.Enable();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift, Key.Space));
                InputSystem.Update();
                Check(asset.FindAction("Move").ReadValue<Vector2>().y > .99f, "W action binding");
                Check(asset.FindAction("Sprint").IsPressed() && asset.FindAction("Jump").IsPressed(), "Shift / Space bindings");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
                InputSystem.Update();
                Check(asset.FindAction("Sprint").WasPressedThisFrame(), "Right mouse dash binding");
                InputSystem.Update();
                Check(asset.FindAction("Sprint").IsPressed() && !asset.FindAction("Sprint").WasPressedThisFrame(), "Held right mouse is not a new press");
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right });
                InputSystem.Update();
                Check(asset.FindAction("Move").ReadValue<Vector2>().x > .99f, "Gamepad movement binding");
            }
            finally
            {
                asset.Disable(); InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad); InputSystem.RemoveDevice(mouse);
                UnityEngine.Object.DestroyImmediate(asset); InputSystem.settings = previousSettings;
                UnityEngine.Object.DestroyImmediate(testSettings);
            }
        }
    }
}
