using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
namespace Staff.Subway {
 public sealed class LocalGodraysFeature:ScriptableRendererFeature {
  public Shader opticsShader,shadowShader;
  [Range(64,256)]public int shadowResolution=128;
  LocalPass pass;
  public override void Create(){pass?.Dispose();pass=new LocalPass(opticsShader,shadowShader){renderPassEvent=(RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing+2)};}
  public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data){
   var sky=SubwayTimeOfDay.ForCamera(data.cameraData.camera);var c=data.cameraData.camera;
   if(!sky||!sky.solarEffects.sunRays||!sky.solarEffects.localLights||sky.solarEffects.localRayIntensity<=0||sky.solarEffects.localRayDensity<=0||!opticsShader||!shadowShader||SubwayPlanarReflection.RenderingMirror||c.cameraType==CameraType.Preview||c.cameraType==CameraType.Reflection||c.orthographic)return;
   var mode=c.GetComponent<AbstractEnvironment>();if(mode&&mode.IsAbstract)return;
   pass.Setup(sky.solarEffects,shadowResolution);pass.ConfigureInput(ScriptableRenderPassInput.Depth|ScriptableRenderPassInput.Color);renderer.EnqueuePass(pass);
  }
  protected override void Dispose(bool disposing){pass?.Dispose();pass=null;}
  sealed class LocalPass:ScriptableRenderPass {
   readonly Material effect;readonly Shader shadowShader;readonly LocalGodraysSources sources=new LocalGodraysSources();
   readonly List<LocalGodraysSources.Source> selected=new List<LocalGodraysSources.Source>();
   readonly Dictionary<Material,Material> shadows=new Dictionary<Material,Material>();
   SolarEffects.Settings settings;int resolution;
   public LocalPass(Shader shader,Shader shadow){requiresIntermediateTexture=true;if(shader)effect=CoreUtils.CreateEngineMaterial(shader);shadowShader=shadow;}
   public void Setup(SolarEffects.Settings s,int r){settings=s;resolution=Mathf.ClosestPowerOfTwo(Mathf.Clamp(r,64,256));}
   public void Dispose(){CoreUtils.Destroy(effect);foreach(var m in shadows.Values)CoreUtils.Destroy(m);shadows.Clear();}
   Material Shadow(Material source){
    if(!shadows.TryGetValue(source,out var m)){m=CoreUtils.CreateEngineMaterial(shadowShader);shadows.Add(source,m);}
    string tex=source.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";m.SetTexture("_MainTex",source.HasProperty(tex)?source.GetTexture(tex):Texture2D.whiteTexture);
    if(source.HasProperty(tex)){m.SetTextureScale("_MainTex",source.GetTextureScale(tex));m.SetTextureOffset("_MainTex",source.GetTextureOffset(tex));}
    bool clip=source.IsKeywordEnabled("_ALPHATEST_ON")||source.renderQueue>=2450||(source.HasProperty("_AlphaClip")&&source.GetFloat("_AlphaClip")>.5f);
    m.SetFloat("_Cutoff",clip?(source.HasProperty("_Cutoff")?source.GetFloat("_Cutoff"):.5f):0);return m;
   }
   struct Draw {public Renderer renderer;public Material material;public int submesh;}
   class Data {public TextureHandle color,copy,depth,atlas,z,rays,blur;public Material effect;public Matrix4x4 invVP;public Matrix4x4[] matrices=new Matrix4x4[24];public Vector4[] positions=new Vector4[4],colors=new Vector4[4];public Draw[][] draws;public int count,resolution,steps;public float density,intensity;public Vector3 camera;}
   static readonly Vector3[] Directions={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
   public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame){
    if(!effect)return;var u=frame.Get<UniversalResourceData>();if(u.isActiveTargetBackBuffer)return;var cd=frame.Get<UniversalCameraData>();var c=cd.camera;
    sources.Select(c,settings,selected);if(selected.Count==0)return;
    using(var b=graph.AddUnsafePass<Data>("Local good-godrays — point shadow volumes",out var d)){
     d.color=u.activeColorTexture;d.depth=u.cameraDepthTexture;d.effect=effect;d.count=selected.Count;d.resolution=resolution;d.steps=Mathf.Clamp(settings.localRaySteps,16,64);d.density=settings.localRayDensity;d.intensity=settings.localRayIntensity;d.camera=c.transform.position;d.invVP=(GL.GetGPUProjectionMatrix(c.projectionMatrix,true)*c.worldToCameraMatrix).inverse;d.draws=new Draw[d.count*6][];
     for(int i=0;i<d.count;i++){
      var s=selected[i];d.positions[i]=new Vector4(s.position.x,s.position.y,s.position.z,s.range);d.colors[i]=s.color;
      var planes=new Plane[6][];
      for(int face=0;face<6;face++){
       var rotation=Quaternion.LookRotation(Directions[face],face==2||face==3?Vector3.forward:Vector3.up);
       var view=Matrix4x4.Scale(new Vector3(1,1,-1))*Matrix4x4.TRS(s.position,rotation,Vector3.one).inverse;
       planes[face]=GeometryUtility.CalculateFrustumPlanes(Matrix4x4.Perspective(90,1,.025f,s.range)*view);
       d.matrices[i*6+face]=GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(90,1,.025f,s.range),true)*view;
      }
      var draws=new List<Draw>();
      foreach(var r in sources.Renderers){
       if(!r||!r.enabled||!r.gameObject.activeInHierarchy||!(r is MeshRenderer||r is SkinnedMeshRenderer)||r.shadowCastingMode==ShadowCastingMode.Off||(c.cullingMask&(1<<r.gameObject.layer))==0||r.bounds.SqrDistance(s.position)>s.range*s.range)continue;
       var mats=r.sharedMaterials;for(int j=0;j<mats.Length;j++)if(mats[j]&&mats[j].renderQueue<=2500)draws.Add(new Draw{renderer=r,material=Shadow(mats[j]),submesh=j});
      }
      for(int face=0;face<6;face++){var visible=new List<Draw>();foreach(var draw in draws)if(GeometryUtility.TestPlanesAABB(planes[face],draw.renderer.bounds))visible.Add(draw);d.draws[i*6+face]=visible.ToArray();}
     }
     var desc=d.color.GetDescriptor(graph);desc.msaaSamples=MSAASamples.None;desc.depthBufferBits=DepthBits.None;desc.name="Local rays scene copy";d.copy=graph.CreateTexture(desc);
     b.UseTexture(d.color,AccessFlags.ReadWrite);b.UseTexture(d.copy,AccessFlags.ReadWrite);b.UseTexture(d.depth,AccessFlags.Read);
     var sd=new TextureDesc(resolution*6,resolution*d.count){colorFormat=GraphicsFormat.R32_SFloat,filterMode=FilterMode.Point,name="Local point shadow atlas"};d.atlas=graph.CreateTexture(sd);b.UseTexture(d.atlas,AccessFlags.ReadWrite);
     sd.colorFormat=GraphicsFormat.None;sd.depthBufferBits=DepthBits.Depth32;sd.name="Local point shadow depth";d.z=graph.CreateTexture(sd);b.UseTexture(d.z,AccessFlags.Write);
     desc.width=Mathf.Max(1,cd.cameraTargetDescriptor.width/2);desc.height=Mathf.Max(1,cd.cameraTargetDescriptor.height/2);desc.colorFormat=GraphicsFormat.R16G16B16A16_SFloat;desc.name="Local rays half resolution";d.rays=graph.CreateTexture(desc);desc.name="Local rays bilateral";d.blur=graph.CreateTexture(desc);b.UseTexture(d.rays,AccessFlags.ReadWrite);b.UseTexture(d.blur,AccessFlags.ReadWrite);
     b.UseAllGlobalTextures(true);b.AllowGlobalStateModification(true);b.AllowPassCulling(false);b.SetRenderFunc((Data data,UnsafeGraphContext context)=>Execute(data,context));
    }
   }
   static void Fullscreen(CommandBuffer cmd,RTHandle src,RTHandle dst,Material m,int pass){cmd.SetRenderTarget(dst);cmd.SetViewport(new Rect(0,0,dst.rt.width,dst.rt.height));cmd.SetGlobalTexture("_MainTex",src);cmd.SetGlobalVector("_MainTex_TexelSize",new Vector4(1f/src.rt.width,1f/src.rt.height,src.rt.width,src.rt.height));cmd.DrawProcedural(Matrix4x4.identity,m,pass,MeshTopology.Triangles,3,1);}
   static void Execute(Data d,UnsafeGraphContext context){
    var cmd=CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);RTHandle color=d.color,copy=d.copy,depth=d.depth,atlas=d.atlas,z=d.z,rays=d.rays,blur=d.blur;
    cmd.Blit(color,copy);cmd.SetRenderTarget(atlas,z);cmd.SetViewport(new Rect(0,0,atlas.rt.width,atlas.rt.height));cmd.ClearRenderTarget(true,true,Color.white);
    for(int i=0;i<d.count;i++)for(int face=0;face<6;face++){
     int row=SystemInfo.graphicsUVStartsAtTop?d.count-1-i:i;cmd.SetViewport(new Rect(face*d.resolution,row*d.resolution,d.resolution,d.resolution));cmd.EnableScissorRect(new Rect(face*d.resolution,row*d.resolution,d.resolution,d.resolution));
     cmd.SetGlobalMatrix("_LocalShadowVP",d.matrices[i*6+face]);cmd.SetGlobalVector("_LocalShadowSource",d.positions[i]);
     foreach(var draw in d.draws[i*6+face])if(draw.renderer)cmd.DrawRenderer(draw.renderer,draw.material,draw.submesh,0);
    }
    cmd.DisableScissorRect();cmd.SetGlobalTexture("_LocalAtlas",atlas);cmd.SetGlobalTexture("_LocalDepth",depth);cmd.SetGlobalMatrix("_LocalInvVP",d.invVP);cmd.SetGlobalMatrixArray("_LocalMatrices",d.matrices);cmd.SetGlobalVectorArray("_LocalPositions",d.positions);cmd.SetGlobalVectorArray("_LocalColors",d.colors);cmd.SetGlobalVector("_LocalCamera",d.camera);
    cmd.SetGlobalVector("_LocalParameters",new Vector4(d.count,d.steps,d.density,d.intensity));cmd.SetGlobalVector("_LocalAtlasSize",new Vector4(atlas.rt.width,atlas.rt.height,d.resolution,0));
    Fullscreen(cmd,copy,rays,d.effect,0);Fullscreen(cmd,rays,blur,d.effect,1);cmd.SetGlobalTexture("_LocalRays",blur);cmd.SetGlobalVector("_LocalRaysSize",new Vector4(blur.rt.width,blur.rt.height,1f/blur.rt.width,1f/blur.rt.height));Fullscreen(cmd,copy,color,d.effect,2);
   }
  }
 }
}
