using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PyramidsFly
{
    [BepInPlugin("spayro.pyramidsfly", "Piramid Helper", "2.5.0")]
    public class Class1 : BaseUnityPlugin
    {
        public static Class1 Instance { get; private set; }

        public static ConfigEntry<bool> FlyEnabled;
        public static ConfigEntry<bool> UnlockBuildings;
        public static ConfigEntry<bool> InfiniteBricks;

        public static ConfigEntry<int> BrickType;
        public static ConfigEntry<int> VolleyCount;

        public static ConfigEntry<float> NormalSpeed;
        public static ConfigEntry<float> FastSpeed;
        public static ConfigEntry<float> VerticalSpeed;
        public static ConfigEntry<float> BrickFlightSpeed;

        public static ConfigEntry<int> MenuTheme;

        public static ConfigEntry<int> SpeedXp;
        public static ConfigEntry<int> StrengthXp;

        private void Awake()
        {
            Instance = this;

            FlyEnabled = Config.Bind(
                "Flight", "Enabled", false, "Включён ли полёт"
            );

            UnlockBuildings = Config.Bind(
                "Buildings", "UnlockAll", false, "Разблокировать все постройки"
            );

            InfiniteBricks = Config.Bind(
                "Trebuchet", "InfiniteBricks", false, "Бесконечные кирпичи"
            );

            BrickType = Config.Bind(
                "Trebuchet", "BrickType", -1, "Выбранный тип кирпича"
            );

            VolleyCount = Config.Bind(
                "Trebuchet", "VolleyCount", 12, "Количество кирпичей в залпе"
            );

            BrickFlightSpeed = Config.Bind(
                "Trebuchet", "BrickFlightSpeed", 1f,
                "Множитель скорости полёта кирпичей (1–100)"
            );

            NormalSpeed = Config.Bind(
                "Flight", "NormalSpeed", 10f, "Обычная скорость полёта"
            );

            FastSpeed = Config.Bind(
                "Flight", "FastSpeed", 25f, "Скорость полёта с Shift"
            );

            VerticalSpeed = Config.Bind(
                "Flight", "VerticalSpeed", 4f, "Вертикальная скорость полёта"
            );

            MenuTheme = Config.Bind(
                "Interface", "Theme", 0,
                "Тема меню: 0 — тёмная, 1 — светлая"
            );

            SpeedXp = Config.Bind(
                "PlayerStats", "SpeedXp", -1, "Сохранённый опыт скорости"
            );

            StrengthXp = Config.Bind(
                "PlayerStats", "StrengthXp", -1, "Сохранённый опыт силы"
            );

            // Проверяем допустимые значения настроек.
            BrickFlightSpeed.Value = Mathf.Clamp(
                BrickFlightSpeed.Value, 1f, 100f
            );

            NormalSpeed.Value = Mathf.Clamp(
                NormalSpeed.Value, 1f, 40f
            );

            FastSpeed.Value = Mathf.Clamp(
                FastSpeed.Value, 1f, 80f
            );

            VerticalSpeed.Value = Mathf.Clamp(
                VerticalSpeed.Value, 1f, 30f
            );

            VolleyCount.Value = Mathf.Clamp(
                VolleyCount.Value, 12, 100
            );

            BrickType.Value = Mathf.Clamp(
                BrickType.Value, -1, 4
            );

            MenuTheme.Value = Mathf.Clamp(
                MenuTheme.Value, 0, 1
            );

            Config.SaveOnConfigSet = true;
            Config.Save();

            gameObject.AddComponent<FlyController>();

            Harmony harmony = new Harmony("spayro.pyramidsfly");
            harmony.PatchAll();

            Logger.LogInfo("Piramid Helper 2.5 загружен!");
        }

        public static void SaveConfiguration()
        {
            if (Instance != null)
                Instance.Config.Save();
        }
    }
}
