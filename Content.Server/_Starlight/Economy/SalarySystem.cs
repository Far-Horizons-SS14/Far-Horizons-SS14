using System.Linq;
using Content.Server._FarHorizons.Banking;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Events;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Starlight.Economy;
public sealed partial class SalarySystem : SharedSalarySystem
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerRolesManager _playerRolesManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _time = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private RoleSystem _roles = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private ISharedFactionManager _factions = default!; // Far Horizons
    [Dependency] private BankingSystem _banking = default!; // Far Horizons

    private float _delayAccumulator = 0f;
    private readonly Stopwatch _stopwatch = new();
    private readonly Dictionary<ICommonSession, TimeSpan> _lastSalary = [];
    private SalariesPrototype? _salaries;

    private const string Standart = "standart";

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(ev => _lastSalary.Clear());
        _salaries = _prototypes.Index<SalariesPrototype>(Standart);
        base.Initialize();
    }
    public override void Update(float frameTime)
    {
        if (_salaries == null) return;

        _delayAccumulator += frameTime;
        if (_delayAccumulator > 2)
        {
            _delayAccumulator = 0;
            _stopwatch.Restart();

            var query = _playerRolesManager.Players.GetEnumerator();
            while (query.MoveNext() && _stopwatch.Elapsed < TimeSpan.FromMilliseconds(0.1))
            {
                if (!_lastSalary.TryGetValue(query.Current.Session, out var lastTime))
                {
                    _lastSalary.Add(query.Current.Session, _time.CurTime);
                    continue;
                }
                if (!_entityManager.TryGetComponent<MobStateComponent>(query.Current.Session.AttachedEntity, out var state) 
                    || state.CurrentState == MobState.Critical 
                    || state.CurrentState == MobState.ActiveCritical // Far Horizons
                    || state.CurrentState == MobState.Dead)
                    continue;
                if (_time.CurTime - lastTime > TimeSpan.FromMinutes(15)
                    && _mind.TryGetMind(query.Current.Session.UserId, out var mind))
                {
                    // Far Horizons start
                    var senderProto = _roles.MindGetFaction(mind.Value.Owner) ?? _factions.GetCurrentFaction() ?? _factions.GetDefaultFaction();
                    var sender = _prototypes.Index(senderProto).Name;
                    // Far Horizons end

                    var roles = _roles.MindGetAllRoleInfo((mind.Value.Owner, mind.Value.Comp));
                    foreach (var role in roles)
                    {
                        if (_salaries.Jobs.TryGetValue(role.Prototype, out var salary))
                        {
                            var amount = CalculateSalaryWithBonuses(salary, query.Current.Session);

                            _banking.ChangeBalance(query.Current.Session.AttachedEntity.Value, amount); // Far Horizons
                            var message = Loc.GetString("economy-chat-salary-message", ("amount", amount), ("sender", sender)); // Far Horizons
                            var wrappedMessage = Loc.GetString("economy-chat-salary-wrapped-message", ("amount", amount), ("sender", sender), ("senderColor", "#2384CE")); // Far Horizons
                            _chat.ChatMessageToOne(ChatChannel.Notifications, message, wrappedMessage, default, false, query.Current.Session.Channel, Color.FromHex("#57A3F7"));
                        }
                    }

                    _lastSalary[query.Current.Session] = _time.CurTime;
                }
            }
        }
    }

    private int CalculateSalaryWithBonuses(int baseSalary, ICommonSession session)
    {
        var bonusMultiplier = 1.0;

        return (int)Math.Ceiling(baseSalary * bonusMultiplier);
    }

    internal void Donate(ICommonSession session, int amount)
    {
        var playerData = _playerRolesManager.GetPlayerData(session);
        if (playerData == null)
            return;

        playerData.Balance += amount;
        if (session.AttachedEntity != null)
            _banking.ChangeBalance(session.AttachedEntity.Value, amount); // Far Horizons

        // We need to make a prototype
        var i = _random.Next(0, 20);
        var message = Loc.GetString($"economy-chat-donate-{i}-message", ("amount", amount));
        var wrappedMessage = Loc.GetString($"economy-chat-donate-{i}-wrapped-message", ("amount", amount));
        _chat.ChatMessageToOne(ChatChannel.Notifications, message, wrappedMessage, default, false, session.Channel, Color.FromHex("#57A3F7"));
    }
}