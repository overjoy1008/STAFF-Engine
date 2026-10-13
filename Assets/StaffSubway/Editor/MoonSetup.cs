using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Staff.Subway;
public static class MoonSetup {
 [MenuItem("STAFF/Subway/Install NASA Moon")]
 public static void Install(){if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");var sky=UnityEngine.Object.FindFirstObjectByType<SubwayTimeOfDay>();if(!sky)throw new Exception("Open subway scene.");Directory.CreateDirectory("Library/StaffSubway");EditorSceneManager.SaveScene(sky.gameObject.scene,"Library/StaffSubway/before-moon.unity",true);Undo.RecordObject(sky,"Install NASA moon");sky.moon.enabled=true;sky.moon.surface=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/NASA/lroc_color_2k.jpg");sky.ApplySky();EditorUtility.SetDirty(sky);EditorSceneManager.MarkSceneDirty(sky.gameObject.scene);EditorSceneManager.SaveScene(sky.gameObject.scene);Debug.Log("NASA Moon saved: phase .5 full, Solar Day orbit, adjustable radius/brightness/tint/earthshine/glow.");}
}
