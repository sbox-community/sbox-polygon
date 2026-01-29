using Sandbox;
using Sandbox.Diagnostics;

public sealed class FinishTrigger : Component, Component.ITriggerListener
{
	public void OnTriggerEnter( Collider other )
	{
		if( other.GameObject.Tags.Has("polygonplayer"))
			PolygonGame.FinishPolygon( other.GameObject.Parent );
	}

}
