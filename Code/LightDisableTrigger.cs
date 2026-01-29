using Sandbox;

public sealed class LightDisableTrigger : Component, Component.ITriggerListener
{
	[Property] public GameObject lightObject { get; set; }
	public void OnTriggerExist( Collider other )
	{
		lightObject.Enabled = false;
	}
}
