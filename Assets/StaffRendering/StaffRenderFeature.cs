using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
namespace Staff.Rendering {
// Native Render Graph integration. Legacy fullscreen shader inputs are bound explicitly.
public sealed class StaffRenderFeature:ScriptableRendererFeature {
 Pass pass; HoyoPass hoyo;
 public Shader geometryShader;
 public override void Create(){pass=new Pass(geometryShader){renderPassEvent=RenderPassEvent.BeforeRenderingPostProcessing};hoyo=new HoyoPass(){renderPassEvent=RenderPassEvent.AfterRenderingOpaques};}
 public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data){
  renderer.EnqueuePass(hoyo);
  var camera=data.cameraData.camera;var n=camera.GetComponent<Staff.Nishita.NishitaCamera>();var a=camera.GetComponent<Staff.Subway.AbstractEnvironment>();
  if((n&&n.isActiveAndEnabled)||(a&&a.isActiveAndEnabled&&a.IsAbstract)){pass.ConfigureInput(ScriptableRenderPassInput.Color|ScriptableRenderPassInput.Depth);renderer.EnqueuePass(pass);}
 }
 protected override void Dispose(bool disposing){pass?.Dispose();}
 class HoyoPass:ScriptableRenderPass{
  class Data{public RendererListHandle stencil,outline;}
  public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame){
   var u=frame.Get<UniversalResourceData>();var c=frame.Get<UniversalCameraData>();var r=frame.Get<UniversalRenderingData>();var l=frame.Get<UniversalLightData>();
   using(var b=graph.AddRasterRenderPass<Data>("HoyoToon stencil and outline",out var p)){
    var filter=new FilteringSettings(RenderQueueRange.all);
    var draw=RenderingUtils.CreateDrawingSettings(new ShaderTagId("StaffHoyoStencil"),r,c,l,SortingCriteria.CommonOpaque);
    p.stencil=graph.CreateRendererList(new RendererListParams(r.cullResults,draw,filter));draw.SetShaderPassName(0,new ShaderTagId("StaffHoyoOutline"));p.outline=graph.CreateRendererList(new RendererListParams(r.cullResults,draw,filter));
    b.UseRendererList(p.stencil);b.UseRendererList(p.outline);b.SetRenderAttachment(u.activeColorTexture,0);b.SetRenderAttachmentDepth(u.activeDepthTexture,AccessFlags.ReadWrite);
    b.SetRenderFunc((Data d,RasterGraphContext ctx)=>{ctx.cmd.DrawRendererList(d.stencil);ctx.cmd.DrawRendererList(d.outline);});
   }
  }
 }
 class Pass:ScriptableRenderPass{

  Shader geometryShader;Dictionary<(Material,bool),Material> geometryMaterials=new Dictionary<(Material,bool),Material>();Material edge;
  public Pass(Shader shader){geometryShader=shader;}
  public void Dispose(){foreach(var m in geometryMaterials.Values)CoreUtils.Destroy(m);geometryMaterials.Clear();CoreUtils.Destroy(edge);}
  class Data{public TextureHandle color,copy,geometry,geometryDepth;public Material tone,edge;public Camera camera;public Renderer[] renderers;public Material[][] mats;}
  Material Geometry(Material original,bool excluded){
   if(!geometryMaterials.TryGetValue((original,excluded),out var m)){m=new Material(geometryShader){hideFlags=HideFlags.HideAndDontSave};geometryMaterials.Add((original,excluded),m);}
   if(original.shader.name=="STAFF/Environment/Abstract Coordinate Plane")return original;
   string tex=original.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";
   if(original.HasProperty(tex)){m.SetTexture("_MainTex",original.GetTexture(tex));m.SetTextureScale("_MainTex",original.GetTextureScale(tex));m.SetTextureOffset("_MainTex",original.GetTextureOffset(tex));}
   string col=original.HasProperty("_BaseColor")?"_BaseColor":"_Color";m.SetColor("_Color",original.HasProperty(col)?original.GetColor(col):Color.white);
   m.SetFloat("_Excluded",excluded?1:0);m.SetFloat("_AlphaClip",excluded?1:original.HasProperty("_AlphaClip")?original.GetFloat("_AlphaClip"):0);m.SetFloat("_Cutoff",original.HasProperty("_AlphaCutoff")?Mathf.Max(original.GetFloat("_AlphaCutoff"),.001f):.1f);return m;
  }
  public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame){
   var resources=frame.Get<UniversalResourceData>();var camera=frame.Get<UniversalCameraData>().camera;
   var n=camera.GetComponent<Staff.Nishita.NishitaCamera>();var a=camera.GetComponent<Staff.Subway.AbstractEnvironment>();bool abstractMode=a&&a.isActiveAndEnabled&&a.IsAbstract;
   using(var b=graph.AddUnsafePass<Data>("STAFF atmosphere / abstract composite",out var p)){
    p.color=resources.activeColorTexture;var desc=p.color.GetDescriptor(graph);desc.msaaSamples=MSAASamples.None;desc.depthBufferBits=DepthBits.None;desc.name="STAFF color copy";p.copy=graph.CreateTexture(desc);p.camera=camera;p.tone=n&&n.isActiveAndEnabled?n.ToneMaterial:null;
    b.UseTexture(p.color,AccessFlags.ReadWrite);b.UseTexture(p.copy,AccessFlags.ReadWrite);b.AllowPassCulling(false);b.AllowGlobalStateModification(true);
    if(abstractMode){
     foreach(var stale in geometryMaterials.Keys.Where(k=>!k.Item1).ToArray()){CoreUtils.Destroy(geometryMaterials[stale]);geometryMaterials.Remove(stale);}
     if(!edge)edge=CoreUtils.CreateEngineMaterial(a.edgeShader);p.edge=edge;edge.SetFloat("_Imaginary",a.Mode==Staff.Subway.AbstractEnvironment.EnvironmentMode.Imaginary?1:0);edge.SetFloat("_LineWidth",a.lineWidth);edge.SetFloat("_NormalThreshold",a.normalThreshold);edge.SetFloat("_DepthThreshold",a.depthThreshold);
     desc.colorFormat=UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;desc.depthBufferBits=DepthBits.None;desc.name="STAFF abstract geometry";desc.clearBuffer=true;desc.clearColor=Color.clear;p.geometry=graph.CreateTexture(desc);b.UseTexture(p.geometry,AccessFlags.ReadWrite);var dd=desc;dd.colorFormat=UnityEngine.Experimental.Rendering.GraphicsFormat.None;dd.depthBufferBits=DepthBits.Depth32;dd.name="STAFF geometry depth";p.geometryDepth=graph.CreateTexture(dd);b.UseTexture(p.geometryDepth,AccessFlags.ReadWrite);
     var env=new HashSet<Renderer>(a.EnvironmentRenderers);p.renderers=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).Where(s=>s.isLoaded).SelectMany(s=>s.GetRootGameObjects()).SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Distinct().Where(x=>x.enabled&&x.gameObject.activeInHierarchy&&((camera.cullingMask&(1<<x.gameObject.layer))!=0)).ToArray();
     p.mats=p.renderers.Select(x=>x.sharedMaterials.Select(m=>m?Geometry(m,!env.Contains(x)):null).ToArray()).ToArray();
    }
    b.SetRenderFunc((Data d,UnsafeGraphContext ctx)=>{
     var cmd=CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);RTHandle color=d.color,copy=d.copy;
     cmd.Blit(color,copy);
     if(d.edge){RTHandle geom=d.geometry,geometryDepth=d.geometryDepth;cmd.SetRenderTarget(geom,geometryDepth);cmd.ClearRenderTarget(true,true,Color.clear);cmd.SetViewProjectionMatrices(d.camera.worldToCameraMatrix,GL.GetGPUProjectionMatrix(d.camera.projectionMatrix,true));cmd.SetGlobalMatrix("_StaffGeometryVP",GL.GetGPUProjectionMatrix(d.camera.projectionMatrix,true)*d.camera.worldToCameraMatrix);cmd.SetGlobalMatrix("_StaffGeometryV",d.camera.worldToCameraMatrix);
      for(int i=0;i<d.renderers.Length;i++)for(int j=0;j<d.mats[i].Length;j++)if(d.renderers[i]&&d.mats[i][j])cmd.DrawRenderer(d.renderers[i],d.mats[i][j],j,d.mats[i][j].shader.name=="STAFF/Environment/Abstract Coordinate Plane"?1:0);
      

      cmd.SetGlobalTexture("_Geometry",geom);cmd.SetGlobalVector("_Geometry_TexelSize",new Vector4(1f/geom.rt.width,1f/geom.rt.height,geom.rt.width,geom.rt.height));cmd.Blit(copy,color,d.edge);
     }else if(d.tone)cmd.Blit(copy,color,d.tone);
    });
   }
  }
 }
}}
