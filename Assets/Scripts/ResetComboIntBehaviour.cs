using UnityEngine;

// This StateMachineBehaviour resets the combo integer when the animator returns to a state it's attached to (e.g., Idle).
public class ResetComboIntBehaviour : StateMachineBehaviour
{
    private readonly int comboStepHash = Animator.StringToHash("comboStep");

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
       // When we enter the Idle state, reset the comboStep integer back to 0.
       // This signals that the combo has ended and the next attack should be the first one.
       animator.SetInteger(comboStepHash, 0);
       Debug.Log("Combo has been reset to 0.");
    }
}
