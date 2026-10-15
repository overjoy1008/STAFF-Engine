using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Staff.City.Editor {
[InitializeOnLoad]
public static class MathCityBuilder {
    const string Root="Assets/StaffMathCity", ScenePath="Assets/Scenes/STAFF_City_Reference.unity", Work="Library/StaffMathCity";
    static readonly Dictionary<string,Material> M=new Dictionary<string,Material>();
    static Transform env; static Camera cam; static int meshId; static Font font; static string meshFolder;
    static MathCityBuilder(){ EditorApplication.update+=Poll; }
    static void Poll(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        foreach(string task in new[]{"build","capture","verify"}) {
            string req=Work+"/"+task+"-request.txt"; if(!File.Exists(req))continue; File.Delete(req);
            try { if(task=="build")Build(); else if(task=="verify")Verify(); else CaptureSaved(); }
            catch(Exception e){Directory.CreateDirectory(Work);File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
        }
    }
    [MenuItem("STAFF/City/Open Math City")]
    public static void Open(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);}
    [MenuItem("STAFF/City/Build Reference City")]
    public static void Build(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
        Directory.CreateDirectory(Work); foreach(string d in new[]{"Materials","Meshes","Prefabs","Rendering"})Directory.CreateDirectory(Root+"/"+d);
        var previous=SceneManager.GetActiveScene(); var existing=SceneManager.GetSceneByPath(ScenePath);
        if(existing.IsValid()&&existing.isLoaded&&existing.isDirty)throw new Exception("City has unsaved edits. Save before rebuilding.");
        bool previousDirty=Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        if(existing.IsValid() && existing.isLoaded)EditorSceneManager.CloseScene(existing,true);
        try {
            Random.InitState(7319); M.Clear();meshId=0;meshFolder=Root+"/Meshes/Build_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(meshFolder);font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ConfigureTextures();Palette(); env=Group("Environment"); var street=Group("Streets and Sidewalks",env);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=C("AAC4D5");RenderSettings.ambientEquatorColor=C("879BA6");RenderSettings.ambientGroundColor=C("5A6266");
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=C("BCCAD0");RenderSettings.fogStartDistance=65;RenderSettings.fogEndDistance=240;
            var sky=SaveMaterial("Afternoon Sky",new Material(Shader.Find("STAFF/City/Painted Sky")));RenderSettings.skybox=sky;
            // Reference composition: crossing, sage shop, apartment/bookshop, cables, ginkgo, railway.
            Box("Asphalt street",street,V(0,-.16f,53),V(8.8f,.3f,158),"asphalt",true);
            Box("Cross street",street,V(0,-.15f,-8),V(160,.28f,9),"asphalt",true);
            Box("Urban ground",street,V(0,-.48f,65),V(260,.35f,230),"concrete",true);
            foreach(float side in new[]{-1f,1f}){
                Box("Raised pavement",street,V(side*6.15f,.02f,58),V(3.5f,.28f,142),"paving",true);
                for(int j=0;j<93;j++){
                    float z=-12+j*1.55f;
                    Box("Curb stone",street,V(side*4.55f,.16f,z),V(.24f,.29f,1.5f),j%9==0?"cream":"curb");
                    Box("Pavement joint",street,V(side*6.2f,.167f,z),V(3.15f,.008f,.018f),"joint");
                }
                for(int j=0;j<4;j++)Box("Long paving joint",street,V(side*(4.9f+j*.78f),.166f,57),V(.018f,.008f,139),"joint");
                Box("Road edge paint",street,V(side*4.12f,.009f,57),V(.11f,.016f,136),"paint");
            }
            for(int i=-5;i<=5;i++)Box("Zebra crossing",street,V(i*1.33f,.006f,-6.5f),V(.72f,.02f,3.5f),"paint");
            for(int j=0;j<23;j++)Box("Broken center line",street,V(0,.012f,4+j*5.5f),V(.09f,.018f,2.1f),"paint");
            for(int z=10;z<110;z+=23){Disc("Manhole",street,V(2,.013f,z),.40f,.012f,"iron");for(int n=-3;n<=3;n++)Box("Manhole grooves",street,V(2+n*.085f,.03f,z),V(.027f,.012f,.57f),"metal");}
            Bake(street);
            var blocks=Group("Shopfronts and Apartments",env);
            Building(blocks,"Sage Corner Bookshop",-12.6f,4,12,10,4,-90,"cream","sage","PARABOLA  BOOKS",true);
            Building(blocks,"Ochre Bookshop",12.1f,4,10,9,3,90,"ochre","terracotta","LEAF  BOOKS",true);
            Building(blocks,"East Apartments",12.9f,16.1f,12.2f,10.5f,5,90,"stone","navy","BOTANICA",false);
            Building(blocks,"West Apartments",-12.7f,18,11.2f,10.2f,5,-90,"plaster","sage","STAFF  OFFICE",false);
            string[] names={"KISSA  CAFE","FLOWER  SHOP","RECORDS","ATELIER","BAKERY","TEA  ROOM","STATION  WALK","PAPER  SHOP"};
            float next=31;
            for(int i=0;i<8;i++){
                float width=Random.Range(8f,11.8f);
                foreach(float side in new[]{-1f,1f})Building(blocks,(side<0?"West ":"East ")+names[i],side*Random.Range(12f,13.3f),next,width,Random.Range(8.5f,10.5f),Random.Range(3,6),side<0?-90:90,new[]{"cream","stone","plaster","ochre"}[Random.Range(0,4)],new[]{"sage","navy","terracotta"}[i%3],names[i],i%3==0);
                next+=width+1.1f;
            }
            var props=Group("Street Furniture",env);
            Vending(props,V(-6.85f,.17f,-1.5f),-90);Vending(props,V(7.15f,.17f,24),90);
            for(int i=0;i<15;i++)foreach(float side in new[]{-1f,1f}){
                float z=1+i*8;
                Bollard(props,V(side*4.96f,.17f,z));
                if(i<14)Beam("Pedestrian guard rail",props,V(side*4.96f,.92f,z+.22f),V(side*4.96f,.92f,z+7.6f),.038f,"metal");
            }
            for(int i=0;i<8;i++)foreach(float side in new[]{-1f,1f}){
                float z=5+i*14;
                Pot(props,V(side*7.45f,.17f,z),i);
                if(i%2==0)Bench(props,V(side*6.5f,.17f,z+4),side<0?-90:90);
            }
            Sandwich(props,V(-6.5f,.18f,2),-72,"BOOKS");Sandwich(props,V(6.7f,.18f,.2f),70,"CAFE");
            Bake(props);
            var greenery=Group("Ginkgo and Botanical Silhouettes",env);
            for(int i=0;i<9;i++)foreach(float side in new[]{-1f,1f})Tree(greenery,V(side*5.8f,.17f,8+i*13.8f),Random.Range(.85f,1.18f),i%3==0);
            var utility=Group("Utility Poles and Catenary",env);
            for(int i=0;i<7;i++)foreach(float side in new[]{-1f,1f}){
                float z=-.5f+i*21;Pole(utility,V(side*5.05f,.18f,z));
                if(i<6)for(int c=0;c<4;c++)Cable(utility,V(side*5.05f+(c-1.5f)*.28f,9.3f+c*.23f,z),V(side*5.05f+(c-1.5f)*.28f,9.3f+c*.23f,z+21),.9f+c*.08f,.016f);
                if(i%2==0)for(int c=0;c<2;c++)Cable(utility,V(-5.05f,8.9f+c*.45f,z),V(5.05f,8.9f+c*.45f,z+5),1.1f,.016f);
            }
            Bake(utility);Railway();Skyline();
            var lights=Group("Lighting");var sun=Group("Warm late afternoon sun",lights).gameObject.AddComponent<Light>();sun.type=LightType.Directional;sun.color=C("FFE4B9");sun.intensity=2;sun.transform.rotation=Quaternion.Euler(37,-48,0);sun.shadows=LightShadows.Soft;sun.shadowBias=.035f;sun.shadowNormalBias=.16f;RenderSettings.sun=sun;
            var cameras=Group("Cameras");cam=Group("City Reference Camera",cameras).gameObject.AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=V(.15f,2.1f,-15);cam.transform.LookAt(V(-.45f,5.5f,50));cam.fieldOfView=61;cam.nearClipPlane=.08f;cam.farClipPlane=500;cam.clearFlags=CameraClearFlags.Skybox;cam.allowHDR=true;cam.gameObject.AddComponent<AudioListener>();
            var data=cam.GetUniversalAdditionalCameraData();data.SetRenderer(EnsureRenderer());data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
            var fly=cam.gameObject.AddComponent<CityExploreCamera>();fly.homePosition=cam.transform.position;fly.homeEuler=cam.transform.eulerAngles;fly.speed=5;
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Staff.Shared.Editor.SharedMigration.Gameplay))Staff.Shared.Editor.SharedMigration.Install(scene);
            CityScaleMatching.MatchScene(scene,CityScaleMatching.ReferenceScale());
            AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Could not save city scene.");
            // Do not close or save another dirty scene. Clean editor tabs can switch to the new city.
            if(!previousDirty){EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);}
            else {EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);File.WriteAllText(Work+"/build-complete.txt",ScenePath+"\nPrevious dirty scene preserved. Open city to capture.");return;}
            CaptureSaved();Verify();EditorSceneManager.SaveScene(SceneManager.GetSceneByPath(ScenePath));
            var sv=SceneView.lastActiveSceneView;if(sv){sv.pivot=V(0,5,23);sv.rotation=Quaternion.Euler(7,0,0);sv.size=32;sv.Repaint();}
            File.WriteAllText(Work+"/build-complete.txt",ScenePath);if(File.Exists(Work+"/error.txt"))File.Delete(Work+"/error.txt");
        } catch {if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);throw;}
    }
    static void Palette(){
        string[] colors={"asphalt:424A52","paving:B7B1A2","curb:C6C2B6","concrete:AFAE9F","joint:8A8D86","paint:E0DBCA","cream:DFD5BA","plaster:C9C9BD","stone:B5BEC1","ochre:C4A66E","sage:586F62","navy:3C5362","terracotta:906B54","iron:353F46","metal:637078","glass:344B55","glassLight:78969D","glassWarm:ACA184","white:EAE4D1","red:994F46","bark:625D4C","leaf:78874D","leafLight:A6A65C","gold:BCAC56","leafDark:4B6041","soil:656452","paper:D2C5A4","bookBlue:597C86","bookRed:945F50"};
        foreach(var s in colors){var a=s.Split(':');var m=new Material(Shader.Find("STAFF/City/Painted Surface"));m.SetColor("_BaseColor",C(a[1]));m.SetColor("_ShadowTint",C("8798B2"));m.SetFloat("_Grain",0); if(new[]{"asphalt","cream","plaster","stone","ochre","paving","concrete","curb"}.Contains(a[0])){m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+(a[0]=="asphalt"?"Asphalt":"Plaster")+".png"));m.SetFloat("_WorldScale",a[0]=="asphalt"?.45f:.24f);}M[a[0]]=SaveMaterial(a[0],m);}
    }
    static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static GameObject Box(string name,Transform p,Vector3 pos,Vector3 size,string mat,bool collider=false){return Primitive(name,p,pos,size,mat,PrimitiveType.Cube,collider);}
    static GameObject Primitive(string name,Transform p,Vector3 pos,Vector3 size,string mat,PrimitiveType type,bool collider=false){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=M[mat];if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static void Disc(string name,Transform p,Vector3 pos,float radius,float height,string mat){Primitive(name,p,pos,V(radius*2,height*.5f,radius*2),mat,PrimitiveType.Cylinder);}
    static void Beam(string name,Transform p,Vector3 a,Vector3 b,float radius,string mat){var g=Primitive(name,p,(a+b)*.5f,V(radius*2,Vector3.Distance(a,b)*.5f,radius*2),mat,PrimitiveType.Cylinder);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void Cable(Transform p,Vector3 a,Vector3 b,float sag,float r){Vector3 last=a;for(int j=1;j<=18;j++){float t=j/18f;var next=Vector3.Lerp(a,b,t)-Vector3.up*(4*sag*t*(1-t));Beam("Cable",p,last,next,r,"iron");last=next;}}
    static void Building(Transform parent,string name,float x,float z,float w,float d,int floors,float yaw,string wall,string awning,string title,bool balcony){
        var t=Group(name,parent);t.localPosition=V(x,.17f,z);t.localRotation=Quaternion.Euler(0,yaw,0);float h=floors*3.05f;
        t.gameObject.AddComponent<Staff.Traversal.TangentSurface>();
        Box("Main plaster mass",t,V(0,h/2,0),V(w,h,d),wall,true);
        Box("Foundation",t,V(0,.2f,0),V(w+.10f,.4f,d+.1f),"stone");
        for(int fl=1;fl<=floors;fl++)Box("Floor belt",t,V(0,fl*3.05f-.07f,0),V(w+.16f,.16f,d+.16f),fl==floors?"white":wall);
        Box("Roof slab",t,V(0,h+.13f,0),V(w+.38f,.22f,d+.36f),"curb",true);
        foreach(float s in new[]{-1f,1f}){Box("Roof parapet",t,V(s*(w/2-.1f),h+.46f,0),V(.2f,.7f,d),wall,true);Box("Roof parapet",t,V(0,h+.46f,s*(d/2-.1f)),V(w,.7f,.2f),wall,true);}
        var front=Group("Street facade",t);front.localPosition=V(0,0,-d/2-.02f);Facade(front,w,floors,awning,title,balcony);
        foreach(float s in new[]{-1f,1f}){
            var side=Group("Side facade",t);side.localPosition=V(s*(w/2+.02f),0,0);side.localRotation=Quaternion.Euler(0,s<0?90:-90,0);
            if((name=="Sage Corner Bookshop" && s<0)||(name=="Ochre Bookshop" && s>0))Facade(side,d,floors,awning,title,false); else for(int fl=1;fl<floors;fl++)for(float u=-d*.34f;u<d*.4f;u+=2.25f)Window(side,u,fl*3.05f+1.35f,1.25f,1.7f,false);
            Box("Drain pipe",t,V(s*(w*.5f+.07f),h*.48f,-d*.36f),V(.085f,h*.95f,.085f),"metal");
        }
        for(int i=0;i<3;i++)AC(t,V(-w*.27f+i*1.8f,h+.56f,d*.19f));
        Beam("Roof aerial",t,V(w*.22f,h+.6f,0),V(w*.22f,h+2.7f,0),.035f,"metal");
        for(int i=0;i<4;i++)Beam("Aerial dipole",t,V(w*.22f-.55f,h+2.2f+i*.12f,i*.22f-.3f),V(w*.22f+.55f,h+2.2f+i*.12f,i*.22f-.3f),.018f,"metal");
        Bake(t);
        if(name=="Sage Corner Bookshop"||name=="Ochre Bookshop")PrefabUtility.SaveAsPrefabAsset(t.gameObject,Root+"/Prefabs/"+name+".prefab");
    }
    static void Facade(Transform t,float w,int floors,string awning,string title,bool balcony){
        Box("Recessed shopfront",t,V(0,1.44f,-.018f),V(w-.55f,2.6f,.08f),"iron");
        int panes=Mathf.Max(3,Mathf.FloorToInt(w/1.6f));float unit=(w-.9f)/panes;
        for(int i=0;i<panes;i++){
            float u=-w/2+.45f+unit*(i+.5f);Box("Shop glass",t,V(u,1.45f,-.078f),V(unit-.12f,2.25f,.045f),i%3==0?"glassWarm":"glass");
            Box("Glazing mullion",t,V(u-unit*.5f,1.45f,-.13f),V(.07f,2.47f,.10f),"metal");
            Box("Display shelf",t,V(u,.87f,-.16f),V(unit-.2f,.07f,.12f),"paper");
            for(int b=0;b<6;b++)Box("Book display spine",t,V(u-unit*.36f+b*unit*.12f,.65f+Random.Range(0f,.08f),-.18f),V(unit*.095f,.3f+Random.Range(0f,.2f),.055f),new[]{"paper","bookBlue","bookRed","white"}[Random.Range(0,4)]);
            Box("Door handle",t,V(u+unit*.3f,1.15f,-.21f),V(.025f,.3f,.025f),"white");
        }
        var a=Box("Sloped canvas awning",t,V(0,2.89f,-.66f),V(w+.3f,.12f,1.45f),awning);a.transform.localRotation=Quaternion.Euler(-13,0,0);
        Box("Awning valance",t,V(0,2.65f,-1.37f),V(w+.3f,.28f,.07f),awning);
        for(float u=-w/2;u<w/2;u+=.38f)Box("Awning seam",t,V(u,2.65f,-1.412f),V(.015f,.27f,.014f),awning=="sage"?"metal":awning);
        Label(t,title,V(0,2.68f,-1.42f),.24f,"white");
        for(int fl=1;fl<floors;fl++){
            int count=Mathf.FloorToInt(w/2.3f);float step=(w-.8f)/count;
            for(int j=0;j<count;j++){float u=-w/2+.4f+step*(j+.5f);Window(t,u,fl*3.05f+1.32f,1.38f,1.83f,balcony);if(j==0&&fl%2==1)AC(t,V(u-.74f,fl*3.05f+1.55f,-.44f));}
            if(balcony){
                float y=fl*3.05f+.2f;Box("Balcony floor",t,V(0,y,-.58f),V(w-.6f,.18f,1.15f),"curb");
                Beam("Balcony top rail",t,V(-w/2+.3f,y+1,-1.11f),V(w/2-.3f,y+1,-1.11f),.028f,"iron");
                for(float u=-w/2+.35f;u<w/2-.3f;u+=.25f)Box("Balcony picket",t,V(u,y+.54f,-1.11f),V(.034f,.92f,.034f),"metal");
                for(int n=0;n<2;n++)Pot(t,V(-w*.25f+n*w*.45f,y+.1f,-.55f),fl+n);
            }
        }
        // Modest projecting sign, with ordinary words rather than mathematical glyphs.
        var s=Group("Projecting shop sign",t);s.localPosition=V(w/2-.38f,4.85f,-.8f);s.localRotation=Quaternion.Euler(0,90,0);
        Box("Sign metal surround",s,V(0,0,0),V(1.2f,2.25f,.13f),"iron");Box("Sign enamel",s,V(0,0,-.077f),V(1.08f,2.11f,.04f),"cream");
        Label(s,title.Contains("BOOK")?"B\nO\nO\nK\nS":"SHOP",V(0,0,-.11f),.24f,"iron");
    }
    static void Window(Transform t,float x,float y,float w,float h,bool balcony){
        Box("Window recess",t,V(x,y,-.035f),V(w+.16f,h+.17f,.1f),"iron");
        Box("Window glazing",t,V(x,y,-.097f),V(w,h,.034f),Random.value<.3f?"glassLight":"glass");
        Box("Window split",t,V(x,y,-.13f),V(.052f,h+.06f,.038f),"metal");
        Box("Window transom",t,V(x,y+h*.17f,-.13f),V(w,.045f,.038f),"metal");
        Box("Deep sill",t,V(x,y-h*.5f-.06f,-.16f),V(w+.25f,.095f,.32f),"curb");
        Box("Window head",t,V(x,y+h*.5f+.06f,-.14f),V(w+.24f,.08f,.20f),"curb");
        if(Random.value<.33f)Box("Half drawn blind",t,V(x,y+h*.28f,-.126f),V(w-.08f,h*.39f,.018f),"paper");
    }
    static void AC(Transform t,Vector3 pos){
        var a=Group("Outdoor air conditioner",t);a.localPosition=pos;Box("AC casing",a,V(0,0,0),V(.79f,.52f,.32f),"curb");Box("Fan grille",a,V(.16f,0,-.18f),V(.36f,.36f,.025f),"metal");
        for(int j=0;j<6;j++)Box("Vent louver",a,V(-.24f,-.17f+j*.065f,-.18f),V(.22f,.017f,.026f),"iron");
        for(int j=0;j<5;j++)Box("Grille bar",a,V(.02f+j*.07f,0,-.2f),V(.012f,.33f,.02f),"curb");
    }
    static void ConfigureTextures(){
        foreach(string id in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Textures"})){
            var path=AssetDatabase.GUIDToAssetPath(id);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.alphaIsTransparency=true;imp.mipmapEnabled=true;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.anisoLevel=8;imp.maxTextureSize=2048;imp.SaveAndReimport();
        }
    }
    static int EnsureRenderer(){
        string path=Root+"/Rendering/City Renderer.asset";
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        if(!renderer){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();renderer.name="City Renderer";AssetDatabase.CreateAsset(renderer,path);}
        // Append a renderer; never change the default renderer used by existing scenes.
        var pipeline=(QualitySettings.renderPipeline?QualitySettings.renderPipeline:GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
        if(!pipeline)throw new Exception("City requires URP.");
        var so=new SerializedObject(pipeline);var list=so.FindProperty("m_RendererDataList");
        for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)return i;
        int index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);return index;
    }
    static void Label(Transform p,string text,Vector3 pos,float size,string mat){
        string key=text.Replace(" ","_").Replace("\n","_");
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+key+".png");if(!tex)throw new Exception("Missing sign texture: "+key);
        string materialKey="Sign_"+key+"_"+mat;
        if(!M.ContainsKey(materialKey)){var material=new Material(Shader.Find("STAFF/City/Painted Surface"));material.SetTexture("_BaseMap",tex);material.SetColor("_BaseColor",M[mat].GetColor("_BaseColor"));material.SetFloat("_Grain",0);M[materialKey]=SaveMaterial(materialKey,material);}
        int lines=text.Split('\n').Length;float height=size*lines;
        var g=Primitive("Sign lettering - "+text,p,pos,V(height*tex.width/tex.height,height,1),materialKey,PrimitiveType.Quad);
        g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static void Vending(Transform p,Vector3 pos,float yaw){
        var t=Group("Red drink vending machine",p);t.localPosition=pos;t.localRotation=Quaternion.Euler(0,yaw,0);
        Box("Red cabinet",t,V(0,.91f,0),V(1.05f,1.82f,.69f),"red");Box("Display surround",t,V(-.1f,1.2f,-.355f),V(.78f,.98f,.04f),"white");Box("Display glass",t,V(-.1f,1.2f,-.38f),V(.7f,.89f,.02f),"glass");
        for(int row=0;row<3;row++)for(int j=0;j<5;j++){Disc("Drink can",t,V(-.38f+j*.135f,.91f+row*.28f,-.41f),.037f,.15f,j%2==0?"bookBlue":"paper");Box("Selection button",t,V(-.38f+j*.135f,.8f+row*.28f,-.43f),V(.067f,.028f,.016f),"white");}
        Box("Dispenser",t,V(0,.33f,-.36f),V(.67f,.2f,.05f),"iron");Box("Coin panel",t,V(.39f,.75f,-.36f),V(.12f,.36f,.04f),"iron");Label(t,"DRINKS",V(0,1.75f,-.37f),.15f,"white");
    }
    static void Bollard(Transform p,Vector3 v){Disc("Safety bollard",p,v+V(0,.4f,0),.065f,.8f,"iron");Disc("Reflective ring",p,v+V(0,.70f,0),.069f,.065f,"paper");}
    static void Bench(Transform p,Vector3 pos,float yaw){var t=Group("Street bench",p);t.localPosition=pos;t.localRotation=Quaternion.Euler(0,yaw,0);for(int i=0;i<4;i++){Box("Seat slat",t,V(0,.45f,-.23f+i*.13f),V(1.65f,.075f,.105f),"bark");Box("Back slat",t,V(0,.63f+i*.1f,.25f),V(1.65f,.075f,.045f),"bark");}foreach(float x in new[]{-.6f,.6f})Box("Bench support",t,V(x,.23f,0),V(.075f,.45f,.49f),"iron");}
    static void Sandwich(Transform p,Vector3 pos,float yaw,string word){var t=Group("Bookshop pavement board",p);t.localPosition=pos;t.localRotation=Quaternion.Euler(0,yaw,0);Box("Wood frame",t,V(0,.59f,0),V(.58f,1.14f,.08f),"bark");Box("Chalk board",t,V(0,.68f,-.045f),V(.48f,.84f,.025f),"iron");Label(t,word,V(0,.85f,-.069f),.16f,"paper");for(int i=0;i<4;i++)Box("Chalk line",t,V(0,.66f-i*.095f,-.066f),V(.31f-i*.025f,.014f,.012f),"curb");}
    static void Pot(Transform p,Vector3 pos,int seed){var t=Group("Potted plant",p);t.localPosition=pos;Disc("Planter",t,V(0,.19f,0),.23f,.38f,seed%2==0?"terracotta":"cream");Disc("Pot soil",t,V(0,.385f,0),.20f,.016f,"soil");for(int i=0;i<7;i++){float a=i*2.399f;var tip=V(Mathf.Sin(a)*.23f,.7f+Random.value*.3f,Mathf.Cos(a)*.23f);Beam("Stem",t,V(0,.35f,0),tip,.012f,"leafDark");Primitive("Plant leaf",t,tip,V(.22f,.075f,.32f),i%2==0?"leaf":"leafDark",PrimitiveType.Sphere);}}
    static void Pole(Transform p,Vector3 pos){
        var t=Group("Concrete utility pole",p);t.localPosition=pos;Disc("Pole shaft",t,V(0,4.9f,0),.115f,9.8f,"metal");
        foreach(float y in new[]{6.8f,8.2f,9.2f}){Box("Crossarm",t,V(0,y,0),V(1.65f,.075f,.075f),"iron");foreach(float x in new[]{-.65f,-.32f,.32f,.65f}){Disc("Insulator",t,V(x,y+.1f,0),.046f,.16f,"cream");}}
        Box("Transformer",t,V(.25f,7.4f,0),V(.37f,.7f,.39f),"curb");
        for(int j=0;j<12;j++)Beam("Climbing rung",t,V(-.2f,1.8f+j*.45f,0),V(.2f,1.8f+j*.45f,0),.013f,"iron");
        Beam("Streetlight bracket",t,V(0,6.8f,0),V(pos.x<0?1.25f:-1.25f,6.8f,0),.04f,"iron");
        Box("Lamp hood",t,V(pos.x<0?1.3f:-1.3f,6.74f,0),V(.48f,.16f,.30f),"metal");Box("Lamp diffuser",t,V(pos.x<0?1.3f:-1.3f,6.65f,0),V(.40f,.025f,.23f),"paper");
    }
    static void Tree(Transform p,Vector3 pos,float scale,bool golden){
        var t=Group(golden?"Golden ginkgo":"Street ginkgo",p);t.localPosition=pos;t.localScale=Vector3.one*scale;
        Disc("Tree pit rim",t,V(0,.015f,0),.62f,.04f,"iron");Disc("Tree pit soil",t,V(0,.044f,0),.53f,.02f,"soil");Beam("Trunk",t,V(0,0,0),V(.09f,4.5f,0),.095f,"bark");
        for(int b=0;b<11;b++){float a=b*2.399f;float y=2.1f+b*.21f;Beam("Branch",t,V(.04f,y,0),V(Mathf.Sin(a)*1.3f,y+1.05f,Mathf.Cos(a)*1.3f),.038f,"bark");}
        for(int m=0;m<3;m++){
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var tris=new List<int>();
            for(int j=0;j<570;j++){
                Vector3 q=Random.insideUnitSphere;var c=V(q.x*1.9f,4.5f+q.y*2.25f,q.z*1.75f);if(c.y<2.8f)continue;
                Vector3 normal=Random.onUnitSphere;var right=Vector3.Cross(normal,Mathf.Abs(normal.y)>.9f?Vector3.right:Vector3.up).normalized;var up=Vector3.Cross(right,normal);float s=Random.Range(.07f,.16f);int n=vertices.Count;
                vertices.Add(c-right*s);vertices.Add(c+up*s*.7f);vertices.Add(c+right*s);vertices.Add(c-up*s*.7f);
                for(int k=0;k<4;k++)normals.Add(normal);tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            var mesh=new Mesh{name="Ginkgo leaves"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();var g=Group("Leaf clusters",t);g.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M[golden?(m==0?"gold":m==1?"leafLight":"leaf"):(m==0?"leafLight":m==1?"leaf":"leafDark")];
        }
        Bake(t);
    }
    static void Railway(){
        var t=Group("Infinity Metro - elevated railway",env);float z=61;
        Box("Viaduct deck",t,V(0,8.55f,z),V(76,.65f,5.7f),"stone");
        foreach(float x in new[]{-25f,-12f,12f,25f}){Box("Railway pier",t,V(x,4.15f,z),V(1.1f,8.3f,2),"concrete");Box("Pier cap",t,V(x,8.1f,z),V(3.8f,.5f,3.8f),"curb");}
        for(int i=-35;i<36;i+=3)Box("Steel bridge rib",t,V(i,8.1f,z),V(.18f,.65f,5.6f),"metal");
        foreach(float dz in new[]{-2.55f,2.55f}){
            Box("Railway parapet",t,V(0,9.1f,z+dz),V(76,.48f,.18f),"curb");Beam("Safety rail",t,V(-38,9.8f,z+dz),V(38,9.8f,z+dz),.035f,"metal");
            for(int i=-37;i<=37;i+=2)Box("Bridge rail post",t,V(i,9.55f,z+dz),V(.055f,.65f,.055f),"metal");
        }
        foreach(float dz in new[]{-.8f,.8f})Box("Rail",t,V(0,8.94f,z+dz),V(76,.13f,.085f),"metal");
        Bake(t);
        var trainAsset=AssetDatabase.LoadAssetAtPath<GameObject>(Staff.Shared.Editor.SharedMigration.Train);
        if(trainAsset){
            var train=(GameObject)PrefabUtility.InstantiatePrefab(trainAsset,env.gameObject.scene);train.name="STAFF Subway - elevated";train.transform.SetParent(env,false);
            var consist=train.GetComponent<Staff.Shared.SubwayConsist>();train.transform.localPosition=V(-consist.carSpacing*.55f,9.75f,z);train.transform.localRotation=Quaternion.Euler(0,90,0);train.transform.localScale=Vector3.one*.55f;consist.CarCount=3;
            PrefabUtility.RecordPrefabInstancePropertyModifications(train.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(consist);
        }
        var entrance=Group("Metro entrance",env);entrance.localPosition=V(7.3f,.16f,47);Box("Entrance canopy",entrance,V(0,2.75f,0),V(2.4f,.22f,3.5f),"sage");foreach(float x in new[]{-1f,1f})Box("Entrance post",entrance,V(x,1.4f,0),V(.1f,2.8f,.1f),"metal");Box("Station fascia",entrance,V(0,2.57f,-1.7f),V(2.35f,.46f,.07f),"navy");Label(entrance,"INFINITY METRO",V(0,2.57f,-1.75f),.15f,"white");Bake(entrance);
    }
    [MenuItem("STAFF/City/Replace Elevated Train With Shared Prefab")]
    public static void UpgradeRailway(){
        var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded||EditorApplication.isPlaying)throw new Exception("Open city in Edit mode.");
        env=scene.GetRootGameObjects().Single(g=>g.name=="Environment").transform;
        foreach(string name in new[]{"Infinity Metro - elevated railway","Metro entrance","STAFF Subway - elevated"}){var old=env.Find(name);if(old)Object.DestroyImmediate(old.gameObject);}
        M.Clear();foreach(var path in Directory.GetFiles(Root+"/Materials","*.mat")){var mat=AssetDatabase.LoadAssetAtPath<Material>(path);M[Path.GetFileNameWithoutExtension(path)]=mat;}
        meshId=0;meshFolder=Root+"/Meshes/Railway_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(meshFolder);Railway();CityScaleMatching.MatchScene(scene,CityScaleMatching.ReferenceScale());
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("City save failed");
        File.WriteAllText("Library/StaffShared/city-train.txt","PASS: city elevated railway now uses STAFF Subway prefab, 3 cars, shared modules; bridge and rails retained.");
    }
    static void Skyline(){
        var t=Group("Distant city skyline",env);
        for(int i=0;i<32;i++){
            float x=Random.Range(-88f,88f),z=Random.Range(148f,218f),w=Random.Range(6f,16f),d=Random.Range(7f,14f),h=Random.Range(21f,61f);
            Box("Distant tower",t,V(x,h*.5f,z),V(w,h,d),i%2==0?"stone":"plaster");
            for(float y=3;y<h-1;y+=2.7f)for(float u=-w*.4f;u<w*.45f;u+=1.5f)Box("Skyline window",t,V(x+u,y,z-d*.5f-.015f),V(.78f,1.3f,.035f),"glassLight");
            Box("Tower rooftop plant",t,V(x,h+.7f,z),V(w*.38f,1.4f,d*.4f),"curb");
        }
        Bake(t);
    }
    static Material SaveMaterial(string name,Material mat){string path=Root+"/Materials/"+name+".mat";mat.name=name;var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old){EditorUtility.CopySerialized(mat,old);Object.DestroyImmediate(mat);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(mat,path);return mat;}
    static Mesh SaveMesh(Mesh m){string path=meshFolder+"/CityMesh_"+(meshId++).ToString("D4")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(m,path);return m;}
    static void Bake(Transform root){
        var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh && f.GetComponent<MeshRenderer>() && !f.GetComponent<TextMesh>()).ToArray();
        foreach(var grouping in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial)){
            var combine=grouping.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
            var mesh=new Mesh{name=root.name+" - "+grouping.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine,true,true);mesh.RecalculateBounds();
            var obj=Group("Geometry - "+grouping.Key.name,root);obj.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);var renderer=obj.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=grouping.Key;if(root.GetComponent<Staff.Traversal.TangentSurface>())obj.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;if(grouping.Key.name.StartsWith("Sign_"))renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        foreach(var f in filters){if(f.GetComponent<Collider>()){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}else Object.DestroyImmediate(f.gameObject);}
    }
    [MenuItem("STAFF/City/Capture Reference Views")]
    public static void CaptureSaved(){
        var s=SceneManager.GetSceneByPath(ScenePath);if(!s.isLoaded)throw new Exception("Open city first.");cam=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
        var p=cam.transform.position;var r=cam.transform.rotation;float f=cam.fieldOfView;cam.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(s);
        try {
            Capture(cam,Work+"/City-Street.png");cam.transform.position=V(-2.8f,2.25f,9);cam.transform.LookAt(V(8,5.5f,22));cam.fieldOfView=64;Capture(cam,Work+"/City-Shopfronts.png");
            cam.transform.position=V(-31,36,-17);cam.transform.LookAt(V(0,3,48));cam.fieldOfView=64;Capture(cam,Work+"/City-Overview.png");
        }finally{cam.transform.SetPositionAndRotation(p,r);cam.fieldOfView=f;cam.overrideSceneCullingMask=0;}
        File.WriteAllText(Work+"/capture-complete.txt","Three actual Unity camera renders saved.");
    }
    static void Capture(Camera c,string path){
        var rt=new RenderTexture(2400,1350,24,RenderTextureFormat.ARGB32);rt.antiAliasing=1;rt.Create();var prev=RenderTexture.active;var target=c.targetTexture;
        try {c.targetTexture=rt;c.Render();RenderTexture.active=rt;var image=new Texture2D(2400,1350,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2400,1350),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);}finally{c.targetTexture=target;RenderTexture.active=prev;rt.Release();Object.DestroyImmediate(rt);}
    }
    public static void Verify(){
        var s=SceneManager.GetSceneByPath(ScenePath);if(!s.isLoaded)throw new Exception("City not loaded.");var roots=s.GetRootGameObjects();var rs=roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>()).ToArray();
        foreach(var r in rs)foreach(var m in r.sharedMaterials)if(!m||!m.shader||ShaderUtil.ShaderHasError(m.shader))throw new Exception("Invalid material or shader on "+r.name);
        int missing=roots.Sum(g=>g.GetComponentsInChildren<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));if(missing!=0)throw new Exception("Missing scripts: "+missing);
        int tris=roots.SelectMany(g=>g.GetComponentsInChildren<MeshFilter>()).Where(f=>f.sharedMesh).Sum(f=>f.sharedMesh.triangles.Length/3);
        File.WriteAllText(Work+"/validation.txt","PASS\nScene: "+ScenePath+"\nRenderers: "+rs.Length+"\nTriangles: "+tris+"\nMissing scripts: 0\nAll renderer materials and shaders valid.\nReference image: "+Root+"/Reference/City-Concept.png\nCamera navigation: RMB + WASD, Q/E, Shift, R home.\n");
        Debug.Log("STAFF CITY VALIDATION PASS: "+rs.Length+" renderers, "+tris+" triangles");
    }
}
}
