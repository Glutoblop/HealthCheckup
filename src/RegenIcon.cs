using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HealthCheckUp
{
    // Vanilla health regen (assembly_valheim, Player.UpdateFood, called from Player.FixedUpdate -> UpdateStats
    // on the owning client only):
    //     m_foodRegenTimer += dt;
    //     if (m_foodRegenTimer >= 10f) {
    //         m_foodRegenTimer = 0f;
    //         regen = sum(food.m_item.m_shared.m_foodRegen);
    //         if (regen > 0) { m_seman.ModifyHealthRegen(ref mult); Heal(regen * mult); }
    //     }
    // So it's a fixed 10 s tick, never reset by damage. The timer keeps running at full health; it pauses while
    // dead, in the intro, or teleporting (UpdateStats isn't called). We read the real timer each frame.
    internal static class RegenIcon
    {
        private const float RegenInterval = 10f; // hard-coded in Player.UpdateFood

        private static Hud _hud;
        private static RectTransform _root;
        private static CanvasGroup _group;
        private static Image _fill;
        private static Image _icon;
        private static TextMeshProUGUI _text;
        private static bool _dirty;
        private static bool _iconResolved;

        private static Sprite _ringSprite;
        private static Sprite _discSprite;

        internal static void MarkDirty() => _dirty = true;

        internal static void Destroy()
        {
            if (_root != null)
                Object.Destroy(_root.gameObject);
            _root = null;
            _hud = null;
            RegenPreview.Destroy();
        }

        internal static void Tick(Hud hud)
        {
            if (!Plugin.Enabled.Value)
            {
                if (_root != null) _root.gameObject.SetActive(false);
                RegenPreview.Hide();
                return;
            }

            if (hud == null || hud.m_healthBarRoot == null)
                return;

            if (_root == null || _hud != hud || _dirty)
            {
                Destroy();
                Build(hud);
                _dirty = false;
            }

            if (!_iconResolved)
                ResolveIcon();

            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                _root.gameObject.SetActive(false);
                RegenPreview.Hide();
                return;
            }

            float healAmount = NextHealAmount(player);
            bool wouldHeal = healAmount > 0f;
            RegenPreview.Tick(hud, player, healAmount, Mathf.Clamp01(player.m_foodRegenTimer / RegenInterval));

            InactiveMode mode = Plugin.WhenInactive.Value;
            if (!wouldHeal && mode == InactiveMode.Hide)
            {
                _root.gameObject.SetActive(false);
                return;
            }

            _root.gameObject.SetActive(true);
            _group.alpha = wouldHeal || mode == InactiveMode.Show ? 1f : Plugin.DimAlpha.Value;

            Position(hud);

            float timer = Mathf.Clamp(player.m_foodRegenTimer, 0f, RegenInterval);
            _fill.fillAmount = timer / RegenInterval;
            _fill.color = Plugin.RingColor.Value;

            if (_text != null)
                _text.text = Mathf.CeilToInt(RegenInterval - timer).ToString() + "s";
        }

        // Same maths as Player.UpdateFood: sum(m_foodRegen) * SEMan.ModifyHealthRegen multiplier, then clamped like
        // Character.RPC_Heal (min(health + hp, maxHealth)). ModifyHealthRegen is side-effect free: the only override
        // is SE_Stats.ModifyHealthRegen, which just adjusts the ref multiplier. Returns 0 when no heal would happen.
        internal static float NextHealAmount(Player player)
        {
            float missing = player.GetMaxHealth() - player.GetHealth();
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

        // Pin the icon's left-centre to the right-centre of the health bar. The bar width follows max health
        // (Hud.SetHealthBarSize), so recompute from world corners every frame instead of relying on anchors.
        private static readonly Vector3[] Corners = new Vector3[4];

        private static void Position(Hud hud)
        {
            hud.m_healthBarRoot.GetWorldCorners(Corners); // 0 BL, 1 TL, 2 TR, 3 BR
            Vector3 rightMid = (Corners[2] + Corners[3]) * 0.5f;
            var parent = (RectTransform)_root.parent;
            Vector3 local = parent.InverseTransformPoint(rightMid);
            _root.localPosition = new Vector3(local.x + Plugin.OffsetX.Value, local.y + Plugin.OffsetY.Value, 0f);
        }

        private static void Build(Hud hud)
        {
            _hud = hud;
            _iconResolved = false;
            EnsureSprites();

            Transform parent = hud.m_healthPanel != null ? hud.m_healthPanel : hud.m_healthBarRoot.parent;
            float size = Plugin.Size.Value;

            var go = new GameObject("HealthCheckUp_RegenIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
            go.layer = parent.gameObject.layer;
            _root = (RectTransform)go.transform;
            _root.SetParent(parent, false);
            _root.SetAsLastSibling();
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0f, 0.5f);
            _root.sizeDelta = new Vector2(size, size);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            _group = go.GetComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;

            // Ring track
            Image bg = NewImage("Track", _root, _ringSprite, Vector2.zero, Vector2.one);
            bg.color = Plugin.RingBackgroundColor.Value;

            // Radial sweep (fills clockwise from 12 o'clock as the 10 s tick approaches)
            _fill = NewImage("Sweep", _root, _ringSprite, Vector2.zero, Vector2.one);
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Radial360;
            _fill.fillOrigin = (int)Image.Origin360.Top;
            _fill.fillClockwise = true;
            _fill.fillAmount = 0f;

            // Inner icon
            _icon = NewImage("Icon", _root, _discSprite, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            _icon.color = new Color(0.85f, 0.15f, 0.15f, 1f);
            _icon.preserveAspect = true;
            _icon.enabled = !string.IsNullOrEmpty(Plugin.IconItem.Value);

            _text = null;
            if (Plugin.ShowSecondsText.Value)
            {
                var tgo = new GameObject("Seconds", typeof(RectTransform));
                tgo.layer = go.layer;
                var trt = (RectTransform)tgo.transform;
                trt.SetParent(_root, false);
                trt.anchorMin = new Vector2(0f, 0f);
                trt.anchorMax = new Vector2(1f, 0f);
                trt.pivot = new Vector2(0.5f, 1f);
                trt.sizeDelta = new Vector2(size, size * 0.5f);
                trt.anchoredPosition = Vector2.zero;
                _text = tgo.AddComponent<TextMeshProUGUI>();
                if (hud.m_healthText != null)
                {
                    _text.font = hud.m_healthText.font;
                    _text.fontSharedMaterial = hud.m_healthText.fontSharedMaterial;
                }
                _text.fontSize = Mathf.Max(10f, size * 0.4f);
                _text.alignment = TextAlignmentOptions.Top;
                _text.raycastTarget = false;
                _text.textWrappingMode = TextWrappingModes.NoWrap;
                _text.color = Color.white;
            }
        }

        private static void ResolveIcon()
        {
            string name = Plugin.IconItem.Value;
            if (string.IsNullOrEmpty(name))
            {
                _iconResolved = true;
                return;
            }
            if (ObjectDB.instance == null)
                return; // try again next frame

            _iconResolved = true;
            GameObject prefab = ObjectDB.instance.GetItemPrefab(name);
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            Sprite sprite = drop != null ? drop.m_itemData.GetIcon() : null;
            if (sprite != null && _icon != null)
            {
                _icon.sprite = sprite;
                _icon.color = Color.white;
            }
        }

        private static Image NewImage(string name, RectTransform parent, Sprite sprite, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        // Procedural sprites so we don't depend on any particular game/Unity built-in asset being present.
        private static void EnsureSprites()
        {
            if (_ringSprite == null) _ringSprite = MakeCircleSprite("HealthCheckUp_Ring", 0.70f);
            if (_discSprite == null) _discSprite = MakeCircleSprite("HealthCheckUp_Disc", 0f);
        }

        private static Sprite MakeCircleSprite(string name, float innerRadius)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            float aa = 1.5f / c; // ~1.5 px antialias band
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01((1f - r) / aa);
                    if (innerRadius > 0f)
                        a = Mathf.Min(a, Mathf.Clamp01((r - innerRadius) / aa));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            Object.DontDestroyOnLoad(tex);
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            Object.DontDestroyOnLoad(sprite);
            return sprite;
        }
    }
}
