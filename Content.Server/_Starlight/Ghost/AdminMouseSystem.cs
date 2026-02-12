using Content.Server.Administration.Managers;
using Content.Server.Polymorph.Systems;
///Far Horizons-Start
using Content.Server._FarHorizons.DiscordLink;
using Content.Shared._FarHorizons.DiscordLink;
///Far Horizons-End
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._Starlight.Ghost;

public sealed class AdminMouseSystem : EntitySystem
{
    [Dependency] private readonly IDiscordLinkManager _playerRoles = default!; /// Far Horizons
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostComponent, GetVerbsEvent<Verb>>(OnGetInteractionVerbs);
    }

    private void OnGetInteractionVerbs(EntityUid uid, GhostComponent component, ref GetVerbsEvent<Verb> args)
    {
        var user = args.User;

        if (args.Target != user || !HasComp<GhostComponent>(user))
            return;

        if (_admin.IsAdmin(user, true))
        {
            var adminName = Loc.GetString("admin-verb-text-make-adminmouse-nanotrasen"); ///Far Horizons
            Verb admin = new()
            {
                Text = adminName,
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_FarHorizons/Mobs/Animals/mouse.rsi"), "mouse-admin"), ///Far Horizons
                Act = () =>
                {
                    _polymorphSystem.PolymorphEntity(user, "AdminMouse");
                },
                Impact = LogImpact.Extreme,
/// Far Horizons-Start - Making two separate admin mice
                Message = string.Join(": ", adminName, Loc.GetString("admin-verb-make-adminmouse-nanotrasen")),
            };
            args.Verbs.Add(admin);
        }

        if (_admin.IsAdmin(user, true))
        {
            var adminName = Loc.GetString("admin-verb-text-make-adminmouse-neosol");
            Verb admin = new()
            {
                Text = adminName,
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_FarHorizons/Mobs/Animals/mouse.rsi"), "mouse-adminsyndie"),
                Act = () =>
                {
                    _polymorphSystem.PolymorphEntity(user, "AdminMouseSyndi");
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", adminName, Loc.GetString("admin-verb-make-adminmouse-neosol")),
            };
            args.Verbs.Add(admin);
        }

        if (_playerRoles.HasPermission(user, AdditionalPermissionsTypes.Mentor))
/// Far Horizons-End
        {
            var mentorName = Loc.GetString("admin-verb-text-make-mentormouse");
            Verb mentor = new()
            {
                Text = mentorName,
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_FarHorizons/Mobs/Animals/mouse.rsi"), "mouse-mentor"), ///Far Horizons
                Act = () =>
                {
                    _polymorphSystem.PolymorphEntity(user, "MentorMouse");
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", mentorName, Loc.GetString("admin-verb-make-mentormouse")),
            };
            args.Verbs.Add(mentor);
        }
    }
}