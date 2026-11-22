using Godot;

// Аналоговый циферблат (рисуется кодом)
public partial class ClockFace : Control
{
    private int _totalMinutes = 0;

    public int TotalMinutes
    {
        get => _totalMinutes;
        set { _totalMinutes = value; QueueRedraw(); }
    }

    public override void _Draw()
    {
        Vector2 c = Size / 2;
        float r = Mathf.Min(Size.X, Size.Y) / 2 - 4;
        Color dark = new Color(0.1f, 0.1f, 0.1f);

        DrawCircle(c, r, new Color(0.95f, 0.95f, 0.9f));
        DrawArc(c, r, 0, Mathf.Tau, 48, dark, 4);

        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.Tau / 12 - Mathf.Pi / 2;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            DrawLine(c + dir * (r - 12), c + dir * (r - 3), dark, 3);
        }

        int h = (_totalMinutes / 60) % 12;
        int m = _totalMinutes % 60;
        float ha = ((h + m / 60f) / 12f) * Mathf.Tau - Mathf.Pi / 2;
        float ma = (m / 60f) * Mathf.Tau - Mathf.Pi / 2;

        DrawLine(c, c + new Vector2(Mathf.Cos(ha), Mathf.Sin(ha)) * r * 0.5f, dark, 6);
        DrawLine(c, c + new Vector2(Mathf.Cos(ma), Mathf.Sin(ma)) * r * 0.8f, dark, 4);
    }
}
