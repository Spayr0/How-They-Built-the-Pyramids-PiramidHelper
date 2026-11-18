using FishNet.Managing;
using FishNet;
using Pyramid.Player;
using Pyramid.Data;
using Pyramid.Training;
using Pyramid.Ending;
using Pyramid.World;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PyramidsFly
{
    public class FlyController : MonoBehaviour
    {
        public static bool Enabled;
        public static bool UnlockAllBuildings;
        public static bool InfiniteTrebuchet;
        public static int SelectedTrebuchetBrick = -1;
        public static int TrebuchetVolley = 12;
        public static float TrebuchetSpeedMultiplier = 1f;
        public static bool MenuOpen { get; private set; }

        private PlayerMotor motor;
        private float normalSpeed = 10f;
        private float fastSpeed = 25f;
        private float verticalSpeed = 4f;
        private float brickFlightSpeed = 1f;
        private int theme;
        private int speedXpSlider;
        private int strengthXpSlider;

        private string statsMessage = "Перемещай ползунки для изменения XP";
        private string coinsMessage = "Нажми, чтобы получить 10 000 монет";
        private bool showMenu;
        private bool showTrebuchetOptions = true;
        private bool hasWindowPosition;

        private Rect windowRect = new Rect(80, 60, 1000, 820);
        private CursorLockMode previousLock;
        private bool previousVisible;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle buttonStyle;

        private float flightAnim = -10f;
        private float buildingsAnim = -10f;
        private float infiniteAnim = -10f;
        private float coinsAnim = -10f;
        private float closeAnim = -10f;

        private const int FlightButtonID = 1101;
        private const int BuildingsButtonID = 1102;
        private const int InfiniteButtonID = 1201;
        private const int CoinsButtonID = 1501;
        private const int CloseButtonID = 1103;
        private const int SpeedXpSliderID = 1301;
        private const int StrengthXpSliderID = 1302;

        private Color Bg => theme == 0
            ? new Color(.055f, .043f, .028f)
            : new Color(.88f, .85f, .77f);

        private Color Panel => theme == 0
            ? new Color(.105f, .078f, .045f)
            : new Color(.96f, .94f, .87f);

        private Color Text => theme == 0
            ? new Color(.94f, .88f, .72f)
            : new Color(.20f, .16f, .10f);

        private Color Muted => theme == 0
            ? new Color(.66f, .57f, .40f)
            : new Color(.43f, .35f, .23f);

        private Color Gold => new Color(.78f, .54f, .16f);
        private Color Green => new Color(.16f, .43f, .10f);

        private void Start()
        {
            Enabled = Class1.FlyEnabled.Value;
            UnlockAllBuildings = Class1.UnlockBuildings.Value;

            // Бесконечные кирпичи намеренно выключаются после перезапуска игры.
            InfiniteTrebuchet = false;
            Class1.InfiniteBricks.Value = false;

            SelectedTrebuchetBrick = Mathf.Clamp(Class1.BrickType.Value, -1, 4);
            TrebuchetVolley = Mathf.Clamp(Class1.VolleyCount.Value, 12, 100);

            normalSpeed = Mathf.Clamp(Class1.NormalSpeed.Value, 1f, 40f);
            fastSpeed = Mathf.Clamp(Class1.FastSpeed.Value, 1f, 80f);
            verticalSpeed = Mathf.Clamp(Class1.VerticalSpeed.Value, 1f, 30f);

            brickFlightSpeed = Mathf.Clamp(Class1.BrickFlightSpeed.Value, 1f, 100f);
            TrebuchetSpeedMultiplier = brickFlightSpeed;
            theme = Mathf.Clamp(Class1.MenuTheme.Value, 0, 1);

            Class1.SaveConfiguration();

            StartCoroutine(RestoreSavedPlayerStats());
            StartCoroutine(DisableTrebuchetOnStartup());
        }

        private IEnumerator DisableTrebuchetOnStartup()
        {
            yield return new WaitForSeconds(.5f);

            InfiniteTrebuchet = false;
            Class1.InfiniteBricks.Value = false;
            Class1.SaveConfiguration();
        }

        private void SaveSettings()
        {
            Class1.FlyEnabled.Value = Enabled;
            Class1.UnlockBuildings.Value = UnlockAllBuildings;
            Class1.InfiniteBricks.Value = InfiniteTrebuchet;
            Class1.BrickType.Value = SelectedTrebuchetBrick;
            Class1.VolleyCount.Value = TrebuchetVolley;

            Class1.NormalSpeed.Value = normalSpeed;
            Class1.FastSpeed.Value = fastSpeed;
            Class1.VerticalSpeed.Value = verticalSpeed;

            Class1.BrickFlightSpeed.Value = brickFlightSpeed;
            Class1.MenuTheme.Value = theme;

            TrebuchetSpeedMultiplier = brickFlightSpeed;

            Class1.SaveConfiguration();
        }

        private void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null)
                return;

            if (k.f5Key.wasPressedThisFrame)
            {
                if (showMenu) CloseMenu();
                else OpenMenu();
            }

            if (showMenu && k.escapeKey.wasPressedThisFrame)
            {
                CloseMenu();
                return;
            }

            if (k.f6Key.wasPressedThisFrame)
                ToggleFlight();

            if (MenuOpen && !Enabled)
                return;

            if (!Enabled)
                return;

            FindMotor();

            if (motor == null || motor.Controller == null)
                return;

            Camera cam = Camera.main;
            if (cam == null)
                return;

            Vector3 forward = cam.transform.forward;
            Vector3 right = cam.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 direction = Vector3.zero;

            if (k.wKey.isPressed) direction += forward;
            if (k.sKey.isPressed) direction -= forward;
            if (k.dKey.isPressed) direction += right;
            if (k.aKey.isPressed) direction -= right;

            float vertical =
                (k.spaceKey.isPressed ? 1f : 0f) -
                ((k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed) ? 1f : 0f);

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            float speed = (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)
                ? fastSpeed
                : normalSpeed;

            motor.Controller.Move(
                (direction * speed + Vector3.up * vertical * verticalSpeed) * Time.deltaTime
            );
        }

        private void ToggleFlight()
        {
            Enabled = !Enabled;
            FindMotor();

            if (Enabled)
                ResetVelocity();

            SaveSettings();
        }

        private void OpenMenu()
        {
            showMenu = true;
            MenuOpen = true;

            if (!hasWindowPosition)
            {
                windowRect.x = (Screen.width - windowRect.width) / 2f;
                windowRect.y = (Screen.height - windowRect.height) / 2f;
                hasWindowPosition = true;
            }

            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CloseMenu()
        {
            showMenu = false;
            MenuOpen = false;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
        }

        private void OnDisable()
        {
            if (showMenu)
                CloseMenu();
        }

        private void OnGUI()
        {
            if (!showMenu)
                return;

            CreateStyles();
            GUI.color = Color.white;
            windowRect = GUI.Window(9876, windowRect, DrawWindow, GUIContent.none);
        }

        private void CreateStyles()
        {
            if (titleStyle != null)
                return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold
            };

            buttonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }

        private void DrawWindow(int id)
        {
            Color old = GUI.color;

            GUI.color = Bg;
            GUI.DrawTexture(
                new Rect(0, 0, windowRect.width, windowRect.height),
                Texture2D.whiteTexture
            );

            // Внутренние панели придают окну контраст и читаемость.
            GUI.color = Panel;
            GUI.DrawTexture(new Rect(12, 92, 476, 642), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(512, 92, 476, 642), Texture2D.whiteTexture);

            DrawOutline(
                new Rect(2, 2, windowRect.width - 4, windowRect.height - 4),
                Gold,
                2
            );

            DrawLine(0, 82, 1000, 3, Gold);

            titleStyle.normal.textColor = Gold;
            subtitleStyle.normal.textColor = Muted;
            labelStyle.normal.textColor = Text;
            valueStyle.normal.textColor = Gold;
            buttonStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(15, 10, 970, 42), "PIRAMID HELPER", titleStyle);
            GUI.Label(new Rect(150, 49, 700, 24), "—  GAME CONTROL CENTER  —", subtitleStyle);
            GUI.Label(new Rect(815, 50, 165, 20), "LAST UPDATE 2.5", subtitleStyle);

            DrawLine(500, 96, 2, 635, new Color(Gold.r, Gold.g, Gold.b, .75f));

            GUI.Label(new Rect(25, 96, 445, 28), "ИГРОВЫЕ ФУНКЦИИ", labelStyle);

            if (DrawActionButton(
                new Rect(25, 132, 445, 54),
                "ПОЛЁТ",
                Enabled,
                FlightButtonID,
                ref flightAnim,
                Green))
            {
                ToggleFlight();
            }

            if (DrawActionButton(
                new Rect(25, 198, 445, 54),
                "ОТКРЫТЬ ВСЕ ПОСТРОЙКИ",
                UnlockAllBuildings,
                BuildingsButtonID,
                ref buildingsAnim,
                Green))
            {
                UnlockAllBuildings = !UnlockAllBuildings;
                SaveSettings();
            }

            DrawLine(25, 268, 445, 2, Gold);

            GUI.Label(new Rect(25, 280, 445, 28), "НАСТРОЙКИ ПОЛЁТА", labelStyle);

            DrawSlider(
                "ОБЫЧНАЯ СКОРОСТЬ",
                ref normalSpeed,
                1f,
                40f,
                new Rect(25, 316, 445, 52),
                " м/с"
            );

            DrawSlider(
                "УСКОРЕНИЕ SHIFT",
                ref fastSpeed,
                1f,
                80f,
                new Rect(25, 378, 445, 52),
                " м/с"
            );

            DrawSlider(
                "ВЕРТИКАЛЬНАЯ СКОРОСТЬ",
                ref verticalSpeed,
                1f,
                30f,
                new Rect(25, 440, 445, 52),
                " м/с"
            );

            DrawLine(25, 504, 445, 2, Gold);

            GUI.Label(new Rect(25, 516, 445, 28), "ПРОКАЧКА ИГРОКА", labelStyle);

            DrawPlayerStat(
                "СКОРОСТЬ",
                TrainingStat.Speed,
                ref speedXpSlider,
                new Rect(25, 550, 445, 72),
                SpeedXpSliderID
            );

            DrawPlayerStat(
                "СИЛА",
                TrainingStat.Strength,
                ref strengthXpSlider,
                new Rect(25, 628, 445, 72),
                StrengthXpSliderID
            );

            subtitleStyle.normal.textColor = Muted;
            GUI.Label(new Rect(25, 704, 445, 24), statsMessage, subtitleStyle);

            GUI.Label(new Rect(525, 96, 420, 28), "БЕСКОНЕЧНАЯ КАТАПУЛЬТА", labelStyle);

            if (DrawChoiceButton(
                new Rect(945, 96, 35, 28),
                showTrebuchetOptions ? "▲" : "▼",
                false))
            {
                showTrebuchetOptions = !showTrebuchetOptions;
            }

            if (DrawActionButton(
                new Rect(525, 132, 455, 54),
                "БЕСКОНЕЧНЫЕ КИРПИЧИ",
                InfiniteTrebuchet,
                InfiniteButtonID,
                ref infiniteAnim,
                Green))
            {
                InfiniteTrebuchet = !InfiniteTrebuchet;
                SaveSettings();
            }

            if (showTrebuchetOptions)
            {
                DrawIntSlider(
                    "РАЗМЕР ЗАЛПА",
                    ref TrebuchetVolley,
                    12,
                    100,
                    new Rect(525, 196, 455, 54),
                    " кирп."
                );

                DrawSlider(
                    "СКОРОСТЬ ПОЛЁТА КИРПИЧЕЙ",
                    ref brickFlightSpeed,
                    1f,
                    100f,
                    new Rect(525, 258, 455, 54),
                    "×",
                    true
                );

                GUI.Label(new Rect(525, 322, 455, 25), "ТИП КИРПИЧА:", labelStyle);

                string[] names =
                {
                    "Обычные",
                    "Полированные",
                    "Глазурованные",
                    "Золотые",
                    "Золотые с гравировкой 100%"
                };

                for (int i = 0; i < names.Length; i++)
                {
                    if (DrawChoiceButton(
                        new Rect(525, 354 + i * 49, 455, 42),
                        names[i],
                        SelectedTrebuchetBrick == i))
                    {
                        SelectedTrebuchetBrick = i;
                        SaveSettings();
                    }
                }
            }

            if (DrawActionButton(
                new Rect(525, 612, 455, 58),
                "+10 000 МОНЕТ",
                false,
                CoinsButtonID,
                ref coinsAnim,
                new Color(.62f, .32f, .02f),
                false))
            {
                GiveCoins();
            }

            subtitleStyle.normal.textColor = Muted;
            GUI.Label(new Rect(525, 674, 455, 24), coinsMessage, subtitleStyle);

            DrawLine(20, 744, 960, 2, Gold);

            GUI.Label(new Rect(25, 753, 100, 30), "ТЕМА:", labelStyle);

            if (DrawChoiceButton(
                new Rect(130, 750, 170, 38),
                "ТЁМНАЯ",
                theme == 0))
            {
                theme = 0;
                SaveSettings();
            }

            if (DrawChoiceButton(
                new Rect(310, 750, 170, 38),
                "СВЕТЛАЯ",
                theme == 1))
            {
                theme = 1;
                SaveSettings();
            }

            if (DrawActionButton(
                new Rect(525, 750, 455, 42),
                "ЗАКРЫТЬ МЕНЮ",
                false,
                CloseButtonID,
                ref closeAnim,
                new Color(.48f, .13f, .07f),
                false))
            {
                CloseMenu();
            }

            GUI.color = old;
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 82));
        }

        private bool DrawActionButton(
            Rect r,
            string text,
            bool active,
            int hint,
            ref float animTime,
            Color activeColor,
            bool showState = true)
        {
            int cid = GUIUtility.GetControlID(hint, FocusType.Passive);
            Event e = Event.current;

            bool hover = r.Contains(e.mousePosition);
            bool pressed = GUIUtility.hotControl == cid;
            bool clicked = false;

            if (e.type == EventType.MouseDown &&
                e.button == 0 &&
                hover &&
                GUIUtility.hotControl == 0)
            {
                GUIUtility.hotControl = cid;
                e.Use();
            }

            if (e.type == EventType.MouseUp &&
                e.button == 0 &&
                GUIUtility.hotControl == cid)
            {
                if (hover)
                {
                    clicked = true;
                    animTime = Time.unscaledTime;
                }

                GUIUtility.hotControl = 0;
                e.Use();
            }

            float pulse = Mathf.Clamp01(
                1f - (Time.unscaledTime - animTime) * 7f
            );

            Rect d = new Rect(
                r.x,
                r.y + (pressed ? 1f : pulse * .4f),
                r.width,
                r.height - (pressed ? 1f : pulse * .4f)
            );

            Color bg = active
                ? activeColor
                : (theme == 0
                    ? new Color(.17f, .12f, .065f)
                    : new Color(.58f, .52f, .40f));

            if (hover) bg *= 1.15f;
            if (pressed) bg *= .82f;

            GUI.color = bg;
            GUI.DrawTexture(d, Texture2D.whiteTexture);

            DrawOutline(
                d,
                active ? new Color(.43f, .85f, .20f) : Gold,
                2
            );

            GUI.color = Color.white;

            GUI.Label(
                d,
                showState
                    ? text + (active ? "   [ON]" : "   [OFF]")
                    : text,
                buttonStyle
            );

            return clicked;
        }

        private bool DrawChoiceButton(Rect r, string text, bool selected)
        {
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            bool hover = r.Contains(Event.current.mousePosition);

            Color bg = selected
                ? Green
                : (theme == 0
                    ? new Color(.16f, .115f, .06f)
                    : new Color(.78f, .73f, .62f));

            if (hover)
                bg *= 1.12f;

            GUI.color = bg;
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            DrawOutline(r, selected ? new Color(.48f, .95f, .25f) : Gold, 1);

            GUI.color = Color.white;
            buttonStyle.normal.textColor = selected ? Color.white : Text;
            GUI.Label(r, text, buttonStyle);

            return clicked;
        }

        private void DrawSlider(
            string title,
            ref float value,
            float min,
            float max,
            Rect r,
            string suffix,
            bool integer = false)
        {
            labelStyle.normal.textColor = Text;
            GUI.Label(new Rect(r.x, r.y, 300, 22), title, labelStyle);

            valueStyle.normal.textColor = Gold;
            GUI.Label(
                new Rect(r.x + 300, r.y, 145, 22),
                (integer ? value.ToString("0") : value.ToString("0.0")) + suffix,
                valueStyle
            );

            Rect track = new Rect(r.x, r.y + 28, r.width, 18);
            float old = Mathf.InverseLerp(min, max, value);

            float next = GUI.HorizontalSlider(
                track,
                old,
                0f,
                1f,
                GUIStyle.none,
                GUIStyle.none
            );

            if (Mathf.Abs(next - old) > .00001f)
            {
                value = integer
                    ? Mathf.Round(Mathf.Lerp(min, max, next))
                    : Mathf.Lerp(min, max, next);

                value = Mathf.Clamp(value, min, max);

                if (title == "СКОРОСТЬ ПОЛЁТА КИРПИЧЕЙ")
                    TrebuchetSpeedMultiplier = value;

                SaveSettings();
            }

            DrawSliderVisual(track, Mathf.InverseLerp(min, max, value));
        }

        private void DrawIntSlider(
            string title,
            ref int value,
            int min,
            int max,
            Rect r,
            string suffix)
        {
            GUI.Label(new Rect(r.x, r.y, 300, 22), title, labelStyle);
            GUI.Label(new Rect(r.x + 300, r.y, 145, 22), value + suffix, valueStyle);

            Rect track = new Rect(r.x, r.y + 28, r.width, 18);
            float old = Mathf.InverseLerp(min, max, value);

            float next = GUI.HorizontalSlider(
                track,
                old,
                0f,
                1f,
                GUIStyle.none,
                GUIStyle.none
            );

            if (Mathf.Abs(next - old) > .00001f)
            {
                value = Mathf.Clamp(
                    Mathf.RoundToInt(Mathf.Lerp(min, max, next)),
                    min,
                    max
                );

                SaveSettings();
            }

            DrawSliderVisual(track, Mathf.InverseLerp(min, max, value));
        }

        private void DrawSliderVisual(Rect track, float normalized)
        {
            Rect bar = new Rect(track.x, track.y + 6, track.width, 7);

            GUI.color = theme == 0
                ? new Color(.18f, .13f, .06f)
                : new Color(.63f, .58f, .48f);

            GUI.DrawTexture(bar, Texture2D.whiteTexture);

            GUI.color = Gold;
            GUI.DrawTexture(
                new Rect(bar.x, bar.y, bar.width * normalized, bar.height),
                Texture2D.whiteTexture
            );

            GUI.color = new Color(.98f, .78f, .30f);

            float x = bar.x + bar.width * normalized;
            GUI.DrawTexture(
                new Rect(x - 8, bar.y - 5, 16, 17),
                Texture2D.whiteTexture
            );

            GUI.color = Color.white;
        }

        private void DrawPlayerStat(
            string title,
            TrainingStat stat,
            ref int sliderValue,
            Rect r,
            int controlHint)
        {
            PlayerStats stats = PlayerStats.Local;

            if (stats == null || stats.Tuning == null)
            {
                GUI.Label(
                    new Rect(r.x, r.y, r.width, 22),
                    title + " | Игрок не найден",
                    labelStyle
                );
                return;
            }

            int maxXp = Math.Max(
                1,
                stats.Tuning.XpForLevel(stat, stats.Tuning.MaxLevel(stat))
            );

            sliderValue = Mathf.Clamp(sliderValue, 0, maxXp);

            GUI.Label(
    new Rect(r.x, r.y, 300, 22),
    title + " | УРОВЕНЬ: " + stats.Data.Level(stat),
    labelStyle
);

            GUI.Label(
                new Rect(r.x + 210, r.y, 235, 22),
                sliderValue.ToString("N0") + " / " + maxXp.ToString("N0") + " XP",
                valueStyle
            );

            Rect track = new Rect(r.x, r.y + 31, r.width, 18);
            float old = (float)sliderValue / maxXp;

            float next = GUI.HorizontalSlider(
                track,
                old,
                0f,
                1f,
                GUIStyle.none,
                GUIStyle.none
            );

            if (Mathf.Abs(next - old) > .000001f)
            {
                sliderValue = Mathf.Clamp(
                    Mathf.RoundToInt(next * maxXp),
                    0,
                    maxXp
                );

                SetPlayerXp(stat, sliderValue);
            }

            DrawSliderVisual(track, (float)sliderValue / maxXp);
        }

        private void SetPlayerXp(TrainingStat stat, int newValue)
        {
            PlayerStats stats = PlayerStats.Local;

            if (stats == null)
            {
                statsMessage = "Игрок не найден";
                return;
            }

            if (!stats.IsServerInitialized)
            {
                statsMessage = "Нужно быть хостом мира";
                return;
            }

            if (stats.Tuning == null)
            {
                statsMessage = "Настройки прокачки не найдены";
                return;
            }

            int speed = stats.Data.Xp(TrainingStat.Speed);
            int strength = stats.Data.Xp(TrainingStat.Strength);

            if (stat == TrainingStat.Speed)
                speed = newValue;
            else
                strength = newValue;

            speed = Mathf.Clamp(
                speed,
                0,
                stats.Tuning.XpForLevel(
                    TrainingStat.Speed,
                    stats.Tuning.MaxLevel(TrainingStat.Speed)
                )
            );

            strength = Mathf.Clamp(
                strength,
                0,
                stats.Tuning.XpForLevel(
                    TrainingStat.Strength,
                    stats.Tuning.MaxLevel(TrainingStat.Strength)
                )
            );

            stats.RestoreProfile(
                string.Format(
                    "{{\"speedXp\":{0},\"strengthXp\":{1}}}",
                    speed,
                    strength
                )
            );

            Class1.SpeedXp.Value = speed;
            Class1.StrengthXp.Value = strength;
            Class1.SaveConfiguration();

            statsMessage = "XP успешно обновлён";
        }

        private IEnumerator RestoreSavedPlayerStats()
        {
            while (PlayerStats.Local == null || PlayerStats.Local.Tuning == null)
                yield return null;

            PlayerStats stats = PlayerStats.Local;

            if (!stats.IsServerInitialized)
            {
                speedXpSlider = stats.Data.Xp(TrainingStat.Speed);
                strengthXpSlider = stats.Data.Xp(TrainingStat.Strength);
                yield break;
            }

            int speed = stats.Data.Xp(TrainingStat.Speed);
            int strength = stats.Data.Xp(TrainingStat.Strength);

            if (Class1.SpeedXp.Value >= 0)
                speed = Class1.SpeedXp.Value;

            if (Class1.StrengthXp.Value >= 0)
                strength = Class1.StrengthXp.Value;

            speed = Mathf.Clamp(
                speed,
                0,
                stats.Tuning.XpForLevel(
                    TrainingStat.Speed,
                    stats.Tuning.MaxLevel(TrainingStat.Speed)
                )
            );

            strength = Mathf.Clamp(
                strength,
                0,
                stats.Tuning.XpForLevel(
                    TrainingStat.Strength,
                    stats.Tuning.MaxLevel(TrainingStat.Strength)
                )
            );

            if (Class1.SpeedXp.Value >= 0 || Class1.StrengthXp.Value >= 0)
            {
                stats.RestoreProfile(
                    string.Format(
                        "{{\"speedXp\":{0},\"strengthXp\":{1}}}",
                        speed,
                        strength
                    )
                );
            }

            speedXpSlider = speed;
            strengthXpSlider = strength;
        }

        private void GiveCoins()
        {
            if (!InstanceFinder.IsServerStarted)
            {
                coinsMessage = "Начисление доступно только хосту";
                return;
            }

            EndingStats.ServerNoteDevTools("coins");

            var nm = InstanceFinder.NetworkManager;

            if (nm != null &&
                nm.TryGetInstance<TeamWallet>(out var wallet) &&
                wallet.IsServerInitialized)
            {
                long before = wallet.Coins;
                wallet.ServerAdd(10000L);

                coinsMessage = wallet.Coins > before
                    ? "+10 000 монет начислено"
                    : "Игра сейчас не разрешает начисление";
            }
            else
            {
                coinsMessage = "TeamWallet не найден. Перестрой карту";
            }
        }

        private void FindMotor()
        {
            if (motor != null)
                return;

            PlayerCarry player = PlayerCarry.Local;

            if (player != null)
                motor = ((Component)player).GetComponent<PlayerMotor>();
        }

        private void ResetVelocity()
        {
            if (motor == null)
                return;

            string[] names = { "_velocity", "_hvSmoothVel", "_surface" };

            foreach (string name in names)
            {
                FieldInfo f = typeof(PlayerMotor).GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic
                );

                if (f != null)
                    f.SetValue(motor, Activator.CreateInstance(f.FieldType));
            }
        }

        private void DrawLine(float x, float y, float w, float h, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawOutline(Rect r, Color c, float thickness)
        {
            DrawLine(r.x, r.y, r.width, thickness, c);
            DrawLine(r.x, r.yMax - thickness, r.width, thickness, c);
            DrawLine(r.x, r.y, thickness, r.height, c);
            DrawLine(r.xMax - thickness, r.y, thickness, r.height, c);
        }
    }
}
