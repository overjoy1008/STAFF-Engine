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
   // One shared weather clock for Game/Scene cameras; no per-camera accumulation.
   Vector2 wind=sky.CloudDisplacement;
   material.SetVector("_WindVector",new Vector4(wind.x,wind.y,0,0));
   float heading=sky.skyWeather.windHeading*Mathf.Deg2Rad;
   material.SetVector("_WindDirection",new Vector4(-Mathf.Cos(heading),-Mathf.Sin(heading),0,0));
   float day=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-8,12,a.Elevation));
   float warm=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,20,a.Elevation));
   Color color=Color.Lerp(Color.white,new Color(1,.65f,.34f),warm)*day*1.25f;
   material.SetVector("_StaffSunDirection",new Vector4(d.x,d.y,d.z,1));
   material.SetColor("_StaffSunColor",color);
   var legacyAmbient=Color.Lerp(new Color(.001f,.002f,.005f),new Color(.16f,.22f,.32f),day);
   material.SetColor("_StaffAmbientFloor",legacyAmbient);
   var palette=sky.skyWeather.cloudColors;
   if(palette!=null&&palette.enabled){
    var localSun=Quaternion.Euler(0,-a.northRotation,0)*d;
    float morning=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.2f,.2f,localSun.x));
    var lighting=CloudTimePalette.Evaluate(palette,a.Elevation,morning);
    material.SetColor("_StaffSunColor",Color.Lerp(color,lighting.sun,palette.strength));
    material.SetColor("_StaffCloudAmbientBottom",lighting.bottom);
    material.SetVector("_StaffCloudAmbientTop",new Vector4(lighting.top.r,lighting.top.g,lighting.top.b,palette.strength));
   }else{
    material.SetColor("_StaffAmbientFloor",legacyAmbient);
    material.SetVector("_StaffCloudAmbientTop",Vector4.zero);
   }
  }
 }
}
