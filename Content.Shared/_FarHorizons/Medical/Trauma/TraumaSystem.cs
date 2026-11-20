using Content.Shared._FarHorizons.LimbDamage;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Medical.Trauma;

public abstract partial class TraumaSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private LimbDamageSystem _limbDamage = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    private const string TORSO = "Torso";

    public override void Update(float frameTime)
    {
        UpdateBleedCap();
        UpdateDamageCap();
    }
}