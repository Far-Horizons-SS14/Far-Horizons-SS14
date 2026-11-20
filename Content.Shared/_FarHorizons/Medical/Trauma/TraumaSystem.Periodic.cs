using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._FarHorizons.Medical.Trauma;

public abstract partial class TraumaSystem
{
    private void UpdateBleedCap()
    {
        var query = EntityQueryEnumerator<SurgeryLimbStatusMinCapBleedComponent, BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var bleed, out var bloodstream))
        {
            if (bleed.NextUpdate > _timing.CurTime ||
                bloodstream.BleedAmount >= bleed.MinBleedCap ||
                _mobState.IsIncapacitated(uid, allowActiveCrit: true))
                continue;
            
            bleed.NextUpdate = _timing.CurTime + bleed.RefreshRate;
            _bloodstream.TryModifyBleedAmount((uid, bloodstream), bleed.MinBleedCap - bloodstream.BleedAmount);
        }
    }

    private void UpdateDamageCap()
    {
        var query = EntityQueryEnumerator<SurgeryLimbStatusMinDamageCapComponent, DamageableComponent>();

        while (query.MoveNext(out var uid, out var cap, out var damageable))
        {
            if (cap.NextUpdate > _timing.CurTime ||
                _mobState.IsIncapacitated(uid, allowActiveCrit: true))
                continue;

            DamageSpecifier? delta = null;
            var allCurrent = _damage.GetPositiveDamage((uid, damageable));

            foreach (var (type, min) in cap.MinDamageCap.DamageDict)
            {
                if (min <= FixedPoint2.Zero)
                    continue;

                allCurrent.DamageDict.TryGetValue(type, out var current);

                if (current >= min)
                    continue;

                var step = FixedPoint2.Min(min - current, cap.DamagePerTick);

                delta ??= new DamageSpecifier();
                delta.DamageDict[type] = step;
            }

            if (delta == null)
                continue;

            cap.NextUpdate = _timing.CurTime + cap.RefreshRate;

            _damage.TryChangeDamage((uid, damageable), delta, true, false);
        }
    }
}