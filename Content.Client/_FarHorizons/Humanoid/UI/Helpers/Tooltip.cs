using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client._FarHorizons.Humanoid.UI.Helpers;

public sealed class Tooltip(Dictionary<string, TooltipContent> dataCollection, FontResource font)
{
    private string? _currentShown;
    private Vector2 _mousePosition;
    private bool _side;

    private List<Vector2>? _textSizes;

    public Color BoxColor = Color.Black;
    public Color BorderColor = Color.White;
    public Color HighlightBorderColor = Color.Orange;

    public static int TitleFontSize = 14;
    public static int SubTitleFontSize = 10;
    public static int BottomFontSize = 11;
    private readonly Font _titleFont = new VectorFont(font, TitleFontSize);
    private readonly Font _subTitleFont = new VectorFont(font, SubTitleFontSize);
    private readonly Font _bottomFont = new VectorFont(font, BottomFontSize);

    public Color TitleColor = Color.White;
    public Color SubTitleColor = Color.Gray;
    public Color BottomColor = Color.LightGray;

    public void WithCurrentShown(string? shown)
    {
        if (shown != null && !dataCollection.ContainsKey(shown))
            return;

        if (_currentShown == shown)
            return;
        
        _textSizes = null;
        _currentShown = shown;
    }

    public void WithMousePosition(Vector2 mousePos) =>
        _mousePosition = mousePos;

    public void WithSide(bool side) =>
        _side = side;

    public float BorderWidth = 2;
    public float Offset = 10;
    public Vector2 TextMargin = new(10, 10);

    public Vector2 Size =>
        _textSizes == null
            ? Vector2.One + (TextMargin * 2)
            : new Vector2(_textSizes.Select(p => p.X).Max(), _textSizes.Sum(p => p.Y)) + (TextMargin * 2);

    public Vector2 BoxAnchor => _mousePosition;

    public UIBox2 Box => new(
        BoxAnchor + new Vector2(_side ? Offset : -Offset - Size.X, Offset),
        BoxAnchor + new Vector2(_side ? Offset + Size.X : -Offset, Offset + Size.Y)
    );

    public UIBox2 InnerBox =>
        new(Box.TopLeft + (Vector2.One * BorderWidth), Box.BottomRight - (Vector2.One * BorderWidth));

    private void PopulateText(DrawingHandleScreen handle, TooltipContent data)
    {
        _textSizes = new List<Vector2>();
        _textSizes.Add(handle.GetDimensions(_titleFont, data.TitleText, 1));
        _textSizes.Add(handle.GetDimensions(_subTitleFont, data.SubTitleText, 1));

        foreach (var line in data.BottomText)
            _textSizes.Add(handle.GetDimensions(_bottomFont, line, 1));
    }

    private void DrawText(DrawingHandleScreen handle, TooltipContent data)
    {
        if (_textSizes == null) return;

        handle.DrawString(_titleFont, InnerBox.TopLeft + TextMargin, data.TitleText, TitleColor);
        handle.DrawString(_subTitleFont, InnerBox.TopLeft + _textSizes[0] with { X = 0 } + TextMargin, data.SubTitleText, SubTitleColor);

        var rollingHeight = _textSizes[0].Y + _textSizes[1].Y;
        for (var i = 0; i < data.BottomText.Count; i++)
        {
            var lineText = data.BottomText[i];
            handle.DrawString(_bottomFont, InnerBox.TopLeft + new Vector2(0, rollingHeight) + TextMargin, lineText, BottomColor);
            rollingHeight += _textSizes[2 + i].Y;
        }
    }

    public void Draw(DrawingHandleScreen handle, float uiScale)
    {
        if (_currentShown == null || !dataCollection.TryGetValue(_currentShown, out var data))
            return;
        
        if (_textSizes == null)
            PopulateText(handle, data);

        handle.DrawRect(Box, data.Highlight ? HighlightBorderColor : BorderColor);
        handle.DrawRect(InnerBox, BoxColor);
        DrawText(handle, data);
    }
}

public record struct TooltipContent(string TitleText, string SubTitleText, List<string> BottomText, bool Highlight = false);