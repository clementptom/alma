using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>
    /// Inventory screen: abilities (locked ones show as ???), key items, stats, and a details
    /// panel for whatever is selected. Opens with the Inventory button or from the pause menu.
    /// </summary>
    public class InventoryUI : OverlayPanel
    {
        RectTransform abilityRow;
        RectTransform itemGrid;
        Text noItemsText;
        Text statsText;
        Image detailIcon;
        Text detailName;
        Text detailBody;
        Text footer;
        readonly List<GameObject> slots = new List<GameObject>();

        protected override void OnBuild()
        {
            var skin = UIBuilder.Skin;
            var dim = UIBuilder.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.7f));
            UIBuilder.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var box = UIBuilder.CreatePanel(transform, "Box").rectTransform;
            UIBuilder.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1640f, 880f));

            var title = UIBuilder.CreateText(box, "Title", "Inventory", 56, TextAnchor.MiddleCenter, skin.accentColor);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(800f, 70f));

            var abilitiesLabel = UIBuilder.CreateText(box, "AbilitiesLabel", "Abilities", 34, TextAnchor.MiddleLeft, skin.accentColor);
            UIBuilder.Place(abilitiesLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -110f), new Vector2(600f, 44f));
            abilityRow = UIBuilder.CreateRect("Abilities", box);
            UIBuilder.Place(abilityRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -160f), new Vector2(1000f, 200f));
            var row = abilityRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 20f;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.UpperLeft;

            var itemsLabel = UIBuilder.CreateText(box, "ItemsLabel", "Items", 34, TextAnchor.MiddleLeft, skin.accentColor);
            UIBuilder.Place(itemsLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -390f), new Vector2(600f, 44f));
            itemGrid = UIBuilder.CreateRect("Items", box);
            UIBuilder.Place(itemGrid, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -440f), new Vector2(1000f, 340f));
            var grid = itemGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150f, 170f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            noItemsText = UIBuilder.CreateText(box, "NoItems", "Nothing yet.", 28, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.5f));
            UIBuilder.Place(noItemsText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -440f), new Vector2(600f, 40f));

            var details = UIBuilder.CreateImage(box, "Details", new Color(0f, 0f, 0f, 0.35f)).rectTransform;
            UIBuilder.Place(details, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -110f), new Vector2(500f, 640f));
            detailIcon = UIBuilder.CreateImage(details, "Icon", Color.white);
            detailIcon.preserveAspect = true;
            UIBuilder.Place(detailIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(220f, 220f));
            detailName = UIBuilder.CreateText(details, "Name", "", 40, TextAnchor.MiddleCenter, skin.accentColor);
            UIBuilder.Place(detailName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(460f, 60f));
            detailBody = UIBuilder.CreateText(details, "Description", "", 28, TextAnchor.UpperCenter);
            UIBuilder.Place(detailBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(440f, 280f));

            statsText = UIBuilder.CreateText(box, "Stats", "", 30, TextAnchor.MiddleLeft);
            UIBuilder.Place(statsText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 30f), new Vector2(1100f, 50f));
            footer = UIBuilder.CreateText(box, "Footer", "", 26, TextAnchor.MiddleRight, new Color(1f, 1f, 1f, 0.6f));
            UIBuilder.Place(footer.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-50f, 30f), new Vector2(500f, 50f));

            SetVisible(false);
        }

        protected override bool ClosePressed(InputReader input) => input.InventoryPressed;

        protected override void OnOpened() => Rebuild();

        void Rebuild()
        {
            foreach (var go in slots) Destroy(go);
            slots.Clear();

            var gm = GameManager.Instance;
            var data = gm != null ? gm.Data : null;
            Selectable first = null;

            foreach (AbilityType ability in Enum.GetValues(typeof(AbilityType)))
            {
                bool owned = data != null && data.HasAbility(ability);
                string name = owned ? AbilityInfo.DisplayName(ability) : "???";
                string body = owned ? AbilityInfo.Description(ability) : "Not found yet.";
                Sprite icon = owned ? AbilityInfo.Icon(ability) : null;
                var slot = UIBuilder.CreateSlot(abilityRow, "Ability " + ability, icon, name, new Vector2(170f, 190f), owned);
                AddSlot(slot, name, body, icon, ref first);
            }

            int itemCount = 0;
            if (data != null)
            {
                foreach (var id in data.keyItems)
                {
                    string name = ItemDatabase.ItemName(id);
                    var icon = ItemDatabase.ItemIcon(id);
                    var slot = UIBuilder.CreateSlot(itemGrid, "Item " + id, icon, name, new Vector2(150f, 170f), true);
                    AddSlot(slot, name, ItemDatabase.ItemDescription(id), icon, ref first);
                    itemCount++;
                }
            }
            noItemsText.gameObject.SetActive(itemCount == 0);

            if (data != null)
            {
                var player = PlayerController.Instance;
                int energy = player != null && player.Energy != null ? player.Energy.Max : data.maxEnergy;
                var time = TimeSpan.FromSeconds(data.playTimeSeconds);
                statsText.text = $"Health {data.maxHealth}     Energy {energy}     Currency {data.currency}     Time {(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
            }
            else
            {
                statsText.text = "";
            }

            var input = InputReader.Instance;
            footer.text = $"{(input != null ? input.PromptFor("Inventory") : "I")} / Esc: Back";

            if (first != null)
            {
                UIManager.Select(first.gameObject);
            }
            else
            {
                ShowDetails("", "", null);
            }
        }

        void AddSlot(Button slot, string name, string body, Sprite icon, ref Selectable first)
        {
            slots.Add(slot.gameObject);
            var hook = slot.gameObject.AddComponent<SelectionHook>();
            hook.onSelected = () => ShowDetails(name, body, icon);
            if (first == null)
            {
                first = slot;
                ShowDetails(name, body, icon);
            }
        }

        void ShowDetails(string name, string body, Sprite icon)
        {
            detailName.text = name;
            detailBody.text = body;
            detailIcon.sprite = icon;
            detailIcon.enabled = icon != null;
        }
    }
}
