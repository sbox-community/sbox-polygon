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
            if (Host.IsClient) 
                if (scoreboardPanel == null || !scoreboardPanel.IsValid())
                    scoreboardPanel = new Scoreboard3D(this);
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

            PanelBounds = new Rect(-1300, -3000, 2500, 2400);
            Style.BackgroundColor = new Color(0f, 0f, 0f, 0.85f);

            scoreboardChildMainPanel = Add.Panel();
            scoreboardChildMainPanel.Style.FlexDirection = FlexDirection.Column;
            scoreboardChildMainPanel.Style.Width = Length.Fraction(1f);
            scoreboardChildMainPanel.Style.Height = Length.Fraction(1f);
            scoreboardChildMainPanel.Style.FlexShrink = 0;

            var header = scoreboardChildMainPanel.Add.Panel();
            
            header.Style.Width = Length.Fraction(1f);
            header.Style.Height = Length.Fraction(0.08f);
            header.Style.BackgroundColor = Color.Magenta;
            header.Style.JustifyContent = Justify.Center;

            var headertext = header.AddChild<Label>();
            headertext.Text = "GLOBAL RECORDS";
            headertext.Style.TextStrokeWidth = Length.Pixels(8f);
            headertext.Style.TextStrokeColor = Color.Black;
            headertext.Style.FontSize = Length.Pixels(72);
            headertext.Style.FontColor = Color.White;
            headertext.Style.FontFamily = "Roboto";

            scoresPanel = scoreboardChildMainPanel.Add.Panel();
            scoresPanel.Style.Width = Length.Fraction(1f);
            scoresPanel.Style.Height = Length.Fraction(1f);
            scoresPanel.Style.FlexDirection = FlexDirection.Column;

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
            tf.Position = tf.Position + (tf.Rotation.Forward* 2f);
            tf.Rotation = Rotation.From(tf.Rotation.Angles()+(new Angles(0,0,0))); //+(new Angles(0,180f,0)

            Transform = tf;
        }
        public void refreshScores()
        {
            scoresPanel.DeleteChildren();

            if (PolygonHUD.globalRecords == null)
            {
                _ = PolygonHUD.Timer(1000, () => refreshScores());

                var norecord = scoresPanel.AddChild<Label>();
                norecord.Style.MarginTop = Length.Percent(35);
                norecord.Text = "No Records Found";
                norecord.Style.FontFamily = "Roboto";
                norecord.Style.FontColor = new Color(1, 1, 1, 0.35f);
                norecord.Style.FontSize = Length.Pixels(80);
                norecord.Style.TextAlign = TextAlign.Center;
                norecord.Style.Set("text-shadow: 0 0 2px #000000;");

                return;
            }

            var i = 0;

            var rowpanelinfo = scoresPanel.Add.Panel();
            rowpanelinfo.Style.JustifyContent = Justify.SpaceBetween;
            rowpanelinfo.Style.Height = Length.Percent(7f);
            rowpanelinfo.Style.Margin = Length.Pixels(50f);

            var rowpanelinforank = rowpanelinfo.AddChild<Label>();
            rowpanelinforank.Text = "RANK";
            rowpanelinforank.Style.FontColor = Color.White;
            rowpanelinforank.Style.Width = Length.Fraction(0.2f);
            rowpanelinforank.Style.FontSize = Length.Pixels(68);
            rowpanelinforank.Style.FontFamily = "Roboto";
            rowpanelinforank.Style.TextAlign = TextAlign.Center;
            rowpanelinforank.Style.Set("text-shadow: 0 0 8px #000000;");

            var rowpanelinfoname = rowpanelinfo.AddChild<Label>();
            rowpanelinfoname.Style.Width = Length.Fraction(0.4f);
            rowpanelinfoname.Text = "NAME";
            rowpanelinfoname.Style.FontColor = Color.White;
            rowpanelinfoname.Style.FontSize = Length.Pixels(68);
            rowpanelinfoname.Style.FontFamily = "Roboto";
            rowpanelinfoname.Style.TextAlign = TextAlign.Center;
            rowpanelinfoname.Style.Set("text-shadow: 0 0 8px #000000;");

            var rowpanelinfoscore = rowpanelinfo.AddChild<Label>();
            rowpanelinfoscore.Style.Width = Length.Fraction(0.3f);
            rowpanelinfoscore.Text = "SCORE";
            rowpanelinfoscore.Style.FontColor = Color.White;
            rowpanelinfoscore.Style.FontSize = Length.Pixels(68);
            rowpanelinfoscore.Style.FontFamily = "Roboto";
            rowpanelinfoscore.Style.TextAlign = TextAlign.Center;
            rowpanelinfoscore.Style.Set("text-shadow: 0 0 8px #000000;");


            foreach (var data in PolygonHUD.globalRecords)
            {

                var rowpanel = scoresPanel.Add.Panel();
                rowpanel.Style.JustifyContent = Justify.SpaceBetween;
                rowpanel.Style.Height = Length.Percent(7f);
                rowpanel.Style.Margin = Length.Pixels(6f);
                rowpanel.Style.MarginLeft = Length.Pixels(70f);
                rowpanel.Style.MarginRight = Length.Pixels(70f);

                var color = i == 0 ? new Color(245f / 255f, 230f / 255f, 66f / 255f, 0.7f) : i == 1 ? new Color(186f / 255f, 186f / 255f, 186f / 255f, 0.4f) : i == 2 ? new Color(195f / 255f, 115f / 255f, 54f / 255f, 0.7f) : new Color(1, 1, 1, 0.7f);

                var order = rowpanel.AddChild<Label>();
                order.Text = $"##{i+1}";
                order.Style.FontColor = color;
                order.Style.FontSize = Length.Pixels(68);
                order.Style.FontFamily = "Roboto";
                order.Style.TextAlign = TextAlign.Center;
                order.Style.Set("text-shadow: 0 0 8px #000000;");

                var name = rowpanel.AddChild<Label>();
                name.Text = $"{data.Name}";
                name.Style.FontColor = color;
                name.Style.FontSize = Length.Pixels(68);
                name.Style.FontFamily = "Roboto";
                name.Style.TextAlign = TextAlign.Center;
                name.Style.Set("text-shadow: 0 0 8px #000000;");

                var score = rowpanel.AddChild<Label>();
                score.Text = $"{data.Score/1000f} sec";
                score.Style.FontColor = color;
                score.Style.FontSize = Length.Pixels(68);
                score.Style.FontFamily = "Roboto";
                score.Style.TextAlign = TextAlign.Center;
                score.Style.Set("text-shadow: 0 0 8px #000000;");

                i++;
            }

            if (PolygonHUD.globalRecords.Length == 0)
            {
                var norecord = scoresPanel.AddChild<Label>();
                norecord.Style.MarginTop = Length.Percent(35);
                norecord.Text = "No Records Found";
                norecord.Style.FontFamily = "Roboto";
                norecord.Style.FontColor = new Color(1, 1, 1, 0.35f);
                norecord.Style.FontSize = Length.Pixels(80);
                norecord.Style.TextAlign = TextAlign.Center;
                norecord.Style.Set("text-shadow: 0 0 2px #000000;");
            }

        }
    }
}
