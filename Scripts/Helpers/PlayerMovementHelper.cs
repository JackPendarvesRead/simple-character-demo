using Godot;

namespace SimpleCharacterDemo.Scripts.Helpers
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
