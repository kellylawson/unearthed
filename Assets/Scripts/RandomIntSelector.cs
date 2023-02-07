using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomIntSelector : StateMachineBehaviour
{
    [SerializeField] string paramName;
    [SerializeField] int stateCount;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) {
        animator.SetInteger(paramName, Random.Range(0, stateCount));
    }
}
