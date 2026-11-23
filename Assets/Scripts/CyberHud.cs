using UnityEngine;
using UnityEngine.UI;

namespace Mindscape
{
    /// <summary>
    /// 赛博终端 HUD 的设计 token 与构件库：单一青色、角括号、发丝线、微型读数条。
    /// 卡片与情绪雷达共用，避免颜色和线宽散落在各处。
    /// </summary>
    public static class CyberHud
    {
        public const float PixelScale = 0.001f;

        public static class Palette
        {
            public static readonly Color Cyan = new Color(0.196f, 0.918f, 1f, 1f);
            public static readonly Color CyanDim = new Color(0.196f, 0.918f, 1f, 0.55f);
            public static readonly Color CyanFaint = new Color(0.196f, 0.918f, 1f, 0.30f);
            public static readonly Color Amber = new Color(1f, 0.690f, 0.125f, 1f);
            public static readonly Color Magenta = new Color(1f, 0.302f, 0.553f, 1f);
            public static readonly Color Panel = new Color(0f, 0f, 0f, 0.32f);
            public static readonly Color TextPrimary = new Color(0.847f, 0.965f, 1f, 1f);
            public static readonly Color TextSecondary = new Color(0.494f, 0.592f, 0.639f, 1f);
            public static readonly Color Neutral = new Color(0.604f, 0.651f, 0.722f, 1f);
        }

        private static Font _mono;
        private static Font _cjk;

        public static Font Mono
        {
            get
            {
                if (_mono == null) _mono = LoadFont("Fonts/RobotoMono");
                return _mono;
            }
        }

        public static Font Cjk
        {
            get
            {
                if (_cjk == null) _cjk = LoadFont("Fonts/SimHei");
                return _cjk;
            }
        }

        private static Font LoadFont(string path)
        {
            var font = Resources.Load<Font>(path);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static GameObject Canvas(string name, float width, float height, int sortingOrder = 10)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = sortingOrder;
            go.transform.localScale = Vector3.one * PixelScale;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            return go;
        }

        public static Text AddText(Transform parent, string content, Vector2 position, Vector2 size, TextAnchor align,
            Font font, int fontSize, Color color, Vector2? anchor = null)
        {
            var go = new GameObject("Text", typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = align;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = content;

            var rect = go.GetComponent<RectTransform>();
            var pivot = anchor ?? new Vector2(0.5f, 0.5f);
            rect.anchorMin = pivot;
            rect.anchorMax = pivot;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return text;
        }

        public static Image AddQuad(Transform parent, Vector2 position, Vector2 size, Color color, Vector2 anchor)
        {
            var go = new GameObject("Line", typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return image;
        }

        /// <summary>四角各两条短线，替代完整边框。</summary>
        public static void AddCornerBrackets(Transform parent, Color color, float length = 18f, float thickness = 1.5f, float inset = 6f)
        {
            var topLeft = new Vector2(0f, 1f);
            var topRight = new Vector2(1f, 1f);
            var bottomLeft = new Vector2(0f, 0f);
            var bottomRight = new Vector2(1f, 0f);

            AddQuad(parent, new Vector2(inset, -inset), new Vector2(length, thickness), color, topLeft);
            AddQuad(parent, new Vector2(inset, -inset), new Vector2(thickness, length), color, topLeft);
            AddQuad(parent, new Vector2(-inset, -inset), new Vector2(length, thickness), color, topRight);
            AddQuad(parent, new Vector2(-inset, -inset), new Vector2(thickness, length), color, topRight);
            AddQuad(parent, new Vector2(inset, inset), new Vector2(length, thickness), color, bottomLeft);
            AddQuad(parent, new Vector2(inset, inset), new Vector2(thickness, length), color, bottomLeft);
            AddQuad(parent, new Vector2(-inset, inset), new Vector2(length, thickness), color, bottomRight);
            AddQuad(parent, new Vector2(-inset, inset), new Vector2(thickness, length), color, bottomRight);
        }

        public static void AddHairLine(Transform parent, float y, float x0, float x1, Color color, float thickness = 1f)
        {
            AddQuad(parent, new Vector2((x0 + x1) * 0.5f, y), new Vector2(x1 - x0, thickness), color, new Vector2(0.5f, 0.5f));
        }

        /// <summary>2px 轨道 + 2px 填充的微型读数条。</summary>
        public static void AddMiniBar(Transform parent, float y, float x0, float x1, float ratio, Color color)
        {
            ratio = Mathf.Clamp01(ratio);
            var track = new Color(color.r, color.g, color.b, 0.30f);
            AddQuad(parent, new Vector2((x0 + x1) * 0.5f, y), new Vector2(x1 - x0, 2f), track, new Vector2(0.5f, 0.5f));
            if (ratio <= 0f) return;
            AddQuad(parent, new Vector2(x0, y), new Vector2((x1 - x0) * ratio, 2f), color, new Vector2(0f, 0.5f));
        }

        public static string AsciiKind(string kind)
        {
            switch (kind)
            {
                case "任务": return "TASK";
                case "提醒": return "REMIND";
                case "人": return "PERSON";
                case "事件": return "EVENT";
                case "感悟": return "INSIGHT";
                default: return "CARD";
            }
        }

        public static string AsciiBucket(string bucket)
        {
            switch (bucket)
            {
                case "overdue": return "OVERDUE";
                case "today": return "TODAY";
                case "soon": return "SOON";
                case "noDue": return "NODUE";
                default: return "OPEN";
            }
        }
    }
}
