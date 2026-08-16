using System;
using System.Collections.Generic;
using Northbound.Narrative;

namespace Northbound.Dialogue
{
    [Serializable]
    public sealed class DialogueChoice
    {
        public string text;
        public string textChinese;
        public string grantedFact;
        public List<NarrativeCounterDelta> counterDeltas = new List<NarrativeCounterDelta>();
        public int nextLineIndex = -1;
    }
}
