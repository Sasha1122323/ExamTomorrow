// Cost      — списывается СРАЗУ при начале действия
// Knowledge — начисляется ПОСЛЕ завершения (в тишине / при шуме)
// Gain      — бодрость, начисляется ПОСЛЕ завершения (в тишине / при шуме)
// Если значение "при шуме" равно -1, оно такое же, как в тишине.
public record GameAction(
    string Id, string Name, string Doing, int Minutes,
    int Cost = 0,
    int Knowledge = 0, int KnowledgeNoisy = -1,
    int Gain = 0, int GainNoisy = -1,
    bool Cancelable = true);

public static class Actions
{
    public static readonly GameAction Study    = new("study",  "Учиться",             "Учёба",              30, Cost: 15, Knowledge: 10, KnowledgeNoisy: 4);
    public static readonly GameAction Sleep    = new("sleep",  "Спать",               "Сон",                60, Gain: 25, GainNoisy: 15);
    public static readonly GameAction Coffee   = new("coffee", "Выпить кофе",         "Приготовление кофе", 10, Gain: 25);
    public static readonly GameAction AskQuiet = new("ask",    "Попросить не шуметь", "Просьба к соседу",   15);
    public static readonly GameAction Forced   = new("forced", "Заснул",              "Сон за столом",      30, Gain: 15, Cancelable: false);
}
