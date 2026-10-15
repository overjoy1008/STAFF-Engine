using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Staff.Characters;
namespace Staff.Traversal {
[DisallowMultipleComponent,RequireComponent(typeof(AdventurePlayer)),DefaultExecutionOrder(100)]
public sealed class TangentTraversal:MonoBehaviour {
 public enum Phase { Idle, Aiming, Casting, Flying, Wall, Landing }
 public AnimationClip castClip,launchClip,flightClip,wallClip,landClip;
 public Texture2D equationTexture;
 [Min(1)] public float range=75, speed=48;
 [Min(.1f)] public float wallHoldSeconds=3;
 public LayerMask surfaceMask=~0;
 public Phase State{get;private set;}
 public bool OwnsMovement=>State==Phase.Casting||State==Phase.Flying||State==Phase.Wall||State==Phase.Landing;
 public bool HasTarget{get;private set;}
 public bool IsScreenAiming{get;private set;}
 public Vector2 AimScreenPosition{get;private set;}
 [Range(.1f,3)] public float aimSensitivity=1;
 public bool BlocksCameraLook=>IsScreenAiming||(player&&player.InputCaptured&&!player.InputBlocked&&(State==Phase.Idle||State==Phase.Wall)&&Keyboard.current!=null&&Keyboard.current.qKey.isPressed);
 Camera aimCamera;Vector3 heldCameraPosition;Quaternion heldCameraRotation;float heldCameraFov;
 public bool BeginScreenAim(Camera camera){
  if(!camera||player.InputBlocked||player.IsSeated||(State!=Phase.Idle&&State!=Phase.Wall))return false;
  aimCamera=camera;heldCameraPosition=camera.transform.position;heldCameraRotation=camera.transform.rotation;heldCameraFov=camera.fieldOfView;
  AimScreenPosition=camera.pixelRect.center;IsScreenAiming=true;aimingFromWall=State==Phase.Wall;if(!aimingFromWall)State=Phase.Aiming;
  player.GetComponent<AdventureDance>()?.StopDance();return true;
 }
 public void MoveScreenAim(Vector2 delta){
  if(!IsScreenAiming||!aimCamera)return;var rect=aimCamera.pixelRect;var p=AimScreenPosition+delta*aimSensitivity;
  AimScreenPosition=new Vector2(Mathf.Clamp(p.x,rect.xMin+12,rect.xMax-12),Mathf.Clamp(p.y,rect.yMin+12,rect.yMax-12));
  HoldAimCamera(aimCamera);Aim(aimCamera.ScreenPointToRay(AimScreenPosition));
 }
 public bool HoldAimCamera(Camera camera){if(!IsScreenAiming||camera!=aimCamera)return false;camera.transform.SetPositionAndRotation(heldCameraPosition,heldCameraRotation);camera.fieldOfView=heldCameraFov;return true;}
 public bool EndScreenAim(){if(!IsScreenAiming)return false;MoveScreenAim(Vector2.zero);IsScreenAiming=false;bool launched=Launch();if(!launched&&!aimingFromWall)Cancel();aimingFromWall=false;return launched;}

 public Vector3 TargetPoint=>point;
 public int CompletedHops{get;private set;}
 public float CameraBoost=>State==Phase.Flying?10:0;
 AdventurePlayer player;CharacterController body;AdventureCamera view;
 Vector3 point,normal,destination,origin,departure,wallNormal;Collider anchor;
 float clock,flightAge;bool aimingFromWall;
 LineRenderer tether,curve,ring;Transform effects,staff,formula;Material ink,glyph,tangentInk;
 PlayableGraph graph;AnimationMixerPlayable mixer;AnimationClipPlayable pose,previousPose;float blendAge;Animator poseAnimator;RuntimeAnimatorController savedController;AnimationClip currentClip;
 void Awake(){player=GetComponent<AdventurePlayer>();body=GetComponent<CharacterController>();}
 void Start(){CreateEffects();}
 void Update(){
  if(!view&&Camera.main)view=Camera.main.GetComponent<AdventureCamera>();
  bool accept=player.InputCaptured&&!player.InputBlocked&&!player.IsSeated&&Application.isFocused;
  if(!accept){if(State!=Phase.Idle)Cancel();return;}
  var k=Keyboard.current;if(k==null)return;
  if(k.qKey.wasPressedThisFrame)BeginScreenAim(view?view.GetComponent<Camera>():Camera.main);
  if(IsScreenAiming)MoveScreenAim(Mouse.current!=null?Mouse.current.delta.ReadValue():Vector2.zero);
  if(k.qKey.wasReleasedThisFrame)EndScreenAim();
  if(k.spaceKey.wasPressedThisFrame&&State==Phase.Wall){var n=wallNormal;Cancel();player.SetTraversalVelocity(n*6+Vector3.up*7);}
 }
 public bool Aim(Ray ray){
  HasTarget=false;float nearest=float.PositiveInfinity;RaycastHit selected=default;
  // The first solid surface occludes targets behind it, including ungrappleable props.
  foreach(var h in Physics.RaycastAll(ray,range,surfaceMask,QueryTriggerInteraction.Ignore)){
   if(h.transform.IsChildOf(transform)||h.distance>=nearest)continue;nearest=h.distance;selected=h;
  }
  if(!selected.collider)return false;
  var marker=selected.collider.GetComponentInParent<TangentSurface>();
  if(!marker||!marker.enabled||Vector3.Distance(transform.position,selected.point)<2||Vector3.Distance(transform.position,selected.point)>range)return false;
  point=selected.point;normal=selected.normal;anchor=selected.collider;HasTarget=true;return true;
 }
 public bool Launch(){
  if(!HasTarget||!anchor||player.InputBlocked||player.IsSeated||State==Phase.Flying||State==Phase.Casting)return false;
  if(!effects)CreateEffects();
  // Offset the capsule center from the surface, including ceilings and floors.
  float support=body.radius+Mathf.Max(0,body.height*.5f-body.radius)*Mathf.Abs(normal.y)+body.skinWidth+.08f;
  destination=point+normal*support-transform.TransformVector(body.center);
  departure=transform.position;origin=StaffTip;clock=flightAge=0;State=Phase.Casting;
  player.GetComponent<AdventureDance>()?.StopDance();player.ClearInteractionMotion();Play(castClip,.1f);return true;
 }
 public bool TickMovement(float dt){
  if(!OwnsMovement)return false;
  if(!anchor||!anchor.enabled||player.InputBlocked||player.IsSeated){Cancel();return false;}
  dt=Mathf.Min(dt,.05f);clock+=dt;
  if(State==Phase.Casting){if(clock>=.08f){clock=0;State=Phase.Flying;Play(launchClip,.65f);}return true;}
  if(State==Phase.Wall){if(clock>=wallHoldSeconds){Cancel();return false;}return true;}
  if(State==Phase.Landing){if(clock>=.2f){Cancel();return false;}return true;}
  flightAge+=dt;
  if(flightAge>.12f&&currentClip!=flightClip)Play(flightClip,.65f);
  var delta=destination-transform.position;float distance=delta.magnitude;
  if(distance<.08f){Arrive();return true;}
  Vector3 direction=delta/distance;
  float step=Mathf.Min(distance,speed*Mathf.SmoothStep(.25f,1,Mathf.Clamp01(flightAge/.12f))*dt);
  Vector3 center=transform.TransformPoint(body.center);float half=Mathf.Max(0,body.height*.5f-body.radius);float allowed=step;RaycastHit obstruction=default;
  foreach(var h in Physics.CapsuleCastAll(center+Vector3.up*half,center-Vector3.up*half,body.radius,direction,step+body.skinWidth,surfaceMask,QueryTriggerInteraction.Ignore)){
   if(h.transform.IsChildOf(transform))continue;float clear=Mathf.Max(0,h.distance-body.skinWidth);if(clear<allowed){allowed=clear;obstruction=h;}
  }
  var before=transform.position;var flags=body.Move(direction*allowed);
  if(player.Visual){var flat=Vector3.ProjectOnPlane(direction,Vector3.up);if(flat.sqrMagnitude>.001f)player.Visual.rotation=Quaternion.LookRotation(flat)*Quaternion.Euler(Mathf.Clamp(-Mathf.Asin(direction.y)*Mathf.Rad2Deg,-65,65),0,0);}
  if(Vector3.Distance(transform.position,destination)<.12f)Arrive();
  else if(allowed<step-.001f&&obstruction.collider&&obstruction.collider.GetComponentInParent<TangentSurface>()==anchor.GetComponentInParent<TangentSurface>()&&distance<1.8f){point=obstruction.point;normal=obstruction.normal;Arrive();}
  else if(allowed<step-.001f||(transform.position-before).sqrMagnitude<.000001f||(flags&CollisionFlags.Above)!=0){Cancel();}
  else if(flightAge>range/Mathf.Max(1,speed)+2)Cancel();
  return true;
 }
 void Arrive(){CompletedHops++;clock=0;wallNormal=normal;State=Mathf.Abs(normal.y)<.65f?Phase.Wall:Phase.Landing;HasTarget=false;Play(State==Phase.Wall?wallClip:landClip,State==Phase.Wall?.85f:.65f);if(player.Visual){var flat=Vector3.ProjectOnPlane(-normal,Vector3.up);if(flat.sqrMagnitude>.001f)player.Visual.rotation=Quaternion.LookRotation(flat);}}
 public void Cancel(){IsScreenAiming=false;aimCamera=null;State=Phase.Idle;HasTarget=false;aimingFromWall=false;anchor=null;StopPose();if(player){player.ClearInteractionMotion();if(player.Visual)player.Visual.rotation=Quaternion.Euler(0,player.Visual.eulerAngles.y,0);}if(effects)effects.gameObject.SetActive(false);if(staff)staff.gameObject.SetActive(false);}
 void OnDisable()=>Cancel();
 void OnDestroy(){StopPose();if(effects)Destroy(effects.gameObject);if(staff)Destroy(staff.gameObject);if(ink)Destroy(ink);if(tangentInk)Destroy(tangentInk);if(glyph)Destroy(glyph);}
 void Play(AnimationClip clip,float phase){
  if(!clip||!player.Animator||!player.Animator.isHuman)return;
  if(!graph.IsValid()){
   poseAnimator=player.Animator;savedController=poseAnimator.runtimeAnimatorController;poseAnimator.runtimeAnimatorController=null;poseAnimator.applyRootMotion=false;
   graph=PlayableGraph.Create("STAFF Tangent Pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);mixer=AnimationMixerPlayable.Create(graph,2);
   var output=AnimationPlayableOutput.Create(graph,"Tangent",poseAnimator);output.SetSourcePlayable(mixer);graph.Play();
  }
  if(previousPose.IsValid()){mixer.DisconnectInput(0);graph.DestroyPlayable(previousPose);}
  if(pose.IsValid()){mixer.DisconnectInput(1);previousPose=pose;graph.Connect(previousPose,0,mixer,0);}
  pose=AnimationClipPlayable.Create(graph,clip);pose.SetApplyFootIK(false);pose.SetTime(clip.length*phase);graph.Connect(pose,0,mixer,1);blendAge=0;mixer.SetInputWeight(0,previousPose.IsValid()?1:0);mixer.SetInputWeight(1,previousPose.IsValid()?0:1);currentClip=clip;
 }
 void StopPose(){if(graph.IsValid())graph.Destroy();pose=default;previousPose=default;if(poseAnimator){poseAnimator.runtimeAnimatorController=savedController;poseAnimator.Rebind();if(poseAnimator.HasState(0,Animator.StringToHash("Base Layer.Locomotion")))poseAnimator.Play("Base Layer.Locomotion");}poseAnimator=null;currentClip=null;}
 void LateUpdate(){
  if(graph.IsValid()){if(player.Animator!=poseAnimator){Cancel();return;}if(State==Phase.Casting||State==Phase.Landing)pose.SetTime(System.Math.Min(currentClip.length-.01,pose.GetTime()+Time.deltaTime));if(State==Phase.Flying&&currentClip==flightClip)pose.SetTime(currentClip.length*Mathf.Lerp(.65f,.85f,Mathf.Clamp01(flightAge)));blendAge+=Time.deltaTime;float blend=previousPose.IsValid()?Mathf.SmoothStep(0,1,Mathf.Clamp01(blendAge/.08f)):1;mixer.SetInputWeight(0,1-blend);mixer.SetInputWeight(1,blend);graph.Evaluate(0);
   // Mixamo clips contain baked hip travel. Keep the sampled pose inside the controller.
   var hips=poseAnimator.GetBoneTransform(HumanBodyBones.Hips);if(hips)hips.position=transform.TransformPoint(body.center);
  }
  if(!effects)CreateEffects();bool active=State!=Phase.Idle;effects.gameObject.SetActive(active);staff.gameObject.SetActive(active);if(!active)return;
  var hand=player.Animator&&player.Animator.isHuman?player.Animator.GetBoneTransform(HumanBodyBones.RightHand):null;
  staff.position=hand?hand.position:transform.position+Vector3.up*1.1f;
  if(hand&&(State==Phase.Aiming||State==Phase.Casting)){var arm=player.Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var fore=player.Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);if(arm&&fore&&HasTarget){arm.rotation=Quaternion.FromToRotation(hand.position-arm.position,point-arm.position)*arm.rotation;fore.rotation=Quaternion.FromToRotation(hand.position-fore.position,point-fore.position)*fore.rotation;staff.position=hand.position;}}
  Vector3 dir=HasTarget||OwnsMovement?(point-staff.position).normalized:transform.forward;
  if(State==Phase.Flying&&hand)dir=(player.Visual.forward+Vector3.up*.2f).normalized;
  staff.rotation=Quaternion.FromToRotation(Vector3.up,dir);
  bool show=HasTarget||State==Phase.Casting||State==Phase.Flying;
  tether.enabled=curve.enabled=ring.enabled=show;formula.gameObject.SetActive(show&&equationTexture);
  if(!show)return;
  Vector3 source=State==Phase.Casting?origin:StaffTip;
  // Exactly two vertices: the tangent never bends, sags, or follows a spline.
  tether.SetPosition(0,source);tether.SetPosition(1,State==Phase.Casting?Vector3.Lerp(origin,point,Mathf.Clamp01(clock/.08f)):point);
  var d=(point-source).normalized;
  var n=Vector3.Cross(d,view?view.transform.right:Vector3.right).normalized;if(n.sqrMagnitude<.01f)n=Vector3.up;
  for(int i=0;i<33;i++){float u=(i/32f-.5f)*3;curve.SetPosition(i,point+d*u+n*u*u*.4f);float a=i/32f*Mathf.PI*2;ring.SetPosition(i,point+normal*.04f+(Vector3.Cross(normal,Mathf.Abs(normal.y)>.9f?Vector3.right:Vector3.up).normalized*Mathf.Cos(a)+Vector3.Cross(normal,Vector3.Cross(normal,Mathf.Abs(normal.y)>.9f?Vector3.right:Vector3.up).normalized)*Mathf.Sin(a))*.2f);}
  formula.position=point+Vector3.up*.7f+normal*.65f;if(view)formula.rotation=view.transform.rotation;
 }
 Vector3 StaffTip=>staff?staff.position+staff.up*.9f:transform.position+Vector3.up*1.3f;
 LineRenderer Line(string name,int count,float width){var go=new GameObject(name);go.transform.SetParent(effects);var l=go.AddComponent<LineRenderer>();l.sharedMaterial=ink;l.positionCount=count;l.startWidth=l.endWidth=width;l.useWorldSpace=true;l.numCapVertices=4;return l;}
 void CreateEffects(){
  effects=new GameObject("Mathematical Symbols - Tangent Traversal").transform;effects.SetParent(transform.parent,false);
  ink=new Material(Shader.Find("Universal Render Pipeline/Unlit"));ink.SetColor("_BaseColor",Color.white);
  tether=Line("Tangent",2,.025f);tangentInk=new Material(ink);tangentInk.SetColor("_BaseColor",Color.white);tether.sharedMaterial=tangentInk;curve=Line("Function curve",33,.018f);ring=Line("Contact",33,.025f);
  staff=GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;staff.name="Tangent casting staff";Destroy(staff.GetComponent<Collider>());staff.localScale=new Vector3(.035f,.9f,.035f);staff.GetComponent<Renderer>().sharedMaterial=ink;staff.SetParent(transform.parent,true);
  formula=GameObject.CreatePrimitive(PrimitiveType.Quad).transform;formula.name="LaTeX tangent equation";Destroy(formula.GetComponent<Collider>());formula.SetParent(effects,false);formula.localScale=new Vector3(3,.5f,1);
  glyph=new Material(Shader.Find("Universal Render Pipeline/Unlit"));glyph.SetFloat("_Surface",1);glyph.SetFloat("_Blend",0);glyph.SetFloat("_SrcBlend",5);glyph.SetFloat("_DstBlend",10);glyph.SetFloat("_ZWrite",0);glyph.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glyph.renderQueue=3000;glyph.SetTexture("_BaseMap",equationTexture);formula.GetComponent<Renderer>().sharedMaterial=glyph;
  formula.gameObject.AddComponent<Staff.MathSpace.LatexSymbol>().SetSource(@"y-f(a)=f'(a)(x-a)",equationTexture,"Assets/StaffTraversal/Mathematics/Tangent/symbol.tex");
  effects.gameObject.SetActive(false);staff.gameObject.SetActive(false);
 }
 void OnGUI(){if(IsScreenAiming){var color=GUI.color;GUI.color=HasTarget?Color.white:new Color(1,.45f,.3f);GUI.DrawTexture(new Rect(AimScreenPosition.x-9,Screen.height-AimScreenPosition.y-1,18,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(AimScreenPosition.x-1,Screen.height-AimScreenPosition.y-9,2,18),Texture2D.whiteTexture);GUI.color=color;}if(player&&player.InputCaptured)GUI.Label(new Rect(22,Screen.height-42,650,30),State==Phase.Wall?"Q hold / release: chain jump     Space: push off wall":"Q hold: lock camera / mouse aims    Release Q: launch");}
}
}
