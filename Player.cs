using Godot;
using SimpleCharacterDemo.Constants;

namespace SimpleCharacterDemo
{
	public partial class Player : CharacterBody2D
	{
		[Export]
		public PlayerMovementData MovementData;

		private Grapple _grapple;

		private Timer _coyoteJumpTimer;
		private RayCast2D _ledgeGrabRayCast;
		private RayCast2D _grabHandRayCast;
		private bool _isGrabbingLedge;
		private bool _isMovingRight;

		// DEBUG
		private bool wasGrabbing = false;
		private bool wasGrappling = false;

		public override void _Ready()
		{
			_grapple = GetNode<Grapple>(PlayerNodeNames.Grapple);
			_coyoteJumpTimer = GetNode<Timer>(PlayerNodeNames.CoyoteJumpTimer);
			_ledgeGrabRayCast = GetNode<RayCast2D>(PlayerNodeNames.LedgeGrabRayCast);
			_grabHandRayCast = GetNode<RayCast2D>(PlayerNodeNames.GrabHandRayCast);
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
			_grapple.HandleGrapple(ref velocity);
			CheckLedgeGrab(ref velocity);

			if (_grapple.IsGrappleOnCooldown)
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

				if (IsOnWall())
				{
					if(MovementData.HasWallSpike && Input.IsActionPressed(PlayerActionNames.Up))
					{
						velocity.Y = Mathf.MoveToward(velocity.Y, 0, MovementData.WallSpikeStrength);
					}
					else if (velocity.Y > 0)
					{
						velocity.Y = Mathf.MoveToward(velocity.Y, 0, MovementData.WallFriction);
					}
				}
			}
		}

		private void HandleJump(ref Vector2 velocity)
		{
			if (Input.IsActionJustPressed(PlayerActionNames.Jump))
			{
				if (Input.IsActionPressed(PlayerActionNames.Down) && IsOnFloor())
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
