using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BumperRoyale.UI
{
    /// <summary>
    /// A grid of color tiles. Tapping a tile selects it, and the selected tile gets a frame.
    /// Tiles are created at runtime from <see cref="colors"/>; lay them out with a GridLayoutGroup
    /// on the container.
    /// </summary>
    public class ColorPicker : MonoBehaviour
    {
        [SerializeField] private RectTransform tileContainer;
        [SerializeField] private Sprite tileSprite;
        [Tooltip("Width of the frame drawn around the selected tile (px).")]
        [SerializeField] private float frameWidth = 10f;
        [SerializeField] private Color frameColor = Color.white;
        [SerializeField] private Color[] colors =
        {
            new Color(0.86f, 0.13f, 0.13f), // red
            new Color(1f, 0.5f, 0.05f),     // orange
            new Color(1f, 0.9f, 0.05f),     // yellow
            new Color(0.15f, 0.7f, 0.25f),  // green
            new Color(0.05f, 0.7f, 0.7f),   // teal
            new Color(0.15f, 0.4f, 0.95f),  // blue
            new Color(0.55f, 0.25f, 0.85f), // purple
            new Color(1f, 0.4f, 0.7f),      // pink
            new Color(0.95f, 0.95f, 0.95f), // white
            new Color(0.12f, 0.12f, 0.14f), // black
        };

        private readonly List<Image> frames = new List<Image>();

        /// <summary>Raised when the player taps a tile.</summary>
        public event Action<Color> ColorChanged;

        public Color Color { get; private set; }

        private void Awake()
        {
            for (int i = 0; i < colors.Length; i++)
                CreateTile(i);
        }

        /// <summary>Shows a color as selected without raising <see cref="ColorChanged"/>.</summary>
        public void SetColor(Color color)
        {
            Color = color;
            string hex = ColorUtility.ToHtmlStringRGB(color);
            for (int i = 0; i < frames.Count; i++)
            {
                bool selected = ColorUtility.ToHtmlStringRGB(colors[i]) == hex;
                frames[i].color = selected ? frameColor : Color.clear;
            }
        }

        private void CreateTile(int index)
        {
            // The tile's own image is the selection frame; the colored fill is inset inside it.
            var tile = new GameObject("Tile", typeof(RectTransform), typeof(Image), typeof(Button));
            tile.transform.SetParent(tileContainer, false);
            var frame = tile.GetComponent<Image>();
            frame.sprite = tileSprite;
            frame.type = Image.Type.Sliced;
            frame.color = Color.clear;
            frames.Add(frame);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRect = (RectTransform)fillGo.transform;
            fillRect.SetParent(tile.transform, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(frameWidth, frameWidth);
            fillRect.offsetMax = new Vector2(-frameWidth, -frameWidth);
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = tileSprite;
            fill.type = Image.Type.Sliced;
            fill.color = colors[index];

            var button = tile.GetComponent<Button>();
            button.targetGraphic = fill;
            Color color = colors[index];
            button.onClick.AddListener(() => OnTileClicked(color));
        }

        private void OnTileClicked(Color color)
        {
            SetColor(color);
            ColorChanged?.Invoke(color);
        }
    }
}
