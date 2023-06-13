using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlternatingInt : StateMachineBehaviour
{
    [SerializeField] string paramName;
    [SerializeField] int maxAttacks;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        int currentValue = animator.GetInteger(paramName);
        currentValue++;
        Debug.Log($"Attack value: {currentValue}");
        animator.SetInteger(paramName, currentValue > maxAttacks ? 1 : currentValue);
    }
}
