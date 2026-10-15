using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
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
  static readonly List<SubwayTimeOfDay> instances=new List<SubwayTimeOfDay>();
  public static SubwayTimeOfDay Active=>instances.LastOrDefault(s=>s&&s.isActiveAndEnabled&&s.gameObject.scene==SceneManager.GetActiveScene());
  public static SubwayTimeOfDay ForCamera(Camera c)=>c&&c.cameraType!=CameraType.SceneView?instances.LastOrDefault(s=>s&&s.isActiveAndEnabled&&s.gameObject.scene==c.gameObject.scene):Active;
  public bool automaticSceneCameras;
  Camera[] boundCameras;
  Camera[] BoundCameras=>boundCameras??cameras;
  public double StarElapsedSeconds => weather?.ElapsedSeconds ?? 0;
  public Vector2 CloudDisplacement => weather?.CloudDisplacement ?? Vector2.zero;
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
  public int ContinuousModeIndex=>presets?.Length??0;
  public int ModeCount=>ContinuousModeIndex+1;
  public int Current=>timeTransition?targetMode:atmosphere&&atmosphere.useDayCycle&&atmosphere.animate?ContinuousModeIndex:current;
  public string Label=>Current==ContinuousModeIndex?"Continuous Solar Day":presets!=null&&presets.Length>0?presets[Mathf.Clamp(Current,0,presets.Length-1)].label:"";
  [Header("Clouds and celestial stars")]
  public NishitaSkyWeather.Settings skyWeather = new NishitaSkyWeather.Settings();
  [Header("Sun optics — F9 flare / F10 rays")]
  public SolarEffects.Settings solarEffects = new SolarEffects.Settings();
  [Header("Moon — NASA surface / phase / glow")]
  public CelestialMoon.Settings moon=new CelestialMoon.Settings();
  NishitaSkyWeather weather;
  double weatherClock;
  [Tooltip("Blend atmospheric density, exposure and added sky fill continuously during Solar Day.")]
  public bool continuousSolarLighting=true;
  Material disc, savedGlobalSky, appliedGlobalSky;
  [Range(-12,12)] public float exposure=-3;
  AdventurePlayer player;
  Material[] savedSky,savedAbstract;
  bool[] savedEnabled;
  CameraClearFlags[] savedFlags;
  float savedFillIntensity;Color savedFillColor;
  bool initializePending;
  void OnEnable(){
   if(!gameObject.scene.IsValid()||!gameObject.scene.isLoaded){initializePending=true;return;}
   InitializeSky();
  }
  void InitializeSky(){initializePending=false;
   if(!instances.Contains(this))instances.Add(this);
   boundCameras=automaticSceneCameras?gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Where(c=>c.cameraType==CameraType.Game).ToArray():cameras;
   weatherClock=Time.realtimeSinceStartupAsDouble;
   weather = new NishitaSkyWeather();
   savedGlobalSky=RenderSettings.skybox;
   player=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AdventurePlayer>()).FirstOrDefault();
   if(skyFill){savedFillIntensity=skyFill.intensity;savedFillColor=skyFill.color;}
   int count=BoundCameras==null?0:BoundCameras.Length;savedSky=new Material[count];savedAbstract=new Material[count];savedEnabled=new bool[count];savedFlags=new CameraClearFlags[count];
   for(int i=0;i<count;i++){var c=BoundCameras[i];if(!c)continue;var sky=c.GetComponent<Skybox>();if(!sky)sky=c.gameObject.AddComponent<Skybox>();savedSky[i]=sky.material;savedEnabled[i]=sky.enabled;savedFlags[i]=c.clearFlags;var mode=c.GetComponent<AbstractEnvironment>();if(mode)savedAbstract[i]=mode.abstractSkybox;}
   if(atmosphere)atmosphere.Tick(true);
   ApplySky();
  }
  void Update(){if(!Application.isPlaying)return;HandleInput(Application.isFocused&&player&&player.InputCaptured&&!player.InputBlocked);}
  public bool HandleInput(bool accept){bool changed=SolarEffects.HandleInput(solarEffects,accept);if(!accept||Keyboard.current==null||!Keyboard.current.tKey.wasPressedThisFrame)return changed;Cycle();return true;}
  [Min(.1f)] public float timeTransitionSeconds=2.5f;
  bool timeTransition;int targetMode;float transitionAge;Preset transitionFrom,transitionTo;
  float fillFrom,fillTo;Color fillColorFrom,fillColorTo;
  public bool IsTimeTransitioning=>timeTransition;
  public void Cycle(){if(presets!=null&&presets.Length>0){int next=(Current+1)%ModeCount;if(Application.isPlaying)TransitionToMode(next);else SetMode(next);}}
  Preset Snapshot()=>new Preset("",atmosphere.Elevation,Mathf.Repeat(Mathf.Atan2(atmosphere.SunDirection.x,atmosphere.SunDirection.z)*Mathf.Rad2Deg,360),atmosphere.airDensity,atmosphere.aerosolDensity,atmosphere.ozoneDensity,atmosphere.brightness,exposure);
  public void TransitionToMode(int index){
   if(!atmosphere||presets==null||presets.Length==0)return;
   var from=Snapshot();float fi=skyFill?skyFill.intensity:0;Color fc=skyFill?skyFill.color:Color.white;
   // Evaluate the destination without touching live atmosphere/LUT history.
   index=Mathf.Clamp(index,0,ContinuousModeIndex);
   if(index<ContinuousModeIndex){transitionTo=presets[index];fillTo=index==0?.22f:index==1?.35f:index==2?.16f:.02f;fillColorTo=index==0?new Color(1,.88f,.65f):index==1?new Color(.85f,.92f,1):index==2?new Color(1,.64f,.48f):new Color(.5f,.63f,1);}
   else{
    var a=atmosphere;var dir=NishitaAtmosphere.SolarDirection(a.timeOfDay,a.latitude,a.solarDeclination,a.northRotation);
    float elevation=Mathf.Asin(Mathf.Clamp(dir.y,-1,1))*Mathf.Rad2Deg;
    float morning=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.2f,.2f,(Quaternion.Euler(0,-a.northRotation,0)*dir).x));
    float day=Mathf.SmoothStep(0,1,Mathf.InverseLerp(8,45,elevation)),awake=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-12,2,elevation));
    var dawn=presets[0];var noon=presets[1];var dusk=presets[2];var night=presets[3];
    transitionTo=new Preset("",elevation,Mathf.Repeat(Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg,360),BlendDay(night.air,dawn.air,noon.air,dusk.air,morning,day,awake),BlendDay(night.aerosols,dawn.aerosols,noon.aerosols,dusk.aerosols,morning,day,awake),a.ozoneDensity,BlendDay(night.brightness,dawn.brightness,noon.brightness,dusk.brightness,morning,day,awake),BlendDay(night.exposure,dawn.exposure,noon.exposure,dusk.exposure,morning,day,awake));
    fillTo=BlendDay(.02f,.22f,.35f,.16f,morning,day,awake);fillColorTo=Color.Lerp(new Color(.5f,.63f,1),Color.Lerp(Color.Lerp(new Color(1,.64f,.48f),new Color(1,.88f,.65f),morning),new Color(.85f,.92f,1),day),awake);
   }
   transitionFrom=from;fillFrom=fi;fillColorFrom=fc;targetMode=index;transitionAge=0;timeTransition=true;ApplyTransition(0);
  }
  void ApplyTransition(float t){
   var a=atmosphere;var f=transitionFrom;var to=transitionTo;a.animate=false;a.useDayCycle=false;
   // Forward azimuth travels through the sky instead of cross-fading sky images.
   a.sunRotation=Mathf.Repeat(f.rotation+Mathf.Repeat(to.rotation-f.rotation,360)*t,360);a.sunElevation=Mathf.Lerp(f.elevation,to.elevation,t);
   a.airDensity=Mathf.Lerp(f.air,to.air,t);a.aerosolDensity=Mathf.Lerp(f.aerosols,to.aerosols,t);a.ozoneDensity=Mathf.Lerp(f.ozone,to.ozone,t);a.brightness=Mathf.Lerp(f.brightness,to.brightness,t);exposure=Mathf.Lerp(f.exposure,to.exposure,t);a.sunDisc=true;
   if(skyFill){skyFill.intensity=Mathf.Lerp(fillFrom,fillTo,t);skyFill.color=Color.Lerp(fillColorFrom,fillColorTo,t);}a.Tick(false);
  }
  public void AdvanceTimeTransition(float dt){if(!timeTransition)return;transitionAge+=Mathf.Max(0,dt);float t=Mathf.Clamp01(transitionAge/Mathf.Max(.1f,timeTransitionSeconds));ApplyTransition(Mathf.SmoothStep(0,1,t));if(t>=1){int target=targetMode;timeTransition=false;SetMode(target);}}

  public void SetMode(int index){
   timeTransition=false;
   if(!atmosphere||presets==null||presets.Length==0)return;
   if(index!=ContinuousModeIndex){SetPreset(index);return;}
   // Resume the authored solar clock; fixed presets never overwrite Time Of Day.
   atmosphere.useDayCycle=true;atmosphere.animate=true;continuousSolarLighting=true;
   ApplySolarDay();atmosphere.Tick(true);ApplySky();
  }
  public void SetPreset(int index){
   timeTransition=false;
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
  void LateUpdate(){if(initializePending){if(!gameObject.scene.IsValid()||!gameObject.scene.isLoaded)return;InitializeSky();}if(Application.isPlaying)AdvanceTimeTransition(Time.unscaledDeltaTime);double now=Time.realtimeSinceStartupAsDouble;weather?.Advance(Application.isPlaying?Time.deltaTime*(timeTransition?12:1):(float)Math.Max(0,now-weatherClock),skyWeather);weatherClock=now;ApplySolarDay();ApplySky();}
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
   var real=atmosphere.GetSky(true);CelestialMoon.Apply(real,moon,atmosphere);
   float lowSun=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,20,atmosphere.Elevation));
   float west=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.2f,.2f,(Quaternion.Euler(0,-atmosphere.northRotation,0)*atmosphere.SunDirection).x));
   real.SetColor("_StaffSunTint",Color.Lerp(Color.white,new Color(1,.78f,.18f),lowSun));
   real.SetFloat("_StaffSunset",west*lowSun*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-12,-2,atmosphere.Elevation)));real.SetTexture("_Lut",agxLut);real.SetFloat("_Exposure",exposure);real.SetFloat("_DiscOnly",0);
   weather?.Apply(real,skyWeather,atmosphere.latitude,atmosphere.northRotation,atmosphere.Elevation);
   if(!disc)disc=new Material(real){name="Subway solar disc",hideFlags=HideFlags.HideAndDontSave};
   disc.CopyPropertiesFromMaterial(real);disc.SetFloat("_DiscOnly",1);
   if(Active==this){RenderSettings.skybox=real;appliedGlobalSky=real;}
   if(BoundCameras==null)return;
   foreach(var c in BoundCameras){if(!c)continue;var mode=c.GetComponent<AbstractEnvironment>();if(mode)mode.abstractSkybox=disc;var sky=c.GetComponent<Skybox>();if(!sky)continue;sky.enabled=true;sky.material=mode&&mode.IsAbstract&&!(mode.IsEntering&&mode.Mode==AbstractEnvironment.EnvironmentMode.Real)?disc:real;c.clearFlags=CameraClearFlags.Skybox;}
  }
  public void ApplyToCamera(Camera c){
   if(!c||!atmosphere||!atmosphere.Ready)return;
   var mode=c.GetComponent<AbstractEnvironment>();var box=c.GetComponent<Skybox>();if(!box)box=c.gameObject.AddComponent<Skybox>();
   if(boundCameras==null||!boundCameras.Contains(c)){
    int count=boundCameras==null?0:boundCameras.Length;Array.Resize(ref boundCameras,count+1);Array.Resize(ref savedSky,count+1);Array.Resize(ref savedAbstract,count+1);Array.Resize(ref savedEnabled,count+1);Array.Resize(ref savedFlags,count+1);
    boundCameras[count]=c;savedSky[count]=box.material;savedAbstract[count]=mode?mode.abstractSkybox:null;savedEnabled[count]=box.enabled;savedFlags[count]=c.clearFlags;
   }
   box.enabled=true;box.material=mode&&mode.IsAbstract&&!(mode.IsEntering&&mode.Mode==AbstractEnvironment.EnvironmentMode.Real)?disc:atmosphere.GetSky(true);c.clearFlags=CameraClearFlags.Skybox;
  }
  // Editor save hooks restore serialized references before saving transient preview materials.
  public void RestorePreviewBindings(){
   if(appliedGlobalSky&&RenderSettings.skybox==appliedGlobalSky)RenderSettings.skybox=savedGlobalSky;
   if(BoundCameras!=null&&savedSky!=null)for(int i=0;i<BoundCameras.Length&&i<savedSky.Length;i++){var c=BoundCameras[i];if(!c)continue;var mode=c.GetComponent<AbstractEnvironment>();if(mode){mode.abstractSkybox=savedAbstract[i];}var sky=c.GetComponent<Skybox>();if(sky){sky.material=savedSky[i];sky.enabled=savedEnabled[i];}c.clearFlags=savedFlags[i];}
  }
  void OnDisable(){
   RestorePreviewBindings();
   instances.Remove(this);
   weather?.Dispose();weather=null;
   if(skyFill){skyFill.intensity=savedFillIntensity;skyFill.color=savedFillColor;}
   if(disc){if(Application.isPlaying)Destroy(disc);else DestroyImmediate(disc);}disc=null;
  }
 }
}
