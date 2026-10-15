using System;using System.Collections.Generic;using UnityEngine;using UnityEngine.UI;
namespace Staff.Traversal {
[Serializable] public class TangentContour{public Vector2[] points;}
[Serializable] public class TangentGlyph{public string key;public float width;public TangentContour[] contours;}
[Serializable] public class TangentSlot{public string key;public float gap;}
[Serializable] public class TangentWriteGlyph{public float fill,stroke,coverage;}
[Serializable] public class TangentWriteFrame{public TangentWriteGlyph[] glyphs;}
[Serializable] public class TangentContourData{public string source;public TangentGlyph[] glyphs;public TangentSlot[] slots;public float width,bottom,height;public TangentWriteFrame[] writeFrames;public float[] smooth;}
public sealed class TangentPlot {
 public Vector2[][] paths;public float fill=1,coverage=1,stroke;
 public TangentPlot(Vector2[][] p){paths=p;}
}
// UI vector geometry, retaining the actual TeX contours, including counters/holes.
public sealed class TangentContourGraphic:MaskableGraphic {
 public List<TangentPlot> plots=new List<TangentPlot>();public Vector2 scale=Vector2.one;public float baseline;
 struct Edge{public Vector2 a,b;public float middle;public float X(float y)=>Mathf.LerpUnclamped(a.x,b.x,(y-a.y)/(b.y-a.y));}
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();foreach(var plot in plots){var paths=Partial(plot.paths,plot.coverage);if(plot.fill>0)Fill(vh,paths,plot.fill);if(plot.stroke>0)foreach(var path in paths)for(int i=0;i+1<path.Length;i++)Stroke(vh,path[i],path[i+1],plot.stroke*Mathf.Max(.12f,scale.y/135f));}}
 Vector2 Pixel(Vector2 p)=>new Vector2(p.x*scale.x,(p.y-baseline)*scale.y);
 void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,float alpha){int n=vh.currentVertCount;if(n>64000)return;var ink=color;ink.a*=alpha;vh.AddVert(a,ink,Vector2.zero);vh.AddVert(b,ink,Vector2.zero);vh.AddVert(c,ink,Vector2.zero);vh.AddVert(d,ink,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
 void Stroke(VertexHelper vh,Vector2 a,Vector2 b,float width){a=Pixel(a);b=Pixel(b);var d=b-a;if(d.sqrMagnitude<.000001f)return;var n=new Vector2(-d.y,d.x).normalized*Mathf.Max(.2f,width*.5f);Quad(vh,a+n,b+n,b-n,a-n,1);}
 void Fill(VertexHelper vh,Vector2[][] paths,float opacity){var ys=new List<float>();foreach(var p in paths)foreach(var v in p)ys.Add(v.y);ys.Sort();var crossings=new List<Edge>();for(int i=0;i+1<ys.Count;i++){float lo=ys[i],hi=ys[i+1];if(hi-lo<.000001f)continue;float mid=(lo+hi)*.5f;crossings.Clear();foreach(var p in paths)for(int j=0;j<p.Length;j++){var a=p[j];var b=p[(j+1)%p.Length];if((a.y<=mid&&b.y>mid)||(b.y<=mid&&a.y>mid)){var edge=new Edge{a=a,b=b};edge.middle=edge.X(mid);crossings.Add(edge);}}crossings.Sort((a,b)=>a.middle.CompareTo(b.middle));for(int j=0;j+1<crossings.Count;j+=2){var l=crossings[j];var r=crossings[j+1];Quad(vh,Pixel(new Vector2(l.X(lo),lo)),Pixel(new Vector2(r.X(lo),lo)),Pixel(new Vector2(r.X(hi),hi)),Pixel(new Vector2(l.X(hi),hi)),opacity);}}}
 public static Vector2[][] Partial(Vector2[][] paths,float progress){if(progress>=.9999f)return paths;if(progress<=0)return Array.Empty<Vector2[]>();var result=new List<Vector2[]>();foreach(var path in paths){float count=progress*path.Length;int n=Mathf.FloorToInt(count);var p=new Vector2[n+2];for(int i=0;i<=n;i++)p[i]=path[i%path.Length];p[n+1]=Vector2.Lerp(path[n%path.Length],path[(n+1)%path.Length],count-n);result.Add(p);}return result.ToArray();}
}
}
