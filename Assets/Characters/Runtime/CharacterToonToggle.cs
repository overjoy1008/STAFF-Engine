using System;
using System.Collections.Generic;
using UnityEngine;
namespace Staff.Characters {
 public enum CharacterShaderMode { HoyoToon, HoyoTwoTone, CharacterToon, Smooth }
 public sealed class CharacterToonToggle : IDisposable {
  readonly Dictionary<Material,Material> converted=new Dictionary<Material,Material>();
  readonly Dictionary<Material,Material> legacy=new Dictionary<Material,Material>();
  readonly Dictionary<Material,Material> originals=new Dictionary<Material,Material>();
  readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
  readonly List<Texture2D> masks=new List<Texture2D>();
  Texture2D lightMask,dataMask,dataMask2;
  Texture2D Solid(Color color){var t=new Texture2D(1,1,TextureFormat.RGBA32,false,true){name="MMD neutral mask",hideFlags=HideFlags.HideAndDontSave};t.SetPixel(0,0,color);t.Apply();masks.Add(t);return t;}
  public void Apply(GameObject model,CharacterShaderMode mode){
   if(!model)return;
   var template=Resources.Load<Material>("StaffHoyoTemplate");
   foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){
    var materials=renderer.sharedMaterials;
    for(int i=0;i<materials.Length;i++){
     var source=materials[i];if(!source)continue;if(originals.TryGetValue(source,out var original))source=original;
     var result=source;
     if((mode==CharacterShaderMode.HoyoToon||mode==CharacterShaderMode.HoyoTwoTone)&&template){
      if(!converted.TryGetValue(source,out result)){
       result=new Material(template){name=source.name+" (HoyoToon MMD)"};
       string texture=source.HasProperty("_BaseMap")?"_BaseMap":"_MainTex",color=source.HasProperty("_BaseColor")?"_BaseColor":"_Color";
       if(source.HasProperty(texture)){result.SetTexture("_MainTex",source.GetTexture(texture));result.SetTextureScale("_MainTex",source.GetTextureScale(texture));result.SetTextureOffset("_MainTex",source.GetTextureOffset(texture));}
       var tint=source.HasProperty(color)?source.GetColor(color):Color.white;
       for(int j=1;j<=5;j++)result.SetColor(j==1?"_Color":"_Color"+j,tint);
       lightMask=lightMask?lightMask:Solid(new Color(.5f,.5f,.5f,1));dataMask=dataMask?dataMask:Solid(new Color(.9f,0,0,0));dataMask2=dataMask2?dataMask2:Solid(new Color(1,.25f,0,0));
       result.SetTexture("_LightTex",lightMask);result.SetTexture("_OtherDataTex",dataMask);result.SetTexture("_OtherDataTex2",dataMask2);
       result.SetFloat("_UseBumpMap",0);result.SetFloat("_SpecIntensity",0);result.SetFloat("_DoubleSided",0);result.SetFloat("_DoubleUV",0);result.SetFloat("_UseAlpha",1);result.SetFloat("_AlphaCutoff",.1f);result.SetFloat("_AlbedoSmoothness",.18f);
       foreach(var name in new[]{"_PostShallowTint","_PostShallowFadeTint","_PostShadowTint","_PostShadowFadeTint","_PostFrontTint","_PostSssTint"})result.SetColor(name,Color.white);
       result.SetFloat("_StaffNeutralLighting",1);
       result.SetFloat("_StaffExposure",.8f);
       converted.Add(source,result);originals.Add(result,source);
      }
     }
     if(mode==CharacterShaderMode.CharacterToon && source.shader.name!="STAFF/Character Toon"){
     if(!legacy.TryGetValue(source,out result)){
      result=new Material(Shader.Find("STAFF/Character Toon")){name=source.name+" (CharacterToon)"};
      string tc=source.HasProperty("_BaseMap")?"_BaseMap":"_MainTex",cc=source.HasProperty("_BaseColor")?"_BaseColor":"_Color";
      if(source.HasProperty(tc))result.SetTexture("_BaseMap",source.GetTexture(tc));
      if(source.HasProperty(cc))result.SetColor("_BaseColor",source.GetColor(cc));
      legacy.Add(source,result);originals.Add(result,source);
     }
    }
    materials[i]=result;
    }
    renderer.sharedMaterials=materials;
    renderer.GetPropertyBlock(properties);properties.SetFloat("_StaffTwoTone",mode==CharacterShaderMode.HoyoTwoTone?1:0);properties.SetFloat("_ToonEnabled",mode==CharacterShaderMode.CharacterToon?1:0);renderer.SetPropertyBlock(properties);properties.Clear();
   }
  }
  public void Dispose(){foreach(var m in converted.Values)UnityEngine.Object.Destroy(m);foreach(var m in legacy.Values)UnityEngine.Object.Destroy(m);foreach(var t in masks)UnityEngine.Object.Destroy(t);legacy.Clear();converted.Clear();originals.Clear();masks.Clear();}
 }
}
