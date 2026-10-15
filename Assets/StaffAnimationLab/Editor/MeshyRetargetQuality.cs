using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;
namespace Staff.AnimationLab.Editor {
public static class MeshyRetargetQuality {
 [MenuItem("Tools/Animation Lab/Verify Corrected Meshy Poses")]
 public static void Run(){
  MeshyMotionSetup.Refresh();
  var catalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset");
  var path="Assets/ThirdParty/MeshyAnimations/KafkaNative60.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.SaveAndReimport();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var reference=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name=="Running");
  var running=catalog.entries.First(e=>e.category=="Meshy"&&e.clip.name=="Running").clip;
  foreach(var phase in new[]{.13f,.47f,.79f}){
   var tag=phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture);
   MeshyRetargetDiagnostic.Capture("Reference60-"+tag,source,reference,phase);
   foreach(var name in new[]{"Kafka_NoCoat","TheHerta"})MeshyRetargetDiagnostic.Capture("TargetFinal-"+name+"-"+tag,catalog.characters.First(c=>c.name==name),running,phase);
  }
  File.WriteAllText("/Users/gradient/Documents/GitHub/STAFF/3D Studio/Animation Lab/Meshy/RetargetFix/final_verification.txt",File.ReadAllText("Library/StaffMeshyImport.result")+"\nRendered Running at 3 phases on Kafka and The Herta, plus direct Meshy 60fps reference. Existing target rigs preserved.");
 }
}
}
