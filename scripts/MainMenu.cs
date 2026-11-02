using Godot;

public partial class MainMenu : Control
{
    private Control _about, _confirm;
    private SettingsPanel _settings;

    public override void _Ready()
    {
        _about = GetNode<Control>("AboutPanel");
        _confirm = GetNode<Control>("ConfirmPanel");
        _settings = GetNode<SettingsPanel>("%SettingsPanel");

        GetNode<Button>("%StartButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
        GetNode<Button>("%SettingsButton").Pressed += () => _settings.Open();
        GetNode<Button>("%AboutButton").Pressed += () => _about.Show();
        GetNode<Button>("%ExitButton").Pressed += () => _confirm.Show();

        GetNode<Button>("%AboutBackButton").Pressed += () => _about.Hide();
        GetNode<Button>("%ConfirmYesButton").Pressed += () => GetTree().Quit();
        GetNode<Button>("%ConfirmNoButton").Pressed += () => _confirm.Hide();
    }
}
