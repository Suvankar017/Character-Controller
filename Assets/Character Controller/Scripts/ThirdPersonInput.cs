using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonCharacter
{
    public class ThirdPersonInput : MonoBehaviour
    {

        public Vector2 Look = Vector2.zero;
        public Vector2 Move = Vector2.zero;
        public bool IsJump = false;
        public bool IsSprint = false;
        public bool IsAttack = false;

        private void OnLook(InputValue value)
        {
            Look = value.Get<Vector2>();
        }

        private void OnMove(InputValue value)
        {
            Move = value.Get<Vector2>();
        }

        private void OnJump(InputValue value)
        {
            IsJump = value.isPressed;
        }

        private void OnSprint(InputValue value)
        {
            IsSprint = value.isPressed;
        }

        private void OnAttack(InputValue value)
        {
            IsAttack = value.isPressed;
        }

        private void OnExit(InputValue value)
        {
            Application.Quit();
        }

    }
}
