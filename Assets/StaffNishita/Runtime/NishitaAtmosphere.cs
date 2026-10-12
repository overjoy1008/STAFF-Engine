using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace Staff.Nishita
{
 [ExecuteAlways,DisallowMultipleComponent]
 public sealed class NishitaAtmosphere:MonoBehaviour
 {
  [Header("Blender Sky Texture inputs")]
  [Range(0,1000)] public float airDensity=1,aerosolDensity=1,ozoneDensity=1;
  [Tooltip("Meters above sea level. Blender clamps Single to 59999m and Multiple to 99999m.")]
  [Range(0,100000)] public float altitude=0;
  public bool sunDisc=true;
  [Tooltip("Angular radius in degrees. Blender Sun Size is twice this value.")]
  [Range(0,45)] public float sunRadius=.2725f;
  [Min(0)] public float sunIntensity=1,brightness=1;
  [Range(-90,90)] public float sunElevation=5;
  [Range(0,360)] public float sunRotation=75;
  [Header("Solar day - local solar time, not civil clock time")]
  public bool useDayCycle=true,animate=true;
  [Range(0,24)] public float timeOfDay=17.5f;
  [Min(10)] public float dayDurationSeconds=240;
  [Range(-89,89)] public float latitude=37;
  [Range(-23.44f,23.44f)] public float solarDeclination=0;
  [Range(0,360)] public float northRotation=0;
  [Header("Elevation art direction")]
  public bool elevationSunSize=false;
  [Range(1,1.5f)] public float horizonRadiusMultiplier=1.18f;
  [Range(1,45)] public float horizonRadiusFadeDegrees=20;
  [Tooltip("When enabled, the elevation curve overrides the manual Ozone Density input.")]
  public bool elevationOzone=false;
  [Range(0,1000)] public float horizonOzone=.6f,noonOzone=45;
  [Range(0,1000)] public float middleOzone=10;
  [Range(1,89)] public float ozoneMiddleDegrees=20;
  [Range(0,80)] public float ozoneStartDegrees=10;
  [Range(1,90)] public float ozoneFullDegrees=60;
  public float EffectiveSunRadius=>sunRadius*(elevationSunSize?Mathf.Lerp(Mathf.Clamp(horizonRadiusMultiplier,1,1.5f),1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,Mathf.Max(1,horizonRadiusFadeDegrees),effectiveElevation))):1);
  public float EffectiveOzone {get {
   if(!elevationOzone)return ozoneDensity;
   float mid=Mathf.Max(ozoneStartDegrees+.1f,ozoneMiddleDegrees),end=Mathf.Max(mid+.1f,ozoneFullDegrees);
   if(effectiveElevation<=mid)return Mathf.Lerp(horizonOzone,middleOzone,Mathf.SmoothStep(0,1,Mathf.InverseLerp(ozoneStartDegrees,mid,effectiveElevation)));
   return Mathf.Lerp(middleOzone,noonOzone,Mathf.SmoothStep(0,1,Mathf.InverseLerp(mid,end,effectiveElevation)));
  }}
  [Header("GPU quality")]
  [Range(64,1024)] public int skyWidth=256;
  [Range(.02f,.5f)] public float updateInterval=.0667f;
  public ComputeShader singleCompute,multipleCompute,lightingCompute;
  public Shader skyShader;
  public bool Ready=>single!=null&&multiple!=null&&single.material&&multiple.material;
  public int RebuildCount{get;private set;}
  public string Status{get;private set;}="Starting atmosphere";
  public Vector3 SunDirection{get;private set;}
  public float Elevation=>effectiveElevation;
  public float Rotation=>effectiveRotation;
  float effectiveElevation,effectiveRotation;
  Model single,multiple;
  Vector4 lastDensity=new Vector4(-1,-1,-1,-1);
  Vector2 lastSun=new Vector2(-999,-999);
  double lastUpdate=-100,lastClock;
  int generation;
  class Model {
   public ComputeShader compute,lighting;
   public RenderTexture current,previous,transmittance;
   public ComputeBuffer sun;
   public Material material;
   public Color skyColor=Color.black,horizonColor=Color.black,directColor=Color.black;
   public bool pending;
   public bool multiple;
  }
  void OnEnable(){lastClock=Time.realtimeSinceStartupAsDouble;lastUpdate=-100;}
  void OnDisable(){Release();}
  void Update(){
   double now=Time.realtimeSinceStartupAsDouble;
   if(useDayCycle&&animate)timeOfDay=Mathf.Repeat(timeOfDay+(float)Math.Min(now-lastClock,.2)*24/Mathf.Max(dayDurationSeconds,10),24);
   lastClock=now;Tick(false);
  }
  public static Vector3 SolarDirection(float hour,float lat,float declination,float north){
   float h=(hour-12)*15*Mathf.Deg2Rad,p=lat*Mathf.Deg2Rad,d=declination*Mathf.Deg2Rad;
   Vector3 dir=new Vector3(-Mathf.Cos(d)*Mathf.Sin(h),Mathf.Sin(p)*Mathf.Sin(d)+Mathf.Cos(p)*Mathf.Cos(d)*Mathf.Cos(h),Mathf.Cos(p)*Mathf.Sin(d)-Mathf.Sin(p)*Mathf.Cos(d)*Mathf.Cos(h));
   return Quaternion.Euler(0,north,0)*dir.normalized;
  }
  public void Tick(bool force){
   airDensity=Mathf.Clamp(airDensity,0,1000);aerosolDensity=Mathf.Clamp(aerosolDensity,0,1000);ozoneDensity=Mathf.Clamp(ozoneDensity,0,1000);
   altitude=Mathf.Clamp(altitude,0,100000);sunRadius=Mathf.Clamp(sunRadius,0,45);sunIntensity=Mathf.Max(0,sunIntensity);brightness=Mathf.Max(0,brightness);
   if(useDayCycle){SunDirection=SolarDirection(timeOfDay,latitude,solarDeclination,northRotation);effectiveElevation=Mathf.Asin(Mathf.Clamp(SunDirection.y,-1,1))*Mathf.Rad2Deg;effectiveRotation=Mathf.Repeat(Mathf.Atan2(SunDirection.x,SunDirection.z)*Mathf.Rad2Deg,360);}
   else {effectiveElevation=Mathf.Clamp(sunElevation,-90,90);effectiveRotation=Mathf.Repeat(sunRotation,360);SunDirection=Quaternion.Euler(-effectiveElevation,effectiveRotation,0)*Vector3.forward;}
   if(!EnsureResources())return;
   var density=new Vector4(airDensity,aerosolDensity,EffectiveOzone,altitude);
   var solar=new Vector2(effectiveElevation,EffectiveSunRadius);
   double now=Time.realtimeSinceStartupAsDouble;
   bool dirty=density!=lastDensity||solar!=lastSun;
   if(force||(dirty&&now-lastUpdate>=updateInterval)){
    bool opticalDirty=density.x!=lastDensity.x||density.y!=lastDensity.y||density.z!=lastDensity.z;
    bool first=lastUpdate<0;
    Render(single,opticalDirty,first);Render(multiple,opticalDirty,first);
    lastDensity=density;lastSun=solar;lastUpdate=now;RebuildCount++;
    Status="Live GPU atmosphere";
   }
   float blend=force?1:Mathf.Clamp01((float)(now-lastUpdate)/Mathf.Max(updateInterval,.02f));
   ConfigureMaterial(single,blend);ConfigureMaterial(multiple,blend);
  }
  bool EnsureResources(){
   if(!SystemInfo.supportsComputeShaders||!singleCompute||!multipleCompute||!lightingCompute||!skyShader){Status="Compute shaders unavailable - reference skies only";return false;}
   int width=Mathf.Clamp(Mathf.RoundToInt(skyWidth/16f)*16,64,1024);
   if(Ready&&single.current.width==width)return true;
   Release();single=CreateModel(false,width);multiple=CreateModel(true,width);lastUpdate=-100;lastDensity=new Vector4(-1,-1,-1,-1);return true;
  }
  static RenderTexture Texture(string name,int width,int height,RenderTextureFormat format){
   var rt=new RenderTexture(width,height,0,format,RenderTextureReadWrite.Linear){name=name,enableRandomWrite=true,useMipMap=false,autoGenerateMips=false,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};rt.Create();return rt;
  }
  Model CreateModel(bool isMultiple,int width){
   var m=new Model{multiple=isMultiple,compute=Instantiate(isMultiple?multipleCompute:singleCompute),lighting=Instantiate(lightingCompute)};
   m.compute.hideFlags=m.lighting.hideFlags=HideFlags.HideAndDontSave;
   m.current=Texture("Live sky",width,width/2,RenderTextureFormat.ARGBFloat);m.previous=Texture("Previous live sky",width,width/2,RenderTextureFormat.ARGBFloat);
   if(isMultiple)m.transmittance=Texture("Spectral transmittance",256,64,RenderTextureFormat.ARGBFloat);
   m.sun=new ComputeBuffer(5,sizeof(float)*4);m.sun.SetData(new Vector4[5]);
   m.material=new Material(skyShader){name=isMultiple?"Multiple Scattering - live":"Single Scattering - live",hideFlags=HideFlags.HideAndDontSave};return m;
  }
  void Parameters(ComputeShader c,int width){
   c.SetInt("_Width",width);c.SetInt("_Height",width/2);c.SetFloat("_Elevation",effectiveElevation*Mathf.Deg2Rad);
   c.SetFloat("_Altitude",altitude);c.SetFloat("_Air",airDensity);c.SetFloat("_Aerosols",aerosolDensity);c.SetFloat("_Ozone",EffectiveOzone);c.SetFloat("_Diameter",EffectiveSunRadius*2*Mathf.Deg2Rad);
  }
  float Horizon(Model m){float radius=m.multiple?6371000:6360000;float h=Mathf.Clamp(altitude,1,m.multiple?99999:59999);return -Mathf.Acos(radius/(radius+h));}
  void Render(Model m,bool opticalDirty,bool first){
   var swap=m.previous;m.previous=m.current;m.current=swap;
   int w=m.current.width;Parameters(m.compute,w);
   if(m.multiple&&opticalDirty){int k=m.compute.FindKernel("GenerateTransmittance");m.compute.SetTexture(k,"_Transmittance",m.transmittance);m.compute.Dispatch(k,32,8,1);}
   int sky=m.compute.FindKernel("GenerateSky");m.compute.SetTexture(sky,"_Sky",m.current);
   if(m.multiple)m.compute.SetTexture(sky,"_Transmittance",m.transmittance);
   m.compute.Dispatch(sky,(w/2+7)/8,((m.multiple?w/2:w/4)+7)/8,1);
   if(!m.multiple){int lower=m.compute.FindKernel("FillLower");m.compute.SetTexture(lower,"_Sky",m.current);m.compute.Dispatch(lower,(w+7)/8,(w/4+7)/8,1);}
   int sun=m.compute.FindKernel("GenerateSun");m.compute.SetBuffer(sun,"_SunData",m.sun);m.compute.Dispatch(sun,1,1,1);
   Parameters(m.lighting,w);m.lighting.SetFloat("_Horizon",Horizon(m));int light=m.lighting.FindKernel("GatherLighting");m.lighting.SetTexture(light,"_Sky",m.current);m.lighting.SetBuffer(light,"_SunData",m.sun);m.lighting.Dispatch(light,1,1,1);
   if(first)Graphics.CopyTexture(m.current,m.previous);
   if(!m.pending&&SystemInfo.supportsAsyncGPUReadback){
    m.pending=true;int token=generation;
    AsyncGPUReadback.Request(m.sun,r=>{if(token!=generation||!this)return;m.pending=false;if(r.hasError)return;var data=r.GetData<Vector4>();ApplyLighting(m,data[2],data[3],data[4]);});
   }
  }
  static void ApplyLighting(Model m,Vector4 sky,Vector4 horizon,Vector4 direct){m.skyColor=new Color(sky.x,sky.y,sky.z);m.horizonColor=new Color(horizon.x,horizon.y,horizon.z);m.directColor=new Color(direct.x,direct.y,direct.z);}
  void ConfigureMaterial(Model m,float blend){
   var mat=m.material;mat.SetTexture("_SkyCurrent",m.current);mat.SetTexture("_SkyPrevious",m.previous);mat.SetBuffer("_SunData",m.sun);
   mat.SetVector("_LutSize",new Vector4(m.current.width,m.current.height,0,0));mat.SetVector("_SunDir",SunDirection);
   mat.SetFloat("_Rotation",effectiveRotation*Mathf.Deg2Rad);mat.SetFloat("_Elevation",effectiveElevation*Mathf.Deg2Rad);mat.SetFloat("_Radius",EffectiveSunRadius*Mathf.Deg2Rad);
   mat.SetFloat("_SunIntensity",sunIntensity);mat.SetFloat("_SunDisc",sunDisc?1:0);mat.SetFloat("_Horizon",Horizon(m));mat.SetFloat("_Brightness",brightness);mat.SetFloat("_Blend",blend);
  }
  public Material GetSky(bool multi)=>Ready?(multi?multiple:single).material:null;
  public Color AmbientSky(bool multi)=>(multi?multiple:single).skyColor*brightness;
  public Color AmbientHorizon(bool multi)=>(multi?multiple:single).horizonColor*brightness;
  public Color DirectSun(bool multi)=>(multi?multiple:single).directColor*(sunDisc?sunIntensity*brightness:0);
  public RenderTexture GetLut(bool multi)=>Ready?(multi?multiple:single).current:null;
  public void ReadLightingSynchronously(){if(!Ready)return;foreach(var m in new[]{single,multiple}){var v=new Vector4[5];m.sun.GetData(v);ApplyLighting(m,v[2],v[3],v[4]);}}
  void Release(){generation++;foreach(var m in new[]{single,multiple}){if(m==null)continue;m.sun?.Release();DestroyResource(m.current);DestroyResource(m.previous);DestroyResource(m.transmittance);DestroyResource(m.material);DestroyResource(m.compute);DestroyResource(m.lighting);}single=multiple=null;}
  static void DestroyResource(UnityEngine.Object obj){if(!obj)return;if(obj is RenderTexture rt)rt.Release();if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
 }
}
