using Sandbox.Services;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

public sealed partial class PolygonPlayer : Component
{
	[Sync] public bool Freeze { get; set; } = false;
	private GameObject Player => GameObject;
	public static GameObject LocalPlayer;
	public static PolygonPlayer LocalPolygonPlayer;
	public static CameraComponent LocalCamera;
	public static SoundPointComponent PolygonMusic;
	public bool videoStarted = false;
	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			LocalPlayer = GameObject;
			LocalPolygonPlayer = this;
			LocalCamera = GameObject.GetComponentInChildren<CameraComponent>();
		}

		_ = RefreshScore();

		base.OnStart();
	}
	protected override void OnUpdate()
	{
		base.OnUpdate();
		OnUpdateWeaponSystem();
	}

	[Rpc.Owner ( Flags= NetFlags.HostOnly | NetFlags.Reliable)]
	public void ShowStatisticsMenu( bool status, bool cheat, long time, string enemytarget, string friendlytarget)
	{
		_ = ProcessScore( status, cheat, time, enemytarget, friendlytarget, GameObject );
	}

	private static async Task ProcessScore( bool status, bool cheat, long time, string enemytarget, string friendlytarget, GameObject go )
	{
		var achievement = Sandbox.Services.Achievements.All.FirstOrDefault( x => x.Name == "success_one_rnd" );
		if ( achievement != null && !achievement.IsUnlocked )
			Sandbox.Services.Achievements.Unlock( "success_one_rnd" );

		string sceneName = Game.ActiveScene.Name.ToLower();

		await PolygonScoreboard.leaderBoard.Refresh();

		var playerScore = Sandbox.Services.Stats.LocalPlayer.Get( sceneName ).Min;
		bool highrecord = status && (playerScore == 0 || playerScore > time);
		bool worldrecord = highrecord &&
			PolygonScoreboard.leaderBoard.Entries
				.Any( x => time < x.Value );

		if ( go != null && go.IsValid() )
		{
			if ( status )
				PolygonGame.EmitPolygonSucceedSound( go, highrecord, worldrecord );
			else
				PolygonGame.EmitPolygonFailedSound( go );
		}

		PolygonMenu.Result( status, cheat, time, enemytarget, friendlytarget, highrecord, worldrecord );

		if ( status && highrecord )
		{
			Sandbox.Services.Stats.SetValue( sceneName, time );
			await GameTask.DelaySeconds( 3f );
			await RefreshScoreManually();
		}
	}

	public static async Task RefreshScore()
	{
		while( true )
		{
			await Sandbox.Services.Stats.Global.Refresh();
			await Sandbox.Services.Stats.LocalPlayer.Refresh();
			await PolygonScoreboard.RefreshScores();
			await GameTask.DelaySeconds( 60f );
		}
	}

	public static async Task RefreshScoreManually()
	{
		await Sandbox.Services.Stats.Global.Refresh();
		await Sandbox.Services.Stats.LocalPlayer.Refresh();
		await PolygonScoreboard.RefreshScores();
	}

	[Rpc.Broadcast( Flags = NetFlags.HostOnly | NetFlags.Reliable )]
	public void EmitPolygonStartingSound()
	{
		Sound.Play( "startingpolygon", PolygonGame.GameInstance.startButton.WorldPosition );
	}

	[Rpc.Owner( Flags = NetFlags.HostOnly | NetFlags.Reliable )]
	public void EmitPolygonCancellationSound()
	{
		var s = Sound.Play( "ui.downvote" );
		s.Pitch = 0.5f;
		s.Volume = 2f;
	}
	[Rpc.Owner( Flags = NetFlags.HostOnly | NetFlags.Reliable )]
	public void Info( string info)
	{
		PolygonMenu.Info( info );
	}
	public async Task StartPolygon( int freezetime, CancellationTokenSource cts )
	{
		HoldSteadyOnTheStartPoint();

		Freeze = true;

		await Task.DelayRealtime( freezetime * 1000 );

		if ( !PolygonGame.PolygonIsInUse() )
			return;

		if ( cts.IsCancellationRequested )
			return;

		PolygonGame.GetPolygonOwner().GetComponent<Rigidbody>().Locking = new PhysicsLock();

		PolygonGame.GameInstance.PolygonInfo.timeStart = PolygonGame.CurTimeMS(); //not accurate?

		Freeze = false;

		PolygonGame.GameInstance.breakStartDoors();

		StartPolygonMusic();
		
		//if( !videoStarted ) // TODO: info
		//{
		//	videoStarted = true;
		//	ConsoleSystem.Run( "video" ); // Start video
		//}
	}

	private void StartPolygonMusic()
	{
		if ( PolygonMusic != null )
			PolygonMusic.StopSound();

		PolygonMusic = GameObject.AddComponent<SoundPointComponent>();
		PolygonMusic.SoundEvent = PolygonGame.GameInstance.PolygonMusic;
		PolygonMusic.StartSound();

	}

	[Rpc.Owner( Flags = NetFlags.HostOnly | NetFlags.Reliable )]
	public void StopPolygonMusic()
	{
		//if ( videoStarted ) // TODO: info
		//{
		//	videoStarted = false;
		//	ConsoleSystem.Run( "video" ); // Stop video
		//}

		if ( PolygonMusic != null )
			PolygonMusic.StopSound();

		PolygonMusic = null;
	}

	public void HoldSteadyOnTheStartPoint()
	{
		var body = Player.GetComponent<Rigidbody>();
		body.Velocity = 0;
		Player.WorldPosition = PolygonGame.GameInstance.startPos.WorldPosition;

		var locking = new PhysicsLock();
		locking.X = true;
		locking.Y = true;
		locking.Z = true;

		body.Locking = locking;
	}

	public static void Respawn(Connection owner)
	{
		var player = PolygonGame.GameInstance.PlayerPrefab.Clone(
			new CloneConfig { Transform = global::Transform.Zero, StartEnabled = false } );

		player.WorldPosition = PolygonGame.GetRandomSpawnPoint();
		player.NetworkSpawn( owner );

		player.Enabled = true;

		player.GetComponent<PolygonPlayer>().Info( "Welcome to Polygon Game! \n\nAll you have to do is press 'E' to start!" );
	}
}
