using UnityEngine;

namespace DivisorGame.UI
{
    /// <summary>화면 색상과 크기 상수. 초등학생 대상이라 대비가 크고 밝은 색을 쓴다.</summary>
    public static class UITheme
    {
        public static readonly Color Background = Hex("EAF2FF");
        public static readonly Color Panel = Hex("FFFFFF");
        public static readonly Color PanelSoft = Hex("F4F8FF");
        public static readonly Color TextDark = Hex("223055");
        public static readonly Color TextMuted = Hex("6B7A9E");
        public static readonly Color TextOnAccent = Hex("FFFFFF");

        public static readonly Color TargetBorder = Hex("FF8A3D");
        public static readonly Color TargetFill = Hex("FFFFFF");
        public static readonly Color TargetClearedFill = Hex("D8F5D0");
        public static readonly Color TargetClearedBorder = Hex("4CAF50");

        public static readonly Color HandBorder = Hex("3D7BFF");
        public static readonly Color HandFill = Hex("FFFFFF");
        public static readonly Color DragGhostBorder = Hex("FFB300");
        public static readonly Color DragGhostFill = Hex("FFF3CC");

        public static readonly Color ButtonPrimary = Hex("3D7BFF");
        public static readonly Color ButtonDanger = Hex("F2545B");
        public static readonly Color ButtonNeutral = Hex("8894B0");
        public static readonly Color ButtonDisabled = Hex("C7CEDC");

        public static readonly Color Positive = Hex("1B8A3A");
        public static readonly Color Negative = Hex("D32F2F");
        public static readonly Color Dim = new Color(0.05f, 0.08f, 0.16f, 0.66f);

        public const int ReferenceWidth = 1920;
        public const int ReferenceHeight = 1080;

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }
    }
}
