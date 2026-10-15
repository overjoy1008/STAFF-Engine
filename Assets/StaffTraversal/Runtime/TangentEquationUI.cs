using System;using System.Collections.Generic;using System.Globalization;using UnityEngine;using UnityEngine.UI;
namespace Staff.Traversal {
// Glyphs are real LaTeX. The substitution motion is sampled from official manimlib FadeIn.
[DefaultExecutionOrder(200)]
public sealed class TangentEquationUI:MonoBehaviour {
 [Serializable] public class Frame{public float alpha,y,scale;}
 [Serializable] public class Metric{public string key;public float x,y,width,height;}
 [Serializable] public class Metrics{public Metric[] entries;}
 readonly Dictionary<string,Metric> metrics=new Dictionary<string,Metric>();
 [Serializable] public class Motion{public Frame[] frames;public string source;}
 readonly Dictionary<string,Texture2D> glyphs=new Dictionary<string,Texture2D>();
 Canvas overlay;readonly List<RawImage> images=new List<RawImage>();int imageIndex;
 Motion motion;TangentTraversal traversal;Camera cameraView;float age;bool visible,locked;Vector3 target;
 public float Slope{get;private set;}public float Height{get;private set;}public float ContactX{get;private set;}public bool Vertical{get;private set;}
 public bool HasSubstitution=>locked;
 void OnEnable(){Initialize(GetComponent<TangentTraversal>());}
 public void Initialize(TangentTraversal owner){traversal=owner;foreach(var key in new[]{"symbol","prefix","left","right","rightMinus","vertical","minus","dot","0","1","2","3","4","5","6","7","8","9"})glyphs[key]=Resources.Load<Texture2D>("TangentMath/"+key+"/symbol");var measurement=Resources.Load<TextAsset>("TangentMath/metrics");if(measurement)foreach(var metric in JsonUtility.FromJson<Metrics>(measurement.text).entries)metrics[metric.key]=metric;var data=Resources.Load<TextAsset>("TangentMath/motion");if(data)motion=JsonUtility.FromJson<Motion>(data.text);var provenance=GetComponent<Staff.MathSpace.LatexSymbol>();if(!provenance)provenance=gameObject.AddComponent<Staff.MathSpace.LatexSymbol>();provenance.SetSource(@"y=f'(a)(x-a)+f(a)",glyphs["symbol"],"Assets/StaffTraversal/Resources/TangentMath/symbol/symbol.tex");}
 public void Measure(Vector3 source,Vector3 point){target=point;var d=point-source;ContactX=new Vector2(d.x,d.z).magnitude;Height=point.y;Vertical=ContactX<.001f;Slope=Vertical?0:Mathf.Tan(Mathf.Atan2(d.y,ContactX));}
 public void Lock(Vector3 source,Vector3 point){Measure(source,point);locked=true;age=0;}
 public void Present(bool show,Camera cam,Vector3 source,Vector3 point){cameraView=cam;if(!show){visible=false;locked=false;age=0;return;}if(!visible)age=0;visible=true;if(!locked)Measure(source,point);else target=point;if(locked)age+=Time.deltaTime;}
 static string Number(float v)=>(Mathf.Abs(v)<.005f?0:v).ToString("0.00",CultureInfo.InvariantCulture);
 List<string> Tokens(){var result=new List<string>();Action<float> number=value=>{foreach(char c in Number(value))result.Add(c=='-'?"minus":c=='.'?"dot":c.ToString());};result.Add(Vertical?"vertical":"prefix");if(Vertical){number(ContactX);return result;}number(Slope);result.Add("left");number(ContactX);result.Add(Height<=-.005f?"rightMinus":"right");number(Mathf.Abs(Height));return result;}
 public static Vector2 DisplaySize(Camera camera,Vector3 point){float depth=Mathf.Max(.1f,Vector3.Dot(point-camera.transform.position,camera.transform.forward));float units=camera.orthographic?camera.orthographicSize*2/Mathf.Max(1,camera.pixelHeight):2*depth*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Max(1,camera.pixelHeight);float h=Mathf.Max(24,.5f/units);h=Mathf.Min(h,Mathf.Max(1,(Screen.width-32)/6f));return new Vector2(h*6,h);}
 void EnsureOverlay(){if(overlay)return;var root=new GameObject("UI - Tangent equation",typeof(RectTransform),typeof(Canvas));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,gameObject.scene);overlay=root.GetComponent<Canvas>();overlay.renderMode=RenderMode.ScreenSpaceOverlay;overlay.sortingOrder=100;root.layer=5;}
 void Draw(Rect rect,Texture texture,Rect uv,Color color){if(!texture||color.a<=0)return;RawImage image;if(imageIndex==images.Count){var go=new GameObject("LaTeX glyph",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));go.layer=5;go.transform.SetParent(overlay.transform,false);image=go.GetComponent<RawImage>();image.raycastTarget=false;images.Add(image);}else image=images[imageIndex];imageIndex++;image.gameObject.SetActive(true);image.texture=texture;image.uvRect=uv;image.color=color;var rt=image.rectTransform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(rect.x,-rect.y);rt.sizeDelta=new Vector2(rect.width,rect.height);}
 void OnDisable(){if(overlay)overlay.gameObject.SetActive(false);}
 void OnDestroy(){if(overlay)Destroy(overlay.gameObject);}
 void LateUpdate(){EnsureOverlay();overlay.gameObject.SetActive(visible);foreach(var image in images)image.gameObject.SetActive(false);imageIndex=0;if(!visible||!cameraView||!traversal||glyphs.Count==0||metrics.Count==0)return;var point=cameraView.WorldToScreenPoint(target);if(point.z<=0)return;var size=DisplaySize(cameraView,target);float x=Mathf.Clamp(point.x-size.x*.5f,16,Mathf.Max(16,Screen.width-size.x-16));float y=Mathf.Clamp(Screen.height-point.y-size.y*1.9f,16,Mathf.Max(16,Screen.height-size.y-16));var tint=Color.white;
 Frame f=motion!=null&&motion.frames.Length>0?motion.frames[Mathf.Min(motion.frames.Length-1,Mathf.FloorToInt(Mathf.Clamp01(age/.45f)*(motion.frames.Length-1)))]:new Frame{alpha=1,scale=1};
 tint=new Color(1,1,1,locked?1-f.alpha:1);var symbol=glyphs["symbol"];if(symbol)Draw(new Rect(x,y,size.x,size.y),symbol,new Rect(0,0,1,1),tint);if(!locked)return;
 // All numeric glyphs use original TeX bounds and a shared baseline, not individual fitted heights.
 var tokens=Tokens();float total=0;foreach(var key in tokens)total+=metrics[key].width+1.1f;float sx=size.x*.94f/Mathf.Max(1,total);float sy=size.y*.61f/metrics["symbol"].height;float pen=x+size.x*.03f;float baseline=y+size.y*.195f-metrics["symbol"].y*sy-f.y*size.y;
 tint=new Color(1,1,1,f.alpha);foreach(var key in tokens){var t=glyphs[key];var m=metrics[key];float width=m.width*sx,height=m.height*sy;if(t)Draw(new Rect(pen+(1-f.scale)*width/2,baseline+m.y*sy+(1-f.scale)*height/2,width*f.scale,height*f.scale),t,new Rect(32f/t.width,32f/t.height,(t.width-64f)/t.width,(t.height-64f)/t.height),tint);pen+=(m.width+1.1f)*sx;}
 }
}
}
