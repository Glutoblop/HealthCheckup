using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HealthCheckUp
{
    // Client-only and read-only: the local player's health regen runs entirely on the owning client
    // (Player.FixedUpdate -> UpdateStats -> UpdateFood, gated by m_nview.IsOwner()).
    // We read the local player's regen state and draw a HUD element.
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "jordan.valheim.healthcheckup";
        public const string Name = "Health Check Up";
        public const string Version = "0.1.3";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> EnablePreview;
        internal static ConfigEntry<Color> PreviewColor;
        internal static ConfigEntry<bool> PreviewPulse;
        internal static ConfigEntry<bool> ShowAmountText;
        internal static ConfigEntry<float> AmountTextSize;

        private Harmony _harmony;

        private void Awake()
        {
            Enabled = Config.Bind("General", "Enabled", true, "Enable Health Check Up.");

            EnablePreview = Config.Bind("Preview", "EnablePreview", true,
                "Draw a translucent 'ghost' segment on the health bar showing how much the next regen tick will heal.");
            PreviewColor = Config.Bind("Preview", "PreviewColor", new Color(0.75f, 1f, 0.75f, 0.6f),
                "Colour of the ghost segment (alpha = maximum opacity).");
            PreviewPulse = Config.Bind("Preview", "PreviewPulse", true,
                "Fade the ghost in as the tick approaches and pulse it in the last 2 seconds. Off = constant PreviewColor.");
            ShowAmountText = Config.Bind("Preview", "ShowAmountText", true, "Show '+N' above the end of the ghost segment.");
            AmountTextSize = Config.Bind("Preview", "AmountTextSize", 14f,
                new ConfigDescription("Font size of the '+N' text.", new AcceptableValueRange<float>(6f, 48f)));

            // Text layout changes rebuild the preview on the next HUD update.
            ShowAmountText.SettingChanged += (_, __) => RegenPreview.MarkDirty();
            AmountTextSize.SettingChanged += (_, __) => RegenPreview.MarkDirty();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            RegenPreview.Destroy();
        }
    }

    [HarmonyPatch(typeof(Hud), "Update")]
    internal static class HudUpdatePatch
    {
        private static void Postfix(Hud __instance) => RegenPreview.Tick(__instance);
    }
}
