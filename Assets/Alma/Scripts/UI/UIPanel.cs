using UnityEngine;

namespace Alma
{
    /// <summary>Base class for the code-built UI panels.</summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIPanel : MonoBehaviour
    {
        protected CanvasGroup group;

        public bool IsOpen { get; protected set; }

        public void Build()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            OnBuild();
        }

        protected abstract void OnBuild();

        public virtual void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
