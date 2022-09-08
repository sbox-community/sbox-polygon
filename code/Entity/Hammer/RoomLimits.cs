using SandboxEditor;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using SWB_Base;

namespace Sandbox
{
	[Library( "ent_polygon_maxheight" )]
	[HammerEntity]//SupportsSolid
    [EditorModel("models/sbox_props/road_signs/sign_mid.vmdl")]
    //[Model( Archetypes = ModelArchetype.animated_model )]
	[VisGroup( VisGroup.Dynamic )]//RenderFields,
    [Title( "Polygon Max. Height" ), Category( "Gameplay" ), Icon( "door_front" )]
	public partial class PolygonMaxHeight : ModelEntity{}

    [Library("ent_polygon_minheight")]
    [HammerEntity]//SupportsSolid
    [EditorModel("models/sbox_props/road_signs/sign_mid.vmdl")]
    //[Model( Archetypes = ModelArchetype.animated_model )]
    [VisGroup(VisGroup.Dynamic)]//RenderFields,
    [Title("Polygon Min. Height"), Category("Gameplay"), Icon("door_front")]
    public partial class PolygonMinxHeight : ModelEntity {}
}
