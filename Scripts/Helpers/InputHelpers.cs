using Godot;
using SimpleCharacterDemo.Scripts.Constants;

namespace SimpleCharacterDemo.Scripts.Helpers
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
