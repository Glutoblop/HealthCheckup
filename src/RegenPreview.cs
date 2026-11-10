using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HealthCheckUp
{
    // "Ghost" segment on the health bar showing how much the next regen tick heals.
    //
    // The health bar is a GuiBar pair (Hud.m_healthBarFast / m_healthBarSlow, assembly_guiutils). A GuiBar draws its
    // fill by resizing its m_bar RectTransform: m_bar.SetSizeWithCurrentAnchors(Horizontal, m_width * value), where
    // m_width is the full bar width (Hud.SetHealthBarSize -> GuiBar.SetWidth, follows max health).
    // The ghost is a child of the fast bar's m_bar, anchored to its right edge and stretched to its height, so it
    // automatically sits at the current fill edge (including smoothing). Its width is m_width * heal / maxHealth.
    internal static class RegenPreview
    {
        private const float RegenInterval = 10f; // Player.UpdateFood

        private static Hud _hud;
        private static RectTransform _ghost;
        private static Image _image;
        private static TextMeshProUGUI _text;
        private static bool _dirty;

        internal static void MarkDirty() => _dirty = true;

        private static void Hide()
        {
            if (_ghost != null) _ghost.gameObject.SetActive(false);
        }

        internal static void Destroy()
        {
            if (_ghost != null)
                Object.Destroy(_ghost.gameObject);
            _ghost = null;
            _image = null;
            _text = null;
            _hud = null;
        }

        internal static void Tick(Hud hud)
        {
            Player player = Player.m_localPlayer;
            if (!Plugin.Enabled.Value || !Plugin.EnablePreview.Value || hud == null || player == null || player.IsDead())
            {
                Hide();
                return;
            }

            GuiBar bar = hud.m_healthBarFast;
            float maxHealth = player.GetMaxHealth();
            float healAmount = NextHealAmount(player, maxHealth - player.GetHealth());
            if (bar == null || bar.m_bar == null || maxHealth <= 0f || healAmount <= 0f)
            {
                Hide();
                return;
            }

            if (_ghost == null || _hud != hud || _dirty)
            {
                Destroy();
                Build(hud, bar);
                _dirty = false;
            }

            float progress = Mathf.Clamp01(player.m_foodRegenTimer / RegenInterval);
            _ghost.gameObject.SetActive(true);
            _ghost.sizeDelta = new Vector2(bar.m_width * healAmount / maxHealth, 0f);

            // Ramp in as the tick approaches; optionally pulse in the last couple of seconds.
            Color c = Plugin.PreviewColor.Value;
            float alpha = c.a;
            if (Plugin.PreviewPulse.Value)
            {
                alpha *= Mathf.Lerp(0.15f, 1f, progress);
                if (progress > 0.8f)
                    alpha *= 0.7f + 0.3f * Mathf.Sin(Time.time * 10f);
            }
            _image.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));

            if (_text != null)
            {
                _text.text = "+" + Mathf.CeilToInt(healAmount);
                _text.alpha = Mathf.Lerp(0.5f, 1f, progress);
            }
        }

        // Match Player.UpdateFood's food regen and status-effect multiplier, capped at missing health.
        private static float NextHealAmount(Player player, float missing)
        {
            if (missing <= 0.01f)
                return 0f;

            float regen = 0f;
            foreach (Player.Food food in player.GetFoods())
                regen += food.m_item.m_shared.m_foodRegen;
            if (regen <= 0f)
                return 0f;

            float mult = 1f;
            player.m_seman.ModifyHealthRegen(ref mult);
            regen *= mult;
            return regen > 0f ? Mathf.Min(regen, missing) : 0f;
        }

        private static void Build(Hud hud, GuiBar bar)
        {
            _hud = hud;
            RectTransform parent = bar.m_bar;

            var go = new GameObject("HealthCheckUp_RegenGhost", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.layer = parent.gameObject.layer;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            _ghost = (RectTransform)go.transform;
            _ghost.SetParent(parent, false);
            _ghost.SetAsLastSibling();
            _ghost.anchorMin = new Vector2(1f, 0f);
            _ghost.anchorMax = new Vector2(1f, 1f);
            _ghost.pivot = new Vector2(0f, 0.5f);
            _ghost.anchoredPosition = Vector2.zero;
            _ghost.sizeDelta = Vector2.zero;

            _image = go.GetComponent<Image>();
            _image.raycastTarget = false;
            // Reuse the health fill's sprite so the ghost has the same shape/texture; tinted by PreviewColor.
            Image barImage = parent.GetComponent<Image>();
            if (barImage != null && barImage.sprite != null)
            {
                _image.sprite = barImage.sprite;
                _image.type = barImage.type == Image.Type.Sliced || barImage.type == Image.Type.Tiled ? barImage.type : Image.Type.Simple;
            }

            _text = null;
            if (Plugin.ShowAmountText.Value)
            {
                var tgo = new GameObject("Amount", typeof(RectTransform));
                tgo.layer = go.layer;
                var trt = (RectTransform)tgo.transform;
                trt.SetParent(_ghost, false);
                // Just above the right end of the ghost.
                trt.anchorMin = trt.anchorMax = new Vector2(1f, 1f);
                trt.pivot = new Vector2(0.5f, 0f);
                trt.sizeDelta = new Vector2(60f, 20f);
                trt.anchoredPosition = new Vector2(0f, 2f);
                _text = tgo.AddComponent<TextMeshProUGUI>();
                if (hud.m_healthText != null)
                {
                    _text.font = hud.m_healthText.font;
                    _text.fontSharedMaterial = hud.m_healthText.fontSharedMaterial;
                }
                _text.fontSize = Plugin.AmountTextSize.Value;
                _text.alignment = TextAlignmentOptions.Bottom;
                _text.textWrappingMode = TextWrappingModes.NoWrap;
                _text.raycastTarget = false;
                _text.color = new Color(0.75f, 1f, 0.75f, 1f);
            }
        }
    }
}
