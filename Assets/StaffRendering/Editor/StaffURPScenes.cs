using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class StaffURPScenes {
 public static readonly string[] Scenes={"Assets/Scenes/SampleScene.unity","Assets/Scenes/STAFF_Subway_Reference.unity","Assets/StaffNishita/Scenes/Nishita Comparison.unity"};
 [MenuItem("STAFF/Rendering/Bake Current Scene (URP)")]
 public static void Bake(){if(!GraphicsSettings.defaultRenderPipeline)throw new Exception("URP is not active");if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())Lightmapping.BakeAsync();}
 public static void Configure(){
 Directory.CreateDirectory("Assets/StaffRendering/Lighting");
 foreach(var path in Scenes){var scene=EditorSceneManager.OpenScene(path);bool subway=path.Contains("Subway");bool nishita=path.Contains("Nishita");string lsPath="Assets/StaffRendering/Lighting/"+(subway?"Subway":nishita?"DynamicAtmosphere":"MathSpace")+".lighting";
 var ls=AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);if(!ls){ls=new LightingSettings();AssetDatabase.CreateAsset(ls,lsPath);}ls.name=Path.GetFileNameWithoutExtension(lsPath);ls.realtimeGI=false;ls.bakedGI=subway;ls.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;ls.lightmapResolution=8;ls.lightmapMaxSize=1024;ls.directSampleCount=32;ls.indirectSampleCount=128;ls.environmentSampleCount=64;ls.mixedBakeMode=MixedLightingMode.IndirectOnly;Lightmapping.lightingSettings=ls;EditorUtility.SetDirty(ls);
 foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))r.sharedMaterials=r.sharedMaterials.Select(StaffURPMaterials.Persistent).ToArray();
 foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){c.allowHDR=true;var data=c.GetUniversalAdditionalCameraData();data.renderPostProcessing=!nishita;data.antialiasing=AntialiasingMode.None;data.requiresDepthOption=CameraOverrideOption.On;}
 if(subway){foreach(var rootName in new[]{"Architecture - Greybox","Tripo - Platform Props","Wayfinding"}){var root=GameObject.Find(rootName);if(!root)continue;foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true)){var flags=GameObjectUtility.GetStaticEditorFlags(r.gameObject);GameObjectUtility.SetStaticEditorFlags(r.gameObject,flags|StaticEditorFlags.ContributeGI|StaticEditorFlags.ReflectionProbeStatic);r.receiveGI=ReceiveGI.Lightmaps;}}
 foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.name.StartsWith("Ceiling pool"))l.lightmapBakeType=LightmapBakeType.Mixed;
 // URP bloom keeps emissive fixtures and signs luminous without altering atmosphere exposure.
 var vp=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/StaffRendering/Lighting/SubwayPost.asset");if(!vp){vp=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(vp,"Assets/StaffRendering/Lighting/SubwayPost.asset");var bloom=vp.Add<Bloom>(true);bloom.threshold.Override(1.1f);bloom.intensity.Override(.18f);AssetDatabase.AddObjectToAsset(bloom,vp);EditorUtility.SetDirty(vp);}
 var vgo=GameObject.Find("URP Lighting and Post");if(!vgo)vgo=new GameObject("URP Lighting and Post");vgo.transform.SetParent(GameObject.Find("Lighting")?.transform,false);var volume=vgo.GetComponent<Volume>();if(!volume)volume=vgo.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=vp;
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }AssetDatabase.SaveAssets();
 }
 public static void Validate(){try{Configure();Directory.CreateDirectory("Library/StaffURPValidation");foreach(var path in Scenes){EditorSceneManager.OpenScene(path);var atmosphere=UnityEngine.Object.FindFirstObjectByType<Staff.Nishita.NishitaAtmosphere>();if(atmosphere){atmosphere.animate=false;atmosphere.Tick(true);}var cams=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.enabled).ToArray();foreach(var cam in cams){var rt=new RenderTexture(960,540,24,RenderTextureFormat.ARGBHalf);rt.Create();try{RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(960,540,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,960,540),0,0);t.Apply();File.WriteAllBytes("Library/StaffURPValidation/"+Path.GetFileNameWithoutExtension(path)+"-"+cam.name.Replace('/','_')+".png",t.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);}finally{rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
 }foreach(var id in AssetDatabase.FindAssets("t:Shader",new[]{"Assets"})){var s=AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(id));foreach(var e in ShaderUtil.GetShaderMessages(s))if(e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)Debug.LogError("SHADER_FAIL "+s.name+" "+e.message+" "+e.line);}Debug.Log("STAFF_URP_SCENES_RENDERED");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
