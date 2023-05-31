using System.Collections;
using System.Collections.Generic;
using ThirdPersonCharacter;
using UnityEngine;

public class AnimatorEventHandler : MonoBehaviour
{
    public ThirdPersonController controller;

    private void RegisterNextAttack()
    {
        controller.CanPerformNextAttack = true;
    }

    public void ResetAttackState()
    {
        controller.ResetAttacking();
    }
}
