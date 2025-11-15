using UnityEngine;
using UnityEngine.UIElements;

namespace MortierFu.Shared {
    public static class VisualElementExtensions {
        /// <summary>
        /// Adds a header label to the VisualElement with optional styling and an optional separator below.
        /// </summary>
        /// <param name="element">Parent VisualElement.</param>
        /// <param name="text">Header text.</param>
        /// <param name="fontSize">Font size (default 14).</param>
        /// <param name="bold">Whether the text is bold (default true).</param>
        /// <param name="color">Text color (default black).</param>
        /// <param name="marginTop">Top margin (default 10).</param>
        /// <param name="marginBottom">Bottom margin (default 2).</param>
        /// <param name="addSeparator">Whether to add a separator below the header (default true).</param>
        /// <param name="separatorHeight">Height of the separator (default 1).</param>
        /// <param name="separatorColor">Color of the separator (default gray).</param>
        public static void AddHeader(
            this VisualElement element,
            string text,
            int fontSize = 14,
            bool bold = true,
            Color? color = null,
            float marginTop = 10f,
            float marginBottom = 2f,
            bool addSeparator = true,
            float separatorHeight = 1f,
            Color? separatorColor = null
        )
        {
            // Create header label
            var header = new Label(text);
            header.style.fontSize = fontSize;
            header.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            if (color != null) {
                header.style.color = new StyleColor(color.Value);
            }
            header.style.marginTop = marginTop;
            header.style.marginBottom = marginBottom;

            element.Add(header);

            // Optionally add separator below
            if (addSeparator)
            {
                element.AddSeparator(separatorHeight, separatorColor ?? Color.gray, 0f, 5f);
            }
        }
        
        /// <summary>
        /// Adds a horizontal separator to the VisualElement.
        /// </summary>
        /// <param name="element">Parent VisualElement.</param>
        /// <param name="height">Height of the separator line (default 1).</param>
        /// <param name="color">Color of the separator (default gray).</param>
        /// <param name="marginTop">Optional top margin (default 0).</param>
        /// <param name="marginBottom">Optional bottom margin (default 5).</param>
        public static void AddSeparator(
            this VisualElement element,
            float height = 1f,
            Color? color = null,
            float marginTop = 0f,
            float marginBottom = 5f
        )
        {
            var separator = new VisualElement();
            separator.style.height = height;
            separator.style.backgroundColor = new StyleColor(color ?? Color.gray);
            separator.style.marginTop = marginTop;
            separator.style.marginBottom = marginBottom;

            element.Add(separator);
        }
        
        public static void SetMargin(this VisualElement element, float value)
        {
            element.style.marginTop = value;
            element.style.marginBottom = value;
            element.style.marginLeft = value;
            element.style.marginRight = value;
        }

        public static void SetPadding(this VisualElement element, float value) {
            element.style.paddingTop = value;
            element.style.paddingBottom = value;
            element.style.paddingLeft = value;
            element.style.paddingRight = value;
        }
    }
}