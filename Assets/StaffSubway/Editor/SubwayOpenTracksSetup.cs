using System;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace Staff.Subway.Editor {
 public static class SubwayOpenTracksSetup {
  [MenuItem("STAFF/Subway/Enable Open Track Extensions")]
  public static void Install(){if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");var scene=SceneManager.GetActiveScene();if(scene.name!="STAFF_Subway_Reference")throw new Exception("Open the subway scene first.");System.IO.Directory.CreateDirectory("Library/StaffSubway");EditorSceneManager.SaveScene(scene,"Library/StaffSubway/before-open-tracks.unity",true);var env=scene.GetRootGameObjects().Single(g=>g.name=="Environment");var extension=env.GetComponent<SubwayOpenTracks>();if(!extension)extension=Undo.AddComponent<SubwayOpenTracks>(env);extension.Build();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("Open tracks: 60m per end, ground colliders and platform access ramps installed.");}
 }
}
