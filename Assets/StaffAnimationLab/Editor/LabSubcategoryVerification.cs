using System;using System.IO;using System.Linq;using System.Reflection;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace Staff.AnimationLab.Editor {
public static class LabSubcategoryVerification {
 const string Out="/Users/gradient/Documents/GitHub/STAFF/3D Studio/Animation Lab/Meshy/LocomotionExpanded";
 [MenuItem("Tools/Animation Lab/Verify Subcategories")]
 public static void Run(){
 var catalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset");var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("Filter verification");SceneManager.MoveGameObjectToScene(go,scene);var lab=go.AddComponent<AnimationLabPlayer>();lab.enabled=false;lab.catalog=catalog;
 GameObject Child(string name){var o=new GameObject(name);o.transform.SetParent(go.transform);return o;}
 lab.viewCamera=Child("Camera").AddComponent<Camera>();lab.seat=Child("Seat");lab.wall=Child("Wall");lab.water=Child("Water");
 try{typeof(AnimationLabPlayer).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(lab,null);lab.SelectCategory("Meshy");lab.SelectSubcategory("Walking & Running");if(lab.VisibleIndices().Length!=21)throw new Exception("Meshy filter count");lab.SelectRelative(1);if(lab.Current.category!="Meshy")throw new Exception("Next escaped filter");lab.SelectCategory("Quaternius");if(lab.VisibleIndices().Length!=84)throw new Exception("Quaternius count");var subs=lab.Subcategories();if(subs.Length!=11)throw new Exception("Expected 10 Quaternius subcategories, got "+(subs.Length-1));foreach(var sub in subs.Skip(1)){lab.SelectSubcategory(sub);if(lab.VisibleIndices().Length==0||lab.VisibleIndices().Any(i=>catalog.entries[i].subcategory!=sub))throw new Exception("Invalid filter "+sub);}lab.SelectCategory("Tripo");if(lab.VisibleIndices().Length!=97)throw new Exception("Category reset failed");File.WriteAllText(Out+"/filter-validation.txt","PASS: Meshy 21; Quaternius 84 across 10 subcategories; next respects filter; category change resets filter; Tripo 97.");}
 finally{UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
 var target=catalog.characters.First(c=>c.name=="TheHerta");foreach(var name in new[]{"Thoughtful_Walk"}){var entry=catalog.entries.First(e=>e.category=="Meshy"&&e.clip.name==name);var label="TargetExpanded-"+name;MeshyRetargetDiagnostic.Capture(label,target,entry.clip);File.Copy("/Users/gradient/Documents/GitHub/STAFF/3D Studio/Animation Lab/Meshy/RetargetFix/"+label+".png",Out+"/"+label+".png",true);}
 Debug.Log("SUBCATEGORY VERIFICATION PASSED");
 }
}
}
