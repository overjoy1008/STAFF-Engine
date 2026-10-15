using UnityEditor;
using UnityEngine;
namespace Staff.Interaction.Editor
{
    public static class SeatAuthoring
    {
        [MenuItem("STAFF/Interaction/Add seat to selected object")]
        static void AddSeat()
        {
            var selected=Selection.activeTransform;
            if(!selected)return;
            var go=new GameObject("Seat interaction");Undo.RegisterCreatedObjectUndo(go,"Add seat interaction");
            go.transform.SetParent(selected,false);
            var seat=go.AddComponent<SeatInteraction>();
            seat.hipsAnchor=new GameObject("Seated hips - adjust to chair").transform;
            seat.hipsAnchor.SetParent(go.transform,false);seat.hipsAnchor.localPosition=Vector3.up*.56f;
            seat.exitAnchor=new GameObject("Stand position - keep clear").transform;
            seat.exitAnchor.SetParent(go.transform,false);seat.exitAnchor.localPosition=new Vector3(0,.03f,1);
            Selection.activeGameObject=go;
        }
        [MenuItem("STAFF/Interaction/Add seat to selected object",true)]
        static bool CanAdd()=>Selection.activeTransform&&!EditorApplication.isPlaying;
    }
    [CustomEditor(typeof(SeatInteraction))]
    public sealed class SeatInteractionEditor:UnityEditor.Editor
    {
        void OnSceneGUI()
        {
            var seat=(SeatInteraction)target;
            if(seat.hipsAnchor){Handles.color=Color.yellow;Handles.ArrowHandleCap(0,seat.hipsAnchor.position,seat.hipsAnchor.rotation,.6f,EventType.Repaint);}
            if(seat.exitAnchor){Handles.color=Color.cyan;Handles.DrawWireDisc(seat.exitAnchor.position,Vector3.up,.3f);}
        }
    }
}
