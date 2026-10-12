using System;
using UnityEngine;
namespace Staff.Subway {
 // Anime background palette: colored direct light and separate cool cloud shadow/sky bounce.
 public static class CloudTimePalette {
  [Serializable] public sealed class Settings {
   public bool enabled=true;
   [Range(0,1)] public float strength=.9f;
   [ColorUsage(false)] public Color daylight=new Color(1,.97f,.92f);
   [ColorUsage(false)] public Color dayShadow=new Color(.53f,.68f,.88f);
   [ColorUsage(false)] public Color dawnLight=new Color(1,.79f,.56f);
   [ColorUsage(false)] public Color dawnShadow=new Color(.59f,.61f,.78f);
   [ColorUsage(false)] public Color sunsetLight=new Color(1,.53f,.38f);
   [ColorUsage(false)] public Color sunsetShadow=new Color(.61f,.43f,.69f);
   [ColorUsage(false)] public Color twilightLight=new Color(.72f,.58f,.86f);
   [ColorUsage(false)] public Color twilightShadow=new Color(.35f,.39f,.65f);
   [ColorUsage(false)] public Color nightShadow=new Color(.24f,.34f,.57f);
  }
  public struct Lighting {public Color sun,top,bottom;}
  static Lighting State(Color sun,float direct,Color shade,float ambient){return new Lighting{sun=sun.linear*direct,bottom=shade.linear*ambient,top=Color.Lerp(shade.linear,sun.linear,.22f)*ambient*1.3f};}
  static Lighting Blend(Lighting a,Lighting b,float t){t=Mathf.SmoothStep(0,1,t);return new Lighting{sun=Color.Lerp(a.sun,b.sun,t),top=Color.Lerp(a.top,b.top,t),bottom=Color.Lerp(a.bottom,b.bottom,t)};}
  public static Lighting Evaluate(Settings s,float elevation,float morning){
   var warm=Color.Lerp(s.sunsetLight,s.dawnLight,morning);var shadow=Color.Lerp(s.sunsetShadow,s.dawnShadow,morning);
   var night=State(s.nightShadow,0,s.nightShadow,.08f);
   var twilight=State(s.twilightLight,.10f,s.twilightShadow,.23f);
   var horizon=State(warm,.70f,shadow,.40f);
   var low=State(warm,1.05f,shadow,.48f);
   var day=State(s.daylight,1.25f,s.dayShadow,.50f);
   if(elevation<-6)return Blend(night,twilight,Mathf.InverseLerp(-14,-6,elevation));
   if(elevation<1)return Blend(twilight,horizon,Mathf.InverseLerp(-6,1,elevation));
   if(elevation<8)return Blend(horizon,low,Mathf.InverseLerp(1,8,elevation));
   return Blend(low,day,Mathf.InverseLerp(8,28,elevation));
  }
 }
}
