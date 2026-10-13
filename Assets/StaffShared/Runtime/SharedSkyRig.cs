using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Staff.Subway;
namespace Staff.Shared {
[ExecuteAlways,DefaultExecutionOrder(200),DisallowMultipleComponent]
public sealed class SharedSkyRig:MonoBehaviour {
 public SubwayTimeOfDay sky;
 [Tooltip("Shared URP renderer contains clouds, stars, solar optics and local rays.")]
 public ScriptableRendererData rendererData;
 public Light sun;
 [Min(0)] public float daylightIntensity=1.15f;
 [Tooltip("Only the active scene owns global ambient/sun and volume settings during additive editing.")]
 public Volume[] skyVolumes;
 void OnEnable(){RenderPipelineManager.beginCameraRendering+=BindCamera;}
 void OnDisable(){RenderPipelineManager.beginCameraRendering-=BindCamera;}
 void BindCamera(ScriptableRenderContext context,Camera camera){
  if(SubwayTimeOfDay.ForCamera(camera)!=sky||camera.cameraType==CameraType.Preview)return;
  var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
  // The project shares the full PC renderer at index zero, including in new scenes.
  if(pipeline)camera.GetUniversalAdditionalCameraData().SetRenderer(0);
  sky.ApplyToCamera(camera);
 }
 void LateUpdate()=>ApplyLighting();
 public void ApplyLighting(){
  if(!sky||!sky.atmosphere||!gameObject.scene.IsValid())return;
  bool active=gameObject.scene==SceneManager.GetActiveScene();
  if(skyVolumes!=null)foreach(var v in skyVolumes)if(v)v.enabled=active;
  if(!sun)return;
  var a=sky.atmosphere;sun.transform.rotation=Quaternion.LookRotation(-a.SunDirection,Vector3.up);
  float day=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-3,12,a.Elevation));
  sun.intensity=day*daylightIntensity;
  sun.color=Color.Lerp(new Color(1,.52f,.25f),new Color(1,.96f,.86f),Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,30,a.Elevation)));
  sun.enabled=active;
  if(active){if(RenderSettings.fog)RenderSettings.fogColor=Color.Lerp(new Color(.008f,.013f,.035f),new Color(.66f,.72f,.75f),day);RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Color.Lerp(new Color(.015f,.024f,.06f),new Color(.48f,.59f,.72f),day);RenderSettings.ambientEquatorColor=Color.Lerp(new Color(.008f,.012f,.03f),new Color(.30f,.34f,.40f),day);RenderSettings.ambientGroundColor=Color.Lerp(new Color(.006f,.008f,.015f),new Color(.17f,.15f,.13f),day);}
 }
}
}
