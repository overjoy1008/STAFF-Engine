using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Staff.Characters {
 [DisallowMultipleComponent,DefaultExecutionOrder(100)]
 public sealed class PmxPhysics : MonoBehaviour {
  [Serializable] public class Bone {public string name,unityName;public int parent;public float[] rest,unityBindRotation;}
  [Serializable] public class Body {public string name;public int bone,mode,shape,group,ignoreMask;public float mass,linearDamping,angularDamping,restitution,friction;public float[] size,pose,localPose;}
  [Serializable] public class Joint {public string name;public int a,b;public float[] pose,limits,springs;}
  [Serializable] public class Profile {public string slug,sha256;public float unityMetersPerMmdUnit,unityRootScale;public Bone[] bones;public Body[] bodies;public Joint[] joints;}
  public TextAsset source;
  public bool PhysicsEnabled=true;
  [Header("Secondary motion")]
  [Tooltip("Overall PMX motion amplitude for every dynamic bone. 0 = animation only; 1 = full PMX amplitude.")]
  [Range(0,1)] public float MotionStrength=.7f;
  [Tooltip("Secondary motion smoothing time in seconds. Larger values soften rapid flutter; 0 disables smoothing. Body animation is unaffected.")]
  [Range(0,.3f)] public float MotionSmoothTime=.05f;
  [Tooltip("Additional gradual return toward the initially gravity-settled pose after movement settles. 0 preserves unassisted PMX physics. Gradually corrects simulation state without a reset; this is an optional gameplay assist.")]
  [Range(0,1)] public float RecoveryStrength=.35f;
  [Tooltip("Recovery response time in seconds. Larger values return more gently. This is an assist, not an authored PMX parameter.")]
  [Range(.25f,5)] public float RecoveryTime=.5f;
  float quietTime;float[] recoveryPreviousTargets,recoveryTargets;Vector3[] recoveryOffsetP;Quaternion[] recoveryOffsetQ;bool recoveryTargetsReady;
  public float RecoveryWeight {get;private set;}
  readonly List<int> dynamicBoneIndices=new List<int>();
  Vector3[] presentedDeltaP;Quaternion[] presentedDeltaQ;bool presentationReady;
  public int BodyCount=>data?.bodies.Length??0;
  public int JointCount=>data?.joints.Length??0;
  public int DynamicBoneCount=>dynamicBones.Count;
  public float MaximumDisplacement {get;private set;}
  public float MaximumRotation {get;private set;}
  public int ResetCount {get;private set;}
  public int SimulationSteps {get;private set;}
  public bool IsReady=>world!=IntPtr.Zero;
  int[] simulationModes;
  public int NormalizedMode2Count {get;private set;}
  Profile data;IntPtr world;Transform[] bones;Quaternion[] correction;
  readonly List<Transform> dynamicBones=new List<Transform>();readonly List<int> bodyOrder=new List<int>();
  readonly Dictionary<Transform,Vector3> localPositions=new Dictionary<Transform,Vector3>();
  readonly Dictionary<Transform,Quaternion> localRotations=new Dictionary<Transform,Quaternion>();
  readonly float[] positionBuffer=new float[3];float[] current,output;float unit,accumulator;bool reset=true,wasEnabled=true;
  Vector3 lastRoot,animationRootP,previousAnimationRootP;
  Quaternion animationRootQ,previousAnimationRootQ;
  Vector3[] animationP,previousAnimationP,solvedP,previousSolvedP;
  Quaternion[] animationQ,previousAnimationQ,solvedQ,previousSolvedQ;
  const float Step=1f/120f;
  const string Lib="StaffPmxBulletUnityV3";
  [DllImport(Lib)] static extern IntPtr sp_create();
  [DllImport(Lib)] static extern void sp_configure_unity(IntPtr w);
  [DllImport(Lib)] static extern void sp_destroy(IntPtr w);
  [DllImport(Lib)] static extern int sp_add_body(IntPtr w,int mode,int shape,float[] size,float mass,float linear,float angular,float bounce,float friction,int group,int ignore,float[] pose);
  [DllImport(Lib)] static extern int sp_add_joint(IntPtr w,int a,int b,float[] pose,float[] limits,float[] springs);
  [DllImport(Lib)] static extern void sp_set_poses(IntPtr w,float[] poses,int reset);
  [DllImport(Lib)] static extern void sp_get_poses(IntPtr w,float[] poses);
  [DllImport(Lib)] static extern void sp_recover(IntPtr w,float[] targets,float strength,float seconds,float dt);
  [DllImport(Lib)] static extern void sp_step(IntPtr w,float dt);
  [DllImport(Lib)] static extern void sp_set_position(IntPtr w,int index,float[] position);
  static Vector3 V(float[] p,int i=0)=>new Vector3(p[i],p[i+1],p[i+2]);
  static Quaternion Q(float[] p,int i=0)=>new Quaternion(p[i],p[i+1],p[i+2],p[i+3]);
  // Current FBX coordinates reflect X relative to the right-handed Bullet world.
  static Vector3 Reflect(Vector3 p)=>new Vector3(-p.x,p.y,p.z);
  static Quaternion Reflect(Quaternion q)=>new Quaternion(q.x,-q.y,-q.z,q.w);
  static void Put(float[] a,int i,Vector3 p,Quaternion q){a[i]=p.x;a[i+1]=p.y;a[i+2]=p.z;a[i+3]=q.x;a[i+4]=q.y;a[i+5]=q.z;a[i+6]=q.w;}
  void Awake(){if(source)Initialize();}
  public void Initialize(){
   if(IsReady)return;
   try {
    data=JsonUtility.FromJson<Profile>(source.text);
    var map=new Dictionary<string,Transform>();foreach(var t in GetComponentsInChildren<Transform>(true))map[t.name]=t;
    unit=data.unityMetersPerMmdUnit*Mathf.Abs(transform.lossyScale.x/data.unityRootScale);
    bones=new Transform[data.bones.Length];correction=new Quaternion[bones.Length];
    for(int i=0;i<bones.Length;i++)if(map.TryGetValue(data.bones[i].unityName,out bones[i])) {
     correction[i]=Quaternion.Inverse(Reflect(Q(data.bones[i].unityBindRotation)))*Q(data.bones[i].rest,3);
    }
    simulationModes=new int[data.bodies.Length];
    for(int i=0;i<simulationModes.Length;i++)simulationModes[i]=data.bodies[i].mode;
    // MMDLoader compatibility rule: a constrained dynamic parent's direct bone
    // child follows its solved position. Repositioning both after solving would
    // override the connected joint solution. No model/body-name conditions.
    // https://github.com/mrdoob/three.js/blob/r169/examples/jsm/loaders/MMDLoader.js
    foreach(var joint in data.joints){
     var parent=data.bodies[joint.a];var child=data.bodies[joint.b];
     if(simulationModes[joint.a]!=0&&simulationModes[joint.b]==2&&parent.bone>=0&&child.bone>=0&&data.bones[child.bone].parent==parent.bone){
      simulationModes[joint.b]=1;NormalizedMode2Count++;
     }
    }
    world=sp_create();
    for(int i=0;i<data.bodies.Length;i++){
     var b=data.bodies[i];if(b.bone>=0&&!bones[b.bone])throw new Exception("Missing PMX bone: "+data.bones[b.bone].name);
     // PMX stores collision membership bits (1 = collide). Legacy JSON field is misnamed ignoreMask.
     if(sp_add_body(world,simulationModes[i],b.shape,b.size,b.mass,b.linearDamping,b.angularDamping,b.restitution,b.friction,b.group,(~b.ignoreMask)&0xffff,b.pose)!=i)throw new Exception("Unsupported PMX shape");
     if(b.mode!=0&&b.bone>=0){bodyOrder.Add(i);var t=bones[b.bone];if(!localPositions.ContainsKey(t)){dynamicBones.Add(t);dynamicBoneIndices.Add(b.bone);localPositions[t]=t.localPosition;localRotations[t]=t.localRotation;}}
    }
    foreach(var j in data.joints)if(sp_add_joint(world,j.a,j.b,j.pose,j.limits,j.springs)<0)throw new Exception("Invalid PMX joint: "+j.name);
    sp_configure_unity(world);
    int Depth(Transform t){int n=0;while(t.parent){n++;t=t.parent;}return n;}
    bodyOrder.Sort((a,b)=>{int d=Depth(bones[data.bodies[a].bone]).CompareTo(Depth(bones[data.bodies[b].bone]));return d!=0?d:a.CompareTo(b);});
    recoveryPreviousTargets=new float[BodyCount*7];recoveryTargets=new float[BodyCount*7];recoveryOffsetP=new Vector3[BodyCount];recoveryOffsetQ=new Quaternion[BodyCount];current=new float[BodyCount*7];output=new float[current.Length];lastRoot=transform.position;reset=true;
    animationP=new Vector3[bones.Length];previousAnimationP=new Vector3[bones.Length];animationQ=new Quaternion[bones.Length];previousAnimationQ=new Quaternion[bones.Length];
    presentedDeltaP=new Vector3[dynamicBones.Count];presentedDeltaQ=new Quaternion[dynamicBones.Count];
    solvedP=new Vector3[dynamicBones.Count];previousSolvedP=new Vector3[dynamicBones.Count];solvedQ=new Quaternion[dynamicBones.Count];previousSolvedQ=new Quaternion[dynamicBones.Count];
   }catch(Exception e){Dispose();enabled=false;Debug.LogError("PMX physics initialization failed: "+e,this);}
  }
  // Restore only dynamic auxiliary bones before Animator evaluates the body.
  public void RestoreAnimationPose(){foreach(var t in dynamicBones)if(t){t.localPosition=localPositions[t];t.localRotation=localRotations[t];}}
  void Update(){if(IsReady)RestoreAnimationPose();}
  // Animation loops and transitions must retain momentum and joint state.
  void LateUpdate(){if(IsReady)Simulate(Time.deltaTime);}
  public void RequestReset(){reset=true;}
  void Targets(){for(int i=0;i<BodyCount;i++){var b=data.bodies[i];if(b.bone<0){var p=transform.TransformPoint(Reflect(V(b.pose))*data.unityMetersPerMmdUnit/data.unityRootScale);Put(current,i*7,Reflect(p)/unit,Reflect(transform.rotation)*Q(b.pose,3));continue;}var t=bones[b.bone];var q=Reflect(t.rotation)*correction[b.bone];var p0=Reflect(t.position)/unit;Put(current,i*7,p0+q*V(b.localPose),q*Q(b.localPose,3));}}
  void CaptureAnimation(){animationRootP=transform.position;animationRootQ=transform.rotation;for(int i=0;i<bones.Length;i++)if(bones[i]){animationP[i]=bones[i].localPosition;animationQ[i]=bones[i].localRotation;}}
  void AnimationPose(float alpha){
   // Root translation/turning must be sampled on the same timeline as the bones.
   // Otherwise 60 Hz movement drives 120 Hz Bullet anchors at alternating 2v / 0v.
   transform.SetPositionAndRotation(Vector3.Lerp(previousAnimationRootP,animationRootP,alpha),Quaternion.Slerp(previousAnimationRootQ,animationRootQ,alpha));
   for(int i=0;i<bones.Length;i++)if(bones[i]){bones[i].localPosition=Vector3.Lerp(previousAnimationP[i],animationP[i],alpha);bones[i].localRotation=Quaternion.Slerp(previousAnimationQ[i],animationQ[i],alpha);}}
  void SaveAnimation(){previousAnimationRootP=animationRootP;previousAnimationRootQ=animationRootQ;Array.Copy(animationP,previousAnimationP,bones.Length);Array.Copy(animationQ,previousAnimationQ,bones.Length);}
  void SaveSolved(){for(int i=0;i<dynamicBones.Count;i++){solvedP[i]=dynamicBones[i].localPosition;solvedQ[i]=dynamicBones[i].localRotation;}}
  void PreviousSolved(){Array.Copy(solvedP,previousSolvedP,solvedP.Length);Array.Copy(solvedQ,previousSolvedQ,solvedQ.Length);}
  public void Simulate(float dt){
   if(!IsReady)return;
   if(!PhysicsEnabled){RestoreAnimationPose();wasEnabled=false;presentationReady=false;return;}
   if(!wasEnabled){reset=true;wasEnabled=true;}
   if(dt<=0)return;
   if(Vector3.Distance(transform.position,lastRoot)>3f)reset=true;
   lastRoot=transform.position;CaptureAnimation();
   if(reset){
    presentationReady=false;quietTime=0;recoveryTargetsReady=false;RecoveryWeight=0;
    SaveAnimation();Targets();sp_set_poses(world,current,1);
    // Settling follows exactly the same type-2 position correction as live steps.
    for(int n=0;n<120;n++){AnimationPose(1);Targets();sp_set_poses(world,current,0);sp_step(world,Step);ApplySolved();}
    // Preserve the natural gravity-settled offset instead of pulling to a
    // rigid bind pose. Targets remain relative to each animated body frame.
    sp_get_poses(world,output);
    for(int i=0;i<BodyCount;i++){int k=i*7;var inverse=Quaternion.Inverse(Q(current,k+3));recoveryOffsetP[i]=inverse*(V(output,k)-V(current,k));recoveryOffsetQ[i]=inverse*Q(output,k+3);}
    SaveSolved();PreviousSolved();accumulator=0;reset=false;ResetCount++;
   }
   // Bound catch-up after editor stalls; do not throw away the physical pose.
   dt=Mathf.Min(dt,.1f);float old=accumulator;accumulator+=dt;int steps=0;
   while(accumulator+1e-7f>=Step&&steps<12){
    PreviousSolved();AnimationPose(Mathf.Clamp01((Step-old+steps*Step)/dt));Targets();
    sp_set_poses(world,current,0);sp_step(world,Step);RecoverPose();ApplySolved();SaveSolved();
    SimulationSteps++;accumulator=Mathf.Max(0,accumulator-Step);steps++;
   }
   SaveAnimation();AnimationPose(1);
   // Render interpolation is never fed back to Bullet, including at >120 Hz.
   float blend=Mathf.Clamp01(accumulator/Step);
   for(int i=0;i<dynamicBones.Count;i++){
    var t=dynamicBones[i];int bone=dynamicBoneIndices[i];
    var rawP=Vector3.Lerp(previousSolvedP[i],solvedP[i],blend);var rawQ=Quaternion.Slerp(previousSolvedQ[i],solvedQ[i],blend);
    float strength=Mathf.Clamp01(MotionStrength);
    var deltaP=(rawP-animationP[bone])*strength;
    var deltaQ=Quaternion.Slerp(Quaternion.identity,Quaternion.Inverse(animationQ[bone])*rawQ,strength);
    float tau=Mathf.Max(0,MotionSmoothTime);
    float weight=!presentationReady||tau<=0?1:1-Mathf.Exp(-dt/tau);
    presentedDeltaP[i]=Vector3.Lerp(presentedDeltaP[i],deltaP,weight);
    presentedDeltaQ[i]=presentationReady?Quaternion.Slerp(presentedDeltaQ[i],deltaQ,weight):deltaQ;
    // Filter only the secondary offset: animation stays immediate and the solver
    // never receives the attenuated pose as collision/joint feedback.
    t.localPosition=animationP[bone]+presentedDeltaP[i];t.localRotation=animationQ[bone]*presentedDeltaQ[i];
   }
   presentationReady=true;
  }
  void RecoverPose(){
   float speed2=0,spin2=0;int anchors=0;
   if(recoveryTargetsReady)for(int i=0;i<BodyCount;i++)if(simulationModes[i]==0){
    int k=i*7;float speed=(V(current,k)-V(recoveryPreviousTargets,k)).magnitude*unit/Step;
    float spin=Quaternion.Angle(Q(current,k+3),Q(recoveryPreviousTargets,k+3))/Step;
    speed2+=speed*speed;spin2+=spin*spin;anchors++;
   }
   bool quiet=recoveryTargetsReady&&anchors>0&&speed2/anchors<.04f&&spin2/anchors<900f;
   quietTime=quiet?quietTime+Step:0;
   // Pause the assist during locomotion or active body animation. Fade in only
   // as soon as motion is quiet, leaving normal secondary motion to the PMX solver.
   float desired=Mathf.Clamp01(quietTime/Mathf.Max(.25f,RecoveryTime))*Mathf.Clamp01(RecoveryStrength);
   RecoveryWeight=Mathf.MoveTowards(RecoveryWeight,desired,Step*2);
   if(RecoveryWeight>0){
    for(int i=0;i<BodyCount;i++){int k=i*7;var q=Q(current,k+3);Put(recoveryTargets,k,V(current,k)+q*recoveryOffsetP[i],q*recoveryOffsetQ[i]);}
    sp_recover(world,recoveryTargets,RecoveryWeight,Mathf.Max(.25f,RecoveryTime),Step);
   }
   Array.Copy(current,recoveryPreviousTargets,current.Length);recoveryTargetsReady=true;
  }
  void ApplySolved(){
   sp_get_poses(world,output);
   foreach(int i in bodyOrder){var b=data.bodies[i];var t=bones[b.bone];int k=i*7;var bodyQ=Q(output,k+3);var boneQ=bodyQ*Quaternion.Inverse(Q(b.localPose,3));var boneP=V(output,k)-boneQ*V(b.localPose);var before=t.position;var rotation=t.rotation;
    var target=Reflect(boneQ*Quaternion.Inverse(correction[b.bone]));var position=Reflect(boneP)*unit;
    if(!float.IsFinite(position.x)||!float.IsFinite(position.y)||!float.IsFinite(position.z)){reset=true;return;}
    if(simulationModes[i]==1)t.SetPositionAndRotation(position,target);else t.rotation=target;
    MaximumDisplacement=Mathf.Max(MaximumDisplacement,Vector3.Distance(before,t.position));MaximumRotation=Mathf.Max(MaximumRotation,Quaternion.Angle(rotation,t.rotation));
    if(simulationModes[i]==2){var q=Reflect(t.rotation)*correction[b.bone];var p=Reflect(t.position)/unit+q*V(b.localPose);positionBuffer[0]=p.x;positionBuffer[1]=p.y;positionBuffer[2]=p.z;sp_set_position(world,i,positionBuffer);}
   }
  }
  void OnEnable(){reset=true;}
  void OnDisable(){RestoreAnimationPose();reset=true;}
  void OnDestroy(){Dispose();}
  void Dispose(){if(world!=IntPtr.Zero){sp_destroy(world);world=IntPtr.Zero;}}
 }
}
