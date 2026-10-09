using UnityEngine;

namespace BumperRoyale.Car
{
    /// <summary>Holds the renderers that show the car's team color so spawners can recolor it.</summary>
    public class CarAppearance : MonoBehaviour
    {
        [SerializeField] private Renderer[] colorRenderers;

        public Color Color { get; private set; } = Color.white;

        public void SetColorMaterial(Material material)
        {
            foreach (Renderer r in colorRenderers)
                r.sharedMaterial = material;
            Color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
        }
    }
}
