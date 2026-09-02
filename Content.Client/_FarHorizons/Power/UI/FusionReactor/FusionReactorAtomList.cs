using System.Linq;
using Content.Shared._FarHorizons.Fusion;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._FarHorizons.Power.UI.FusionReactor;

[Virtual]
public class FusionAtomList : ScrollContainer
{
    protected readonly BoxContainer _list = new()
    {
        Orientation = BoxContainer.LayoutOrientation.Vertical,
        VerticalExpand = true,
        HorizontalExpand = true,
        Margin = new(2, 2, 2, 2),
    };

    protected readonly Dictionary<FusionAtom, FusionAtomEntry> _entries = [];

    public bool Strict = true;

    public string DisplayUnit = "";

    public FusionAtomList() => Initialize();

    public FusionAtomList(Dictionary<FusionAtom, double> entries)
    {
        Update(entries);
        Initialize();
    }

    public FusionAtomList(List<(FusionAtom, double)> entries)
    {
        Update(entries);
        Initialize();
    }

    private void Initialize()
    {
        HorizontalExpand = true;
        AddChild(_list);

        foreach (var (atom, entry) in _entries)
        {
            _list.AddChild(entry);
        }
    }

    public virtual void Update(FusionAtom atom, double mol)
    {
        if (_entries.TryGetValue(atom, out var entry))
        {
            entry.Update(mol);
            return;
        }

        _entries.Add(atom, new(atom, mol) { Unit = DisplayUnit });
        _list.AddChild(_entries[atom]);
    }

    public void Update(Dictionary<FusionAtom, double> entries)
    {
        foreach (var (atom, mol) in entries)
        {
            Update(atom, mol);
        }

        if (Strict)
            RemoveExcess(entries);
    }

    public void Update(List<(FusionAtom, double)> entries)
    {
        foreach (var (atom, mol) in entries)
        {
            Update(atom, mol);
        }

        if (Strict)
            RemoveExcess(entries);
    }

    public void Remove(FusionAtom atom)
    {
        if (!_entries.TryGetValue(atom, out var entry))
            return;

        _list.RemoveChild(entry);

        _entries.Remove(atom);
    }

    public void RemoveZero()
    {
        List<FusionAtom> remQ = [];
        foreach (var (atom, entry) in _entries)
        {
            if (entry.Mols <= 0)
                remQ.Add(atom);
        }

        foreach (var atom in remQ)
        {
            Remove(atom);
        }
    }

    public void RemoveExcess(Dictionary<FusionAtom, double> entries)
    {
        var excess = _entries.ExceptBy(entries.Select(n => n.Key), e => e.Key).ToList();
        foreach (var (atom, mol) in excess)
        {
            Remove(atom);
        }
    }

    public void RemoveExcess(List<(FusionAtom, double)> entries)
    {
        var excess = _entries.ExceptBy(entries.Select(n => n.Item1), e => e.Key).ToList();
        foreach (var (atom, mol) in excess)
        {
            Remove(atom);
        }
    }

    public void Clear()
    {
        _entries.Clear();
        _list.RemoveAllChildren();
    }

    public void Order()
    {
        _list.RemoveAllChildren();
        // Sorts by element, then matter/antimatter, then isotope
        foreach (var (atom, entry) in _entries.OrderBy(e => Math.Abs(e.Key.Proton)).ThenBy(e => -e.Key.Proton).ThenBy(e => e.Key.Neutron))
        {
            _list.AddChild(entry);
        }
    }

    public bool Contains(FusionAtom atom) => _entries.ContainsKey(atom);
}

[Virtual]
public class FusionAtomEntry : BoxContainer
{
    public FusionAtom Atom { get; protected set; }
    public double Mols { get; protected set; }
    public string Unit = "";

    protected readonly BoxContainer _dataBox;
    protected readonly Label _atomLabel;
    protected readonly Label _molLabel;

    public FusionAtomEntry(FusionAtom atom, double mols)
    {
        Orientation = LayoutOrientation.Vertical;
        _dataBox = new()
        {
            Orientation = LayoutOrientation.Horizontal,
        };
        _atomLabel = new()
        {

        };
        _molLabel = new()
        {
            HorizontalAlignment = HAlignment.Right,
            HorizontalExpand = true,
        };

        _dataBox.AddChild(_atomLabel);
        _dataBox.AddChild(_molLabel);
        AddChild(_dataBox);

        Atom = atom;
        Mols = mols;
        Update();
    }

    public void Update()
    {
        _atomLabel.Text = Atom.ToString();
        _molLabel.Text = SIFormat(Mols, Unit);
    }

    public void Update(double mols)
    {
        Mols = mols;
        Update();
    }

    protected static string SIFormat(double value, string unit = "")
    {
        const int MinExponentPower = -24;
        const int MaxExponentPower = 12;

        if (value == 0)
            return $"0 {unit}";

        var exponent = Math.Floor(Math.Log10(Math.Abs(value)));
        var magnitude = (int)Math.Clamp(Math.Floor(exponent / 3) * 3, MinExponentPower, MaxExponentPower);

        var scaled = value * Math.Pow(10, -magnitude);

        return $"{Loc.GetString("fusion-reactor-controller-ui-fmt-prefix", ("divided", scaled), ("places", (magnitude - MinExponentPower) / 3))}{unit}";
    }
}

public sealed class FusionInjectionList : FusionAtomList
{
    public Action<KeyValuePair<FusionAtom, FusionReactorTransferData>>? OnTransferSet;

    public override void Update(FusionAtom atom, double mol)
    {
        if (_entries.TryGetValue(atom, out var entry))
        {
            entry.Update(mol);
            return;
        }
        var newEntry = new FusionAtomInjectEntry(atom, mol) { Unit = DisplayUnit };
        newEntry.OnSetPressed += val => OnTransferSet?.Invoke(new(atom, val));
        _entries.Add(atom, newEntry);
        _list.AddChild(_entries[atom]);
    }

    public void UpdateSelectors(Dictionary<FusionAtom, FusionReactorTransferData> transferData)
    {
        foreach (var (atom, data) in transferData)
        {
            if (!_entries.TryGetValue(atom, out var entry) || entry is not FusionAtomInjectEntry injectEntry)
                continue;

            injectEntry.UpdateInject(data);
        }
    }
}

public sealed class FusionAtomInjectEntry : FusionAtomEntry
{
    private readonly BoxContainer _controlBox;
    private readonly OptionButton _injectMode;
    private readonly LineEdit _entryBox;
    private readonly Button _setButton;

    public Action<FusionReactorTransferData>? OnSetPressed;

    public FusionAtomInjectEntry(FusionAtom atom, double mols) : base(atom, mols)
    {
        _controlBox ??= new()
        {
            Orientation = LayoutOrientation.Horizontal,
        };
        _entryBox ??= new()
        {
            Text = "0",
            MinWidth = 100,
        };
        _setButton ??= new()
        {
            Text = Loc.GetString("fusion-reactor-controller-ui-set"),
            Disabled = true,
        };
        _injectMode ??= new()
        {
            HorizontalExpand = true,
        };

        _injectMode.Clear();
        foreach (var type in Enum.GetValues<FusionReactorTransferType>())
        {
            _injectMode.AddItem(Loc.GetString($"fusion-reactor-controller-ui-{type.ToString().ToLower()}"), (int)type);
        }

        _injectMode.OnItemSelected += obj =>
        {
            _injectMode.SelectId(obj.Id);
            _setButton.Disabled = false;
        };
        _entryBox.OnTextChanged += _ => _setButton.Disabled = false;
        _setButton.OnPressed += _ =>
        {
            if (float.TryParse(_entryBox.Text, out var num) && !float.IsNaN(num))
            {
                OnSetPressed?.Invoke(new((FusionReactorTransferType)_injectMode.SelectedId, num));
            }
            _setButton.Disabled = true;
        };

        _controlBox.AddChild(_injectMode);
        _controlBox.AddChild(_entryBox);
        _controlBox.AddChild(_setButton);
        AddChild(_controlBox);
    }

    public void UpdateInject(FusionReactorTransferData data)
    {
        if (!_setButton.Disabled)
            return;

        _injectMode.SelectId((int)data.transferType);
        _entryBox.Text = data.Quantity.ToString();
    }
}
