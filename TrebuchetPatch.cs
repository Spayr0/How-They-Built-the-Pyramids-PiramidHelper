
using System.Collections.Generic;
using HarmonyLib;
using Pyramid.Build;
using Pyramid.Chaos;
using Pyramid.Data;
using Pyramid.World;
using UnityEngine;

namespace PyramidsFly
{
    // Изменяем реальное количество кирпичей в залпе
    [HarmonyPatch(typeof(BrickTrebuchet), "get_Volley")]
    public static class TrebuchetVolleyPatch
    {
        private static void Postfix(ref int __result)
        {
            if (!FlyController.InfiniteTrebuchet)
                return;

            if (FlyController.SelectedTrebuchetBrick < 0 ||
                FlyController.SelectedTrebuchetBrick > 4)
                return;

            __result = Mathf.Clamp(
                FlyController.TrebuchetVolley,
                12,
                100
            );
        }
    }

    // Бесконечное количество кирпичей
    [HarmonyPatch(typeof(BrickTrebuchet), "get_Loaded")]
    public static class TrebuchetLoadedPatch
    {
        private static void Postfix(BrickTrebuchet __instance, ref int __result)
        {
            if (!FlyController.InfiniteTrebuchet)
                return;

            int selected = FlyController.SelectedTrebuchetBrick;

            if (selected < 0 || selected > 4)
            {
                __result = 0;
                return;
            }

            __result = FlyController.TrebuchetVolley;
        }
    }

    // Отображение кирпичей
    [HarmonyPatch(typeof(BrickTrebuchet), "get_DisplayBricks")]
    public static class TrebuchetDisplayPatch
    {
        private static void Postfix(BrickTrebuchet __instance, ref int __result)
        {
            if (!FlyController.InfiniteTrebuchet)
                return;

            int selected = FlyController.SelectedTrebuchetBrick;

            if (selected < 0 || selected > 4)
            {
                __result = 0;
                return;
            }

            __result = FlyController.TrebuchetVolley;
        }
    }

    // Выбранный тип кирпича непосредственно перед выстрелом
    [HarmonyPatch(typeof(BrickTrebuchet), "ServerFire")]
    public static class TrebuchetFireCheckPatch
    {
        private struct FireState
        {
            public bool Changed;
            public BrickStack OriginalStack;
        }

        private static bool Prefix(
            BrickTrebuchet __instance,
            ref bool __result,
            out FireState __state)
        {
            __state = default;

            if (!FlyController.InfiniteTrebuchet)
                return true;

            int selected = FlyController.SelectedTrebuchetBrick;

            // Обязательный выбор кирпича
            if (selected < 0 || selected > 4)
            {
                __result = false;
                return false;
            }

            BrickKind kind;

            switch (selected)
            {
                case 0:
                    kind = BrickKind.Plain;
                    break;

                case 1:
                    kind = BrickKind.Polished;
                    break;

                case 2:
                    kind = BrickKind.Glazed;
                    break;

                case 3:
                case 4:
                    kind = BrickKind.Golden;
                    break;

                default:
                    __result = false;
                    return false;
            }

            // Сохраняем настоящий запас
            __state.OriginalStack = __instance.LoadStack;
            __state.Changed = true;

            // Временно подставляем выбранные кирпичи
            __instance._load.Value =
                BrickStack.Of(kind, FlyController.TrebuchetVolley);

            return true;
        }

        private static void Postfix(
            BrickTrebuchet __instance,
            FireState __state)
        {
            if (!__state.Changed)
                return;

            // Возвращаем настоящий запас без расхода кирпичей
            __instance._load.Value = __state.OriginalStack;
        }
    }

    // Убираем ограничение дальности и высоты
    // при поиске целей для залпа
    [HarmonyPatch(typeof(BrickTrebuchet), "NearestReachableCells")]
    public static class TrebuchetReachableCellsPatch
    {
        private static bool Prefix(
            PyramidState py,
            Vector3 origin,
            int max,
            HashSet<int> reserved,
            List<int> result,
            ref int __result)
        {
            if (!FlyController.InfiniteTrebuchet)
                return true;

            result.Clear();

            if (py == null || max <= 0)
            {
                __result = 0;
                return false;
            }

            List<KeyValuePair<float, int>> scored =
                new List<KeyValuePair<float, int>>();

            PyramidLayout layout = py.Layout;

            foreach (int sectionIndex in py.Progress.OpenSections)
            {
                PyramidSection section =
                    layout.Sections[sectionIndex];

                for (int i = section.FirstCell;
                     i < section.FirstCell + section.Count;
                     i++)
                {
                    if (py.IsTaken(i))
                        continue;

                    if (reserved != null && reserved.Contains(i))
                        continue;

                    if (PyramidOccupancy.IsBlocked(py, i, out _))
                        continue;

                    Vector3 cell = py.CellWorldCenter(i);

                    float dx = cell.x - origin.x;
                    float dz = cell.z - origin.z;

                    float distance = dx * dx + dz * dz;

                    scored.Add(
                        new KeyValuePair<float, int>(distance, i));
                }
            }

            scored.Sort((a, b) => a.Key.CompareTo(b.Key));

            for (int i = 0; i < scored.Count && result.Count < max; i++)
            {
                result.Add(scored[i].Value);
            }

            __result = result.Count;
            return false;
        }
    }

    // Убираем ограничения при поиске цели для прицела
    [HarmonyPatch(typeof(BrickTrebuchet), "NearestReachableCell")]
    public static class TrebuchetReachableCellPatch
    {
        private static bool Prefix(
            PyramidState py,
            Vector3 near,
            HashSet<int> reserved,
            ICollection<int> exclude,
            Vector3 origin,
            ref int __result)
        {
            if (!FlyController.InfiniteTrebuchet)
                return true;

            __result = -1;

            if (py == null)
                return false;

            float bestDistance = float.MaxValue;

            PyramidLayout layout = py.Layout;

            foreach (int sectionIndex in py.Progress.OpenSections)
            {
                PyramidSection section =
                    layout.Sections[sectionIndex];

                for (int i = section.FirstCell;
                     i < section.FirstCell + section.Count;
                     i++)
                {
                    if (py.IsTaken(i))
                        continue;

                    if (reserved != null && reserved.Contains(i))
                        continue;

                    if (exclude != null && exclude.Contains(i))
                        continue;

                    if (PyramidOccupancy.IsBlocked(py, i, out _))
                        continue;

                    Vector3 cell = py.CellWorldCenter(i);

                    float distance = (cell - near).sqrMagnitude;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        __result = i;
                    }
                }
            }

            return false;
        }
    }
}
