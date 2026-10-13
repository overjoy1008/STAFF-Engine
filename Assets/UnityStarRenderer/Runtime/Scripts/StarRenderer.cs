// Unity Star Renderer — Thomas Baron, MIT. STAFF terrestrial/URP adaptation.
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public class StarRenderer : MonoBehaviour
{
    public TextAsset starDataAsset;
    public Material starMaterial;
    [Range(0,6.5f)] public float magnitudeLimit = 6.5f;
    public int StarCount { get; private set; }
    public IReadOnlyList<Mesh> Meshes => meshes;
    readonly List<Mesh> meshes = new List<Mesh>();
    TextAsset builtAsset;
    float builtLimit = -1;

    public void EnsureMeshes()
    {
        if (builtAsset == starDataAsset && Mathf.Approximately(builtLimit,magnitudeLimit)) return;
        Release();
        if (!starDataAsset) return;
        var positions = new List<Vector3>(); var photometry = new List<Vector2>();
        var rows = starDataAsset.text.Split('\n');
        for (int i=1; i<rows.Length; i++) {
            var c=rows[i].Split(',');
            if(c.Length<23 || !Read(c[13],out float mag) || mag>magnitudeLimit ||
               !Read(c[17],out float x) || !Read(c[19],out float y) || !Read(c[18],out float z) ||
               !Read(c[14],out float absolute)) continue;
            var p=new Vector3(x,y,z); if(p.sqrMagnitude<1e-12f) continue;
            Read(c[16],out float color); positions.Add(p);photometry.Add(new Vector2(absolute,color));
        }
        StarCount=positions.Count;
        for(int start=0; start<StarCount; start+=5000) {
            int count=Math.Min(5000,StarCount-start);
            var v=new Vector3[count*4];var normals=new Vector3[v.Length];
            var uv=new Vector2[v.Length];var data=new Vector2[v.Length];var tris=new int[count*6];
            for(int j=0;j<count;j++) {
                int k=j*4,t=j*6;
                for(int n=0;n<4;n++){v[k+n]=positions[start+j];normals[k+n]=-Vector3.forward;data[k+n]=photometry[start+j];}
                uv[k]=Vector2.zero;uv[k+1]=Vector2.right;uv[k+2]=Vector2.up;uv[k+3]=Vector2.one;
                tris[t]=k;tris[t+1]=k+2;tris[t+2]=k+1;tris[t+3]=k+2;tris[t+4]=k+3;tris[t+5]=k+1;
            }
            var mesh=new Mesh { name="HYG stars "+start,hideFlags=HideFlags.HideAndDontSave };
            mesh.vertices=v;mesh.normals=normals;mesh.uv=uv;mesh.uv2=data;mesh.triangles=tris;
            mesh.UploadMeshData(true);meshes.Add(mesh);
        }
        builtAsset=starDataAsset;builtLimit=magnitudeLimit;
    }
    static bool Read(string s,out float v) => float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v);
    void OnDisable() => Release();
    public void Release() {
        foreach(var m in meshes) { if(Application.isPlaying) Destroy(m); else DestroyImmediate(m); }
        meshes.Clear();StarCount=0;builtAsset=null;builtLimit=-1;
    }
}
