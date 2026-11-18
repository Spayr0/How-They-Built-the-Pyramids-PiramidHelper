
using HarmonyLib;
using Pyramid.World;

namespace PyramidsFly
{
    [HarmonyPatch(typeof(ShopService), "LockOf")]
    public static class BuildingUnlockPatch
    {
        public static void Postfix(
            ref ShopLock __result,
            ref long needBricks)
        {
            if (!FlyController.UnlockAllBuildings)
                return;

            if (__result == ShopLock.Bricks ||
                __result == ShopLock.LevelBricks ||
                __result == ShopLock.Permits)
            {
                __result = ShopLock.None;
                needBricks = 0L;
            }
        }
    }
}
