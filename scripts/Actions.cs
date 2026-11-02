public record GameAction(string Id, string Name, string Doing, int Minutes, int Knowledge = 0, int Energy = 0);

public static class Actions
{
    public static readonly GameAction Study      = new("study",  "Учиться",              "Учусь",                  30, Knowledge: 10, Energy: -15);
    public static readonly GameAction StudyNoisy = new("study",  "Учиться при шуме",     "Учусь (сосед мешает)",   30, Knowledge: 4,  Energy: -15);
    public static readonly GameAction Sleep      = new("sleep",  "Спать",                "Сплю",                   60, Energy: 25);
    public static readonly GameAction Coffee     = new("coffee", "Выпить кофе",          "Пью кофе",               10, Energy: 25);
    public static readonly GameAction AskQuiet   = new("ask",    "Попросить не шуметь",  "Прошу соседа не шуметь", 15);
    public static readonly GameAction Forced     = new("forced", "Заснул",               "Спит за столом",         30, Energy: 15);
}
