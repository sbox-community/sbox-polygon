using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

public partial class PolygonHUD : Panel
{
    private static Label counter;
    private static Panel activeMenu;
    private static Panel scoreboard;
    public static long startInfoActive = 0;
    public static Label startInfoPanel;
    private string beforeTime = "";
    private static Label highRecord;

    public PolygonHUD()
    {
        welcomeMenu();
    }

    public static Panel buildMenu(Panel root, int width = 15, int height = 20, bool enable_closebutton = true)
    {
        if(activeMenu!=null)
        {
            activeMenu.Delete();
            activeMenu = null;
        }

        var background = root.Add.Panel();
        activeMenu = background;
        background.Style.Position = PositionMode.Absolute;
        background.Style.Width = Length.Percent(100);
        background.Style.Height = Length.Percent(100);
        background.Style.BackgroundColor = new Color(0, 0, 0, 0.35f);
        background.Style.BackdropFilterBlur = Length.Pixels(5);
        background.Style.AlignContent = Align.Center;
        background.Style.JustifyContent = Justify.Center;
        background.Style.AlignItems= Align.Center;
        background.Style.PointerEvents = "all";

        var p = background.Add.Panel();
        p.Style.Position = PositionMode.Absolute;
        p.Style.Width = Length.Percent(width);
        p.Style.Height = Length.Percent(height);
        p.Style.AlignContent = Align.Center;
        p.Style.JustifyContent = Justify.Center;
        p.Style.FlexDirection = FlexDirection.Column;
        p.Style.BorderWidth = Length.Pixels(1);
        p.Style.BorderColor = new Color(0, 0, 0, 0.3f);
        p.Style.Set("background: linear-gradient(to bottom, rgba(20,20,20,1) 0%, rgba(56,56,56,1) 100%);box-shadow: 0px 0px 20px 0px rgb(0, 0, 0);transition: all 0.1s ease-out;transform: scale(0.9);");

        //animation
        _ = Timer(1, () => {
            if(activeMenu != null && p != null)
                p.Style.Set("transition: all 0.1s ease-out;transform: scale(1);");
        });

        if (enable_closebutton)
        { 
            var b = p.AddChild<Button>();
            b.Style.AlignItems = Align.Center;
            b.Style.JustifyContent = Justify.Center;
            b.Style.FontColor = new Color(1, 1, 1, 0.5f);
            b.Style.FontSize = Length.Pixels(14);
            b.Style.Margin = Length.Percent(1);
            b.Style.MarginLeft = Length.Percent(35);
            b.Style.MarginRight = Length.Percent(35);
            b.Style.BorderWidth = Length.Pixels(1);
            b.Style.BorderColor = Color.Black;
            b.Style.Padding = Length.Percent(1);
            b.SetText("OK");
            b.Style.FlexShrink = 0;
            b.Style.Order = 999;
            b.Style.Set("background: linear-gradient(to bottom, rgba(20,20,20,1) 0%, rgba(56,56,56,1) 100%);text-shadow: 0 0 2px #000000;");
            b.AddEventListener("onclick", () => { activeMenu.Delete(); (Local.Pawn as PolygonPlayer).PlaySound("swb_hitmarker").SetVolume(0.1f).SetPitch(1.3f); });
        }
        return p;
    }

    public static void infoMenu(ref string info)
    {
        var p = buildMenu(Local.Hud);

        var infolabel = p.AddChild<Label>();
        infolabel.Text = info;
        infolabel.Style.FontColor = new Color(1,1,1,0.7f);
        infolabel.Style.FontSize = Length.Pixels(14);
        infolabel.Style.Margin = Length.Percent(5);
        infolabel.Style.FontFamily = "Roboto";
        infolabel.Style.TextAlign = TextAlign.Center;
        infolabel.Style.AlignItems = Align.Center;
        infolabel.Style.Set("text-shadow: 0 0 2px #000000;");

    }

    public static void welcomeMenu()
    {
        var p = buildMenu(Local.Hud, width:20);

        var label = p.AddChild<Label>();
        label.Text = "Welcome to Polygon Gamemode!\n\nAll you have to do is take a weapon and press 'E' the start button!";
        label.Style.FontColor = new Color(1, 1, 1, 0.7f);
        label.Style.FontSize = Length.Pixels(14);
        label.Style.Margin = Length.Percent(5);
        label.Style.FontFamily = "Roboto";
        label.Style.TextAlign = TextAlign.Center;
        label.Style.Set("text-shadow: 0 0 2px #000000;");

    }

    public static void scoreboardBuild()
    {
        scoreboard = buildMenu(Local.Hud, width: 50, height: 35, enable_closebutton: false);
        scoreboard.Style.FlexDirection = FlexDirection.Row;

        var localscores = scoreboard.Add.Panel();
        localscores.Style.FlexGrow = 0;
        localscores.Style.Width = Length.Percent(100);
        localscores.Style.Margin = Length.Percent(2);
        localscores.Style.MarginLeft = Length.Percent(2);
        localscores.Style.MarginRight = Length.Percent(2);
        localscores.Style.AlignContent = Align.Center;
        localscores.Style.AlignItems = Align.Center;
        localscores.Style.FlexDirection = FlexDirection.Column;

        var localscorelabel = localscores.AddChild<Label>();
        localscorelabel.Text = "Local Records";
        localscorelabel.Style.FlexShrink = 0;
        localscorelabel.Style.FontColor = new Color(1, 1, 1, 0.7f);
        localscorelabel.Style.FontSize = Length.Pixels(24);
        localscorelabel.Style.Margin = Length.Percent(2);
        localscorelabel.Style.MarginBottom = Length.Percent(6);
        localscorelabel.Style.FontFamily = "Roboto";
        localscorelabel.Style.TextAlign = TextAlign.Center;
        localscorelabel.Style.Set("text-shadow: 0 0 2px #000000;");

        var i = 0;

        foreach(var data in (Local.Pawn as PolygonPlayer).LocalScores)
        {
            var rowpanel = localscores.Add.Panel();
            rowpanel.Style.JustifyContent = Justify.SpaceBetween;
            rowpanel.Style.Width = Length.Percent(100);
            rowpanel.Style.Margin = Length.Percent(0.5f);
            rowpanel.Style.FlexShrink = 0;

            //not working, because of engine command
            rowpanel.AddEventListener("onclick", () => { ConsoleSystem.Run($"demo {data.demoid}"); });

            var color = i == 0 ? new Color(245f / 255f, 230f / 255f, 66f / 255f, 0.7f) : i == 1 ? new Color(186f / 255f, 186f / 255f, 186f / 255f, 0.7f) : i == 2 ? new Color(195f / 255f, 115f / 255f, 54f / 255f, 0.7f) : new Color(1, 1, 1, 0.7f);
            var score = rowpanel.AddChild<Label>();
            score.Text = $"{data.score} sec";
            score.Style.FontColor = color;
            score.Style.FontSize = Length.Pixels(18);
            score.Style.FontFamily = "Roboto";
            score.Style.TextAlign = TextAlign.Center;
            score.Style.Set("text-shadow: 0 0 2px #000000;");
            score.Style.Top = Length.Percent(-45);

            var date = rowpanel.AddChild<Label>();
            date.Text = $"{DateTimeOffset.FromUnixTimeSeconds(data.date).LocalDateTime.ToString("HH:mm:ss  (MM/dd/yyyy)")}";
            date.Style.FontColor = color.WithAlpha(0.5f);
            date.Style.FontSize = Length.Pixels(16);
            date.Style.FontFamily = "Roboto";
            date.Style.TextAlign = TextAlign.Center;
            date.Style.Set("text-shadow: 0 0 2px #000000;");
            date.Style.Top = Length.Percent(-45);
            i++;

        }

        var seperator = scoreboard.Add.Panel();
        seperator.Style.FlexGrow = 0;
        seperator.Style.Width = Length.Percent(1);
        seperator.Style.Height = Length.Percent(100);
        seperator.Style.BackgroundColor = new Color(0, 0, 0, 0.3f);

        var serverscores = scoreboard.Add.Panel();//it can be global scores
        serverscores.Style.FlexGrow = 0;
        serverscores.Style.Width = Length.Percent(100);
        serverscores.Style.Margin = Length.Percent(2);
        serverscores.Style.MarginLeft = Length.Percent(2);
        serverscores.Style.MarginRight = Length.Percent(2);
        serverscores.Style.AlignContent = Align.Center;
        serverscores.Style.AlignItems = Align.Center;
        serverscores.Style.FlexDirection = FlexDirection.Column;

        var serverscorelabel = serverscores.AddChild<Label>();
        serverscorelabel.Text = "Server Records";
        serverscorelabel.Style.FlexShrink = 0;
        serverscorelabel.Style.FontColor = new Color(1, 1, 1, 0.7f);
        serverscorelabel.Style.FontSize = Length.Pixels(24);
        serverscorelabel.Style.Margin = Length.Percent(2);
        serverscorelabel.Style.MarginBottom = Length.Percent(6);
        serverscorelabel.Style.FontFamily = "Roboto";
        serverscorelabel.Style.TextAlign = TextAlign.Center;
        serverscorelabel.Style.Set("text-shadow: 0 0 2px #000000;");

        i = 0;

        foreach (var data in (Game.Current as PolygonGame).top10)
        {
            var rowpanel = serverscores.Add.Panel();
            rowpanel.Style.JustifyContent = Justify.SpaceBetween;
            rowpanel.Style.Width = Length.Percent(100);
            rowpanel.Style.Margin = Length.Percent(0.5f);
            rowpanel.Style.FlexShrink = 0;

            var color = i == 0 ? new Color(245f / 255f, 230f / 255f, 66f / 255f, 0.7f) : i == 1 ? new Color(186f / 255f, 186f / 255f, 186f / 255f, 0.7f) : i == 2 ? new Color(195f / 255f, 115f / 255f, 54f / 255f, 0.7f) : new Color(1, 1, 1, 0.7f);
            var score = rowpanel.AddChild<Label>();
            score.Text = $"{data.score} sec";
            score.Style.FontColor = color;
            score.Style.FontSize = Length.Pixels(18);
            score.Style.FontFamily = "Roboto";
            score.Style.TextAlign = TextAlign.Center;
            score.Style.Set("text-shadow: 0 0 2px #000000;");
            score.Style.Top = Length.Percent(-45);

            var name = rowpanel.AddChild<Label>();
            name.Text = $"{data.name}";
            name.Style.FontColor = color;
            name.Style.FontSize = Length.Pixels(14);
            name.Style.FontFamily = "Roboto";
            name.Style.TextAlign = TextAlign.Center;
            name.Style.Set("text-shadow: 0 0 2px #000000;");
            name.Style.Top = Length.Percent(-45);

            var date = rowpanel.AddChild<Label>();
            date.Text = $"{DateTimeOffset.FromUnixTimeSeconds(data.date).LocalDateTime.ToString("HH:mm:ss  (MM/dd/yyyy)")}";
            date.Style.FontColor = color.WithAlpha(0.5f);
            date.Style.FontSize = Length.Pixels(16);
            date.Style.FontFamily = "Roboto";
            date.Style.TextAlign = TextAlign.Center;
            date.Style.Set("text-shadow: 0 0 2px #000000;");
            date.Style.Top = Length.Percent(-45);
            i++;
        }
    }
    public static void scoreboardRemove()
    {
        if (scoreboard != null)
        {
            if (scoreboard.Parent != null)
                scoreboard.Parent.Delete();
            scoreboard.Delete();
            scoreboard = null;
        }
    }

    public static void resultMenu(ref bool status,ref bool cheat, ref long time, ref string enemytarget, ref string friendlytarget)
    {
        if(highRecord != null)
        {
            highRecord.Delete();
            highRecord = null;
        }

        var p = buildMenu(Local.Hud);
        var statuslabel = p.AddChild<Label>();
        statuslabel.Text = status ? "Succeed" : "Failed";
        statuslabel.Style.TextAlign = TextAlign.Center;
        statuslabel.Style.AlignItems = Align.Center;
        statuslabel.Style.FontColor = status ? Color.Green : Color.Red;
        statuslabel.Style.FontSize = Length.Pixels(26);
        statuslabel.Style.Margin = Length.Percent(2.5f);
        statuslabel.Style.FontFamily = "Roboto";
        statuslabel.Style.Overflow = OverflowMode.Visible;

        if (status && (Local.Pawn as PolygonPlayer).isNewHighRecord(time / 1000f))
        {
            highRecord = statuslabel.AddChild<Label>();
            highRecord.Style.Left = Length.Fraction(0.24f);
            highRecord.Style.Top = Length.Fraction(0.345f);
            highRecord.Style.TransformOriginX = Length.Fraction(0.5f);
            highRecord.Style.TransformOriginY = Length.Fraction(0.5f);
            highRecord.Text = "High Record!";
            highRecord.Style.FontColor = Color.Red;
            var tf = new PanelTransform();
            tf.AddRotation(0, 0, -15);
            highRecord.Style.Transform = tf;
        }

        var timelabel = p.AddChild<Label>();
        timelabel.Text = $"Time: {DateTimeOffset.FromUnixTimeMilliseconds(time).LocalDateTime.ToString("mm:ss:fff")}";
        timelabel.Style.FontFamily = "Roboto";
        timelabel.Style.FontColor = new Color(1, 0.7f, 0.7f, 0.9f);
        timelabel.Style.FontSize = Length.Pixels(14);
        timelabel.Style.Margin = Length.Percent(3f);
        timelabel.Style.Set("text-shadow: 0 0 2px #000000;");
        timelabel.Style.TextAlign = TextAlign.Center;
        timelabel.Style.AlignItems = Align.Center;

    

        var enemytargetlabel = p.AddChild<Label>();
        enemytargetlabel.Text = $"Enemy Targets: {enemytarget}";
        enemytargetlabel.Style.FontColor = new Color(1, 1, 1, 0.7f);
        enemytargetlabel.Style.FontSize = Length.Pixels(14);
        enemytargetlabel.Style.Margin = Length.Percent(0.5f);
        enemytargetlabel.Style.Set("text-shadow: 0 0 2px #000000;");
        enemytargetlabel.Style.FontFamily = "Roboto";
        enemytargetlabel.Style.TextAlign = TextAlign.Center;
        enemytargetlabel.Style.AlignItems = Align.Center;

        var friendlytargetlabel = p.AddChild<Label>();
        friendlytargetlabel.Text = $"Friendly Targets: {friendlytarget}";
        friendlytargetlabel.Style.FontColor = new Color(1, 1, 1, 0.7f);
        friendlytargetlabel.Style.FontSize = Length.Pixels(14);
        friendlytargetlabel.Style.Margin = Length.Percent(0.5f);
        friendlytargetlabel.Style.Set("text-shadow: 0 0 2px #000000;");
        friendlytargetlabel.Style.FontFamily = "Roboto";
        friendlytargetlabel.Style.TextAlign = TextAlign.Center;
        friendlytargetlabel.Style.AlignItems = Align.Center;

        var cheatlabel = p.AddChild<Label>();
        cheatlabel.Text = $"Cheat: {(cheat ? "Found" : "Clear")}";
        cheatlabel.Style.FontSize = Length.Pixels(14);
        cheatlabel.Style.Margin = Length.Percent(3);
        cheatlabel.Style.Set("text-shadow: 0 0 2px #000000;");
        cheatlabel.Style.FontColor = cheat ? Color.Red : Color.White;
        cheatlabel.Style.TextAlign = TextAlign.Center;
        cheatlabel.Style.AlignItems = Align.Center;
        cheatlabel.Style.FontSize = Length.Pixels(12);
        cheatlabel.Style.FontFamily = "Roboto";

    }

    public static void startInfoPanelBuild()
    {
        startInfoPanel = Local.Hud.AddChild<Label>();
        startInfoPanel.Style.Top = Length.Percent(5);
        startInfoPanel.Style.Left = Length.Percent(48);
        startInfoPanel.Style.FontSize = Length.Pixels(100);
        startInfoPanel.Style.FontFamily = "Roboto";
        startInfoPanel.Style.TextAlign = TextAlign.Center;
        startInfoPanel.Style.FontColor = Color.White;
        startInfoPanel.Style.Set("text-shadow: 0px 0px 2px black;");
    }
    public static void timerPanelBuild()
    {
        counter = Local.Hud.AddChild<Label>();
        counter.Style.FlexGrow = 1;
        counter.Style.TextAlign = TextAlign.Center;
        counter.Style.JustifyContent = Justify.Center;
        counter.Style.AlignContent = Align.Center;
        counter.Style.AlignItems = Align.Center;
        counter.Style.Top = Length.Percent(-45);
        counter.Style.FontFamily = "Roboto";
        counter.Style.TransformOriginX = Length.Percent(50);
        counter.Style.TransformOriginY = Length.Percent(50);
        counter.Style.FontColor = Color.White;
        counter.Style.Set("text-shadow: 0px 0px 3px #000b;");

        (Local.Pawn as PolygonPlayer).PolygonMusic = (Local.Pawn as PolygonPlayer).PlaySound("mw2ostcliffhanger");//.SetVolume(0.2f);
    }
    
    public override void Tick()
    {
        base.Tick();
        if( startInfoActive != 0 )
        {
            float timeleft = startInfoActive - PolygonGame.curTime;
            var timeleftSTR = timeleft.ToString();
            startInfoPanel.Text = timeleftSTR;
            if(beforeTime != timeleftSTR)
            {
                beforeTime = timeleftSTR;
                if(beforeTime == "0")
                    Local.Pawn.PlaySound("counterclick_last").SetVolume(4f);
                else
                    Local.Pawn.PlaySound("counterclick").SetVolume(1.5f);
            }

            if (timeleft <= 0)
            {
                startInfoActive = 0;
                startInfoPanel.Delete();
                timerPanelBuild();
            }
            
        }
        else if((Local.Pawn as PolygonPlayer).InPolygon != 0)
        {
            if (counter == null)
                timerPanelBuild();

            var timeleft = PolygonGame.curTimeMS - (Local.Pawn as PolygonPlayer).InPolygon;
            var fontscale = 68f * MathF.Abs(MathF.Cos(Time.Now*4)).Remap(0f,1f,0.95f, 1.05f);

            counter.Text = $"{DateTimeOffset.FromUnixTimeMilliseconds(timeleft).LocalDateTime.ToString("mm:ss:fff")}";

            if( timeleft / 1000 > 30 )//after 30 sec, warn ply via the counter, maybe, it can be average finish time for per map
                counter.Style.Set("text-shadow: 0px 0px 20px #f20a;mix-blend-mode: lighten;");

            counter.Style.FontSize = Length.Pixels((float)fontscale);

        }
        else
        {
            if (counter!= null)
            {
                counter.Delete();
                counter = null;

                if(!(Local.Pawn as PolygonPlayer).PolygonMusic.Finished)
                    (Local.Pawn as PolygonPlayer).PolygonMusic.Stop();
            }
        }

        if(Input.Pressed(InputButton.Score))
            scoreboardBuild();
        else if(Input.Released(InputButton.Score))
            scoreboardRemove();

        if(highRecord!=null)
        {

            var fontscale = 10f * MathF.Abs(MathF.Cos(Time.Now * 4)).Remap(0f, 1f, 0.95f, 1.05f);
            highRecord.Style.FontSize = fontscale; 
        }

    }
    async static Task Timer(int s, Action callback)
    {
        await System.Threading.Tasks.Task.Delay(s);
        callback?.Invoke();
    }
}
