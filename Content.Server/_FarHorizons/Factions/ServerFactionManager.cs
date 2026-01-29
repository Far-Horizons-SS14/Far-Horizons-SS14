using System.Linq;
using Content.Server.GameTicking;
using Content.Shared._FarHorizons.CCVar;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Factions;

public sealed partial class ServerFactionManager : SharedFactionManager, IServerFactionManager
{
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private HashSet<ProtoId<FactionPrototype>> _enabledFactions = new();

    public new void Init()
    {
        base.Init();
        _netManager.RegisterNetMessage<MsgFactionSelected>();
        _netManager.Connected += Connected; // Whenever a new client is connected - we're sending them current faction
        _sawmill = _logManager.GetSawmill("factions");
    }

    public void PostInit() => _cfg.OnValueChanged(FHCCVars.EnabledFactions, UpdateEnabled, true);

    private void UpdateEnabled(string factions)
    {
        foreach (var id in factions.Split(','))
        {
            if (_prototypeManager.TryIndex(id, out FactionPrototype? prototype))
                _enabledFactions.Add(prototype);
            else
                _sawmill.Fatal($"Faction prototype {id} does not exist.");
        }
    }

    public override void Shutdown(){
        _netManager.Connected -= Connected;
        base.Shutdown();
    }

    public ProtoId<FactionPrototype>? DecideFactionForJob(ProtoId<JobPrototype> job){
        if (_currentFaction is null)
            return null;

        var possibleFactions = ListSpawnableFactionIDs().ToHashSet();
        return ListFactionJobs().Where(p => possibleFactions.Contains(p.Faction) && p.Job == job).Select(p => p.Faction).FirstOrNull();
    }

    public FactionPrototype MustHaveCurrentFaction(){
        if (GetCurrentFaction() != null)
            return GetCurrentFaction()!;
        
        if (!CanSetFaction())
            throw new InvalidOperationException("MustGetCurrentFaction() called without faction selected and while locked");

        SetCurrentFaction(GetDefaultFaction());
        return GetCurrentFaction()!;
    }

    private bool CanSetFaction() => _entitySystem.GetEntitySystem<GameTicker>().RunLevel == GameRunLevel.PreRoundLobby;


    public bool SetCurrentFaction(ProtoId<FactionPrototype>? faction){
        if (!CanSetFaction())
            return false;
        _currentFaction = faction;
        SyncCurrentFaction();
        CallOnFactionUpdated();
        return true;
    }

    private void Connected(object? sender, NetChannelArgs args) => SyncCurrentFaction(args.Channel);

    private void SyncCurrentFaction(INetChannel? target = null){
        var msg = new MsgFactionSelected
        {
            Faction = _currentFaction
        };

        if (target == null)
            _netManager.ServerSendToAll(msg);
        else
            _netManager.ServerSendMessage(msg, target);
    }
    public IEnumerable<FactionPrototype> ListEnabledFactions() =>
        ListPlayableFactions().Where(p => p.Major && (_enabledFactions.Contains(p) || p == _currentFaction));
}