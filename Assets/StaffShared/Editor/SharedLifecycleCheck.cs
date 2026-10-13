using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;using Staff.Subway;
namespace Staff.Shared.Editor {
[InitializeOnLoad] public static class SharedLifecycleCheck {
 static SharedLifecycleCheck(){EditorApplication.update+=Tick;}
 static void Tick(){const string w="Library/StaffShared";if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||!File.Exists(w+"/lifecycle.request"))return;File.Delete(w+"/lifecycle.request");var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  try{var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SharedMigration.Sky),scene);var sky=go.GetComponentInChildren<SubwayTimeOfDay>();sky.enabled=false;sky.enabled=true;sky.atmosphere.Tick(true);sky.ApplySky();
   var cameraObject=new GameObject("Late camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;var box=cameraObject.AddComponent<Skybox>();box.enabled=false;box.material=null;
   sky.ApplyToCamera(camera);if(!box.enabled||!box.material||camera.clearFlags!=CameraClearFlags.Skybox)throw new Exception("Late camera did not bind");sky.enabled=false;if(box.enabled||box.material||camera.clearFlags!=CameraClearFlags.SolidColor)throw new Exception("Camera state did not restore");
   File.WriteAllText(w+"/lifecycle-validation.txt","PASS: sky instantiated before camera; late-added camera binds; disabling sky restores original camera flags/material/enabled state.\n");
  }catch(Exception e){File.WriteAllText(w+"/lifecycle-validation.txt","FAIL "+e);Debug.LogException(e);}finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
 }
}
}
