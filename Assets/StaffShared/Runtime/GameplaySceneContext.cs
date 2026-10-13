using UnityEngine;
namespace Staff.Shared {
// Scene-specific geometry references must never be applied to the shared gameplay prefab.
public sealed class GameplaySceneContext:MonoBehaviour {
 public Transform environmentRoot;
 public Transform[] additionalEnvironmentRoots=new Transform[0];
 public Material reflectionFloor;
 public float reflectionHeight=.004f;
}
}
