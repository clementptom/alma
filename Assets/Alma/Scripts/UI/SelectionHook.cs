using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Alma
{
    /// <summary>
    /// Runs a callback when a UI element is selected (gamepad/keyboard) or hovered (mouse),
    /// e.g. to show an item's description.
    /// </summary>
    public class SelectionHook : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public Action onSelected;

        public void OnSelect(BaseEventData eventData) => onSelected?.Invoke();

        public void OnPointerEnter(PointerEventData eventData)
        {
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != gameObject) es.SetSelectedGameObject(gameObject);
            else onSelected?.Invoke();
        }
    }
}
