using System;
using UnityEngine;
using UnityEngine.UI;

namespace BumperRoyale.UI
{
    /// <summary>
    /// HSV color picker: a saturation/brightness square, a vertical hue bar and a row of preset
    /// swatches. The square and bar textures are generated at runtime.
    /// </summary>
    public class ColorPicker : MonoBehaviour
    {
        [Header("Saturation / brightness square")]
        [SerializeField] private RawImage svImage;
        [SerializeField] private ColorPickerArea svArea;
        [SerializeField] private RectTransform svHandle;
        [Tooltip("Optional image inside the handle that shows the current color.")]
        [SerializeField] private Image svHandleFill;
        [SerializeField] private int svTextureSize = 64;

        [Header("Hue bar")]
        [SerializeField] private RawImage hueImage;
        [SerializeField] private ColorPickerArea hueArea;
        [SerializeField] private RectTransform hueHandle;
        [SerializeField] private int hueTextureSize = 128;

        [Header("Presets")]
        [SerializeField] private RectTransform swatchContainer;
        [SerializeField] private Sprite swatchSprite;
        [SerializeField] private float swatchSize = 64f;
        [SerializeField] private Color[] presetColors =
        {
            new Color(1f, 0.9f, 0.05f), new Color(1f, 0.5f, 0.05f), new Color(0.9f, 0.15f, 0.15f),
            new Color(0.95f, 0.3f, 0.7f), new Color(0.55f, 0.25f, 0.9f), new Color(0.15f, 0.45f, 0.95f),
            new Color(0.1f, 0.8f, 0.35f), new Color(0.95f, 0.95f, 0.95f),
        };

        private Texture2D svTexture;
        private Texture2D hueTexture;
        private float hue;
        private float saturation;
        private float value;

        /// <summary>Raised whenever the player changes the color.</summary>
        public event Action<Color> ColorChanged;
        /// <summary>Raised when the player lets go of the square or bar, or taps a swatch.</summary>
        public event Action EditFinished;

        public Color Color => Color.HSVToRGB(hue, saturation, value);

        private void Awake()
        {
            hueTexture = new Texture2D(1, hueTextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                name = "HueBar",
            };
            for (int y = 0; y < hueTextureSize; y++)
                hueTexture.SetPixel(0, y, Color.HSVToRGB((float)y / (hueTextureSize - 1), 1f, 1f));
            hueTexture.Apply();
            hueImage.texture = hueTexture;

            svTexture = new Texture2D(svTextureSize, svTextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                name = "SaturationValue",
            };
            svImage.texture = svTexture;

            svArea.Changed += OnSvChanged;
            hueArea.Changed += OnHueChanged;
            svArea.Released += OnReleased;
            hueArea.Released += OnReleased;

            CreateSwatches();
            Refresh(true);
        }

        private void OnDestroy()
        {
            svArea.Changed -= OnSvChanged;
            hueArea.Changed -= OnHueChanged;
            svArea.Released -= OnReleased;
            hueArea.Released -= OnReleased;
            Destroy(svTexture);
            Destroy(hueTexture);
        }

        /// <summary>Shows a color without raising <see cref="ColorChanged"/>.</summary>
        public void SetColor(Color color)
        {
            Color.RGBToHSV(color, out hue, out saturation, out value);
            Refresh(true);
        }

        private void OnSvChanged(Vector2 position)
        {
            saturation = position.x;
            value = position.y;
            Refresh(false);
            ColorChanged?.Invoke(Color);
        }

        private void OnHueChanged(Vector2 position)
        {
            hue = position.y;
            Refresh(true);
            ColorChanged?.Invoke(Color);
        }

        private void OnReleased() => EditFinished?.Invoke();

        private void OnSwatchClicked(Color color)
        {
            SetColor(color);
            ColorChanged?.Invoke(Color);
            EditFinished?.Invoke();
        }

        private void Refresh(bool hueChanged)
        {
            if (svTexture == null)
                return;
            if (hueChanged)
                FillSvTexture();

            svHandle.anchorMin = svHandle.anchorMax = new Vector2(saturation, value);
            svHandle.anchoredPosition = Vector2.zero;
            hueHandle.anchorMin = hueHandle.anchorMax = new Vector2(0.5f, hue);
            hueHandle.anchoredPosition = Vector2.zero;
            if (svHandleFill != null)
                svHandleFill.color = Color;
        }

        private void FillSvTexture()
        {
            int size = svTextureSize;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Color.HSVToRGB(hue, (float)x / (size - 1), v);
            }
            svTexture.SetPixels32(pixels);
            svTexture.Apply();
        }

        private void CreateSwatches()
        {
            if (swatchContainer == null)
                return;
            foreach (Color preset in presetColors)
            {
                var go = new GameObject("Swatch", typeof(RectTransform), typeof(Image), typeof(Button));
                var rect = (RectTransform)go.transform;
                rect.SetParent(swatchContainer, false);
                rect.sizeDelta = new Vector2(swatchSize, swatchSize);

                var image = go.GetComponent<Image>();
                image.sprite = swatchSprite;
                image.type = Image.Type.Sliced;
                image.color = preset;

                var button = go.GetComponent<Button>();
                button.targetGraphic = image;
                Color captured = preset;
                button.onClick.AddListener(() => OnSwatchClicked(captured));
            }
        }
    }
}
