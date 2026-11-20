using HexBattleMage.Assets;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core
{
    internal sealed class RuneBlockNotifications : MonoBehaviour
    {
        private float _nextRefresh;

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            var player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            if (ZNet.instance == null)
            {
                return;
            }

            if (ZoneSystem.instance == null)
            {
                return;
            }

            if (MessageHud.instance == null)
            {
                return;
            }

            var prefab = AssetBundleHelper.Bundle.LoadAsset<GameObject>(RuneBlockManager.PrefabName);
            var profile = PrefabSettings.Require<HexBattleMageRuneBlockProfile>(prefab);
            _nextRefresh = Time.unscaledTime + profile.RefreshInterval;
            int level = RuneBlockProgression.GetLevel(profile);

            if (RuneBlockProgression.RecordLevel(player, ZNet.instance.GetWorldUID(), level, profile.BaseLevel))
            {
                player.Message(MessageHud.MessageType.Center, ModLocalization.Format(profile.UpgradeMessage, level));
            }
        }
    }
}
