using UnityEngine;
using Staff.Nishita;
namespace Staff.Subway {
 // Cloud-only sun override: existing scene lights retain their authored transforms and intensities.
 [ExecuteAlways,DisallowMultipleComponent]
 public sealed class NishitaCloudLighting:MonoBehaviour {
  public SubwayTimeOfDay sky;
  void OnEnable(){VolumetricCloudsURP.CameraFilter+=Allows;VolumetricCloudsURP.ConfigureForCamera+=Configure;}
  void OnDisable(){VolumetricCloudsURP.CameraFilter-=Allows;VolumetricCloudsURP.ConfigureForCamera-=Configure;}
  bool Allows(Camera c){var mode=c.GetComponent<AbstractEnvironment>();return (!sky||sky.skyWeather.clouds)&&(!mode||!mode.IsAbstract);}
  void Configure(Material material,Camera c){
   if(!sky||!sky.atmosphere||!sky.atmosphere.Ready)return;
   var a=sky.atmosphere;var d=a.SunDirection;
   float day=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-8,12,a.Elevation));
   float warm=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,20,a.Elevation));
   Color color=Color.Lerp(Color.white,new Color(1,.65f,.34f),warm)*day*1.25f;
   material.SetVector("_StaffSunDirection",new Vector4(d.x,d.y,d.z,1));
   material.SetColor("_StaffSunColor",color);
   material.SetColor("_StaffAmbientFloor",Color.Lerp(new Color(.001f,.002f,.005f),new Color(.16f,.22f,.32f),day));
  }
 }
}
