using System.Windows;
using touch_cursor.Models;
using touch_cursor.Services;
using TouchCursor.Support.Local.Helpers;

namespace touch_cursor;

partial class App : PrismApplication
{
    
    protected override Window CreateShell()
    {
        return Container.Resolve<ShellWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Options
        var options = TouchCursorOptions.Load(TouchCursorOptions.GetDefaultConfigPath());
        containerRegistry.RegisterInstance<ITouchCursorOptions>(options);
        containerRegistry.RegisterInstance(options);

        // Services
        containerRegistry.RegisterSingleton<TypingLogger>();
        containerRegistry.RegisterSingleton<IKeyMappingService, KeyMappingService>();
        containerRegistry.RegisterSingleton<KeyboardHookService>();

        // Views
        containerRegistry.Register<ShellWindow>();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Wire up SendKey event
        var mappingService = Container.Resolve<IKeyMappingService>();
        var hookService = Container.Resolve<KeyboardHookService>();

        if (mappingService is KeyMappingService keyMappingService)
        {
            keyMappingService.SendKeyRequested += hookService.SendKey;
        }
    }
}