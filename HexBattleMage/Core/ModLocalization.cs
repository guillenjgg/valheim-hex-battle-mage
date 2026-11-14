using Jotunn.Entities;
using Jotunn.Managers;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace HexBattleMage.Core
{
    internal static class ModLocalization
    {
        internal const string MageTableName = "$piece_hexbattlemage_magetable";
        internal const string MageTableDescription = "$piece_hexbattlemage_magetable_description";
        internal const string BeechWandName = "$item_hexbattlemage_beechwand";
        internal const string BeechWandDescription = "$item_hexbattlemage_beechwand_description";
        private const string EnglishResourceName = "HexBattleMage.Assets.Translations.English.json";
        private static CustomLocalization _localization;

        internal static void Initialize()
        {
            _localization = LocalizationManager.Instance.GetLocalization();

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EnglishResourceName))
            using (var reader = new StreamReader(stream))
            {
                _localization.AddJsonFile("English", reader.ReadToEnd());
            }
        }

        internal static string Format(string token, params object[] arguments)
        {
            string translation = _localization.TryTranslate(token);

            return string.Format(CultureInfo.CurrentCulture, translation, arguments);
        }
    }
}
