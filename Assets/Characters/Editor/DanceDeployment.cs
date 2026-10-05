using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Staff.Characters.Editor
{
    // Apply imported assets in the already-open editor without reopening its scene.
    [InitializeOnLoad] public static class DanceDeployment
    {
        const string Request="Library/StaffDance/apply-request.txt";
        static DanceDeployment(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(Request);
            try {DanceSetup.BuildBatch();File.WriteAllText("Library/StaffDance/deploy-result.txt","PASS: dance library imported in current editor; existing open scene preserved.");}
            catch(Exception e){File.WriteAllText("Library/StaffDance/deploy-error.txt",e.ToString());Debug.LogException(e);}
        }
    }
}
