using UnityEngine;
using UnityEngine.InputSystem;

namespace Staff.City {
    /// <summary>Scene-local inspection camera; no global input or rendering changes.</summary>
    public sealed class CityExploreCamera : MonoBehaviour {
        public Vector3 homePosition;
        public Vector3 homeEuler;
        public float speed = 5f;
        float yaw, pitch;
        void OnEnable() { Sync(); }
        void Sync() { yaw = transform.eulerAngles.y; pitch = transform.eulerAngles.x; if (pitch > 180) pitch -= 360; }
        void Update() {
            var k = Keyboard.current; var m = Mouse.current;
            if (k == null || m == null || !Application.isFocused) { Release(); return; }
            if (k.rKey.wasPressedThisFrame) { transform.SetPositionAndRotation(homePosition, Quaternion.Euler(homeEuler)); Sync(); }
            if (k.escapeKey.wasPressedThisFrame || !m.rightButton.isPressed) { Release(); return; }
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            var d = m.delta.ReadValue(); yaw += d.x * .12f; pitch = Mathf.Clamp(pitch - d.y * .12f, -85, 85);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            var move = new Vector3((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.eKey.isPressed ? 1 : 0) - (k.qKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            transform.position += transform.TransformDirection(move.normalized) * speed * (k.leftShiftKey.isPressed ? 3 : 1) * Time.deltaTime;
        }
        void OnApplicationFocus(bool focus) { if (!focus) Release(); }
        void OnDisable() { Release(); }
        void Release() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
