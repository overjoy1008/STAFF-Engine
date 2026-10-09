using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Staff.Characters.Editor {
 [InitializeOnLoad] public static class ExpressionDeployment {
  static ExpressionDeployment(){EditorApplication.update+=Poll;}
  static void Poll(){const string request="Library/StaffExpressions/deploy-request.txt";if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   File.Delete(request);try{var c=Resources.Load<ExpressionCatalog>("StaffExpressionCatalog");if(!c||c.characters.Length!=11||!c.blushMaterial||ShaderUtil.ShaderHasError(c.blushMaterial.shader))throw new Exception("Invalid expression catalog or shader");var d=JsonUtility.FromJson<AdventureExpressions.Data>(c.json.text);foreach(var model in c.characters){if(!model.avatar||model.overlays.Length==0)throw new Exception("Missing expression model "+model.id);foreach(var o in model.overlays)if(!o.source||!o.blush)throw new Exception("Missing cheek mesh "+model.id);}foreach(var entry in d.characters)if(entry.presets.Length!=23)throw new Exception("Missing presets");
    // Domain reload while playing does not fire sceneLoaded again.
    if(EditorApplication.isPlaying)foreach(var p in UnityEngine.Object.FindObjectsByType<AdventurePlayer>(FindObjectsSortMode.None))if(!p.GetComponent<AdventureExpressions>())p.gameObject.AddComponent<AdventureExpressions>();
    File.WriteAllText("Library/StaffExpressions/deployment.json","{\"pass\":true,\"characters\":11,\"presets\":23,\"liveEditorImported\":true}");
   }catch(Exception e){File.WriteAllText("Library/StaffExpressions/deploy-error.txt",e.ToString());Debug.LogException(e);}
  }
 }
}
