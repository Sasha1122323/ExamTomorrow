using Godot;

// Основные объекты и интерфейс лежат в сцене Game.tscn.
// Новые элементы HUD (циферблат, кнопка отмены, отметка цели бодрости) создаются кодом,
// чтобы не ломать вашу расстановку в сцене.
public partial class Game : Node2D
{
    private GameState _gs;
    private CanvasLayer _hud;

    private Label _clock, _notice, _busy, _coffee, _neighborState;
    private Label _knowledgeTitle, _energyTitle, _actionTitle, _noiseIcon;
    private Label _resultTitle, _resultText;
    private ProgressBar _knowledgeBar, _energyBar;
    private Control _actionPanel, _pauseMenu, _resultScreen;
    private Button _actionButton, _cancelButton;
    private SettingsPanel _settings;
    private ClockFace _face;

    private StyleBoxFlat _red, _yellow, _green;

    private string _pendingId = "";
    private double _noticeTimer = 0;

    public override void _Ready()
    {
        _gs = GetNode<GameState>("/root/GameState");
        _hud = GetNode<CanvasLayer>("Hud");

        _clock = GetNode<Label>("%ClockLabel");
        _notice = GetNode<Label>("%NoticeLabel");
        _busy = GetNode<Label>("%BusyLabel");
        _coffee = GetNode<Label>("%CoffeeLabel");
        _neighborState = GetNode<Label>("%NeighborState");
        _knowledgeTitle = GetNode<Label>("%KnowledgeTitle");
        _energyTitle = GetNode<Label>("%EnergyTitle");
        _noiseIcon = GetNode<Label>("%NoiseIcon");
        _knowledgeBar = GetNode<ProgressBar>("%KnowledgeBar");
        _energyBar = GetNode<ProgressBar>("%EnergyBar");

        _actionPanel = GetNode<Control>("%ActionPanel");
        _actionTitle = GetNode<Label>("%ActionTitle");
        _actionButton = GetNode<Button>("%ActionButton");
        _pauseMenu = GetNode<Control>("%PauseMenu");
        _resultScreen = GetNode<Control>("%ResultScreen");
        _resultTitle = GetNode<Label>("%ResultTitle");
        _resultText = GetNode<Label>("%ResultText");
        _settings = GetNode<SettingsPanel>("%SettingsPanel");

        BuildExtraUi();

        // Предметы из узла Objects
        foreach (Node n in GetNode("Objects").GetChildren())
        {
            if (n is Interactable item)
                item.Clicked += OnObjectClicked;
        }

        // Кнопки
        _actionButton.Pressed += OnActionPressed;
        GetNode<Button>("%CloseButton").Pressed += () => _actionPanel.Hide();
        GetNode<Button>("%PauseButton").Pressed += OnPause;
        GetNode<Button>("%ContinueButton").Pressed += OnContinue;
        GetNode<Button>("%PauseSettingsButton").Pressed += () => _settings.Open();
        GetNode<Button>("%ToMenuButton").Pressed += ToMenu;
        GetNode<Button>("%RestartButton").Pressed += () => GetTree().ReloadCurrentScene();
        GetNode<Button>("%ResultMenuButton").Pressed += ToMenu;
        _cancelButton.Pressed += () => _gs.CancelAction();

        _gs.StatsChanged += Refresh;
        _gs.GameOver += OnGameOver;
        _gs.Notice += OnNotice;

        _gs.Reset();
    }

    public override void _ExitTree()
    {
        _gs.StatsChanged -= Refresh;
        _gs.GameOver -= OnGameOver;
        _gs.Notice -= OnNotice;
        _gs.Stop();
    }

    public override void _Process(double delta)
    {
        if (_noticeTimer > 0 && !_gs.Paused)
        {
            _noticeTimer -= delta;
            if (_noticeTimer <= 0) _notice.Text = "";
        }
    }

    // ---------- Новые элементы HUD (создаются кодом) ----------
    private void AddHud(Control c)
    {
        _hud.AddChild(c);
        _hud.MoveChild(c, _pauseMenu.GetIndex());   // ниже меню паузы и экрана результата
    }

    private void BuildExtraUi()
    {
        _red = new StyleBoxFlat { BgColor = new Color(0.90f, 0.30f, 0.30f) };
        _yellow = new StyleBoxFlat { BgColor = new Color(0.95f, 0.80f, 0.25f) };
        _green = new StyleBoxFlat { BgColor = new Color(0.30f, 0.80f, 0.40f) };

        // Циферблат рядом с цифровыми часами
        _face = new ClockFace
        {
            Position = new Vector2(240, 10),
            Size = new Vector2(110, 110),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddHud(_face);

        // Отметка цели бодрости (30)
        float w = _energyBar.Size.X > 0 ? _energyBar.Size.X : 340f;
        float h = _energyBar.Size.Y > 0 ? _energyBar.Size.Y : 36f;
        var mark = new ColorRect
        {
            Position = new Vector2(w * Balance.WinEnergy / 100f - 2, 0),
            Size = new Vector2(4, h),
            Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _energyBar.AddChild(mark);

        var goal = new Label
        {
            Text = $"цель {Balance.WinEnergy}",
            Position = _energyBar.Position + new Vector2(w * Balance.WinEnergy / 100f - 35, 38),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        goal.AddThemeFontSizeOverride("font_size", 20);
        AddHud(goal);

        // Кнопка «Отменить» (снизу, появляется во время действия)
        _cancelButton = new Button
        {
            Text = "Отменить действие",
            Position = new Vector2(1300, 985),
            Size = new Vector2(300, 60),
            Visible = false
        };
        _cancelButton.AddThemeFontSizeOverride("font_size", 30);
        AddHud(_cancelButton);
    }

    // ---------- Действия по предметам ----------
    private string NameFor(string id) => id switch
    {
        "textbook" => "Учебник",
        "bed" => "Кровать",
        "kettle" => "Чайник",
        "neighbor" => "Сосед",
        _ => id
    };

    private GameAction ActionFor(string id) => id switch
    {
        "textbook" => Actions.Study,
        "bed" => Actions.Sleep,
        "kettle" => Actions.Coffee,
        "neighbor" => _gs.IsNoisy ? Actions.AskQuiet : null,   // тихого соседа нажимать бесполезно
        _ => null
    };

    private void OnObjectClicked(string id)
    {
        if (!_gs.Running || _gs.Paused || _gs.IsOver) return;

        var action = ActionFor(id);
        if (action == null)
        {
            _actionPanel.Hide();
            return;
        }

        _pendingId = id;
        _actionTitle.Text = NameFor(id);

        string text = $"{action.Name} ({action.Minutes} мин)";
        if (_gs.IsNoisy && (action.Id == "study" || action.Id == "sleep"))
            text += " — шум мешает";
        _actionButton.Text = text;

        _actionPanel.Show();
    }

    private void OnActionPressed()
    {
        var action = ActionFor(_pendingId);
        _actionPanel.Hide();
        if (action != null) _gs.TryStart(action);
    }

    // ---------- Пауза ----------
    private void OnPause()
    {
        if (!_gs.Running) return;
        _gs.SetPaused(true);
        _actionPanel.Hide();
        _pauseMenu.Show();
    }

    private void OnContinue()
    {
        _pauseMenu.Hide();
        _gs.SetPaused(false);
    }

    private void ToMenu() => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");

    // ---------- Интерфейс ----------
    private void Refresh()
    {
        _clock.Text = _gs.TimeText();
        _face.TotalMinutes = _gs.TimeMin;

        _knowledgeBar.Value = _gs.Knowledge;
        _energyBar.Value = _gs.Energy;

        // Цвет шкал: знания 0–29 красный, 30–69 жёлтый, 70–100 зелёный
        //            бодрость 0–29 красный, 30–59 жёлтый, 60–100 зелёный
        _knowledgeBar.AddThemeStyleboxOverride("fill",
            _gs.Knowledge < 30 ? _red : _gs.Knowledge < 70 ? _yellow : _green);
        _energyBar.AddThemeStyleboxOverride("fill",
            _gs.Energy < 30 ? _red : _gs.Energy < 60 ? _yellow : _green);

        _knowledgeTitle.Text = $"ЗНАНИЯ {_gs.Knowledge}/100";

        // Предупреждение о низкой бодрости (20 и ниже)
        if (_gs.Energy <= Balance.LowEnergyWarning)
        {
            _energyTitle.Text = $"БОДРОСТЬ {_gs.Energy}/100  (низкая!)";
            _energyTitle.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.35f));
        }
        else
        {
            _energyTitle.Text = $"БОДРОСТЬ {_gs.Energy}/100";
            _energyTitle.RemoveThemeColorOverride("font_color");
        }

        _coffee.Text = $"Кофе: {_gs.CoffeeLeft}/{Balance.CoffeeMax}";

        _neighborState.Text = _gs.IsNoisy ? "Сосед: Шумит" : "Сосед: Тихо";
        _neighborState.AddThemeColorOverride("font_color",
            _gs.IsNoisy ? new Color(1f, 0.4f, 0.35f) : new Color(0.6f, 0.9f, 0.6f));
        _noiseIcon.Visible = _gs.IsNoisy;

        // Текущее действие
        if (_gs.CurrentAction != null)
        {
            _busy.Text = $"{_gs.CurrentAction.Doing} — осталось {_gs.BusyLeft} мин";
            _cancelButton.Visible = _gs.CurrentAction.Cancelable;
        }
        else
        {
            _busy.Text = "";
            _cancelButton.Visible = false;
        }
    }

    private void OnNotice(string text)
    {
        _notice.Text = text;
        _noticeTimer = 3.0;   // 3 реальные секунды
    }

    private void OnGameOver(string result)
    {
        _actionPanel.Hide();
        _cancelButton.Hide();

        switch (result)
        {
            case "win":
                _resultTitle.Text = "ПОБЕДА!";
                _resultText.Text = $"Экзамен сдан.\nЗнания: {_gs.Knowledge}/100, бодрость: {_gs.Energy}/100";
                break;
            case "tired":
                _resultTitle.Text = "НЕ СМОГ СДАТЬ";
                _resultText.Text = $"Знаний хватило, но не хватило бодрости.\nЗнания: {_gs.Knowledge}/100, бодрость: {_gs.Energy}/100 (нужно {Balance.WinEnergy})";
                break;
            default:
                _resultTitle.Text = "ПЕРЕСДАЧА";
                _resultText.Text = $"Знаний не хватило.\nЗнания: {_gs.Knowledge}/100 (нужно {Balance.WinKnowledge})";
                break;
        }
        _resultScreen.Show();
    }
}
