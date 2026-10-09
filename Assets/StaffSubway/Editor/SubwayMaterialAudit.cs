using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
namespace Staff.Subway.Editor {
public static class SubwayMaterialAudit {
[MenuItem("STAFF/Subway/Audit Car Lighting")]
static void Run(){
var s=new StringBuilder();
foreach(var p in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))s.AppendLine($"PROBE {p.name} bounds={p.bounds} texture={p.texture} scale={p.transform.lossyScale}");
foreach(var car in Object.FindObjectsByType<SubwayCar>(FindObjectsSortMode.None).OrderBy(c=>c.transform.position.z)){
s.AppendLine($"CAR {car.name} {car.transform.position}");
foreach(var r in car.GetComponentsInChildren<Renderer>()){
if(!r.sharedMaterials.Any(m=>m&&m.name.StartsWith("Body")))continue;
var probes=new List<ReflectionProbeBlendInfo>();r.GetClosestReflectionProbes(probes);
s.AppendLine($"  {r.name} bounds={r.bounds} probeUsage={r.reflectionProbeUsage} probes={string.Join(",",probes.Select(p=>p.probe.name+":"+p.weight))}");
foreach(var m in r.sharedMaterials)s.AppendLine($"    {m.name} shader={m.shader.name} tex={AssetDatabase.GetAssetPath(m.mainTexture)} color={m.color} emission={(m.HasProperty("_EmissionColor")?m.GetColor("_EmissionColor").ToString():"-")}");
}
}
File.WriteAllText("Library/StaffSubway/material-audit.txt",s.ToString());
}}
}
