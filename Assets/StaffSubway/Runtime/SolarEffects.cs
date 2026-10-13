using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Staff.Subway {
 public static class SolarEffects {
  [Serializable] public sealed class Settings {
   [Tooltip("F9 in Game view; editable in the Inspector outside Play Mode.")] public bool lensFlare=true;
   [InspectorName("Godrays (F10)")]
   [Tooltip("F10 master switch for sun and local shadow-map godrays.")] public bool sunRays=true;
   [Range(0,2)] public float flareIntensity=.55f;
   [Range(.05f,1)] public float glareSize=.3f;
   [Range(.001f,.02f)] public float flareSize=.003f;
   [Range(2,8)] public int starPoints=5;
   [Header("Independent flare spokes")]
   [Tooltip("Additive opacity of sun star rays; does not dim the central core or lens ghosts.")]
   [Range(0,1)] public float starRayOpacity=.65f;
   public bool animateStarPoints=true;
   [Range(0,1)] public float starIntensityVariation=.75f;
   [Range(0,.8f)] public float starAnimationAmount=.35f;
   [Range(0,3)] public float starAnimationSpeed=.7f;
   public int starAnimationSeed=2718;
   [Tooltip("Animate the flare in Edit Mode while it is enabled and the sun is above the horizon.")]
   public bool previewStarAnimation=true;
   [Range(.05f,1)] public float ghostScale=.25f;
   public bool secondaryGhosts=true;
   [Header("Lens ghosts")]
   [Range(0,12)] public int hexGhostCount=6;
   [Range(0,8)] public int roundGhostCount=3;
   [Tooltip("Additive visibility: 0 hides hex ghosts, 1 is full brightness.")]
   [Range(0,1)] public float hexGhostOpacity=1;
   [Tooltip("Additive visibility: 0 hides round ghosts, 1 is full brightness.")]
   [Range(0,1)] public float roundGhostOpacity=1;
   [Range(.25f,3)] public float hexGhostSize=1;
   [Range(.25f,3)] public float roundGhostSize=1;
   public bool anamorphic=false;
   [Tooltip("Art-direction gain, independent of previously saved density settings.")]
   [Range(0,1)] public float solarRayStrength=.3f;
   [Range(0,.04f)] public float rayDensity=.0078125f;
   [Range(0,.8f)] public float rayMaxOpacity=.3f;
   [Range(16,96)] public int raySteps=48;
   [Range(10,200)] public float rayDistance=80;
   [Range(0,4)] public float distanceAttenuation=2;
   [ColorUsage(false,true)] public Color rayColor=new Color(1,.9f,.7f);
   [Header("Train front/rear lamp rays — F10")]
   public bool localLights=true;
   [Tooltip("Only Body_Lamp emitters at the train front and rear.")]
   [Range(0,1)] public float trainEndRayStrength=.4f;
   // Kept only for serialized compatibility; these sources are no longer collected.
   [HideInInspector] public float interiorRayStrength=0;
   [HideInInspector] public bool fluorescentRays=false;
   [InspectorName("Train end lamps")] public bool trainEmissionRays=true;
   [Tooltip("Maximum local shadow cubes per camera. Each cube renders six faces.")]
   [Range(1,4)] public int localLightBudget=3;
   [Range(16,64)] public int localRaySteps=32;
   [Range(0,.3f)] public float localRayDensity=.025f;
   [Range(0,3)] public float localRayIntensity=.6f;
   [Range(0,2)] public float trainRayIntensity=.65f;
   [HideInInspector] public float trainLedRange=3;
   [Range(2,16)] public float trainLampRange=8;
   [Tooltip("Volumetric tint for textured train LEDs; does not change their surface emission.")]
   [HideInInspector] public Color trainLedColor=new Color(.12f,.4f,1);
   [ColorUsage(false,true)] public Color trainLampColor=new Color(1,.85f,.65f);
  }
  // Art-directed dawn/noon/sunset colors, independent of warm direct-sun chromaticity.
  public static Color SolarRayColor(Settings s,float elevation,float morning){
   Color warm=Color.Lerp(new Color(1,.12f,.04f),new Color(1,.68f,.16f),morning);
   Color day=new Color(.28f,.58f,1);
   return Color.Lerp(warm,day,Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,45,elevation))).linear*s.rayColor;
  }
  public static int StarSpokeCount(Settings s)=>s.anamorphic?2:Mathf.Clamp(s.starPoints,2,8)*2;
  static float StarHash(int id,int seed,int salt){unchecked{uint x=(uint)id*747796405u+(uint)seed*2891336453u+(uint)salt*277803737u;x=(x^(x>>16))*2246822519u;x=(x^(x>>13))*3266489917u;return ((x^(x>>16))&0x00ffffffu)/16777215f;}}
  public static void FillStarGains(Settings s,double seconds,float[] gains){
   int count=StarSpokeCount(s);double time=seconds*Mathf.Max(0,s.starAnimationSpeed)*1.25;float sum=0;
   for(int i=0;i<count;i++){
    float baseline=Mathf.Lerp(1,.2f+1.6f*StarHash(i,s.starAnimationSeed,1),Mathf.Clamp01(s.starIntensityVariation*1.25f));
    // Seeded smooth random targets: independent cadence and brightness for each spoke.
    double clock=time*(.55+.65*StarHash(i,s.starAnimationSeed,4))+StarHash(i,s.starAnimationSeed,2)*20;
    int segment=(int)Math.Floor(clock);float f=(float)(clock-Math.Floor(clock));f=f*f*(3-2*f);
    float value=Mathf.Lerp(StarHash(i,s.starAnimationSeed,segment+100),StarHash(i,s.starAnimationSeed,segment+101),f);
    float peak=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,1,value));
    float target=Mathf.Lerp(.04f,3.6f,peak*peak);
    float amount=s.animateStarPoints&&s.starAnimationSpeed>0?Mathf.Clamp01(s.starAnimationAmount*2.8f):0;
    gains[i]=baseline*Mathf.Lerp(1,target,amount);sum+=gains[i];
   }
   // Keep dark dwell intact: per-frame normalization would brighten every dim interval.
   for(int i=0;i<count;i++)gains[i]=Mathf.Clamp(gains[i],.015f,5);
   for(int i=count;i<gains.Length;i++)gains[i]=1;
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
