using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Services;
using Sandbox.Utility;
using System;
using static Sandbox.PhysicsContact;
using static Sandbox.Services.Inventory;

public sealed class Target : Component, Component.ExecuteInEditor, Component.IDamageable
{
	[Property] public bool isEnemy { get; set; } = false;
	private Vector3? motionPos = null;

	public void OnDamage( in DamageInfo damage )
	{
		if ( !PolygonGame.IsOwnerOfPolygon( damage.Attacker ) )
			return;

		var gibs = GameObject.GetComponent<Prop>().CreateGibs();
		foreach ( var gib in gibs )
		{
			gib.GameObject.Tags.Add( "targetgib" );

			var body = gib.GetComponent<Rigidbody>();
			var renderer = gib.GetComponent<ModelRenderer>();
			
			body.ApplyImpulse( body.Mass * damage.Damage * 0.5f + Vector3.Random * 100f + Vector3.Up * 200f);
			gib.FadeTime = 5;
			gib.MaterialGroup = isEnemy ? "enemy" : "friend";
			

			renderer.WorldScale *= Vector3.One + Vector3.Random; 

			if ( !IsProxy )
				gib.GameObject.NetworkSpawn();

		}

		PlayBreakSounds();

		if ( isEnemy )
		{
			//using ( Rpc.FilterInclude( c => c == PolygonGame.GetPolygonOwner().Network.Owner ) )
			{
				PlayBellSounds();
			}
		}

		PolygonGame.UpdateTargets( this, damage.Attacker );
		DestroyGameObject();
	}

	[Rpc.Broadcast( Flags = NetFlags.HostOnly | NetFlags.Reliable )]
	public void PlayBreakSounds()
	{
		Sound.Play( "break_wood_plank", WorldPosition );
	}

	[Rpc.Broadcast( Flags = NetFlags.HostOnly | NetFlags.Reliable )]
	public void PlayBellSounds()
	{
		Sound.Play( "bell_impact", GameObject.WorldPosition );
	}

	protected override void OnStart()
	{
		base.OnStart();

		if ( GetComponent<ModelRenderer>() is ModelRenderer model )
			model.MaterialGroup = isEnemy ? "enemy" : "friend";
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Scene.IsEditor )
		{
			if ( GetComponent<ModelRenderer>() is ModelRenderer model )
				model.MaterialGroup = isEnemy ? "enemy" : "friend";
		}
		else
		{
			if( motionPos != null)
			{
				WorldPosition = Vector3.Lerp(
					WorldPosition,
					motionPos.Value,
					Time.Delta * 6f
				);

				if( WorldPosition.Distance( motionPos.Value ) < 1f )
					motionPos = null;
			}
		}
	}

	public void SetMotionPos(Vector3 pos, GameObject owner)
	{
		if( motionPos == null && PolygonGame.IsOwnerOfPolygon( owner ) )
			motionPos = pos;
	}
}
