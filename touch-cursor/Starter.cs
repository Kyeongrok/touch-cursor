using Velopack;

namespace touch_cursor;

internal class Starter
{
    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        _ = new App().Run();
    }
}