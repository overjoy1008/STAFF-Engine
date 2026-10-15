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
 [Min(1)] public float range=400, speed=48;
 [Min(.1f)] public float wallHoldSeconds=3;
 [Min(0)] public float cancelJumpSpeed=10;
 public LayerMask surfaceMask=~0;
 public Phase State{get;private set;}
 public bool OwnsMovement=>State==Phase.Casting||State==Phase.Flying||State==Phase.Wall||State==Phase.Landing;
 public bool HasTarget{get;private set;}
 public bool IsScreenAiming{get;private set;}
 public Vector2 AimScreenPosition{get;private set;}
 [Range(.1f,3)] public float aimSensitivity=1;
 public bool BlocksCameraLook=>IsScreenAiming||(player&&player.InputCaptured&&!player.InputBlocked&&(State==Phase.Idle||State==Phase.Wall)&&Keyboard.current!=null&&Keyboard.current.qKey.isPressed);
 Camera aimCamera;Vector3 heldCameraPosition,heldPlayerPosition;Quaternion heldCameraRotation;float heldCameraFov;
 public bool BeginScreenAim(Camera camera){
  if(!camera||player.InputBlocked||player.IsSeated||(State!=Phase.Idle&&State!=Phase.Wall))return false;
  aimCamera=camera;heldCameraPosition=camera.transform.position;heldPlayerPosition=transform.position;heldCameraRotation=camera.transform.rotation;heldCameraFov=camera.fieldOfView;
  AimScreenPosition=camera.pixelRect.center;IsScreenAiming=true;aimingFromWall=State==Phase.Wall;if(!aimingFromWall)State=Phase.Aiming;
  player.GetComponent<AdventureDance>()?.StopDance();return true;
 }
 public void MoveScreenAim(Vector2 delta){
  if(!IsScreenAiming||!aimCamera)return;var rect=aimCamera.pixelRect;var p=AimScreenPosition+delta*aimSensitivity;
  AimScreenPosition=new Vector2(Mathf.Clamp(p.x,rect.xMin+12,rect.xMax-12),Mathf.Clamp(p.y,rect.yMin+12,rect.yMax-12));
  HoldAimCamera(aimCamera);Aim(aimCamera.ScreenPointToRay(AimScreenPosition));
 }
 public bool HoldAimCamera(Camera camera){if(!IsScreenAiming||camera!=aimCamera)return false;
  // Follow root translation while preserving the orientation captured on Q-down.
  Vector3 desired=heldCameraPosition+(transform.position-heldPlayerPosition);
  var origin=transform.position+Vector3.up*1.25f;var offset=desired-origin;
  if(offset.magnitude>.1f)foreach(var hit in Physics.SphereCastAll(origin,.1f,offset.normalized,offset.magnitude,~0,QueryTriggerInteraction.Ignore)){
   if(!hit.transform.IsChildOf(transform))desired=origin+offset.normalized*Mathf.Min(Vector3.Distance(origin,desired),Mathf.Max(0,hit.distance-.02f));
  }
  camera.transform.SetPositionAndRotation(desired,heldCameraRotation);camera.fieldOfView=heldCameraFov;return true;}
 public bool EndScreenAim(){if(!IsScreenAiming)return false;MoveScreenAim(Vector2.zero);ReleaseAimCamera();IsScreenAiming=false;bool launched=Launch();if(!launched&&!aimingFromWall)Cancel();aimingFromWall=false;return launched;}

 public Vector3 TargetPoint=>point;
 public int CompletedHops{get;private set;}
 public float CameraBoost=>State==Phase.Flying?10:0;
 AdventurePlayer player;CharacterController body;AdventureCamera view;
 Vector3 point,normal,destination,origin,departure,wallNormal,anchorPoint,anchorNormal,lastAnchorPoint;Collider anchor,lockedAnchor;
 float clock,flightAge;bool aimingFromWall;
 TangentEquationUI equationUI;
 LineRenderer tether,ring;Transform effects,staff;Material ink,tangentInk;
 PlayableGraph graph;AnimationMixerPlayable mixer;AnimationClipPlayable pose,previousPose;float blendAge;Animator poseAnimator;RuntimeAnimatorController savedController;AnimationClip currentClip;
 void Awake(){player=GetComponent<AdventurePlayer>();body=GetComponent<CharacterController>();}
 void Start(){CreateEffects();EnsureEquationUI();}
 void EnsureEquationUI(){if(!equationUI){equationUI=GetComponent<TangentEquationUI>();if(!equationUI)equationUI=gameObject.AddComponent<TangentEquationUI>();equationUI.Initialize(this);}}
 void Update(){
  if(!view){var rig=GetComponentInParent<Staff.Shared.SharedGameplay>();view=rig?rig.cameraController:Camera.main?Camera.main.GetComponent<AdventureCamera>():null;}
  bool accept=player.InputCaptured&&!player.InputBlocked&&!player.IsSeated&&Application.isFocused;
  if(!accept){if(State!=Phase.Idle)Cancel();return;}
  var k=Keyboard.current;if(k==null)return;
  if(k.qKey.wasPressedThisFrame)BeginScreenAim(view?view.GetComponent<Camera>():Camera.main);
  if(IsScreenAiming)MoveScreenAim(Mouse.current!=null?Mouse.current.delta.ReadValue():Vector2.zero);
  if(k.qKey.wasReleasedThisFrame)EndScreenAim();
  if(k.spaceKey.wasPressedThisFrame)CancelWithJump();
 }
 public bool Aim(Ray ray){
  HasTarget=false;float nearest=float.PositiveInfinity;RaycastHit selected=default;
  // The first solid surface occludes targets behind it, including ungrappleable props.
  foreach(var h in Physics.RaycastAll(ray,range+Vector3.Distance(ray.origin,transform.position),surfaceMask,QueryTriggerInteraction.Ignore)){
   if(h.transform.IsChildOf(transform)||h.distance>=nearest)continue;nearest=h.distance;selected=h;
  }
  if(!selected.collider)return false;
  var marker=selected.collider.GetComponentInParent<TangentSurface>();
  if(!marker||!marker.enabled||Vector3.Distance(transform.position,selected.point)<2||Vector3.Distance(transform.position,selected.point)>range)return false;
  point=selected.point;normal=selected.normal;anchor=selected.collider;HasTarget=true;return true;
 }
 void RememberAnchor(){lockedAnchor=anchor;anchorPoint=anchor.transform.InverseTransformPoint(point);anchorNormal=anchor.transform.InverseTransformDirection(normal);lastAnchorPoint=point;}
 void RefreshAnchor(){
  var contact=lockedAnchor.transform.TransformPoint(anchorPoint);var contactNormal=lockedAnchor.transform.TransformDirection(anchorNormal).normalized;
  if(State==Phase.Wall||State==Phase.Landing){var carry=contact-lastAnchorPoint;if(carry.sqrMagnitude>0)body.Move(carry);wallNormal=contactNormal;}
  if((State!=Phase.Wall&&State!=Phase.Landing)||!IsScreenAiming){point=contact;normal=contactNormal;}
  lastAnchorPoint=contact;
  float support=body.radius+Mathf.Max(0,body.height*.5f-body.radius)*Mathf.Abs(contactNormal.y)+body.skinWidth+.08f;destination=contact+contactNormal*support-transform.TransformVector(body.center);
 }

 public bool Launch(){
  if(!HasTarget||!anchor||player.InputBlocked||player.IsSeated||State==Phase.Flying||State==Phase.Casting)return false;
  if(!effects)CreateEffects();
  RememberAnchor();
  // Offset the capsule center from the surface, including ceilings and floors.
  float support=body.radius+Mathf.Max(0,body.height*.5f-body.radius)*Mathf.Abs(normal.y)+body.skinWidth+.08f;
  destination=point+normal*support-transform.TransformVector(body.center);
  departure=transform.position;origin=StaffTip;clock=flightAge=0;State=Phase.Casting;
  player.GetComponent<AdventureDance>()?.StopDance();player.ClearInteractionMotion();Play(castClip,.1f);return true;
 }
 public bool TickMovement(float dt){
  if(!OwnsMovement)return false;
  if(!lockedAnchor||!lockedAnchor.enabled||!lockedAnchor.gameObject.activeInHierarchy||player.InputBlocked||player.IsSeated){Cancel();return false;}
  RefreshAnchor();
  dt=Mathf.Min(dt,.05f);clock+=dt;
  if(State==Phase.Casting){if(clock>=.08f){clock=0;State=Phase.Flying;if(equationUI)equationUI.Lock(StaffTip,point);Play(launchClip,.65f);}return true;}
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
  else if(allowed<step-.001f&&obstruction.collider&&obstruction.collider.GetComponentInParent<TangentSurface>()==lockedAnchor.GetComponentInParent<TangentSurface>()&&distance<1.8f){point=obstruction.point;normal=obstruction.normal;anchor=obstruction.collider;RememberAnchor();Arrive();}
  else if(allowed<step-.001f||(transform.position-before).sqrMagnitude<.000001f||(flags&CollisionFlags.Above)!=0){Cancel();}
  else if(flightAge>range/Mathf.Max(1,speed)+2)Cancel();
  return true;
 }
 void Arrive(){lastAnchorPoint=point;CompletedHops++;clock=0;wallNormal=normal;State=Mathf.Abs(normal.y)<.65f?Phase.Wall:Phase.Landing;HasTarget=false;Play(State==Phase.Wall?wallClip:landClip,State==Phase.Wall?.85f:.65f);if(player.Visual){var flat=Vector3.ProjectOnPlane(-normal,Vector3.up);if(flat.sqrMagnitude>.001f)player.Visual.rotation=Quaternion.LookRotation(flat);}}
 public bool CancelWithJump(){if(!OwnsMovement)return false;bool wall=State==Phase.Wall;var n=wallNormal;Cancel();var push=wall?Vector3.ProjectOnPlane(n,Vector3.up).normalized*6:Vector3.zero;player.SetTraversalVelocity(push+Vector3.up*cancelJumpSpeed);return true;}
 void ReleaseAimCamera(){if(IsScreenAiming&&aimCamera){var controller=aimCamera.GetComponent<AdventureCamera>();if(controller)controller.ResumeAfterTangentAim();}}
 public void Cancel(){ReleaseAimCamera();IsScreenAiming=false;aimCamera=null;State=Phase.Idle;HasTarget=false;aimingFromWall=false;anchor=null;lockedAnchor=null;StopPose();if(player){player.ClearInteractionMotion();if(player.Visual)player.Visual.rotation=Quaternion.Euler(0,player.Visual.eulerAngles.y,0);}if(effects)effects.gameObject.SetActive(false);if(staff)staff.gameObject.SetActive(false);if(equationUI)equationUI.Present(false,null,Vector3.zero,Vector3.zero);}
 void OnDisable()=>Cancel();
 void OnDestroy(){StopPose();if(effects)Destroy(effects.gameObject);if(staff)Destroy(staff.gameObject);if(ink)Destroy(ink);if(tangentInk)Destroy(tangentInk);}
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
 void LateUpdate(){EnsureEquationUI();
  if(graph.IsValid()){if(player.Animator!=poseAnimator){Cancel();return;}if(State==Phase.Casting||State==Phase.Landing)pose.SetTime(System.Math.Min(currentClip.length-.01,pose.GetTime()+Time.deltaTime));if(State==Phase.Flying&&currentClip==flightClip)pose.SetTime(currentClip.length*Mathf.Lerp(.65f,.85f,Mathf.Clamp01(flightAge)));blendAge+=Time.deltaTime;float blend=previousPose.IsValid()?Mathf.SmoothStep(0,1,Mathf.Clamp01(blendAge/.08f)):1;mixer.SetInputWeight(0,1-blend);mixer.SetInputWeight(1,blend);graph.Evaluate(0);
   // Mixamo clips contain baked hip travel. Keep the sampled pose inside the controller.
   var hips=poseAnimator.GetBoneTransform(HumanBodyBones.Hips);if(hips)hips.position=transform.TransformPoint(body.center);
  }
  if(!effects)CreateEffects();bool active=State!=Phase.Idle;effects.gameObject.SetActive(active);staff.gameObject.SetActive(active);if(!active){if(equationUI)equationUI.Present(false,null,Vector3.zero,Vector3.zero);return;}
  var hand=player.Animator&&player.Animator.isHuman?player.Animator.GetBoneTransform(HumanBodyBones.RightHand):null;
  staff.position=hand?hand.position:transform.position+Vector3.up*1.1f;
  if(hand&&(State==Phase.Aiming||State==Phase.Casting)){var arm=player.Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var fore=player.Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);if(arm&&fore&&HasTarget){arm.rotation=Quaternion.FromToRotation(hand.position-arm.position,point-arm.position)*arm.rotation;fore.rotation=Quaternion.FromToRotation(hand.position-fore.position,point-fore.position)*fore.rotation;staff.position=hand.position;}}
  Vector3 dir=HasTarget||OwnsMovement?(point-staff.position).normalized:transform.forward;
  if(State==Phase.Flying&&hand)dir=(player.Visual.forward+Vector3.up*.2f).normalized;
  staff.rotation=Quaternion.FromToRotation(Vector3.up,dir);
  bool show=HasTarget||State==Phase.Casting||State==Phase.Flying;
  tether.enabled=ring.enabled=show;var displayCamera=view?view.GetComponent<Camera>():Camera.main;if(equationUI)equationUI.Present(show,displayCamera,StaffTip,point);
  if(!show)return;
  Vector3 source=State==Phase.Casting?origin:StaffTip;
  // Exactly two vertices: the tangent never bends, sags, or follows a spline.
  tether.SetPosition(0,source);tether.SetPosition(1,State==Phase.Casting?Vector3.Lerp(origin,point,Mathf.Clamp01(clock/.08f)):point);
  float unitsPerPixel=displayCamera?2*Mathf.Max(.1f,Vector3.Dot(point-displayCamera.transform.position,displayCamera.transform.forward))*Mathf.Tan(displayCamera.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Max(1,displayCamera.pixelHeight):0;float radius=Mathf.Max(.2f,unitsPerPixel*14);ring.startWidth=ring.endWidth=Mathf.Max(.025f,unitsPerPixel*2);var ringNormal=normal;
  for(int i=0;i<33;i++){float a=i/32f*Mathf.PI*2;ring.SetPosition(i,point+normal*.04f+(Vector3.Cross(ringNormal,Mathf.Abs(ringNormal.y)>.9f?Vector3.right:Vector3.up).normalized*Mathf.Cos(a)+Vector3.Cross(ringNormal,Vector3.Cross(ringNormal,Mathf.Abs(ringNormal.y)>.9f?Vector3.right:Vector3.up).normalized)*Mathf.Sin(a))*radius);}
 }
 Vector3 StaffTip=>staff?staff.position+staff.up*.9f:transform.position+Vector3.up*1.3f;
 LineRenderer Line(string name,int count,float width){var go=new GameObject(name);go.transform.SetParent(effects);var l=go.AddComponent<LineRenderer>();l.sharedMaterial=ink;l.positionCount=count;l.startWidth=l.endWidth=width;l.useWorldSpace=true;l.numCapVertices=4;return l;}
 void CreateEffects(){
  effects=new GameObject("Mathematical Symbols - Tangent Traversal").transform;effects.SetParent(transform.parent,false);
  ink=new Material(Shader.Find("Universal Render Pipeline/Unlit"));ink.SetColor("_BaseColor",Color.white);
  tether=Line("Tangent",2,.025f);tangentInk=new Material(ink);tangentInk.SetColor("_BaseColor",Color.white);tether.sharedMaterial=tangentInk;ring=Line("Contact",33,.025f);
  staff=GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;staff.name="Tangent casting staff";Destroy(staff.GetComponent<Collider>());staff.localScale=new Vector3(.035f,.9f,.035f);staff.GetComponent<Renderer>().sharedMaterial=ink;staff.SetParent(transform.parent,true);
  effects.gameObject.SetActive(false);staff.gameObject.SetActive(false);
 }
 void OnGUI(){if(IsScreenAiming){var color=GUI.color;GUI.color=HasTarget?Color.white:new Color(1,.45f,.3f);GUI.DrawTexture(new Rect(AimScreenPosition.x-9,Screen.height-AimScreenPosition.y-1,18,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(AimScreenPosition.x-1,Screen.height-AimScreenPosition.y-9,2,18),Texture2D.whiteTexture);GUI.color=color;}if(player&&player.InputCaptured)GUI.Label(new Rect(22,Screen.height-42,650,30),State==Phase.Wall?"Q hold / release: chain jump     Space: push off wall":"Q hold: lock rotation / mouse aims    Release Q: launch");}
}
}
