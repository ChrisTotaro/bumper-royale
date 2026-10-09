using UnityEngine;

namespace BumperRoyale.Car
{
    /// <summary>The player's chosen car body color, saved on the device (PlayerPrefs).</summary>
    public static class CarColorPreference
    {
        private const string Key = "BumperRoyale.PlayerCarColor";

        /// <summary>False if the player has never picked a color.</summary>
        public static bool TryLoad(out Color color)
        {
            color = Color.white;
            string hex = PlayerPrefs.GetString(Key, string.Empty);
            return hex.Length > 0 && ColorUtility.TryParseHtmlString("#" + hex, out color);
        }

        /// <summary>Stores the color in memory. Call <see cref="Flush"/> to write it to disk.</summary>
        public static void Set(Color color)
        {
            PlayerPrefs.SetString(Key, ColorUtility.ToHtmlStringRGB(color));
        }

        public static void Flush()
        {
            PlayerPrefs.Save();
        }
    }
}
