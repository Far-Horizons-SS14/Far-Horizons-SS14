namespace Content.Shared.EntityEffects.Effects;

public sealed partial class ModifyBatteryLevel : EntityEffectBase<ModifyBatteryLevel>
{
    [DataField] public float amount = 1.0f;
}