using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private const float ScrollbarWidth = 18f;
        private const float ScrollbarSpacing = 8f;
        private const float WheelScrollDistance = 140f;
        private const float ToolbarHeight = 48f;
        private InventoryGui _gui;
        private Button _tab;
        private TMP_Text _tabLabel;
        private GameObject _panel;
        private RectTransform _content;
        private readonly HashSet<Heightmap.Biome> _collapsedBiomes = new HashSet<Heightmap.Biome>();
        private string _contentKey;
        private Heightmap.Biome? _focusedBiome;
        private ScrollRect _scroll;
        private Button _collapseAllButton;
        private TMP_Text _collapseAllLabel;
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
            _panel.GetComponent<Image>().color = new Color(0.14f, 0.095f, 0.055f, 0.96f);
            _panel.GetComponent<Mask>().showMaskGraphic = true;

            var viewport = new GameObject("UnlocksViewport", typeof(RectTransform), typeof(RectMask2D));

            viewport.transform.SetParent(_panel.transform, false);
            var viewportRect = (RectTransform)viewport.transform;

            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-ScrollbarWidth - ScrollbarSpacing, -ToolbarHeight);

            var content = new GameObject("UnlocksContent", typeof(RectTransform));

            content.transform.SetParent(viewport.transform, false);
            _content = (RectTransform)content.transform;
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = Vector2.one;
            _content.pivot = new Vector2(0.5f, 1f);
            _scroll = _panel.GetComponent<ScrollRect>();
            _scroll.viewport = viewportRect;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = WheelScrollDistance;
            _scroll.verticalScrollbar = CreateScrollbar();
            _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            CreateCollapseAllButton();
            _panel.SetActive(false);
        }

        private Scrollbar CreateScrollbar()
        {
            var rail = new GameObject("UnlocksScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));

            rail.transform.SetParent(_panel.transform, false);
            var railRect = (RectTransform)rail.transform;

            railRect.anchorMin = new Vector2(1f, 0f);
            railRect.anchorMax = Vector2.one;
            railRect.offsetMin = new Vector2(-ScrollbarWidth - 4f, 8f);
            railRect.offsetMax = new Vector2(-4f, -ToolbarHeight - 8f);
            rail.GetComponent<Image>().color = new Color(0.25f, 0.16f, 0.07f);

            var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));

            slidingArea.transform.SetParent(rail.transform, false);
            Stretch((RectTransform)slidingArea.transform, 2f, 2f, 2f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));

            handle.transform.SetParent(slidingArea.transform, false);
            var handleRect = (RectTransform)handle.transform;

            Stretch(handleRect, 0f, 0f, 0f);
            var scrollbar = rail.GetComponent<Scrollbar>();

            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var colors = scrollbar.colors;

            colors.normalColor = new Color(0.95f, 0.65f, 0.22f);
            colors.highlightedColor = new Color(1f, 0.82f, 0.4f);
            colors.pressedColor = new Color(1f, 0.9f, 0.55f);
            colors.selectedColor = colors.highlightedColor;
            scrollbar.colors = colors;
            return scrollbar;
        }

        private void CreateCollapseAllButton()
        {
            _collapseAllButton = Instantiate(_gui.m_tabCraft, _panel.transform);
            _collapseAllButton.name = "UnlocksCollapseAll";
            _collapseAllButton.onClick = new Button.ButtonClickedEvent();
            _collapseAllButton.onClick.AddListener(ToggleAllBiomes);
            _collapseAllButton.gameObject.SetActive(true);
            var rect = (RectTransform)_collapseAllButton.transform;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(PanelPadding, -8f);
            rect.sizeDelta = new Vector2(160f, 32f);
            _collapseAllLabel = _collapseAllButton.GetComponentInChildren<TMP_Text>(true);
            _collapseAllLabel.font = _gui.m_recipeDecription.font;
            _collapseAllLabel.fontSize = 18f;
            _collapseAllLabel.color = new Color(1f, 0.647f, 0f);
            _collapseAllLabel.alignment = TextAlignmentOptions.Center;
            Stretch(_collapseAllLabel.rectTransform, 6f, 6f, 0f);
            _collapseAllLabel.text = ModLocalization.Format("$hexbattlemage_unlocks_collapse_all");
        }

        private void ToggleAllBiomes()
        {
            var player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            var biomes = MageEquipmentCatalog.GetEntries().Where(entry => MageEquipmentCatalog.IsVisible(entry, player)).Select(entry => entry.Biome).Distinct().ToArray();
            bool expand = biomes.All(biome => _collapsedBiomes.Contains(biome));

            foreach (var biome in biomes)
            {
                if (expand)
                {
                    _collapsedBiomes.Remove(biome);
                }
                else
                {
                    _collapsedBiomes.Add(biome);
                }
            }

            _focusedBiome = null;
            RefreshText(player);
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1f;
        }

        private void RefreshText(Player player)
        {
            var entries = MageEquipmentCatalog.GetEntries().Where(entry => MageEquipmentCatalog.IsVisible(entry, player)).ToArray();
            _collapseAllButton.interactable = entries.Length > 0;
            bool allCollapsed = entries.Length > 0 && entries.All(entry => _collapsedBiomes.Contains(entry.Biome));

            _collapseAllLabel.text = ModLocalization.Format(allCollapsed ? "$hexbattlemage_unlocks_expand_all" : "$hexbattlemage_unlocks_collapse_all");
            float width = _scroll.viewport.rect.width - PanelPadding * 2f;
            var key = new StringBuilder(width.ToString("0"));

            key.Append(ModLocalization.Format("$hexbattlemage_unlocks_intro"));

            foreach (var entry in entries)
            {
                key.Append(entry.Item.name).Append(ModLocalization.Format(entry.Item.m_itemData.m_shared.m_name));
                key.Append(MageEquipmentCatalog.IsUnlocked(entry, player)).Append(MageEquipmentCatalog.BuildDetails(entry, player));
                key.Append(_collapsedBiomes.Contains(entry.Biome));
            }

            string contentKey = key.ToString();

            if (_contentKey == contentKey)
            {
                return;
            }

            _contentKey = contentKey;
            float scrollOffset = _content.anchoredPosition.y;

            for (int index = _content.childCount - 1; index >= 0; index--)
            {
                var child = _content.GetChild(index).gameObject;

                child.SetActive(false);

                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            float top = PanelPadding;
            var intro = CreateLabel("Introduction", _content, ModLocalization.Format(entries.Length == 0 ? "$hexbattlemage_unlocks_empty" : "$hexbattlemage_unlocks_intro"), 17f);
            float introHeight = intro.GetPreferredValues(intro.text, width, Mathf.Infinity).y;

            Position(intro.rectTransform, top, introHeight);
            top += introHeight + 16f;

            foreach (var group in entries.GroupBy(entry => entry.Biome))
            {
                top = CreateBiomeHeader(group.Key, group.Count(), top);

                if (_collapsedBiomes.Contains(group.Key))
                {
                    continue;
                }

                foreach (var entry in group)
                {
                    top = CreateEntry(entry, player, top, width);
                }
            }

            _content.sizeDelta = new Vector2(0f, top + PanelPadding);
            float maximumOffset = Mathf.Max(0f, _content.rect.height - _scroll.viewport.rect.height);

            _content.anchoredPosition = new Vector2(0f, Mathf.Clamp(scrollOffset, 0f, maximumOffset));
        }

        private float CreateEntry(MageEquipmentCatalog.Entry entry, Player player, float top, float width)
        {
            const float NameLeft = 54f;
            const float StatusWidth = 110f;
            const float MinimumRowHeight = 64f;
            bool unlocked = MageEquipmentCatalog.IsUnlocked(entry, player);
            var row = new GameObject("Gear_" + entry.Item.name, typeof(RectTransform), typeof(Image));

            row.transform.SetParent(_content, false);
            row.GetComponent<Image>().color = new Color(0.23f, 0.18f, 0.15f, 1f);
            row.GetComponent<Image>().raycastTarget = false;
            var name = CreateLabel("Name", row.transform, ModLocalization.Format(entry.Item.m_itemData.m_shared.m_name), 18f);
            float height = Mathf.Max(MinimumRowHeight, name.GetPreferredValues(name.text, Mathf.Max(40f, width - NameLeft - StatusWidth), Mathf.Infinity).y + 18f);

            Position((RectTransform)row.transform, top, height);
            Stretch(name.rectTransform, NameLeft, StatusWidth, 8f);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            var status = CreateLabel("State", row.transform, ModLocalization.Format(unlocked ? "$hexbattlemage_unlocks_unlocked" : "$hexbattlemage_unlocks_locked"), 14f);

            status.color = unlocked ? new Color(0.64f, 0.84f, 0.52f) : new Color(0.93f, 0.60f, 0.46f);
            status.alignment = TextAlignmentOptions.MidlineRight;
            status.rectTransform.anchorMin = new Vector2(1f, 0f);
            status.rectTransform.anchorMax = Vector2.one;
            status.rectTransform.offsetMin = new Vector2(-StatusWidth, 8f);
            status.rectTransform.offsetMax = new Vector2(-10f, -8f);
            var marker = new GameObject("StateBar", typeof(RectTransform), typeof(Image));

            marker.transform.SetParent(row.transform, false);
            marker.GetComponent<Image>().color = status.color;
            marker.GetComponent<Image>().raycastTarget = false;
            var markerRect = (RectTransform)marker.transform;

            markerRect.anchorMin = Vector2.zero;
            markerRect.anchorMax = new Vector2(0f, 1f);
            markerRect.offsetMin = Vector2.zero;
            markerRect.offsetMax = new Vector2(4f, 0f);
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));

            icon.transform.SetParent(row.transform, false);
            var image = icon.GetComponent<Image>();

            image.sprite = entry.Item.m_itemData.GetIcon();
            image.preserveAspect = true;
            image.raycastTarget = false;
            var iconRect = (RectTransform)icon.transform;

            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(34f, 40f);
            top += height + 6f;

            if (!unlocked)
            {
                var details = CreateLabel("Requirements", _content, MageEquipmentCatalog.BuildDetails(entry, player), 17f);
                float detailHeight = details.GetPreferredValues(details.text, width - 16f, Mathf.Infinity).y;

                Position(details.rectTransform, top + 4f, detailHeight);
                details.rectTransform.offsetMin += Vector2.right * 8f;
                top += detailHeight + 18f;
            }

            return top;
        }

        private float CreateBiomeHeader(Heightmap.Biome biome, int count, float top)
        {
            const float HeaderHeight = 42f;
            var root = new GameObject("Biome_" + biome, typeof(RectTransform), typeof(Image), typeof(Button));

            root.transform.SetParent(_content, false);
            root.GetComponent<Image>().color = new Color(0.28f, 0.18f, 0.09f, 1f);
            var button = root.GetComponent<Button>();

            button.onClick.AddListener(() => ToggleBiome(biome));

            if (_focusedBiome == biome)
            {
                button.Select();
            }

            string name = ModLocalization.Format("$biome_" + biome.ToString().ToLowerInvariant());
            string disclosure = _collapsedBiomes.Contains(biome) ? "+ " : "- ";
            var label = CreateLabel("BiomeName", root.transform, disclosure + ModLocalization.Format("$hexbattlemage_unlocks_biome", name, count), 20f);

            label.color = new Color(1f, 0.647f, 0f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(label.rectTransform, 12f, 12f, 4f);
            Position((RectTransform)root.transform, top, HeaderHeight);
            return top + HeaderHeight + 8f;
        }

        private void ToggleBiome(Heightmap.Biome biome)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            _focusedBiome = biome;

            if (!_collapsedBiomes.Add(biome))
            {
                _collapsedBiomes.Remove(biome);
            }

            RefreshText(Player.m_localPlayer);
        }

        private TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float size)
        {
            var child = new GameObject(name, typeof(RectTransform));

            child.transform.SetParent(parent, false);
            child.SetActive(false);
            var label = child.AddComponent<TextMeshProUGUI>();

            label.font = _gui.m_recipeDecription.font;
            label.fontSize = size;
            label.color = Color.white;
            label.richText = true;
            label.text = text;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.alignment = TextAlignmentOptions.TopLeft;
            child.SetActive(true);
            return label;
        }

        private static void Position(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(PanelPadding, -top - height);
            rect.offsetMax = new Vector2(-PanelPadding, -top);
        }

        private static void Stretch(RectTransform rect, float left, float right, float vertical)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, vertical);
            rect.offsetMax = new Vector2(-right, -vertical);
        }
    }
}