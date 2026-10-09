using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace Staff.Characters {
 [DefaultExecutionOrder(2000), DisallowMultipleComponent]
 public sealed class AdventureExpressions : MonoBehaviour {
  [Serializable] public class Weight { public string name; public float weight; }
  [Serializable] public class Preset { public string id,title; public float blush; public Weight[] weights; }
  [Serializable] public class Character { public string modelId,character,title; public string[] morphNames; public Preset[] presets; }
  [Serializable] public class Data { public int schemaVersion; public float transitionSeconds; public Character[] characters; }
  class Binding { public SkinnedMeshRenderer renderer; public int index; public string name; public float original,from,current,target; }
  class Overlay { public SkinnedMeshRenderer source,renderer; }
  readonly List<Binding> bindings=new List<Binding>();
  readonly List<Overlay> overlays=new List<Overlay>();
  AdventurePlayer player; CharacterSwitcher switcher; Animator animator;
  ExpressionCatalog catalog; Data data; Character character; Material blushMaterial;
  InputAction previous,next,neutral,automatic;
  Font hudFont;
  float elapsed,blushFrom,blushValue,blushTarget;
  public float TransitionSeconds=.45f;
  public bool Manual { get; private set; }
  public int Index { get; private set; }
  public int Count => character?.presets?.Length ?? 0;
  public string CurrentTitle => Count>0?character.presets[Index].title:"표정 미지원";
  public float Blush => blushValue;
  public int BindingCount => bindings.Count;
  public int OverlayCount => overlays.Count;
  public string[] MissingShapes { get; private set; }=Array.Empty<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Register(){SceneManager.sceneLoaded-=OnScene;SceneManager.sceneLoaded+=OnScene;}
  static void OnScene(Scene scene,LoadSceneMode mode){foreach(var p in FindObjectsByType<AdventurePlayer>(FindObjectsSortMode.None))if(!p.GetComponent<AdventureExpressions>())p.gameObject.AddComponent<AdventureExpressions>();}
  void Awake(){player=GetComponent<AdventurePlayer>();switcher=GetComponent<CharacterSwitcher>();catalog=Resources.Load<ExpressionCatalog>("StaffExpressionCatalog");if(catalog&&catalog.json){data=JsonUtility.FromJson<Data>(catalog.json.text);TransitionSeconds=data.transitionSeconds;}}
  void OnEnable(){previous=new InputAction("PreviousExpression",InputActionType.Button,"<Keyboard>/leftArrow");next=new InputAction("NextExpression",InputActionType.Button,"<Keyboard>/rightArrow");neutral=new InputAction("NeutralExpression",InputActionType.Button,"<Keyboard>/home");automatic=new InputAction("AutomaticFace",InputActionType.Button,"<Keyboard>/f7");previous.Enable();next.Enable();neutral.Enable();automatic.Enable();}
  void OnDisable(){previous?.Dispose();next?.Dispose();neutral?.Dispose();automatic?.Dispose();Release();ClearModel();}
  void Update(){RefreshModel();HandleInput(player&&player.InputCaptured&&!player.InputBlocked&&Application.isFocused&&!(switcher&&switcher.PickerOpen));}
  public void HandleInput(bool accept){if(!accept||previous==null)return;if(automatic.WasPressedThisFrame()){Release();return;}if(neutral.WasPressedThisFrame())Select(0);else if(previous.WasPressedThisFrame())Step(-1);else if(next.WasPressedThisFrame())Step(1);}
  public void Step(int direction){RefreshModel();if(Count>0)Select((Index+direction+Count)%Count);}
  public bool Select(int index){RefreshModel();if(index<0||index>=Count)return false;
   bool wasManual=Manual;Manual=true;Index=index;var preset=character.presets[index];var weights=new Dictionary<string,float>();foreach(var w in preset.weights)weights[w.name]=w.weight;
   foreach(var b in bindings){b.from=wasManual?b.current:b.renderer.GetBlendShapeWeight(b.index);b.current=b.from;b.target=weights.TryGetValue(b.name,out var w)?w:0;}
   blushFrom=blushValue;blushTarget=preset.blush/100f;elapsed=0;return true;
  }
  public void Release(){Manual=false;foreach(var b in bindings)if(b.renderer)b.renderer.SetBlendShapeWeight(b.index,b.original);blushValue=blushFrom=blushTarget=0;if(blushMaterial)blushMaterial.SetFloat("_Strength",0);}
  void ClearModel(){foreach(var b in bindings)if(b.renderer)b.renderer.SetBlendShapeWeight(b.index,b.original);bindings.Clear();foreach(var o in overlays)if(o.renderer){o.renderer.gameObject.SetActive(false);Destroy(o.renderer.gameObject);}overlays.Clear();if(blushMaterial)Destroy(blushMaterial);blushMaterial=null;character=null;animator=null;blushValue=0;}
  public void RefreshModel(){if(!player||player.Animator==animator)return;bool keep=Manual;int selected=Index;ClearModel();animator=player.Animator;if(!animator||!catalog||data?.characters==null)return;
   var entry=Array.Find(catalog.characters,c=>c.avatar==animator.avatar);if(entry==null)return;character=Array.Find(data.characters,c=>c.character==entry.id);if(character==null)return;
   var names=new HashSet<string>(character.morphNames);var found=new HashSet<string>();var sources=animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
   foreach(var r in sources){var mesh=r.sharedMesh;if(!mesh)continue;for(int i=0;i<mesh.blendShapeCount;i++){string name=DanceFacePlayback.ShapeName(mesh.GetBlendShapeName(i));if(!names.Contains(name))continue;float v=r.GetBlendShapeWeight(i);bindings.Add(new Binding{renderer=r,index=i,name=name,original=v,current=v});found.Add(name);}}
   var missing=new List<string>();foreach(var preset in character.presets)foreach(var w in preset.weights)if(!found.Contains(w.name)&&!missing.Contains(w.name))missing.Add(w.name);MissingShapes=missing.ToArray();if(missing.Count>0)Debug.LogWarning("Expression shapes missing: "+string.Join(", ",missing),this);
   blushMaterial=new Material(catalog.blushMaterial);blushMaterial.SetFloat("_Strength",0);
   foreach(var r in sources){var spec=Array.Find(entry.overlays,o=>o.source==r.sharedMesh);if(spec==null)continue;var obj=new GameObject("Expression cheek blush");obj.transform.SetParent(r.transform,false);var overlay=obj.AddComponent<SkinnedMeshRenderer>();overlay.sharedMesh=spec.blush;overlay.bones=r.bones;overlay.rootBone=r.rootBone;overlay.localBounds=r.localBounds;overlay.updateWhenOffscreen=true;overlay.sharedMaterial=blushMaterial;overlay.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;overlay.receiveShadows=false;overlays.Add(new Overlay{source=r,renderer=overlay});}
   Index=Mathf.Clamp(selected,0,Count-1);Manual=false;if(keep)Select(Index);
  }
  void LateUpdate(){Tick(Time.unscaledDeltaTime);}
  public void Tick(float dt){RefreshModel();if(!Manual||Count==0)return;elapsed+=Mathf.Max(0,dt);float t=Mathf.Clamp01(elapsed/Mathf.Max(.01f,TransitionSeconds));t=t*t*(3-2*t);foreach(var b in bindings)if(b.renderer){b.current=Mathf.Lerp(b.from,b.target,t);b.renderer.SetBlendShapeWeight(b.index,b.current);}blushValue=Mathf.Lerp(blushFrom,blushTarget,t);if(blushMaterial)blushMaterial.SetFloat("_Strength",blushValue);
   foreach(var o in overlays)if(o.source&&o.renderer){o.renderer.sharedMaterial=blushMaterial;for(int i=0;i<o.source.sharedMesh.blendShapeCount;i++)o.renderer.SetBlendShapeWeight(i,o.source.GetBlendShapeWeight(i));}
  }
  void OnDestroy(){if(hudFont)Destroy(hudFont);}
  void OnGUI(){if(!player||(switcher&&switcher.PickerOpen))return;if(!hudFont)hudFont=Font.CreateDynamicFontFromOSFont(new[]{"Apple SD Gothic Neo","Malgun Gothic","Arial"},16);var previousFont=GUI.skin.font;GUI.skin.font=hudFont;float width=Mathf.Min(470,Screen.width-24);var rect=new Rect(12,Screen.height-100,width,88);GUI.Box(rect,GUIContent.none);GUI.Label(new Rect(24,rect.y+9,width-24,24),"표정: "+(Manual?CurrentTitle:"자동 · 춤 표정"));GUI.Label(new Rect(24,rect.y+34,width-24,24),"← / → 표정 선택   Home 기본   F7 자동 복귀");GUI.Label(new Rect(24,rect.y+58,width-24,24),Count>0?"Game 화면을 클릭한 뒤 키를 누르세요":"현재 캐릭터는 얼굴 표정을 지원하지 않습니다");GUI.skin.font=previousFont;}
 }
}
