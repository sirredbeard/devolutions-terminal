namespace Devolutions.Terminal.Shell.Gtk;

/// <summary>
/// Real GTK4/libadwaita toplevel. GirCore P/Invokes libgtk-4 and libadwaita-1.
/// No Avalonia. No Skia chrome. No CSS color painting to chase screenshots.
/// Stock Adw widgets own materials, contrast, and density.
/// Product name appears only in About (and desktop metadata), never in chrome.
/// </summary>
public static class GtkShellApplication
{
    public const string ApplicationId = "com.devolutions.terminal";

    /// <summary>About dialog and .desktop only. Not for window or tab titles.</summary>
    public const string ProductName = "Devolutions Terminal";

    private static Adw.ApplicationWindow? _mainWindow;
    private static Adw.TabView? _tabView;
    private static Adw.WindowTitle? _titleWidget;

    public static int Run(IReadOnlyList<string>? args = null)
    {
        var argv = args is null ? null : args as string[] ?? args.ToArray();

        // Adw.Application.New runs adw_init and loads the desktop stylesheet.
        var application = Adw.Application.New(ApplicationId, Gio.ApplicationFlags.FlagsNone);
        LogToolkitBanner();
        RegisterActions(application);
        application.OnActivate += OnActivate;
        return application.RunWithSynchronizationContext(argv);
    }

    private static void LogToolkitBanner()
    {
        // Proof for operators: this process is the system toolkit, not a painted theme.
        var gtkMajor = global::Gtk.Functions.GetMajorVersion();
        var gtkMinor = global::Gtk.Functions.GetMinorVersion();
        var gtkMicro = global::Gtk.Functions.GetMicroVersion();
        Console.Error.WriteLine(
            $"dt-gtk: native shell via GirCore -> libgtk-4.so / libadwaita-1.so " +
            $"(GTK {gtkMajor}.{gtkMinor}.{gtkMicro}). No Avalonia chrome on this path.");
    }

    private static void OnActivate(Gio.Application sender, EventArgs args)
    {
        var app = (Adw.Application)sender;
        if (_mainWindow is not null)
        {
            _mainWindow.Present();
            return;
        }

        _mainWindow = CreateMainWindow(app);
        app.AddWindow(_mainWindow);
        _mainWindow.Present();
    }

    private static void RegisterActions(Adw.Application app)
    {
        AddStatelessAction(app, "about", ShowAbout);
        AddStatelessAction(app, "preferences", static () => { /* phase 2 */ });
        AddStatelessAction(app, "palette", static () => { /* phase 2 */ });
        AddStatelessAction(app, "find", static () => { /* phase 2 */ });
        AddStatelessAction(app, "new-tab", static () => NewTabFromAction());
        AddStatelessAction(app, "profile-default", static () => NewTabFromAction("Default"));
        AddStatelessAction(app, "profile-pwsh", static () => NewTabFromAction("PowerShell"));
        AddStatelessAction(app, "profile-bash", static () => NewTabFromAction("bash"));
    }

    private static void AddStatelessAction(Adw.Application app, string name, Action handler)
    {
        var action = Gio.SimpleAction.New(name, null);
        action.OnActivate += (_, _) => handler();
        app.AddAction(action);
    }

    internal static Adw.ApplicationWindow CreateMainWindow(Adw.Application app)
    {
        var window = Adw.ApplicationWindow.New(app);
        window.SetDefaultSize(960, 640);

        // Stock Adw header. Decorations follow the desktop (:close on GNOME).
        var header = Adw.HeaderBar.New();
        header.SetDecorationLayout(":close");
        header.SetShowStartTitleButtons(false);
        header.SetShowEndTitleButtons(true);
        header.SetCenteringPolicy(Adw.CenteringPolicy.Strict);

        // Session title in chrome. Product name stays out (About only).
        var sessionTitle = SessionTitle.ForCurrentDirectory();
        _titleWidget = Adw.WindowTitle.New(sessionTitle, string.Empty);
        header.SetTitleWidget(_titleWidget);
        window.SetTitle(sessionTitle);

        // WT behavior through a stock Adw control: primary opens default, menu picks profile.
        var newTabSplit = Adw.SplitButton.New();
        newTabSplit.SetIconName("list-add-symbolic");
        newTabSplit.SetTooltipText("New tab (default profile)");
        newTabSplit.SetDropdownTooltip("Select profile");
        newTabSplit.SetActionName("app.new-tab");
        newTabSplit.SetMenuModel(BuildProfileMenu());
        header.PackStart(newTabSplit);

        var findButton = global::Gtk.Button.NewFromIconName("edit-find-symbolic");
        findButton.SetTooltipText("Find");
        findButton.SetActionName("app.find");

        var appMenuButton = global::Gtk.MenuButton.New();
        appMenuButton.SetIconName("open-menu-symbolic");
        appMenuButton.SetTooltipText("Menu");
        appMenuButton.SetPrimary(true);
        appMenuButton.SetMenuModel(BuildAppMenu());

        // Stock .linked group. Theme draws the pill; we do not.
        var endBox = global::Gtk.Box.New(global::Gtk.Orientation.Horizontal, 0);
        endBox.AddCssClass("linked");
        endBox.Append(findButton);
        endBox.Append(appMenuButton);
        header.PackEnd(endBox);

        _tabView = Adw.TabView.New();
        _tabView.OnNotify += (view, notifyArgs) =>
        {
            if (notifyArgs.Pspec is not null &&
                string.Equals(notifyArgs.Pspec.GetName(), "selected-page", StringComparison.Ordinal))
            {
                SyncTitleFromSelectedTab(window);
            }
        };

        AppendSessionTab(sessionTitle);

        var tabBar = Adw.TabBar.New();
        tabBar.SetView(_tabView);
        tabBar.SetAutohide(true);
        tabBar.SetExpandTabs(true);

        // Stock Adw tabbed window composition. Materials come from libadwaita.
        var toolbar = Adw.ToolbarView.New();
        toolbar.AddTopBar(header);
        toolbar.AddTopBar(tabBar);
        toolbar.SetTopBarStyle(Adw.ToolbarStyle.Flat);
        toolbar.SetContent(_tabView);

        window.SetContent(toolbar);
        SyncTitleFromSelectedTab(window);
        return window;
    }

    private static void NewTabFromAction(string? profileLabel = null)
    {
        if (_tabView is null || _mainWindow is null)
        {
            return;
        }

        var title = profileLabel is null
            ? SessionTitle.ForCurrentDirectory()
            : SessionTitle.ForProfile(profileLabel);
        var page = AppendSessionTab(title);
        _tabView.SetSelectedPage(page);
        SyncTitleFromSelectedTab(_mainWindow);
    }

    private static Adw.TabPage AppendSessionTab(string title)
    {
        var page = _tabView!.Append(BuildTerminalSurface());
        page.SetTitle(title);
        page.SetTooltip(title);
        return page;
    }

    private static void SyncTitleFromSelectedTab(Adw.ApplicationWindow window)
    {
        if (_tabView is null || _titleWidget is null)
        {
            return;
        }

        var page = _tabView.GetSelectedPage();
        var text = page?.GetTitle();
        if (string.IsNullOrWhiteSpace(text))
        {
            text = SessionTitle.ForCurrentDirectory();
        }

        _titleWidget.SetTitle(text);
        _titleWidget.SetSubtitle(string.Empty);
        window.SetTitle(text);
    }

    /// <summary>
    /// Content hole for phase 1 frame sink. Default widget background from Adwaita.
    /// No fill color overrides.
    /// </summary>
    private static global::Gtk.Widget BuildTerminalSurface()
    {
        var surface = global::Gtk.Box.New(global::Gtk.Orientation.Vertical, 0);
        surface.SetHexpand(true);
        surface.SetVexpand(true);
        return surface;
    }

    private static void ShowAbout()
    {
        if (_mainWindow is null)
        {
            return;
        }

        var about = Adw.AboutDialog.New();
        about.SetApplicationName(ProductName);
        about.SetApplicationIcon("utilities-terminal");
        about.SetDeveloperName("Devolutions");
        about.SetVersion("0.1.0");
        about.SetComments(
            "Cross-platform terminal with Windows Terminal behavior. " +
            "This window is a real GTK4/libadwaita shell (GirCore), not Avalonia chrome.");
        about.SetWebsite("https://github.com/Devolutions/devolutions-terminal");
        about.SetIssueUrl("https://github.com/Devolutions/devolutions-terminal/issues");
        about.SetLicenseType(global::Gtk.License.MitX11);
        about.Present(_mainWindow);
    }

    private static Gio.Menu BuildProfileMenu()
    {
        var menu = Gio.Menu.New();
        menu.Append("Default profile", "app.profile-default");
        menu.Append("PowerShell", "app.profile-pwsh");
        menu.Append("bash", "app.profile-bash");
        return menu;
    }

    private static Gio.Menu BuildAppMenu()
    {
        var newSection = Gio.Menu.New();
        newSection.Append("New Tab", "app.new-tab");
        newSection.AppendSection("Profiles", BuildProfileMenu());

        var tools = Gio.Menu.New();
        tools.Append("Find", "app.find");
        tools.Append("Command palette", "app.palette");
        tools.Append("Preferences", "app.preferences");

        var about = Gio.Menu.New();
        about.Append("About", "app.about");

        var menu = Gio.Menu.New();
        menu.AppendSection(null, newSection);
        menu.AppendSection(null, tools);
        menu.AppendSection(null, about);
        return menu;
    }
}

/// <summary>
/// Tab and window titles are session strings, never the product name.
/// </summary>
internal static class SessionTitle
{
    public static string ForCurrentDirectory(string? suffix = null)
    {
        var user = Environment.UserName;
        var host = GetHostName();
        var path = FormatPath(Directory.GetCurrentDirectory());
        var core = string.IsNullOrEmpty(host)
            ? $"{user}:{path}"
            : $"{user}@{host}:{path}";
        return suffix is null ? core : core + suffix;
    }

    public static string ForProfile(string profileName) => profileName;

    private static string GetHostName()
    {
        try
        {
            return Environment.MachineName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FormatPath(string path)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(home) &&
            path.StartsWith(home, StringComparison.Ordinal))
        {
            var rest = path[home.Length..];
            return rest.Length == 0 ? "~" : "~" + rest.Replace('\\', '/');
        }

        return path.Replace('\\', '/');
    }
}
