using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class StaffURPMaterials {
 public static bool Needs(Material m)=>m&&m.shader&&(m.shader.name=="Standard"||m.shader.name=="Standard (Specular setup)");
 public static void Convert(Material m){bool spec=m.shader.name.Contains("Specular");var tex=m.GetTexture("_MainTex");var tint=m.color;var scale=m.GetTextureScale("_MainTex");var offset=m.GetTextureOffset("_MainTex");float smooth=m.GetFloat(m.GetTexture(spec?"_SpecGlossMap":"_MetallicGlossMap")?"_GlossMapScale":"_Glossiness");float mode=m.GetFloat("_Mode");m.shader=Shader.Find("Universal Render Pipeline/Lit");m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",tint);m.SetTextureScale("_BaseMap",scale);m.SetTextureOffset("_BaseMap",offset);m.SetFloat("_WorkflowMode",spec?0:1);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Surface",mode>=2?1:0);m.SetFloat("_Blend",mode==3?1:0);m.SetFloat("_AlphaClip",mode==1?1:0);var gui=(ShaderGUI)Activator.CreateInstance(typeof(Unity.Rendering.Universal.ShaderUtils).Assembly.GetType("UnityEditor.Rendering.Universal.ShaderGUI.LitShader"));gui.ValidateMaterial(m);}
 public static Material Persistent(Material m){if(!Needs(m))return m;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m,out string guid,out long id);string path="Assets/StaffRendering/Materials/"+(string.IsNullOrEmpty(guid)?"builtin":guid)+"-"+id+".mat";var converted=AssetDatabase.LoadAssetAtPath<Material>(path);if(converted)return converted;Directory.CreateDirectory("Assets/StaffRendering/Materials");converted=new Material(m){name=m.name+" URP"};Convert(converted);AssetDatabase.CreateAsset(converted,path);return converted;}
 public static void Models(){foreach(var guid in AssetDatabase.FindAssets("t:Model",new[]{"Assets"})){var path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(!importer)continue;bool dirty=false;foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(path))if(obj is Material m&&Needs(m)){var converted=Persistent(m);importer.AddRemap(new AssetImporter.SourceAssetIdentifier(m),converted);dirty=true;}if(dirty)importer.SaveAndReimport();}AssetDatabase.SaveAssets();}
}
public class StaffURPModelMaterials:AssetPostprocessor {
 void OnPostprocessMaterial(Material material){if(StaffURPMaterials.Needs(material))StaffURPMaterials.Convert(material);}
}
