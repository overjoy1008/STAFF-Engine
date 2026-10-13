using System;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Staff.Subway;
public static class CaptureAuthoredMoon {
 [MenuItem("STAFF/Subway/Capture Authored Moon")]
 public static void Capture(){var sky=UnityEngine.Object.FindFirstObjectByType<SubwayTimeOfDay>();if(!sky)throw new Exception("No sky");File.WriteAllText("/tmp/staff-authored-moon.json",JsonUtility.ToJson(sky.moon,true));File.WriteAllText("/tmp/staff-authored-weather.json",JsonUtility.ToJson(sky.skyWeather,true));Debug.Log("Captured authored Moon; playing="+EditorApplication.isPlaying);}
}
