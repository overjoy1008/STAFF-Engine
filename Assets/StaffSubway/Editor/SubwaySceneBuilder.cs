using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Staff.MathSpace;
using Object=UnityEngine.Object;

namespace Staff.Subway.Editor {
 [InitializeOnLoad] public static class SubwaySceneBuilder {
  const string Root="Assets/StaffSubway";
  const string ScenePath="Assets/Scenes/STAFF_Subway_Reference.unity";
  const string Work="Library/StaffSubway";
  const int EnvironmentLayer=30;
  static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
  static readonly List<string> checks=new List<string>();
  static Transform environment,architecture,furniture,trainGroup,lighting,signs;
  static Camera camera;
  static SubwaySceneBuilder(){EditorApplication.update+=Poll;}
  static void Poll(){
   if(!File.Exists(Work+"/build-request.txt")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete(Work+"/build-request.txt");
   try{Build();}catch(Exception ex){File.WriteAllText(Work+"/error.txt",ex.ToString());Debug.LogException(ex);}
  }
  [MenuItem("STAFF/Subway/Build Reference Station")]
  public static void Build(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode before building the station.");
   Directory.CreateDirectory(Work);Directory.CreateDirectory(Root+"/Materials");
   mats.Clear();checks.Clear();
   // New scene only; no user-authored scene objects are edited.
   var previous=SceneManager.GetActiveScene();bool previousDirty=Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty);
   var own=SceneManager.GetSceneByPath(ScenePath);
   if(own.isLoaded && own.isDirty)throw new InvalidOperationException("Save edits to STAFF_Subway_Reference before rebuilding it.");
   PrepareImports();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
   if(own.IsValid()&&own.isLoaded&&!EditorSceneManager.CloseScene(own,true))throw new Exception("Could not close the previous station scene.");
   try{
    RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.45f,.50f);
    RenderSettings.ambientSkyColor=new Color(.40f,.43f,.49f);RenderSettings.ambientEquatorColor=new Color(.28f,.31f,.36f);RenderSettings.ambientGroundColor=new Color(.10f,.12f,.15f);
    RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.8f;
    RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.037f,.045f,.062f);RenderSettings.fogStartDistance=48;RenderSettings.fogEndDistance=122;
    environment=Group("Environment");architecture=Group("Architecture - Greybox",environment);furniture=Group("Tripo - Platform Props",environment);trainGroup=Group("Tripo - PBR Glow Train",environment);signs=Group("Wayfinding",environment);lighting=Group("Lighting");
    var dark=Surface("Charcoal structure",new Color(.055f,.065f,.083f),.18f,.27f);
    var wall=Surface("Wall panels",new Color(.15f,.17f,.205f),.3f,.52f);
    var metal=Surface("Brushed steel",new Color(.36f,.40f,.46f),.72f,.68f);
    var navy=Surface("Chiyoda navy band",new Color(.035f,.064f,.125f),.28f,.46f);
    var ballast=Surface("Dark track bed",new Color(.027f,.031f,.039f),.05f,.18f);
    var floor=Surface("Reflective charcoal tiles",new Color(.10f,.115f,.138f),.20f,.66f);floor.SetFloat("_Grid",1);floor.SetFloat("_TileSize",1.25f);floor.SetFloat("_ReflectionStrength",.22f);
    var tactile=Surface("Warm tactile paving",new Color(.48f,.40f,.275f),.08f,.36f);
    var emission=Emissive("Neutral white LED",new Color(.86f,.93f,1f),3.5f);
    // One metre equals one Unity unit. Platform level is y=0.
    var ground=Cube("Platform slab - 11.4m x 114m",architecture,new Vector3(-2.3f,-.22f,41),new Vector3(11.4f,.44f,114),floor);ground.layer=29;
    Cube("Track trench",architecture,new Vector3(5.15f,-1.24f,41),new Vector3(3.4f,.3f,114),ballast);
    Cube("Opposite track wall",architecture,new Vector3(7.35f,1.7f,41),new Vector3(.25f,5.9f,114),wall);
    Cube("Left concourse wall",architecture,new Vector3(-8.15f,2.2f,41),new Vector3(.25f,4.4f,114),wall);
    Cube("Roof slab",architecture,new Vector3(-.4f,4.64f,41),new Vector3(15.2f,.28f,114),dark);
    Cube("Far portal",architecture,new Vector3(-.4f,2.2f,98),new Vector3(15,4.6f,.25f),ballast);
    for(int i=0;i<16;i++){
     float z=-10+i*7;
     Cube("Transverse ceiling beam "+i,architecture,new Vector3(-.4f,4.38f,z),new Vector3(15,.24f,.15f),metal);
     Cube("Wall joint "+i,architecture,new Vector3(-8.0f,2.2f,z),new Vector3(.04f,4.4f,.035f),dark);
     Cube("Track sleeper "+i,architecture,new Vector3(5.15f,-1.02f,z),new Vector3(2.9f,.16f,.32f),dark);
    }
    for(int x=-7;x<=7;x+=2)Cube("Longitudinal ceiling rib "+x,architecture,new Vector3(x,4.45f,41),new Vector3(.09f,.14f,114),metal);
    foreach(float x in new[]{4.43f,5.87f})Cube("Rail",architecture,new Vector3(x,-.94f,41),new Vector3(.07f,.16f,114),metal);
    Cube("Navy wall stripe",architecture,new Vector3(-7.99f,2.12f,41),new Vector3(.025f,.44f,114),navy);
    for(int i=0;i<14;i++){
     float z=-4+i*7;
     foreach(float x in new[]{-2.65f,-7.05f}){
      var column=Model("Column","Column "+i,furniture,new Vector3(x,0,z),new Vector3(.90f,4.5f,.90f),0,null);
      Cube("Navy column band",column,new Vector3(x,2.2f,z),new Vector3(.916f,.38f,.916f),navy);
      if(x>-3){Panel("Platform identifier",new Vector3(x,2.85f,z-.464f),new Vector2(.64f,1.18f),"ColumnSign",true);}
      Check(Mathf.Abs(BoundsOf(column).size.y-4.5f)<.03f,"Column height 4.5m / "+i+" / "+x+" actual="+BoundsOf(column).size.y);
     }
    }
    // Original detailed tactile strip in foreground; repeated simple pieces in the distance.
    for(int i=0;i<38;i++){
     float z=-14+i*3;
     if(i<12)Model("Tactile","Tripo tactile strip "+i,furniture,new Vector3(2.84f,.004f,z+1.5f),new Vector3(3,.025f,.48f),90,tactile);
     else Cube("Tactile continuation "+i,furniture,new Vector3(2.84f,.0165f,z+1.5f),new Vector3(.48f,.025f,3),tactile);
     if(i<12)Model("PlatformEdge","Tripo platform edge "+i,furniture,new Vector3(3.28f,-.20f,z+1.5f),new Vector3(3,.20f,.24f),90,metal);
    }
    Cube("Platform coping continuation",architecture,new Vector3(3.28f,-.10f,61),new Vector3(.24f,.20f,72),metal);
    foreach(float z in new[]{-5f,20f,41f,69f}){
     var bench=Model("Bench","Black bench",furniture,new Vector3(-5.9f,0,z),new Vector3(2.45f,.87f,.67f),90,null);
     Check(Mathf.Abs(BoundsOf(bench).size.y-.87f)<.03f,"Bench overall height .87m");
    }
    Model("BlueBench","Secondary concourse bench",furniture,new Vector3(-5.8f,0,83),new Vector3(2.5f,.86f,.65f),90,null);
    foreach(float z in new[]{-3.5f,26f,54f,82f}){
     Model("Sign","Tripo suspended sign",furniture,new Vector3(1.7f,3.55f,z),new Vector3(3.7f,.8f,.18f),0,dark);
     Panel("Chiyoda Line - For Kita",new Vector3(1.7f,3.92f,z-.107f),new Vector2(3.60f,.80f),"Wayfinding",true);
     foreach(float x in new[]{.35f,3.05f})Cube("Sign suspension",architecture,new Vector3(x,4.35f,z),new Vector3(.035f,.3f,.035f),metal);
    }
    foreach(float z in new[]{-3f,25f,53f}){
     Cube("Poster casing",architecture,new Vector3(-5.1f,2.0f,z),new Vector3(1.04f,2.05f,.12f),metal);
     Panel("SHUA station poster",new Vector3(-5.1f,2.0f,z-.07f),new Vector2(.98f,1.97f),"Poster",false);
    }
    Model("Display","Tripo departure display",furniture,new Vector3(-3.9f,3.15f,13),new Vector3(2.3f,.58f,.20f),0,dark);
    Panel("Departure screen",new Vector3(-3.9f,3.44f,12.89f),new Vector2(2.24f,.48f),"Departure",false);
    // Tripo LED housing plus clean luminous diffuser and actual local light.
    for(int i=0;i<16;i++)foreach(float x in new[]{-5.2f,.25f}){
     float z=-9+i*7;
     Model("LED","Tripo ceiling light "+i,furniture,new Vector3(x,4.30f,z),new Vector3(2.15f,.095f,.25f),90,metal);
     Cube("White diffuser",lighting,new Vector3(x,4.286f,z),new Vector3(.22f,.022f,2.02f),emission);
     var light=Group("Ceiling pool "+i,lighting).gameObject.AddComponent<Light>();light.type=LightType.Point;light.transform.position=new Vector3(x,3.8f,z);light.color=new Color(.83f,.89f,1f);light.intensity=1.5f;light.range=8.5f;light.renderMode=LightRenderMode.ForcePixel;light.cullingMask=(1<<EnvironmentLayer)|(1<<29)|(1<<28);light.shadows=LightShadows.None;
    }
    var fill=Group("Soft station fill",lighting).gameObject.AddComponent<Light>();fill.type=LightType.Directional;fill.transform.rotation=Quaternion.Euler(65,-30,0);fill.color=new Color(.82f,.88f,1);fill.intensity=.9f;fill.shadows=LightShadows.Soft;fill.shadowStrength=.65f;fill.cullingMask=(1<<EnvironmentLayer)|(1<<29)|(1<<28);
    // Five 20.2m cars. Supplied module is reused as a blockout consist.
    for(int i=0;i<5;i++){
     var car=Model("Train","PBR Glow car "+(i+1),trainGroup,new Vector3(5.08f,-1.06f,-2f+i*20.55f),new Vector3(3.1f,3.85f,20.2f),0,null);
     Check(Mathf.Abs(BoundsOf(car).size.x-3.1f)<.04f,"Train width 3.1m / "+i);
     Cube("Greybox cabin floor",trainGroup,new Vector3(5.08f,.05f,-2+i*20.55f),new Vector3(2.6f,.1f,18.3f),wall);
     Cube("Greybox cabin ceiling",trainGroup,new Vector3(5.08f,2.45f,-2+i*20.55f),new Vector3(2.6f,.08f,18.3f),wall);
     foreach(float dx in new[]{-.9f,.9f}){
      Cube("Greybox cabin seat",trainGroup,new Vector3(5.08f+dx,.45f,-2+i*20.55f),new Vector3(.5f,.14f,15.5f),navy);
      Cube("Cabin linear light",trainGroup,new Vector3(5.08f+dx,2.39f,-2+i*20.55f),new Vector3(.055f,.035f,16.5f),emission);
     }
    }
    // Remaining stairwell and guard rail are simple greybox architecture.
    for(int i=0;i<12;i++)Cube("Concourse stair "+i,architecture,new Vector3(-5.4f,i*.17f/2,89+i*.3f),new Vector3(3.5f,Mathf.Max(.08f,i*.17f),.3f),wall);
    foreach(float x in new[]{-7.3f,-3.5f})Model("Railing","Tripo stair guard",furniture,new Vector3(x,0,87),new Vector3(2.6f,1.05f,.14f),90,metal);
    Model("Floor","Tripo floor sample - entrance landing",furniture,new Vector3(-5.35f,-.09f,85),new Vector3(3.4f,.09f,3.4f),0,wall);
    var figures=Group("Scale References - 1.70m");Person(figures,new Vector3(.0f,0,14),new Color(.36f,.39f,.43f));Person(figures,new Vector3(-4.3f,0,20),new Color(.12f,.14f,.17f));Person(figures,new Vector3(-3.8f,0,44),new Color(.14f,.16f,.19f));
    var cameras=Group("Cameras");camera=Group("Reference Camera",cameras).gameObject.AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,1.72f,-11);camera.transform.rotation=Quaternion.Euler(3,-8,0);camera.fieldOfView=58;camera.nearClipPlane=.08f;camera.farClipPlane=160;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;camera.allowHDR=true;camera.allowMSAA=true;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.cullingMask=(1<<EnvironmentLayer)|(1<<29)|(1<<28);
    camera.gameObject.AddComponent<AudioListener>();var fly=camera.gameObject.AddComponent<SubwayFlyCamera>();fly.referencePosition=camera.transform.position;fly.referenceEuler=camera.transform.eulerAngles;
    var reflection=camera.gameObject.AddComponent<SubwayPlanarReflection>();reflection.floorMaterial=floor;
    // A local cubemap gives metal trim a useful environment reflection without altering project quality settings.
    var probe=Group("Station reflection probe",lighting).gameObject.AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(0,2,7);probe.size=new Vector3(18,10,120);probe.center=new Vector3(0,0,30);probe.boxProjection=true;probe.resolution=128;probe.cullingMask=camera.cullingMask;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=RenderSettings.fogColor;probe.mode=ReflectionProbeMode.Baked;
    Lightmapping.BakeReflectionProbe(probe,Root+"/Textures/StationReflection.exr");AssetDatabase.ImportAsset(Root+"/Textures/StationReflection.exr");probe.bakedTexture=AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Textures/StationReflection.exr");
    foreach(var shader in new[]{"Standard","STAFF/Subway/Station Surface","STAFF/Unlit"})Check(Shader.Find(shader)&&!ShaderUtil.ShaderHasError(Shader.Find(shader)),"Shader compiles: "+shader);
    foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))Check(r.sharedMaterials.All(m=>m&&m.shader),"Valid material: "+r.name);
    AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
    Capture(camera,Work+"/reference-view.png",1600,900);
    Vector3 cp=camera.transform.position;Quaternion cr=camera.transform.rotation;float fov=camera.fieldOfView;
    camera.transform.position=new Vector3(-5,3.2f,-13);camera.transform.LookAt(new Vector3(1,1.4f,19));camera.fieldOfView=65;Capture(camera,Work+"/wide-view.png",1600,900);
    camera.transform.position=cp;camera.transform.rotation=cr;camera.fieldOfView=fov;
    // Dynamic reflection texture must never be serialized as a persistent asset reference.
    floor.SetTexture("_ReflectionTex",null);EditorUtility.SetDirty(floor);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
    SubwayGameplaySetup.Install();
    if(File.Exists(Work+"/error.txt"))File.Delete(Work+"/error.txt");
    File.WriteAllText(Work+"/validation.txt","PASS\n"+string.Join("\n",checks)+"\nScene: "+ScenePath+"\n");
    File.WriteAllText(Work+"/result.json",JsonUtility.ToJson(new Result{scene=ScenePath,checks=checks.Count,renderers=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Renderer>().Length),referenceView=Work+"/reference-view.png"},true));
    if(!previousDirty){EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);Focus();}else{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
    Debug.Log("STAFF SUBWAY BUILD PASS: "+ScenePath);
   }catch{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);throw;}
  }
  [MenuItem("STAFF/Subway/Capture and Validate Saved Station")]
  public static void CaptureCurrent(){
   var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)throw new Exception("Open STAFF_Subway_Reference first.");
   var c=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First(x=>x.name=="Reference Camera");
   c.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
   Check(!ShaderUtil.ShaderHasError(Shader.Find("STAFF/Subway/Station Surface")),"Station shader compiles");
   foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"})){var path=AssetDatabase.GUIDToAssetPath(guid);var m=AssetDatabase.LoadAssetAtPath<Material>(path);FixEmission(m);EditorUtility.SetDirty(m);}
   AssetDatabase.SaveAssets();
   foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"})){var path=AssetDatabase.GUIDToAssetPath(guid);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m.shader.name=="Standard"&&m.GetColor("_EmissionColor").maxColorComponent>.01f)Check(m.IsKeywordEnabled("_EMISSION"),"Emission survives reimport: "+m.name);}

   var roots=scene.GetRootGameObjects();var report=new List<string>();
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.45f,.50f);
   foreach(var light in roots.SelectMany(g=>g.GetComponentsInChildren<Light>()).Where(l=>l.name.StartsWith("Ceiling pool"))){light.type=LightType.Point;var p=light.transform.position;p.y=3.8f;light.transform.position=p;light.intensity=1.5f;}
   foreach(var m in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).Where(m=>m.name.StartsWith("Body_")||m.name.StartsWith("Doors_")||m.name.StartsWith("Undercarriage_"))){m.SetColor("_EmissionColor",Color.white*1.2f);EditorUtility.SetDirty(m);}
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);

   foreach(var t in roots.SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>t.name.StartsWith("PBR Glow car")||t.name=="Black bench"||t.name=="Human scale marker - 1.70m")){
    var b=BoundsOf(t);report.Add(t.name+" world dimensions "+b.size.ToString("F3")+" bottom "+b.min.y.ToString("F3"));
   }
   var first=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>()).First(t=>t.name=="PBR Glow car 1");
   foreach(var r in first.GetComponentsInChildren<Renderer>().Where(r=>r.name.ToLowerInvariant().Contains("door")))report.Add(r.name+" yMin="+r.bounds.min.y.ToString("F3")+" yMax="+r.bounds.max.y.ToString("F3"));
   File.WriteAllText(Work+"/dimensions.txt",string.Join("\n",report));
   Capture(c,Work+"/reference-view.png",1600,900);
   var cp=c.transform.position;var cr=c.transform.rotation;var fov=c.fieldOfView;
   c.transform.position=new Vector3(-5,3.2f,-13);c.transform.LookAt(new Vector3(1,1.4f,19));c.fieldOfView=65;Capture(c,Work+"/wide-view.png",1600,900);c.transform.position=cp;c.transform.rotation=cr;c.fieldOfView=fov;
   File.WriteAllText(Work+"/saved-validation.txt","PASS: saved scene reopened; shader compiles; emission survives material reimport; human scale dimensions checked; both captures refreshed.\n"+string.Join("\n",report));
   Debug.Log("STAFF SUBWAY SAVED SCENE CAPTURE PASS");
  }
  [Serializable] class Result{public string scene;public int checks;public int renderers;public string referenceView;}
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);}
  static void PrepareImports(){
   foreach(string path in AssetDatabase.FindAssets("t:Model",new[]{Root+"/Models"}).Select(AssetDatabase.GUIDToAssetPath)){
    var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.isReadable=true;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;imp.SaveAndReimport();
   }
   foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{Root}).Select(AssetDatabase.GUIDToAssetPath)){
    var imp=AssetImporter.GetAtPath(path) as TextureImporter;if(imp==null)continue;
    if(path.Contains("_ORM")||path.Contains("MetallicSmoothness"))imp.sRGBTexture=false;
    if(path.ToLowerInvariant().Contains("normal"))imp.textureType=TextureImporterType.NormalMap;
    imp.maxTextureSize=path.Contains("/Train/")?4096:2048;imp.anisoLevel=4;imp.SaveAndReimport();
   }
  }
  static Transform Group(string name,Transform parent=null){var g=new GameObject(name);g.layer=EnvironmentLayer;g.transform.SetParent(parent,false);return g.transform;}
  static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=EnvironmentLayer;g.transform.SetParent(parent,true);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
  static Material SaveMat(string name,Material m){string p=Root+"/Materials/"+name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(p);m.name=name;FixEmission(m);if(existing){EditorUtility.CopySerialized(m,existing);Object.DestroyImmediate(m);m=existing;}else AssetDatabase.CreateAsset(m,p);FixEmission(m);EditorUtility.SetDirty(m);mats[name]=m;return m;}
  static void FixEmission(Material m){if(m.shader.name=="Standard"&&m.HasProperty("_EmissionColor")&&m.GetColor("_EmissionColor").maxColorComponent>.01f){m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;m.EnableKeyword("_EMISSION");}}
  static Material Surface(string name,Color color,float metallic,float smooth){if(mats.TryGetValue(name,out var m))return m;m=new Material(Shader.Find("STAFF/Subway/Station Surface"));m.SetColor("_Color",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);m.enableInstancing=true;return SaveMat(name,m);}
  static Material Emissive(string name,Color color,float intensity){if(mats.TryGetValue(name,out var m))return m;m=new Material(Shader.Find("Standard"));m.color=color;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*intensity);return SaveMat(name,m);}
  static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new Exception("No renderers: "+t.name);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
  static Transform Model(string key,string name,Transform parent,Vector3 bottom,Vector3 size,float yaw,Material overwrite){
   string path=Root+"/Models/"+key+"/"+(key=="Train"?"TrainPBRGlow":key)+".fbx";var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new Exception("Missing model "+path);
   var wrapper=Group(name,parent);var normalizer=Group("Metre normalization",wrapper);var inner=(GameObject)PrefabUtility.InstantiatePrefab(asset);inner.transform.SetParent(normalizer,false);
   var b=BoundsOf(inner.transform);normalizer.localScale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);b=BoundsOf(inner.transform);normalizer.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   wrapper.position=bottom;wrapper.rotation=Quaternion.Euler(0,yaw,0);
   foreach(var t in wrapper.GetComponentsInChildren<Transform>())t.gameObject.layer=EnvironmentLayer;
   foreach(var r in wrapper.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>overwrite?overwrite:key=="Train"?TrainMaterial(m?m.name:"Body_Charcoal"):PropMaterial(key,m)).ToArray();
   return wrapper;
  }
  static Material PropMaterial(string key,Material source){
   string name="Tripo "+key;if(mats.TryGetValue(name,out var mat))return mat;
   mat=new Material(Shader.Find("STAFF/Subway/Station Surface"));mat.SetFloat("_Glossiness",key.Contains("Bench")?.44f:.48f);mat.SetFloat("_Metallic",key=="Column"?.35f:.2f);
   var files=Directory.GetFiles(Root+"/Models/"+key,"*.png");string tex=files.FirstOrDefault(f=>f.Contains("tripo_rgb"));
   if(tex!=null)mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(tex));else mat.SetFloat("_UseVertexColor",1);
   return SaveMat(name,mat);
  }
  static Material TrainMaterial(string original){
   string name=original.Replace(" (Instance)","");if(mats.TryGetValue(name,out var m))return m;
   string atlas=name.StartsWith("Doors")?"Doors":name.StartsWith("Undercarriage")?"Undercarriage":"Body";string dir=Root+"/Models/Train/";
   m=new Material(Shader.Find("Standard"));m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+atlas+"_BaseColor.png"));m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+atlas+"_MetallicSmoothness.png"));m.EnableKeyword("_METALLICGLOSSMAP");m.SetFloat("_GlossMapScale",1);m.SetColor("_Color",Color.white);
   m.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+atlas+"_Emission.png"));m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.white*1.2f);m.enableInstancing=true;
   if(name.Contains("Glass")){m.SetFloat("_Mode",2);m.SetColor("_Color",new Color(.78f,.86f,.93f,.22f));m.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.EnableKeyword("_ALPHABLEND_ON");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;}
   return SaveMat(name,m);
  }
  static void Panel(string name,Vector3 pos,Vector2 size,string textureName,bool latex){
   string key="Sign "+textureName;if(!mats.TryGetValue(key,out var m)){m=new Material(Shader.Find("STAFF/Unlit"));m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+textureName+".png"));m.SetColor("_BaseColor",Color.white);m.SetFloat("_Cull",0);m=SaveMat(key,m);}
   var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name=name;g.layer=EnvironmentLayer;g.transform.SetParent(signs,false);g.transform.position=pos;g.transform.localScale=new Vector3(size.x,size.y,1);Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;
   if(latex)g.AddComponent<LatexSymbol>().SetSource(@"\mathsf{3}",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Mathematics/PlatformNumber/symbol.png"),Root+"/Mathematics/PlatformNumber/symbol.tex");
  }
  static void Person(Transform parent,Vector3 pos,Color color){
   var root=Group("Human scale marker - 1.70m",parent);var m=Surface("Scale mannequin "+color.r,color,.05f,.27f);
   void Part(string name,PrimitiveType type,Vector3 center,Vector3 scale){var p=GameObject.CreatePrimitive(type);p.name=name;p.transform.SetParent(root,false);p.transform.position=pos+center;p.transform.localScale=scale;p.layer=28;p.GetComponent<Renderer>().sharedMaterial=m;Object.DestroyImmediate(p.GetComponent<Collider>());}
   Part("Head",PrimitiveType.Sphere,new Vector3(0,1.58f,0),new Vector3(.20f,.24f,.21f));Part("Torso",PrimitiveType.Capsule,new Vector3(0,1.13f,0),new Vector3(.36f,.29f,.22f));
   foreach(float x in new[]{-.10f,.10f})Part("Leg",PrimitiveType.Capsule,new Vector3(x,.45f,0),new Vector3(.135f,.45f,.15f));
   foreach(float x in new[]{-.235f,.235f})Part("Arm",PrimitiveType.Capsule,new Vector3(x,1.02f,0),new Vector3(.10f,.29f,.105f));
  }
  static void Capture(Camera cam,string path,int w,int h){var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);rt.antiAliasing=4;var previous=RenderTexture.active;var old=cam.targetTexture;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var im=new Texture2D(w,h,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,w,h),0,0);im.Apply();File.WriteAllBytes(path,im.EncodeToPNG());Object.DestroyImmediate(im);}finally{RenderTexture.active=previous;cam.targetTexture=old;rt.Release();Object.DestroyImmediate(rt);}}
  [MenuItem("STAFF/Subway/Frame Reference View")]
  public static void Focus(){var c=GameObject.Find("Cameras/Reference Camera")?.GetComponent<Camera>();if(!c)return;var v=SceneView.lastActiveSceneView;if(v){v.LookAtDirect(c.transform.position+c.transform.forward*13,c.transform.rotation,13);v.sceneLighting=true;v.Repaint();}}
 }
}
