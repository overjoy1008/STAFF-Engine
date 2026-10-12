using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Staff.Nishita.Editor
{
 [InitializeOnLoad]
 public static class NishitaSceneBuilder
 {
  const string Root="Assets/StaffNishita";
  public const string ScenePath=Root+"/Scenes/Nishita Comparison.unity";
  const string Request="Library/StaffNishita/build-request.txt";
  [Serializable] class Manifest { public Record[] records; }
  [Serializable] class Record {public string name;public float elevation,exposure;public float[] ambientSky,ambientHorizon;}
  static NishitaSceneBuilder(){EditorApplication.update+=Poll;}
  static void Poll(){
   if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete(Request);
   try{
    bool clean=true;for(int i=0;i<SceneManager.sceneCount;i++)clean&=!SceneManager.GetSceneAt(i).isDirty;
    if(!File.Exists(ScenePath))Build();
    if(clean)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
    Selection.activeObject=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
    File.WriteAllText("Library/StaffNishita/deploy-result.txt","PASS: scene created. Opened="+clean);
   }catch(Exception e){File.WriteAllText("Library/StaffNishita/deploy-error.txt",e.ToString());Debug.LogException(e);}
  }
  [MenuItem("Tools/Staff/Nishita/Open Comparison")]
  public static void Open(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);}
  [MenuItem("Tools/Staff/Nishita/Build Comparison")]
  public static void Build(){
   Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Scenes");AssetDatabase.Refresh();
   foreach(string path in Directory.GetFiles(Root+"/Textures","*.exr")){
    var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Default;t.sRGBTexture=false;t.mipmapEnabled=false;
    t.npotScale=TextureImporterNPOTScale.None;t.wrapModeU=TextureWrapMode.Repeat;t.wrapModeV=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;
    var platform=t.GetDefaultPlatformTextureSettings();platform.maxTextureSize=2048;platform.format=TextureImporterFormat.RGBAFloat;t.SetPlatformTextureSettings(platform);t.SaveAndReimport();
   }
   string lutPath=Root+"/Textures/BlenderAgX.png";var lut=(TextureImporter)AssetImporter.GetAtPath(lutPath);
   lut.sRGBTexture=false;lut.mipmapEnabled=false;lut.npotScale=TextureImporterNPOTScale.None;lut.wrapMode=TextureWrapMode.Clamp;lut.filterMode=FilterMode.Bilinear;
   var lp=lut.GetDefaultPlatformTextureSettings();lp.maxTextureSize=4096;lp.format=TextureImporterFormat.RGBA32;lut.SetPlatformTextureSettings(lp);lut.SaveAndReimport();
   string floatLutPath=Root+"/Textures/BlenderAgX.asset";
   var floatLut=AssetDatabase.LoadAssetAtPath<Texture2D>(floatLutPath);
   if(!floatLut){floatLut=new Texture2D(2304,48,TextureFormat.RGBAFloat,false,true);AssetDatabase.CreateAsset(floatLut,floatLutPath);}
   floatLut.LoadRawTextureData(File.ReadAllBytes(Root+"/Textures/BlenderAgX.bytes"));floatLut.wrapMode=TextureWrapMode.Clamp;floatLut.filterMode=FilterMode.Bilinear;floatLut.Apply();EditorUtility.SetDirty(floatLut);
   var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/Textures/BakeManifest.json"));
   var previous=SceneManager.GetActiveScene();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
   try{
    SceneManager.SetActiveScene(scene);
    var cameras=new GameObject("Cameras");var lighting=new GameObject("Lighting");var environment=new GameObject("Environment");var ui=new GameObject("UI");
    var controller=ui.AddComponent<NishitaComparison>();controller.agxLut=floatLut;
    controller.presets=new NishitaComparison.Preset[3];
    string[] names={"Daylight","Golden hour","Twilight"};
    for(int i=0;i<3;i++){
     var a=manifest.records[i*2];var b=manifest.records[i*2+1];
     controller.presets[i]=new NishitaComparison.Preset{label=names[i],elevation=a.elevation,exposure=a.exposure,single=Variant(a),multiple=Variant(b),
      sunIntensity=i==0?85:i==1?24:0,sunColor=i==0?new Color(1,.93f,.80f):new Color(1,.58f,.24f)};
    }
    var sunObj=new GameObject("Sun - gallery approximation");sunObj.transform.SetParent(lighting.transform);var sun=sunObj.AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.shadowStrength=.75f;sun.intensity=24f/256;sun.color=controller.presets[1].sunColor;sun.transform.rotation=Quaternion.Euler(5,255,0);controller.sun=sun;
    controller.singleCamera=Camera(cameras.transform,"Single Scattering",controller,false);
    controller.multipleCamera=Camera(cameras.transform,"Multiple Scattering",controller,true);
    controller.singleCamera.gameObject.tag="MainCamera";controller.singleCamera.gameObject.AddComponent<AudioListener>();
    var gallery=new GameObject("Material gallery");gallery.transform.SetParent(environment.transform);controller.gallery=gallery;
    Gallery(gallery.transform);
    RenderSettings.fog=false;RenderSettings.skybox=controller.presets[1].multiple.sky;RenderSettings.sun=sun;
    RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=controller.presets[1].multiple.ambientSky/256;
    RenderSettings.ambientEquatorColor=controller.presets[1].multiple.ambientHorizon/256;RenderSettings.ambientGroundColor=controller.presets[1].multiple.ambientHorizon*(.12f/256);
    RenderSettings.reflectionIntensity=0;
    controller.SelectPreset(1);controller.ApplyLayout();
    EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
   }finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
  }
  static NishitaComparison.Variant Variant(Record r){
   string path=Root+"/Materials/"+r.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Staff/Nishita/Native Blender Panorama"));AssetDatabase.CreateAsset(m,path);}
   m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+r.name+".exr");EditorUtility.SetDirty(m);
   return new NishitaComparison.Variant{sky=m,ambientSky=new Color(r.ambientSky[0],r.ambientSky[1],r.ambientSky[2]),ambientHorizon=new Color(r.ambientHorizon[0],r.ambientHorizon[1],r.ambientHorizon[2])};
  }
  static Camera Camera(Transform root,string name,NishitaComparison c,bool multiple){
   var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=new Vector3(0,2.2f,-12);
   var camera=go.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.Skybox;camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.depth=multiple?1:0;camera.allowHDR=true;
   var sky=go.AddComponent<Skybox>();sky.material=multiple?c.presets[1].multiple.sky:c.presets[1].single.sky;
   var effect=go.AddComponent<NishitaCamera>();effect.comparison=c;effect.multiple=multiple;effect.toneShader=Shader.Find("Hidden/Staff/Nishita/BlenderAgX");return camera;
  }
  static Material Matte(string name,Color color){
   string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Glossiness",.18f);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);return m;
  }
  static GameObject Primitive(PrimitiveType type,string name,Transform parent,Vector3 pos,Vector3 scale,Material m){
   var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
  }
  static void Gallery(Transform root){
   // Gallery aligned to the initial viewing direction. All labels are ordinary UI, no math glyphs.
   root.position=new Vector3(0,0,-12);root.rotation=Quaternion.Euler(0,64,0);
   var stone=Matte("Warm limestone",new Color(.56f,.51f,.43f));var pale=Matte("Chalk",new Color(.8f,.8f,.76f));
   var clay=Matte("Terracotta",new Color(.55f,.20f,.095f));var blue=Matte("Glazed blue",new Color(.09f,.24f,.32f));
   Primitive(PrimitiveType.Plane,"Horizon ground",root,new Vector3(0,-.08f,0),new Vector3(180,1,180),stone);
   Primitive(PrimitiveType.Cube,"Display plinth",root,new Vector3(0,.22f,9),new Vector3(8,.45f,3),pale);
   Primitive(PrimitiveType.Sphere,"Chalk sphere",root,new Vector3(-2.4f,1.2f,9),Vector3.one*1.5f,pale);
   Primitive(PrimitiveType.Sphere,"Terracotta sphere",root,new Vector3(0,1.2f,9),Vector3.one*1.5f,clay);
   Primitive(PrimitiveType.Sphere,"Blue sphere",root,new Vector3(2.4f,1.2f,9),Vector3.one*1.5f,blue);
   for(int i=0;i<7;i++){float x=(i-3)*4;Primitive(PrimitiveType.Cylinder,"Limestone column",root,new Vector3(x,2.4f,22),new Vector3(.65f,2.4f,.65f),pale);}
  }
  public static void BuildBatch(){
   try{var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.SaveScene(scratch,"Assets/ValidationScratch.unity");Build();Verify();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
  [MenuItem("Tools/Staff/Nishita/Verify Comparison")]
  public static void Verify(){
   Directory.CreateDirectory("Library/StaffNishita");
   var setup=EditorSceneManager.GetSceneManagerSetup();var previous=SceneManager.GetActiveScene();
   var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
   try{
    var c=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<NishitaComparison>(true)).Single();
    if(c.presets.Length!=3||!c.agxLut||c.agxLut.width!=2304||c.agxLut.height!=48)throw new Exception("Invalid presets / AgX LUT");
    foreach(var p in c.presets)foreach(var v in new[]{p.single,p.multiple}){
     if(!v.sky||!v.sky.mainTexture||v.sky.mainTexture.width!=2048||ShaderUtil.ShaderHasError(v.sky.shader))throw new Exception("Missing or invalid HDR sky");
    }
    var tone=c.singleCamera.GetComponent<NishitaCamera>().toneShader;
    if(!tone||ShaderUtil.ShaderHasError(tone))throw new Exception("AgX shader failed");
    for(int i=0;i<3;i++){
     c.SelectPreset(i);c.showGallery=true;c.ApplyLayout();
     var left=Capture(c.singleCamera);var right=Capture(c.multipleCamera);
     var a=left.GetPixels();var b=right.GetPixels();double diff=0;for(int j=0;j<a.Length;j++)diff+=Math.Abs(a[j].r-b[j].r)+Math.Abs(a[j].g-b[j].g)+Math.Abs(a[j].b-b[j].b);
     if(diff/a.Length<.002)throw new Exception("Models unexpectedly identical: "+i);
     var both=new Texture2D(left.width*2,left.height,TextureFormat.RGB24,false);
     both.SetPixels(0,0,left.width,left.height,a);both.SetPixels(left.width,0,right.width,right.height,b);both.Apply();
     File.WriteAllBytes("Library/StaffNishita/"+new[]{"Daylight","GoldenHour","Twilight"}[i]+".png",both.EncodeToPNG());
     UnityEngine.Object.DestroyImmediate(left);UnityEngine.Object.DestroyImmediate(right);UnityEngine.Object.DestroyImmediate(both);
    }
    foreach(NishitaComparison.DisplayMode mode in Enum.GetValues(typeof(NishitaComparison.DisplayMode))){
     c.displayMode=mode;c.ApplyLayout();if(c.singleCamera.enabled!=(mode!=NishitaComparison.DisplayMode.Multiple)||c.multipleCamera.enabled!=(mode!=NishitaComparison.DisplayMode.Single))throw new Exception("Camera mode invalid");
    }
    File.WriteAllText("Library/StaffNishita/verification.json","{\"pass\":true,\"nativeSkyVariants\":6,\"displayModes\":3,\"renderedModelPairs\":3,\"agxLut\":true}");
   }finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
  }
  static Texture2D Capture(Camera cam){
   var oldTarget=cam.targetTexture;var oldRect=cam.rect;var oldActive=RenderTexture.active;
   var rt=new RenderTexture(720,720,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);rt.Create();
   try{cam.targetTexture=rt;cam.rect=new Rect(0,0,1,1);cam.Render();RenderTexture.active=rt;var linear=new Texture2D(720,720,TextureFormat.RGBAFloat,false,true);linear.ReadPixels(new Rect(0,0,720,720),0,0);linear.Apply();
    var pixels=linear.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
    var t=new Texture2D(720,720,TextureFormat.RGB24,false);t.SetPixels(pixels);t.Apply();UnityEngine.Object.DestroyImmediate(linear);return t;}
   finally{cam.targetTexture=oldTarget;cam.rect=oldRect;RenderTexture.active=oldActive;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
  }
 }
}
