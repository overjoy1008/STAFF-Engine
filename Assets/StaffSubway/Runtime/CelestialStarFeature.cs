using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
namespace Staff.Subway {
 // Draw the upstream star billboards after the sky, before volumetric cloud compositing.
 public sealed class CelestialStarFeature : ScriptableRendererFeature {
  public TextAsset catalog;
  public Material starMaterial;
  StarPass pass;
  public override void Create(){pass?.Dispose();pass=new StarPass {renderPassEvent=RenderPassEvent.AfterRenderingSkybox};}
  public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data){
   var sky=SubwayTimeOfDay.ForCamera(data.cameraData.camera);
   if(!sky||!sky.atmosphere||!sky.skyWeather.stars||sky.skyWeather.starBackend!=NishitaSkyWeather.StarBackend.UnityStarRenderer||!catalog||!starMaterial)return;
   var c=data.cameraData.camera;var mode=c.GetComponent<AbstractEnvironment>();
   if((mode&&mode.IsAbstract)||c.cameraType==CameraType.Preview||c.orthographic)return;
   if(sky.atmosphere.Elevation>=-3)return;
   pass.Setup(sky,catalog,starMaterial);renderer.EnqueuePass(pass);
  }
  protected override void Dispose(bool disposing){pass?.Dispose();pass=null;}
  sealed class StarPass:ScriptableRenderPass {
   GameObject owner;StarRenderer stars;SubwayTimeOfDay sky;Material material;
   public void Setup(SubwayTimeOfDay s,TextAsset catalog,Material m){
    sky=s;material=m;
    if(!owner){owner=new GameObject("Unity Star Renderer batches"){hideFlags=HideFlags.HideAndDontSave};stars=owner.AddComponent<StarRenderer>();}
    stars.starDataAsset=catalog;stars.starMaterial=m;stars.magnitudeLimit=s.skyWeather.starMagnitudeLimit;stars.EnsureMeshes();
   }
   public void Dispose(){if(owner){if(Application.isPlaying)Object.Destroy(owner);else Object.DestroyImmediate(owner);}owner=null;stars=null;}
   sealed class Data { public Mesh[] meshes;public Material material;public MaterialPropertyBlock properties;public int shaderPass; }
   public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame){
    if(!sky||!stars||stars.StarCount==0)return;
    var camera=frame.Get<UniversalCameraData>().camera;var resources=frame.Get<UniversalResourceData>();
    var s=sky.skyWeather;var a=sky.atmosphere;
    float phase=s.starPhaseDegrees+(float)(sky.StarElapsedSeconds/Mathf.Max(60,s.starRevolutionSeconds)%1)*360;
    var pole=NishitaSkyWeather.PoleDirection(a.latitude,a.northRotation);
    var east=Quaternion.Euler(0,a.northRotation,0)*Vector3.right;
    var meridian=Vector3.Cross(pole,east).normalized;
    var rotation=Quaternion.AngleAxis(phase,pole);
    // HYG (x,z,y): RA=0 -> upper meridian; RA=6h -> east; declination=90 -> pole.
    var matrix=Matrix4x4.identity;
    matrix.SetColumn(0,rotation*meridian);matrix.SetColumn(1,pole);matrix.SetColumn(2,rotation*east);
    var props=new MaterialPropertyBlock();props.SetMatrix("_StaffCelestialRotation",matrix);
    props.SetFloat("_StaffStarVisibility",s.starBrightness*Mathf.Pow(2,s.starExposure)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-12,-3,a.Elevation))));
    props.SetVector("_StaffMoonDisc",CelestialMoon.Disc(sky.moon,a));props.SetFloat("_StaffMoonVisibility",CelestialMoon.Visibility(sky.moon,a));
    props.SetFloat("_StaffStarTime",(float)sky.StarElapsedSeconds);
    props.SetFloat("StarSize",s.starSize);props.SetVector("ViewerPosition",Vector3.zero);
    props.SetVector("CameraUp",camera.transform.up);props.SetVector("CameraRight",camera.transform.right);
    using(var builder=graph.AddRasterRenderPass<Data>("Unity Star Renderer (HYG)",out var d)){
     d.meshes=new Mesh[stars.Meshes.Count];for(int i=0;i<d.meshes.Length;i++)d.meshes[i]=stars.Meshes[i];
     d.material=material;d.properties=props;d.shaderPass=material.FindPass("Universal Forward");if(d.shaderPass<0)d.shaderPass=0;
     builder.SetRenderAttachment(resources.activeColorTexture,0,AccessFlags.ReadWrite);
     builder.SetRenderAttachmentDepth(resources.activeDepthTexture,AccessFlags.Read);
     builder.SetRenderFunc((Data data,RasterGraphContext ctx)=>{foreach(var mesh in data.meshes)ctx.cmd.DrawMesh(mesh,Matrix4x4.identity,data.material,0,data.shaderPass,data.properties);});
    }
   }
  }
 }
}
