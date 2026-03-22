namespace Content.Shared.Humanoid.Markings;

public partial record struct Marking
{
    public Marking WithGlowing(bool glowing) =>
        this with { IsGlowing = glowing };
}