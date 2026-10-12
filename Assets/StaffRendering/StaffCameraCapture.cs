using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Staff.Rendering {
 public static class StaffCameraCapture {
  public static void Render(Camera camera) {
   if(!camera.targetTexture)throw new System.InvalidOperationException("Capture requires a target texture.");
   RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=camera.targetTexture});
  }
 }
}
