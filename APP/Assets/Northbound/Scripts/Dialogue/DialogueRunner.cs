using System;
using System.Collections.Generic;
using Northbound.Core;
using Northbound.Narrative;

namespace Northbound.Dialogue
{
    public sealed class DialogueRunner
    {
        public const int MaximumChoices = 4;

        private readonly NarrativeStateStore narrativeState;
        private readonly InputGate inputGate;
        private IDisposable inputLease;
        private DialogueAsset asset;
        private int currentIndex = -1;
        private int terminalChoiceResponseIndex = -1;

        public DialogueRunner(NarrativeStateStore narrativeState, InputGate inputGate = null)
        {
            this.narrativeState = narrativeState ?? throw new ArgumentNullException(nameof(narrativeState));
            this.inputGate = inputGate;
        }

        public DialogueLine Current { get; private set; }
        public string ActiveDialogueId => asset != null ? asset.id : string.Empty;
        public int CurrentLineIndex => currentIndex;

        public bool IsRunning { get; private set; }

        public string LastValidationError { get; private set; } = string.Empty;

        public event Action Changed;

        public event Action Completed;

        public void Start(DialogueAsset dialogueAsset)
        {
            Start(dialogueAsset, 0);
        }

        public void Start(DialogueAsset dialogueAsset, int startLineIndex)
        {
            Stop();
            LastValidationError = string.Empty;
            if (dialogueAsset == null || dialogueAsset.lines == null || dialogueAsset.lines.Count == 0)
            {
                return;
            }

            if (!dialogueAsset.TryValidate(out var validationError))
            {
                LastValidationError = validationError;
                return;
            }

            asset = dialogueAsset;
            if (!TrySetCurrent(startLineIndex))
            {
                asset = null;
                return;
            }

            IsRunning = true;
            inputLease = inputGate?.Acquire(this);
            Changed?.Invoke();
        }

        public bool Advance()
        {
            if (!IsRunning || HasChoices(Current))
            {
                return false;
            }

            Apply(Current.grantedFact, Current.counterDeltas);
            if (currentIndex == terminalChoiceResponseIndex && Current.nextLineIndex < 0)
            {
                terminalChoiceResponseIndex = -1;
                Complete();
                return true;
            }
            MoveToNext(Current.nextLineIndex);
            return true;
        }

        public bool Choose(int index)
        {
            if (!IsRunning || !HasChoices(Current) || index < 0 || index >= MaximumChoices || index >= Current.choices.Count)
            {
                return false;
            }

            var choice = Current.choices[index];
            if (choice == null)
            {
                return false;
            }

            Apply(Current.grantedFact, Current.counterDeltas);
            Apply(choice.grantedFact, choice.counterDeltas);
            if ((choice.counterDeltas == null || choice.counterDeltas.Count == 0) &&
                !string.IsNullOrWhiteSpace(choice.grantedFact))
            {
                ChoiceConsequenceResolver.ApplyImplicit(narrativeState, choice.grantedFact);
            }
            terminalChoiceResponseIndex = choice.nextLineIndex >= 0 ? choice.nextLineIndex : currentIndex + 1;
            MoveToNext(choice.nextLineIndex);
            return true;
        }

        public void Stop()
        {
            IsRunning = false;
            Current = null;
            asset = null;
            currentIndex = -1;
            terminalChoiceResponseIndex = -1;
            ReleaseInput();
            Changed?.Invoke();
        }

        public void ResetSession()
        {
            Stop();
            Completed = null;
        }

        private void MoveToNext(int nextLineIndex)
        {
            var index = nextLineIndex >= 0 ? nextLineIndex : currentIndex + 1;
            if (!TrySetCurrent(index))
            {
                Complete();
                return;
            }

            Changed?.Invoke();
        }

        private bool TrySetCurrent(int index)
        {
            var visited = new HashSet<int>();
            while (index >= 0 && index < asset.lines.Count && visited.Add(index))
            {
                var candidate = asset.lines[index];
                if (candidate != null && (string.IsNullOrEmpty(candidate.requiredFact) || narrativeState.Has(candidate.requiredFact)))
                {
                    currentIndex = index;
                    Current = candidate;
                    return true;
                }

                // A gated line that is not eligible is only skipped locally. Its
                // nextLineIndex belongs to the line after it has actually played;
                // following it here would skip later sibling callbacks.
                index++;
            }

            return false;
        }

        private void Complete()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            Current = null;
            asset = null;
            currentIndex = -1;
            ReleaseInput();
            Changed?.Invoke();
            Completed?.Invoke();
        }

        private void Apply(string fact, IEnumerable<NarrativeCounterDelta> counterDeltas)
        {
            if (!string.IsNullOrEmpty(fact))
            {
                if (!ChapterStoryMarkResolver.TrySetExclusive(narrativeState, fact))
                    narrativeState.Set(fact, true);
            }

            if (counterDeltas == null)
            {
                return;
            }

            foreach (var delta in counterDeltas)
            {
                if (delta != null && !string.IsNullOrWhiteSpace(delta.id) && delta.amount != 0)
                {
                    narrativeState.Add(delta.id, delta.amount);
                }
            }
        }

        private void ReleaseInput()
        {
            inputLease?.Dispose();
            inputLease = null;
        }

        private static bool HasChoices(DialogueLine line)
        {
            return line != null && line.choices != null && line.choices.Count > 0;
        }
    }
}
