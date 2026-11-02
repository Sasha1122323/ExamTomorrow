using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
    [Signal] public delegate void StatsChangedEventHandler();
    [Signal] public delegate void GameOverEventHandler(string result);
    [Signal] public delegate void NoticeEventHandler(string text);

    public int Knowledge { get; private set; } = 0;
    public int Energy { get; private set; } = 100;
    public int TimeMin { get; private set; } = Balance.StartMin;
    public int CoffeeLeft { get; private set; } = Balance.CoffeeMax;

    public bool IsNoisy { get; private set; } = false;
    public bool Running { get; private set; } = false;
    public bool Paused { get; private set; } = false;
    public bool IsOver { get; private set; } = false;

    public GameAction CurrentAction { get; private set; } = null;
    public int BusyLeft { get; private set; } = 0;

    private double _acc = 0;
    private int _noiseEndsAt = -1;
    private readonly List<(int expireAt, int amount)> _boosts = new List<(int expireAt, int amount)>();

    // ---------- Управление сессией ----------
    public void Reset()
    {
        Knowledge = 0;
        Energy = 100;
        TimeMin = Balance.StartMin;
        CoffeeLeft = Balance.CoffeeMax;
        IsNoisy = false;
        IsOver = false;
        Paused = false;
        CurrentAction = null;
        BusyLeft = 0;
        _acc = 0;
        _noiseEndsAt = -1;
        _boosts.Clear();
        Running = true;
        Notify();
    }

    public void Stop()
    {
        Running = false;
        Paused = false;
    }

    public void SetPaused(bool paused)
    {
        if (!Running) return;
        Paused = paused;
    }

    public override void _Process(double delta)
    {
        if (!Running || Paused) return;
        _acc += delta;
        while (_acc >= Balance.RealSecPerGameMin)
        {
            _acc -= Balance.RealSecPerGameMin;
            Tick();
            if (!Running) break;
        }
    }

    // ---------- Одна игровая минута ----------
    private void Tick()
    {
        TimeMin++;

        // Закончилось действие кофе: бодрость падает обратно
        for (int i = _boosts.Count - 1; i >= 0; i--)
        {
            if (TimeMin >= _boosts[i].expireAt)
            {
                Energy = Mathf.Max(0, Energy - _boosts[i].amount);
                _boosts.RemoveAt(i);
            }
        }

        UpdateNoise();

        if (CurrentAction != null)
        {
            BusyLeft--;
            if (BusyLeft <= 0) FinishAction();
        }

        if (CurrentAction == null && Energy <= 0)
        {
            Say("Вы уснули за столом!");
            BeginAction(Actions.Forced);
        }

        if (TimeMin >= Balance.EndMin)
            Finish(Knowledge >= Balance.WinKnowledge ? "win" : "retake");

        Notify();
    }

    // ---------- Сосед ----------
    private void UpdateNoise()
    {
        if (IsNoisy && TimeMin >= _noiseEndsAt)
        {
            IsNoisy = false;
            Say("Сосед затих.");
        }

        foreach (int start in Balance.NoiseStarts)
        {
            if (TimeMin == start)
            {
                IsNoisy = true;
                _noiseEndsAt = start + Balance.NoiseDurationMin;
                Say("Сосед включил музыку!");
            }
        }
    }

    // ---------- Действия ----------
    public bool TryStart(GameAction a)
    {
        if (!Running || Paused || IsOver) return false;

        if (CurrentAction != null)
        {
            Say($"Вы заняты: {CurrentAction.Doing}.");
            return false;
        }

        if (a.Id == "coffee")
        {
            if (CoffeeLeft <= 0) { Say("Кофе нет."); return false; }
            CoffeeLeft--;
        }

        BeginAction(a);
        Notify();
        return true;
    }

    private void BeginAction(GameAction a)
    {
        CurrentAction = a;
        BusyLeft = a.Minutes;
    }

    private void FinishAction()
    {
        var a = CurrentAction;
        CurrentAction = null;
        BusyLeft = 0;

        int before = Energy;
        Knowledge = Mathf.Clamp(Knowledge + a.Knowledge, 0, 100);
        Energy = Mathf.Clamp(Energy + a.Energy, 0, 100);

        if (a.Id == "coffee")
        {
            int gain = Energy - before;
            if (gain > 0) _boosts.Add((TimeMin + Balance.CoffeeBoostMin, gain));
            if (CoffeeLeft == 0) Say("Кофе закончился!");
        }

        if (a.Id == "ask" && IsNoisy)
        {
            IsNoisy = false;
            Say("Сосед затих.");
        }
    }

    // ---------- Служебное ----------
    public string TimeText()
    {
        int h = (TimeMin / 60) % 24;
        int m = TimeMin % 60;
        return $"{h:00}:{m:00}";
    }

    private void Finish(string result)
    {
        if (IsOver) return;
        IsOver = true;
        Running = false;
        Notify();
        EmitSignal(SignalName.GameOver, result);
    }

    private void Say(string text) => EmitSignal(SignalName.Notice, text);
    private void Notify() => EmitSignal(SignalName.StatsChanged);
}
