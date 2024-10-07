
namespace SimpleU.NetworkChainedStateMachine
{
    public class BaseIdleState : AState
    {
        internal override void ForwardEnter()
        {
            base.ForwardEnter();
            condition.SetAsDefault(this);
        }
    }
}
