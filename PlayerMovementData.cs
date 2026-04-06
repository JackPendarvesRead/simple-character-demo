using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCharacterDemo
{
    [GlobalClass]
    public partial class PlayerMovementData : Resource
    {
        [Export]
        public float Speed = 200.0f;

        [Export]
        public float Acceleration = 20.0f;

        [Export]
        public float Friction = 30f;

        [Export]
        public float JumpVelocity = -400.0f;

        [Export]
        public float GravityScale = 1.0f;

        [Export]
        public float WallJumpPower = 200.0f;

        [Export]
        public float AirResistance = 10.0f;

        [Export]
        public float AirAcceleration = 10.0f;
    }
}
