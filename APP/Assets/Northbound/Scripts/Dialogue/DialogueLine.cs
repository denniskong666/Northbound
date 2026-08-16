using System;
using System.Collections.Generic;
using Northbound.Narrative;
using UnityEngine;

namespace Northbound.Dialogue
{
    public enum DialoguePresentation
    {
        Character = 0,
        Narration = 1
    }

    [Serializable]
    public sealed class DialogueLine
    {
        public string speakerId;
        public DialoguePresentation presentation;
        [TextArea(2, 6)] public string text;
        [TextArea(2, 6)] public string textChinese;
        public Sprite portrait;
        public AudioClip reactionClip;
        public string requiredFact;
        public string grantedFact;
        public List<NarrativeCounterDelta> counterDeltas = new List<NarrativeCounterDelta>();
        public int nextLineIndex = -1;
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }
}
