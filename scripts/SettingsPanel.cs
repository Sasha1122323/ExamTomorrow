using Godot;

public partial class SettingsPanel : Control
{
    private HSlider _slider;
    private Label _label;

    public override void _Ready()
    {
        _slider = GetNode<HSlider>("%VolumeSlider");
        _label = GetNode<Label>("%VolumeLabel");
        _slider.ValueChanged += OnChanged;
        GetNode<Button>("%BackButton").Pressed += () => Hide();
        UpdateLabel();
    }

    public void Open()
    {
        _slider.SetValueNoSignal(AppSettings.Volume);
        UpdateLabel();
        Show();
    }

    private void OnChanged(double value)
    {
        AppSettings.Volume = (int)value;
        AppSettings.Apply();
        UpdateLabel();
    }

    private void UpdateLabel() => _label.Text = $"Громкость: {AppSettings.Volume}%";
}
