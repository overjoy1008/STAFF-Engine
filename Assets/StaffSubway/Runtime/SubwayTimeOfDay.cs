using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Staff.Characters;
using Staff.Nishita;
namespace Staff.Subway {
 [DefaultExecutionOrder(100),DisallowMultipleComponent]
 public sealed class SubwayTimeOfDay:MonoBehaviour {
  [Serializable] public struct Preset {
   public string label;
   public float elevation,rotation,air,aerosols,ozone,brightness,exposure;
   public Preset(string label,float elevation,float rotation,float air,float aerosols,float ozone,float brightness,float exposure){this.label=label;this.elevation=elevation;this.rotation=rotation;this.air=air;this.aerosols=aerosols;this.ozone=ozone;this.brightness=brightness;this.exposure=exposure;}
  }
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
  [SerializeField] int current=0;
  public int Current=>current;
  public string Label=>presets!=null&&presets.Length>0?presets[current].label:"";
  [Header("Clouds and celestial stars")]
  public NishitaSkyWeather.Settings skyWeather = new NishitaSkyWeather.Settings();
  NishitaSkyWeather weather;
  Material disc;
  AdventurePlayer player;
  Material[] savedSky,savedAbstract;
  bool[] savedEnabled;
  CameraClearFlags[] savedFlags;
  float savedFillIntensity;Color savedFillColor;
  void OnEnable(){
   weather = new NishitaSkyWeather();
   player=FindFirstObjectByType<AdventurePlayer>();
   if(skyFill){savedFillIntensity=skyFill.intensity;savedFillColor=skyFill.color;}
   int count=cameras==null?0:cameras.Length;savedSky=new Material[count];savedAbstract=new Material[count];savedEnabled=new bool[count];savedFlags=new CameraClearFlags[count];
   for(int i=0;i<count;i++){var c=cameras[i];if(!c)continue;var sky=c.GetComponent<Skybox>();if(!sky)sky=c.gameObject.AddComponent<Skybox>();savedSky[i]=sky.material;savedEnabled[i]=sky.enabled;savedFlags[i]=c.clearFlags;var mode=c.GetComponent<AbstractEnvironment>();if(mode)savedAbstract[i]=mode.abstractSkybox;}
   SetPreset(current);
  }
  void Update(){HandleInput(Application.isFocused&&player&&player.InputCaptured&&!player.InputBlocked);}
  public bool HandleInput(bool accept){if(!accept||Keyboard.current==null||!Keyboard.current.tKey.wasPressedThisFrame)return false;Cycle();return true;}
  public void Cycle()=>SetPreset((current+1)%presets.Length);
  public void SetPreset(int index){
   if(!atmosphere||presets==null||presets.Length==0)return;
   current=Mathf.Clamp(index,0,presets.Length-1);var p=presets[current];
   atmosphere.animate=false;atmosphere.useDayCycle=false;
   atmosphere.airDensity=p.air;atmosphere.aerosolDensity=p.aerosols;atmosphere.ozoneDensity=p.ozone;
   atmosphere.altitude=1523;atmosphere.sunRadius=.941f;atmosphere.sunIntensity=1.22f;
   atmosphere.brightness=p.brightness;atmosphere.sunElevation=p.elevation;atmosphere.sunRotation=p.rotation;atmosphere.sunDisc=p.elevation>=0;
   atmosphere.Tick(true);ApplySky();
   // Only the added sky fill varies. Fluorescents, station fill and reflection remain intact.
   if(skyFill){skyFill.intensity=current==0?.22f:current==1?.35f:current==2?.16f:.02f;skyFill.color=current==0?new Color(1,.88f,.65f):current==1?new Color(.85f,.92f,1):current==2?new Color(1,.64f,.48f):new Color(.5f,.63f,1);}
  }
  void LateUpdate(){weather?.Advance(Time.deltaTime,skyWeather);ApplySky();}
  public void ApplySky(){
   if(!atmosphere||!atmosphere.Ready||!agxLut)return;
   var real=atmosphere.GetSky(true);real.SetTexture("_Lut",agxLut);real.SetFloat("_Exposure",presets[current].exposure);real.SetFloat("_DiscOnly",0);
   weather?.Apply(real,skyWeather,atmosphere.latitude,atmosphere.northRotation,atmosphere.Elevation);
   if(!disc)disc=new Material(real){name="Subway solar disc",hideFlags=HideFlags.HideAndDontSave};
   disc.CopyPropertiesFromMaterial(real);disc.SetFloat("_DiscOnly",1);
   if(cameras==null)return;
   foreach(var c in cameras){if(!c)continue;var mode=c.GetComponent<AbstractEnvironment>();if(mode)mode.abstractSkybox=disc;var sky=c.GetComponent<Skybox>();sky.enabled=true;sky.material=mode&&mode.IsAbstract?disc:real;c.clearFlags=CameraClearFlags.Skybox;}
  }
  void OnDisable(){
   weather?.Dispose();weather=null;
   if(cameras!=null&&savedSky!=null)for(int i=0;i<cameras.Length&&i<savedSky.Length;i++){var c=cameras[i];if(!c)continue;var mode=c.GetComponent<AbstractEnvironment>();if(mode){mode.SetMode(AbstractEnvironment.EnvironmentMode.Real);mode.abstractSkybox=savedAbstract[i];}var sky=c.GetComponent<Skybox>();if(sky){sky.material=savedSky[i];sky.enabled=savedEnabled[i];}c.clearFlags=savedFlags[i];}
   if(skyFill){skyFill.intensity=savedFillIntensity;skyFill.color=savedFillColor;}
   if(disc)Destroy(disc);disc=null;
  }
 }
}
