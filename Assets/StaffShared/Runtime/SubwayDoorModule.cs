using UnityEngine;
using Staff.Subway;
namespace Staff.Shared {
public sealed class SubwayDoorModule:MonoBehaviour {
 public SubwayDoors.Leaf[] leaves;
 public BoxCollider[] gates;
 public Bounds localBodyBounds;
}
}
