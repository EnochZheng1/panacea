using Godot;

namespace Panacea;

/// <summary>
/// Development tool: when the game is run with <c>-- --shot path.png [--frames N]</c>, waits N frames
/// for the scene to settle, saves the viewport to a PNG, and quits. Used for headless visual review.
/// </summary>
public partial class Capture : Node
{
    private string? _path;
    private int _frames = 90;

    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--shot") _path = args[i + 1];
            if (args[i] == "--frames" && int.TryParse(args[i + 1], out var n)) _frames = n;
        }
        if (_path == null) SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (--_frames > 0 || _path == null) return;
        var image = GetViewport().GetTexture().GetImage();
        var err = image.SavePng(_path);
        GD.Print(err == Error.Ok ? $"Saved {_path}" : $"Failed to save {_path}: {err}");
        GetTree().Quit();
    }
}
