using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Editor
{
    /// <summary>Button colour families of the pixel UI skin (Art/UI/ui9_btn{tone}_5.png).</summary>
    public enum Tone
    {
        Green,
        Gold,
        Blue,
        Red,
        Purple,
        Gray
    }

    /// <summary>
    /// Pixel UI skin for the scene builders: 9-slice buttons with a pressed sprite (disabled = gray), window frames,
    /// inset slots, tabs, pills, gauges and 16 px icons. All art lives in Art/UI (own work, see ASSET_LICENSES).
    /// </summary>
    public static class UiSkin
    {
        private const string Dir = "Assets/SoloHero/Art/UI/";

        public static Sprite Get(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Dir + file + ".png");

        public static Sprite Icon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "Icons/icon_" + name + ".png");

        public static Sprite ButtonSprite(Tone tone) => Get("ui9_btn" + tone.ToString().ToLowerInvariant() + "_5");

        public static Sprite Frame => Get("ui9_frame_7");
        public static Sprite Slot => Get("ui9_slot_4");
        public static Sprite TopBar => Get("ui9_topbar_4");
        public static Sprite Pill => Get("ui9_pill_5");
        public static Sprite Tab => Get("ui9_tab_5");
        public static Sprite TabActive => Get("ui9_taba_5");
        public static Sprite Gauge => Get("ui9_gauge_3");
        public static Sprite GaugeFill => Get("ui9_gaugefill_2");
        public static Sprite GaugeRed => Get("ui9_gaugered_2");
        public static Sprite Banner => Get("ui9_banner_5");
        public static Sprite White => Get("ui_white");
        public static Sprite Stone => Get("ui_stone");

        // Skin v2 (tools/art/uigen.py): grade frames, cards, badges and portrait props.
        public static Sprite GradeFrame(SoloHero.Core.Gacha.Grade grade) => Get("ui9_grade" + "crel"[(int)grade] + "_3");
        public static Sprite GradeNone => Get("ui9_gradenone_3");
        public static Sprite Selection => Get("ui9_select_6");
        public static Sprite Badge => Get("ui9_badge_3");
        public static Sprite Tag => Get("ui9_tag_3");
        public static Sprite Inset => Get("ui9_inset_4");
        public static Sprite Card => Get("ui9_card_4");
        public static Sprite Chip => Get("ui9_chip_3");
        public static Sprite ChipWhite => Get("ui9_chipw_3");
        public static Sprite GaugeBlue => Get("ui9_gaugeblue_2");
        public static Sprite Glow => Get("ui_glow");
        public static Sprite Pedestal => Get("ui_pedestal");
        public static Sprite Portrait => Get("ui9_portrait_4");
        public static Sprite Plate => Get("ui9_plate_3");

        public static void Sliced(Image image, Sprite sprite)
        {
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
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
                pressedSprite = Get("ui9_btnp" + tone.ToString().ToLowerInvariant() + "_5"),
                disabledSprite = ButtonSprite(Tone.Gray),
            };
        }

        /// <summary>A pixel drop shadow keeps white text readable on busy backgrounds.</summary>
        public static void TextShadow(Text text)
        {
            if (text == null || text.GetComponent<Shadow>() != null) return;
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(0f, -3f);
        }
    }
}
