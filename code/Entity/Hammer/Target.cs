using SandboxEditor;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

// Another version of Door Entity
namespace Sandbox
{
	/// <summary>
	/// Sounds to be used by ent_target if it does not override sounds.
	/// </summary>
	[ModelDoc.GameData( "target_sounds" )]
	public class ModelTargetSounds
	{
		/// <summary>
		/// Sound to play when the target reaches it's fully activated position.
		/// </summary>
		[JsonPropertyName( "fully_activated_sound" )]
		public string FullyActivedSound { get; set; }

		/// <summary>
		/// Sound to play when the target reaches it's fully closed position.
		/// </summary>
		[JsonPropertyName( "fully_deactivated_sound" )]
		public string FullyDeactivatedSound { get; set; }

		/// <summary>
		/// Sound to play when the target starts to activating.
		/// </summary>
		[JsonPropertyName( "activate_sound" ), Title( "Start activating sound" )]
		public string ActivateSound { get; set; }

		/// <summary>
		/// Sound to play when the target starts to deactivating.
		/// </summary>
		[JsonPropertyName( "deactivate_sound" ), Title( "Start deactivating sound" )]
		public string DeactivateSound { get; set; }

		/// <summary>
		/// Sound to play while the target is moving. Typically this should be looping or very long.
		/// </summary>
		[JsonPropertyName( "moving_sound" )]
		public string MovingSound { get; set; }
	}

	/// <summary>
	/// A basic entity target that can move or rotate.
	/// The target will rotate around the model's origin. 
	/// </summary>
	[Library( "ent_target_enemy" )]
	[HammerEntity]//SupportsSolid
    [EditorModel("models/enemy/enemy.vmdl", "red", "red")]
    //[Model( Archetypes = ModelArchetype.animated_model )]
	[DoorHelper( "movedir", "movedir_islocal", "movedir_type", "distance" )]
	[VisGroup( VisGroup.Dynamic )]//RenderFields,
    [Title( "Enemy Target" ), Category( "Gameplay" ), Icon( "door_front" )]
	public partial class Target : KeyframeEntity
	{
        public virtual bool EnemyTarget { get; set; } = true;

		/// <summary>
		/// The direction the target will move, when it activated.
		/// </summary>
		[Property( "movedir", Title = "Move Direction (Pitch Yaw Roll)" )]
		public Angles MoveDir { get; set; }

		/// <summary>
		/// If checked, the movement direction angle is in local space and should be rotated by the entity's angles after spawning.
		/// </summary>
		[Property( "movedir_islocal", Title = "Move Direction is Expressed in Local Space" )]
		public bool MoveDirIsLocal { get; set; } = true;

		public enum TargetMoveType
		{
			Moving,
			Rotating
		}

		/// <summary>
		/// Movement type of the target.
		/// </summary>
		[Property( "movedir_type", Title = "Movement Type" )]
		public TargetMoveType MoveDirType { get; set; } = TargetMoveType.Moving;

		/// <summary>
		/// Moving target: The amount, in inches, of the target to leave sticking out of the wall it recedes into when pressed. Negative values make the target recede even further into the wall.
		/// Rotating target: The amount, in degrees, that the target should rotate when it's pressed.
		/// </summary>
		[Property]
		public float Distance { get; set; } = 0;

		/// <summary>
		/// How far the target should be activated on spawn where 0% = deactivated and 100% = fully activated.
		/// </summary>
		[Property( "initial_position" ), Category( "Spawn Settings" )]
		[MinMax( 0, 100 )]
		public float InitialPosition { get; set; } = 0;

		/// <summary>
		/// The speed at which the target moves.
		/// </summary>
		[Property]
		public float Speed { get; set; } = 100;
		public bool Breakable { get; set; } = true;

		/// <summary>
		/// Sound to play when the target starts to activating.
		/// </summary>
		[Property( "activate_sound", Title = "Start Activating Sound" ), FGDType( "sound" ), Category( "Sounds" )]
		public string ActivateSound { get; set; } = "";

		/// <summary>
		/// Sound to play when the target reaches it's fully activated position.
		/// </summary>
		[Property( "fully_activated_sound", Title = "Fully Activated Sound" ), FGDType( "sound" ), Category( "Sounds" )]
		public string FullyActivatedSound { get; set; } = "";

		/// <summary>
		/// Sound to play when the target starts to deactivate.
		/// </summary>
		[Property( "deactivate_sound", Title = "Start Deactivating Sound" ), FGDType( "sound" ), Category( "Sounds" )]
		public string DeactivateSound { get; set; } = "";

		/// <summary>
		/// Sound to play when the target reaches it's fully deactivated position.
		/// </summary>
		[Property( "fully_deactivated_sound", Title = "Fully Deactivated Sound" ), FGDType( "sound" ), Category( "Sounds" )]
		public string FullyDeactivatedSound { get; set; } = "";

		/// <summary>
		/// Sound to play while the target is moving. Typically this should be looping or very long.
		/// </summary>
		[Property( "moving_sound", Title = "Moving Sound" ), FGDType( "sound" ), Category( "Sounds" )]
		public string MovingSound { get; set; } = "";

		/// <summary>
		/// Used to override the activate/deactivate animation of moving and rotating targets. X axis (input, left to right) is the animation, Y axis (output, bottom to top) is how activate the target is at that point in the animation.
		/// </summary>
		[Property( "activate_ease", Title = "Ease Function" )]
		public FGDCurve ActivateCurve { get; set; }

		/// <summary>
		/// The easing function for both movement and rotation
		/// TODO: Expose to hammer in a nice way
		/// </summary>
		public Easing.Function Ease { get; set; } = Easing.EaseOut;

		Vector3 PositionA;
		Vector3 PositionB;
		Rotation RotationA;
		Rotation RotationB;
		Rotation RotationB_Normal;
		Rotation RotationB_Opposite;
        public enum TargetState
		{
			Active,
			Deactive,
			Activating,
			Deactivating
		}

		/// <summary>
		/// Which position the target is in.
		/// </summary>
		[Net]
		public TargetState State { get; protected set; } = TargetState.Active;

		public override void Spawn()
		{
			base.Spawn();

            SetModel("models/enemy/enemy.vmdl");
            SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

			// TargetMoveType.Moving
			{
				PositionA = LocalPosition;

				// Get the direction we want to move in
				var dir = MoveDir.Direction;

				// Active position is the size of the bbox in the direction minus the lip size
				var boundSize = CollisionBounds.Size;

				PositionB = PositionA + dir * (MathF.Abs( boundSize.Dot( dir ) ) - Distance);

				if ( MoveDirIsLocal )
				{
					var dir_world = Transform.NormalToWorld( dir );
					PositionB = PositionA + dir_world * (MathF.Abs( boundSize.Dot( dir ) ) - Distance);
				}
			}

			// TargetMoveType.Rotating
			{
				RotationA = LocalRotation;

				var axis = Rotation.From( MoveDir ).Up;

				if ( !MoveDirIsLocal ) axis = Transform.NormalToLocal( axis );

				RotationB_Opposite = RotationA.RotateAroundAxis( axis, -Distance );
				RotationB_Normal = RotationA.RotateAroundAxis( axis, Distance );
				RotationB = RotationB_Normal;
			}

			State = TargetState.Deactive;

			if ( InitialPosition > 0 )
			{
				SetPosition( InitialPosition / 100.0f );
			}

			if ( Model.HasData<ModelTargetSounds>() )
			{
				ModelTargetSounds sounds = Model.GetData<ModelTargetSounds>();

				if ( string.IsNullOrEmpty( MovingSound ) ) MovingSound = sounds.MovingSound;
				if ( string.IsNullOrEmpty( DeactivateSound ) ) DeactivateSound = sounds.DeactivateSound;
				if ( string.IsNullOrEmpty( FullyDeactivatedSound ) ) FullyDeactivatedSound = sounds.FullyDeactivatedSound;
				if ( string.IsNullOrEmpty( ActivateSound ) ) ActivateSound = sounds.ActivateSound;
				if ( string.IsNullOrEmpty( FullyActivatedSound ) ) FullyActivatedSound = sounds.FullyActivedSound;

			}

			if ( ActivateCurve != null )
			{
				Ease = delegate ( float x ) { return ActivateCurve.GetNormalized( x ); };
			}

			Tags.Add(EnemyTarget ? "enemy" : "friend" );

			SetMaterialGroup( EnemyTarget ? "Enemy": "Friend");
        }

		protected override void OnDestroy()
		{
			if ( MoveSoundInstance.HasValue )
			{
				MoveSoundInstance.Value.Stop();
				MoveSoundInstance = null;
			}

			base.OnDestroy();
		}

		/// <summary>
		/// Sets the target's position to given percentage. The expected input range is 0..1
		/// </summary>
		[Input]
		void SetPosition( float progress )
		{
			if ( MoveDirType == TargetMoveType.Moving ) { LocalPosition = PositionA.LerpTo( PositionB, progress ); }
			else if ( MoveDirType == TargetMoveType.Rotating ) { LocalRotation = Rotation.Lerp( RotationA, RotationB, progress ); }
			else { Log.Warning( $"{this}: Unknown target move type {MoveDirType}!" ); }

			if ( progress >= 1.0f ) State = TargetState.Active;
		}

		void UpdateAnimGraph( bool open )
		{
			SetAnimParameter( "open", open );
		}

		protected override void OnAnimGraphCreated()
		{
			base.OnAnimGraphCreated();

			// Anim graph doesnt exist in Spawn()
			if ( State == TargetState.Active )
			{
				UpdateAnimGraph( true );
			}

			if ( InitialPosition > 0 )
			{
				SetAnimParameter( "initial_position", InitialPosition / 100.0f );
			}
		}

		#region Breakable

		public override void OnNewModel( Model model )
		{
			base.OnNewModel( model );

			// When a model is reloaded, all entities get set to NULL model first
			if ( model.IsError ) return;

			if ( IsServer )
			{
				if ( model.TryGetData( out ModelPropData propInfo ) )
				{
					Health = propInfo.Health;
					
				}

				// If health is unset, set it to -1 - which means it cannot be destroyed
				if ( Health <= 0 ) Health = -1;
			}
			RenderColor = Color.Magenta;
			
		}

		DamageInfo LastDamage;

		/// <summary>
		/// Fired when the entity gets damaged, even if it is unbreakable.
		/// </summary>
		protected Output OnDamaged { get; set; }

		public override void TakeDamage( DamageInfo info )
		{
            // The target was damaged, even if its unbreakable, we still want to fire it
            // TODO: Add damage type as argument? Or should it be the new health value?
            OnDamaged.Fire( this );
			if ( !Breakable ) return;
            LastDamage = info ;//because of lack of base.TakeDamage is like await
			base.TakeDamage( info );
            LastDamage = info;
		}

		/// <summary>
		/// Fired when the entity gets destroyed.
		/// </summary>
		protected Output OnBreak { get; set; }

		public override void OnKilled()
		{

            if ( LifeState != LifeState.Alive )
				return;

			var result = new Breakables.Result();
            result.CopyParamsFrom( LastDamage );

            //in order to prevent breaking target other players
            if (LastAttacker != Owner)
                return;

            Breakables.Break( this, result );
			OnBreak.Fire( LastDamage.Attacker );
            PolygonPlayer.hitTarget(Position);

            base.OnKilled();
		}

		/// <summary>
		/// Causes this prop to break, regardless if it is actually breakable or not. (i.e. ignores health and whether the model has gibs)
		/// </summary>
		[Input]
		public void Break()
		{
			OnKilled();
			LifeState = LifeState.Dead;
			Delete();
		}

		#endregion

		/// <summary>
		/// Toggle the activate state of the target. Obeys locked state.
		/// </summary>
		[Input]
		public void Toggle( Entity activator = null )
		{
			if ( State == TargetState.Active || State == TargetState.Activating ) Close( activator );
			else if ( State == TargetState.Deactive || State == TargetState.Deactivating ) Activate( activator );
		}

		/// <summary>
		/// Active the target. Obeys locked state.
		/// </summary>
		[Input]
		public void Activate( Entity activator = null )
		{

			if ( State == TargetState.Deactive )
			{
				PlaySound( ActivateSound );
			}

			if ( State == TargetState.Deactive || State == TargetState.Deactivating ) State = TargetState.Activating;

			if ( activator != null && MoveDirType == TargetMoveType.Rotating &&  State != TargetState.Active )
			{
				// TODO: In this case the target could be moving faster than given speed if we are trying to activate the target while it is closing from the opposite side
				var axis = Rotation.From( MoveDir ).Up;
				if ( !MoveDirIsLocal ) axis = Transform.NormalToLocal( axis );

				// Generate the correct "inward" direction for the target since we can't assume RotationA.Forward is it
				// TODO: This does not handle non UP axis targets!
				var Dir = (WorldSpaceBounds.Center.WithZ( Position.z ) - Position).Normal;
				var Pos1 = Position + Rotation.FromAxis( Dir, 0 ).RotateAroundAxis( axis, Distance ) * Dir * 24.0f;
				var Pos2 = Position + Rotation.FromAxis( Dir, 0 ).RotateAroundAxis( axis, -Distance ) * Dir * 24.0f;

				var PlyPos = activator.Position;
				if ( PlyPos.Distance( Pos2 ) < PlyPos.Distance( Pos1 ) )
				{
					RotationB = RotationB_Normal;
				}
				else
				{
					RotationB = RotationB_Opposite;
				}
			}

			UpdateAnimGraph( true );

			UpdateState();

		}

		/// <summary>
		/// Close the target. Obeys locked state.
		/// </summary>
		[Input]
		public void Close( Entity activator = null )
		{

			if ( State == TargetState.Active )
			{
				PlaySound( DeactivateSound );
			}

			if ( State == TargetState.Active || State == TargetState.Activating ) State = TargetState.Deactivating;

			UpdateAnimGraph( false );
			UpdateState();

		}

		/// <summary>
		/// Fired when the target starts to activating. This can be called multiple times during a single "target activating"
		/// </summary>
		protected Output OnTargetActive { get; set; }

		/// <summary>
		/// Fired when the target starts to deactivating. This can be called multiple times during a single "target deactivating"
		/// </summary>
		protected Output OnTargetClose { get; set; }

		/// <summary>
		/// Called when the target fully activated.
		/// </summary>
		protected Output OnTargetFullyActivated { get; set; }

		/// <summary>
		/// Called when the target fully deactivated.
		/// </summary>
		protected Output OnTargetFullyDeactivated { get; set; }

		public virtual void UpdateState()
		{
			bool open = (State == TargetState.Activating) || (State == TargetState.Active);

			_ = DoMove( open );
		}

		int movement = 0;
		Sound? MoveSoundInstance = null;
		//bool AnimGraphFinished = false;
		async Task DoMove( bool state )
		{
			if ( !MoveSoundInstance.HasValue && !string.IsNullOrEmpty( MovingSound ) )
			{
				MoveSoundInstance = PlaySound( MovingSound );
			}

			var moveId = ++movement;

			if ( State == TargetState.Activating )
			{
				_ = OnTargetActive.Fire( this );
			}
			else if ( State == TargetState.Deactivating )
			{
				_ = OnTargetClose.Fire( this );
			}

			if ( MoveDirType == TargetMoveType.Moving )
			{
				var position = state ? PositionB : PositionA;

				var distance = Vector3.DistanceBetween( LocalPosition, position );
				var timeToTake = distance / Math.Max( Speed, 0.1f );

				var success = await LocalKeyframeTo( position, timeToTake, Ease );
				if ( !success )
					return;
			}
			else if ( MoveDirType == TargetMoveType.Rotating )
			{
				var target = state ? RotationB : RotationA;

				Rotation diff = LocalRotation * target.Inverse;
				var timeToTake = diff.Angle() / Math.Max( Speed, 0.1f );

				var success = await LocalRotateKeyframeTo( target, timeToTake, Ease );
				if ( !success )
					return;
			}
		
			else { Log.Warning( $"{this}: Unknown target move type {MoveDirType}!" ); }

			if ( moveId != movement || !this.IsValid() )
				return;

			if ( State == TargetState.Activating )
			{
				_ = OnTargetFullyActivated.Fire( this );
				State = TargetState.Active;
				PlaySound( FullyActivatedSound );
			}
			else if ( State == TargetState.Deactivating )
			{
				_ = OnTargetFullyDeactivated.Fire( this );
				State = TargetState.Deactive;
				PlaySound( FullyDeactivatedSound );
			}

			if ( MoveSoundInstance.HasValue )
			{
				MoveSoundInstance.Value.Stop();
				MoveSoundInstance = null;
			}
		}

		/*protected override void OnAnimGraphTag( string tag, AnimGraphTagEvent fireMode )
		{
			if ( tag == "AnimationFinished" && fireMode != AnimGraphTagEvent.End )
			{
				AnimGraphFinished = true;
			}
		}*/
	}

    [Library("ent_target_friendly")]
    [HammerEntity]//SupportsSolid
    [EditorModel("models/enemy/enemy.vmdl", "blue", "blue")]
    //[Model(Archetypes = ModelArchetype.animated_model)]
    [DoorHelper("movedir", "movedir_islocal", "movedir_type", "distance")]
    [VisGroup(VisGroup.Dynamic)]//RenderFields,
    [Title("Friendly Target"), Category("Gameplay"), Icon("door_front")]
    public class TargetFriendly : Target{

        public override bool EnemyTarget { get; set; } = false;
    }

 }
