using System;
using System.Data;
using System.Threading;

public sealed partial class PolygonGame : Component, Component.INetworkListener
{
	public static PolygonGame GameInstance;
	[Property] public GameObject PlayerPrefab { get; set; }
	public required List<GameObject> enemyTargets { get; set; } = new();
	public required List<GameObject> friendTargets { get; set; } = new();
	public required List<GameObject> startDoors { get; set; } = new();
	public required List<GameObject> finishDoors { get; set; } = new();
	public required GameObject startButton { get; set; }
	public required GameObject startPos { get; set; }
	public required GameObject finishTrigger { get; set; }
	public required GameObject safeZone { get; set; }
	[Property] public required int coolDown { get; set; } = 180; //3min cooldown, it should be calculated for per map 
	[Property] public required int freezetime { get; set; } = 4;

	[Property] public required SoundEvent PolygonMusic { get; set; }
	//[Property] public required SoundEvent PolygonStartSound { get; set; }
	//[Property] public required SoundEvent PolygonTickSound { get; set; }
	//[Property] public required SoundEvent PolygonFinalSound { get; set; }
	[Property] public required SoundEvent PolygonSucceedSound { get; set; }
	[Property] public required SoundEvent PolygonSucceedHighRecordSound { get; set; }
	[Property] public required SoundEvent PolygonSucceedWorldRecordSound { get; set; }
	[Property] public required SoundEvent PolygonFailedSound { get; set; }

	[Sync(SyncFlags.FromHost)] public PolygonData PolygonInfo { get; set; }


	public bool notSupported = false;
	private List<Vector3> spawnPoints;
	private BBox safeZoneBBox;
	private PolygonGOClones clonedSceneGOs = new();

	public class PolygonData
	{
		public required GameObject owner { get; init; }
		public required long timeStart { get; set; }
		public required long timeLeft { get; set; }
		public required int initialEnemyTargets { get; set; }
		public required ushort shootedEnemyTargets { get; set; }
		public required int initialFriendlyTargets { get; set; }
		public required ushort shootedFriendlyTargets { get; set; }
		public required bool cheated { get; set; } //firedbulletcount, weapontype, targethitpos
		public required bool active { get; init; }
		public required CancellationTokenSource cts { get; init; }
	}
	private class PolygonGOClones
	{
		public List<GameObject> enemyTargets = new();
		public List<GameObject> friendTargets = new();
		public List<GameObject> startDoors = new();
		public List<GameObject> finishDoors = new();
	}

	public static bool PolygonIsInUse() => GameInstance.PolygonInfo != null;
	public static bool IsOwnerOfPolygon(GameObject go) => PolygonIsInUse() && GameInstance.PolygonInfo.owner == go;
	public static GameObject GetPolygonOwner() => GameInstance.PolygonInfo.owner;
	public static bool AmIPolygonOwner() => PolygonIsInUse() && GetPolygonOwner().Network.Owner == Connection.Local;
	public static long CurTime() => DateTimeOffset.Now.ToUnixTimeSeconds();
	public static long CurTimeMS() => DateTimeOffset.Now.ToUnixTimeMilliseconds();
	public static Vector3 GetRandomSpawnPoint() => Game.Random.FromList( GameInstance.spawnPoints );

	protected override void OnStart()
	{
		base.OnStart();

		GameInstance = this;

		notSupported = FindPolygonSceneObjects();

		//if ( !notSupported )
		//	clonedSceneGOs = new() { enemyTargets = enemyTargets, finishDoors = finishDoors, friendTargets = friendTargets, startDoors = startDoors };

		if ( Networking.IsHost )
			PolygonPlayer.Respawn( Connection.Host );
	}

	private bool FindPolygonSceneObjects()
	{
		var allObjects = Game.ActiveScene.GetAllObjects( true );

		safeZone = allObjects.First( x => x.Tags.Has( "safezonetrigger" ) );
		safeZoneBBox = safeZone.GetComponent<BoxCollider>().GetWorldBounds();

		spawnPoints = allObjects.Where( x => x.GetComponent<PolygonSpawnPoint>().IsValid() ).Select( x => x.WorldPosition ).ToList();
		
		foreach ( var door in allObjects.Where( x => x.Tags.Has( "startdoor" ) ) )
		{
			var clone = door.Clone( new CloneConfig { StartEnabled = false, Transform = door.WorldTransform } );
			startDoors.Add( door );

			clonedSceneGOs.startDoors.Add( clone );
		}

		foreach ( var door in allObjects.Where( x => x.Tags.Has( "finishdoor" ) ) )
		{
			var clone = door.Clone( new CloneConfig { StartEnabled = false, Transform = door.WorldTransform } );
			finishDoors.Add( door );

			clonedSceneGOs.finishDoors.Add( clone );
		}

		foreach ( var target in allObjects.Where( x => x.Tags.Has( "enemytarget" ) && x.GetComponent<Target>() != null ) )
		{
			var clone = target.Clone( new CloneConfig { StartEnabled = false, Transform = target.WorldTransform } );
			enemyTargets.Add( target );

			clonedSceneGOs.enemyTargets.Add( clone );
		}

		foreach ( var target in allObjects.Where( x => x.Tags.Has( "friendlytarget" ) && x.GetComponent<Target>() != null ) )
		{
			var clone = target.Clone( new CloneConfig { StartEnabled = false, Transform = target.WorldTransform } );
			friendTargets.Add( target );

			clonedSceneGOs.friendTargets.Add( clone );
		}

		startButton = allObjects.First( x => x.Tags.Has( "startbutton" ));
		startPos = allObjects.First( x => x.Tags.Has( "startpoint" ));
		finishTrigger = allObjects.First( x => x.Tags.Has( "finishtrigger" ));
		
		return CheckMap();
	}
	private bool CheckMap()
	{
		return enemyTargets.Count == 0 ||
			friendTargets.Count == 0 ||
			startDoors.Count == 0 ||
			finishDoors.Count == 0 ||
			startButton == null ||
			!startButton.IsValid() ||
			startPos == null ||
			!startPos.IsValid() ||
			finishTrigger == null ||
			!finishTrigger.IsValid() ||
			safeZone == null ||
			!safeZone.IsValid();
	}

	public static bool StartPolygon( GameObject activator )
	{
		if ( GameInstance.notSupported )
		{
			Info( activator, "Map is not supported." );
			return false;
		}

		if ( PolygonIsInUse() )
		{
			Info( activator, "Polygon is in use." );
			return false;
		}

		ReloadMap();

		GameInstance.PolygonInfo = new PolygonData() { active = true, owner = activator, initialEnemyTargets = GameInstance.enemyTargets.Count, initialFriendlyTargets = GameInstance.friendTargets.Count, cheated = false, timeStart = CurTime() + GameInstance.freezetime, timeLeft = CurTime() + GameInstance.coolDown + GameInstance.freezetime, shootedEnemyTargets = 0, shootedFriendlyTargets = 0, cts = new CancellationTokenSource() };

		var polygonPlayer = activator.GetComponent<PolygonPlayer>();

		polygonPlayer.EmitPolygonStartingSound();

		//if ( ply.ActiveChild is WeaponBase wep )
		//	wep.Primary.Ammo = wep.BulletCocking ? wep.Primary.ClipSize + 1 : wep.Primary.ClipSize;

		GiveWeapon( polygonPlayer );

		_ = polygonPlayer.StartPolygon( GameInstance.freezetime, GameInstance.PolygonInfo.cts );

		return true;
	}
	public static void FinishPolygon( GameObject activator, bool forcefailed = false, bool suppressResult = false )
	{
		if ( activator == null )
			return;

		if ( !PolygonIsInUse() )
			return;

		var PolygonInfo = GameInstance.PolygonInfo;

		if ( PolygonInfo.owner != activator )
			return;

		var polygonPlayer = activator.GetComponent<PolygonPlayer>();
		polygonPlayer.StopPolygonMusic();

		PolygonInfo.cts.Cancel();

		TakeWeapon( polygonPlayer );

		if ( !PolygonInfo.cheated )
			PolygonInfo.cheated = PolygonInfo.shootedFriendlyTargets < 0 ||
			PolygonInfo.shootedFriendlyTargets > GameInstance.friendTargets.Count ||
			PolygonInfo.shootedEnemyTargets < 0 ||
			PolygonInfo.shootedEnemyTargets > PolygonInfo.initialEnemyTargets ||
			PolygonInfo.shootedEnemyTargets != GameInstance.enemyTargets.Count( x => x.IsDestroyed ) || 
			PolygonInfo.shootedFriendlyTargets != GameInstance.friendTargets.Count( x => x.IsDestroyed );

		var succeed = !(PolygonInfo.cheated) && !forcefailed && PolygonInfo.shootedFriendlyTargets == 0 && PolygonInfo.shootedEnemyTargets == PolygonInfo.initialEnemyTargets;
		var score = CurTimeMS() - PolygonInfo.timeStart;

		if( !suppressResult )
			polygonPlayer.ShowStatisticsMenu( succeed, PolygonInfo.cheated, score, $"{PolygonInfo.shootedEnemyTargets}/{PolygonInfo.initialEnemyTargets}", $"{PolygonInfo.shootedFriendlyTargets}/{PolygonInfo.initialFriendlyTargets}" );

		if ( succeed )
		{

			//EmitPolygonSucceedSound( activator);
			EmitBalloon( GameInstance.finishTrigger.WorldPosition );
		}
		//else if(!forcefailed)
		//	EmitPolygonFailedSound( activator );

		if ( forcefailed )
		{
			polygonPlayer.HoldSteadyOnTheStartPoint();
			polygonPlayer.EmitPolygonCancellationSound();
		}
	
		GameInstance.BreakFinishDoors();

		activator.GetComponent<Rigidbody>().Locking = new PhysicsLock();

		GameInstance.PolygonInfo = null;

	}

	public static void ReloadMap() {

		//SceneLoadOptions options = new SceneLoadOptions();
		//options.SetScene( Game.ActiveScene.Name + ".scene" );
		//options.ShowLoadingScreen = false;
		//Game.ChangeScene( options );	

		foreach ( var door in GameInstance.startDoors )
			if ( door.IsValid() )
				door.DestroyImmediate();

		GameInstance.startDoors.Clear();

		foreach ( var door in GameInstance.finishDoors )
			if ( door.IsValid() )
				door.DestroyImmediate();

		GameInstance.finishDoors.Clear();

		foreach ( var target in GameInstance.enemyTargets )
			if ( target.IsValid() )
				target.DestroyImmediate();

		GameInstance.enemyTargets.Clear();

		foreach ( var target in GameInstance.friendTargets )
			if ( target.IsValid() )
				target.DestroyImmediate();

		GameInstance.friendTargets.Clear();

		foreach ( var obj in GameInstance.clonedSceneGOs.enemyTargets )
		{
			var newObj = obj.Clone( new CloneConfig { StartEnabled = true, Transform = obj.WorldTransform } );
			GameInstance.enemyTargets.Add( newObj );
		}

		foreach ( var obj in GameInstance.clonedSceneGOs.friendTargets )
		{
			var newObj = obj.Clone( new CloneConfig { StartEnabled = true, Transform = obj.WorldTransform } );
			GameInstance.friendTargets.Add( newObj );
		}

		foreach ( var obj in GameInstance.clonedSceneGOs.startDoors )
		{
			var newObj = obj.Clone( new CloneConfig { StartEnabled = true, Transform = obj.WorldTransform } );
			GameInstance.startDoors.Add( newObj );
		}

		foreach ( var obj in GameInstance.clonedSceneGOs.finishDoors )
		{
			var newObj = obj.Clone( new CloneConfig { StartEnabled = true, Transform = obj.WorldTransform } );
			GameInstance.finishDoors.Add( newObj );
		}
	}

	public static void Info(GameObject client, string info)
	{
		using ( Rpc.FilterInclude( c => c == client.Network.Owner ) )
			GameInstance.Info( info );
	}

	[Rpc.Broadcast( Flags = NetFlags.HostOnly | NetFlags.Reliable)]
	public void Info( string info )
	{
		//PolygonHUD.infoMenu( info );
	}
	public void breakStartDoors()
	{
		foreach ( var door in startDoors )
			door.DestroyImmediate();
	}
	public void BreakFinishDoors()
	{
		foreach ( var door in finishDoors )
			door.DestroyImmediate();
	}

	//[Rpc.Broadcast( Flags = NetFlags.HostOnly | NetFlags.Reliable )]
	public static void EmitPolygonSucceedSound( GameObject player, bool highRecord, bool worldRecord )
	{
		Sound.Play( !highRecord && !worldRecord ? GameInstance.PolygonSucceedSound : !worldRecord ? GameInstance.PolygonSucceedHighRecordSound : GameInstance.PolygonSucceedWorldRecordSound , player.WorldPosition );
	}

	[Rpc.Broadcast( Flags = NetFlags.HostOnly | NetFlags.Reliable )]
	public static void EmitPolygonFailedSound( GameObject player )
	{
		Sound.Play( GameInstance.PolygonFailedSound, player.WorldPosition );
	}
	[Rpc.Host]
	public static void ForceFinish(GameObject player)
	{
		FinishPolygon( player, forcefailed: true, suppressResult: true );
	}
	public static void EmitBalloon(Vector3 pos)
	{
		for(var i = 0; i < 10; i++ )
		{
			//make balloons with prefabs
		}
	}
	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !Networking.IsHost )
			return;

		if ( PolygonIsInUse() )
		{
			CheckCheat();
			CheckTimeLeft();
		}
	}
	private void CheckCheat()
	{
		if ( Game.IsEditor || 
				!GameInstance.PolygonInfo.owner.IsValid() || 
				!safeZoneBBox.Overlaps( GameInstance.PolygonInfo.owner.GetComponent<Rigidbody>().GetWorldBounds()) || 
				Game.ActiveScene.TimeScale != 1f || 
				GameInstance.PolygonInfo.owner.GetComponent<PlayerController>() is not { } pcontroller || 
				pcontroller.WalkSpeed != 230 || 
				pcontroller.RunSpeed != 320 || 
				pcontroller.DuckedSpeed != 70 || 
				pcontroller.JumpSpeed != 300 )
			GameInstance.PolygonInfo.cheated = true;
	}
	public void CheatDetected(GameObject player)
	{
		if ( PolygonIsInUse() && GameInstance.PolygonInfo.owner == player)
			GameInstance.PolygonInfo.cheated = true;
	}
	private void CheckTimeLeft()
	{
		if ( CurTime() > GameInstance.PolygonInfo.timeLeft )
			FinishPolygon( GameInstance.PolygonInfo.owner, forcefailed: true );
	}

	[Rpc.Broadcast( Flags = NetFlags.HostOnly | NetFlags.Reliable )]
	public static void StartSound( GameObject go )
	{
		Sound.Play( "alarm_bell_trimmed", go.WorldPosition);
	}
	public static void GiveWeapon( PolygonPlayer owner )
	{
		owner.Pickup( "weapons/glock/glock.prefab" );
		owner.GiveAmmo( ResourceLibrary.Get<AmmoResource>( "ammotype/9mm.ammo" ), 999999, false );
	}

	public static void TakeWeapon( PolygonPlayer owner )
	{
		if( owner.GameObject.GetComponentInChildren<GlockWeapon>() is { } glock )
		{
			glock.Destroy();
			glock.DestroyGameObject();
		}
	}	
	public void OnActive( Connection owner )
	{
		PolygonPlayer.Respawn( owner );
	}
	public static void UpdateTargets(Target target, GameObject activator)
	{
		if ( target == null )
			return;

		if ( activator == null )
			return;

		if ( !PolygonIsInUse() )
			return;

		var PolygonInfo = GameInstance.PolygonInfo;

		if ( PolygonInfo.owner != activator )
			return;

		if ( target.isEnemy )
			PolygonInfo.shootedEnemyTargets += 1;
		else
			PolygonInfo.shootedFriendlyTargets += 1;
	}
}
