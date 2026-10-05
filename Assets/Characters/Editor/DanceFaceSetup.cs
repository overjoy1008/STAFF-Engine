using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Staff.Characters.Editor
{
    public static class DanceFaceSetup
    {
        [MenuItem("Tools/Characters/Import Dance Faces")]
        public static void BuildBatch()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            const string path="Assets/Characters/Resources/StaffDanceFaces.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<DanceFaceCatalog>(path);
            if(!catalog){catalog=ScriptableObject.CreateInstance<DanceFaceCatalog>();AssetDatabase.CreateAsset(catalog,path);}
            catalog.characters=Directory.GetFiles("Assets/ThirdParty/StaffDances/Faces","*.json").OrderBy(p=>p).Select(p=>
            {
                string slug=Path.GetFileNameWithoutExtension(p);
                string modelPath="Assets/ThirdParty/StaffCharacters/"+slug+"/"+slug+".fbx";
                var avatar=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().First(a=>a.isValid&&a.isHuman);
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                int shapes=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Sum(r=>r.sharedMesh?r.sharedMesh.blendShapeCount:0);
                if(shapes==0)throw new System.Exception("Missing facial blendshapes: "+slug);
                Debug.Log("FACE MODEL: "+slug+" shapes="+shapes);
                return new DanceFaceCatalog.Entry{avatar=avatar,samples=AssetDatabase.LoadAssetAtPath<TextAsset>(p)};
            }).ToArray();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
    }
}
