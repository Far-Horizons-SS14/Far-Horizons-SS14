using Content.Shared._FarHorizons.UI.BackgroundTraits;

namespace Content.Shared._FarHorizons.Vampire.Traits;

public abstract partial class LesserVampireTraitComponent : BackgroundTraitComponent
{
    [DataField] public float PassiveDrain;
}

public abstract partial class LesserVampirePassiveTraitComponent : BackgroundPassiveTraitComponent
{
    [DataField] public float PassiveDrain;
}

public abstract partial class LesserVampireActionTraitComponent : BackgroundActionTraitComponent
{
    [DataField] public float PassiveDrain;
}

public abstract partial class LesserVampireToggleActionComponent : BackgroundToggleActionComponent
{
    [DataField] public float PassiveDrain;
    [DataField] public float DrainWhenToggled;
}