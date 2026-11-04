using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>
    /// Full-screen map built from the rooms the player has discovered. Rooms are drawn as
    /// rectangles at their world positions, so lay out all scenes in shared world coordinates.
    /// </summary>
    public class MapUI : UIPanel
    {
        RectTransform content;
        Text title;
        Text footer;
        readonly List<GameObject> spawned = new List<GameObject>();
        RectTransform playerMarker;
        float blink;

        protected override void OnBuild()
        {
            var bg = UIBuilder.CreateImage(transform, "Background", new Color(0.02f, 0.02f, 0.05f, 0.93f));
            UIBuilder.Stretch(bg.rectTransform);
            bg.raycastTarget = true;

            title = UIBuilder.CreateText(transform, "Title", "", 48, TextAnchor.MiddleCenter, UIBuilder.Skin.accentColor);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1600f, 70f));

            content = UIBuilder.CreateRect("Content", transform);
            UIBuilder.Place(content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1700f, 820f));

            footer = UIBuilder.CreateText(transform, "Footer", "", 26, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f));
            UIBuilder.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1600f, 40f));

            SetVisible(false);
        }

        public void Show()
        {
            Rebuild();
            IsOpen = true;
            SetVisible(true);
        }

        public void Hide()
        {
            IsOpen = false;
            SetVisible(false);
        }

        void Rebuild()
        {
            foreach (var go in spawned) Destroy(go);
            spawned.Clear();
            playerMarker = null;

            var gm = GameManager.Instance;
            var current = RoomTracker.Instance != null ? RoomTracker.Instance.Current : null;
            title.text = current != null ? current.displayName : "Map";
            var input = InputReader.Instance;
            footer.text = $"Blue: save station     Purple: travel point     {(input != null ? input.PromptFor("Map") : "Map")} to close";

            if (gm == null || gm.Data == null || gm.Data.rooms.Count == 0) return;
            var rooms = gm.Data.rooms;

            Rect world = rooms[0].Rect;
            foreach (var r in rooms) world = Union(world, r.Rect);

            Vector2 area = content.rect.size;
            float scale = Mathf.Min(area.x / Mathf.Max(1f, world.width), area.y / Mathf.Max(1f, world.height)) * 0.92f;

            var skin = UIBuilder.Skin;
            foreach (var r in rooms)
            {
                bool isCurrent = current != null && current.Id == r.id;
                var img = UIBuilder.CreateImage(content, "Room " + r.displayName, isCurrent ? skin.mapCurrentRoomColor : skin.mapRoomColor);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = (r.Rect.center - world.center) * scale;
                rt.sizeDelta = new Vector2(Mathf.Max(4f, r.width * scale - 6f), Mathf.Max(4f, r.height * scale - 6f));
                spawned.Add(img.gameObject);
            }

            // Save stations and travel points
            foreach (var m in gm.Data.markers)
            {
                bool travel = m.kind == MapMarkerRecord.Travel;
                Sprite icon = travel ? skin.mapTravelIcon : skin.mapSaveIcon;
                Color color = icon != null ? Color.white : (travel ? skin.mapTravelColor : skin.mapSaveColor);
                var marker = UIBuilder.CreateImage(content, "Marker " + m.label, color, icon);
                var mrt = marker.rectTransform;
                mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.5f);
                mrt.sizeDelta = new Vector2(18f, 18f);
                mrt.anchoredPosition = (m.Position - world.center) * scale;
                if (travel) mrt.localRotation = Quaternion.Euler(0f, 0f, 45f);
                spawned.Add(marker.gameObject);
            }

            var player = PlayerController.Instance;
            if (player != null)
            {
                var marker = UIBuilder.CreateImage(content, "Player", Color.white, skin.mapPlayerIcon);
                playerMarker = marker.rectTransform;
                playerMarker.anchorMin = playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
                playerMarker.sizeDelta = new Vector2(22f, 22f);
                playerMarker.anchoredPosition = (player.Center - world.center) * scale;
                spawned.Add(marker.gameObject);
            }
        }

        static Rect Union(Rect a, Rect b)
        {
            float xMin = Mathf.Min(a.xMin, b.xMin), yMin = Mathf.Min(a.yMin, b.yMin);
            float xMax = Mathf.Max(a.xMax, b.xMax), yMax = Mathf.Max(a.yMax, b.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        void Update()
        {
            if (!IsOpen || playerMarker == null) return;
            blink += Time.unscaledDeltaTime * 4f;
            playerMarker.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(blink));
        }
    }
}
