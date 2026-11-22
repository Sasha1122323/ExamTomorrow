// ВСЕ ЧИСЛА БАЛАНСА В ОДНОМ МЕСТЕ
public static class Balance
{
    public const int StartMin = 21 * 60;              // 21:00
    public const int EndMin = 33 * 60;                // 09:00 следующего дня

    // Скорость времени (реальных секунд на 1 игровую минуту)
    public const float SecPerMinIdle = 1.0f;          // обычный режим: 1 игр. минута = 1 сек
    public const float SecPerMinBusy = 0.5f;          // во время действия: 2 игр. минуты = 1 сек

    public const int WinKnowledge = 70;               // знаний для сдачи
    public const int WinEnergy = 30;                  // бодрости к 09:00 для сдачи
    public const int LowEnergyWarning = 20;           // предупреждение о низкой бодрости

    public const int CoffeeMax = 3;
    public const int CoffeeBoostMin = 60;             // бодрость от кофе действует 1 игровой час

    // Пассивная потеря бодрости (не действует во время сна)
    public const int PassiveEveryMin = 30;
    public const int PassiveLoss = 2;                 // В документе 8.5 написано 2, в разделе 11 написано 1. УТОЧНИТЬ.

    // Шум соседа: 23:00, 01:00, 03:00, 05:00, 07:00
    public static readonly int[] NoiseStarts = { 23 * 60, 25 * 60, 27 * 60, 29 * 60, 31 * 60 };
    public const int NoiseDurationMin = 60;
}
