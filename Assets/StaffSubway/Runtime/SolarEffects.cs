using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Staff.Subway {
 public static class SolarEffects {
  [Serializable] public sealed class Settings {
   [Tooltip("F9 in Game view; editable in the Inspector outside Play Mode.")] public bool lensFlare=true;
   [Tooltip("F10 in Game view; shadow-map raymarched sunlight.")] public bool sunRays=true;
   [Range(0,2)] public float flareIntensity=.55f;
   [Range(.05f,1)] public float glareSize=.3f;
   [Range(.001f,.02f)] public float flareSize=.003f;
   [Range(2,8)] public int starPoints=5;
   [Range(.05f,1)] public float ghostScale=.25f;
   public bool secondaryGhosts=true;
   public bool anamorphic=false;
   [Range(0,.04f)] public float rayDensity=.0078125f;
   [Range(0,.8f)] public float rayMaxOpacity=.3f;
   [Range(16,96)] public int raySteps=48;
   [Range(10,200)] public float rayDistance=80;
   [Range(0,4)] public float distanceAttenuation=2;
   [ColorUsage(false,true)] public Color rayColor=new Color(1,.9f,.7f);
  }
  public static bool HandleInput(Settings s,bool accept) {
   if(!accept||Keyboard.current==null)return false;
   bool changed=false;
   if(Keyboard.current.f9Key.wasPressedThisFrame){s.lensFlare=!s.lensFlare;changed=true;}
   if(Keyboard.current.f10Key.wasPressedThisFrame){s.sunRays=!s.sunRays;changed=true;}
   return changed;
  }
 }
}
