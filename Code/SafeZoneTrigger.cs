using Sandbox;
using Sandbox.Diagnostics;

public sealed class SafeZoneTrigger : Component, Component.ITriggerListener
{
	public void OnTriggerExist( Collider other )
	{
		PolygonGame.GameInstance.CheatDetected( other.GameObject );
	}
}
