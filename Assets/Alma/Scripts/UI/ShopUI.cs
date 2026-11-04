using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>Shop screen: the stock list on the left, details on the right, currency at the top.</summary>
    public class ShopUI : OverlayPanel
    {
        Shopkeeper shop;
        Text title;
        Text currency;
        RectTransform list;
        Image detailIcon;
        Text detailName;
        Text detailBody;
        Text detailPrice;
        Text message;
        readonly List<GameObject> rows = new List<GameObject>();
        readonly List<Text> priceLabels = new List<Text>();
        int selected;

        public void SetShop(Shopkeeper shopkeeper) => shop = shopkeeper;

        protected override void OnBuild()
        {
            var skin = UIBuilder.Skin;
            var dim = UIBuilder.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.7f));
            UIBuilder.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var box = UIBuilder.CreatePanel(transform, "Box").rectTransform;
            UIBuilder.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1440f, 860f));

            title = UIBuilder.CreateText(box, "Title", "", 52, TextAnchor.MiddleLeft, skin.accentColor);
            UIBuilder.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -24f), new Vector2(900f, 70f));
            currency = UIBuilder.CreateText(box, "Currency", "", 38, TextAnchor.MiddleRight, skin.currencyColor);
            UIBuilder.Place(currency.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -24f), new Vector2(500f, 70f));

            list = UIBuilder.CreateRect("Stock", box);
            UIBuilder.Place(list, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -120f), new Vector2(720f, 620f));
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var details = UIBuilder.CreateImage(box, "Details", new Color(0f, 0f, 0f, 0.35f)).rectTransform;
            UIBuilder.Place(details, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -120f), new Vector2(540f, 620f));
            detailIcon = UIBuilder.CreateImage(details, "Icon", Color.white);
            detailIcon.preserveAspect = true;
            UIBuilder.Place(detailIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(200f, 200f));
            detailName = UIBuilder.CreateText(details, "Name", "", 40, TextAnchor.MiddleCenter, skin.accentColor);
            UIBuilder.Place(detailName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(500f, 60f));
            detailBody = UIBuilder.CreateText(details, "Description", "", 28, TextAnchor.UpperCenter);
            UIBuilder.Place(detailBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(480f, 200f));
            detailPrice = UIBuilder.CreateText(details, "Price", "", 34, TextAnchor.MiddleCenter, skin.currencyColor);
            UIBuilder.Place(detailPrice.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(480f, 50f));

            message = UIBuilder.CreateText(box, "Message", "", 30, TextAnchor.MiddleLeft);
            UIBuilder.Place(message.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 30f), new Vector2(900f, 50f));
            var footer = UIBuilder.CreateText(box, "Footer", "Esc / B: Leave", 26, TextAnchor.MiddleRight, new Color(1f, 1f, 1f, 0.6f));
            UIBuilder.Place(footer.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 30f), new Vector2(400f, 50f));

            SetVisible(false);
        }

        protected override void OnOpened()
        {
            message.text = "";
            Rebuild();
        }

        void Rebuild()
        {
            foreach (var go in rows) Destroy(go);
            rows.Clear();
            priceLabels.Clear();
            if (shop == null) return;

            title.text = shop.shopName;
            var skin = UIBuilder.Skin;
            Selectable first = null;
            var size = new Vector2(720f, 78f);

            for (int i = 0; i < shop.stock.Length; i++)
            {
                int index = i;
                var bg = UIBuilder.CreateImage(list, "Entry " + i, Color.white, skin.buttonSprite);
                bg.raycastTarget = true;
                bg.rectTransform.sizeDelta = size;
                var element = bg.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = size.x;
                element.preferredHeight = size.y;
                var button = bg.gameObject.AddComponent<Button>();
                var colors = button.colors;
                colors.normalColor = skin.buttonColor;
                colors.highlightedColor = skin.buttonHighlightColor;
                colors.selectedColor = skin.buttonHighlightColor;
                colors.pressedColor = skin.accentColor;
                button.colors = colors;
                button.targetGraphic = bg;
                button.onClick.AddListener(() => Buy(index));

                var name = UIBuilder.CreateText(bg.transform, "Name", shop.EntryName(i), 32, TextAnchor.MiddleLeft);
                UIBuilder.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(500f, 60f));
                var price = UIBuilder.CreateText(bg.transform, "Price", "", 32, TextAnchor.MiddleRight, skin.currencyColor);
                UIBuilder.Place(price.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(200f, 60f));
                priceLabels.Add(price);

                var hook = bg.gameObject.AddComponent<SelectionHook>();
                hook.onSelected = () => ShowDetails(index);
                rows.Add(bg.gameObject);
                if (first == null) first = button;
            }

            RefreshPrices();
            if (first != null)
            {
                UIManager.Select(first.gameObject);
                ShowDetails(0);
            }
            else
            {
                detailName.text = "Nothing for sale.";
                detailBody.text = "";
                detailPrice.text = "";
                detailIcon.enabled = false;
            }
        }

        void RefreshPrices()
        {
            var gm = GameManager.Instance;
            currency.text = "Currency: " + (gm != null ? gm.Currency : 0);
            for (int i = 0; i < priceLabels.Count; i++)
                priceLabels[i].text = shop.IsSoldOut(i) ? "Sold" : shop.stock[i].price.ToString();
        }

        void ShowDetails(int index)
        {
            if (shop == null || index < 0 || index >= shop.stock.Length) return;
            selected = index;
            detailName.text = shop.EntryName(index);
            detailBody.text = shop.EntryDescription(index);
            var icon = shop.EntryIcon(index);
            detailIcon.sprite = icon;
            detailIcon.enabled = icon != null;
            detailPrice.text = shop.IsSoldOut(index) ? "Sold out" : "Price: " + shop.stock[index].price;
        }

        void Buy(int index)
        {
            if (shop == null) return;
            bool ok = shop.TryBuy(index, out string result);
            message.text = result;
            AudioManager.PlaySfx(ok ? AudioIds.ShopBuy : AudioIds.UiError);
            RefreshPrices();
            ShowDetails(selected);
        }
    }
}
