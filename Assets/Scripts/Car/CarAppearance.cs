using UnityEngine;

namespace BumperRoyale.Car
{
    /// <summary>Holds the renderers that show the car's team color so spawners can recolor it.</summary>
    public class CarAppearance : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] colorRenderers;

        // Per-car copy of the color material, created the first time SetColor is called.
        private Material customMaterial;

        public Color Color { get; private set; } = Color.white;

        private void Awake()
        {
            if (colorRenderers.Length > 0 && colorRenderers[0] != null)
                Color = GetColor(colorRenderers[0].sharedMaterial);
        }

        private void OnDestroy()
        {
            if (customMaterial != null)
                Destroy(customMaterial);
        }

        public void SetColorMaterial(Material material)
        {
            foreach (Renderer r in colorRenderers)
                r.sharedMaterial = material;
            Color = GetColor(material);
        }

        /// <summary>Recolors the car with any color, without touching the shared material assets.</summary>
        public void SetColor(Color color)
        {
            if (customMaterial == null)
            {
                customMaterial = new Material(colorRenderers[0].sharedMaterial);
                customMaterial.name += " (Custom)";
                foreach (Renderer r in colorRenderers)
                    r.sharedMaterial = customMaterial;
            }
            customMaterial.SetColor(BaseColorId, color);
            Color = color;
        }

        private static Color GetColor(Material material)
        {
            return material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : material.color;
        }
    }
}
