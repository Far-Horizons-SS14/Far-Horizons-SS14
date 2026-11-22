using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.Resources;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._FarHorizons.UserInterface.RichText;

public sealed partial class NSCFALogoTag : IMarkupTag
{
    [Dependency] private IEntitySystemManager _entitySystem = default!;
    private SpriteSystem? _spriteSystem;
    private IResourceCache? _resourceCache;

    public string Name => "nscfalogo";

    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        _spriteSystem ??= _entitySystem.GetEntitySystem<SpriteSystem>();
        _resourceCache ??= IoCManager.Resolve<IResourceCache>();

        var icon = new TextureRect
        {
            Texture = _resourceCache.GetTexture("/Textures/_FarHorizons/Logo/NSCFALogo.png"),
            TextureScale = new Vector2(0.5f, 0.5f),
        };

        control = icon;
        return true;
    }
}