using UnityEngine;
namespace Staff.Subway {
 [ExecuteAlways,RequireComponent(typeof(Camera))] public sealed class SubwayPlanarReflection : MonoBehaviour {
  public Material floorMaterial;public float height=.004f;public int resolution=1024;
  Camera mirror;RenderTexture texture;bool rendering;
  void OnPreCull(){
   if(rendering||!floorMaterial)return;var source=GetComponent<Camera>();
   if(!mirror){var go=new GameObject("Subway Reflection Camera"){hideFlags=HideFlags.HideAndDontSave};mirror=go.AddComponent<Camera>();mirror.enabled=false;}
   if(!texture){texture=new RenderTexture(resolution,resolution,24,RenderTextureFormat.ARGBHalf){name="Subway Planar Reflection",useMipMap=true,autoGenerateMips=true,hideFlags=HideFlags.HideAndDontSave};texture.Create();}
   mirror.CopyFrom(source);mirror.enabled=false;mirror.targetTexture=texture;mirror.cullingMask=source.cullingMask&~(1<<29);mirror.useOcclusionCulling=false;
   var reflection=Matrix4x4.identity;reflection.m11=-1;reflection.m13=2*height;
   mirror.worldToCameraMatrix=source.worldToCameraMatrix*reflection;
   Vector3 pos=source.transform.position;pos.y=2*height-pos.y;mirror.transform.position=pos;
   var normal=mirror.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
   var point=mirror.worldToCameraMatrix.MultiplyPoint(new Vector3(0,height+.015f,0));
   mirror.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(point,normal)));
   bool old=GL.invertCulling;
   try{rendering=true;GL.invertCulling=!old;mirror.Render();floorMaterial.SetTexture("_ReflectionTex",texture);}
   finally{GL.invertCulling=old;rendering=false;}
  }
  void OnDisable(){if(mirror)DestroyImmediate(mirror.gameObject);if(texture){texture.Release();DestroyImmediate(texture);}if(floorMaterial)floorMaterial.SetTexture("_ReflectionTex",null);}
 }
}
