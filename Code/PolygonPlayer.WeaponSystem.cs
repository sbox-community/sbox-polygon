using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Rendering;
using System;
using System.Threading.Tasks;
using static Sandbox.VideoWriter;


// Weapon System Integration
public sealed partial class PolygonPlayer : Component, IPlayerEvent, PlayerController.IEvents
{
	private PlayerController _Controller;
	public PlayerController Controller => _Controller ??= GameObject.GetComponent<PlayerController>();
	[Sync]
	public NetDictionary<AmmoResource, int> AmmoCounts { get; set; } = new();

	[Sync] public BaseCarryable ActiveWeapon { get; private set; }
	public List<BaseCarryable> Weapons => GetComponentsInChildren<BaseCarryable>( true ).OrderBy( x => x.InventorySlot ).ThenBy( x => x.InventoryOrder ).ToList();

	void PlayerController.IEvents.PreInput()
	{
		OnControl();
	}
	public void OnUpdateWeaponSystem()
	{
		if ( ActiveWeapon.IsValid() )
		{
			ActiveWeapon.OnFrameUpdate( this );
		}
		//else
		//{
		//	Pickup( "weapons/glock/glock.prefab" );
		//	GiveAmmo( ResourceLibrary.Get<AmmoResource>( "ammotype/9mm.ammo" ), 999999, false );
		//}
	}
	public void OnControl()
	{
		if ( ActiveWeapon.IsValid() )
		{
			ActiveWeapon.OnPlayerUpdate( this );
		}
	}

	//void IPlayerEvent.OnSpawned()
	//{
	//	GiveDefaultWeapons();
	//}
	void IPlayerEvent.OnCameraMove( ref Angles angles )
	{
		if ( ActiveWeapon.IsValid() )
		{
			ActiveWeapon.OnCameraMove( this, ref angles );
		}
	}

	void IPlayerEvent.OnCameraPostSetup( Sandbox.CameraComponent camera )
	{
		if ( ActiveWeapon.IsValid() )
		{
			ActiveWeapon.OnCameraSetup( this, camera );
		}
	}

	public bool Pickup( string prefabName, bool notice = true )
	{
		if ( !Networking.IsHost )
			return false;

		var prefab = GameObject.GetPrefab( prefabName );
		if ( prefab is null )
		{
			Log.Warning( $"Prefab not found: {prefabName}" );
			return false;
		}

		return Pickup( prefab, notice );
	}
	public bool Pickup( GameObject prefab, bool notice = true )
	{
		if ( !Networking.IsHost )
			return false;

		var baseCarry = prefab.Components.Get<BaseCarryable>( true );
		if ( baseCarry is null )
			return false;

		var existing = Weapons.Where( x => x.GameObject.Name == prefab.Name ).FirstOrDefault();
		if ( existing.IsValid() )
		{
			// We already have this weapon type

			if ( baseCarry is BaseWeapon baseWeapon && baseWeapon.UsesAmmo )
			{
				var ammo = baseWeapon.AmmoResource;
				if ( ammo is null )
					return false;

				if ( GetAmmoCount( ammo ) >= ammo.MaxAmount )
					return false;

				GiveAmmo( ammo, baseWeapon.UsesClips ? baseWeapon.ClipContents : baseWeapon.StartingAmmo, notice );
				OnClientPickup( existing, true );
				return true;
			}

			return false;
		}

		var clone = prefab.Clone( new CloneConfig { Parent = GameObject, StartEnabled = false } );
		clone.NetworkSpawn( false, Network.Owner );

		var weapon = clone.Components.Get<BaseCarryable>( true );
		Assert.NotNull( weapon );

		weapon.OnAdded( this );

		IPlayerEvent.PostToGameObject( GameObject, e => e.OnPickup( weapon ) );
		OnClientPickup( weapon );
		return true;
	}

	[Rpc.Owner]
	private void OnClientPickup( BaseCarryable weapon, bool justAmmo = false )
	{
		if ( !weapon.IsValid() ) return;

		SwitchWeapon( weapon );

		if ( !Player.IsProxy )
			ILocalPlayerEvent.Post( e => e.OnPickup( weapon ) );
	}


	public void SwitchWeapon( BaseCarryable weapon )
	{
		if ( weapon == ActiveWeapon ) return;

		if ( ActiveWeapon.IsValid() )
		{
			ActiveWeapon.OnHolstered( this );
			ActiveWeapon.GameObject.Enabled = false;
		}

		ActiveWeapon = weapon;

		if ( ActiveWeapon.IsValid() )
		{
			ActiveWeapon.OnEquipped( this );
			ActiveWeapon.GameObject.Enabled = true;
		}
	}

	public int GetAmmoCount( AmmoResource resource )
	{
		if ( resource == null ) return default;
		return AmmoCounts.GetValueOrDefault( resource, 0 );
	}

	[Rpc.Owner]
	public void GiveAmmo( AmmoResource resource, int count, bool notice )
	{
		if ( resource == null )
			return;

		if ( GetAmmoCount( resource ) + count > resource.MaxAmount )
		{
			count = resource.MaxAmount - GetAmmoCount( resource );
		}

		if ( count <= 0 )
			return;

		var amountGained = AddAmmoCount( resource, count );

		//if ( notice && amountGained > 0 )
		//	ShowNotice( $"{resource.AmmoType} x {amountGained}" );
	}

	public int SetAmmoCount( AmmoResource resource, int count )
	{
		return AmmoCounts[resource] = count;
	}

	public int AddAmmoCount( AmmoResource resource, int count )
	{
		var amountToGain = Math.Min( count, resource.MaxAmount - GetAmmoCount( resource ) );

		AmmoCounts[resource] = GetAmmoCount( resource ) + amountToGain;

		return amountToGain;
	}

	public int SubtractAmmoCount( AmmoResource resource, int count )
	{
		var current = GetAmmoCount( resource );

		count = Math.Min( count, current );
		if ( count <= 0 )
			return 0;

		var to = current - count;

		AmmoCounts[resource] = to;
		return count;
	}

}


public static class HudPainterExtensions
{
	public static float Scale => Screen.Height / 1080.0f;

	public static void DrawHudElement( this HudPainter hud, string text, Vector2 position, Texture icon = null, float iconSize = 32f, TextFlag flags = TextFlag.LeftCenter )
	{
		var textScope = new TextRendering.Scope( text, Color.White, 32 * Scale );
		textScope.TextColor = "white";
		textScope.FontName = "Poppins";
		textScope.FontWeight = 600;
		textScope.Shadow = new TextRendering.Shadow { Enabled = true, Color = "#f506", Offset = 0, Size = 2 };

		hud.SetBlendMode( BlendMode.Lighten );

		if ( icon != null )
		{
			if ( flags.HasFlag( TextFlag.Right ) )
				position.x -= iconSize * Scale;

			hud.DrawTexture( icon, new Rect( position, iconSize * Scale ), textScope.TextColor );
		}

		const float padding = 16f;

		if ( flags.HasFlag( TextFlag.Left ) )
			position.x += (iconSize + padding) * Scale;

		var rect = new Rect( position, new Vector2( 256 * Scale, iconSize * Scale ) );
		if ( flags.HasFlag( TextFlag.Right ) )
			rect.Right = rect.Left - padding * Scale;

		hud.DrawText( textScope, rect, flags );
	}
}

public static class Extensions
{
	public static Vector3 WithAimCone( this Vector3 direction, float degrees )
	{
		var angle = Rotation.LookAt( direction );
		angle *= new Angles( Game.Random.Float( -degrees / 2.0f, degrees / 2.0f ), Game.Random.Float( -degrees / 2.0f, degrees / 2.0f ), 0 );
		return angle.Forward;
	}

	public static Vector3 WithAimCone( this Vector3 direction, float horizontalDegrees, float verticalDegrees )
	{
		var angle = Rotation.LookAt( direction );
		angle *= new Angles( Game.Random.Float( -verticalDegrees / 2.0f, verticalDegrees / 2.0f ), Game.Random.Float( -horizontalDegrees / 2.0f, horizontalDegrees / 2.0f ), 0 );
		return angle.Forward;
	}
}
