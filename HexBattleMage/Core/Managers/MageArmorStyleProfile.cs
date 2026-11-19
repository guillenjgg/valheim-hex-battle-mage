using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    [Serializable]
    public sealed class MageArmorStyle
    {
        public string DisplayName;
        public Material Material;
        public Sprite Icon;
    }

    [DataContract]
    public sealed class MageArmorStyleAsset
    {
        [DataMember] public string DisplayName;
        [DataMember] public string MaterialPath;
        [DataMember] public string IconPath;
    }

    [DataContract]
    public sealed class MageArmorStyleData
    {
        [DataMember] public int Version = 1;
        [DataMember] public string RendererName;
        [DataMember] public MageArmorStyleAsset[] Styles;
    }

    public sealed class MageArmorStyleProfile : MonoBehaviour
    {
        public string RendererName = "Cloth";
        [Tooltip("Array order is the saved item variant. Keep existing entries in order.")]
        public MageArmorStyle[] Styles = new MageArmorStyle[0];
        public TextAsset BakedStyles;
        [NonSerialized] private bool _defaultsApplied;

        public static string Encode(MageArmorStyleData data)
        {
            var serializer = new DataContractJsonSerializer(typeof(MageArmorStyleData));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, data);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public void ApplyBakedDefaults(AssetBundle bundle)
        {
            if (_defaultsApplied)
            {
                return;
            }

            if (BakedStyles == null)
            {
                return;
            }

            var serializer = new DataContractJsonSerializer(typeof(MageArmorStyleData));
            MageArmorStyleData data;

            using (var stream = new MemoryStream(BakedStyles.bytes))
            {
                data = (MageArmorStyleData)serializer.ReadObject(stream);
            }

            if (data == null || data.Version != 1 || data.Styles == null || data.Styles.Length == 0)
            {
                throw new InvalidOperationException("Invalid authored armor styles: " + BakedStyles.name);
            }

            var styles = new MageArmorStyle[data.Styles.Length];

            for (int index = 0; index < styles.Length; index++)
            {
                var entry = data.Styles[index];
                var material = bundle.LoadAsset<Material>(entry.MaterialPath);
                var icon = bundle.LoadAsset<Sprite>(entry.IconPath);

                if (material == null || icon == null || string.IsNullOrEmpty(entry.DisplayName))
                {
                    throw new InvalidOperationException("Missing authored armor style asset in " + BakedStyles.name + ", entry " + index);
                }

                styles[index] = new MageArmorStyle { DisplayName = entry.DisplayName, Material = material, Icon = icon };
            }

            RendererName = data.RendererName;
            Styles = styles;
            _defaultsApplied = true;
        }
    }
}
