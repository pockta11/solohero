using System.Collections.Generic;
using System.IO;
using SoloHero.Game.UI.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Editor
{
    /// <summary>Button colour families of the UI skin.</summary>
    public enum Tone
    {
        Green,
        Gold,
        Blue,
        Red,
        Purple,
        Gray,
        Orange
    }

    /// <summary>
    /// UI skin for the scene builders. D-108: the smooth casual skin in Art/UI/Hd (tools/art/uigen3.py) - candy
    /// buttons with a pressed sprite (disabled = gray), cream panels and cards, dark HUD pills and gauges, grade slots.
    /// Sprites are drawn at 1 px = 1 canvas unit. The pixel 16 px icons in Art/UI/Icons are unchanged.
    /// </summary>
    public static class UiSkin
    {
        private const string Dir = "Assets/SoloHero/Art/UI/";
        private const string HdDir = "Assets/SoloHero/Art/UI/Hd/";
        private static Dictionary<string, Sprite> _hd;

        public static Sprite Get(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Dir + file + ".png");

        /// <summary>
        /// A UI icon by name: the D-108 smooth icon (Art/UI/Hd/Icons/hdicon_{name}.png, tools/art/icongen3.py) when one
        /// exists, else the 16 px pixel icon (Art/UI/Icons/icon_{name}.png).
        /// </summary>
        public static Sprite Icon(string name)
        {
            Sprite smooth = AssetDatabase.LoadAssetAtPath<Sprite>(HdDir + "Icons/hdicon_" + name + ".png");
            return smooth != null ? smooth : PixelIcon(name);
        }

        /// <summary>The 16 px pixel icon (world-space sprites and anything that must stay on the pixel grid).</summary>
        public static Sprite PixelIcon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "Icons/icon_" + name + ".png");

        /// <summary>A D-108 sprite by name without the border suffix: "hd9_btngreen" finds hd9_btngreen_30.png.</summary>
        public static Sprite Hd(string name)
        {
            if (_hd == null)
            {
                _hd = new Dictionary<string, Sprite>();
                foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { HdDir.TrimEnd('/') }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string file = Path.GetFileNameWithoutExtension(path);
                    string key = file.StartsWith("hd9_") ? file.Substring(0, file.LastIndexOf('_')) : file;
                    _hd[key] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }

            if (_hd.TryGetValue(name, out Sprite sprite) && sprite != null) return sprite;
            Debug.LogWarning("[UI] missing skin sprite " + name + " (run tools/art/uigen3.py)");
            return null;
        }

        /// <summary>Forgets the sprite cache (after a reimport of the skin folder).</summary>
        public static void Reload() => _hd = null;

        private static string ToneName(Tone tone) => tone.ToString().ToLowerInvariant();

        public static Sprite ButtonSprite(Tone tone) => Hd("hd9_btn" + ToneName(tone));
        public static Sprite PressedSprite(Tone tone) => Hd("hd9_btnp" + ToneName(tone));
        public static Sprite PlateSprite(Tone tone) => Hd("hd9_plate" + ToneName(tone));
        public static Sprite RoundSprite(Tone tone) => Hd("hd_round" + ToneName(tone));

        /// <summary>Cream growth panel (bottom tabs).</summary>
        public static Sprite Panel => Hd("hd9_panel");
        /// <summary>Popup window: wood-gold frame around a cream body.</summary>
        public static Sprite Window => Hd("hd9_window");
        public static Sprite Card => Hd("hd9_card");
        public static Sprite CardLilac => Hd("hd9_cardlilac");
        public static Sprite Inset => Hd("hd9_inset");
        public static Sprite InsetDark => Hd("hd9_insetdark");
        public static Sprite Ribbon => Hd("hd9_ribbon");
        public static Sprite HudBar => Hd("hd9_hudbar");
        public static Sprite Pill => Hd("hd9_pill");
        public static Sprite Gauge => Hd("hd9_gauge");
        /// <summary>Gauge well for the cream surfaces (tan, recessed).</summary>
        public static Sprite GaugeLight => Hd("hd9_gaugelight");
        public static Sprite FillRed => Hd("hd9_fillred");
        public static Sprite FillBlue => Hd("hd9_fillblue");
        public static Sprite FillGold => Hd("hd9_fillgold");
        public static Sprite FillGreen => Hd("hd9_fillgreen");
        public static Sprite FillPurple => Hd("hd9_fillpurple");
        public static Sprite TabBar => Hd("hd9_tabbar");
        public static Sprite TabActive => Hd("hd9_tabactive");
        public static Sprite AvatarRing => Hd("hd_avatar_ring");
        public static Sprite AvatarDisc => Hd("hd_avatar_disc");
        public static Sprite LevelBadge => Hd("hd9_lvbadge");
        public static Sprite Dot => Hd("hd_dot");
        public static Sprite RailTile => Hd("hd9_railtile");
        public static Sprite Caption => Hd("hd9_caption");
        public static Sprite SlotEmpty => Hd("hd9_slotempty");
        public static Sprite SlotDark => Hd("hd9_slotdark");
        public static Sprite Selection => Hd("hd9_select");
        public static Sprite Veil => Hd("hd9_veil");

        /// <summary>D-113: one frame per gear grade, c u r e l m a (common .. ancient); skills use the frame of the same name.</summary>
        public static Sprite GradeFrame(SoloHero.Core.Gacha.GearGrade grade) => Hd("hd9_slot" + "curelma"[(int)grade]);

        public static Sprite GradeFrame(SoloHero.Core.Gacha.Grade grade) => GradeFrame(SoloHero.Core.Gacha.GearGrades.ToGearGrade(grade));

        // Kept from the pixel skin: plain white, radial glow and the summon / card art.
        public static Sprite White => Get("ui_white");
        public static Sprite Glow => Get("ui_glow");
        public static Sprite Pedestal => Get("ui_pedestal");

        public static void Sliced(Image image, Sprite sprite, float pixelsPerUnit = 1f)
        {
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = pixelsPerUnit;
            image.color = Color.white;
        }

        /// <summary>Normal sprite, pushed sprite on press, gray while not interactable (unaffordable / busy).</summary>
        public static void Button(Button button, Tone tone)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            Sliced(image, ButtonSprite(tone));
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                pressedSprite = PressedSprite(tone),
                disabledSprite = ButtonSprite(Tone.Gray),
            };
            if (button.GetComponent<PressScale>() == null) button.gameObject.AddComponent<PressScale>();
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null) ButtonText(label, tone);
        }

        /// <summary>Outline colour that sits well on each button tone (a dark shade of the same hue).</summary>
        public static Color ToneInk(Tone tone)
        {
            switch (tone)
            {
                case Tone.Green: return new Color32(0x17, 0x5A, 0x26, 0xFF);
                case Tone.Gold: return new Color32(0x7A, 0x40, 0x06, 0xFF);
                case Tone.Blue: return new Color32(0x14, 0x3E, 0x86, 0xFF);
                case Tone.Red: return new Color32(0x74, 0x1A, 0x26, 0xFF);
                case Tone.Purple: return new Color32(0x43, 0x22, 0x86, 0xFF);
                case Tone.Orange: return new Color32(0x84, 0x34, 0x0C, 0xFF);
                default: return new Color32(0x4A, 0x45, 0x5E, 0xFF);
            }
        }

        /// <summary>White button label with a toned outline and a soft drop shadow.</summary>
        public static void ButtonText(Text text, Tone tone)
        {
            if (text == null) return;
            text.color = Color.white;
            Outlined(text, ToneInk(tone), 3f, 0.55f);
        }

        /// <summary>
        /// Lettering on dark surfaces (HUD, battle field, dim overlays): a dark plum outline plus a drop shadow, so
        /// the rounded font reads on busy backgrounds.
        /// </summary>
        public static void TextShadow(Text text)
        {
            if (text == null) return;
            Outlined(text, new Color(0.13f, 0.09f, 0.2f, 1f), 3f, 0.7f);
        }

        /// <summary>D-108 text on the cream surfaces: plain coloured lettering without outline or shadow.</summary>
        public static void Ink(Text text, Color color)
        {
            if (text == null) return;
            foreach (Shadow s in text.GetComponents<Shadow>()) Object.DestroyImmediate(s);
            text.color = color;
        }

        private static void Outlined(Text text, Color ink, float width, float shadowAlpha)
        {
            Outline outline = text.GetComponent<Outline>();
            if (outline == null) outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = ink;
            outline.effectDistance = new Vector2(width, -width);
            Shadow shadow = null;
            foreach (Shadow s in text.GetComponents<Shadow>())
                if (!(s is Outline)) shadow = s;
            if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(ink.r * 0.6f, ink.g * 0.6f, ink.b * 0.6f, shadowAlpha);
            shadow.effectDistance = new Vector2(0f, -width - 2f);
        }
    }
}
