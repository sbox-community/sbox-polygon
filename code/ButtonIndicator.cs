using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;
using static Sandbox.Event.Entity;

public partial class ButtonIndicator : Panel
{

    private List<Vector3> indicatorPosition = new();
    private List<Entity> indicatorEnt = new();
    private static List<ButtonIndicator> panels = new();
    private ushort step = 0;
    public ButtonIndicator()
    {
        foreach (var ent in PolygonWeapon.All)
        {
            if (ent as PolygonWeapon is not PolygonWeapon)
                continue;

            indicatorPosition.Add(ent.WorldSpaceBounds.Center);
            indicatorEnt.Add(ent);
            break;
        }

        foreach (var ent in Entity.All)
        {
            if (ent.Name == "polygon_start")
            {
                indicatorPosition.Add(ent.WorldSpaceBounds.Center);
                indicatorEnt.Add(ent);
                break;
            }
        }

        if (indicatorEnt.Count > 1)
        {
            Parent = Local.Hud; // parent must be rootpanel
            Style.Position = PositionMode.Absolute;

            Style.BackgroundImage = Texture.Load(FileSystem.Mounted, "materials/polygon/indicator_pointer.png");
            Style.Width = Length.Pixels(80f);
            Style.Height = Length.Pixels(80f);
            Style.BackgroundSizeX = Length.Cover;
            Style.BackgroundSizeY = Length.Cover;
            Style.BackgroundRepeat = BackgroundRepeat.NoRepeat;

            var pos = indicatorPosition[0].ToScreen();
            Local.Pawn.PlaySound("confirmation_001").SetPitch(1.5f);

            panels.Add(this);
        }
        else
            Remove();
    }
    public static void Think()
    {
        if (panels.Count == 0)
            return;

        foreach (var panel in panels)
        {
            var screenpos = (panel.indicatorPosition[panel.step].ToScreen()).Clamp(0f, 0.95f);
            panel.Style.Left = Length.Fraction(screenpos.x);
            panel.Style.Top = Length.Fraction(screenpos.y);
            panel.Style.FilterBrightness = MathF.Abs(MathF.Sin(Time.Now * 6f));
            panel.Style.BackgroundSizeX = Length.Fraction(MathF.Abs(MathF.Sin(Time.Now * 3f)).Clamp(0.9f, 1f));
            panel.Style.BackgroundSizeY = Length.Fraction(MathF.Abs(MathF.Sin(Time.Now * 3f)).Clamp(0.9f, 1f));
        }
    }
    public static void Check(Entity ent)
    {
        if (panels.Count == 0)
            return;

        foreach (var panel in panels)
            if (panel.indicatorEnt[panel.step] == ent || (panel.step != 0 && ent.Name == "polygon_start"))
            {
                if (panel.step == 0)
                {
                    panel.step++;
                    Local.Pawn.PlaySound("confirmation_001").SetPitch(1.5f);
                }
                else
                {
                    panel.Remove();
                    Local.Pawn.PlaySound("confirmation_001").SetPitch(2.5f);
                }
                break;
            }
    }
    public void Remove()
    {
        if (this != null && this.IsValid())
        {
            panels.Remove(this);
            Delete();
        }
    }

    [PreCleanup]
    public static void RemoveAll()
    {
        if (panels.Count == 0)
            return;

        for (var i = 0; panels.Count > i; i++)
            panels[i].Remove();
    }
}
