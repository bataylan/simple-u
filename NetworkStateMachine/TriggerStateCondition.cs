namespace SimpleU.NetworkChainedStateMachine
{
    public class TriggerStateCondition : StateCondition
    {
        protected override void TriggerOnValueChanged(bool previousValue, bool newValue)
        {
            if (!newValue)
                return;

            base.TriggerOnValueChanged(previousValue, newValue);
            ChangeValue(false);
        }
    }
}