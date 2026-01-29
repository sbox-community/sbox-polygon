using Sandbox;

public sealed class TargetMovementTrigger : Component, Component.ITriggerListener
{

	[Property] public Vector3 targetPos { get; set; } = Vector3.Zero;
	public void OnTriggerEnter( Collider other )
	{
		if ( other.GameObject.Tags.Has( "polygonplayer" ) && targetPos != Vector3.Zero && GameObject.Parent.GetComponent<Target>() is { } target )
			target.SetMotionPos( targetPos, other.GameObject.Parent );
	}
}
