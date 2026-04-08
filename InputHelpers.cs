using Godot;
using SimpleCharacterDemo.Constants;

namespace SimpleCharacterDemo
{
    internal static class InputHelpers
    {
        public static Vector2 GetPlayerAimVector() => Input.GetVector(
            PlayerActionNames.AimLeft,
            PlayerActionNames.AimRight,
            PlayerActionNames.AimUp,
            PlayerActionNames.AimDown);

        public static bool PlayerIsAiming() => GetPlayerAimVector() != Vector2.Zero;
    }
}
