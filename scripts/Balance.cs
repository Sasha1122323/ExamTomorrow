// ВСЕ ЧИСЛА БАЛАНСА В ОДНОМ МЕСТЕ
public static class Balance
{
    public const int StartMin = 21 * 60;              // 21:00
    public const int EndMin = 33 * 60;                // 09:00 следующего дня
    public const float RealSecPerGameMin = 1.0f;      // 1 игровая минута = 1 реальная секунда (ночь = 12 мин)

    public const int WinKnowledge = 70;
    public const int CoffeeMax = 3;
    public const int CoffeeBoostMin = 60;             // бодрость от кофе действует 1 игровой час

    // Шум соседа: 23:00, 01:00, 03:00, 05:00, 07:00
    public static readonly int[] NoiseStarts = { 23 * 60, 25 * 60, 27 * 60, 29 * 60, 31 * 60 };
    public const int NoiseDurationMin = 60;
}
