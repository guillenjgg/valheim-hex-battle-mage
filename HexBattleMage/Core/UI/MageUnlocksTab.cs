using HexBattleMage.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HexBattleMage.Core.UI
{
    internal sealed class MageUnlocksTab : MonoBehaviour
    {
        private const float TabSpacing = 6f;
        private const float PanelPadding = 14f;
        private const float RefreshInterval = 0.5f;
        private InventoryGui _gui;
        private Button _tab;
        private TMP_Text _tabLabel;
        private GameObject _panel;
        private TextMeshProUGUI _text;
        private ScrollRect _scroll;
        private float _nextRefresh;

        internal bool IsOpen => _panel != null && _panel.activeSelf;

        internal void Initialize(InventoryGui gui)
        {
            _gui = gui;
            _tab = Instantiate(gui.m_tabUpgrade, gui.m_tabUpgrade.transform.parent);
            _tab.name = "HexMageUnlocksTab";
            _tab.onClick = new Button.ButtonClickedEvent();
            _tab.onClick.AddListener(Open);

            var tabRect = (RectTransform)_tab.transform;
            var upgradeRect = (RectTransform)gui.m_tabUpgrade.transform;

            tabRect.anchoredPosition = upgradeRect.anchoredPosition + Vector2.right * (upgradeRect.rect.width + TabSpacing);
            _tabLabel = _tab.GetComponentInChildren<TMP_Text>(true);
            _tab.gameObject.SetActive(false);
            CreatePanel();
        }

        internal void Close()
        {
            if (_panel == null)
            {
                return;
            }

            _panel.SetActive(false);
            if (_tab != null)
            {
                _tab.interactable = true;
            }
        }

        private void Update()
        {
            if (_tab == null)
            {
                return;
            }

            var player = Player.m_localPlayer;
            var station = player != null ? player.GetCurrentCraftingStation() : null;
            bool atMageTable = HexBattleMage.Core.Managers.CraftingStationManager.IsMageTable(station);

            _tab.gameObject.SetActive(atMageTable);

            if (!atMageTable)
            {
                Close();
                return;
            }

            _tabLabel.text = ModLocalization.Format("$hexbattlemage_unlocks_tab");

            if (!IsOpen)
            {
                return;
            }

            _gui.m_tabCraft.interactable = true;
            _gui.m_tabUpgrade.interactable = true;
            _tab.interactable = false;
            _panel.transform.SetAsLastSibling();

            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + RefreshInterval;
            RefreshText(player);
        }

        private void OnDisable()
        {
            Close();
        }

        private void Open()
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            var corners = new Vector3[4];

            ((RectTransform)_tab.transform).GetWorldCorners(corners);

            float tabBottom = _gui.m_crafting.InverseTransformPoint(corners[0]).y;
            float headerHeight = Mathf.Max(0f, _gui.m_crafting.rect.yMax - tabBottom + TabSpacing);
            var panelRect = (RectTransform)_panel.transform;

            panelRect.offsetMax = new Vector2(-PanelPadding, -headerHeight);
            _panel.SetActive(true);
            RefreshText(Player.m_localPlayer);
            _scroll.verticalNormalizedPosition = 1f;
        }

        private void CreatePanel()
        {
            _panel = new GameObject("HexMageUnlocksPanel", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            _panel.transform.SetParent(_gui.m_crafting, false);

            var rect = (RectTransform)_panel.transform;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(PanelPadding, PanelPadding);
            rect.offsetMax = new Vector2(-PanelPadding, -100f);
            _panel.GetComponent<Image>().color = new Color(0.12f, 0.09f, 0.12f, 1f);
            _panel.GetComponent<Mask>().showMaskGraphic = true;

            var textObject = new GameObject("UnlocksText", typeof(RectTransform));

            textObject.SetActive(false);
            textObject.transform.SetParent(_panel.transform, false);
            _text = textObject.AddComponent<TextMeshProUGUI>();
            _text.font = _gui.m_recipeDecription.font;
            _text.fontSize = 18f;
            _text.color = new Color(0.93f, 0.88f, 0.8f);
            _text.richText = true;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.raycastTarget = false;
            _text.margin = new Vector4(PanelPadding, PanelPadding, PanelPadding, PanelPadding);

            var textRect = (RectTransform)textObject.transform;

            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;

            _scroll = _panel.GetComponent<ScrollRect>();
            _scroll.viewport = rect;
            _scroll.content = textRect;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 25f;
            _panel.SetActive(false);
            textObject.SetActive(true);
        }

        private void RefreshText(Player player)
        {
            string content = MageEquipmentCatalog.BuildText(player);

            if (_text.text == content)
            {
                return;
            }

            _text.text = content;

            float width = ((RectTransform)_panel.transform).rect.width - PanelPadding * 2f;
            float height = _text.GetPreferredValues(content, width, Mathf.Infinity).y + PanelPadding * 2f;

            _text.rectTransform.sizeDelta = new Vector2(0f, height);
        }
    }
}
