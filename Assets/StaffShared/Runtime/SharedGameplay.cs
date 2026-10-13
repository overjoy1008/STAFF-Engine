using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Staff.Characters;
using Staff.Subway;
namespace Staff.Shared {
// One prefab owns both sides of the player/camera relationship. Scene data lives separately.
[ExecuteAlways,DefaultExecutionOrder(-1000),DisallowMultipleComponent]
public sealed class SharedGameplay:MonoBehaviour {
 public AdventurePlayer player;
 public AdventureCamera cameraController;
 bool bound;
 void OnEnable(){bound=false;BindScene();}
 void Update(){if(!bound)BindScene();}
 public void BindScene(){
  if(!gameObject.scene.IsValid()||!gameObject.scene.isLoaded||!cameraController)return;
  bound=true;var roots=gameObject.scene.GetRootGameObjects();
  var context=roots.SelectMany(g=>g.GetComponentsInChildren<GameplaySceneContext>(true)).FirstOrDefault();
  var mode=cameraController.GetComponent<AbstractEnvironment>();
  if(mode){mode.environmentRoot=context&&context.environmentRoot?context.environmentRoot:roots.FirstOrDefault(g=>g.name=="Environment")?.transform;mode.additionalEnvironmentRoots=context?context.additionalEnvironmentRoots:roots.Where(g=>g.name=="Coordinates").Select(g=>g.transform).ToArray();}
  var mirror=cameraController.GetComponent<SubwayPlanarReflection>();
  if(mirror){mirror.floorMaterial=context?context.reflectionFloor:null;mirror.height=context?context.reflectionHeight:.004f;mirror.enabled=mirror.floorMaterial;}
  cameraController.GetComponent<Camera>().GetUniversalAdditionalCameraData().SetRenderer(0);
 }
}
}
