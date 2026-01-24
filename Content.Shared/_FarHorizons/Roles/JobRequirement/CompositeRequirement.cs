using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;


namespace Content.Shared._FarHorizons.Roles.JobRequirement;


[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class CompositeRequirement : Shared.Roles.JobRequirement
{
    [DataField(required: true)]
    public List<Shared.Roles.JobRequirement> Requirements;

    // It is ((ALL Requirements) OR (ALL AlternativeRequirements))
    [DataField(required: true)]
    public List<Shared.Roles.JobRequirement> AlternativeRequirements;

    public override bool Check(IEntityManager entManager,
        ICommonSession? player,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan>? playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();
        bool requirementsOutput = true;
        bool altRequirementsOutput = true;
        
        foreach (var requirement in Requirements)
        {
            if (requirement.Check(entManager, player, protoManager, profile, playTimes, out var reasonOutput)) 
                continue;
            requirementsOutput = false;
            reason.AddMessage(reasonOutput);
            reason.PushNewline();
        }
        foreach (var alternativeRequirement in AlternativeRequirements)
        {
            if (alternativeRequirement.Check(entManager, player, protoManager, profile, playTimes,
                    out var reasonOutput)) continue;
            if (!requirementsOutput)
            {
                reason.AddText("OR");
                reason.PushNewline();
            }
            altRequirementsOutput = false;
            reason.AddMessage(reasonOutput);
            reason.PushNewline();
        }

        return requirementsOutput || altRequirementsOutput;
    }
}
