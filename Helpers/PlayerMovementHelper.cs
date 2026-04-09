using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCharacterDemo.Helpers
{
    internal static class PlayerMovementHelper
    {
        public static void MoveDownOnePixel(CharacterBody2D characterBody)
        {
            var p = characterBody.Position;
            p.Y += 1;
            characterBody.Position = p;
            return;
        }
    }
}
