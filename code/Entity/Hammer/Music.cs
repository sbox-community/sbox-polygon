using SandboxEditor;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using SWB_Base;

namespace Sandbox
{
    [Library("ent_polygon_music")]
    [HammerEntity]
    [Title("Polygon Music"), Category("Gameplay"), Icon("door_front")]
    public partial class PolygonMusic : Entity {
        /// <summary>
        /// The music when starts the polygon race.
        /// </summary>
        [Property("music", Title = "Music"), FGDType("sound"), Category("Sounds")]
        [JsonPropertyName("music")]
        public string Music { get; set; } = "";
    }
}
