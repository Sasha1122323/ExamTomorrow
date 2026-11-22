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
    private int _noisyMin = 0;   // сколько минут текущего действия прошло при шуме
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
        _noisyMin = 0;
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
        while (true)
        {
            // Во время действия время идёт вдвое быстрее
            double step = CurrentAction != null ? Balance.SecPerMinBusy : Balance.SecPerMinIdle;
            if (_acc < step) break;
            _acc -= step;
            Tick();
            if (!Running) break;
        }
    }

    // ---------- Одна игровая минута ----------
    private void Tick()
    {
        TimeMin++;

        // Пассивная потеря бодрости (во сне не действует)
        bool sleeping = CurrentAction != null && (CurrentAction.Id == "sleep" || CurrentAction.Id == "forced");
        if (!sleeping && (TimeMin - Balance.StartMin) % Balance.PassiveEveryMin == 0)
            Energy = Mathf.Max(0, Energy - Balance.PassiveLoss);

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
            if (IsNoisy) _noisyMin++;
            BusyLeft--;
            if (BusyLeft <= 0) FinishAction();
        }

        if (CurrentAction == null && Energy <= 0)
        {
            Say("Вы уснули за столом!");
            BeginAction(Actions.Forced);
        }

        if (TimeMin >= Balance.EndMin)
        {
            if (Knowledge < Balance.WinKnowledge) Finish("retake");
            else if (Energy < Balance.WinEnergy) Finish("tired");
            else Finish("win");
        }

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

        if (a.Cost > 0 && Energy < a.Cost)
        {
            Say("Не хватает бодрости.");
            return false;
        }

        if (a.Id == "coffee")
        {
            if (CoffeeLeft <= 0) { Say("Кофе нет."); return false; }
            CoffeeLeft--;                       // порция списывается сразу
        }

        Energy = Mathf.Max(0, Energy - a.Cost); // бодрость списывается сразу
        BeginAction(a);
        Notify();
        return true;
    }

    public bool CancelAction()
    {
        if (!Running || Paused || CurrentAction == null || !CurrentAction.Cancelable) return false;

        // Потраченное не возвращается, награда не начисляется
        CurrentAction = null;
        BusyLeft = 0;
        _noisyMin = 0;
        Say("Действие отменено.");
        Notify();
        return true;
    }

    private void BeginAction(GameAction a)
    {
        CurrentAction = a;
        BusyLeft = a.Minutes;
        _noisyMin = 0;
    }

    private void FinishAction()
    {
        var a = CurrentAction;
        int noisyMin = _noisyMin;
        CurrentAction = null;
        BusyLeft = 0;
        _noisyMin = 0;

        // K = Tтиш/Tобщ * Kтиш + Tшум/Tобщ * Kшум
        float share = a.Minutes > 0 ? Mathf.Clamp((float)noisyMin / a.Minutes, 0f, 1f) : 0f;
        int kNoisy = a.KnowledgeNoisy < 0 ? a.Knowledge : a.KnowledgeNoisy;
        int gNoisy = a.GainNoisy < 0 ? a.Gain : a.GainNoisy;
        int knowledge = Mathf.RoundToInt(a.Knowledge * (1f - share) + kNoisy * share);
        int gain = Mathf.RoundToInt(a.Gain * (1f - share) + gNoisy * share);

        int before = Energy;
        Knowledge = Mathf.Clamp(Knowledge + knowledge, 0, 100);
        Energy = Mathf.Clamp(Energy + gain, 0, 100);

        if (a.Id == "coffee")
        {
            int added = Energy - before;
            if (added > 0) _boosts.Add((TimeMin + Balance.CoffeeBoostMin, added));
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
