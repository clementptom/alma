using System;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Shows and hides objects according to story flags, so the world matches the story
    /// after a reload or a Continue: the priest is gone once he has vanished, the footprints
    /// appear, the camp guards are only there after the capture.
    /// Keep this on an object that is always active; it controls other objects.
    /// </summary>
    public class FlagActivator : MonoBehaviour
    {
        [Serializable]
        public class Rule
        {
            public GameObject target;
            [Tooltip("Shown only once this flag is set (empty = shown from the start).")]
            public string showAfterFlag = "";
            [Tooltip("Hidden for good once this flag is set (empty = never).")]
            public string hideAfterFlag = "";
        }

        public Rule[] rules = new Rule[0];

        void Start()
        {
            Apply();
            GameEvents.FlagSet += OnFlagSet;
        }

        void OnDestroy()
        {
            GameEvents.FlagSet -= OnFlagSet;
        }

        void OnFlagSet(string flag) => Apply();

        public void Apply()
        {
            var gm = GameManager.Instance;
            foreach (var rule in rules)
            {
                if (rule == null || rule.target == null) continue;
                bool shown = string.IsNullOrEmpty(rule.showAfterFlag) || (gm != null && gm.HasFlag(rule.showAfterFlag));
                if (!string.IsNullOrEmpty(rule.hideAfterFlag) && gm != null && gm.HasFlag(rule.hideAfterFlag)) shown = false;
                if (rule.target.activeSelf != shown) rule.target.SetActive(shown);
            }
        }
    }
}
