using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>List of unlocked travel points. Choosing one fades out and takes you there.</summary>
    public class FastTravelUI : OverlayPanel
    {
        string originId;
        RectTransform list;
        Text emptyText;
        readonly List<GameObject> rows = new List<GameObject>();

        public void SetOrigin(string spawnId) => originId = spawnId;

        protected override void OnBuild()
        {
            var skin = UIBuilder.Skin;
            var dim = UIBuilder.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.7f));
            UIBuilder.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var box = UIBuilder.CreatePanel(transform, "Box").rectTransform;
            UIBuilder.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 820f));

            var title = UIBuilder.CreateText(box, "Title", "Travel", 56, TextAnchor.MiddleCenter, skin.accentColor);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(800f, 70f));

            list = UIBuilder.CreateRect("Destinations", box);
            UIBuilder.Place(list, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(760f, 580f));
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            emptyText = UIBuilder.CreateText(box, "Empty", "Find more travel points to travel between them.", 30, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f));
            UIBuilder.Place(emptyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 120f));

            var footer = UIBuilder.CreateText(box, "Footer", "Esc / B: Back", 26, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f));
            UIBuilder.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(800f, 50f));

            SetVisible(false);
        }

        protected override void OnOpened() => Rebuild();

        void Rebuild()
        {
            foreach (var go in rows) Destroy(go);
            rows.Clear();

            var gm = GameManager.Instance;
            Selectable first = null;
            int destinations = 0;
            if (gm != null && gm.Data != null)
            {
                foreach (var marker in gm.Data.markers)
                {
                    if (marker.kind != MapMarkerRecord.Travel) continue;
                    bool here = marker.id == originId;
                    var record = marker;
                    string label = here ? marker.label + "   (you are here)" : marker.label;
                    var button = UIBuilder.CreateButton(list, "Destination " + marker.label, label, () =>
                    {
                        if (GameManager.Instance != null) GameManager.Instance.FastTravel(record);
                    }, new Vector2(760f, 76f));
                    button.interactable = !here;
                    rows.Add(button.gameObject);
                    if (!here)
                    {
                        destinations++;
                        if (first == null) first = button;
                    }
                }
            }
            emptyText.gameObject.SetActive(destinations == 0);
            if (first != null) UIManager.Select(first.gameObject);
        }
    }
}
