using Content.Shared._CD.Records;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void OnPhysicalDescChanged(TextEdit.TextEditEventArgs args)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithPhysicalDesc(Rope.Collapse(args.TextRope).Trim());
        IsDirty = true;
    }

    private void OnPersonalityDescChanged(TextEdit.TextEditEventArgs args)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithPersonalityDesc(Rope.Collapse(args.TextRope).Trim());
        IsDirty = true;
    }

    private void OnExploitablesChanged(TextEdit.TextEditEventArgs args)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithExploitable(Rope.Collapse(args.TextRope).Trim());
        IsDirty = true;
    }

    private void OnSecretsChanged(TextEdit.TextEditEventArgs args)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithSecrets(Rope.Collapse(args.TextRope).Trim());
        IsDirty = true;
    }

    private void OnPersonalNotesChanged(TextEdit.TextEditEventArgs args)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithPersonalNotes(Rope.Collapse(args.TextRope).Trim());
        IsDirty = true;
    }
    private void OnOOCNotesChanged(TextEdit.TextEditEventArgs args)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithOOCNotes(Rope.Collapse(args.TextRope).Trim());
        IsDirty = true;
    }

    private void UpdateProfileRecords(PlayerProvidedCharacterRecords records)
    {
        if (Profile is null)
            return;

        // Persist the record edits on the working profile so they will be saved later.
        // Store the freshly edited records back onto the profile blob.
        Profile = Profile.WithCDCharacterRecords(records);
        SetDirty();
    }

    private void UpdateCharacterInfoEditorText()
    {
        if (!_allowFlavorText)
            return;
        ICInfoEditor.PhysicalDescInput.TextRope = new Rope.Leaf(Profile?.PhysicalDescription ?? "");
        ICInfoEditor.PersonalityDescInput.TextRope = new Rope.Leaf(Profile?.PersonalityDescription ?? "");
        ICInfoEditor.ExploitableInput.TextRope = new Rope.Leaf(Profile?.ExploitableInfo ?? "");
        ICInfoEditor.SecretsInput.TextRope = new Rope.Leaf(Profile?.Secrets ?? "");

        OOCInfoEditor.PersonalNotesInput.TextRope = new Rope.Leaf(Profile?.PersonalNotes ?? "");
        OOCInfoEditor.OOCNotesInput.TextRope = new Rope.Leaf(Profile?.OOCNotes ?? "");
    }
}