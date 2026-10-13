using UnityEngine;
using UnityEditor;
using Staff.Subway;
using Staff.Nishita;
namespace Staff.Subway.Editor {
 [CustomEditor(typeof(SubwayTimeOfDay))]
 public sealed class SubwaySkyEditor:UnityEditor.Editor {
  UnityEditor.Editor atmosphereEditor;
  public override void OnInspectorGUI(){
   var sky=(SubwayTimeOfDay)target;
   if(sky.presets!=null&&sky.presets.Length>0){
    var labels=new string[sky.ModeCount];for(int i=0;i<sky.presets.Length;i++)labels[i]=sky.presets[i].label;labels[sky.ContinuousModeIndex]="Continuous Solar Day";
    EditorGUI.BeginChangeCheck();int next=EditorGUILayout.Popup("Sky mode (T)",sky.Current,labels);
    if(EditorGUI.EndChangeCheck()){
     Undo.RecordObject(sky,"Change sky preset");if(sky.atmosphere)Undo.RecordObject(sky.atmosphere,"Change sky preset");if(sky.skyFill)Undo.RecordObject(sky.skyFill,"Change sky preset");
     sky.SetMode(next);EditorUtility.SetDirty(sky);if(sky.atmosphere)EditorUtility.SetDirty(sky.atmosphere);
    }
   }
   DrawDefaultInspector();
   if(sky.atmosphere){
    EditorGUILayout.Space();EditorGUILayout.LabelField("Live Nishita parameters",EditorStyles.boldLabel);
    EditorGUILayout.HelpBox("Solar time: enable Use Day Cycle and change Time Of Day. Otherwise use Sun Elevation / Sun Rotation. Parameters are preserved until you select another preset.",MessageType.Info);
    CreateCachedEditor(sky.atmosphere,null,ref atmosphereEditor);atmosphereEditor.OnInspectorGUI();
    EditorGUILayout.LabelField("Effective sun radius",sky.atmosphere.EffectiveSunRadius.ToString("F3")+" degrees");
    EditorGUILayout.LabelField("Effective ozone",sky.atmosphere.EffectiveOzone.ToString("F3"));
   }
  }
  void OnDisable(){if(atmosphereEditor)DestroyImmediate(atmosphereEditor);}
 }
 [InitializeOnLoad]
 static class SkyEditPreview {
  static double next;static string last;static double settle;
  static SkyEditPreview(){
   EditorApplication.update+=Tick;
   UnityEditor.SceneManagement.EditorSceneManager.sceneSaving+=(scene,path)=>{foreach(var sky in Object.FindObjectsByType<SubwayTimeOfDay>(FindObjectsSortMode.None))if(sky.isActiveAndEnabled&&sky.gameObject.scene==scene)sky.RestorePreviewBindings();};
   UnityEditor.SceneManagement.EditorSceneManager.sceneSaved+=scene=>{foreach(var sky in Object.FindObjectsByType<SubwayTimeOfDay>(FindObjectsSortMode.None))if(sky.isActiveAndEnabled&&sky.gameObject.scene==scene)sky.ApplySky();};
  }
  static void Tick(){
   if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
   next=EditorApplication.timeSinceStartup+.05;
   var skies=Object.FindObjectsByType<SubwayTimeOfDay>(FindObjectsSortMode.None);
   string state="";foreach(var sky in skies)if(sky&&sky.isActiveAndEnabled&&sky.atmosphere)state+=JsonUtility.ToJson(sky)+JsonUtility.ToJson(sky.atmosphere);
   if(state!=last){last=state;settle=next+.4;}
   if(next>settle){
    foreach(var sky in skies)if(sky&&sky.isActiveAndEnabled&&sky.atmosphere&&((sky.atmosphere.useDayCycle&&sky.atmosphere.animate)||(sky.skyWeather.animate&&(sky.skyWeather.clouds||sky.skyWeather.stars)))){EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();break;}
    foreach(var sky in skies)if(sky&&sky.isActiveAndEnabled&&sky.atmosphere&&sky.atmosphere.Elevation<-3&&sky.skyWeather.stars&&sky.skyWeather.animate){EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();break;}
    foreach(var sky in skies)if(sky&&sky.isActiveAndEnabled&&sky.atmosphere&&sky.atmosphere.Elevation>-1&&sky.solarEffects.lensFlare&&sky.solarEffects.animateStarPoints&&sky.solarEffects.previewStarAnimation&&sky.solarEffects.starAnimationSpeed>0&&sky.solarEffects.starAnimationAmount>0){EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();break;}
    return;
   }
   foreach(var sky in skies)if(sky&&sky.isActiveAndEnabled&&sky.atmosphere){sky.atmosphere.Tick(false);sky.ApplySky();}
   EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();
  }
 }
}
