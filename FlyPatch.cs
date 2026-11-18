
using HarmonyLib;
using Pyramid.Player;

namespace PyramidsFly
{
    [HarmonyPatch(typeof(PlayerMotor), "Update")]
    public static class FlyPatch
    {
        public static bool Prefix()
        {
            // Отключаем обычную физику только во время полёта.
            // Открытие меню F5 не отключает полёт.
            return !FlyController.Enabled;
        }
    }
}
