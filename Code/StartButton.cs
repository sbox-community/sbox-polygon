public sealed class StartButton : Component, Component.IPressable
{
	public bool Press( IPressable.Event e )
	{
		return PolygonGame.StartPolygon( e.Source.GameObject );
	}
}
