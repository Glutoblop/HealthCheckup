using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HealthCheckUp
{
    public enum InactiveMode
    {
        /// <summary>Hide the icon when no regen tick would heal you.</summary>
        Hide,
        /// <summary>Keep the icon visible but faded when no regen tick would heal you.</summary>
        Dim,
        /// <summary>Always show the icon at full opacity.</summary>
        Show,
    }

    // Client-only and read-only: the local player's health regen runs entirely on the owning client
    // (Player.FixedUpdate -> UpdateStats -> UpdateFood, gated by m_nview.IsOwner()). We only read
    // Player.m_foodRegenTimer and draw a HUD element. No RPCs, no ZDO writes, no gameplay changes.
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "jordan.valheim.healthcheckup";
        public const string Name = "Health Check Up";
        public const string Version = "0.1.0";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<InactiveMode> WhenInactive;
        internal static ConfigEntry<float> DimAlpha;
        internal static ConfigEntry<bool> ShowSecondsText;
        internal static ConfigEntry<float> OffsetX;
        internal static ConfigEntry<float> OffsetY;
        internal static ConfigEntry<float> Size;
        internal static ConfigEntry<string> IconItem;
        internal static ConfigEntry<Color> RingColor;
        internal static ConfigEntry<Color> RingBackgroundColor;
        internal static ConfigEntry<bool> EnablePreview;
        internal static ConfigEntry<Color> PreviewColor;
        internal static ConfigEntry<bool> PreviewPulse;
        internal static ConfigEntry<bool> ShowAmountText;
        internal static ConfigEntry<float> AmountTextSize;

        private Harmony _harmony;

        private void Awake()
        {
            Enabled = Config.Bind("General", "Enabled", true, "Show the health regen cooldown icon next to the health bar.");

            WhenInactive = Config.Bind("General", "WhenInactive", InactiveMode.Hide,
                "What to do when the next regen tick would not heal you (full health, no food with regen, " +
                "or a status effect reducing health regen to 0). Hide / Dim / Show.");

            DimAlpha = Config.Bind("General", "DimAlpha", 0.3f,
                new ConfigDescription("Opacity used when WhenInactive = Dim.", new AcceptableValueRange<float>(0f, 1f)));

            ShowSecondsText = Config.Bind("General", "ShowSecondsText", false,
                "Show the seconds remaining until the next regen tick under the icon.");

            OffsetX = Config.Bind("Layout", "OffsetX", 8f, "Horizontal gap (HUD pixels) between the right end of the health bar and the icon.");
            OffsetY = Config.Bind("Layout", "OffsetY", 0f, "Vertical offset (HUD pixels) from the health bar's vertical centre.");
            Size = Config.Bind("Layout", "Size", 32f,
                new ConfigDescription("Icon diameter in HUD pixels.", new AcceptableValueRange<float>(8f, 256f)));

            IconItem = Config.Bind("Appearance", "IconItem", "MeadHealthMinor",
                "Prefab name of an item (ObjectDB) whose icon is drawn inside the ring. Empty = no inner icon. " +
                "Falls back to a plain red dot if the item can't be found.");
            RingColor = Config.Bind("Appearance", "RingColor", new Color(0.9f, 0.2f, 0.2f, 1f), "Colour of the radial cooldown sweep.");
            RingBackgroundColor = Config.Bind("Appearance", "RingBackgroundColor", new Color(0f, 0f, 0f, 0.55f), "Colour of the ring track behind the sweep.");

            EnablePreview = Config.Bind("Preview", "EnablePreview", true,
                "Draw a translucent 'ghost' segment on the health bar showing how much the next regen tick will heal.");
            PreviewColor = Config.Bind("Preview", "PreviewColor", new Color(0.75f, 1f, 0.75f, 0.6f),
                "Colour of the ghost segment (alpha = maximum opacity).");
            PreviewPulse = Config.Bind("Preview", "PreviewPulse", true,
                "Fade the ghost in as the tick approaches and pulse it in the last 2 seconds. Off = constant PreviewColor.");
            ShowAmountText = Config.Bind("Preview", "ShowAmountText", true, "Show '+N' above the end of the ghost segment.");
            AmountTextSize = Config.Bind("Preview", "AmountTextSize", 14f,
                new ConfigDescription("Font size of the '+N' text.", new AcceptableValueRange<float>(6f, 48f)));

            // Layout / appearance changes rebuild the widget on the next HUD update.
            OffsetX.SettingChanged += (_, __) => RegenIcon.MarkDirty();
            OffsetY.SettingChanged += (_, __) => RegenIcon.MarkDirty();
            Size.SettingChanged += (_, __) => RegenIcon.MarkDirty();
            IconItem.SettingChanged += (_, __) => RegenIcon.MarkDirty();
            ShowSecondsText.SettingChanged += (_, __) => RegenIcon.MarkDirty();
            RingBackgroundColor.SettingChanged += (_, __) => RegenIcon.MarkDirty();
            ShowAmountText.SettingChanged += (_, __) => RegenPreview.MarkDirty();
            AmountTextSize.SettingChanged += (_, __) => RegenPreview.MarkDirty();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            RegenIcon.Destroy();
        }
    }

    [HarmonyPatch(typeof(Hud), "Update")]
    internal static class HudUpdatePatch
    {
        private static void Postfix(Hud __instance) => RegenIcon.Tick(__instance);
    }
}
