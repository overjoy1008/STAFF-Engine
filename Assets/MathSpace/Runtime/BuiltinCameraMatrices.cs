using UnityEngine;
namespace Staff.MathSpace {
// Supplies the GPU projection inverse for both Game and Scene cameras.
[ExecuteAlways, DisallowMultipleComponent] public sealed class BuiltinCameraMatrices : MonoBehaviour {
 void OnEnable(){Camera.onPreCull-=BeforeCamera;Camera.onPreCull+=BeforeCamera;}
 void OnDisable(){Camera.onPreCull-=BeforeCamera;}
 static void BeforeCamera(Camera camera){var projection=GL.GetGPUProjectionMatrix(camera.projectionMatrix,false);Shader.SetGlobalMatrix("_StaffInverseViewProjection",(projection*camera.worldToCameraMatrix).inverse);}
}}
