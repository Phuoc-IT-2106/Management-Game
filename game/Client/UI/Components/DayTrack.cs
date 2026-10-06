using Godot;

namespace ManagementGame.UiKit;

/// <summary>Question, drawn day track, legend and full text equivalent. No focus, action or key.</summary>
public static class DayTrack
{
    public static VBoxContainer Create(UiContext ui, DayTrackData data)
    {
        data.Validate();
        var stack = ui.Stack(); stack.SetMeta("day_track", data.Question);
        stack.AddChild(SectionHeader.Create(ui, $"Day {data.FirstDay}–{data.LastDay}", data.Question));
        var canvas = new DayTrackCanvas(); canvas.Bind(ui, data); stack.AddChild(canvas);
        stack.AddChild(SemanticText.Create(ui, PresentationText.DayTrackLegend(data), TypographyRole.Annotation, ColorRole.TextSecondary));
        foreach (var line in PresentationText.DayTrackSummary(data))
            stack.AddChild(SemanticText.Create(ui, line, TypographyRole.Label, line.Contains(" over ", StringComparison.Ordinal) ? ColorRole.StateWarning : ColorRole.TextPrimary));
        stack.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, data.Assumption));
        return stack;
    }
}

/// <summary>Drawing only; every drawn fact is also present in the summary text.</summary>
public partial class DayTrackCanvas : Control
{
    private UiContext ui = null!;
    private DayTrackData data = null!;
    private Font font = null!;
    private int fontSize, gap, rowHeight, barHeight, labelWidth;
    private Color Tone(ColorRole role) => UiTheme.ToGodot(ui.Tokens.Color(role));

    public void Bind(UiContext context, DayTrackData value)
    {
        ui = context; data = value; MouseFilter = MouseFilterEnum.Ignore; FocusMode = FocusModeEnum.None;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        font = ui.Theme.DefaultFont; fontSize = ui.Tokens.FontSize(TypographyRole.Annotation);
        gap = ui.Tokens.Space(SpaceRole.SpaceInline);
        rowHeight = fontSize + 2 * gap; barHeight = 2 * ui.Tokens.ControlHeight;
        labelWidth = (int)Math.Ceiling(RowLabels().Max(x => font.GetStringSize(x, HorizontalAlignment.Left, -1, fontSize).X)) + ui.Tokens.Space(SpaceRole.SpaceRelated);
        CustomMinimumSize = new Vector2(0, barHeight + gap + rowHeight * (data.Rows.Length + 1));
        QueueRedraw();
    }
    private string LimitText => $"{data.LimitLabel} {data.Limit}";
    private IEnumerable<string> RowLabels() => data.Rows.Select(r => r.Label).Append("Load").Append("Day").Append(LimitText);

    public override void _Draw()
    {
        if (data is null) return;
        var cell = (Size.X - labelWidth) / data.Length;
        if (cell <= 0) return;
        float X(int day) => labelWidth + (day - data.FirstDay) * cell;
        var ascent = font.GetAscent(fontSize);
        void Text(string text, float x, float y, ColorRole role) => DrawString(font, new Vector2(x, y + ascent), text, HorizontalAlignment.Left, -1, fontSize, Tone(role));

        // Load bars against the limit.
        var scale = (float)Math.Max(data.Limit, Math.Max(data.Current.Max(), data.Proposed.IsEmpty ? 0 : data.Proposed.Max()));
        float Y(int value) => barHeight - value / scale * barHeight;
        var limitY = Y(data.Limit); var barWidth = Math.Max(UiTokens.BorderWidth, cell - gap);
        // Row label column carries the limit value beside its line, so no text overlaps the bars.
        var limitLabelY = Math.Clamp(limitY - fontSize / 2f, 0, barHeight - 2 * fontSize);
        Text(LimitText, 0, limitLabelY, ColorRole.TextPrimary);
        Text("Load", 0, Math.Max(limitLabelY + fontSize + gap, barHeight - fontSize), ColorRole.TextSecondary);
        for (var i = 0; i < data.Length; i++)
        {
            var x = X(data.FirstDay + i) + (cell - barWidth) / 2f;
            var current = data.Current[i];
            DrawRect(new Rect2(x, Y(current), barWidth, barHeight - Y(current)), Tone(ColorRole.Border));
            if (current > data.Limit) DrawRect(new Rect2(x, Y(current), barWidth, limitY - Y(current)), Tone(ColorRole.StateWarning));
            if (data.Proposed.IsEmpty || data.Proposed[i] <= current) continue;
            var proposed = data.Proposed[i];
            var top = Y(proposed);
            if (proposed > data.Limit) DrawRect(new Rect2(x, top, barWidth, Math.Min(limitY, Y(current)) - top), Tone(ColorRole.StateWarning));
            DrawRect(new Rect2(x, top, barWidth, Y(current) - top), Tone(ColorRole.TextPrimary), false, UiTokens.BorderWidth);
        }
        DrawLine(new Vector2(labelWidth, limitY), new Vector2(Size.X, limitY), Tone(ColorRole.TextPrimary), UiTokens.FocusWidth);

        // Marker rows, then the day axis.
        var y = barHeight + gap;
        foreach (var row in data.Rows)
        {
            Text(row.Label, 0, y + gap, ColorRole.TextSecondary);
            var size = Math.Min(cell - gap, rowHeight - 2 * gap);
            foreach (var day in row.Days.Where(d => d >= data.FirstDay && d <= data.LastDay))
            {
                var center = new Vector2(X(day) + cell / 2f, y + rowHeight / 2f);
                var box = new Rect2(center - new Vector2(size, size) / 2f, new Vector2(size, size));
                if (row.Shape == DayMarkShape.Diamond)
                    DrawColoredPolygon([center + new Vector2(0, -size / 2f), center + new Vector2(size / 2f, 0), center + new Vector2(0, size / 2f), center + new Vector2(-size / 2f, 0)], Tone(ColorRole.TextPrimary));
                else if (row.Shape == DayMarkShape.Filled) DrawRect(box, Tone(ColorRole.TextSecondary));
                else DrawRect(box, Tone(ColorRole.TextPrimary), false, UiTokens.FocusWidth);
            }
            y += rowHeight;
        }
        Text("Day", 0, y + gap, ColorRole.TextSecondary);
        var lastEnd = float.MinValue;
        foreach (var day in Enumerable.Range(data.FirstDay, data.Length).Where(d => d == data.FirstDay || d == data.LastDay || d % 7 == 0))
        {
            var label = day.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var width = font.GetStringSize(label, HorizontalAlignment.Left, -1, fontSize).X;
            var x = Math.Min(X(day) + (cell - width) / 2f, Size.X - width);
            if (x < lastEnd + gap) continue;
            Text(label, x, y + gap, ColorRole.TextSecondary); lastEnd = x + width;
        }
    }
}
