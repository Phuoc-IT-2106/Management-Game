using Godot;
using System.Runtime.InteropServices;

public partial class Main : Control
{
    private Label _status = null!;
    public override async void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Contains("--ui"))
        {
            var ui = new DenseUi();
            AddChild(ui);
            return;
        }
        var panel = new VBoxContainer();
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(panel);
        panel.AddChild(new Label { Text = "QG-01 / synthetic managed interaction" });
        _status = new Label { Text = "Ready" };
        panel.AddChild(_status);
        var action = new Button { Text = "Write and read tiny local fixture" };
        action.Pressed += Persist;
        panel.AddChild(action);
        var quit = new Button { Text = "Exit" };
        quit.Pressed += () => GetTree().Quit();
        panel.AddChild(quit);
        action.GrabFocus();
        GD.Print($"QG01_RUNTIME {RuntimeInformation.FrameworkDescription}");
        GD.Print($"QG01_RUNTIME_DIRECTORY {RuntimeEnvironment.GetRuntimeDirectory()}");
        if (OS.GetCmdlineUserArgs().Contains("--smoke"))
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            action.EmitSignal(Button.SignalName.Pressed);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (OS.GetCmdlineUserArgs().Contains("--capture") && DisplayServer.GetName() != "headless")
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng("user://qg01-render.png");
            }
            GetTree().Quit(_status.Text == "Read-back verified" ? 0 : 1);
        }
    }

    private void Persist()
    {
        try
        {
            string path = ProjectSettings.GlobalizePath("user://qg01.txt");
            System.IO.File.WriteAllText(path, "managed-fixture-v1");
            if (System.IO.File.ReadAllText(path) != "managed-fixture-v1")
                throw new InvalidOperationException("Read-back mismatch");
            _status.Text = "Read-back verified";
            GD.Print("QG01_INTERACTION_PASS");
        }
        catch (Exception error)
        {
            _status.Text = error.Message;
            GD.PushError(error.ToString());
        }
    }
}
