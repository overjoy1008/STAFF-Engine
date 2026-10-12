using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Staff.Characters;
using Staff.Nishita;
namespace Staff.Subway {
 [ExecuteAlways,DefaultExecutionOrder(100),DisallowMultipleComponent]
 public sealed class SubwayTimeOfDay:MonoBehaviour {
  [Serializable] public struct Preset {
   public string label;
   public float elevation,rotation,air,aerosols,ozone,brightness,exposure;
   public Preset(string label,float elevation,float rotation,float air,float aerosols,float ozone,float brightness,float exposure){this.label=label;this.elevation=elevation;this.rotation=rotation;this.air=air;this.aerosols=aerosols;this.ozone=ozone;this.brightness=brightness;this.exposure=exposure;}
  }
  public static SubwayTimeOfDay Active { get; private set; }
  public double StarElapsedSeconds => weather?.ElapsedSeconds ?? 0;
  public NishitaAtmosphere atmosphere;
  public Texture2D agxLut;
  public Camera[] cameras;
  public Light skyFill;
  public Preset[] presets={
   new Preset("Yellow sunrise",5.5f,92.2f,1.6f,1.8f,.6f,.27f,-2),
   new Preset("Blue noon",60,180,1.2f,.3f,2.5f,.35f,-3),
   new Preset("Red sunset",.8f,267.8f,1.8f,4.5f,1.1f,.27f,-1.5f),
   new Preset("Night",-12,300,1.8f,1,2.92f,.27f,-2)
  };
  [SerializeField,HideInInspector] int current=1;
  public int Current=>current;
  public string Label=>presets!=null&&presets.Length>0?presets[Mathf.Clamp(current,0,presets.Length-1)].label:"";
  [Header("Clouds and celestial stars")]
  public NishitaSkyWeather.Settings skyWeather = new NishitaSkyWeather.Settings();
  [Header("Sun optics — F9 flare / F10 rays")]
  public SolarEffects.Settings solarEffects = new SolarEffects.Settings();
  NishitaSkyWeather weather;
  double weatherClock;
  [Tooltip("Blend atmospheric density, exposure and added sky fill continuously during Solar Day.")]
  public bool continuousSolarLighting=true;
  Material disc, savedGlobalSky;
  [Range(-12,12)] public float exposure=-3;
  AdventurePlayer player;
  Material[] savedSky,savedAbstract;
  bool[] savedEnabled;
  CameraClearFlags[] savedFlags;
  float savedFillIntensity;Color savedFillColor;
  void OnEnable(){
   Active=this;weatherClock=Time.realtimeSinceStartupAsDouble;
   weather = new NishitaSkyWeather();
   savedGlobalSky=RenderSettings.skybox;
   player=FindFirstObjectByType<AdventurePlayer>();
   if(skyFill){savedFillIntensity=skyFill.intensity;savedFillColor=skyFill.color;}
   int count=cameras==null?0:cameras.Length;savedSky=new Material[count];savedAbstract=new Material[count];savedEnabled=new bool[count];savedFlags=new CameraClearFlags[count];
   for(int i=0;i<count;i++){var c=cameras[i];if(!c)continue;var sky=c.GetComponent<Skybox>();if(!sky)sky=c.gameObject.AddComponent<Skybox>();savedSky[i]=sky.material;savedEnabled[i]=sky.enabled;savedFlags[i]=c.clearFlags;var mode=c.GetComponent<AbstractEnvironment>();if(mode)savedAbstract[i]=mode.abstractSkybox;}
   if(atmosphere)atmosphere.Tick(true);
   ApplySky();
  }
  void Update(){if(!Application.isPlaying)return;HandleInput(Application.isFocused&&player&&player.InputCaptured&&!player.InputBlocked);}
  public bool HandleInput(bool accept){bool changed=SolarEffects.HandleInput(solarEffects,accept);if(!accept||Keyboard.current==null||!Keyboard.current.tKey.wasPressedThisFrame)return changed;Cycle();return true;}
  public void Cycle(){if(presets!=null&&presets.Length>0)SetPreset((current+1)%presets.Length);}
  public void SetPreset(int index){
   if(!atmosphere||presets==null||presets.Length==0)return;
   current=Mathf.Clamp(index,0,presets.Length-1);var p=presets[current];
   atmosphere.animate=false;atmosphere.useDayCycle=false;
   atmosphere.airDensity=p.air;atmosphere.aerosolDensity=p.aerosols;atmosphere.ozoneDensity=p.ozone;
   exposure=p.exposure;
   atmosphere.brightness=p.brightness;atmosphere.sunElevation=p.elevation;atmosphere.sunRotation=p.rotation;atmosphere.sunDisc=p.elevation>=0;
   atmosphere.Tick(true);ApplySky();
   // Only the added sky fill varies. Fluorescents, station fill and reflection remain intact.
   if(skyFill){skyFill.intensity=current==0?.22f:current==1?.35f:current==2?.16f:.02f;skyFill.color=current==0?new Color(1,.88f,.65f):current==1?new Color(.85f,.92f,1):current==2?new Color(1,.64f,.48f):new Color(.5f,.63f,1);}
  }
  void LateUpdate(){double now=Time.realtimeSinceStartupAsDouble;weather?.Advance(Application.isPlaying?Time.deltaTime:(float)Math.Min(.2,Math.Max(0,now-weatherClock)),skyWeather);weatherClock=now;ApplySolarDay();ApplySky();}
  public void ApplySolarDay(){
   var a=atmosphere;if(!a||!a.useDayCycle||!continuousSolarLighting||presets==null||presets.Length<4)return;
   // All transitions are evaluated from the same continuous sun direction, never preset index.
   var direction=NishitaAtmosphere.SolarDirection(a.timeOfDay,a.latitude,a.solarDeclination,a.northRotation);
   float elevation=Mathf.Asin(Mathf.Clamp(direction.y,-1,1))*Mathf.Rad2Deg;
   float morning=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.2f,.2f,(Quaternion.Euler(0,-a.northRotation,0)*direction).x));
   float day=Mathf.SmoothStep(0,1,Mathf.InverseLerp(8,45,elevation));
   float awake=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-12,2,elevation));
   var dawn=presets[0];var noon=presets[1];var dusk=presets[2];var night=presets[3];
   a.airDensity=BlendDay(night.air,dawn.air,noon.air,dusk.air,morning,day,awake);
   a.aerosolDensity=BlendDay(night.aerosols,dawn.aerosols,noon.aerosols,dusk.aerosols,morning,day,awake);
   a.brightness=BlendDay(night.brightness,dawn.brightness,noon.brightness,dusk.brightness,morning,day,awake);
   exposure=BlendDay(night.exposure,dawn.exposure,noon.exposure,dusk.exposure,morning,day,awake);
   // The shader clips the disc at the horizon; a night preset must not latch it off all day.
   a.sunDisc=true;
   if(skyFill){skyFill.intensity=BlendDay(.02f,.22f,.35f,.16f,morning,day,awake);skyFill.color=Color.Lerp(new Color(.5f,.63f,1),Color.Lerp(Color.Lerp(new Color(1,.64f,.48f),new Color(1,.88f,.65f),morning),new Color(.85f,.92f,1),day),awake);}
   a.Tick(false);
  }
  static float BlendDay(float night,float dawn,float noon,float dusk,float morning,float day,float awake){return Mathf.Lerp(night,Mathf.Lerp(Mathf.Lerp(dusk,dawn,morning),noon,day),awake);}
  public void ApplySky(){
   if(!atmosphere||!atmosphere.Ready||!agxLut)return;
   var real=atmosphere.GetSky(true);
   float lowSun=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,20,atmosphere.Elevation));
   float west=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.2f,.2f,(Quaternion.Euler(0,-atmosphere.northRotation,0)*atmosphere.SunDirection).x));
   real.SetColor("_StaffSunTint",Color.Lerp(Color.white,new Color(1,.78f,.18f),lowSun));
   real.SetFloat("_StaffSunset",west*lowSun*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-12,-2,atmosphere.Elevation)));real.SetTexture("_Lut",agxLut);real.SetFloat("_Exposure",exposure);real.SetFloat("_DiscOnly",0);
   weather?.Apply(real,skyWeather,atmosphere.latitude,atmosphere.northRotation,atmosphere.Elevation);
   if(!disc)disc=new Material(real){name="Subway solar disc",hideFlags=HideFlags.HideAndDontSave};
   disc.CopyPropertiesFromMaterial(real);disc.SetFloat("_DiscOnly",1);
   RenderSettings.skybox=real;
   if(cameras==null)return;
   foreach(var c in cameras){if(!c)continue;var mode=c.GetComponent<AbstractEnvironment>();if(mode)mode.abstractSkybox=disc;var sky=c.GetComponent<Skybox>();if(!sky)continue;sky.enabled=true;sky.material=mode&&mode.IsAbstract?disc:real;c.clearFlags=CameraClearFlags.Skybox;}
  }
  // Editor save hooks restore serialized references before saving transient preview materials.
  public void RestorePreviewBindings(){
   RenderSettings.skybox=savedGlobalSky;
   if(cameras!=null&&savedSky!=null)for(int i=0;i<cameras.Length&&i<savedSky.Length;i++){var c=cameras[i];if(!c)continue;var mode=c.GetComponent<AbstractEnvironment>();if(mode){mode.abstractSkybox=savedAbstract[i];}var sky=c.GetComponent<Skybox>();if(sky){sky.material=savedSky[i];sky.enabled=savedEnabled[i];}c.clearFlags=savedFlags[i];}
  }
  void OnDisable(){
   if(Active==this)Active=null;
   RestorePreviewBindings();
   weather?.Dispose();weather=null;
   if(skyFill){skyFill.intensity=savedFillIntensity;skyFill.color=savedFillColor;}
   if(disc){if(Application.isPlaying)Destroy(disc);else DestroyImmediate(disc);}disc=null;
  }
 }
}
