using UnityEditor;
using UnityEngine;
namespace Staff.Subway.Editor {
 [InitializeOnLoad] public static class SolarOpticsShortcuts {
  static SolarOpticsShortcuts(){SceneView.duringSceneGui+=OnSceneGUI;}
  static void OnSceneGUI(SceneView view){
   if(EditorApplication.isPlaying)return;
   var e=Event.current;if(e.type!=EventType.KeyDown||e.alt||e.control||e.command||e.shift)return;
   var sky=SubwayTimeOfDay.Active;if(!sky)return;
   if(e.keyCode!=KeyCode.F9&&e.keyCode!=KeyCode.F10)return;
   Undo.RecordObject(sky,"Toggle solar optics");
   if(e.keyCode==KeyCode.F9)sky.solarEffects.lensFlare=!sky.solarEffects.lensFlare;
   else sky.solarEffects.sunRays=!sky.solarEffects.sunRays;
   EditorUtility.SetDirty(sky);e.Use();EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();
  }
 }
}
