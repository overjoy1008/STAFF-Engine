using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Staff.Subway {
 [RequireComponent(typeof(Camera)),DisallowMultipleComponent]
 public sealed class AbstractEnvironment:MonoBehaviour {
  public Transform environmentRoot;
  public Transform[] additionalEnvironmentRoots = new Transform[0];
  public Shader analyticShader;
  // Lighting contains visible diffuser meshes as well as Light components. Include it
  // in old saved scenes too; only renderers are converted, never the lights themselves.
  IEnumerable<Transform> LightingRoots => gameObject.scene.IsValid() ? gameObject.scene.GetRootGameObjects().Where(g=>g.name=="Lighting").Select(g=>g.transform) : Enumerable.Empty<Transform>();
  public IEnumerable<Renderer> EnvironmentRenderers => new[]{environmentRoot}.Concat(additionalEnvironmentRoots??new Transform[0]).Concat(LightingRoots).Concat(UnityEngine.Object.FindObjectsByType<Staff.Shared.SubwayConsist>(FindObjectsSortMode.None).Where(t=>t.gameObject.scene==gameObject.scene).Select(t=>t.transform)).Where(t=>t).SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).Distinct().Where(r=>!r.GetComponentInParent<Staff.Characters.AdventurePlayer>());
  public Shader surfaceShader,geometryShader,edgeShader;
  // Optional camera-local sky: retain the solar disc without atmospheric color.
  public Material abstractSkybox;
  Skybox localSky;Material originalSky;bool originalSkyEnabled;
  public bool startAbstract=false;
  public enum EnvironmentMode { Real, Abstract, Imaginary }
  public EnvironmentMode Mode {get;private set;}
  Staff.MathSpace.MonochromeMode monochrome; bool originalInverted;
  [Range(.4f,3f)] public float entryDuration=1.25f;
  float entryStart;bool entering;
  public bool IsEntering=>entering;
  public EnvironmentMode EntryFrom {get;private set;}
  public Vector3 EntryOrigin {get;private set;}
  public float EntryRange {get;private set;}=100;
  public float EntryProgress=>entering?Mathf.Clamp01((Time.unscaledTime-entryStart)/Mathf.Max(.1f,entryDuration)):1;
  public void BeginEntry(Vector3 origin)=>BeginTransition(EnvironmentMode.Abstract,origin);
  public void BeginTransition(EnvironmentMode target,Vector3 origin){
   if(entering)SetMode(Mode);
   if(target==Mode)return;
   if(!environmentRoot||!surfaceShader||!geometryShader||!edgeShader)return;
   EntryOrigin=origin;EntryRange=20;
   foreach(var r in EnvironmentRenderers){var b=r.bounds;var delta=new Vector2(b.center.x-origin.x,b.center.z-origin.z);EntryRange=Mathf.Max(EntryRange,delta.magnitude+new Vector2(b.extents.x,b.extents.z).magnitude);}
   EntryRange=Mathf.Min(EntryRange,GetComponent<Camera>().farClipPlane*2);
   EntryFrom=Mode;entryStart=Time.unscaledTime;entering=true;Mode=target;
   // Real sky remains live behind the surface wipe when returning from Imaginary.
   if(target==EnvironmentMode.Real&&IsAbstract){source.clearFlags=clearFlags;source.backgroundColor=background;if(localSky){localSky.material=originalSky;localSky.enabled=originalSkyEnabled;}}

  }
  public Material[] EntryMaterials(Renderer renderer){var mats=originals.TryGetValue(renderer,out var saved)?saved:renderer.sharedMaterials;return Mode==EnvironmentMode.Real?mats:mats.Select(m=>m?Convert(m):null).ToArray();}
  public void CycleMode(){var target=(EnvironmentMode)(((int)Mode+1)%3);if(Application.isPlaying){var rig=GetComponentInParent<Staff.Shared.SharedGameplay>();var player=rig?rig.player:gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Staff.Characters.AdventurePlayer>()).FirstOrDefault();BeginTransition(target,player?player.transform.position:transform.position);}else SetMode(target);}
  public void SetMode(EnvironmentMode mode){entering=false;SetAbstract(mode!=EnvironmentMode.Real);if(mode!=EnvironmentMode.Real&&!IsAbstract)return;Mode=mode;if(monochrome&&IsAbstract)monochrome.SetInverted(false);}
  [Range(.5f,2)]public float lineWidth=1;
  [Range(.05f,1)]public float normalThreshold=.18f;
  [Range(.001f,.1f)]public float depthThreshold=.008f;
  public bool IsAbstract{get;private set;}
  readonly Dictionary<Renderer,Material[]> originals=new Dictionary<Renderer,Material[]>();
  readonly Dictionary<Material,Material> replacements=new Dictionary<Material,Material>();
  Camera source,geometryCamera;Material edges;RenderTexture geometry;
  SubwayPlanarReflection reflection;bool reflectionEnabled;CameraClearFlags clearFlags;Color background;
  void OnEnable(){source=GetComponent<Camera>();if(startAbstract)SetAbstract(true);}
  public void SetAbstract(bool value){
   if(value==IsAbstract){if(!value){foreach(var m in replacements.Values)if(m)ReleaseObject(m);replacements.Clear();}return;}
   if(value){
    if(!environmentRoot||!surfaceShader||!geometryShader||!edgeShader){Debug.LogError("Abstract Environment needs its root and shaders.",this);return;}
    if(!surfaceShader.isSupported||!geometryShader.isSupported||!edgeShader.isSupported){Debug.LogError("Abstract shaders are unsupported; keeping original environment materials.",this);return;}
    monochrome=FindFirstObjectByType<Staff.MathSpace.MonochromeMode>();if(monochrome){originalInverted=monochrome.Inverted;monochrome.SetInverted(false);}
    source=GetComponent<Camera>();clearFlags=source.clearFlags;background=source.backgroundColor;
    reflection=GetComponent<SubwayPlanarReflection>();reflectionEnabled=reflection&&reflection.enabled;if(reflection)reflection.enabled=false;
    foreach(var r in EnvironmentRenderers){
     if(r.GetComponentInParent<Staff.Characters.AdventurePlayer>())continue;
     var mats=r.sharedMaterials;originals[r]=mats;var abstractMats=new Material[mats.Length];
     for(int i=0;i<mats.Length;i++)abstractMats[i]=mats[i]?Convert(mats[i]):null;
     r.sharedMaterials=abstractMats;
    }
    source.clearFlags=CameraClearFlags.SolidColor;source.backgroundColor=Color.black;
    if(abstractSkybox){localSky=GetComponent<Skybox>();if(!localSky)localSky=gameObject.AddComponent<Skybox>();originalSky=localSky.material;originalSkyEnabled=localSky.enabled;localSky.material=abstractSkybox;localSky.enabled=true;source.clearFlags=CameraClearFlags.Skybox;}
    IsAbstract=true;Mode=EnvironmentMode.Abstract;
   }else{
    foreach(var pair in originals)if(pair.Key)pair.Key.sharedMaterials=pair.Value;originals.Clear();
    foreach(var m in replacements.Values)ReleaseObject(m);replacements.Clear();
    if(monochrome)monochrome.SetInverted(originalInverted);
    Mode=EnvironmentMode.Real;
    if(source){source.clearFlags=clearFlags;source.backgroundColor=background;}
    if(localSky){localSky.material=originalSky;localSky.enabled=originalSkyEnabled;localSky=null;}
    if(reflection)reflection.enabled=reflectionEnabled;IsAbstract=false;ReleaseRenderResources();
   }
  }
  void LateUpdate(){if(entering&&EntryProgress>=1){var target=Mode;entering=false;SetMode(target);}if(!IsAbstract)return;foreach(var r in EnvironmentRenderers){if(originals.ContainsKey(r))continue;var mats=r.sharedMaterials;originals[r]=mats;r.sharedMaterials=mats.Select(m=>m?Convert(m):null).ToArray();}}
  Material Convert(Material original){
   
   if(replacements.TryGetValue(original,out var m))return m;
   if(original.shader.name=="STAFF/Infinite Coordinate Plane"){
    if(!analyticShader)throw new System.InvalidOperationException("Abstract analytic shader is not assigned.");
    m=new Material(analyticShader){name=original.name+" (Abstract)",hideFlags=HideFlags.HideAndDontSave};
    m.CopyPropertiesFromMaterial(original);m.SetFloat("_Inverted",0);replacements[original]=m;return m;
   }
   m=new Material(surfaceShader){name=original.name+" (Abstract)",hideFlags=HideFlags.HideAndDontSave};
   if(original.HasProperty("_EmissionColor")&&original.IsKeywordEnabled("_EMISSION")){
    m.SetColor("_EmissionColor",original.GetColor("_EmissionColor"));
    if(original.HasProperty("_EmissionMap")&&original.GetTexture("_EmissionMap")){m.SetTexture("_EmissionMap",original.GetTexture("_EmissionMap"));m.SetTextureScale("_EmissionMap",original.GetTextureScale("_EmissionMap"));m.SetTextureOffset("_EmissionMap",original.GetTextureOffset("_EmissionMap"));}
   }
   if(original.shader.name=="STAFF/Unlit"&&original.HasProperty("_BaseMap")){m.SetFloat("_Graphic",1);m.SetTexture("_MainTex",original.GetTexture("_BaseMap"));m.SetTextureScale("_MainTex",original.GetTextureScale("_BaseMap"));m.SetTextureOffset("_MainTex",original.GetTextureOffset("_BaseMap"));if(original.HasProperty("_AlphaClip"))m.SetFloat("_AlphaClip",original.GetFloat("_AlphaClip"));if(original.HasProperty("_SrcBlend")&&original.GetFloat("_SrcBlend")==5)m.SetFloat("_AlphaClip",1);if(original.HasProperty("_Cutoff"))m.SetFloat("_Cutoff",original.GetFloat("_Cutoff"));}
   // Omit decorative station tile seams in Abstract / Imaginary.
   m.SetFloat("_Grid",0);
   if(original.HasProperty("_TileSize"))m.SetFloat("_TileSize",original.GetFloat("_TileSize"));
   replacements[original]=m;return m;
  }
  void OnRenderImage(RenderTexture src,RenderTexture dst){
   if(!IsAbstract){Graphics.Blit(src,dst);return;}
   if(!edges)edges=new Material(edgeShader){hideFlags=HideFlags.HideAndDontSave};
   if(!geometryCamera){var go=new GameObject("Abstract geometry buffer"){hideFlags=HideFlags.HideAndDontSave};geometryCamera=go.AddComponent<Camera>();geometryCamera.enabled=false;}
   if(!geometry||geometry.width!=src.width||geometry.height!=src.height){if(geometry){geometry.Release();ReleaseObject(geometry);}geometry=new RenderTexture(src.width,src.height,24,RenderTextureFormat.ARGBFloat){filterMode=FilterMode.Point,hideFlags=HideFlags.HideAndDontSave};geometry.Create();}
   geometryCamera.CopyFrom(source);geometryCamera.enabled=false;geometryCamera.allowMSAA=false;geometryCamera.targetTexture=geometry;geometryCamera.clearFlags=CameraClearFlags.SolidColor;geometryCamera.backgroundColor=Color.clear;
   geometryCamera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);geometryCamera.worldToCameraMatrix=source.worldToCameraMatrix;geometryCamera.projectionMatrix=source.projectionMatrix;
   geometryCamera.RenderWithShader(geometryShader,"RenderType");edges.SetFloat("_EntryActive",0);edges.SetFloat("_Imaginary",Mode==EnvironmentMode.Imaginary?1:0);edges.SetTexture("_Geometry",geometry);edges.SetFloat("_LineWidth",lineWidth);edges.SetFloat("_NormalThreshold",normalThreshold);edges.SetFloat("_DepthThreshold",depthThreshold);Graphics.Blit(src,dst,edges);
  }
  void ReleaseRenderResources(){if(geometryCamera)ReleaseObject(geometryCamera.gameObject);geometryCamera=null;if(geometry){geometry.Release();ReleaseObject(geometry);}geometry=null;if(edges)ReleaseObject(edges);edges=null;}
  static void ReleaseObject(Object value){if(!value)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
  void OnDisable(){entering=false;SetAbstract(false);ReleaseRenderResources();}
 }
}
