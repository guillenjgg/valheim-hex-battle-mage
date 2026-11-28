using System;
using System.Linq;
using System.Text;
using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HexBattleMage.Core.UI
{
    internal sealed class WandAttunementTab : MonoBehaviour
    {
        private const float Padding = 12f;
        private const float HeaderHeight = 118f;
        private const float FooterHeight = 156f;
        private const float RefreshInterval = 0.5f;
        private const string Orange = "#FFA500";
        private const string Blue = "#B7ED8D";
        private const string Green = "#A8CD79";
        private const string Muted = "#BBBBBB";
        private InventoryGui _gui;
        private Button _tab;
        private Button _action;
        private TMP_Text _actionLabel;
        private TMP_Text _summary;
        private TMP_Text _validation;
        private TMP_Text _actionNote;
        private string _previewInspected;
        private static readonly Color ActiveBorder = new Color(0.57f, 0.83f, 0.36f);
        private static readonly Color SelectedBorder = new Color(1f, 0.75f, 0.32f);
        private GameObject _panel;
        private RectTransform _list;
        private RectTransform _details;
        private WandElement? _inspected;
        private WandElement? _replacement;
        private string _contentKey;
        private float _nextRefresh;
        internal bool IsOpen => _panel != null && _panel.activeSelf;

        internal void Initialize(InventoryGui gui)
        {
            _gui = gui;
            _tab = Instantiate(gui.m_tabUpgrade, gui.m_tabUpgrade.transform.parent);
            _tab.name = "HexMageAttunementTab";
            _tab.onClick = new Button.ButtonClickedEvent();
            _tab.onClick.AddListener(Open);
            var rect = (RectTransform)_tab.transform;
            var upgrade = (RectTransform)gui.m_tabUpgrade.transform;

            rect.anchoredPosition = upgrade.anchoredPosition + Vector2.right * (upgrade.rect.width + 6f) * 2f;
            _tab.GetComponentInChildren<TMP_Text>(true).text = ModLocalization.Format("$hexbattlemage_attune_tab");
            _tab.gameObject.SetActive(false);
            _panel = new GameObject("HexMageAttunementPanel", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            _panel.transform.SetParent(gui.m_crafting, false);
            var panel = (RectTransform)_panel.transform;

            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = new Vector2(Padding, Padding);
            _panel.GetComponent<Image>().color = new Color(0.14f, 0.095f, 0.055f, 1f);
            var summaryBackground = new GameObject("ActiveAttunementFrame", typeof(RectTransform), typeof(Image));

            summaryBackground.transform.SetParent(panel, false);
            var summaryRect = (RectTransform)summaryBackground.transform;

            Place(summaryRect, Padding, Padding, Padding, HeaderHeight - Padding * 2f);
            summaryBackground.GetComponent<Image>().color = new Color(0.11f, 0.17f, 0.10f);
            Border(summaryRect, ActiveBorder, 5f, 0f, "ActiveBorder");
            _summary = Text(summaryRect, "CharacterSummary", 18f);
            _summary.rectTransform.anchorMin = Vector2.zero;
            _summary.rectTransform.anchorMax = Vector2.one;
            _summary.rectTransform.offsetMin = new Vector2(12f, 6f);
            _summary.rectTransform.offsetMax = new Vector2(-12f, -6f);
            _list = ScrollColumn(panel, "Elements", 0f, 0.39f, 0f);
            _details = ScrollColumn(panel, "ElementDetails", 0.39f, 1f, FooterHeight);
            _validation = Text(panel, "ActionValidation", 15f);
            _actionNote = Text(panel, "ActionNote", 14f);
            _actionNote.enableAutoSizing = true;
            _actionNote.fontSizeMin = 12f;
            _actionNote.fontSizeMax = 14f;
            FooterText(_validation.rectTransform, 114f, 36f);
            FooterText(_actionNote.rectTransform, 2f, 58f);
            _action = Instantiate(gui.m_tabCraft, panel);
            _action.name = "AttunementAction";
            _action.onClick = new Button.ButtonClickedEvent();
            _actionLabel = _action.GetComponentInChildren<TMP_Text>(true);
            _actionLabel.enableAutoSizing = true;
            _actionLabel.fontSizeMin = 13f;
            _actionLabel.fontSizeMax = 20f;
            var actionRect = (RectTransform)_action.transform;

            actionRect.anchorMin = new Vector2(0.39f, 0f);
            actionRect.anchorMax = new Vector2(1f, 0f);
            actionRect.pivot = Vector2.zero;
            actionRect.offsetMin = new Vector2(Padding, 64f);
            actionRect.offsetMax = new Vector2(-Padding, 108f);
            StyleButton(_action, new Color(0.32f, 0.24f, 0.13f), new Color(0.23f, 0.14f, 0.12f));
            _action.gameObject.SetActive(true);
            _panel.SetActive(false);
        }

        internal void Close()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
                _tab.interactable = true;
            }
        }

        private void OnDisable()
        {
            Close();
        }

        private void Update()
        {
            var player = Player.m_localPlayer;
            bool available = player != null && RuneBlockManager.IsRuneBlock(player.GetCurrentCraftingStation());

            _tab.gameObject.SetActive(available);

            if (!available)
            {
                Close();
                return;
            }

            if (!IsOpen)
            {
                return;
            }

            _panel.transform.SetAsLastSibling();
            _gui.m_tabCraft.interactable = true;
            _gui.m_tabUpgrade.interactable = true;
            _tab.interactable = false;

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshInterval;
                Refresh(player);
            }
        }

        private void Open()
        {
            var player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            if (!RuneBlockManager.IsRuneBlock(player.GetCurrentCraftingStation()))
            {
                return;
            }

            GetComponent<MageUnlocksTab>()?.Close();
            var corners = new Vector3[4];

            ((RectTransform)_tab.transform).GetWorldCorners(corners);
            float tabBottom = _gui.m_crafting.InverseTransformPoint(corners[0]).y;
            float header = Mathf.Max(0f, _gui.m_crafting.rect.yMax - tabBottom + 6f);

            ((RectTransform)_panel.transform).offsetMax = new Vector2(-Padding, -header);
            _panel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            Refresh(player);
        }

        private void Refresh(Player player)
        {
            var definitions = WandAttunements.Definitions.ToArray();
            var active = WandAttunements.GetActive(player);

            if (!_inspected.HasValue || !definitions.Any(definition => definition.Element == _inspected.Value))
            {
                _inspected = definitions.FirstOrDefault(definition => active.Contains(definition.Element))?.Element ?? definitions.FirstOrDefault()?.Element;
            }

            if (!_replacement.HasValue || !active.Contains(_replacement.Value))
            {
                _replacement = active.Count > 0 ? (WandElement?)active[0] : null;
            }

            var key = new StringBuilder();

            key.Append(player.GetInstanceID()).Append(((RectTransform)_panel.transform).rect.width).Append(_inspected).Append(_replacement).Append(WandAttunements.MaximumActive).Append(player.GetCurrentCraftingStation().GetLevel());
            key.Append(string.Join(",", active)).Append(_previewInspected);
            key.Append(ModLocalization.Format("$hexbattlemage_attunements_title"));

            foreach (var definition in definitions)
            {
                var recipe = definition.GetComponent<MageRecipeProfile>();

                key.Append(WandAttunements.HasLearned(player, definition.Element)).Append(WandAttunements.IsDiscovered(player, recipe)).Append(WandElementText.Damage(definition.Element, 1, true));

                foreach (var resource in recipe.GetResources())
                {
                    var item = ObjectDB.instance.GetItemPrefab(resource.ItemPrefab).GetComponent<ItemDrop>().m_itemData;

                    key.Append(player.GetInventory().CountItems(item.m_shared.m_name));
                }
            }

            foreach (var preview in WandAttunements.Profile.AttunementPreviews)
            {
                key.Append(preview.Id).Append(preview.DisplayName).Append(preview.Description);

                if (Enum.TryParse(preview.Id, out WandElement previewElement))
                {
                    key.Append(WandElementText.Damage(previewElement, 1, true));
                }
            }

            if (_contentKey == key.ToString())
            {
                return;
            }

            _contentKey = key.ToString();
            Clear(_list);
            Clear(_details);
            string activeNames = active.Count == 0 ? ModLocalization.Format("$hexbattlemage_attune_physical") : string.Join(", ", active.Select(ElementName));

            _summary.text = ModLocalization.Format("$hexbattlemage_attunements_summary", active.Count, WandAttunements.MaximumActive, activeNames);
            float top = 8f;

            Label(_list, ModLocalization.Format("$hexbattlemage_attunements_list_heading"), ref top, 18f);

            foreach (var definition in definitions)
            {
                ElementRow(definition, player, active.Contains(definition.Element), ref top);
            }

            var previews = WandAttunements.Profile.AttunementPreviews.Where(preview => !definitions.Any(definition => definition.Element.ToString() == preview.Id)).ToArray();

            foreach (var preview in previews)
            {
                PreviewRow(preview, player, ref top);
            }

            _list.sizeDelta = new Vector2(0f, top + Padding);
            var selected = definitions.FirstOrDefault(definition => definition.Element == _inspected);

            var validationBorder = _action.transform.Find("ValidationBorder");

            if (validationBorder != null)
            {
                validationBorder.gameObject.SetActive(false);
                Destroy(validationBorder.gameObject);
            }

            _action.onClick = new Button.ButtonClickedEvent();
            _action.interactable = false;
            _validation.text = string.Empty;
            _actionNote.text = string.Empty;
            StyleButton(_action, new Color(0.45f, 0.31f, 0.13f), new Color(0.18f, 0.16f, 0.12f));

            var selectedPreview = previews.FirstOrDefault(preview => preview.Id == _previewInspected);

            if (selectedPreview != null)
            {
                float previewTop = 8f;

                Label(_details, "<color=" + Orange + ">" + ModLocalization.Format(selectedPreview.DisplayName) + "</color>", ref previewTop, 25f);
                Label(_details, ModLocalization.Format(selectedPreview.Description), ref previewTop, 19f);

                if (Enum.TryParse(selectedPreview.Id, out WandElement previewElement))
                {
                    Label(_details, ModLocalization.Format("$hexbattlemage_element_preview_values"), ref previewTop, 17f);
                    DamageDetails(previewElement, ref previewTop);
                }
                else
                {
                    Label(_details, ModLocalization.Format("$hexbattlemage_element_damage_unconfigured"), ref previewTop, 18f);
                }
                Label(_details, ModLocalization.Format("$hexbattlemage_attunements_preview_requirements"), ref previewTop, 18f);
                _details.sizeDelta = new Vector2(0f, previewTop + Padding);
                _actionLabel.text = ModLocalization.Format("$hexbattlemage_attunements_preview");
                return;
            }

            if (selected == null)
            {
                _actionLabel.text = ModLocalization.Format("$hexbattlemage_attunements_no_elements");
                return;
            }

            BuildDetails(player, selected, active);
        }

        private void ElementRow(HexBattleMageWandAttunementProfile definition, Player player, bool active, ref float top)
        {
            bool learned = WandAttunements.HasLearned(player, definition.Element);
            var recipe = definition.GetComponent<MageRecipeProfile>();
            bool discovered = WandAttunements.IsDiscovered(player, recipe) && player.GetCurrentCraftingStation().GetLevel() >= recipe.MinimumStationLevel;
            string status = active ? "$hexbattlemage_attunements_active" : learned ? "$hexbattlemage_attunements_learned" : discovered ? "$hexbattlemage_attunements_unlearned" : "$hexbattlemage_attunements_locked";
            string color = active ? Blue : learned ? Green : Muted;
            var button = Instantiate(_gui.m_tabCraft, _list);

            button.name = "Element_" + definition.Element;
            button.interactable = true;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => { _previewInspected = null; _inspected = definition.Element; Refresh(player); });
            var label = button.GetComponentInChildren<TMP_Text>(true);

            label.text = ModLocalization.Format(definition.DisplayName) + "\n<color=" + color + ">" + ModLocalization.Format(status) + "</color>";
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 19f;
            label.textWrappingMode = TextWrappingModes.Normal;
            Position((RectTransform)button.transform, top, 70f);

            bool inspecting = _previewInspected == null && _inspected == definition.Element;

            StyleElementButton(button, active, inspecting);

            button.gameObject.SetActive(true);
            top += 78f;
        }

        private void BuildDetails(Player player, HexBattleMageWandAttunementProfile definition, System.Collections.Generic.List<WandElement> active)
        {
            float top = 8f;
            string name = ModLocalization.Format(definition.DisplayName);
            bool learned = WandAttunements.HasLearned(player, definition.Element);
            bool selected = active.Contains(definition.Element);
            var recipe = definition.GetComponent<MageRecipeProfile>();
            bool requirementsDiscovered = WandAttunements.IsDiscovered(player, recipe);
            bool stationReady = player.GetCurrentCraftingStation().GetLevel() >= recipe.MinimumStationLevel;

            Label(_details, "<color=" + Orange + "><b>" + name + "</b></color>", ref top, 25f);
            string state = selected ? "$hexbattlemage_attunements_active" : learned ? "$hexbattlemage_attunements_learned" : requirementsDiscovered && stationReady ? "$hexbattlemage_attunements_unlearned" : "$hexbattlemage_attunements_locked";

            Label(_details, "<color=" + (selected ? Blue : learned ? Green : Muted) + ">" + ModLocalization.Format(state) + "</color>", ref top, 18f);
            Label(_details, ModLocalization.Format(definition.Description), ref top, 19f);
            DamageDetails(definition.Element, ref top);

            if (!learned)
            {
                Label(_details, ModLocalization.Format("$hexbattlemage_attunements_requirements"), ref top, 19f);
                int currentLevel = player.GetCurrentCraftingStation().GetLevel();
                string levelColor = currentLevel >= recipe.MinimumStationLevel ? Green : Orange;

                Label(_details, "<color=" + levelColor + ">" + ModLocalization.Format("$hexbattlemage_attunements_station_level", recipe.MinimumStationLevel) + "</color>", ref top, 17f);
                int bossIndex = recipe.MinimumStationLevel - WandAttunements.Profile.BaseLevel - 1;
                var bossNames = WandAttunements.Profile.BossDisplayNames;

                if (bossIndex >= 0 && bossIndex < bossNames.Length)
                {
                    Label(_details, ModLocalization.Format("$hexbattlemage_attunements_boss_requirement", ModLocalization.Format(bossNames[bossIndex])), ref top, 17f);
                }

                foreach (var resource in recipe.GetResources())
                {
                    var resourcePrefab = ObjectDB.instance.GetItemPrefab(resource.ItemPrefab);

                    if (resourcePrefab == null)
                    {
                        continue;
                    }

                    var item = resourcePrefab.GetComponent<ItemDrop>().m_itemData;
                    int count = player.GetInventory().CountItems(item.m_shared.m_name);
                    string line = ModLocalization.Format("$hexbattlemage_attune_cost", ModLocalization.Format(item.m_shared.m_name), count, resource.Amount);
                    string color = count >= resource.Amount ? Green : Orange;

                    Label(_details, "<color=" + color + ">" + line + "</color>", ref top, 18f);
                }
            }

            if (learned)
            {
                Label(_details, ModLocalization.Format("$hexbattlemage_attunements_requirements_paid"), ref top, 17f);
            }

            if (!learned)
            {
                bool canLearn = WandAttunements.CanLearn(player, definition);

                _actionLabel.text = canLearn ? ModLocalization.Format("$hexbattlemage_attune_learn_named", name) : ModLocalization.Format("$hexbattlemage_attunements_missing");
                _action.interactable = canLearn;
                _validation.text = ModLocalization.Format(canLearn ? "$hexbattlemage_attunements_validation_ready" : "$hexbattlemage_attunements_validation_missing");
                _validation.color = canLearn ? ActiveBorder : new Color(0.91f, 0.71f, 0.64f);
                _actionNote.text = ModLocalization.Format("$hexbattlemage_attunements_learning_note");
                StyleButton(_action, new Color(0.52f, 0.36f, 0.16f), new Color(0.27f, 0.15f, 0.12f));
                Border((RectTransform)_action.transform, canLearn ? SelectedBorder : new Color(0.66f, 0.40f, 0.31f), 2f, 0f, "ValidationBorder");
                _action.onClick.AddListener(() => { WandAttunements.TryLearn(player, definition); Refresh(player); });
            }
            else if (selected)
            {
                Label(_details, ModLocalization.Format("$hexbattlemage_attunements_already_applied"), ref top, 19f);
                _actionLabel.text = ModLocalization.Format("$hexbattlemage_attunements_already_active");
            }
            else
            {
                bool full = active.Count >= WandAttunements.MaximumActive;

                if (full)
                {
                    Label(_details, ModLocalization.Format("$hexbattlemage_attunements_replace_message", name, ElementName(_replacement.Value)), ref top, 19f);

                    if (active.Count > 1)
                    {
                        foreach (var element in active)
                        {
                            var choice = element;
                            var button = Instantiate(_gui.m_tabCraft, _details);

                            button.onClick = new Button.ButtonClickedEvent();
                            button.onClick.AddListener(() => { _replacement = choice; Refresh(player); });
                            button.GetComponentInChildren<TMP_Text>(true).text = ModLocalization.Format("$hexbattlemage_attunements_replace_choice", ElementName(choice));
                            button.interactable = choice != _replacement;
                            Position((RectTransform)button.transform, top, 36f);
                            button.gameObject.SetActive(true);
                            top += 44f;
                        }
                    }
                }

                _actionNote.text = ModLocalization.Format("$hexbattlemage_attunements_free");
                var replacement = full ? _replacement : null;

                _actionLabel.text = ModLocalization.Format("$hexbattlemage_attunements_activate_action", name);
                _action.interactable = WandAttunements.CanActivate(player, definition, replacement);
                _action.onClick.AddListener(() => { WandAttunements.TryActivate(player, definition, replacement); Refresh(player); });
            }

            _details.sizeDelta = new Vector2(0f, top + Padding);
        }

        private void DamageDetails(WandElement element, ref float top)
        {
            Label(_details, ModLocalization.Format("$hexbattlemage_element_damage_heading"), ref top, 19f);
            Label(_details, WandElementText.Damage(element, 1, true), ref top, 18f);
            Label(_details, ModLocalization.Format("$hexbattlemage_element_attune_damage_basis"), ref top, 15f);
        }

        private void PreviewRow(AttunementPreview preview, Player player, ref float top)
        {
            var button = Instantiate(_gui.m_tabCraft, _list);

            button.name = "Element_" + preview.Id;
            button.interactable = true;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => { _previewInspected = preview.Id; Refresh(player); });
            var label = button.GetComponentInChildren<TMP_Text>(true);

            label.text = ModLocalization.Format(preview.DisplayName) + "\n<color=" + Muted + ">" + ModLocalization.Format("$hexbattlemage_attunements_preview") + "</color>";
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 19f;
            label.textWrappingMode = TextWrappingModes.Normal;
            Position((RectTransform)button.transform, top, 70f);
            StyleElementButton(button, false, _previewInspected == preview.Id);
            button.gameObject.SetActive(true);
            top += 78f;
        }

        private static void StyleButton(Button button, Color normal, Color disabled)
        {
            var animator = button.GetComponent<Animator>();

            if (animator != null)
            {
                animator.enabled = false;
            }

            var image = button.GetComponent<Image>();

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;

            colors.normalColor = normal;
            colors.highlightedColor = Color.Lerp(normal, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(normal, Color.black, 0.2f);
            colors.selectedColor = normal;
            colors.disabledColor = disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            image.canvasRenderer.SetColor(button.interactable ? normal : disabled);
        }

        private static void StyleElementButton(Button button, bool active, bool inspecting)
        {
            var surface = inspecting ? new Color(0.43f, 0.30f, 0.14f) : new Color(0.23f, 0.18f, 0.12f);

            StyleButton(button, surface, surface);
            var rect = (RectTransform)button.transform;

            if (active)
            {
                Border(rect, ActiveBorder, 5f, 0f, "ActiveBorder");
            }

            if (inspecting)
            {
                Border(rect, SelectedBorder, 2f, active ? 6f : 0f, "InspectionBorder");
            }
        }

        private static void Border(RectTransform parent, Color color, float thickness, float inset, string name)
        {
            var frame = new GameObject(name, typeof(RectTransform));

            frame.transform.SetParent(parent, false);
            var rect = (RectTransform)frame.transform;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);

            for (int side = 0; side < 4; side++)
            {
                var edge = new GameObject("Edge", typeof(RectTransform), typeof(Image));

                edge.transform.SetParent(rect, false);
                var edgeRect = (RectTransform)edge.transform;
                var image = edge.GetComponent<Image>();

                image.color = color;
                image.raycastTarget = false;
                edgeRect.anchorMin = side == 1 ? Vector2.right : side == 3 ? Vector2.up : Vector2.zero;
                edgeRect.anchorMax = side == 0 ? Vector2.up : side == 2 ? Vector2.right : Vector2.one;
                edgeRect.offsetMin = side == 1 ? new Vector2(-thickness, 0f) : side == 3 ? new Vector2(0f, -thickness) : Vector2.zero;
                edgeRect.offsetMax = side == 0 ? new Vector2(thickness, 0f) : side == 2 ? new Vector2(0f, thickness) : Vector2.zero;
            }
        }

        private static void FooterText(RectTransform rect, float bottom, float height)
        {
            rect.anchorMin = new Vector2(0.39f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = new Vector2(Padding, bottom);
            rect.offsetMax = new Vector2(-Padding, bottom + height);
        }

        private string ElementName(WandElement element)
        {
            var definition = WandAttunements.Definitions.FirstOrDefault(value => value.Element == element);

            return ModLocalization.Format(definition != null ? definition.DisplayName : "$hexbattlemage_element_" + element.ToString().ToLowerInvariant());
        }

        private RectTransform ScrollColumn(RectTransform parent, string name, float left, float right, float bottom)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));

            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;

            rect.anchorMin = new Vector2(left, 0f);
            rect.anchorMax = new Vector2(right, 1f);
            rect.offsetMin = new Vector2(4f, bottom + 4f);
            rect.offsetMax = new Vector2(-4f, -HeaderHeight);
            root.GetComponent<Image>().color = new Color(0.14f, 0.095f, 0.055f, 1f);
            var content = new GameObject(name + "Content", typeof(RectTransform));

            content.transform.SetParent(root.transform, false);
            var contentRect = (RectTransform)content.transform;

            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(-12f, 0f);
            var scroll = root.GetComponent<ScrollRect>();

            scroll.viewport = rect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 140f;
            var rail = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));

            rail.transform.SetParent(root.transform, false);
            var railRect = (RectTransform)rail.transform;

            railRect.anchorMin = new Vector2(1f, 0f);
            railRect.anchorMax = Vector2.one;
            railRect.offsetMin = new Vector2(-10f, 4f);
            railRect.offsetMax = new Vector2(-2f, -4f);
            rail.GetComponent<Image>().color = new Color(0.28f, 0.2f, 0.1f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));

            handle.transform.SetParent(rail.transform, false);
            var handleRect = (RectTransform)handle.transform;

            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            handle.GetComponent<Image>().color = new Color(0.95f, 0.65f, 0.22f);
            var bar = rail.GetComponent<Scrollbar>();

            bar.handleRect = (RectTransform)handle.transform;
            bar.targetGraphic = handle.GetComponent<Image>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return contentRect;
        }

        private TMP_Text Text(Transform parent, string name, float size)
        {
            var root = new GameObject(name, typeof(RectTransform));

            root.SetActive(false);
            root.transform.SetParent(parent, false);
            var text = root.AddComponent<TextMeshProUGUI>();

            text.font = _gui.m_recipeDecription.font;
            text.fontSize = size;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            root.SetActive(true);
            return text;
        }

        private void Label(RectTransform parent, string value, ref float top, float size)
        {
            var text = Text(parent, "AttunementText", size);
            float height = text.GetPreferredValues(value, parent.rect.width - Padding * 2f, Mathf.Infinity).y + 12f;

            text.text = value;
            Position(text.rectTransform, top, Mathf.Max(height, size + 12f));
            top += Mathf.Max(height, size + 12f) + 6f;
        }

        private static void Position(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(Padding, -top - height);
            rect.offsetMax = new Vector2(-Padding, -top);
        }

        private static void Place(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Clear(RectTransform content)
        {
            foreach (Transform child in content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }
}
