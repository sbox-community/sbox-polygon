//WIP


//using Sandbox;
//using Sandbox.MovieMaker;
//using Sandbox.MovieMaker.Compiled;
//using static Sandbox.PhysicsContact;
//using static System.Net.Mime.MediaTypeNames;

//public sealed class MovieMaker : Component
//{
//	private CompiledPropertyTrack<Vector3> positionTrack;
//	private CompiledPropertyTrack<Rotation> rotationTrack;
//	private CompiledPropertyTrack<float> fovTrack;

//	CompiledReferenceTrack<GameObject> objectTrack;
//	//CompiledReferenceTrack<GameObject> objectTrack2;
//	bool baslat = false;
//	List<Vector3> test = new();
//	List<Rotation> test2 = new();
//	TrackBinder trackBinder2;
//	//VideoWriter test;
//	protected override void OnStart()
//	{
//		base.OnStart();
//		 objectTrack = MovieClip.RootGameObject( "test" );
//		 //objectTrack2 = MovieClip.RootGameObject( "test2" );

//		//	positionTrack = objectTrack
//		//.Property<Vector3>( "LocalPosition" )
//		//.WithSamples( timeRange: (0f, 3f), sampleRate: 2, [new Vector3( 100, 200, 300 ), new Vector3( 100, 200, 300 ), new Vector3( 100, 200, 300 ), new Vector3( 200, 100, -800 ), new Vector3( 200, 100, -800 )] );
//		//.WithConstant( timeRange: (0.0, 2.0), new Vector3( 100, 200, 300 ) )
//		//.WithConstant( timeRange: (2.0, 5.0), new Vector3( 200, 100, -800 ) );


//		//	fovTrack = objectTrack
//		//.Component<CameraComponent>()
//		//.Property<float>( "FieldOfView" )
//		//.WithSamples( timeRange: (1f, 3f), sampleRate: 2, [60f, 75f, 65f, 90f, 50f] );

//		var target = TrackBinder.Default.Get( objectTrack );
//		target.Bind( GameObject );


//		//trackBinder2 = new TrackBinder( Game.ActiveScene );
//		//trackBinder2.Get( GameObject ).Bind( GameObject.Children.FirstOrDefault(x=>x.Name == "Main Camera" ) );


//		//var binder = new TrackBinder( Game.ActiveScene );

//		//binder.Get( objectTrack ).Bind( Game.ActiveScene.Camera );

//		//Log.Info( target );
//		//test = VideoWriter.

//		//var test = new ScreenRecorder();

//		//ConsoleSystem.Run( "video" );
//	}
//	protected override void OnUpdate()
//	{
//		base.OnUpdate();
//		var time = Time.Now % 60f;
		

		
//		//fovTrack.Update( time );

//		if(time > 3f)
//		{
	

//			if(!baslat)
//			{
//				baslat = true;


//				positionTrack = objectTrack.Property<Vector3>( "LocalPosition" ).WithSamples( timeRange: (0f, 3f), 60, test.ToArray() );
//				rotationTrack = objectTrack.Component<CameraComponent>().Property<Rotation>( "WorldRotation" ).WithSamples( timeRange: (0f, 3f), 60, test2.ToArray() );

//			}
//			var time2 = (Time.Now - 3f) % 60;
//			Log.Info( time2 );
//			positionTrack.Update( time2 );
//			rotationTrack.Update( time2, trackBinder2 );

//		}
//		else
//		{
//			test.Add( LocalPosition );
//			test2.Add( GameObject.Children.FirstOrDefault( x => x.Name == "Main Camera" ).LocalRotation );
//		}

//		//if(time > 2)
//		//	test.FinishAsync();
//		//var camTrack = clip.GetReference( "Camera" );
//		//var posTrack = clip.GetProperty<Vector3>( "Camera", "LocalPosition" );
//		//var objectTrack = MovieClip.RootGameObject( "Main Camera" );
//		//Log.Info( objectTrack );
//	}
//	//public static VideoWriter CreateVideoWriter( string path, VideoWriter.Config config )
//	//{
//	//	return new VideoWriter( path, config );
//	//}
//}
