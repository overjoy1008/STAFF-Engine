using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.Rendering;
public static class StaffURPMigration {
 public static void Run(){try {
 var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
 if(!rp)throw new Exception("PC URP asset missing");
 GraphicsSettings.defaultRenderPipeline=rp;for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=QualitySettings.names[i]=="Mobile"?AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset"):rp;}
 rp.supportsHDR=true;rp.msaaSampleCount=1;rp.supportsCameraDepthTexture=true;rp.supportsCameraOpaqueTexture=true;
 foreach(var id in AssetDatabase.FindAssets("t:UniversalRendererData")){
 var rd=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(id));var so=new SerializedObject(rd);so.FindProperty("m_RenderingMode").intValue=0;so.ApplyModifiedPropertiesWithoutUndo();
 var f=rd.rendererFeatures.OfType<Staff.Rendering.StaffRenderFeature>().FirstOrDefault();if(!f){f=ScriptableObject.CreateInstance<Staff.Rendering.StaffRenderFeature>();f.name="STAFF Atmosphere and HoyoToon";AssetDatabase.AddObjectToAsset(f,rd);rd.rendererFeatures.Add(f);}f.geometryShader=Shader.Find("Hidden/STAFF/URP Geometry");EditorUtility.SetDirty(f);EditorUtility.SetDirty(rd);
 }
 int converted=0;foreach(var id in AssetDatabase.FindAssets("t:Material")){var path=AssetDatabase.GUIDToAssetPath(id);if(!path.EndsWith(".mat"))continue;var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m||!m.shader||m.shader.name!="Standard")continue;
 var tex=m.GetTexture("_MainTex");var color=m.GetColor("_Color");var scale=m.GetTextureScale("_MainTex");var offset=m.GetTextureOffset("_MainTex");float smooth=m.GetFloat("_Glossiness");float mode=m.GetFloat("_Mode");m.shader=Shader.Find("Universal Render Pipeline/Lit");m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",color);m.SetTextureScale("_BaseMap",scale);m.SetTextureOffset("_BaseMap",offset);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Surface",mode>=2?1:0);m.SetFloat("_AlphaClip",mode==1?1:0);var gui=(ShaderGUI)Activator.CreateInstance(typeof(Unity.Rendering.Universal.ShaderUtils).Assembly.GetType("UnityEditor.Rendering.Universal.ShaderGUI.LitShader"));gui.ValidateMaterial(m);EditorUtility.SetDirty(m);converted++;}
 EditorUtility.SetDirty(rp);AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("STAFF_URP_CONFIGURED materials="+converted);
 foreach(var id in AssetDatabase.FindAssets("t:Shader",new[]{"Assets"})){var sh=AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(id));foreach(var e in ShaderUtil.GetShaderMessages(sh))if(e.severity==ShaderCompilerMessageSeverity.Error)Debug.LogError(sh.name+": "+e.message+" "+e.file+":"+e.line);}
 EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
