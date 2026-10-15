using UnityEngine;
using UnityEngine.SceneManagement;
using Staff.Subway;
namespace Staff.Interaction
{
    public static class InteractionSceneInstaller
    {
        public static void Install(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Cabin seat between doors")
                {
                    if (t.GetComponentInChildren<SeatInteraction>(true)) continue;
                    int count = Mathf.Clamp(Mathf.FloorToInt(t.localScale.z / .8f), 1, 8);
                    for (int index = 0; index < count; index++)
                    {
                        var cabinSeat = new GameObject("Cabin seat interaction").AddComponent<SeatInteraction>();
                        cabinSeat.transform.SetParent(t, false);
                        // The cushion is a scaled cube. Cancel only that cube's scale, retaining the car's scale.
                        cabinSeat.transform.localScale = new Vector3(1 / t.localScale.x, 1 / t.localScale.y, 1 / t.localScale.z);
                        float z = (index - (count - 1) * .5f) * .8f;
                        float facing = t.localPosition.x > 0 ? -1 : 1;
                        cabinSeat.hipsAnchor = new GameObject("Seated hips").transform;
                        cabinSeat.hipsAnchor.SetParent(cabinSeat.transform, false);
                        cabinSeat.hipsAnchor.localPosition = new Vector3(0, t.localScale.y * .5f + .08f, z);
                        cabinSeat.hipsAnchor.localRotation = Quaternion.Euler(0, facing * 90, 0);
                        cabinSeat.exitAnchor = new GameObject("Stand position").transform;
                        cabinSeat.exitAnchor.SetParent(cabinSeat.transform, false);
                        cabinSeat.exitAnchor.localPosition = new Vector3(facing * .85f, .18f - t.localPosition.y, z);
                    }
                    continue;
                }
                bool street = t.name == "Street bench";
                if (!street && t.name != "Black bench" && t.name != "Secondary concourse bench") continue;
                if (t.GetComponentInChildren<SeatInteraction>(true)) continue;
                // Subway models are normalized by their builder; the outer station scale is retained.
                for (int i = 0; i < 2; i++)
                {
                    var seat = new GameObject("Seat interaction").AddComponent<SeatInteraction>();
                    seat.transform.SetParent(t, false);
                    seat.hipsAnchor = new GameObject("Seated hips").transform;
                    seat.hipsAnchor.SetParent(seat.transform, false);
                    seat.hipsAnchor.localPosition = new Vector3(i == 0 ? -.42f : .42f, .56f, 0);
                    seat.hipsAnchor.localRotation = Quaternion.Euler(0, street ? 180 : 0, 0);
                    seat.exitAnchor = new GameObject("Stand position").transform;
                    seat.exitAnchor.SetParent(seat.transform, false);
                    seat.exitAnchor.localPosition = new Vector3(i == 0 ? -.42f : .42f, .03f, street ? -.95f : .95f);
                    seat.range = 2.5f;
                }
            }
        }
        public static void InstallDoors(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var doors in root.GetComponentsInChildren<SubwayDoors>())
            foreach (var gate in doors.doorwayGates)
            {
                if (!gate || gate.name.EndsWith(" L") || gate.GetComponent<SubwayDoorInteraction>()) continue;
                var action = gate.gameObject.AddComponent<SubwayDoorInteraction>();
                action.doors = doors; action.gate = gate; action.range = 3;
            }
        }
    }
}
