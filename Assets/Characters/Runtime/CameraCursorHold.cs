using UnityEngine;
using UnityEngine.InputSystem;
namespace Staff.Characters
{
    // Temporary cursor release does not release gameplay input or interrupt dances.
    public sealed class CameraCursorHold
    {
        bool released;
        int resumedFrame = -1;
        public bool OptionHeld => Keyboard.current != null &&
            (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);
        public bool LookPaused => OptionHeld || resumedFrame == Time.frameCount;
        public void Update(bool held, bool canCapture)
        {
            if (held && canCapture)
            { released = true; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else if (released)
            {
                released = false;
                if (canCapture)
                {
                    // Keep the existing camera angles. Ignore only the relock/warp delta;
                    // subsequent relative mouse movement continues from this view.
                    resumedFrame = Time.frameCount;
                    Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                }
            }
        }
        public void Reset() { released = false; resumedFrame = -1; }
    }
}
