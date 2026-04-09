using Godot;
using SimpleCharacterDemo.Abilities;
using SimpleCharacterDemo.Constants;
using SimpleCharacterDemo.Helpers;

namespace SimpleCharacterDemo
{
	public partial class Player : CharacterBody2D
	{
		[Export]
		public PlayerMovementData MovementData;

		private Grapple _grapple;
		private LedgeGrab _ledgeGrab;
		private Timer _coyoteJumpTimer;

		public override void _Ready()
		{
			_grapple = GetNode<Grapple>(PlayerNodeNames.Grapple);
			_ledgeGrab = GetNode<LedgeGrab>(PlayerNodeNames.LedgeGrab);
			_coyoteJumpTimer = GetNode<Timer>(PlayerNodeNames.CoyoteJumpTimer);
		}

		public override void _PhysicsProcess(double delta)
		{
			Velocity = GetUpdatedVelocity(delta);
			
			SetAnimation();

			var currentOnFloor = IsOnFloor();

			MoveAndSlide();

			if (currentOnFloor && !IsOnFloor())
			{
				_coyoteJumpTimer.Start();
			}
		}

		private Vector2 GetUpdatedVelocity(double delta)
		{
			Vector2 velocity = Velocity;
			_grapple.HandleGrapple(ref velocity);
			_ledgeGrab.Check(ref velocity);

			if (_grapple.IsGrappleOnCooldown)
			{
				return velocity;
			}

			if (_ledgeGrab.IsGrabbing)
			{
				_ledgeGrab.Handle(ref velocity);
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
					PlayerMovementHelper.MoveDownOnePixel(this);
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

		private void SetAnimation()
		{
			AnimatedSprite2D animatedSprite = GetNode<AnimatedSprite2D>(PlayerNodeNames.AnimatedSprite);

			if (_ledgeGrab.IsGrabbing || IsOnWallOnly())
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
