using SandboxEditor;
using Sandbox.UI;
using static Sandbox.Package;

namespace Sandbox
{
	[Library( "ent_polygon_scoreboard" )]
	[HammerEntity]
    [EditorModel("models/walls/corrugated_wall_a_128.vmdl")]
	[VisGroup( VisGroup.Dynamic )]//RenderFields,
    [Title( "Polygon Scoreboard" ), Category( "Gameplay" ), Icon( "door_front" )]
	public partial class PolygonScoreboard : ModelEntity
    {
        public static Scoreboard3D scoreboardPanel;

        public PolygonScoreboard()
        {
            if (Host.IsClient) { 
                if (scoreboardPanel != null && scoreboardPanel.IsValid())
                {
                    scoreboardPanel.Delete();
                    scoreboardPanel = null;
                }
                scoreboardPanel = new Scoreboard3D(this);
            }
        }
        ~PolygonScoreboard()
        {
            if(scoreboardPanel != null)
            {
                scoreboardPanel.Delete();
                scoreboardPanel = null;
            }
        }
        public static void refreshScores()
        {
            if (scoreboardPanel != null && scoreboardPanel.IsValid())
                scoreboardPanel.refreshScores();
        }
    }

    public partial class Scoreboard3D : WorldPanel
    {

        private Panel scoreboardChildMainPanel;
        private Panel scoresPanel;
        private Entity parentEntity;

        public Scoreboard3D(Entity parent){

            parentEntity = parent;

            PanelBounds = new Rect(-1300, -3000, 2500, 3000);
            Style.Width = Length.Pixels(10000f);
            Style.Height = Length.Pixels(500f);
            Style.BackgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);

            scoreboardChildMainPanel = Add.Panel();
            scoreboardChildMainPanel.Style.FlexShrink = 0;

            var header = scoreboardChildMainPanel.Add.Panel();
            header.Style.Width = Length.Fraction(1f);
            header.Style.Height = Length.Fraction(0.07f);
            header.Style.BackgroundColor = Color.Magenta;

            var headertext = header.AddChild<Label>();
            headertext.Text = "GLOBAL RECORDS";
            header.Style.JustifyContent = Justify.Center;

            headertext.Style.FontSize = Length.Pixels(64);
            headertext.Style.FontColor = Color.White;

            headertext.Style.FontFamily = "Roboto";

            scoresPanel = Add.Panel();
            scoresPanel.Style.FlexDirection = FlexDirection.Row;
            scoresPanel.Style.FlexShrink = 0;

            refreshScores();
        }

        ~Scoreboard3D(){
            if (scoreboardChildMainPanel != null)
            {
                scoreboardChildMainPanel.Delete();
                scoreboardChildMainPanel = null;
            }       
            if (scoresPanel != null)
            {
                scoresPanel.Delete();
                scoresPanel = null;
            }
        }

        [Event.PreRender]
        public void FrameUpdate()
        {
            if (parentEntity == null || !parentEntity.IsValid())
                return;

            var tf = parentEntity.Transform;
            tf.Rotation = Rotation.From(tf.Rotation.Angles()+(new Angles(0,0,0))); //+(new Angles(0,180f,0)

            Transform = tf;
        }
        public void refreshScores()
        {
            if (PolygonHUD.globalRecords.Entries == null)
            {
                _ = PolygonHUD.Timer(1000, () => refreshScores());
                return;
            }

            scoresPanel.DeleteChildren();

            var i = 0;

            foreach (var data in PolygonHUD.globalRecords.Entries)
            {
                var rowpanel = scoresPanel.Add.Panel();
                rowpanel.Style.JustifyContent = Justify.SpaceBetween;
                rowpanel.Style.Width = Length.Percent(100);
                rowpanel.Style.Margin = Length.Percent(0.5f);
                rowpanel.Style.FlexShrink = 0;

                var color = i == 0 ? new Color(245f / 255f, 230f / 255f, 66f / 255f, 0.7f) : i == 1 ? new Color(186f / 255f, 186f / 255f, 186f / 255f, 0.7f) : i == 2 ? new Color(195f / 255f, 115f / 255f, 54f / 255f, 0.7f) : new Color(1, 1, 1, 0.7f);
                var score = rowpanel.AddChild<Label>();
                score.Text = $"{data.Rating} sec";
                score.Style.FontColor = color;
                score.Style.FontSize = Length.Pixels(18);
                score.Style.FontFamily = "Roboto";
                score.Style.TextAlign = TextAlign.Center;
                score.Style.Set("text-shadow: 0 0 2px #000000;");
                score.Style.Top = Length.Percent(-45);

                var name = rowpanel.AddChild<Label>();
                name.Text = $"{data.DisplayName}";
                name.Style.FontColor = color;
                name.Style.FontSize = Length.Pixels(14);
                name.Style.FontFamily = "Roboto";
                name.Style.TextAlign = TextAlign.Center;
                name.Style.Set("text-shadow: 0 0 2px #000000;");
                name.Style.Top = Length.Percent(-45);

                i++;
            }

            if (PolygonHUD.globalRecords.Entries.Count == 0)
            {
                var norecord = scoresPanel.AddChild<Label>();
                norecord.Style.Margin = Length.Percent(20);
                norecord.Text = "No Records Found";
                norecord.Style.FontFamily = "Roboto";
                norecord.Style.FontColor = new Color(1, 1, 1, 0.2f);
                norecord.Style.FontSize = Length.Pixels(18);
                norecord.Style.TextAlign = TextAlign.Center;
                norecord.Style.Set("text-shadow: 0 0 2px #000000;");
            }

        }
    }
}
