using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Staff.AnimationLab.Editor {
public static class MixamoLibraryVerification {
 [Serializable] sealed class Report {public string status;public int mixamoEntries,packFilters,primaryFilters,relativeSelections;public bool idSearchMetadata,scaleFlags,sceneCatalogReference;public string error;}
 [MenuItem("Tools/Animation Lab/Verify Complete Mixamo Library Filters")]
 public static void Run(){var report=new Report();GameObject holder=null;Scene preview=default;
  try {
   var catalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset");var motions=catalog.entries.Where(e=>e.category=="Mixamo").ToArray();report.mixamoEntries=motions.Length;
   if(motions.Length!=2446||motions.Select(e=>e.catalogId).Distinct().Count()!=2446)throw new Exception("Mixamo ID count mismatch");
   report.idSearchMetadata=motions.All(e=>e.title.EndsWith(" ["+e.catalogId.Split(':')[1]+"]")&&e.source.Contains(e.catalogId));if(!report.idSearchMetadata)throw new Exception("Missing ID display/search metadata");
   report.scaleFlags=MixamoLibraryImport.Manifest.motions.All(m=>{var i=(ModelImporter)AssetImporter.GetAtPath(m.path);return i&&i.useFileScale&&i.useFileUnits&&Math.Abs(i.globalScale-1f)<.0001f&&i.animationType==ModelImporterAnimationType.Human;});if(!report.scaleFlags)throw new Exception("Missing scale flags");
   var scene=EditorSceneManager.OpenScene(AnimationLabSetup.ScenePath);var original=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AnimationLabPlayer>(true)).Single();report.sceneCatalogReference=original.catalog==catalog;if(!report.sceneCatalogReference)throw new Exception("Test scene references a different catalog");
   preview=EditorSceneManager.NewPreviewScene();holder=new GameObject("Mixamo runtime filter check");SceneManager.MoveGameObjectToScene(holder,preview);var lab=holder.AddComponent<AnimationLabPlayer>();lab.enabled=false;lab.catalog=catalog;
   int character=Array.FindIndex(catalog.characters,c=>c&&c.name=="Nicole");if(character<0)throw new Exception("Missing Nicole");lab.SelectCharacter(character);lab.SelectCategory("Mixamo");if(lab.VisibleIndices().Length!=2446)throw new Exception("Mixamo runtime filter count mismatch");
   foreach(var pack in MixamoLibraryImport.Manifest.packs){lab.SelectSubcategory(pack.title);var actual=lab.VisibleIndices().Select(i=>catalog.entries[i].catalogId).ToHashSet();if(!actual.SetEquals(pack.motionIds))throw new Exception("Runtime pack filter mismatch "+pack.id);lab.SelectRelative(1);if(!actual.Contains(lab.Current.catalogId))throw new Exception("Relative selection escaped filter");lab.Seek(lab.Current.clip.length*.5);if(double.IsNaN(lab.TimePosition)||double.IsInfinity(lab.TimePosition))throw new Exception("Invalid scrub time");lab.TogglePause();if(lab.Playing)throw new Exception("Pause failed");report.packFilters++;report.relativeSelections++;}
   foreach(var primary in motions.Select(e=>e.subcategory).Distinct()){lab.SelectSubcategory(primary);if(lab.VisibleIndices().Length==0)throw new Exception("Empty primary subcategory "+primary);report.primaryFilters++;}
   report.status="complete";
  }catch(Exception e){report.status="failed";report.error=e.ToString();Debug.LogException(e);}
  finally {if(holder)UnityEngine.Object.DestroyImmediate(holder);if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);File.WriteAllText(MixamoLibraryImport.ReportRoot+"/runtime-verification.json",JsonUtility.ToJson(report,true));Debug.Log("MIXAMO RUNTIME FILTER CHECK "+JsonUtility.ToJson(report));if(Application.isBatchMode)EditorApplication.Exit(report.status=="complete"?0:1);}
 }
}
}
