
using HarmonyLib;
using Pyramid.Player;
using Pyramid.CameraRig;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PyramidsFly
{
    // Разрешаем игровому персонажу двигаться при открытом меню.
    [HarmonyPatch(typeof(GameCamera), "get_InputLocked")]
    public static class CameraInputLockPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (FlyController.MenuOpen)
                __result = false;
        }
    }

    // Передаём обычному PlayerMotor управление WASD.
    [HarmonyPatch(typeof(PlayerMotor), "Update")]
    public static class PlayerMotorPatch
    {
        private static void Prefix(PlayerMotor __instance)
        {
            if (!FlyController.MenuOpen)
                return;

            __instance.Pinned = false;

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                __instance.TestMoveInput = Vector2.zero;
                return;
            }

            Vector2 movement = Vector2.zero;

            if (keyboard.wKey.isPressed) movement.y += 1f;
            if (keyboard.sKey.isPressed) movement.y -= 1f;
            if (keyboard.dKey.isPressed) movement.x += 1f;
            if (keyboard.aKey.isPressed) movement.x -= 1f;

            __instance.TestMoveInput =
                Vector2.ClampMagnitude(movement, 1f);
        }

        private static void Postfix(PlayerMotor __instance)
        {
            if (FlyController.MenuOpen)
                __instance.TestMoveInput = Vector2.zero;
        }
    }
}
