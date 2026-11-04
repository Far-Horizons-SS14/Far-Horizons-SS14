using Content.Client._FarHorizons.UserInterface.RichText; //FH
using Content.Client.UserInterface.RichText;
using Robust.Client.UserInterface.RichText;

namespace Content.Client.RichText;

/// <summary>
/// Contains rules for what markup tags are allowed to be used by players.
/// </summary>
public static class UserFormattableTags
{
    /// <summary>
    /// The basic set of "rich text" formatting tags that shouldn't cause any issues.
    /// Limit user rich text to these by default.
    /// </summary>
    public static readonly Type[] BaseAllowedTags =
    [
        typeof(BoldItalicTag),
        typeof(BoldTag),
        typeof(BulletTag),
        typeof(ColorTag),
        typeof(HeadingTag),
        typeof(ItalicTag),
        typeof(MonoTag),
        typeof(DateTimeTagHandler), // Starlight
        //Far Horizons-Start
        typeof(FormTagHandler),
        typeof(SignatureTagHandler),
        typeof(LogoTag),
        typeof(SyndieLogoTag),
        typeof(CCLogoTag),
        typeof(CFALogoTag),
        typeof(NSLogoTag),
        typeof(HCLogoTag),
        typeof(MELogoTag),
        typeof(NSGSLLogoTag),
        typeof(CDLogoTag),
        typeof(IPLogoTag),
        typeof(MRILogoTag),
        typeof(ACSLogoTag),
        typeof(NSCFALogoTag),
        typeof(CheckTagHandler)
        //Far Horizons-End
    ];

    /// <summary>
    /// Tags allowed in Silicon UIs. Extends from BaseAllowedTags.
    /// </summary>
    public static readonly Type[] SiliconAllowedTags =
    [
        ..BaseAllowedTags,
        typeof(ScrambleTag)
    ];
}
