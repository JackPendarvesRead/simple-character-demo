using Godot;
using SimpleCharacterDemo.Constants;

namespace SimpleCharacterDemo
{
	public partial class Player : CharacterBody2D
	{
		[Export]
		public PlayerMovementData MovementData;

		private Timer _coyoteJumpTimer;
		private Timer _grappleTimer;
		private RayCast2D _ledgeGrabRayCast;
		private RayCast2D _grabHandRayCast;
		private RayCast2D _grappleRayCast;
		private Line2D _grappleRope;
		private Polygon2D _aimCursor;
		private bool _isGrabbingLedge;
		private bool _isMovingRight;
		private bool _isGrappling;

		// DEBUG
		private bool wasGrabbing = false;
		private bool wasGrappling = false;

		public override void _Ready()
		{
			_coyoteJumpTimer = GetNode<Timer>(PlayerNodeNames.CoyoteJumpTimer);
			_grappleTimer = GetNode<Timer>(PlayerNodeNames.GrappleTimer);
			_ledgeGrabRayCast = GetNode<RayCast2D>(PlayerNodeNames.LedgeGrabRayCast);
			_grabHandRayCast = GetNode<RayCast2D>(PlayerNodeNames.GrabHandRayCast);
			_grappleRayCast = GetNode<RayCast2D>(PlayerNodeNames.GrappleRayCast);
			_grappleRope = GetNode<Line2D>(PlayerNodeNames.GrappleRope);
			_aimCursor = GetNode<Polygon2D>(PlayerNodeNames.AimCursor);
		}

		public override void _Process(double delta)
		{
			_grappleRope.Visible = !_grappleTimer.IsStopped();
			_aimCursor.Visible = IsAiming();

			if(_isGrappling != wasGrappling)
			{
				GD.Print($"Grappling state changed: {_isGrappling}");
				wasGrappling = _isGrappling;
			}

			if(_isGrabbingLedge != wasGrabbing)
			{
				GD.Print($"Grabbing state changed: {_isGrabbingLedge}");
				wasGrabbing = _isGrabbingLedge;
			}
		}

		public override void _PhysicsProcess(double delta)
		{
			Velocity = GetUpdatedVelocity(delta);
			UpdateDirection(Velocity);
			SetAnimation();

			var currentOnFloor = IsOnFloor();

			base.MoveAndSlide();

			if (currentOnFloor && !IsOnFloor())
			{
				_coyoteJumpTimer.Start();
			}
		}

		private void HandleGrapple(ref Vector2 velocity)
		{
			if (_isGrappling)
			{
				if(_grappleTimer.IsStopped() 
					&& (IsOnCeiling() || IsOnWall() || IsOnFloor()))
				{
					_isGrappling = false;
				}

				return;
			}

			var aim = GetAimVector();
			if (aim != Vector2.Zero)
			{
				_grappleRayCast.Rotation = aim.Angle() - Mathf.Pi / 2;
				_grappleRayCast.ForceRaycastUpdate();

				if (Input.IsActionPressed(PlayerActionNames.Grapple))
				{
					if (_grappleRayCast.IsColliding())
					{
						_isGrappling = true;
						var grapplePoint = _grappleRayCast.GetCollisionPoint();
						var direction = (grapplePoint - Position).Normalized();
						velocity = direction * MovementData.GrapplePower;
						_grappleTimer.Start();
					}
				}
			}
		}

		private void UpdateDirection(Vector2 velocity)
		{
			if (velocity.X == 0)
			{
				return;
			}

			_isMovingRight = velocity.X > 0;
			_grabHandRayCast.RotationDegrees = _isMovingRight ? 270 : 90;
			_grabHandRayCast.ForceRaycastUpdate();
			_ledgeGrabRayCast.RotationDegrees = _isMovingRight ? 270 : 90;
			_ledgeGrabRayCast.ForceRaycastUpdate();
		}

		private Vector2 GetUpdatedVelocity(double delta)
		{
			Vector2 velocity = Velocity;
			HandleGrapple(ref velocity);
			CheckLedgeGrab(ref velocity);

			if (!_grappleTimer.IsStopped())
			{
				return velocity;
			}

			if (_isGrabbingLedge)
			{
				HandleLedgeGrab(ref velocity);
			}
			else
			{
				HandleGravity(ref velocity, delta);
				HandleJump(ref velocity);
				HandleHorizontalMovement(ref velocity);
			}

			return velocity;
		}

		private void HandleHorizontalMovement(ref Vector2 velocity)
		{
			var horizontalAxis = Input.GetAxis(PlayerActionNames.MoveLeft, PlayerActionNames.MoveRight);
			if (horizontalAxis != 0)
			{
				HandleAcceleration(ref velocity, horizontalAxis);
			}
			else
			{
				HandleFriction(ref velocity);
			}
		}

		private void HandleGravity(ref Vector2 velocity, double delta)
		{
			if (!IsOnFloor())
			{
				velocity += GetGravity() * (float)delta * MovementData.GravityScale;

				if (IsOnWall() && velocity.Y > 0)
				{
					velocity.Y = Mathf.MoveToward(velocity.Y, 0, MovementData.WallFriction);
				}
			}
		}

		private void HandleJump(ref Vector2 velocity)
		{
			if (Input.IsActionJustPressed(PlayerActionNames.Jump))
			{
				if (Input.IsActionPressed(PlayerActionNames.Down))
				{
					var p = Position;
					p.Y += 1;
					Position = p;
					return;
				}

				if (IsOnFloor() || !_coyoteJumpTimer.IsStopped())
				{
					velocity.Y = MovementData.JumpVelocity;
					_coyoteJumpTimer.Stop();
				}
				else if (IsOnWall())
				{
					velocity.Y = MovementData.JumpVelocity;
					velocity.X = GetWallNormal().X * MovementData.WallJumpPower;
				}
			}
		}

		private void HandleAcceleration(ref Vector2 velocity, float horizontalAxis)
		{
			var acceleration = IsOnFloor() ? MovementData.Acceleration : MovementData.AirAcceleration;
			velocity.X = Mathf.MoveToward(velocity.X, MovementData.Speed * horizontalAxis, acceleration);
		}

		private void HandleFriction(ref Vector2 velocity)
		{
			var friction = IsOnFloor() ? MovementData.Friction : MovementData.AirResistance;
			velocity.X = Mathf.MoveToward(velocity.X, 0, friction);
		}

		private void HandleLedgeGrab(ref Vector2 velocity)
		{
			if (Input.IsActionJustPressed(PlayerActionNames.Jump))
			{
				velocity.Y = MovementData.JumpVelocity;
				velocity.X = GetWallNormal().X * MovementData.WallJumpPower;
				_isGrabbingLedge = false;
			}
		}

		private void CheckLedgeGrab(ref Vector2 velocity)
		{
			var isFalling = velocity.Y > 0;
			var grabAvailable = _ledgeGrabRayCast.IsColliding() && !_grabHandRayCast.IsColliding();
			var canGrab = isFalling && grabAvailable && !_isGrabbingLedge && IsOnWallOnly();

			if (canGrab)
			{
				_isGrabbingLedge = true;
				velocity = Vector2.Zero;
			}
			else if(_isGrabbingLedge && !IsOnWall())
			{
				_isGrabbingLedge = false;
			}
		}

		private void SetAnimation()
		{
			AnimatedSprite2D animatedSprite = GetNode<AnimatedSprite2D>(PlayerNodeNames.AnimatedSprite);

			if (_isGrabbingLedge || IsOnWallOnly())
			{
				animatedSprite.Play(PlayerAnimationNames.Grab);
				animatedSprite.FlipH = GetWallNormal().X < 0;
				return;
			}

			if (Velocity.X != 0)
			{
				animatedSprite.FlipH = Velocity.X < 0;
			}

			if (!IsOnFloor())
			{
				animatedSprite.Play(PlayerAnimationNames.Jump);
			}
			else if (Velocity.X == 0 && IsOnFloor())
			{
				animatedSprite.Play(PlayerAnimationNames.Idle);
			}
			else
			{
				animatedSprite.Play(PlayerAnimationNames.Walk);
			}
		}

		private Vector2 GetAimVector() => Input.GetVector(
					PlayerActionNames.AimLeft,
					PlayerActionNames.AimRight,
					PlayerActionNames.AimUp,
					PlayerActionNames.AimDown);

		private bool IsAiming() => GetAimVector() != Vector2.Zero;
	}    
}
