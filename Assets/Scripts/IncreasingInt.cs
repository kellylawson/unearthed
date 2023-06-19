using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncreasingInt : StateMachineBehaviour
{
    [SerializeField] string paramName;
    [SerializeField] int maxIndex;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        int currentValue = animator.GetInteger(paramName);
        currentValue++;
        Debug.Log($"Attack value: {currentValue}");
        animator.SetInteger(paramName, currentValue > maxIndex ? 1 : currentValue);
    }
}
