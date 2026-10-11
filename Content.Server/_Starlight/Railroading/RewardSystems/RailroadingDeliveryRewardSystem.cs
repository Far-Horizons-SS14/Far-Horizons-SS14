using Content.Server.Chat.Managers;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Chat;
using Content.Shared.Clothing;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.StationRecords;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared._Starlight.Railroading;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingDeliveryRewardSystem : EntitySystem
{
    [Dependency] private FingerprintReaderSystem _fingerprintReader = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private LabelSystem _label = default!;
    [Dependency] private LoadoutSystem _loadout = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RailroadDeliveryRewardComponent, RailroadingCardCompletedEvent>(OnCompleted);
    }

    private void OnCompleted(Entity<RailroadDeliveryRewardComponent> ent, ref RailroadingCardCompletedEvent args)
    {
        if (_station.GetStationInMap(Transform(args.Subject).MapID) is not { } station)
            return;

        EntityUid? spawner = null;

        var spawners = EntityQueryEnumerator<DeliverySpawnerComponent>();
        while (spawners.MoveNext(out var spawnerUid, out var spawnerComp))
        {
            var spawnerStation = _station.GetOwningStation(spawnerUid);

            if (spawnerStation != station)
                continue;

            if (!_power.IsPowered(spawnerUid))
                continue;

            if (spawnerComp.ContainedDeliveryAmount >= spawnerComp.MaxContainedDeliveryAmount)
                continue;

            spawner = spawnerUid;
            break;
        }

        if (spawner == null)
            return;

        var delivery = Spawn(ent.Comp.Delivery, Transform(spawner.Value).Coordinates);
        var profile = _loadout.GetProfile(args.Subject);
        var recordID = _records.GetRecordByName(station, MetaData(args.Subject).EntityName);

        if (ent.Comp.Dataset != null && _playerManager.TryGetSessionByEntity(args.Subject, out var session))
        {
            var dataset = _protoMan.Index(ent.Comp.Dataset);
            var pick = _random.Pick(dataset.Values);
            if (ent.Comp.WrappedDataset != null)
            {
                var wrappedDataset = _protoMan.Index(ent.Comp.Dataset);
                _chat.ChatMessageToOne(ChatChannel.Notifications, Loc.GetString(pick), Loc.GetString(wrappedDataset.Values[dataset.Values.IndexOf(pick)]), default, false, session.Channel, Color.FromHex("#57A3F7"));
            }
        }

        if (!TryComp<DeliveryComponent>(delivery, out var deliveryComp)
            || recordID == null
            || !_records.TryGetRecord<GeneralStationRecord>(_records.Convert((GetNetEntity(station), recordID.Value)), out var entry))
            return;


        deliveryComp.RecipientName = entry.Name;
        deliveryComp.RecipientJobTitle = entry.JobTitle;
        deliveryComp.RecipientStation = station;

        _appearance.SetData(delivery, DeliveryVisuals.JobIcon, entry.JobIcon);

        _label.Label(delivery, deliveryComp.RecipientName);

        if (TryComp<FingerprintReaderComponent>(delivery, out var reader) && entry.Fingerprint != null)
            _fingerprintReader.AddAllowedFingerprint((delivery, reader), entry.Fingerprint);

        Dirty(delivery, deliveryComp);
    }
}
