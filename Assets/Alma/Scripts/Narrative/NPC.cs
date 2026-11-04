using UnityEngine;
using UnityEngine.Events;

namespace Alma
{
    /// <summary>
    /// A character (or sign, statue, journal...) you talk to. Plays its first conversation,
    /// then the repeat conversation on later visits. Lines can be typed inline or come
    /// from a DialogueAsset.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class NPC : Interactable
    {
        public string npcName = "Stranger";

        [Header("First conversation")]
        public DialogueAsset dialogue;
        public DialogueLine[] lines = new DialogueLine[0];

        [Header("Later conversations (optional)")]
        public DialogueAsset repeatDialogue;
        public DialogueLine[] repeatLines = new DialogueLine[0];

        [Header("Behavior")]
        public bool facePlayer = true;
        public Transform visual;
        public Animator animator;
        [Tooltip("Called when the first conversation finishes, e.g. to give an item.")]
        public UnityEvent onFirstConversationEnd = new UnityEvent();

        PersistentId pid;
        CharacterAnimator anim;

        const string TalkedFlag = "talked";

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            if (visual == null) visual = transform.Find("Visual");
            if (animator == null) animator = GetComponentInChildren<Animator>();
            anim = new CharacterAnimator(animator);
            if (prompt == "Interact") prompt = "Talk";
        }

        void Start()
        {
            anim.Play(AnimStates.Idle);
        }

        public override void Interact(PlayerController player)
        {
            if (facePlayer && visual != null)
            {
                Vector3 s = visual.localScale;
                s.x = Mathf.Abs(s.x) * (player.Center.x >= transform.position.x ? 1f : -1f);
                visual.localScale = s;
            }
            player.SetFacing(transform.position.x >= player.Center.x ? 1 : -1);

            bool talkedBefore = pid.IsFlagSet(TalkedFlag);
            DialogueLine[] toPlay = null;
            if (talkedBefore) toPlay = Pick(repeatDialogue, repeatLines);
            if (toPlay == null || toPlay.Length == 0) toPlay = Pick(dialogue, lines);
            if (toPlay == null || toPlay.Length == 0) return;

            DialogueRunner.Play(toPlay, npcName, () =>
            {
                if (!talkedBefore)
                {
                    pid.SetFlag(TalkedFlag);
                    onFirstConversationEnd.Invoke();
                }
            });
        }

        static DialogueLine[] Pick(DialogueAsset asset, DialogueLine[] inline)
        {
            if (asset != null && asset.lines != null && asset.lines.Length > 0) return asset.lines;
            return inline;
        }
    }
}
