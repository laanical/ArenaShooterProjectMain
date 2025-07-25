using UnityEngine;

// This is a StateMachineBehaviour script. You add it to animation states in the Animator.
// It will run code when the Animator enters that state.
public class ResetAttackBoolBehaviour : StateMachineBehaviour
{
    private readonly int isAttackingBoolHash = Animator.StringToHash("isAttacking");

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
       // As soon as we enter an attack state (AxeSwing1 or AxeSwing2),
       // we immediately set the 'isAttacking' boolean back to false.
       // This "consumes" the attack command and prevents it from re-triggering during blends.
       animator.SetBool(isAttackingBoolHash, false);
    }
}
