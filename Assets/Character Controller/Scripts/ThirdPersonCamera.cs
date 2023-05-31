using UnityEngine;

namespace ThirdPersonCharacter
{
    [RequireComponent(typeof(ThirdPersonInput))]
    public class ThirdPersonCamera : MonoBehaviour
    {

        public float CursorMoveThreshold = 0.01f;
        public float Sensitivity = 1.0f;
        public float TopClampedAngle = 70.0f;
        public float BottomClampedAngle = -30.0f;
        public Transform CameraFollowTransform = null;

        private Vector2 _CameraRotation = Vector2.zero;
        private ThirdPersonInput _Input = null;

        private void Awake()
        {
            _Input = GetComponent<ThirdPersonInput>();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void CameraRotation()
        {
            Vector2 look = _Input.Look;

            if (look.sqrMagnitude > CursorMoveThreshold)
            {
                _CameraRotation.x += look.y * Sensitivity;
                _CameraRotation.y += look.x * Sensitivity;
            }

            _CameraRotation.x = ClampAngle(_CameraRotation.x, BottomClampedAngle, TopClampedAngle);
            _CameraRotation.y = ClampAngle(_CameraRotation.y, float.MinValue, float.MaxValue);

            CameraFollowTransform.rotation = Quaternion.Euler(_CameraRotation.x, _CameraRotation.y, 0.0f);
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360.0f) angle += 360.0f;
            else if (angle > 360.0f) angle -= 360.0f;
            return Mathf.Clamp(angle, min, max);
        }

    }
}
