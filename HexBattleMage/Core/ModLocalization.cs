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
        internal const string BeechWandName = "$item_hexbattlemage_wand_meadows_01";
        internal const string BeechWandDescription = "$item_hexbattlemage_wand_meadows_01_description";
        private const string TranslationResourcePrefix = "HexBattleMage.Assets.Translations.";
        private const string TranslationResourceExtension = ".json";
        private static CustomLocalization _localization;

        internal static void Initialize()
        {
            LoadTranslations(LocalizationManager.Instance.GetLocalization());
        }

        internal static void LoadTranslations(CustomLocalization localization)
        {
            _localization = localization;

            var assembly = Assembly.GetExecutingAssembly();

            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.StartsWith(TranslationResourcePrefix, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (!resourceName.EndsWith(TranslationResourceExtension, System.StringComparison.Ordinal))
                {
                    continue;
                }

                string language = resourceName.Substring(TranslationResourcePrefix.Length, resourceName.Length - TranslationResourcePrefix.Length - TranslationResourceExtension.Length);

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    _localization.AddJsonFile(language, reader.ReadToEnd());
                }
            }
        }

        internal static string Format(string token, params object[] arguments)
        {
            string translation = _localization.TryTranslate(token);

            return string.Format(CultureInfo.CurrentCulture, translation, arguments);
        }
    }
}
