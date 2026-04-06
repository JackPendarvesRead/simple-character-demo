using Godot;

namespace SimpleCharacterDemo
{
	public partial class Player : CharacterBody2D
	{
		[Export]
		public PlayerMovementData MovementData;

		private Timer _coyoteJumpTimer;
		private RayCast2D _ledgeGrabRayCast;
		private RayCast2D _grabHandRayCast;
		private bool _isGrabbingLedge;
		private bool _isMovingRight;

		public override void _Ready()
		{
			_coyoteJumpTimer = GetNode<Timer>("CoyoteJumpTimer");
			_ledgeGrabRayCast = GetNode<RayCast2D>("LedgeGrabRayCast");
			_grabHandRayCast = GetNode<RayCast2D>("GrabHandRayCast");
		}

		public override void _PhysicsProcess(double delta)
		{
			Velocity = GetUpdatedVelocity(delta);
			UpdateDirection(Velocity);
			SetAnimation();

			var currentOnFloor = IsOnFloor();

			base.MoveAndSlide();

			if(currentOnFloor && !IsOnFloor())
			{
				_coyoteJumpTimer.Start();
			}
		}

		private void UpdateDirection(Vector2 velocity)
		{
			if(velocity.X == 0)
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
			CheckLedgeGrab(ref velocity);
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
			Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
			if (direction != Vector2.Zero)
			{
				HandleAcceleration(ref velocity, direction);
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
			}
		}

		private void HandleJump(ref Vector2 velocity)
		{
			if (Input.IsActionJustPressed("ui_accept"))
			{
				if (Input.IsActionPressed("ui_down"))
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

		private void HandleAcceleration(ref Vector2 velocity, Vector2 direction)
		{
			var acceleration = IsOnFloor() ? MovementData.Acceleration : MovementData.AirAcceleration;
			velocity.X = Mathf.MoveToward(velocity.X, MovementData.Speed * direction.X, acceleration);
		}

		private void HandleFriction(ref Vector2 velocity)
		{
			var friction = IsOnFloor() ? MovementData.Friction : MovementData.AirResistance;
			velocity.X = Mathf.MoveToward(velocity.X, 0, friction);
		}

		private void HandleLedgeGrab(ref Vector2 velocity)
		{
			if (Input.IsActionJustPressed("ui_accept"))
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
				GD.Print($"Grabbing ledge. IsOnFloor={IsOnFloor()}");
				_isGrabbingLedge = true;
				velocity = Vector2.Zero;
			}
		}

		private void SetAnimation()
		{
			AnimatedSprite2D animatedSprite = GetNode<AnimatedSprite2D>("Sprite");

			if (_isGrabbingLedge || IsOnWallOnly())
			{
				animatedSprite.Play("grab");
				animatedSprite.FlipH = GetWallNormal().X < 0;
				return;
			}

			if (Velocity.X != 0)
			{
				animatedSprite.FlipH = Velocity.X < 0;
			}

			if (!IsOnFloor())
			{
				animatedSprite.Play("jump");
			}
			else if (Velocity.X == 0 && IsOnFloor())
			{
				animatedSprite.Play("idle");
			}
			else
			{
				animatedSprite.Play("walk");
			}
		}
	}
}
