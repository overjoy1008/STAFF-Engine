using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Staff.Nishita.Editor
{
 public static class NishitaRuntimeVerification
 {
  const string Root="Assets/StaffNishita";
  const string Output="Library/StaffNishita/Runtime";
  static T Load<T>(string path)where T:UnityEngine.Object{var a=AssetDatabase.LoadAssetAtPath<T>(Root+"/"+path);if(!a)throw new Exception("Missing asset "+path);return a;}
  static void Check(bool pass,string why){if(!pass)throw new Exception(why);}
  [MenuItem("Tools/Staff/Nishita/Upgrade To Live Atmosphere")]
  public static void Upgrade(){
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   var scene=EditorSceneManager.OpenScene(NishitaSceneBuilder.ScenePath,OpenSceneMode.Single);
   var c=UnityEngine.Object.FindFirstObjectByType<NishitaComparison>();
   var lighting=scene.GetRootGameObjects().First(x=>x.name=="Lighting");
   var a=lighting.GetComponent<NishitaAtmosphere>();if(!a)a=lighting.AddComponent<NishitaAtmosphere>();
   a.singleCompute=Load<ComputeShader>("Shaders/Scattering/Single.compute");a.multipleCompute=Load<ComputeShader>("Shaders/Scattering/Multiple.compute");a.lightingCompute=Load<ComputeShader>("Shaders/Scattering/Lighting.compute");a.skyShader=Load<Shader>("Shaders/LiveSky.shader");
   c.atmosphere=a;c.numberAtlas=Load<Texture2D>("Textures/NumberAtlas.png");
   var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/NumberAtlas.png");importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.SaveAndReimport();
   a.timeOfDay=17.5f;a.useDayCycle=true;a.animate=true;c.exposure=-2;c.yaw=255;c.pitch=-9;
   if(c.gallery)c.gallery.transform.rotation=Quaternion.Euler(0,255,0);
   c.ApplyLayout();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  public static void RunBatch(){
   try{Directory.CreateDirectory(Output);File.Delete(Output+"/failure.txt");File.Delete(Output+"/parameter-response.txt");Upgrade();Run();EditorApplication.Exit(0);}catch(Exception e){File.WriteAllText(Output+"/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
  }
  public static void Run(){
   var c=UnityEngine.Object.FindFirstObjectByType<NishitaComparison>();var a=c.atmosphere;a.animate=false;
   Check(SystemInfo.supportsComputeShaders,"Compute unavailable");
   foreach(var shader in new[]{a.singleCompute,a.multipleCompute,a.lightingCompute}){
    var errors=ShaderUtil.GetComputeShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").ToArray();Check(errors.Length==0,"Compute errors: "+shader.name+" "+string.Join("\n",errors.Select(e=>e.message)));
   }
   Check(!ShaderUtil.ShaderHasError(a.skyShader),"Live sky shader failed");
   a.useDayCycle=false;a.sunRotation=75;a.sunElevation=35;a.Tick(true);a.ReadLightingSynchronously();Check(a.Ready,"Atmosphere not ready");
   c.yaw=64;c.pitch=-9;if(c.gallery)c.gallery.transform.rotation=Quaternion.Euler(0,64,0);
   double[] referenceError=new double[2];
   for(int model=0;model<2;model++){
    var lut=Read(a.GetLut(model==1));var reference=Read(Load<Texture2D>("Textures/Daylight_"+(model==0?"Single":"Multiple")+".exr"));
    double error=0;int count=0;
    foreach(float elev in new[]{12f,30f,60f,80f})foreach(float az in new[]{-130f,-65f,30f,100f}){
     float u=Mathf.Repeat(az/360+.5f,1);float v=Mathf.Sqrt(elev/90)*.5f+.5f;
     Color xyz=lut.GetPixelBilinear(u,v)*256;Color got=XYZ(xyz);
     Color expected=reference.GetPixelBilinear(Mathf.Repeat((az+75)/360+.5f,1),elev/180+.5f);
     error+=(Math.Abs(got.r-expected.r)+Math.Abs(got.g-expected.g)+Math.Abs(got.b-expected.b))/Math.Max(.001,expected.r+expected.g+expected.b);count++;
    }
    referenceError[model]=error/count;UnityEngine.Object.DestroyImmediate(lut);UnityEngine.Object.DestroyImmediate(reference);
    Check(referenceError[model]<.2,"Blender reference mismatch "+model+" error="+referenceError[model]);
   }
   var baseline=Samples(a);
   File.WriteAllText(Output+"/reference-error.txt",string.Join(", ",referenceError));
   int parameterChecks=0;
   foreach(string parameter in new[]{"air","aerosol","ozone","height","elevation"}){
    Reset(a);
    if(parameter=="air")a.airDensity=2;if(parameter=="aerosol")a.aerosolDensity=3;if(parameter=="ozone")a.ozoneDensity=3;if(parameter=="height")a.altitude=10000;if(parameter=="elevation")a.sunElevation=5;
    a.Tick(true);var pixels=Samples(a);
    for(int i=0;i<2;i++){double diff=0,sum=0;for(int j=0;j<pixels[i].Length;j++){var b=baseline[i][j];var x=pixels[i][j];diff+=Math.Abs(x.r-b.r)+Math.Abs(x.g-b.g)+Math.Abs(x.b-b.b);sum+=b.r+b.g+b.b;}
     File.AppendAllText(Output+"/parameter-response.txt",parameter+" "+i+" "+(diff/Math.Max(sum,1e-10))+"\n");
     Check(diff/Math.Max(sum,1e-10)>.005,"Parameter did not change model: "+parameter+" "+i);parameterChecks++;}
   }
   Reset(a);a.Tick(true);c.showGallery=false;c.yaw=75;c.pitch=-35;c.fieldOfView=58;c.displayMode=NishitaComparison.DisplayMode.Single;c.ApplyLayout();
   a.brightness=0;a.Tick(true);var black=Capture(c.singleCamera);Check(black.GetPixels().Max(p=>Mathf.Max(p.r,Mathf.Max(p.g,p.b)))<.04,"Brightness zero does not black out sky");UnityEngine.Object.DestroyImmediate(black);
   a.brightness=1;a.sunRadius=.2725f;a.Tick(true);var small=Capture(c.singleCamera);a.sunRadius=2;a.Tick(true);var big=Capture(c.singleCamera);
   SaveImage(small,"SunSmall");SaveImage(big,"SunBig");File.WriteAllText(Output+"/sun-pixels.txt",BrightPixels(small)+" -> "+BrightPixels(big));
   Check(BrightPixels(big)>BrightPixels(small)*2,"Sun radius not visible");UnityEngine.Object.DestroyImmediate(small);UnityEngine.Object.DestroyImmediate(big);parameterChecks+=2;
   a.sunDisc=false;a.Tick(true);var off=Capture(c.singleCamera);a.sunDisc=true;a.sunIntensity=0;a.Tick(true);var zero=Capture(c.singleCamera);
   Check(Difference(off,zero)<.001,"Sun intensity zero differs from disabled disc");UnityEngine.Object.DestroyImmediate(off);UnityEngine.Object.DestroyImmediate(zero);parameterChecks++;
   a.sunIntensity=1;a.sunRotation=210;a.Tick(true);Check(Vector3.Angle(a.SunDirection,Quaternion.Euler(-35,210,0)*Vector3.forward)<.01,"Sun rotation incorrect");parameterChecks++;
   Reset(a);a.useDayCycle=true;float[] dayHours={0,5.5f,6,12,18,18.5f,24};
   foreach(float hour in dayHours){a.timeOfDay=hour;a.Tick(true);CheckFinite(a);}
   Check(Vector3.Distance(NishitaAtmosphere.SolarDirection(0,37,0,0),NishitaAtmosphere.SolarDirection(24,37,0,0))<1e-5,"Day seam discontinuity");
   Check(Math.Abs(NishitaAtmosphere.SolarDirection(6,37,0,0).y)<1e-5,"Equinox sunrise wrong");
   Check(NishitaAtmosphere.SolarDirection(12,37,0,0).y>.7&&NishitaAtmosphere.SolarDirection(0,37,0,0).y<-.7,"Day/night solar direction wrong");
   float maxStep=0;for(int i=0;i<1440;i++)maxStep=Mathf.Max(maxStep,Vector3.Angle(NishitaAtmosphere.SolarDirection(i/60f,37,0,0),NishitaAtmosphere.SolarDirection((i+1)/60f,37,0,0)));Check(maxStep<.3f,"Solar path jumps");
   Reset(a);a.airDensity=a.aerosolDensity=a.ozoneDensity=0;a.sunRadius=0;a.Tick(true);CheckFinite(a);
   a.airDensity=a.aerosolDensity=a.ozoneDensity=1000;a.altitude=100000;a.sunElevation=-90;a.Tick(true);CheckFinite(a);
   Reset(a);a.sunRadius=.2725f;c.showGallery=true;c.displayMode=NishitaComparison.DisplayMode.Compare;c.yaw=64;c.pitch=-9;c.ApplyLayout();
   foreach(var pair in new[]{new Vector2(35,-3),new Vector2(5,-2),new Vector2(-4,0)}){
    a.sunElevation=pair.x;c.exposure=pair.y;a.Tick(true);a.ReadLightingSynchronously();Pair(c,"Elevation_"+pair.x.ToString(System.Globalization.CultureInfo.InvariantCulture));
   }
   File.WriteAllText(Output+"/verification.json","{\"pass\":true,\"singleMeanRelativeError\":"+referenceError[0].ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"multipleMeanRelativeError\":"+referenceError[1].ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"parameterChecks\":"+parameterChecks+",\"solarMinuteSamples\":1441,\"renderedHours\":7,\"extremeInputsFinite\":true}");
  }
  static void Reset(NishitaAtmosphere a){a.airDensity=a.aerosolDensity=a.ozoneDensity=1;a.altitude=0;a.sunElevation=35;a.sunRotation=75;a.sunRadius=.2725f;a.sunIntensity=a.brightness=1;a.sunDisc=true;a.useDayCycle=false;}
  static Color XYZ(Color c){return new Color(Mathf.Max(0,3.2404542f*c.r-1.5371385f*c.g-.4985314f*c.b),Mathf.Max(0,-.969266f*c.r+1.8760108f*c.g+.041556f*c.b),Mathf.Max(0,.0556434f*c.r-.2040259f*c.g+1.0572252f*c.b));}
  static Color[][] Samples(NishitaAtmosphere a){var result=new Color[2][];for(int i=0;i<2;i++){var t=Read(a.GetLut(i==1));result[i]=t.GetPixels();UnityEngine.Object.DestroyImmediate(t);}return result;}
  static float[] Metrics(NishitaAtmosphere a){var result=new float[2];for(int i=0;i<2;i++){var t=Read(a.GetLut(i==1));foreach(var c in t.GetPixels())result[i]+=c.r+c.g+c.b;UnityEngine.Object.DestroyImmediate(t);}return result;}
  static void CheckFinite(NishitaAtmosphere a){for(int i=0;i<2;i++){var t=Read(a.GetLut(i==1));foreach(var c in t.GetPixels())Check(!float.IsNaN(c.r)&&!float.IsNaN(c.g)&&!float.IsNaN(c.b)&&!float.IsInfinity(c.r)&&!float.IsInfinity(c.g)&&!float.IsInfinity(c.b),"Non-finite radiance");UnityEngine.Object.DestroyImmediate(t);}}
  static Texture2D Read(Texture source){var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);Graphics.Blit(source,rt);RenderTexture.active=rt;var t=new Texture2D(source.width,source.height,TextureFormat.RGBAFloat,false,true);t.ReadPixels(new Rect(0,0,source.width,source.height),0,0);t.Apply();RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);return t;}
  static Texture2D Capture(Camera cam){var target=cam.targetTexture;var rect=cam.rect;var active=RenderTexture.active;var rt=new RenderTexture(640,640,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);rt.Create();try{cam.targetTexture=rt;cam.rect=new Rect(0,0,1,1);Staff.Rendering.StaffCameraCapture.Render(cam);RenderTexture.active=rt;var t=new Texture2D(640,640,TextureFormat.RGBAFloat,false,true);t.ReadPixels(new Rect(0,0,640,640),0,0);t.Apply();return t;}finally{cam.targetTexture=target;cam.rect=rect;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
  static void SaveImage(Texture2D t,string name){var png=new Texture2D(t.width,t.height,TextureFormat.RGB24,false);png.SetPixels(t.GetPixels().Select(p=>p.gamma).ToArray());png.Apply();File.WriteAllBytes(Output+"/"+name+".png",png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);}
  static int BrightPixels(Texture2D t)=>t.GetPixels().Count(c=>c.r>.99f&&c.g>.99f&&c.b>.99f);
  static float Difference(Texture2D a,Texture2D b){var x=a.GetPixels();var y=b.GetPixels();float error=0;for(int i=0;i<x.Length;i++)error+=Mathf.Abs(x[i].r-y[i].r)+Mathf.Abs(x[i].g-y[i].g)+Mathf.Abs(x[i].b-y[i].b);return error/x.Length;}
  static void Pair(NishitaComparison c,string name){var a=Capture(c.singleCamera);var b=Capture(c.multipleCamera);var t=new Texture2D(1280,640,TextureFormat.RGB24,false);t.SetPixels(0,0,640,640,a.GetPixels().Select(p=>p.gamma).ToArray());t.SetPixels(640,0,640,640,b.GetPixels().Select(p=>p.gamma).ToArray());t.Apply();File.WriteAllBytes(Output+"/"+name+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);UnityEngine.Object.DestroyImmediate(t);}
 }
}
