using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.LimbStatusEffect;

[ImplicitDataDefinitionForInheritors]
public partial interface ILimbStatusEffect
{
    [DataField] public ProtoId<OrganCategoryPrototype>? Limb { get; set; }

    public string? Id();

    public bool Eq(ILimbStatusEffect other);

    public ILimbStatusEffect Stack(ILimbStatusEffect other);

    public void Apply(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs);
    public void Remove(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs);
    public void Update(IEntityManager entMan, EntityUid target, ILimbStatusEffect old, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs);

    ILimbStatusEffect Clone();
    void Normalize();
}

public abstract partial class LimbStatusEffectBase<T> : ILimbStatusEffect where T : LimbStatusEffectBase<T>
{
    [DataField] public ProtoId<OrganCategoryPrototype>? Limb { get; set; }

    public abstract string? Id();

    protected abstract bool Eq(T other);
    protected abstract T Stack(T other);

    public abstract void Apply(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs);
    public abstract void Remove(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs);

    public virtual void Update(IEntityManager entMan, EntityUid target, ILimbStatusEffect old, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs)
    {
        old.Remove(entMan, target, organs);
        Apply(entMan, target, organs);
    }

    bool ILimbStatusEffect.Eq(ILimbStatusEffect other)
        => other is T t && Eq(t);

    ILimbStatusEffect ILimbStatusEffect.Stack(ILimbStatusEffect other)
    {
        if (other is not T t)
            throw new InvalidOperationException($"Cannot stack {GetType()} with {other.GetType()}");

        var result = Stack(t);
        return result;
    }

    // You have to overwrite this if your effect uses lists, otherwise weirdness happens
    public virtual ILimbStatusEffect Clone() => (ILimbStatusEffect) MemberwiseClone();

    public virtual void Normalize() { }

    // Helpers

    private const string TORSO = "Torso";

    protected bool TryGetTarget(EntityUid body, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs, out EntityUid limb)
    {
        limb = default;

        if (Limb is not { } id)
            return false;

        if (id == TORSO)
            limb = body;
        else if (!organs.TryGetValue(id, out limb))
            return false;

        return true;
    }
}

public abstract partial class LimbStatusEffectGlobal<T> : LimbStatusEffectBase<T> where T : LimbStatusEffectGlobal<T>
{
    public sealed override string? Id() => GetType().Name;
}

public abstract partial class LimbStatusEffectLocal<T> : LimbStatusEffectBase<T> where T : LimbStatusEffectLocal<T>
{
    public sealed override string? Id() => $"{GetType().Name}:{Limb}";
}
