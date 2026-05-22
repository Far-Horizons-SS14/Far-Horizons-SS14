using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Events;
using Content.Server.Mind;
using Content.Server.Roles;
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
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerRolesManager _playerRolesManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly MindSystem _mind = default!;

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

                    var roles = _roles.MindGetAllRoleInfo((mind.Value.Owner, mind.Value.Comp));
                    foreach (var role in roles)
                    {
                        if (_salaries.Jobs.TryGetValue(role.Prototype, out var salary))
                        {
                            var amount = CalculateSalaryWithBonuses(salary, query.Current.Session);

                            query.Current.Data.Balance += amount;
                            var message = Loc.GetString("economy-chat-salary-message", ("amount", amount), ("sender", "NanoTrasen"));
                            var wrappedMessage = Loc.GetString("economy-chat-salary-wrapped-message", ("amount", amount), ("sender", "NanoTrasen"), ("senderColor", "#2384CE"));
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

        // We need to make a prototype
        var i = _random.Next(0, 20);
        var message = Loc.GetString($"economy-chat-donate-{i}-message", ("amount", amount));
        var wrappedMessage = Loc.GetString($"economy-chat-donate-{i}-wrapped-message", ("amount", amount));
        _chat.ChatMessageToOne(ChatChannel.Notifications, message, wrappedMessage, default, false, session.Channel, Color.FromHex("#57A3F7"));
    }
}