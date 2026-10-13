using System;using UnityEngine;using Staff.Nishita;
namespace Staff.Subway {
 public static class CelestialMoon {
  [Serializable] public sealed class Settings {
   public bool enabled=true;
   public Texture2D surface;
   [Tooltip("0=new, .25=first quarter, .5=full, .75=last quarter.")][Range(0,1)]public float phase=.5f;
   public bool followSolarDay=true;
   [Range(-90,90)] public float elevation=30;
   [Range(0,360)] public float azimuth=0;
   [Tooltip("Art-directed angular radius; real Moon is about .26 degrees.")][Range(.1f,3)]public float radius=1.4f;
   [Range(0,3)]public float brightness=1;
   [ColorUsage(false)] public Color tint=new Color(.702830195f,.833294988f,1);
   [Range(0,.2f)]public float earthshine=.025f;
   [Range(0,.3f)]public float glow=.01f;
   [Range(1.2f,8)]public float glowRadius=5.5f;
   [Range(-180,180)]public float textureRotation=0;
  }
  public static Vector3 Direction(Settings s,NishitaAtmosphere a){
   if(!s.followSolarDay)return Quaternion.Euler(-s.elevation,s.azimuth,0)*Vector3.forward;
   if(a.useDayCycle)return NishitaAtmosphere.SolarDirection(Mathf.Repeat(a.timeOfDay-s.phase*24,24),a.latitude,a.solarDeclination*Mathf.Cos(s.phase*Mathf.PI*2),a.northRotation);
   return Quaternion.AngleAxis(s.phase*360,NishitaSkyWeather.PoleDirection(a.latitude,a.northRotation))*a.SunDirection;
  }
  public static Vector4 Disc(Settings s,NishitaAtmosphere a){var d=Direction(s,a);return new Vector4(d.x,d.y,d.z,Mathf.Clamp(s.radius,.1f,3)*Mathf.Deg2Rad);}
  public static float Visibility(Settings s,NishitaAtmosphere a){return s.enabled?Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.5f,2,Mathf.Asin(Mathf.Clamp(Direction(s,a).y,-1,1))*Mathf.Rad2Deg)):0;}
  public static void Apply(Material m,Settings s,NishitaAtmosphere a){
   var tex=s.surface?s.surface:Resources.Load<Texture2D>("NASA/lroc_color_2k");var d=Direction(s,a);var right=Vector3.Cross(Vector3.up,d);if(right.sqrMagnitude<.001f)right=Vector3.right;right.Normalize();var up=Vector3.Cross(d,right).normalized;
   float night=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-12,0,a.Elevation));
   m.SetTexture("_MoonMap",tex);m.SetVector("_MoonDisc",Disc(s,a));m.SetVector("_MoonRight",right);m.SetVector("_MoonUp",up);
   m.SetVector("_MoonParams",new Vector4(s.phase,s.brightness,s.earthshine,tex?Visibility(s,a)*Mathf.Lerp(.12f,1,night):0));
   m.SetVector("_MoonGlow",new Vector4(s.glow,s.glowRadius,s.textureRotation*Mathf.Deg2Rad,0));m.SetColor("_MoonTint",s.tint.linear);
  }
 }
}
