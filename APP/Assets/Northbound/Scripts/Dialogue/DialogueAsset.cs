using System.Collections.Generic;
using UnityEngine;

namespace Northbound.Dialogue
{
    [CreateAssetMenu(menuName = "Northbound/Dialogue")]
    public sealed class DialogueAsset : ScriptableObject
    {
        public string id;
        public List<DialogueLine> lines = new List<DialogueLine>();

        public bool TryValidate(out string error)
        {
            if (lines != null)
            {
                for (var index = 0; index < lines.Count; index++)
                {
                    var line = lines[index];
                    if (line != null && line.choices != null && line.choices.Count > DialogueRunner.MaximumChoices)
                    {
                        error = $"Dialogue line {index} has more than four choices.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
