using HarmonyLib;
using Pyramid.Chaos;

namespace PyramidsFly
{
    #region БЕЗ ОГРАНИЧЕНИЙ ДАЛЬНОСТИ И ВЫСОТЫ

    // Убираем ограничение дальности только при включённой бесконечной катапульте.
    [HarmonyPatch(typeof(BrickTrebuchet), "MaxRange", MethodType.Getter)]
    public static class TrebuchetMaxRangePatch
    {
        private static void Postfix(ref float __result)
        {
            if (!FlyController.InfiniteTrebuchet)
                return;

            __result = 1000000f;
        }
    }

    // Убираем ограничение высоты только при включённой бесконечной катапульте.
    [HarmonyPatch(typeof(BrickTrebuchet), "WithinRise")]
    public static class TrebuchetHeightPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!FlyController.InfiniteTrebuchet)
                return true;

            __result = true;
            return false;
        }
    }

    #endregion
}
