using System;
using System.Reflection;
using HarmonyLib;
using Pyramid.CameraRig;

namespace PyramidsFly
{
    [HarmonyPatch(typeof(GameCamera), "Update")]
    public static class GameCameraPatch
    {
        private static readonly FieldInfo YawField =
            typeof(GameCamera).GetField(
                "_yaw",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        private static readonly FieldInfo PitchField =
            typeof(GameCamera).GetField(
                "_pitch",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        private struct CameraState
        {
            public bool ShouldRestore;
            public float Yaw;
            public float Pitch;
        }

        [HarmonyPrefix]
        private static void Prefix(GameCamera __instance, out CameraState __state)
        {
            __state = default(CameraState);

            if (!FlyController.MenuOpen || __instance == null)
                return;

            if (YawField == null || PitchField == null)
                return;

            try
            {
                object yawObject = YawField.GetValue(__instance);
                object pitchObject = PitchField.GetValue(__instance);

                if (!(yawObject is float) || !(pitchObject is float))
                    return;

                __state.Yaw = (float)yawObject;
                __state.Pitch = (float)pitchObject;
                __state.ShouldRestore = true;
            }
            catch (Exception)
            {
                __state = default(CameraState);
            }
        }

        [HarmonyPostfix]
        private static void Postfix(GameCamera __instance, CameraState __state)
        {
            if (!__state.ShouldRestore || __instance == null)
                return;

            if (YawField == null || PitchField == null)
                return;

            try
            {
                YawField.SetValue(__instance, __state.Yaw);
                PitchField.SetValue(__instance, __state.Pitch);
            }
            catch (Exception)
            {
                // Не прерываем работу игры, если поля камеры недоступны.
            }
        }
    }
}
