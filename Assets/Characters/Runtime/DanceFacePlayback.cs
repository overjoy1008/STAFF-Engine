using System;
using System.Collections.Generic;
using UnityEngine;
namespace Staff.Characters
{
    // MMD morphs are separate from Humanoid muscles. Apply them after Animator evaluation.
    public sealed class DanceFacePlayback
    {
        [Serializable] public class Track { public string name; public float[] weights; }
        [Serializable] public class Clip { public string id; public float fps; public Track[] tracks; }
        [Serializable] public class Data { public string character; public Clip[] clips; }
        class Binding { public SkinnedMeshRenderer renderer; public int index; public float original; public Track track; }
        readonly Dictionary<Avatar,Data> cache = new Dictionary<Avatar,Data>();
        readonly List<Binding> bindings = new List<Binding>();
        DanceFaceCatalog catalog;
        float fps;
        public int BindingCount => bindings.Count;
        public static string ShapeName(string name)
        { int separator=name.LastIndexOf('.'); return separator<0?name:name.Substring(separator+1); }
        public void Begin(Animator animator,string id)
        {
            Reset();
            if(!animator || !animator.avatar)return;
            if(!catalog)catalog=Resources.Load<DanceFaceCatalog>("StaffDanceFaces");
            if(!catalog)return;
            if(!cache.TryGetValue(animator.avatar,out var data))
            {
                foreach(var item in catalog.characters)
                    if(item.avatar==animator.avatar && item.samples)
                    {data=JsonUtility.FromJson<Data>(item.samples.text);cache[animator.avatar]=data;break;}
            }
            if(data?.clips==null)return;
            var clip=Array.Find(data.clips,c=>c.id==id);
            if(clip?.tracks==null)return;
            fps=clip.fps;
            var tracks=new Dictionary<string,Track>();
            foreach(var track in clip.tracks)tracks[track.name]=track;
            foreach(var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh=renderer.sharedMesh;if(!mesh)continue;
                for(int i=0;i<mesh.blendShapeCount;i++)
                    if(tracks.TryGetValue(ShapeName(mesh.GetBlendShapeName(i)),out var track))
                        bindings.Add(new Binding{renderer=renderer,index=i,original=renderer.GetBlendShapeWeight(i),track=track});
            }
        }
        public void Apply(float seconds)
        {
            float frame=Mathf.Max(0,seconds*fps);
            foreach(var binding in bindings)
            {
                if(!binding.renderer)continue;
                var weights=binding.track.weights;if(weights==null||weights.Length==0)continue;
                int a=Mathf.Min(Mathf.FloorToInt(frame),weights.Length-1),b=Mathf.Min(a+1,weights.Length-1);
                binding.renderer.SetBlendShapeWeight(binding.index,Mathf.Lerp(weights[a],weights[b],frame-a));
            }
        }
        public void Reset()
        {
            foreach(var binding in bindings)
                if(binding.renderer)binding.renderer.SetBlendShapeWeight(binding.index,binding.original);
            bindings.Clear();
        }
    }
}
