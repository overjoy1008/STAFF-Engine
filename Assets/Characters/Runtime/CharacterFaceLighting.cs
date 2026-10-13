using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Staff.Characters {
 // Runtime shader uniforms only: no mesh, texture, or source material changes.
 [DefaultExecutionOrder(2500),DisallowMultipleComponent]
 public sealed class CharacterFaceLighting : MonoBehaviour {
  [Serializable] class Map { public Entry[] characters; }
  [Serializable] class Entry { public string character;public string[] materials; }
  struct Binding {public Renderer renderer;public int index;public bool face,skin;}
  static Map map;
  readonly List<Binding> bindings=new List<Binding>();
  MaterialPropertyBlock block;
  Vector4 leftEyePatch,rightEyePatch,mouthPatch;Vector3 leftEyeNormal,rightEyeNormal,mouthNormal;
  Transform head;Vector3 right,up,forward,center;Vector2 radii;CharacterShaderMode mode;bool herta;
  public void Configure(CharacterShaderMode value){mode=value;block??=new MaterialPropertyBlock();if(!head)Bind();Refresh();}
  void Bind(){
   var animator=GetComponent<Animator>();if(!animator||!animator.isHuman)return;
   var catalog=Resources.Load<ExpressionCatalog>("StaffExpressionCatalog");var entry=catalog?Array.Find(catalog.characters,c=>c.avatar==animator.avatar):null;if(entry==null)return;
   if(map==null){var text=Resources.Load<TextAsset>("ExpressionMaterials");if(text)map=JsonUtility.FromJson<Map>(text.text);}
   var face=map?.characters?.FirstOrDefault(c=>c.character==entry.id);if(face==null)return;
   head=animator.GetBoneTransform(HumanBodyBones.Head);if(!head)return;herta=entry.id=="TheHerta";
   right=head.InverseTransformDirection(transform.right);up=head.InverseTransformDirection(transform.up);forward=head.InverseTransformDirection(transform.forward);
   var points=new List<Vector3>();var pointNormals=new List<Vector3>();var normalSum=Vector3.zero;
   foreach(var r in GetComponentsInChildren<SkinnedMeshRenderer>(true)){
    if(r.name=="Expression cheek blush"||r.name=="First-person full-body shadow")continue;
    var mats=r.sharedMaterials;Mesh baked=null;
    for(int i=0;i<mats.Length;i++){
     if(!mats[i])continue;var name=mats[i].name.Split(' ')[0];bool isFace=face.materials.Contains(name);
     // Original PMX labels: skin, skin2, skin2+, hand. Never tint cloth/eyes/teeth.
     bool skin=herta&&(isFace||name=="Mat_013"||name=="Mat_014"||name=="Mat_015"||name=="Mat_016");
     if(!isFace&&!skin)continue;bindings.Add(new Binding{renderer=r,index=i,face=isFace,skin=skin});
     if(!isFace||i>=r.sharedMesh.subMeshCount)continue;
     // Compensate imported FBX scale before TransformPoint; default BakeMesh double-scales these avatars.
     if(!baked){baked=new Mesh();r.BakeMesh(baked,true);}var vertices=baked.vertices;var normals=baked.normals;
     foreach(var v in r.sharedMesh.GetTriangles(i).Distinct()){points.Add(r.transform.TransformPoint(vertices[v]));var n=r.transform.TransformDirection(normals[v]).normalized;pointNormals.Add(n);normalSum+=n;}
    }
    if(baked)Destroy(baked);
   }
   if(points.Count==0){head=null;bindings.Clear();return;}
   // Imported avatar roots may face -Z. Derive the anatomical front from the face surface.
   var worldUp=transform.up;var worldForward=Vector3.ProjectOnPlane(normalSum,worldUp).normalized;
   if(worldForward.sqrMagnitude<.5f)worldForward=transform.forward;
   var worldRight=Vector3.Cross(worldUp,worldForward).normalized;
   right=head.InverseTransformDirection(worldRight);up=head.InverseTransformDirection(worldUp);forward=head.InverseTransformDirection(worldForward);
   var local=points.Select(p=>new Vector3(Vector3.Dot(p-head.position,worldRight),Vector3.Dot(p-head.position,worldUp),Vector3.Dot(p-head.position,worldForward))).ToArray();
   var min=local.Aggregate(Vector3.Min);var max=local.Aggregate(Vector3.Max);var mid=(min+max)*.5f;
   center=head.InverseTransformPoint(head.position+worldRight*mid.x+worldUp*mid.y+worldForward*mid.z);
   radii=new Vector2(Mathf.Max((max.x-min.x)*.5f,.001f),Mathf.Max((max.y-min.y)*.5f,.001f));
   Vector2 Eye(Transform bone,float fallbackX){if(!bone)return new Vector2(fallbackX,-.1f);var d=bone.position-head.TransformPoint(center);return new Vector2(Vector3.Dot(d,worldRight)/radii.x,Vector3.Dot(d,worldUp)/radii.y);}
   var a=Eye(animator.GetBoneTransform(HumanBodyBones.LeftEye),-.45f);var b=Eye(animator.GetBoneTransform(HumanBodyBones.RightEye),.45f);
   leftEyePatch=new Vector4(a.x,a.y,.30f,.19f);rightEyePatch=new Vector4(b.x,b.y,.30f,.19f);
   mouthPatch=new Vector4(0,-.65f,.30f,.14f);
   // Use the actual surrounding surface normals, not a replacement whole-face dome.
   Vector3 SurroundingNormal(Vector4 patch){var sum=Vector3.zero;float total=0;
    for(int k=0;k<local.Length;k++){var xy=new Vector2((local[k].x-mid.x)/radii.x,(local[k].y-mid.y)/radii.y);var q=new Vector2((xy.x-patch.x)/patch.z,(xy.y-patch.y)/patch.w);float distance=q.magnitude;
     if(distance<1 || distance>2 || Vector3.Dot(pointNormals[k],worldForward)<.45f)continue;
     // Central nose vertices must not influence eye smoothing.
     if(Mathf.Abs(xy.x)<.2f && xy.y>mouthPatch.y+mouthPatch.w)continue;
     float weight=2-distance;sum+=pointNormals[k]*weight;total+=weight;
    }return head.InverseTransformDirection(total>0?sum.normalized:worldForward);
   }
   leftEyeNormal=SurroundingNormal(leftEyePatch);rightEyeNormal=SurroundingNormal(rightEyePatch);mouthNormal=SurroundingNormal(mouthPatch);

  }
  void LateUpdate()=>Refresh();
  public void Refresh(){if(!head)return;
   foreach(var b in bindings){if(!b.renderer)continue;block.Clear();b.renderer.GetPropertyBlock(block);
    block.SetFloat("_StaffFaceEnabled",b.face && CharacterToonToggle.IsCharacterToon(mode)?1:0);block.SetFloat("_StaffSkinStrength",b.skin && CharacterToonToggle.IsCharacterToon(mode)?1:0);
    // Linear RGB ratio of Herta's warm neck base (248,232,224) to cool skin base (232,228,228).
    block.SetVector("_StaffSkinScale",new Vector4(1.157f,1.040f,.962f,1));
    block.SetVector("_StaffFaceCenter",head.TransformPoint(center));block.SetVector("_StaffFaceRight",head.TransformDirection(right));block.SetVector("_StaffFaceUp",head.TransformDirection(up));block.SetVector("_StaffFaceForward",head.TransformDirection(forward));block.SetVector("_StaffFaceRadius",new Vector4(radii.x,radii.y,0,0));
    block.SetVector("_StaffLeftEyePatch",leftEyePatch);block.SetVector("_StaffRightEyePatch",rightEyePatch);block.SetVector("_StaffMouthPatch",mouthPatch);
    block.SetVector("_StaffLeftEyeNormal",head.TransformDirection(leftEyeNormal));block.SetVector("_StaffRightEyeNormal",head.TransformDirection(rightEyeNormal));block.SetVector("_StaffMouthNormal",head.TransformDirection(mouthNormal));
    block.SetFloat("_StaffTwoTone",0);block.SetFloat("_ToonEnabled",mode==CharacterShaderMode.CharacterToonTwoTone?1:0);b.renderer.SetPropertyBlock(block,b.index);
   }
  }
  void OnDisable(){foreach(var b in bindings)if(b.renderer)b.renderer.SetPropertyBlock(null,b.index);}
 }
}
