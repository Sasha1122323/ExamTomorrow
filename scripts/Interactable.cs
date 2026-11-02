using Godot;

public partial class Interactable : Area2D
{
    [Signal] public delegate void ClickedEventHandler(string objectId);

    [Export] public string ObjectId { get; set; } = "";

    public override void _Ready()
    {
        // Подсветка при наведении (работает на любые дочерние картинки)
        MouseEntered += () => Modulate = new Color(1.25f, 1.25f, 1.25f);
        MouseExited += () => Modulate = Colors.White;
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            EmitSignal(SignalName.Clicked, ObjectId);
        }
    }
}
