using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Staff.Characters;
namespace Staff.Subway.Editor {
 [InitializeOnLoad] public static class SubwayReadmeCapture {
 const string Key="Staff.Subway.ReadmeCapture";
 static SubwayReadmeCapture(){EditorApplication.update+=Tick;}
 [MenuItem("STAFF/Subway/Verify Sample Default")]
 static void Sample(){Run("SampleScene");}
 [MenuItem("STAFF/Subway/Capture The Herta")]
 static void Subway(){Run("STAFF_Subway_Reference");}
 static void Run(string scene){if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");SessionState.SetString(Key,scene);EditorApplication.isPlaying=true;}
 static void Capture(Camera c,string name){var rt=new RenderTexture(1920,1080,24);var old=c.targetTexture;var active=RenderTexture.active;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;var img=new Texture2D(1920,1080,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1920,1080),0,0);img.Apply();File.WriteAllBytes("Library/StaffSubway/"+name+".png",img.EncodeToPNG());UnityEngine.Object.DestroyImmediate(img);}finally{c.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);}}
 static void Tick(){var scene=SessionState.GetString(Key,"");if(scene==""||!EditorApplication.isPlaying||Time.frameCount<60)return;SessionState.SetString(Key,"");Directory.CreateDirectory("Library/StaffSubway");try{var s=UnityEngine.Object.FindFirstObjectByType<CharacterSwitcher>();if(!s||s.CurrentName!="TheHerta")throw new Exception("Default is not TheHerta: "+(s?s.CurrentName:"missing"));var p=s.GetComponent<AdventurePlayer>();if(!p.Animator.isHuman)throw new Exception("Missing humanoid animator");if(scene=="STAFF_Subway_Reference"){p.enabled=false;var c=Camera.main;Capture(c,"the-herta-gameplay");c.GetComponent<AdventureCamera>().enabled=false;p.Visual.rotation=Quaternion.Euler(0,160,0);p.Animator.Update(.1f);c.transform.position=new Vector3(-1.7f,1.65f,-8.1f);c.transform.LookAt(new Vector3(.8f,1.3f,1));c.fieldOfView=57;Capture(c,"the-herta-subway");}File.WriteAllText("Library/StaffSubway/"+scene+"-default.txt","PASS: Play mode default TheHerta; valid humanoid avatar.");}catch(Exception e){File.WriteAllText("Library/StaffSubway/"+scene+"-default.txt","FAIL: "+e);Debug.LogException(e);}finally{EditorApplication.isPlaying=false;}}
 }
}
