using HarmonyLib;
using Pyramid.Chaos;
using UnityEngine;

namespace PyramidsFly
{
    [HarmonyPatch(typeof(BrickTrebuchet), nameof(BrickTrebuchet.BrickArc))]
    public static class TrebuchetFlightSpeedPatch
    {
        private static void Postfix(ref BallisticArc __result)
        {
            // Значение обновляется слайдером и загружается из конфига в FlyController.
            float multiplier = Mathf.Clamp(
                FlyController.TrebuchetSpeedMultiplier,
                1f,
                100f
            );

            if (multiplier <= 1.001f)
                return;

            BallisticArc original = __result;

            // Не изменяем некорректную траекторию.
            if (original.Gravity <= 0f)
                return;

            float apex = original.From.y +
                original.VerticalSpeed * original.VerticalSpeed /
                (2f * original.Gravity);

            float newGravity = original.Gravity * multiplier * multiplier;

            __result = BallisticArc.Through(
                original.From,
                original.To,
                apex,
                newGravity
            );
        }
    }
}
