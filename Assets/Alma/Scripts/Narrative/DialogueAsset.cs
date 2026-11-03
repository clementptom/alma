using System;
using UnityEngine;

namespace Alma
{
    [Serializable]
    public class DialogueLine
    {
        public string speaker = "";
        [TextArea(2, 5)] public string text = "";
        [Tooltip("Optional portrait shown next to the text box.")]
        public Sprite portrait;
    }

    /// <summary>A reusable conversation. Create via Assets > Create > Alma > Dialogue.</summary>
    [CreateAssetMenu(menuName = "Alma/Dialogue", fileName = "NewDialogue")]
    public class DialogueAsset : ScriptableObject
    {
        public DialogueLine[] lines = new DialogueLine[0];
    }
}
