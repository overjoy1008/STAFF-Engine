using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class StaffURPBake {
 public static void Run(){try{
 StaffURPMaterials.Models();StaffURPScenes.Configure();EditorSceneManager.OpenScene(StaffURPScenes.Scenes[1]);
 var paths=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>GameObjectUtility.AreStaticEditorFlagsSet(f.gameObject,StaticEditorFlags.ContributeGI)).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).Distinct().ToArray();foreach(var path in paths){var mi=AssetImporter.GetAtPath(path) as ModelImporter;if(mi&&!mi.generateSecondaryUV){mi.generateSecondaryUV=true;mi.SaveAndReimport();}}
 // A sparse regular probe grid gives moving characters/train interiors baked indirect light.
 var go=GameObject.Find("Station Light Probes");if(!go)go=new GameObject("Station Light Probes");go.transform.SetParent(GameObject.Find("Lighting")?.transform,false);var group=go.GetComponent<LightProbeGroup>();if(!group)group=go.AddComponent<LightProbeGroup>();var probes=new System.Collections.Generic.List<Vector3>();for(float z=-12;z<=95;z+=8)foreach(float x in new[]{-6f,-2f,2f,6f})foreach(float y in new[]{.4f,1.7f,3.5f})probes.Add(new Vector3(x,y,z));group.probePositions=probes.ToArray();EditorSceneManager.SaveOpenScenes();
 Debug.Log("STAFF_URP_BAKE_START");bool success=Lightmapping.Bake();EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();File.WriteAllText("Library/StaffURPValidation/bake.txt","success="+success+" lightmaps="+LightmapSettings.lightmaps.Length+" probes="+probes.Count);Debug.Log("STAFF_URP_BAKE_COMPLETE "+success+" maps="+LightmapSettings.lightmaps.Length);EditorApplication.Exit(success?0:1);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
