using Sandbox;

public sealed class LightEnableTrigger : Component, Component.ITriggerListener
{
	[Property] public GameObject lightObject { get; set; }
	public void OnTriggerExist( Collider other )
	{
		lightObject.Enabled = true;
	}
}
