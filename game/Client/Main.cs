using Godot;
public partial class Main : Control
{
    public override void _Ready() => AddChild(new Label { Text = "Management Game — production skeleton" });
}
