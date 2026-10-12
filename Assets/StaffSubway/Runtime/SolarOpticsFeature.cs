using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
namespace Staff.Subway {
 public sealed class SolarOpticsFeature:ScriptableRendererFeature {
  public Shader opticsShader,shadowShader;
  [Range(256,2048)] public int shadowResolution=1024;
  OpticsPass pass;
  public override void Create(){pass?.Dispose();pass=new OpticsPass(opticsShader,shadowShader){renderPassEvent=(RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing+1)};}
  public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data){
   var sky=SubwayTimeOfDay.Active;var c=data.cameraData.camera;
   if(SubwayPlanarReflection.RenderingMirror||!sky||!sky.atmosphere||!sky.atmosphere.Ready||!opticsShader||!shadowShader||c.cameraType==CameraType.Preview||c.cameraType==CameraType.Reflection||c.orthographic)return;
   var mode=c.GetComponent<AbstractEnvironment>();if(mode&&mode.IsAbstract)return;
   if(sky.atmosphere.Elevation<=-1||(!sky.solarEffects.lensFlare&&!sky.solarEffects.sunRays))return;
   pass.Setup(sky,shadowResolution);pass.ConfigureInput(ScriptableRenderPassInput.Depth|ScriptableRenderPassInput.Color);renderer.EnqueuePass(pass);
  }
  protected override void Dispose(bool disposing){pass?.Dispose();pass=null;}
  sealed class OpticsPass:ScriptableRenderPass {
   readonly Material effect;readonly Shader shadowShader;
   readonly Dictionary<Material,Material> shadowMaterials=new Dictionary<Material,Material>();
   SubwayTimeOfDay sky;int resolution;Renderer[] renderers;double nextScan;
   public OpticsPass(Shader shader,Shader shadow){requiresIntermediateTexture=true;if(shader)effect=CoreUtils.CreateEngineMaterial(shader);shadowShader=shadow;}
   public void Setup(SubwayTimeOfDay value,int size){sky=value;resolution=Mathf.ClosestPowerOfTwo(size);}
   public void Dispose(){CoreUtils.Destroy(effect);foreach(var m in shadowMaterials.Values)CoreUtils.Destroy(m);shadowMaterials.Clear();}
   Material ShadowMaterial(Material source){
    if(!shadowMaterials.TryGetValue(source,out var m)){m=CoreUtils.CreateEngineMaterial(shadowShader);shadowMaterials.Add(source,m);}
    string texture=source.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";
    m.SetTexture("_MainTex",source.HasProperty(texture)?source.GetTexture(texture):Texture2D.whiteTexture);
    if(source.HasProperty(texture)){m.SetTextureScale("_MainTex",source.GetTextureScale(texture));m.SetTextureOffset("_MainTex",source.GetTextureOffset(texture));}
    float cutoff=source.HasProperty("_Cutoff")?source.GetFloat("_Cutoff"):source.HasProperty("_AlphaCutoff")?source.GetFloat("_AlphaCutoff"):.5f;
    bool clip=source.IsKeywordEnabled("_ALPHATEST_ON")||source.renderQueue>=2450||(source.HasProperty("_AlphaClip")&&source.GetFloat("_AlphaClip")>.5f);
    m.SetFloat("_Cutoff",clip?cutoff:0);return m;
   }
   struct Draw {public Renderer renderer;public Material material;public int submesh;}
   class Data {public TextureHandle color,copy,depth,shadow,shadowDepth,rays,blur,visibility;public Material effect;public Draw[] draws;public Camera camera;public Matrix4x4 lightVP,lightView,invVP;public Vector3 lightPosition,sun;public Vector4 sunUV;public SolarEffects.Settings settings;public float daylight,extent,time;public bool clouds;}
   public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame){
    if(!effect||!sky)return;
    var u=frame.Get<UniversalResourceData>();var cd=frame.Get<UniversalCameraData>();var c=cd.camera;var s=sky.solarEffects;
    if(u.isActiveTargetBackBuffer)return;
    var sun=sky.atmosphere.SunDirection.normalized;float extent=Mathf.Clamp(s.rayDistance,10,200);
    Vector3 lightPosition=c.transform.position+sun*extent;
    var lightRotation=Quaternion.LookRotation(-sun,Mathf.Abs(sun.y)>.99f?Vector3.forward:Vector3.up);
    var view=Matrix4x4.Scale(new Vector3(1,1,-1))*Matrix4x4.TRS(lightPosition,lightRotation,Vector3.one).inverse;
    var projection=GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-extent,extent,-extent,extent,.1f,extent*3),true);
    var vp=projection*view;
    var clips=GL.GetGPUProjectionMatrix(c.projectionMatrix,true)*c.worldToCameraMatrix;
    var sunClip=clips*new Vector4(sun.x,sun.y,sun.z,0);
    var sunUV=new Vector4(-10,-10,0,0);
    if(sunClip.w>0){sunUV=new Vector4(sunClip.x/sunClip.w*.5f+.5f,sunClip.y/sunClip.w*.5f+.5f,1,0);if(SystemInfo.graphicsUVStartsAtTop)sunUV.y=1-sunUV.y;}
    var draws=new List<Draw>();
    if(s.sunRays){
     if(renderers==null||Time.realtimeSinceStartupAsDouble>nextScan){renderers=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);nextScan=Time.realtimeSinceStartupAsDouble+1;foreach(var stale in shadowMaterials.Keys.Where(k=>!k).ToArray()){CoreUtils.Destroy(shadowMaterials[stale]);shadowMaterials.Remove(stale);}}
     var volume=new Bounds(c.transform.position,Vector3.one*extent*4);
     foreach(var r in renderers){
      if(!r||!r.enabled||!r.gameObject.activeInHierarchy||r.shadowCastingMode==ShadowCastingMode.Off||!(r is MeshRenderer||r is SkinnedMeshRenderer)||(c.cullingMask&(1<<r.gameObject.layer))==0||!volume.Intersects(r.bounds))continue;
      var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]&&materials[i].renderQueue<=2500)draws.Add(new Draw{renderer=r,material=ShadowMaterial(materials[i]),submesh=i});
     }
    }
    using(var b=graph.AddUnsafePass<Data>("Solar optics — Mancini flare / good-godrays",out var d)){
     d.color=u.activeColorTexture;d.depth=u.cameraDepthTexture;d.camera=c;d.effect=effect;d.draws=draws.ToArray();d.lightVP=vp;d.lightView=view;d.lightPosition=lightPosition;d.invVP=clips.inverse;d.sun=sun;d.sunUV=sunUV;d.settings=s;d.extent=extent;d.time=(float)sky.StarElapsedSeconds;d.daylight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-1,5,sky.atmosphere.Elevation));
     var cloudVolume=VolumeManager.instance.stack?.GetComponent<VolumetricClouds>();d.clouds=sky.skyWeather.clouds&&cloudVolume!=null&&cloudVolume.IsActive();
     var desc=d.color.GetDescriptor(graph);desc.msaaSamples=MSAASamples.None;desc.depthBufferBits=DepthBits.None;desc.name="Solar scene copy";d.copy=graph.CreateTexture(desc);
     b.UseTexture(d.color,AccessFlags.ReadWrite);b.UseTexture(d.copy,AccessFlags.ReadWrite);b.UseTexture(d.depth,AccessFlags.Read);b.UseAllGlobalTextures(true);
     var mask=new TextureDesc(1,1){colorFormat=GraphicsFormat.R16G16_SFloat,name="Sun disc visibility",clearBuffer=false};d.visibility=graph.CreateTexture(mask);b.UseTexture(d.visibility,AccessFlags.ReadWrite);
     if(s.sunRays){
      var sd=new TextureDesc(resolution,resolution){colorFormat=GraphicsFormat.R32_SFloat,name="Nishita directional shadow",clearBuffer=false,filterMode=FilterMode.Point};d.shadow=graph.CreateTexture(sd);b.UseTexture(d.shadow,AccessFlags.ReadWrite);
      sd.colorFormat=GraphicsFormat.None;sd.depthBufferBits=DepthBits.Depth32;sd.name="Sun shadow depth";d.shadowDepth=graph.CreateTexture(sd);b.UseTexture(d.shadowDepth,AccessFlags.Write);
      desc.width=Mathf.Max(1,cd.cameraTargetDescriptor.width/2);desc.height=Mathf.Max(1,cd.cameraTargetDescriptor.height/2);desc.colorFormat=GraphicsFormat.R16G16B16A16_SFloat;desc.name="Godrays half resolution";d.rays=graph.CreateTexture(desc);desc.name="Godrays bilateral";d.blur=graph.CreateTexture(desc);b.UseTexture(d.rays,AccessFlags.ReadWrite);b.UseTexture(d.blur,AccessFlags.ReadWrite);
     }
     b.AllowGlobalStateModification(true);b.AllowPassCulling(false);b.SetRenderFunc((Data data,UnsafeGraphContext context)=>Execute(data,context));
    }
   }
   static void DrawFullscreen(CommandBuffer cmd,RTHandle source,RTHandle destination,Material m,int pass){
    cmd.SetRenderTarget(destination);cmd.SetViewport(new Rect(0,0,destination.rt.width,destination.rt.height));
    cmd.SetGlobalTexture("_MainTex",source);cmd.SetGlobalVector("_MainTex_TexelSize",new Vector4(1f/source.rt.width,1f/source.rt.height,source.rt.width,source.rt.height));
    cmd.DrawProcedural(Matrix4x4.identity,m,pass,MeshTopology.Triangles,3,1);
   }
   static void Execute(Data d,UnsafeGraphContext context){
    var cmd=CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);RTHandle color=d.color,copy=d.copy,depth=d.depth,visibility=d.visibility;var m=d.effect;var s=d.settings;
    cmd.Blit(color,copy);
    cmd.SetGlobalTexture("_SolarSceneDepth",depth);cmd.SetGlobalMatrix("_SolarInvVP",d.invVP);cmd.SetGlobalMatrix("_SolarLightVP",d.lightVP);cmd.SetGlobalMatrix("_SolarLightView",d.lightView);
    cmd.SetGlobalVector("_SolarCamera",d.camera.transform.position);cmd.SetGlobalVector("_SolarSun",d.sun);cmd.SetGlobalVector("_SolarSunUV",d.sunUV);cmd.SetGlobalFloat("_SolarClouds",d.clouds?1:0);cmd.SetGlobalFloat("_SolarExtent",d.extent);
    m.SetVector("_SolarScreen",new Vector4(copy.rt.width,copy.rt.height,1f/copy.rt.width,1f/copy.rt.height));
    m.SetVector("_FlareParameters",new Vector4(s.glareSize,s.flareSize,s.starPoints,s.ghostScale));m.SetVector("_FlareOptions",new Vector4(s.secondaryGhosts?1:0,s.anamorphic?1:0,d.time,s.lensFlare?s.flareIntensity*d.daylight:0));
    m.SetVector("_RayParameters",new Vector4(s.rayDensity,s.rayMaxOpacity,s.raySteps,s.distanceAttenuation));m.SetColor("_RayColor",s.rayColor*d.daylight);m.SetFloat("_RaysEnabled",s.sunRays?1:0);
    DrawFullscreen(cmd,copy,visibility,m,0);cmd.SetGlobalTexture("_SolarVisibility",visibility);
    if(s.sunRays){
     RTHandle shadow=d.shadow,shadowDepth=d.shadowDepth,rays=d.rays,blur=d.blur;
     cmd.SetRenderTarget(shadow,shadowDepth);cmd.ClearRenderTarget(true,true,Color.white);cmd.SetViewport(new Rect(0,0,shadow.rt.width,shadow.rt.height));
     foreach(var draw in d.draws)if(draw.renderer)cmd.DrawRenderer(draw.renderer,draw.material,draw.submesh,0);
     cmd.SetGlobalTexture("_SolarShadow",shadow);
     DrawFullscreen(cmd,copy,rays,m,1);DrawFullscreen(cmd,rays,blur,m,2);cmd.SetGlobalTexture("_SolarRays",blur);cmd.SetGlobalVector("_SolarRaysSize",new Vector4(blur.rt.width,blur.rt.height,1f/blur.rt.width,1f/blur.rt.height));
    }
    DrawFullscreen(cmd,copy,color,m,3);

   }
  }
 }
}
