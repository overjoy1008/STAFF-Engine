using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Staff.Characters;
[InitializeOnLoad] public static class StaffURPPlayValidation {
 const string K="StaffURPQA";static StaffURPPlayValidation(){EditorApplication.update+=Tick;}
 public static void RunMathOnly(){Run();SessionState.SetInt(K+"Limit",1);}
 public static void Run(){SessionState.SetInt(K+"Limit",3);SessionState.SetInt(K,1);SessionState.SetInt(K+"Scene",0);Directory.CreateDirectory("Library/StaffURPValidation");}
 static void Tick(){int stage=SessionState.GetInt(K,0);if(stage==0||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 try{int index=SessionState.GetInt(K+"Scene",0);
 if(stage==1&&!EditorApplication.isPlayingOrWillChangePlaymode){if(index==SessionState.GetInt(K+"Limit",3)){SessionState.SetInt(K,0);Debug.Log("STAFF_URP_PLAY_PASS");EditorApplication.Exit(0);return;}EditorSceneManager.OpenScene(StaffURPScenes.Scenes[index]);SessionState.SetInt(K,2);EditorApplication.isPlaying=true;return;}
 if(stage!=2||!EditorApplication.isPlaying||Time.frameCount<35)return;Application.runInBackground=true;
 foreach(var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.enabled)){Capture(cam,"Play-"+index+"-"+cam.name);var a=cam.GetComponent<Staff.Subway.AbstractEnvironment>();if(a){a.SetMode(Staff.Subway.AbstractEnvironment.EnvironmentMode.Abstract);Capture(cam,"Abstract-"+index);a.SetMode(Staff.Subway.AbstractEnvironment.EnvironmentMode.Imaginary);Capture(cam,"Imaginary-"+index);a.SetMode(Staff.Subway.AbstractEnvironment.EnvironmentMode.Real);}}
 var player=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();if(player){var roster=player.GetComponent<CharacterSwitcher>();if(roster){for(int i=1;i<roster.Count;i++)if(roster.NameAt(i)=="TheHerta"){roster.Select(i);break;}player.Animator.Update(.1f);var head=player.Animator.GetBoneTransform(HumanBodyBones.Head);var go=new GameObject("Hoyo QA");var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=.34f;cam.nearClipPlane=.03f;var target=head.position+Vector3.up*.03f;cam.transform.position=target+player.Visual.forward*1.4f;cam.transform.LookAt(target);cam.backgroundColor=Color.gray;cam.clearFlags=CameraClearFlags.SolidColor;for(int i=0;i<4;i++){Capture(cam,"Character-"+index+"-"+roster.ShaderMode);roster.ToggleToon();}UnityEngine.Object.DestroyImmediate(go);}}
 foreach(var id in AssetDatabase.FindAssets("t:Shader",new[]{"Assets"})){var s=AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(id));foreach(var e in ShaderUtil.GetShaderMessages(s))if(e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception("SHADER_FAIL "+s.name+" "+e.message+" "+e.line);}
 SessionState.SetInt(K+"Scene",index+1);SessionState.SetInt(K,1);EditorApplication.isPlaying=false;
 }catch(Exception e){Debug.LogException(e);SessionState.SetInt(K,0);EditorApplication.Exit(1);}}
 static void Capture(Camera cam,string name){var old=cam.targetTexture;var rect=cam.rect;var rt=new RenderTexture(960,540,24,RenderTextureFormat.ARGBHalf);rt.Create();try{cam.targetTexture=rt;cam.rect=new Rect(0,0,1,1);Staff.Rendering.StaffCameraCapture.Render(cam);var active=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(960,540,TextureFormat.RGBAFloat,false,true);t.ReadPixels(new Rect(0,0,960,540),0,0);t.Apply();var png=new Texture2D(960,540,TextureFormat.RGB24,false);png.SetPixels(t.GetPixels().Select(c=>c.gamma).ToArray());png.Apply();File.WriteAllBytes("Library/StaffURPValidation/"+name.Replace('/','_')+".png",png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);int pink=t.GetPixels32().Count(p=>p.r>245&&p.g<10&&p.b>245);Debug.Log("URP_CAPTURE "+name+" pink="+pink);RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(t);}finally{cam.targetTexture=old;cam.rect=rect;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
}
