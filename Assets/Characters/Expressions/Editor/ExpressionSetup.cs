using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Staff.Characters.Editor {
 public static class ExpressionSetup {
  [Serializable] class MaterialMap { public Item[] characters; }
  [Serializable] class Item { public string character; public string[] materials; }
  [MenuItem("Tools/Characters/Install Expression Library")]
  public static void BuildBatch(){
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   const string root="Assets/Characters/Expressions";
   Directory.CreateDirectory(root+"/Generated");AssetDatabase.Refresh();
   var text=AssetDatabase.LoadAssetAtPath<TextAsset>(root+"/Resources/StaffExpressions.json");
   var data=JsonUtility.FromJson<AdventureExpressions.Data>(text.text);
   var map=JsonUtility.FromJson<MaterialMap>(File.ReadAllText(root+"/Resources/ExpressionMaterials.json"));
   var catalog=AssetDatabase.LoadAssetAtPath<ExpressionCatalog>(root+"/Resources/StaffExpressionCatalog.asset");
   if(!catalog){catalog=ScriptableObject.CreateInstance<ExpressionCatalog>();AssetDatabase.CreateAsset(catalog,root+"/Resources/StaffExpressionCatalog.asset");}
   catalog.json=text;
   var shader=Shader.Find("STAFF/ExpressionCheekBlush");if(!shader)throw new Exception("Missing blush shader");
   if(UnityEditor.ShaderUtil.ShaderHasError(shader))throw new Exception("Blush shader compile error");
   var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"/Resources/ExpressionBlush.mat");if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,root+"/Resources/ExpressionBlush.mat");}catalog.blushMaterial=mat;
   catalog.characters=data.characters.Select(c=>{
    string path="Assets/ThirdParty/StaffCharacters/"+c.character+"/"+c.character+".fbx";
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().First(a=>a.isValid&&a.isHuman);
    var overlays=new System.Collections.Generic.List<ExpressionCatalog.Overlay>();var face=Array.Find(map.characters,m=>m.character==c.character);
    var meshes=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
    var names=meshes.SelectMany(r=>Enumerable.Range(0,r.sharedMesh.blendShapeCount).Select(i=>DanceFacePlayback.ShapeName(r.sharedMesh.GetBlendShapeName(i)))).ToHashSet();
    foreach(var p in c.presets)foreach(var w in p.weights)if(!names.Contains(w.name))throw new Exception(c.character+" missing "+w.name);
    foreach(var r in meshes){var indices=new System.Collections.Generic.List<int>();for(int i=0;i<r.sharedMaterials.Length;i++)if(face.materials.Contains(r.sharedMaterials[i].name))indices.AddRange(r.sharedMesh.GetTriangles(i));if(indices.Count==0)continue;
     var source=r.sharedMesh;var selected=indices.Distinct().ToArray();var lookup=selected.Select((v,i)=>new {v,i}).ToDictionary(v=>v.v,v=>v.i);indices=indices.Select(i=>lookup[i]).ToList();
     var vertices=source.vertices;var normals=source.normals;var boneWeights=source.boneWeights;var mesh=new Mesh();mesh.name=c.character+"CheekBlush";mesh.vertices=selected.Select(i=>vertices[i]).ToArray();mesh.normals=selected.Select(i=>normals[i]).ToArray();mesh.boneWeights=selected.Select(i=>boneWeights[i]).ToArray();mesh.bindposes=source.bindposes;mesh.SetTriangles(indices,0);
     var dv=new Vector3[source.vertexCount];var dn=new Vector3[source.vertexCount];var dt=new Vector3[source.vertexCount];
     for(int shape=0;shape<source.blendShapeCount;shape++)for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++){source.GetBlendShapeFrameVertices(shape,frame,dv,dn,dt);mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),selected.Select(i=>dv[i]).ToArray(),selected.Select(i=>dn[i]).ToArray(),selected.Select(i=>dt[i]).ToArray());}
     var positions=mesh.vertices.Select(v=>prefab.transform.InverseTransformPoint(r.transform.TransformPoint(v))).ToArray();var points=indices.Distinct().Select(i=>positions[i]).ToArray();
     float minX=points.Min(p=>p.x),maxX=points.Max(p=>p.x),minY=points.Min(p=>p.y),maxY=points.Max(p=>p.y),minZ=points.Min(p=>p.z),maxZ=points.Max(p=>p.z),width=maxX-minX,height=maxY-minY;
     if(width<.00001f||height<.00001f)throw new Exception("Invalid face bounds "+c.character);
     var colors=new Color[mesh.vertexCount];foreach(int i in indices.Distinct()){var v=positions[i];float dx=(Mathf.Abs(v.x-(minX+maxX)*.5f)-width*.27f)/(width*.17f),dy=(v.y-(minY+height*.40f))/(height*.105f);float front=Mathf.SmoothStep(0,1,Mathf.InverseLerp(minZ+(maxZ-minZ)*.45f,minZ+(maxZ-minZ)*.75f,v.z));colors[i]=new Color(1,1,1,Mathf.Exp(-2*(dx*dx+dy*dy))*front);}
     if(colors.Max(v=>v.a)<.1f)throw new Exception("Empty cheek mask "+c.character);mesh.colors=colors;
     string output=root+"/Generated/"+c.character+"-"+overlays.Count+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(output);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,output);
     overlays.Add(new ExpressionCatalog.Overlay{source=r.sharedMesh,blush=mesh});
    }
    if(overlays.Count==0)throw new Exception("Missing face material "+c.character);return new ExpressionCatalog.Character{id=c.character,avatar=avatar,overlays=overlays.ToArray()};
   }).ToArray();
   EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Directory.CreateDirectory("Library/StaffExpressions");File.WriteAllText("Library/StaffExpressions/install.json","{\"pass\":true,\"characters\":"+catalog.characters.Length+",\"presets\":"+data.characters[0].presets.Length+"}");
  }
 }
}
