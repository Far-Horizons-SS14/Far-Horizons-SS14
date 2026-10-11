using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._FarHorizons.Humanoid.UI.Helpers;

public sealed class ExpandableButton(
    IGameTiming timing,
    string id,
    Vector2 size,
    Vector2 offset,
    bool side = false
)
{
    public string Id = id;
    public Vector2 BasePosition = Vector2.Zero;
    public Vector2 Size = size;
    public Vector2 Offset = offset;
    public bool Hovered;
    public bool Expanded;
    public bool Side = side;
    public Vector2i InnerButtons = new(2, 2);

    public TimeSpan AnimSpeed = TimeSpan.FromSeconds(0.1);

    private TimeSpan _sizeAnimStart = TimeSpan.Zero;
    private TimeSpan _sizeAnimEnd = TimeSpan.Zero;
    private float _animProgress => Math.Clamp((float)(timing.CurTime - _sizeAnimStart).TotalSeconds / (float)(_sizeAnimEnd - _sizeAnimStart).TotalSeconds, 0f, 1f);

    private List<SubButtonElement> _subButtonData = new();

    private const int ButtonMargin = 2;
    public Color BackgroundColor = Color.Black;
    public Color ForegroundColor = Color.White;
    public Color SubButtonColor = Color.DimGray;
    public Color SubButtonHoveredColor = Color.LightGray;
    public Color SubButtonToggledColor = Color.DarkOrange;
    public Color SubButtonToggledHoveredColor = Color.Orange;

    private int? _hoveredId;
    private int? _toggledId;

    public Action<(string?, string?)>? SelectionChanged;
    public Action<string>? CloseRequest;

    public UIBox2 Box => 
        new(BasePosition + Offset, BasePosition + Offset + Size);

    public Vector2 ExpandedSize =>
        new(Size.X * InnerButtons.X, Size.Y * InnerButtons.Y);

    public UIBox2 ExpandedBox =>
        Side
            ? new(BasePosition + Offset, BasePosition + Offset + ExpandedSize)
            : new(BasePosition.X + Offset.X - ExpandedSize.X + Size.X, BasePosition.Y + Offset.Y, BasePosition.X + Offset.X + Size.X, BasePosition.Y + Offset.Y + ExpandedSize.Y);

    public Vector2 AnimatedSize => Vector2.Lerp(Size, ExpandedSize, _animProgress);

    public UIBox2 AnimatedBox =>
        Side
            ? new(BasePosition + Offset, BasePosition + Offset + AnimatedSize)
            : new(BasePosition + Offset + new Vector2(Size.X - AnimatedSize.X, 0),
                BasePosition + Offset + new Vector2(Size.X - AnimatedSize.X, 0) + AnimatedSize);

    public Vector2 ExpandedTotalMarginSize =>
        new(ButtonMargin * (InnerButtons.X + 1), ButtonMargin * (InnerButtons.Y + 1));

    public Vector2 AnimatedSubButtonSize =>
        new((int)((AnimatedSize.X - ExpandedTotalMarginSize.X) / InnerButtons.X), (int)((AnimatedSize.Y - ExpandedTotalMarginSize.Y) / InnerButtons.Y));

    public Vector2 ExpandedSubButtonSize =>
        new((int)((ExpandedSize.X - ExpandedTotalMarginSize.X) / InnerButtons.X), (int)((ExpandedSize.Y - ExpandedTotalMarginSize.Y) / InnerButtons.Y));
    
    public void WithBasePosition(Vector2 position) =>
        BasePosition = position;
    
    public void WithHovered(string? target) =>
        Hovered = Id == target;

    public void WithExpanded(string? target)
    {
        var oldExpanded = Expanded;
        Expanded = Id == target;

        if (oldExpanded || !Expanded) return;
        _sizeAnimStart = timing.CurTime;
        _sizeAnimEnd = _sizeAnimStart + AnimSpeed;
    }
        
    
    public bool IsHovering(Vector2 mousePos, float uiScale) =>
        mousePos.X > Box.Left * uiScale && mousePos.Y > Box.Top * uiScale && mousePos.X < Box.Right * uiScale && mousePos.Y < Box.Bottom * uiScale;

    public bool IsExpandedHovering() => 
        Expanded && _hoveredId != null;

    public void UpdateHovering(Vector2 mousePos, float uiScale)
    {
        if (!Expanded) return;

        _hoveredId = null;

        var index = 0;
        for (var x = 1; x <= InnerButtons.X; x++)
        for (var y = 1; y <= InnerButtons.Y; y++)
        {
            var buttonPosX = ExpandedBox.Left + (ButtonMargin * x) + (ExpandedSubButtonSize.X * (x - 1));
            var buttonPosY = ExpandedBox.Top + (ButtonMargin * y) + (ExpandedSubButtonSize.Y * (y - 1));

            if (mousePos.X > buttonPosX * uiScale &&
                mousePos.Y > buttonPosY * uiScale &&
                mousePos.X < (buttonPosX + ExpandedSubButtonSize.X) * uiScale &&
                mousePos.Y < (buttonPosY + ExpandedSubButtonSize.Y) * uiScale)
            {
                _hoveredId = index;
                return;
            }                

            index++;
        }
    }

    public void ProcessClick()
    {
        var toggledBefore = _toggledId;
        var selectionBefore = GetSelection();

        if (_toggledId == _hoveredId)
            _toggledId = null;
        else if (_hoveredId != null && _subButtonData.TryGetValue(_hoveredId.Value, out _))
            _toggledId = _hoveredId;
        else
            _toggledId = null;

        if (toggledBefore != _toggledId)
            SelectionChanged?.Invoke((selectionBefore, GetSelection()));
        
        CloseRequest?.Invoke(Id);
    }

    public string? GetSelection()
    {
        if (_toggledId == null || !_subButtonData.TryGetValue(_toggledId.Value, out var data))
            return null;

        return data.Id;
    }

    public string? GetHovered()
    {
        if (_hoveredId == null || !_subButtonData.TryGetValue(_hoveredId.Value, out var data))
            return null;

        return data.Id;
    }

    public void SetSelection(string? id)
    {
        if (id == null)
        {
            _toggledId = null;
            return;
        }

        if (_subButtonData.All(p => p.Id != id))
            return;

        for (var index = 0; index < _subButtonData.Count; index++)
            if (_subButtonData[index].Id == id)
            {
                _toggledId = index;
                return;
            }
    }

    // Basically hardcoded preset sizes
    private static (int, int) BestSize(int amount) =>
        amount switch
        {
            <= 2 => (2, 1),
            <= 4 => (2, 2),
            <= 6 => (3, 2),
            <= 8 => (4, 2),
            <= 12 => (4, 3),
            <= 15 => (5, 3),
            <= 20 => (5, 4),
            _ => (6, 5)
        };

    public void SetupSubButtons(List<SubButtonElement> elements)
    {
        var buttonBox = BestSize(elements.Count + 1);
        InnerButtons = new(buttonBox.Item1, buttonBox.Item2);
        _subButtonData = elements;
    }

    public void Draw(DrawingHandleScreen handle, float uiScale)
    {
        if (Expanded)
            DrawExpanded(handle, uiScale);
        else
            DrawNormal(handle, uiScale);
    }

    private void DrawNormal(DrawingHandleScreen handle, float uiScale)
    {
        handle.DrawRect(new(Box.TopLeft * uiScale, Box.BottomRight * uiScale), ForegroundColor);

        var buttonArea = new UIBox2((Box.TopLeft + (Vector2.One * ButtonMargin)) * uiScale,
            (Box.BottomRight - (Vector2.One * ButtonMargin)) * uiScale);

        if (!Hovered)
            handle.DrawRect(buttonArea, BackgroundColor);

        if (_toggledId == null || !_subButtonData.TryGetValue(_toggledId.Value, out var data))
            return;
        
        var xOffset = data.Offset?.X ?? 0;
        var yOffset = data.Offset?.Y ?? 0;

        var textureBox = new UIBox2((Box.Left + xOffset) * uiScale, (Box.Top + yOffset) * uiScale,
            (Box.Left + (ExpandedSubButtonSize.X * data.Scale) + xOffset) * uiScale,
            (Box.Top + (ExpandedSubButtonSize.Y * data.Scale) + yOffset) * uiScale);
        handle.DrawTextureRect(data.Texture, textureBox);
    }

    private void DrawExpanded(DrawingHandleScreen handle, float uiScale)
    {
        handle.DrawRect(new(AnimatedBox.TopLeft * uiScale, AnimatedBox.BottomRight * uiScale), ForegroundColor);
        handle.DrawRect(new((AnimatedBox.TopLeft + (Vector2.One * ButtonMargin)) * uiScale, (AnimatedBox.BottomRight - (Vector2.One * ButtonMargin)) * uiScale), BackgroundColor);
        
        var index = 0;
        for (var x = 1; x <= InnerButtons.X; x++)
        for (var y = 1; y <= InnerButtons.Y; y++)
        {
            var buttonPosX = AnimatedBox.Left + (ButtonMargin * x) + (AnimatedSubButtonSize.X * (x - 1));
            var buttonPosY = AnimatedBox.Top + (ButtonMargin * y) + (AnimatedSubButtonSize.Y * (y - 1));

            var subButtonBox = new UIBox2(buttonPosX * uiScale, buttonPosY * uiScale,
                (buttonPosX + AnimatedSubButtonSize.X) * uiScale, (buttonPosY + AnimatedSubButtonSize.Y) * uiScale);

            var buttonColor = SubButtonColor;
            if (_toggledId == index && _hoveredId == index)
                buttonColor = SubButtonToggledHoveredColor;
            else if (_toggledId == index)
                buttonColor = SubButtonToggledColor;
            else if (_hoveredId == index)
                buttonColor = SubButtonHoveredColor;
            

            handle.DrawRect(subButtonBox, buttonColor);

            if (_subButtonData.TryGetValue(index, out var data))
            {
                var xOffset = data.Offset?.X ?? 0;
                var yOffset = data.Offset?.Y ?? 0;

                var textureBox = new UIBox2((buttonPosX + xOffset) * uiScale, (buttonPosY + yOffset) * uiScale,
                    (buttonPosX + (ExpandedSubButtonSize.X * data.Scale) + xOffset) * uiScale,
                    (buttonPosY + (ExpandedSubButtonSize.Y * data.Scale) + yOffset) * uiScale);
                handle.DrawTextureRect(data.Texture, textureBox);
            }
                
            
            index++;
        }
    }
}

public record struct SubButtonElement(string Id, Texture Texture, float Scale = 1, Vector2? Offset = null);