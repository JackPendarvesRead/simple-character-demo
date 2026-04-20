using Godot;
using SimpleCharacterDemo.Scripts.Constants;
using SimpleCharacterDemo.Scripts.Helpers;

namespace SimpleCharacterDemo.Abilities
{
	public partial class LedgeGrab : Node2D
	{
		private Player _player;

		private RayCast2D _checkLeftRayCast;
		private RayCast2D _checkRightRayCast;
		private RayCast2D _grabLeftRayCast;
		private RayCast2D _grabRightRayCast;
		private bool _grabInProgress = false;

		[Export]
		public float GrabDistance = 8.5f;

		public bool IsGrabbing { get; private set; } = false;

		public override void _Ready()
		{
			_player = GetParent<Player>();
			_checkLeftRayCast = GetNode<RayCast2D>(LedgeGrabNodeNames.CheckClearLeftRayCast);
			_checkRightRayCast = GetNode<RayCast2D>(LedgeGrabNodeNames.CheckClearRightRayCast);
			_grabLeftRayCast = GetNode<RayCast2D>(LedgeGrabNodeNames.GrabLeftRayCast);
			_grabRightRayCast = GetNode<RayCast2D>(LedgeGrabNodeNames.GrabRightRayCast);
		}

		public void Check(ref Vector2 velocity)
		{
			// Grab in progress used whilst player is snapping towards the wall within grab distance.
			if (_grabInProgress)
			{
				if(velocity == Vector2.Zero)
				{
					_grabInProgress = false;
					GD.Print("Grab complete, player is now grabbing the ledge.");
				}

				if (!IsGrabbing)
				{
					_grabInProgress = false;
					GD.Print("Grab cancelled, player is no longer grabbing the ledge.");
				}

				return;
			}

			if (IsGrabbing && !_player.IsOnWall())
			{
				// If the player is no longer on the wall release the grabbing state.
				IsGrabbing = false;
				return;
			}

			var isFalling = velocity.Y > 0;
			if (!isFalling)
			{
				// Only allow grabbing if the player is falling.
				return;
			}

			var grabLeft = IsGrabAvailableLeft();
			var grabRight = IsGrabAvailableRight();

			Vector2 collisionPoint = Vector2.Zero;
			if (grabLeft && grabRight)
			{
				GD.Print("Not implemented grab on both sides yet.");
			}
			else if (grabLeft)
			{
				collisionPoint = _grabLeftRayCast.GetCollisionPoint();
			}
			else if (grabRight)
			{
				collisionPoint = _grabRightRayCast.GetCollisionPoint();
			}

			if (IsPlayerInGrabDistance(collisionPoint))
			{
				_grabInProgress = true;
				IsGrabbing = true;
				velocity.Y = 0;
				velocity.X = Mathf.MoveToward(_player.Position.X, collisionPoint.X, 1200);
			}
		}

		public void Handle(ref Vector2 velocity)
		{
			if (Input.IsActionJustPressed(PlayerActionNames.Jump))
			{
				velocity.Y = _player.MovementData.JumpVelocity;
				velocity.X = _player.GetWallNormal().X * _player.MovementData.WallJumpPower;
				IsGrabbing = false;
			}

			if (Input.IsActionJustPressed(PlayerActionNames.Down))
			{
				IsGrabbing = false;
				PlayerMovementHelper.MoveDownOnePixel(_player);
			}

			if (Input.IsActionJustPressed(PlayerActionNames.Up))
			{
				// implement climb up
				// would likely involve a short animation and then snapping the player up and over the ledge
			}
		}

		private bool IsPlayerInGrabDistance(Vector2 grabCollision)
		{
			if (grabCollision == Vector2.Zero)
			{
				return false;
			}

			if (_player.IsOnWallOnly())
			{
				GD.Print($"Player is already against the wall, grabbing confirmed.");
				return true;
			}			
			else if (!_player.IsOnFloor())
			{
				var distanceToGrabPoint = (_player.Position - grabCollision).Length();
				GD.Print($"Distance to grab point: {distanceToGrabPoint}. Grab distance: {GrabDistance}");
				return distanceToGrabPoint <= GrabDistance;
			}

			return false;
		}

		private bool IsGrabAvailableLeft() => !_checkLeftRayCast.IsColliding() && _grabLeftRayCast.IsColliding();

		private bool IsGrabAvailableRight() => !_checkRightRayCast.IsColliding() && _grabRightRayCast.IsColliding();
	} 
}
