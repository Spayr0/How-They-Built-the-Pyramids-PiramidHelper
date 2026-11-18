using System;
using System.Collections.Generic;
using System.Threading;
using FishNet.Object;
using HarmonyLib;
using Pyramid.Chaos;
using Pyramid.Data;
using Pyramid.Engrave;
using Pyramid.World;
using UnityEngine;

namespace PyramidsFly
{
    [HarmonyPatch(
        typeof(EngraveCarry),
        "ServerClaimFrom",
        new Type[] { typeof(IBrickStore), typeof(BrickKind) }
    )]
    public static class TrebuchetEngravingPatch
    {
        private static int claimCallCount;

        // __0 и __1 привязываются к аргументам оригинального метода по позиции,
        // а не по именам параметров.
        private static bool Prefix(
            IBrickStore __0,
            BrickKind __1,
            ref ushort __result)
        {
            bool isTrebuchet = __0 is BrickTrebuchet;

            // Важно: эта запись выполняется ДО проверок режима.
            // Так мы узнаем, вызывается ли Prefix вообще и проходят ли условия.
            if (isTrebuchet)
            {
                Debug.Log(
                    "[Piramid Helper][Engraving] Prefix вызван: " +
                    "InfiniteTrebuchet=" + FlyController.InfiniteTrebuchet +
                    ", SelectedTrebuchetBrick=" + FlyController.SelectedTrebuchetBrick +
                    ", kind=" + __1 +
                    ", store=" + (__0 == null ? "null" : __0.GetType().FullName)
                );
            }

            if (!FlyController.InfiniteTrebuchet ||
                FlyController.SelectedTrebuchetBrick != 4 ||
                !isTrebuchet)
            {
                return true;
            }

            int callNumber = Interlocked.Increment(ref claimCallCount);

            Debug.Log(
                "[Piramid Helper][Engraving] Целевой вызов #" +
                callNumber + ": начинаем создание рисунка."
            );

            NetworkBehaviour networkBehaviour = __0 as NetworkBehaviour;
            if (networkBehaviour == null)
            {
                Debug.LogWarning(
                    "[Piramid Helper][Engraving] Вызов #" + callNumber +
                    ": BrickTrebuchet не является NetworkBehaviour. Оригинал заблокирован."
                );
                __result = 0;
                return false;
            }

            EngravingRegistry registry = EngravingRegistry.Active;
            if (registry == null)
            {
                Debug.LogWarning(
                    "[Piramid Helper][Engraving] Вызов #" + callNumber +
                    ": EngravingRegistry.Active == null."
                );
                __result = 0;
                return false;
            }

            if (!registry.IsServerInitialized)
            {
                Debug.LogWarning(
                    "[Piramid Helper][Engraving] Вызов #" + callNumber +
                    ": EngravingRegistry не инициализирован на сервере."
                );
                __result = 0;
                return false;
            }

            List<EngraveStrokes.Stroke> strokes =
                new List<EngraveStrokes.Stroke>();

            strokes.Add(new EngraveStrokes.Stroke
            {
                Pigment = Pigment.Gold,
                Width = 8,
                Mirror = false,
                XY = new byte[]
                {
                    105, 105,
                    105, 80,
                    115, 60,
                    135, 48,
                    155, 45,
                    175, 48,
                    195, 60,
                    205, 80,
                    205, 105,
                    195, 120,
                    195, 190,
                    115, 190,
                    115, 120,
                    105, 105
                }
            });

            strokes.Add(new EngraveStrokes.Stroke
            {
                Pigment = Pigment.Gold,
                Width = 8,
                Mirror = false,
                XY = new byte[]
                {
                    115, 185,
                    95, 185,
                    75, 195,
                    65, 215,
                    75, 235,
                    100, 245,
                    125, 235,
                    145, 210
                }
            });

            strokes.Add(new EngraveStrokes.Stroke
            {
                Pigment = Pigment.Gold,
                Width = 8,
                Mirror = false,
                XY = new byte[]
                {
                    195, 185,
                    215, 185,
                    235, 195,
                    245, 215,
                    235, 235,
                    210, 245,
                    185, 235,
                    165, 210
                }
            });

            try
            {
                byte[] drawing = EngraveStrokes.Encode(strokes);
                string why;

                ushort id = registry.ServerCreate(
                    null,
                    drawing,
                    100,
                    0,
                    BrickKind.Golden,
                    out why
                );

                Debug.Log(
                    "[Piramid Helper][Engraving] Вызов #" + callNumber +
                    ": ServerCreate id=" + id +
                    ", why=\"" + (why ?? "null") + "\"."
                );

                if (id == 0)
                {
                    Debug.LogWarning(
                        "[Piramid Helper][Engraving] Вызов #" + callNumber +
                        ": рисунок не создан; оригинал заблокирован."
                    );
                    __result = 0;
                    return false;
                }

                var storeKey = EngraveCarry.StoreKey(networkBehaviour);

                Debug.Log(
                    "[Piramid Helper][Engraving] Вызов #" + callNumber +
                    ": вызываем ServerPutInto для id=" + id +
                    ", StoreKey=" + Convert.ToString(storeKey) + "."
                );

                registry.ServerPutInto(storeKey, id);

                Debug.Log(
                    "[Piramid Helper][Engraving] Вызов #" + callNumber +
                    ": ServerPutInto завершился для id=" + id + "."
                );

                __result = id;
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[Piramid Helper][Engraving] Исключение в вызове #" +
                    callNumber + ": " + exception
                );
                __result = 0;
                return false;
            }
        }
    }
}
