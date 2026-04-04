using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.Disease.UI;

[Serializable, NetSerializable]
public enum VaccinatorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CreateVaccineMessage() : BoundUserInterfaceMessage;