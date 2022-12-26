#r "C:\Program Files\workspacer\workspacer.Shared.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Bar\workspacer.Bar.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Gap\workspacer.Gap.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.TitleBar\workspacer.TitleBar.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.ActionMenu\workspacer.ActionMenu.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.FocusIndicator\workspacer.FocusIndicator.dll"

using System;
using System.Windows.Forms;
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

public class BatteryWidget : BarWidgetBase
{
    public Color LowChargeColor { get; set; } = new Color(250, 55, 55);
    public Color MedChargeColor { get; set; } = new Color(252,185, 15);
    public double LowChargeThreshold { get; set; } = 0.20;
    public double MedChargeThreshold { get; set; } = 0.50;
    public int Interval { get; set; } = 5000;
    private System.Timers.Timer _timer;

    public override IBarWidgetPart[] GetParts()
    {
        PowerStatus pwr = SystemInformation.PowerStatus;
        float currentBatteryCharge = pwr.BatteryLifePercent;
        if (currentBatteryCharge <= LowChargeThreshold)
        {
            return Parts(Part(currentBatteryCharge.ToString("#0%")+" ", LowChargeColor, fontname: FontName));
        }
        else if (currentBatteryCharge <= MedChargeThreshold)
        {
            return Parts(Part(currentBatteryCharge.ToString("#0%")+" ", MedChargeColor, fontname: FontName));
        }
        else
        {
            return Parts(Part(currentBatteryCharge.ToString("#0%")+" ", fontname: FontName));
        }
    }

    public override void Initialize()
    {
        _timer = new System.Timers.Timer(Interval);
        _timer.Elapsed += (s, e) => MarkDirty();
        _timer.Enabled = true;
    }
}

public class ActiveLayoutWidget : BarWidgetBase
{
    private System.Timers.Timer _timer;
    public ActiveLayoutWidget() { }

    public override IBarWidgetPart[] GetParts()
    {
        string icon = "";
        var currentWorkspace = Context.WorkspaceContainer.GetWorkspaceForMonitor(Context.Monitor);
        if (String.Equals(currentWorkspace.LayoutName, "full")) {
            icon = "";
        }
        if (String.Equals(currentWorkspace.LayoutName, "tall")) {
            icon = "";
        }
        if (String.Equals(currentWorkspace.LayoutName, "dwindle")) {
            icon = "";
        }

        return Parts(Part(LeftPadding + icon + RightPadding, partClicked: () =>
        {
            Context.Workspaces.FocusedWorkspace.NextLayoutEngine();
        }, fontname: FontName));
    }

    public override void Initialize()
    {
        _timer = new System.Timers.Timer(200);
        _timer.Elapsed += (s, e) => MarkDirty();
        _timer.Enabled = true;
    }
}

public class StatusWidget: BarWidgetBase
{
    private System.Timers.Timer _timer;
    private string _text;
    IConfigContext _context;

    public StatusWidget(IConfigContext context)
    {
        _context = context;
    }
    public override IBarWidgetPart[] GetParts()
    {
        if (_context.Enabled) {
            _text = "";
        }
        else{
            _text = "";
        }
        return Parts(Part(_text,  partClicked: () =>_context.Enabled = !_context.Enabled, fontname: FontName));
    }

    public override void Initialize()
    {
        _timer = new System.Timers.Timer(200);
        _timer.Elapsed += (s, e) => MarkDirty();
        _timer.Enabled = true;
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
    private System.Timers.Timer _timer;
    private System.Timers.Timer _blinkTimer;
    private int _pomodoroMinutes = 0;
    private int _minutesLeft = 0;
    private Boolean _overtimeBackgroundOn;
    private Color _overtimeBackgroundColor;
    private string _incentive;


    public PomodoroWidget(int pomodoroMinutes = 50, Color overtimeBackgroundColor = null, string incentive = "")
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
            return " " + _minutesLeft.ToString() + " m";
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
        _timer = new System.Timers.Timer(60000);
        _timer.Elapsed += (s,e) => {
            _minutesLeft -= 1;
            if (_minutesLeft <= 0) {
                _blinkTimer.Start();
            }
            MarkDirty();
        };
        _timer.Stop();

        _blinkTimer = new System.Timers.Timer(1000);
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
    var fontSize = 10;
    var barHeight = 22;
    var fontName = "FiraCode Nerd Font Mono";
    var background = new Color(25, 25, 25);
    var foreground = new Color(255, 255, 255);

/* Config */
    context.CanMinimizeWindows = true;

/* Title Bar*/
    var titleBarPluginConfig = new TitleBarPluginConfig(new TitleBarStyle(showTitleBar: false, showSizingBorder: false));
    context.AddTitleBar(titleBarPluginConfig);

/* Gap */
    var gap = barHeight - 8;
    var gapPlugin = context.AddGap(new GapPluginConfig() { InnerGap = gap, OuterGap = gap / 2, Delta = gap / 2 });

/* Bar */  
     var leftWidgets = () => new IBarWidget[]
    {
        new TextWidget("|"),
        new WorkspaceWidget()
        {
            WorkspaceHasFocusColor = new Color(55, 120, 246),
            WorkspaceEmptyColor = new Color(60, 67, 87),
            WorkspaceIndicatingBackColor = new Color(55, 91, 169),
        },
    };

    var rightWidgets = () => new IBarWidget[]
    {
        new PomodoroWidget(),
        new TextWidget("|"),
        new HourIconWidget(),
        new TimeWidget(1000, "HH:mm"),
        new TextWidget("|"),
        new BatteryWidget(),
        new TextWidget("|"),
        new ActiveLayoutWidget(),
        new TextWidget("|"),
        new StatusWidget(context),
        new TextWidget("|"),
    };
    context.AddBar(new BarPluginConfig()
    {
        FontSize = fontSize,
        BarHeight = barHeight,
        FontName = fontName,
        DefaultWidgetBackground = background,
        DefaultWidgetForeground = foreground,
        LeftWidgets =leftWidgets,
        RightWidgets = rightWidgets,
    });
/* Bar focus indicator */
    context.AddFocusIndicator( new FocusIndicatorPluginConfig() {
        BorderColor = background,
        BorderSize = 10,
        TimeToShow = 1000,
    });

/* Default layouts */
    var defaultLayouts = () => new ILayoutEngine[]
    {
        new DwindleLayoutEngine(),
        new TallLayoutEngine(),
        new FullLayoutEngine(),
    };
    context.DefaultLayouts = defaultLayouts;
    context.WorkspaceContainer.CreateWorkspaces("", "", "", "");
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
    manager.Subscribe(win, workspacer.Keys.Escape,
        () => context.Enabled = !context.Enabled, "toggle enable/disable");
    
    // Workspace
    manager.Subscribe(win, workspacer.Keys.D1, 
        () => context.Workspaces.SwitchToWorkspace(0), "switch to workspace 1");
    manager.Subscribe(win, workspacer.Keys.D2, 
        () => context.Workspaces.SwitchToWorkspace(1), "switch to workspace 2");
    manager.Subscribe(win, workspacer.Keys.D3, 
        () => context.Workspaces.SwitchToWorkspace(2), "switch to workspace 3");
    manager.Subscribe(win, workspacer.Keys.D4, 
        () => context.Workspaces.SwitchToWorkspace(3), "switch to workspace 4");
    manager.Subscribe(win, workspacer.Keys.D5, 
        () => context.Workspaces.SwitchToWorkspace(4), "switch to workspace 5");

    manager.Subscribe(winShift, workspacer.Keys.D1, 
        () => context.Workspaces.MoveFocusedWindowToWorkspace(0), "switch focused window to workspace 1");
    manager.Subscribe(winShift, workspacer.Keys.D2, 
        () => context.Workspaces.MoveFocusedWindowToWorkspace(1), "switch focused window to workspace 2");
    manager.Subscribe(winShift, workspacer.Keys.D3, 
        () => context.Workspaces.MoveFocusedWindowToWorkspace(2), "switch focused window to workspace 3");
    manager.Subscribe(winShift, workspacer.Keys.D4, 
        () => context.Workspaces.MoveFocusedWindowToWorkspace(3), "switch focused window to workspace 4");
    manager.Subscribe(winShift, workspacer.Keys.D5, 
        () => context.Workspaces.MoveFocusedWindowToWorkspace(4), "switch focused window to workspace 5");

    // H, L keys
    manager.Subscribe(winShift, workspacer.Keys.H, 
        () => context.Workspaces.FocusedWorkspace.ShrinkPrimaryArea(), "shrink primary area");
    manager.Subscribe(winShift, workspacer.Keys.L, 
        () => context.Workspaces.FocusedWorkspace.ExpandPrimaryArea(), "expand primary area");

    // K, J keys
    manager.Subscribe(winShift, workspacer.Keys.K, 
        () => context.Workspaces.FocusedWorkspace.SwapFocusAndNextWindow(), "swap focus and next window");
    manager.Subscribe(winShift, workspacer.Keys.J, 
        () => context.Workspaces.FocusedWorkspace.SwapFocusAndPreviousWindow(), "swap focus and previous window");
    manager.Subscribe(win, workspacer.Keys.K, 
        () => context.Workspaces.FocusedWorkspace.FocusNextWindow(), "focus next window");
    manager.Subscribe(win, workspacer.Keys.J, 
        () => context.Workspaces.FocusedWorkspace.FocusPreviousWindow(), "focus previous window");

    // Other shortcuts
    manager.Subscribe(winShift, workspacer.Keys.P, 
        () => actionMenu.ShowDefault(), "open action menu");
    manager.Subscribe(winShift, workspacer.Keys.Escape, 
        () => context.Enabled = !context.Enabled, "toggle enabled/disabled");
    manager.Subscribe(winShift, workspacer.Keys.I, 
        () => context.ToggleConsoleWindow(), "toggle console window");

};
return doConfig;
