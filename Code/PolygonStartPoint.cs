using Sandbox;
using Sandbox.Utility;

public sealed class PolygonStartPoint : Component
{
	private static Model Model = Model.Load( "models/editor/spawnpoint.vmdl" );
	protected override void DrawGizmos()
	{
		Gizmo.Hitbox.Model( Model );
		Gizmo.Draw.Color = Color.Green.WithAlpha( (Gizmo.IsHovered || Gizmo.IsSelected) ? 0.7f : 0.5f );

		var so = Gizmo.Draw.Model( Model );

		if ( so is not null )
		{
			so.Flags.CastShadows = true;
		}
	}
}
