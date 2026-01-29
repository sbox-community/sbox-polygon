//From https://github.com/Facepunch/sbox-hc1 

public sealed class SoundEmitter : Component
{
	[Property] public string SoundString { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		Sound.Play( SoundString, WorldPosition);
	}
	protected override void OnUpdate()
	{

	}
}
