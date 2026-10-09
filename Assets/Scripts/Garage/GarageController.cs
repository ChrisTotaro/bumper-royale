using BumperRoyale.Car;
using BumperRoyale.UI;
using UnityEngine;

namespace BumperRoyale.Garage
{
    /// <summary>
    /// Car tab of the main menu: connects the color tiles to the showcased car and saves the
    /// chosen body color for matches.
    /// </summary>
    public class GarageController : MonoBehaviour
    {
        [SerializeField] private CarShowcase showcase;
        [SerializeField] private ColorPicker colorPicker;

        private void Start()
        {
            // The showcase car already wears the saved color.
            colorPicker.SetColor(showcase.Appearance.Color);

            colorPicker.ColorChanged += OnColorChanged;
        }

        private void OnDestroy()
        {
            colorPicker.ColorChanged -= OnColorChanged;
        }

        private void OnColorChanged(Color color)
        {
            showcase.Appearance.SetColor(color);
            CarColorPreference.Set(color);
            CarColorPreference.Flush();
        }
    }
}
