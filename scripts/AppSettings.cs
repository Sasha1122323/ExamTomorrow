using Godot;

// Настройки живут, пока запущено приложение (между сценами)
public static class AppSettings
{
    public static int Volume = 100;   // 0..100

    public static void Apply()
    {
        AudioServer.SetBusMute(0, Volume <= 0);
        AudioServer.SetBusVolumeDb(0, Volume <= 0 ? -80f : Mathf.LinearToDb(Volume / 100f));
    }
}
