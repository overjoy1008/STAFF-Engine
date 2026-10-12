using UnityEngine;
using UnityEngine.Rendering;
namespace Staff.Nishita
{
 [ExecuteAlways, RequireComponent(typeof(Camera),typeof(Skybox))]
 public sealed class NishitaCamera : MonoBehaviour
 {
  public NishitaComparison comparison;
  public bool multiple;
  public Shader toneShader;
  Material tone;
  AmbientMode oldMode; Color oldSky,oldEquator,oldGround,oldSun;
  SphericalHarmonicsL2 oldProbe;
  float oldIntensity,oldReflection; Quaternion oldRotation; bool scoped;
  void OnEnable() { GetComponent<Camera>().allowHDR=true; RenderPipelineManager.beginCameraRendering+=Begin;RenderPipelineManager.endCameraRendering+=End; }
  void Begin(ScriptableRenderContext context,Camera camera){if(camera==GetComponent<Camera>())OnPreCull();}
  void End(ScriptableRenderContext context,Camera camera){if(camera==GetComponent<Camera>())Restore();}
  public Material ToneMaterial {get {if(!comparison||!comparison.agxLut||!toneShader)return null;if(!tone)tone=new Material(toneShader){hideFlags=HideFlags.HideAndDontSave};tone.SetTexture("_Lut",comparison.agxLut);tone.SetFloat("_Exposure",comparison.exposure);return tone;}}
  void OnPreCull()
  {
   if(!comparison || comparison.presets==null || comparison.presets.Length==0)return;
   var p=comparison.Current; var v=multiple?p.multiple:p.single;
   var atmosphere=comparison.atmosphere;
   bool live=atmosphere && atmosphere.Ready;
   Color skyColor=live?atmosphere.AmbientSky(multiple):v.ambientSky;
   Color horizonColor=live?atmosphere.AmbientHorizon(multiple):v.ambientHorizon;
   GetComponent<Skybox>().material=live?atmosphere.GetSky(multiple):v.sky;
   oldProbe=RenderSettings.ambientProbe; oldMode=RenderSettings.ambientMode; oldSky=RenderSettings.ambientSkyColor;
   oldEquator=RenderSettings.ambientEquatorColor; oldGround=RenderSettings.ambientGroundColor;
   oldReflection=RenderSettings.reflectionIntensity;
   RenderSettings.ambientMode=AmbientMode.Custom;
   var sh=new SphericalHarmonicsL2();
   sh.AddAmbientLight((skyColor*.45f+horizonColor*.25f)/256);
   sh.AddDirectionalLight(Vector3.up,skyColor/256,.55f);
   RenderSettings.ambientProbe=sh;
   if(comparison.gallery){
    var block=new MaterialPropertyBlock();block.CopySHCoefficientArraysFrom(new[]{sh});
    foreach(var renderer in comparison.gallery.GetComponentsInChildren<Renderer>()){
     renderer.lightProbeUsage=LightProbeUsage.CustomProvided;renderer.SetPropertyBlock(block);
    }
   }
   RenderSettings.ambientSkyColor=skyColor/256;
   RenderSettings.ambientEquatorColor=horizonColor/256;
   RenderSettings.ambientGroundColor=horizonColor*(0.12f/256);
   RenderSettings.reflectionIntensity=0;
   if(comparison.sun) {
    oldSun=comparison.sun.color; oldIntensity=comparison.sun.intensity; oldRotation=comparison.sun.transform.rotation;
    if(live){
     var irradiance=atmosphere.DirectSun(multiple);float strength=Mathf.Max(irradiance.r,Mathf.Max(irradiance.g,irradiance.b));
     comparison.sun.color=strength>1e-8f?(irradiance/strength).gamma:Color.white;comparison.sun.intensity=strength/256;
     comparison.sun.transform.rotation=Quaternion.LookRotation(-atmosphere.SunDirection,Vector3.up);
    }else{
     comparison.sun.color=p.sunColor;comparison.sun.intensity=p.sunIntensity/256;
     comparison.sun.transform.rotation=Quaternion.Euler(p.elevation,255,0);
    }
   }
   scoped=true;
  }
  void OnPostRender(){ Restore(); }
  void Restore(){
   if(!scoped)return; scoped=false;
   RenderSettings.ambientProbe=oldProbe; RenderSettings.ambientMode=oldMode; RenderSettings.ambientSkyColor=oldSky;
   RenderSettings.ambientEquatorColor=oldEquator; RenderSettings.ambientGroundColor=oldGround;
   RenderSettings.reflectionIntensity=oldReflection;
   if(comparison && comparison.sun){comparison.sun.color=oldSun;comparison.sun.intensity=oldIntensity;comparison.sun.transform.rotation=oldRotation;}
  }
  void OnRenderImage(RenderTexture source,RenderTexture destination) {
   if(!comparison || !comparison.agxLut || !toneShader){Graphics.Blit(source,destination);return;}
   if(!tone)tone=new Material(toneShader){hideFlags=HideFlags.HideAndDontSave};
   tone.SetTexture("_Lut",comparison.agxLut); tone.SetFloat("_Exposure",comparison.exposure);
   Graphics.Blit(source,destination,tone);
  }
  void OnDisable(){RenderPipelineManager.beginCameraRendering-=Begin;RenderPipelineManager.endCameraRendering-=End;Restore();if(tone){if(Application.isPlaying)Destroy(tone);else DestroyImmediate(tone);}}
 }
}
