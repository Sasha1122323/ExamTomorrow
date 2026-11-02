using Godot;

// Все объекты и интерфейс лежат в сцене Game.tscn.
// Этот скрипт подключается к ним и управляет игрой.
public partial class Game : Node2D
{
    private GameState _gs;

    private Label _clock, _notice, _busy, _coffee, _neighborState;
    private Label _knowledgeTitle, _energyTitle, _actionTitle, _noiseIcon;
    private Label _resultTitle, _resultText;
    private ProgressBar _knowledgeBar, _energyBar;
    private Control _actionPanel, _pauseMenu, _resultScreen;
    private Button _actionButton;
    private SettingsPanel _settings;

    private string _pendingId = "";
    private double _noticeTimer = 0;

    public override void _Ready()
    {
        _gs = GetNode<GameState>("/root/GameState");

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
        "textbook" => _gs.IsNoisy ? Actions.StudyNoisy : Actions.Study,
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
        _actionButton.Text = $"{action.Name} ({action.Minutes} мин)";
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
        _knowledgeBar.Value = _gs.Knowledge;
        _energyBar.Value = _gs.Energy;
        _knowledgeTitle.Text = $"ЗНАНИЯ {_gs.Knowledge}/100";
        _energyTitle.Text = $"БОДРОСТЬ {_gs.Energy}/100";
        _coffee.Text = $"Кофе: {_gs.CoffeeLeft}/{Balance.CoffeeMax}";

        _neighborState.Text = _gs.IsNoisy ? "Сосед: Шумит" : "Сосед: Тихо";
        _neighborState.AddThemeColorOverride("font_color",
            _gs.IsNoisy ? new Color(1f, 0.4f, 0.35f) : new Color(0.6f, 0.9f, 0.6f));
        _noiseIcon.Visible = _gs.IsNoisy;

        _busy.Text = _gs.CurrentAction != null
            ? $"{_gs.CurrentAction.Doing}... (ещё {_gs.BusyLeft} мин)"
            : "";
    }

    private void OnNotice(string text)
    {
        _notice.Text = text;
        _noticeTimer = 3.0;   // 3 реальные секунды
    }

    private void OnGameOver(string result)
    {
        _actionPanel.Hide();
        if (result == "win")
        {
            _resultTitle.Text = "ПОБЕДА!";
            _resultText.Text = $"Экзамен сдан.\nЗнания: {_gs.Knowledge}/100";
        }
        else
        {
            _resultTitle.Text = "ПЕРЕСДАЧА";
            _resultText.Text = $"Знаний не хватило.\nЗнания: {_gs.Knowledge}/100 (нужно {Balance.WinKnowledge})";
        }
        _resultScreen.Show();
    }
}
