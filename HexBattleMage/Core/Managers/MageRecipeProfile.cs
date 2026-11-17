using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    [Serializable, DataContract]
    public sealed class MageRecipeResource
    {
        [DataMember] public string ItemPrefab;
        [DataMember] public int Amount;
        [DataMember] public int AmountPerQuality;
        [DataMember] public bool Recover = true;
    }

    public sealed class MageRecipeProfile : MonoBehaviour
    {
        [DataContract]
        private sealed class ResourceData
        {
            [DataMember] public int Version;
            [DataMember] public string CraftingStation;
            [DataMember] public string RepairStation;
            [DataMember] public int MinimumStationLevel;
            [DataMember] public int Amount;
            [DataMember] public MageRecipeResource[] Resources;
        }

        public string CraftingStation = "HexArcaneWorkbench";
        public string RepairStation = "HexArcaneWorkbench";
        public int MinimumStationLevel = 1;
        public int Amount = 1;
        public MageRecipeResource[] Resources = new MageRecipeResource[0];
        public TextAsset BakedRecipe;
        [NonSerialized] private bool _defaultsApplied;

        public string GetBakedData()
        {
            var data = new ResourceData { Version = 1, CraftingStation = CraftingStation, RepairStation = RepairStation, MinimumStationLevel = MinimumStationLevel, Amount = Amount, Resources = Resources };
            var serializer = new DataContractJsonSerializer(typeof(ResourceData));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, data);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public void ApplyBakedDefaults()
        {
            if (_defaultsApplied)
            {
                return;
            }

            if (BakedRecipe == null)
            {
                return;
            }

            ResourceData data;
            var serializer = new DataContractJsonSerializer(typeof(ResourceData));
            byte[] bytes = BakedRecipe.bytes;

            try
            {
                using (var stream = new MemoryStream(bytes))
                {
                    data = (ResourceData)serializer.ReadObject(stream);
                }
            }
            catch (SerializationException exception)
            {
                throw new InvalidOperationException($"Cannot decode recipe asset '{BakedRecipe.name}' on '{gameObject.name}' ({bytes.Length} bytes).", exception);
            }

            if (data == null || data.Version != 1 || data.Resources == null)
            {
                throw new InvalidOperationException($"Invalid baked recipe data on '{gameObject.name}', asset '{BakedRecipe.name}', bytes {bytes.Length}, version {data?.Version}, resources {data?.Resources?.Length}.");
            }

            CraftingStation = data.CraftingStation;
            RepairStation = data.RepairStation;
            MinimumStationLevel = data.MinimumStationLevel;
            Amount = data.Amount;
            Resources = data.Resources;
            _defaultsApplied = true;
        }

        public MageRecipeResource[] GetResources()
        {
            ApplyBakedDefaults();

            if (Resources != null && Resources.Length > 0)
            {
                return Resources;
            }

            return new MageRecipeResource[0];
        }
    }
}
