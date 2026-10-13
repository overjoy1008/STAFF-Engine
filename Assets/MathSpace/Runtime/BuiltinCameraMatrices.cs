using UnityEngine;
using UnityEngine.Rendering;
namespace Staff.MathSpace {
[ExecuteAlways,DisallowMultipleComponent] public sealed class BuiltinCameraMatrices:MonoBehaviour{
 void OnEnable(){Camera.onPreCull+=BeforeCamera;RenderPipelineManager.beginCameraRendering+=BeforeSRP;}
 void OnDisable(){Camera.onPreCull-=BeforeCamera;RenderPipelineManager.beginCameraRendering-=BeforeSRP;}
 static void BeforeSRP(ScriptableRenderContext c,Camera camera){BeforeCamera(camera);}
 static void BeforeCamera(Camera camera){var projection=GL.GetGPUProjectionMatrix(camera.projectionMatrix,camera.targetTexture!=null||GraphicsSettings.currentRenderPipeline!=null);Shader.SetGlobalMatrix("_StaffInverseViewProjection",(projection*camera.worldToCameraMatrix).inverse);}
}}
