using Godot;
using SimpleCharacterDemo.Scripts.Constants;
using SimpleCharacterDemo.Scripts.Helpers;

namespace SimpleCharacterDemo.Abilities
{
	public partial class Grapple : Node2D
	{
		CharacterBody2D _character;
		private Line2D _grappleRope;
		private Polygon2D _aimCursor;
		private RayCast2D _grappleRayCast;
		private Timer _grappleTimer;

		public bool IsGrappling { get; private set; } = false;

		public bool IsGrappleOnCooldown => !_grappleTimer.IsStopped();

		[Export]
		public float GrapplePower = 400.0f;

		public override void _Ready()
		{
			_character = GetParent<CharacterBody2D>();
			_grappleRayCast = GetNode<RayCast2D>(GrappleNodeNames.GrappleRayCast);
			_grappleTimer = GetNode<Timer>(GrappleNodeNames.GrappleTimer);
			_grappleRope = GetNode<Line2D>(GrappleNodeNames.GrappleRope);
			_aimCursor = GetNode<Polygon2D>(GrappleNodeNames.AimCursor);
		}

		public override void _Process(double delta)
		{
			_grappleRope.Visible = !_grappleTimer.IsStopped();
			_aimCursor.Visible = InputHelpers.PlayerIsAiming();
		}

		public void HandleGrapple(ref Vector2 velocity)
		{
			if (IsGrappling)
			{
				if (_grappleTimer.IsStopped() && CharacterHasLanded())
				{
					IsGrappling = false;
				}

				return;
			}

			var aim = InputHelpers.GetPlayerAimVector();
			if (aim != Vector2.Zero)
			{
				Rotation = aim.Angle();
				//_grappleRayCast.Rotation = aim.Angle() - Mathf.Pi / 2;
				//_grappleRayCast.ForceRaycastUpdate();

				if (Input.IsActionJustPressed(PlayerActionNames.Grapple))
				{
					if (_grappleRayCast.IsColliding())
					{
						IsGrappling = true;
						var grapplePoint = _grappleRayCast.GetCollisionPoint();
						var direction = (grapplePoint - _character.Position).Normalized();
						velocity = direction * GrapplePower;
						_grappleTimer.Start();
					}
				}
			}
		}

		private bool CharacterHasLanded()
			=> _character.IsOnCeiling() 
			|| _character.IsOnWall()
			|| _character.IsOnFloor();
	}
}
