
namespace SimpleU.NetworkChainedStateMachine
{
    public class BaseIdleState : AState
    {
        public override void ForwardEnter()
        {
            base.ForwardEnter();
            condition.SetAsDefault(this);
        }
    }
}
