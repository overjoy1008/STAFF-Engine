using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.InputSystem;
using Staff.Characters;
namespace Staff.AnimationLab {
public sealed class AnimationLabPlayer : MonoBehaviour {
 public AnimationLabCatalog catalog;
 public Camera viewCamera;
 public GameObject seat, wall, water;
 public int Selected { get; private set; }
 public int CharacterIndex { get; private set; }
 public Animator Animator { get; private set; }
 public double TimePosition { get; private set; }
 public bool Playing { get; private set; } = true;
 public bool Repeat = true;
 public float Speed = 1;
 GameObject model;
 CharacterToonToggle shading;
 PlayableGraph graph;
 AnimationClipPlayable motion;
 Vector2 scroll;
 string search="", category="All", subcategory="All";
 string[] categories;
 float yaw=180,pitch=8,distance=4.8f;
 float frontYaw=180;
 GUIStyle titleStyle, subtitleStyle, clipStyle;
 GUISkin labSkin;
 bool libraryOpen, orbitDragging, panDragging, subcategoryPickerOpen;
 Vector3 panOffset;
 bool Compact => Screen.width<1000;
 float SidebarWidth => Compact?0:Mathf.Clamp(Screen.width*.31f,310,420);
 const float ControlsHeight=214;
 Rect PreviewRect => new Rect(SidebarWidth+24,112,Mathf.Max(1,Screen.width-SidebarWidth-36),Mathf.Max(1,Screen.height-ControlsHeight-136));
 public AnimationLabCatalog.Entry Current => catalog.entries[Selected];
 void Start() {
  Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  categories=new[]{"All"}.Concat(catalog.entries.Select(e=>e.category).Distinct().OrderBy(x=>x)).ToArray();
  shading=new CharacterToonToggle();
  int index=Array.FindIndex(catalog.characters,c=>c && c.name=="Nicole");
  SelectCharacter(index>=0?index:0);
 }
 public void SelectCharacter(int index) {
  if(graph.IsValid())graph.Destroy();
  if(model){model.SetActive(false);Destroy(model);}
  CharacterIndex=(index+catalog.characters.Length)%catalog.characters.Length;
  model=Instantiate(catalog.characters[CharacterIndex],transform);
  model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
  Animator=model.GetComponentInChildren<Animator>();
  Animator.runtimeAnimatorController=null;Animator.applyRootMotion=false;Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  foreach(var physics in model.GetComponentsInChildren<PmxPhysics>())physics.PhysicsEnabled=false;
  shading?.Apply(model,CharacterShaderMode.CharacterToonSmooth);
  SelectClip(Selected);
 }
 public void SelectClip(int index) {
  Selected=(index+catalog.entries.Length)%catalog.entries.Length;
  if(graph.IsValid())graph.Destroy();
  graph=PlayableGraph.Create("Animation Lab Preview");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
  motion=AnimationClipPlayable.Create(graph,Current.clip);motion.SetApplyFootIK(false);motion.SetApplyPlayableIK(false);
  var output=AnimationPlayableOutput.Create(graph,"Character",Animator);output.SetSourcePlayable(motion);graph.Play();
  Playing=true;Seek(0);HideContextProps();
  // Measure the first evaluated pose, including rotations baked into each clip.
  var left=Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
  var right=Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
  var across=left&&right?right.position-left.position:Vector3.zero;
  across=Vector3.ProjectOnPlane(across,Vector3.up);
  if(across.sqrMagnitude<.0001f){
   left=Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);right=Animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
   across=left&&right?Vector3.ProjectOnPlane(right.position-left.position,Vector3.up):model.transform.right;
  }
  var forward=Vector3.Cross(across.normalized,Vector3.up);
  frontYaw=forward.sqrMagnitude>.0001f?Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg+180:180;
  ResetView();
 }
 public void Seek(double seconds) {
  TimePosition=Math.Max(0,Math.Min(Current.clip.length,seconds));
  motion.SetTime(TimePosition);graph.Evaluate(0);
  model.transform.localPosition=Current.Context=="Swim"?Vector3.up:Vector3.zero;model.transform.localRotation=Quaternion.identity;
 }
 public void SelectCategory(string value) {
  category=value;subcategory="All";subcategoryPickerOpen=false;scroll=Vector2.zero;
  var visible=VisibleIndices();if(visible.Length>0&&!visible.Contains(Selected))SelectClip(visible[0]);
 }
 public string[] Subcategories()=>category=="All"?Array.Empty<string>():new[]{"All"}.Concat(catalog.entries.Where(e=>e.category==category).SelectMany(e=>new[]{e.subcategory}.Concat(e.additionalSubcategories??Array.Empty<string>())).Where(s=>!string.IsNullOrEmpty(s)).Distinct().OrderBy(x=>x)).ToArray();
 public void SelectSubcategory(string value){subcategory=value;subcategoryPickerOpen=false;scroll=Vector2.zero;var visible=VisibleIndices();if(visible.Length>0&&!visible.Contains(Selected))SelectClip(visible[0]);}
 bool Matches(AnimationLabCatalog.Entry e)=>(category=="All"||e.category==category)&&(subcategory=="All"||e.subcategory==subcategory||(e.additionalSubcategories?.Contains(subcategory)??false))&&(string.IsNullOrWhiteSpace(search)||(e.title+" "+e.source+" "+e.subcategory+" "+string.Join(" ",e.additionalSubcategories??Array.Empty<string>())+" "+e.clip.name).Contains(search,StringComparison.OrdinalIgnoreCase));
 public int[] VisibleIndices()=>Enumerable.Range(0,catalog.entries.Length).Where(i=>Matches(catalog.entries[i])).ToArray();
 public void SelectRelative(int direction) {
  var visible=VisibleIndices();if(visible.Length==0)return;
  int index=Array.IndexOf(visible,Selected);SelectClip(visible[(index<0?(direction>0?0:visible.Length-1):(index+direction+visible.Length)%visible.Length)]);
 }
 public void ResetView(){orbitDragging=false;panDragging=false;panOffset=Vector3.zero;yaw=frontYaw;pitch=8;distance=4.8f;UpdateCamera();}
 // Keep scene references compatible, but never display preview helper geometry.
 void OnEnable()=>HideContextProps();
 void HideContextProps() {
  if(seat)seat.SetActive(false);
  if(wall)wall.SetActive(false);
  if(water)water.SetActive(false);
 }
 public void TogglePause()=>Playing=!Playing;
 void Update() {
  if(!graph.IsValid())return;
  if(Playing){var t=TimePosition+Time.deltaTime*Speed;if(t>=Current.clip.length){if(Repeat)t%=Current.clip.length;else {t=Current.clip.length;Playing=false;}}Seek(t);}
  var k=Keyboard.current;
  if(k!=null && GUI.GetNameOfFocusedControl()!="Search"){
   if(k.spaceKey.wasPressedThisFrame)TogglePause();
   if(k.rightArrowKey.wasPressedThisFrame)SelectRelative(1);
   if(k.leftArrowKey.wasPressedThisFrame)SelectRelative(-1);
   if(k.cKey.wasPressedThisFrame)SelectCharacter(CharacterIndex+1);
   if(k.rKey.wasPressedThisFrame)Seek(0);
  }
  var m=Mouse.current;
  if(m!=null){
   bool inPreview=!(Compact&&libraryOpen)&&PreviewRect.Contains(new Vector2(m.position.ReadValue().x,Screen.height-m.position.ReadValue().y));
   if(m.leftButton.wasPressedThisFrame)orbitDragging=inPreview;
   if(m.rightButton.wasPressedThisFrame)panDragging=inPreview;
   if(!m.leftButton.isPressed)orbitDragging=false;
   if(!m.rightButton.isPressed)panDragging=false;
   if(panDragging){
    var d=m.delta.ReadValue();
    // Translate in the camera plane at a constant screen-space drag speed.
    float unitsPerPixel=2*distance*Mathf.Tan(viewCamera.fieldOfView*.5f*Mathf.Deg2Rad)/PreviewRect.height;
    panOffset-=(viewCamera.transform.right*d.x+viewCamera.transform.up*d.y)*unitsPerPixel;
   }
   else if(orbitDragging){var d=m.delta.ReadValue();yaw+=d.x*.25f;pitch=Mathf.Clamp(pitch-d.y*.2f,-25,70);}
   if(inPreview)distance=Mathf.Clamp(distance-m.scroll.ReadValue().y*.004f,2,9);
  }
  UpdateCamera();
 }
 void UpdateCamera(){
  if(!viewCamera)return;
  var preview=PreviewRect;
  viewCamera.rect=new Rect(preview.x/Screen.width,(Screen.height-preview.yMax)/Screen.height,preview.width/Screen.width,preview.height/Screen.height);
  var focus=transform.position+panOffset+Vector3.up*(Current.Context=="Swim"?1.65f:.95f);
  viewCamera.transform.position=focus+Quaternion.Euler(pitch,yaw,0)*Vector3.back*distance;
  viewCamera.transform.LookAt(focus);
 }
 static void FillPanel(Rect rect){
  var old=GUI.color;GUI.color=new Color(.055f,.065f,.085f,1);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;
 }
 void OnGUI() {
  if(catalog==null||!graph.IsValid())return;
  if(!labSkin){
   labSkin=Instantiate(GUI.skin);
   foreach(var style in new[]{labSkin.label,labSkin.button,labSkin.textField,labSkin.toggle})style.fontSize=18;
   labSkin.button.padding=new RectOffset(12,12,10,10);labSkin.button.wordWrap=true;
   labSkin.textField.padding=new RectOffset(10,10,10,10);
   labSkin.toggle.padding=new RectOffset(24,8,10,10);
   labSkin.horizontalSlider.fixedHeight=28;labSkin.horizontalSliderThumb.fixedWidth=24;labSkin.horizontalSliderThumb.fixedHeight=28;
   labSkin.verticalScrollbar.fixedWidth=24;labSkin.verticalScrollbarThumb.fixedWidth=24;
   titleStyle=new GUIStyle(labSkin.label){fontSize=28,fontStyle=FontStyle.Bold,wordWrap=true};
   subtitleStyle=new GUIStyle(labSkin.label){fontSize=16,wordWrap=true};
   clipStyle=new GUIStyle(labSkin.button){alignment=TextAnchor.MiddleLeft,fontSize=18,padding=new RectOffset(14,12,12,12)};
  }
  var oldSkin=GUI.skin;GUI.skin=labSkin;
  float side=SidebarWidth;
  var preview=PreviewRect;
  FillPanel(new Rect(0,0,Screen.width,preview.y));
  FillPanel(new Rect(0,preview.yMax,Screen.width,Screen.height-preview.yMax));
  FillPanel(new Rect(0,preview.y,preview.x,preview.height));
  FillPanel(new Rect(preview.xMax,preview.y,Screen.width-preview.xMax,preview.height));
  if(Compact&&libraryOpen)FillPanel(new Rect(0,0,Screen.width,Screen.height));
  if(!Compact||libraryOpen){
  float panel=Compact?Screen.width-24:side;
  GUI.Box(new Rect(12,Compact?72:12,panel,Screen.height-(Compact?84:24)),GUIContent.none);
  GUILayout.BeginArea(new Rect(26,Compact?84:24,panel-28,Screen.height-(Compact?108:48)));
  GUILayout.Label("ANIMATION LAB",titleStyle);GUILayout.Label("STAFF / MOTION EXPLORER",subtitleStyle);GUILayout.Space(12);
  // Filters share the scroll area so small windows can still reach every motion.
  scroll=GUILayout.BeginScrollView(scroll,false,true,GUIStyle.none,GUI.skin.verticalScrollbar);
  GUILayout.Label("FIND A MOTION");GUI.SetNextControlName("Search");search=GUILayout.TextField(search,GUILayout.MinHeight(44));
  GUILayout.Space(10);var nextCategory=categories[GUILayout.SelectionGrid(Array.IndexOf(categories,category),categories,2,GUILayout.Height(Mathf.Ceil(categories.Length/2f)*60))];if(nextCategory!=category)SelectCategory(nextCategory);GUILayout.Space(12);
  var subs=Subcategories();if(subs.Length>1){if(GUILayout.Button("Subcategory: "+subcategory,GUILayout.Height(60)))subcategoryPickerOpen=!subcategoryPickerOpen;if(subcategoryPickerOpen){var nextSub=subs[GUILayout.SelectionGrid(Math.Max(0,Array.IndexOf(subs,subcategory)),subs,2,GUILayout.Height(Mathf.Ceil(subs.Length/2f)*60))];if(nextSub!=subcategory)SelectSubcategory(nextSub);}GUILayout.Space(12);}
  for(int i=0;i<catalog.entries.Length;i++){
   var e=catalog.entries[i];if(!Matches(e))continue;
   GUI.backgroundColor=i==Selected?new Color(.2f,.9f,.8f):Color.white;
   if(GUILayout.Button(e.title,clipStyle,GUILayout.MinHeight(48),GUILayout.Width(panel-76))){GUI.FocusControl(null);SelectClip(i);}
  }
  GUI.backgroundColor=Color.white;GUILayout.EndScrollView();
  GUILayout.Space(8);GUILayout.Label("Arrows: motion   C: character\nSpace: pause   R: restart\nLeft drag: orbit   Right drag: pan\nTwo-finger scroll / wheel: zoom",subtitleStyle);GUILayout.EndArea();
  }
  if(Compact&&GUI.Button(new Rect(16,16,132,44),libraryOpen?"Back":"Motions"))libraryOpen=!libraryOpen;
  float x=side+36,width=Screen.width-x-24;
  if(!(Compact&&libraryOpen)){
  GUILayout.BeginArea(new Rect(Compact?160:x,20,Compact?Screen.width-184:width,90));
  GUILayout.Label(Current.title,titleStyle);GUILayout.Label(Current.source+" / "+Current.category+(string.IsNullOrEmpty(Current.subcategory)?"":" / "+Current.subcategory),subtitleStyle);GUILayout.EndArea();
  GUI.Box(new Rect(side+24,Screen.height-ControlsHeight-12,Screen.width-side-36,ControlsHeight),GUIContent.none);
  GUILayout.BeginArea(new Rect(x,Screen.height-ControlsHeight,width,ControlsHeight-16));
  GUILayout.BeginHorizontal();if(GUILayout.Button("Previous"))SelectRelative(-1);
  if(GUILayout.Button(Playing?"Pause":"Play"))TogglePause();
  if(GUILayout.Button("Next"))SelectRelative(1);
  if(GUILayout.Button("Restart"))Seek(0);
  GUILayout.EndHorizontal();
  var t=GUILayout.HorizontalSlider((float)TimePosition,0,Current.clip.length);if(Math.Abs(t-TimePosition)>.001)Seek(t);
  GUILayout.BeginHorizontal();Repeat=GUILayout.Toggle(Repeat,"Repeat");
  Speed=new[]{.5f,1f,1.5f}[GUILayout.SelectionGrid(Speed<1?0:Speed>1?2:1,new[]{"Slow","Normal","Fast"},3)];
  if(GUILayout.Button("Front view"))ResetView();GUILayout.EndHorizontal();
  GUILayout.BeginHorizontal();if(GUILayout.Button("Character: "+catalog.characters[CharacterIndex].name))SelectCharacter(CharacterIndex+1);
  GUILayout.EndHorizontal();GUILayout.EndArea();
  }
  GUI.skin=oldSkin;
 }
 void OnDestroy(){if(graph.IsValid())graph.Destroy();shading?.Dispose();if(labSkin)Destroy(labSkin);}
}
}
