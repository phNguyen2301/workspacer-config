#r "C:\Program Files\workspacer\workspacer.Shared.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Bar\workspacer.Bar.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Gap\workspacer.Gap.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.TitleBar\workspacer.TitleBar.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.ActionMenu\workspacer.ActionMenu.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.FocusIndicator\workspacer.FocusIndicator.dll"

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Timers;
using System.Linq;
using workspacer;
using workspacer.Bar;
using workspacer.Bar.Widgets;
using workspacer.TitleBar;
using workspacer.Gap;
using workspacer.ActionMenu;
using workspacer.FocusIndicator;


public class WorkspaceWidget : BarWidgetBase
{
    public Color WorkspaceHasFocusColor { get; set; } = new Color(143, 189, 187);
    public Color WorkspaceEmptyColor { get; set; } = Color.Gray;
    public Color WorkspaceIndicatingBackColor { get; set; } = Color.Teal;
    public int BlinkPeriod { get; set; } = 1000;

    private Timer _blinkTimer;
    private ConcurrentDictionary<IWorkspace, bool> _blinkingWorkspaces;

    public override void Initialize()
    {
        Context.Workspaces.WorkspaceUpdated += () => UpdateWorkspaces();
        Context.Workspaces.WindowMoved += (w, o, n) => UpdateWorkspaces();

        _blinkingWorkspaces = new ConcurrentDictionary<IWorkspace, bool>();

        _blinkTimer = new Timer(BlinkPeriod);
        _blinkTimer.Elapsed += (s, e) => BlinkIndicatingWorkspaces();
        _blinkTimer.Enabled = true;
    }

    public override IBarWidgetPart[] GetParts()
    {
        var parts = new List<IBarWidgetPart>();
        var workspaces = Context.WorkspaceContainer.GetWorkspaces(Context.Monitor);
        int index = 0;
        foreach (var workspace in workspaces)
        {
            parts.Add(CreatePart(workspace, index));
            index++;
        }
        return parts.ToArray();
    }

    private bool WorkspaceIsIndicating(IWorkspace workspace)
    {
        if (workspace.IsIndicating)
        {
            if (_blinkingWorkspaces.ContainsKey(workspace))
            {
                _blinkingWorkspaces.TryGetValue(workspace, out bool value);
                return value;
            } else
            {
                _blinkingWorkspaces.TryAdd(workspace, true);
                return true;
            }
        }
        else if (_blinkingWorkspaces.ContainsKey(workspace))
        {
            _blinkingWorkspaces.TryRemove(workspace, out bool _);
        }
        return false;
    }

    private IBarWidgetPart CreatePart(IWorkspace workspace, int index)
    {
        var backColor = WorkspaceIsIndicating(workspace) ? WorkspaceIndicatingBackColor : null;

        return Part(GetDisplayName(workspace, index), GetDisplayColor(workspace, index), backColor, () =>
        {
            Context.Workspaces.SwitchMonitorToWorkspace(Context.Monitor.Index, index);
        },
        FontName);
    }

    private void UpdateWorkspaces()
    {
        MarkDirty();
    }

    protected virtual string GetDisplayName(IWorkspace workspace, int index)
    {
        var monitor = Context.WorkspaceContainer.GetCurrentMonitorForWorkspace(workspace);
        var visible = Context.Monitor == monitor;

        return visible ? LeftPadding + workspace.Name + RightPadding : workspace.Name;
    }

    protected virtual Color GetDisplayColor(IWorkspace workspace, int index)
    {
        var monitor = Context.WorkspaceContainer.GetCurrentMonitorForWorkspace(workspace);
        if (Context.Monitor == monitor)
        {
            return WorkspaceHasFocusColor;
        }

        var hasWindows = workspace.ManagedWindows.Count != 0;
        return hasWindows ? null : WorkspaceEmptyColor;
    }

    private void BlinkIndicatingWorkspaces()
    {
        var workspaces = _blinkingWorkspaces.Keys;

        var didFlip = false;
        foreach (var workspace in workspaces)
        {
            if (_blinkingWorkspaces.TryGetValue(workspace, out bool value))
            {
                _blinkingWorkspaces.TryUpdate(workspace, !value, value);
                didFlip = true;
            }
        }

        if (didFlip)
        {
            MarkDirty();
        }
    }

}

public class HourIconWidget : BarWidgetBase
{   
    private string _text;
    private int _index;
    private string[] _icon = {"","","","","","","","","","","",""};

    public HourIconWidget()
    {
        _text = "";
    }

    public override IBarWidgetPart[] GetParts()
    {
        if (DateTime.Now.TimeOfDay.Hours > 12)
        {
            _index = DateTime.Now.TimeOfDay.Hours - 12;
        }
        else
        {
            _index = DateTime.Now.TimeOfDay.Hours;
        }
        _text = _icon[_index];
        return Parts(Part(_text, fontname: FontName));
    }

    public override void Initialize()
    {
    }
}

public class PomodoroWidget : BarWidgetBase
{
    private Timer _timer;
    private Timer _blinkTimer;
    private int _pomodoroMinutes = 0;
    private int _minutesLeft = 0;
    private Boolean _overtimeBackgroundOn;
    private Color _overtimeBackgroundColor;
    private string _incentive;


    public PomodoroWidget(int pomodoroMinutes = 25, Color overtimeBackgroundColor = null, string incentive = "Click to start pomodoro!")
    {
        _pomodoroMinutes = pomodoroMinutes;
        _incentive = incentive;

        if (overtimeBackgroundColor == null) {
            _overtimeBackgroundColor = Color.Red;
        } else {
            _overtimeBackgroundColor = overtimeBackgroundColor;
        }
    }

    public override IBarWidgetPart[] GetParts()
    {
        return Parts(
            Part(
                text: GetMessage(),
                back: GetBackgroundColor(),
                partClicked: () => {
                    if (_timer.Enabled) {
                        StopPomodoro();
                    } else {
                        StartPomodoro();
                    }
                }
            )
        );
    }
    
    private string GetMessage() {
        if (_timer.Enabled) {
            return _minutesLeft.ToString() + " minutes left";
        } else {
            return _incentive;
        }
    }

    private Color GetBackgroundColor() {
        if (_minutesLeft <= 0 && _timer.Enabled && _overtimeBackgroundOn) {
            return _overtimeBackgroundColor;
        } else {
            return null;
        }
    }

    public override void Initialize()
    {
        _timer = new Timer(60000);
        _timer.Elapsed += (s,e) => {
            _minutesLeft -= 1;
            if (_minutesLeft <= 0) {
                _blinkTimer.Start();
            }
            MarkDirty();
        };
        _timer.Stop();

        _blinkTimer = new Timer(1000);
        _blinkTimer.Elapsed += (s,e) => {
            _overtimeBackgroundOn = !_overtimeBackgroundOn;
            MarkDirty();
        };
        _blinkTimer.Stop();
    }

    private void StartPomodoro() 
    {
        _minutesLeft = _pomodoroMinutes;
        _timer.Start();
        _blinkTimer.Stop();
        MarkDirty();
    }

    private void StopPomodoro() 
    {
        _timer.Stop();
        _blinkTimer.Stop();
        MarkDirty();
    }
}



Action<IConfigContext> doConfig = (context) =>
{
/* Variables */
    var fontSize = 13;
    var barHeight = 25;
    var fontName = "FiraCode NFM";
    var background = new Color(10, 12, 18);

/* Config */
    context.CanMinimizeWindows = true;

/* Title Bar*/
    var titleBarPluginConfig = new TitleBarPluginConfig(new TitleBarStyle(showTitleBar: false, showSizingBorder: false));
    context.AddTitleBar(titleBarPluginConfig);

/* Gap */
    var gap = barHeight - 8;
    var gapPlugin = context.AddGap(new GapPluginConfig() { InnerGap = gap, OuterGap = gap / 2, Delta = gap / 2 });

/* Bar */  
    context.AddBar(new BarPluginConfig()
    {
        FontSize = fontSize,
        BarHeight = barHeight,
        FontName = fontName,
        DefaultWidgetBackground = background,
        LeftWidgets = () => new IBarWidget[]
        {
            new WorkspaceWidget(), 
        },
        RightWidgets = () => new IBarWidget[]
        {
            // new BatteryWidget(),
            new PomodoroWidget(),
            new HourIconWidget(),
            new TimeWidget(1000, "HH:mm"),
            // new ActiveLayoutWidget(),
        }
    });
/* Bar focus indicator */
    context.AddFocusIndicator( new FocusIndicatorPluginConfig() {
        BorderColor = background,
        BorderSize = 10,
        TimeToShow = 200,
    });

/* Default layouts */
    Func<ILayoutEngine[]> defaultLayouts = () => new ILayoutEngine[]
    {
        new DwindleLayoutEngine(),
        new TallLayoutEngine(),
        new FullLayoutEngine(),
    };
    context.DefaultLayouts = defaultLayouts;

/* Workspaces */
    // Array of workspace names and their layouts
    (string, ILayoutEngine[])[] workspaces =
    {
        ("[ main]", defaultLayouts()),
        ("[ code]", defaultLayouts()),
        ("[ todo]", new ILayoutEngine[] { new TallLayoutEngine() }),
        ("[ music]", defaultLayouts()),
        ("[ other]", defaultLayouts()),
    };
    foreach ((string name, ILayoutEngine[] layouts) in workspaces)
    {
        context.WorkspaceContainer.CreateWorkspace(name, layouts);
    }
/* Action Menu */


/* SubMenu */
    // Add "log off" menu to action menu (Alt + P)
     var actionMenu = context.AddActionMenu(new ActionMenuPluginConfig()
    {
        RegisterKeybind = false,
        MenuHeight = barHeight,
        FontSize = fontSize,
        FontName = fontName,
        Background = background,
    });

    var subMenu = actionMenu.Create();
    // Sleep
    string sleepCmd;
    sleepCmd = "/C rundll32.exe powrprof.dll,SetSuspendState 0,1,0";
    // Lock Desktop
    string lockCmd;
    lockCmd = "/C rundll32.exe user32.dll,LockWorkStation";
    // Shutdown
    string shutdownCmd;
    shutdownCmd = "/C shutdown /s /t 0";
    // Restart
    string restartCmd;
    restartCmd = "/C shutdown /r /t 0";
    subMenu.Add("sleep", () => System.Diagnostics.Process.Start("CMD.exe", sleepCmd));
    subMenu.Add("lock desktop", () => System.Diagnostics.Process.Start("CMD.exe", lockCmd));
    subMenu.Add("shutdown", () => System.Diagnostics.Process.Start("CMD.exe", shutdownCmd));
    subMenu.Add("restart", () => System.Diagnostics.Process.Start("CMD.exe", restartCmd));
    actionMenu.DefaultMenu.AddMenu("log off", subMenu);

/* Keybindings */

    KeyModifiers winShift = KeyModifiers.Win | KeyModifiers.Shift;
    KeyModifiers winCtrl = KeyModifiers.Win | KeyModifiers.Control;
    KeyModifiers win = KeyModifiers.Win;

    IKeybindManager manager = context.Keybinds;

    manager.UnsubscribeAll();

    // Mouse switch focus
    manager.Subscribe(MouseEvent.LButtonDown,
        () => context.Workspaces.SwitchFocusedMonitorToMouseLocation());

    // Toogle
    manager.Subscribe(win, Keys.Escape,
        () => context.Enabled = !context.Enabled, "toggle enable/disable");
    
    // Workspace
    manager.Subscribe(win, Keys.D1, 
        () => context.Workspaces.SwitchToWorkspace(0), "switch to workspace 1");
    manager.Subscribe(win, Keys.D2, 
        () => context.Workspaces.SwitchToWorkspace(1), "switch to workspace 2");
    manager.Subscribe(win, Keys.D3, 
        () => context.Workspaces.SwitchToWorkspace(2), "switch to workspace 3");
    manager.Subscribe(win, Keys.D4, 
        () => context.Workspaces.SwitchToWorkspace(3), "switch to workspace 4");
    manager.Subscribe(win, Keys.D5, 
        () => context.Workspaces.SwitchToWorkspace(4), "switch to workspace 5");

    manager.Subscribe(winShift, Keys.D1, 
        () => context.Workspaces.MoveFocusedWindowToWorkspace(0), "switch focused window to workspace 1");
    manager.Subscribe(winShift, Keys.D2, 
        () => context.Workspaces.MoveFocusedWindowToWorkspace(1), "switch focused window to workspace 2");
    manager.Subscribe(winShift, Keys.D3, 
        () => context.Workspaces.MoveFocusedWindowToWorkspace(2), "switch focused window to workspace 3");
    manager.Subscribe(winShift, Keys.D4, 
        () => context.Workspaces.MoveFocusedWindowToWorkspace(3), "switch focused window to workspace 4");
    manager.Subscribe(winShift, Keys.D5, 
        () => context.Workspaces.MoveFocusedWindowToWorkspace(4), "switch focused window to workspace 5");

    // H, L keys
    manager.Subscribe(winShift, Keys.H, 
        () => context.Workspaces.FocusedWorkspace.ShrinkPrimaryArea(), "shrink primary area");
    manager.Subscribe(winShift, Keys.L, 
        () => context.Workspaces.FocusedWorkspace.ExpandPrimaryArea(), "expand primary area");

    // K, J keys
    manager.Subscribe(winShift, Keys.K, 
        () => context.Workspaces.FocusedWorkspace.SwapFocusAndNextWindow(), "swap focus and next window");
    manager.Subscribe(winShift, Keys.J, 
        () => context.Workspaces.FocusedWorkspace.SwapFocusAndPreviousWindow(), "swap focus and previous window");
    manager.Subscribe(win, Keys.K, 
        () => context.Workspaces.FocusedWorkspace.FocusNextWindow(), "focus next window");
    manager.Subscribe(win, Keys.J, 
        () => context.Workspaces.FocusedWorkspace.FocusPreviousWindow(), "focus previous window");

    // Other shortcuts
    manager.Subscribe(winShift, Keys.P, 
        () => actionMenu.ShowDefault(), "open action menu");
    manager.Subscribe(winShift, Keys.Escape, 
        () => context.Enabled = !context.Enabled, "toggle enabled/disabled");
    manager.Subscribe(winShift, Keys.I, 
        () => context.ToggleConsoleWindow(), "toggle console window");

};
return doConfig;