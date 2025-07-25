using UnityEngine;

// This is a StateMachineBehaviour script. You can add it to an animation state in the Animator.
// It will run code when the Animator enters or exits that state.
public class ResetComboBehaviour : StateMachineBehaviour
{
    private readonly int comboStepHash = Animator.StringToHash("comboStep");

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
       // When we enter a state that has this script (like our Idle state),
       // we reset the comboStep integer back to 0.
       animator.SetInteger(comboStepHash, 0);
       Debug.Log("Combo has been reset to 0.");
    }
}
