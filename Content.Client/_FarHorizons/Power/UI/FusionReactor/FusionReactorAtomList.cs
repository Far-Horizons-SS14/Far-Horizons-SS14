using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._FarHorizons.Fusion;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._FarHorizons.Power.UI.FusionReactor;

public sealed class FusionAtomList : ScrollContainer
{
    private readonly BoxContainer _list = new()
    {
        Orientation = BoxContainer.LayoutOrientation.Vertical,
        VerticalExpand = true,
        HorizontalExpand = true,
        Margin = new(2, 2, 2, 2),
    };

    private readonly Dictionary<FusionAtom, FusionAtomEntry> _entries = [];

    public string DisplayUnit = "";

    public FusionAtomList() => Initialize();

    public FusionAtomList(Dictionary<FusionAtom, double> entries)
    {
        foreach (var (atom, mol) in entries)
        {
            _entries.Add(atom, new(atom, mol) { Unit = DisplayUnit });
        }
        Initialize();
    }

    public FusionAtomList(List<(FusionAtom, double)> entries)
    {
        foreach (var (atom, mol) in entries)
        {
            _entries.Add(atom, new(atom, mol) { Unit = DisplayUnit });
        }
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

    public void Update(FusionAtom atom, double mol)
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

        var excess = _entries.ExceptBy(entries.Select(n => n.Key), e => e.Key).ToList();
        foreach (var (atom, mol) in excess)
        {
            Remove(atom);
        }
    }

    public void Update(List<(FusionAtom, double)> entries)
    {
        foreach (var (atom, mol) in entries)
        {
            Update(atom, mol);
        }

        var excess = _entries.ExceptBy(entries.Select(n => n.Item1), e => e.Key).ToList();
        foreach (var (atom, mol) in excess)
        {
            Remove(atom);
        }
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

    public void Clear()
    {
        _entries.Clear();
        _list.RemoveAllChildren();
    }
}

[Virtual]
public class FusionAtomEntry : BoxContainer
{
    public FusionAtom Atom { get; private set; }
    public double Mols { get; private set; }
    public string Unit = "";

    protected Label _atomLabel;
    protected Label _molLabel;

    public FusionAtomEntry(FusionAtom atom, double mols)
    {
        Initialize();
        Atom = atom;
        Mols = mols;
        Update();
    }

    [MemberNotNull(nameof(_atomLabel)), MemberNotNull(nameof(_molLabel))]
    private void Initialize()
    {
        Orientation = LayoutOrientation.Horizontal;
        _atomLabel = new()
        {

        };
        _molLabel = new()
        {
            HorizontalAlignment = HAlignment.Right,
            HorizontalExpand = true,
        };

        AddChild(_atomLabel);
        AddChild(_molLabel);
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

    private static string SIFormat(double value, string unit = "")
    {
        string[] Prefixes = ["y", "z", "a", "f", "p", "n", "u", "m", "", "k", "M", "G", "T"];
        const int MinExponentPower = -24;
        const int MaxExponentPower = 12;

        if (value == 0)
            return $"0 {unit}";

        var exponent = Math.Floor(Math.Log10(Math.Abs(value)));
        var magnitude = (int)Math.Clamp(Math.Floor(exponent / 3) * 3, MinExponentPower, MaxExponentPower);

        var scaled = value * Math.Pow(10, -magnitude);

        var prefix = Prefixes[(magnitude - MinExponentPower) / 3];

        return $"{scaled:0.#}\t{prefix}{unit}";
    }
}

