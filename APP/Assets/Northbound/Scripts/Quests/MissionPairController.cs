using System;
using System.Collections.Generic;
using Northbound.Core;
using Northbound.Dialogue;
using Northbound.Narrative;
using UnityEngine;

namespace Northbound.Quests
{
    public sealed class MissionPairController
    {
        public const string CommitmentMessage = "This will take the rest of the evening.";

        private readonly string firstQuestId;
        private readonly string secondQuestId;
        private readonly NarrativeStateStore state;
        private readonly SaveGameService saveGame;
        private readonly DialogueRunner dialogue;
        private string pendingQuestId;
        private string confirmationFact;

        public MissionPairController(string firstQuestId, string secondQuestId, NarrativeStateStore narrativeState)
            : this(firstQuestId, secondQuestId, narrativeState, null, null)
        {
        }

        public MissionPairController(
            string firstQuestId,
            string secondQuestId,
            NarrativeStateStore narrativeState,
            SaveGameService saveGame,
            DialogueRunner dialogue = null)
        {
            if (string.IsNullOrWhiteSpace(firstQuestId))
            {
                throw new ArgumentException("A first mission id is required.", nameof(firstQuestId));
            }

            if (string.IsNullOrWhiteSpace(secondQuestId))
            {
                throw new ArgumentException("A second mission id is required.", nameof(secondQuestId));
            }

            if (firstQuestId == secondQuestId)
            {
                throw new ArgumentException("Mission pairs require two different mission ids.", nameof(secondQuestId));
            }

            this.firstQuestId = firstQuestId;
            this.secondQuestId = secondQuestId;
            state = narrativeState ?? throw new ArgumentNullException(nameof(narrativeState));
            this.saveGame = saveGame;
            this.dialogue = dialogue;
        }

        public string CommittedQuestId => LoadCommittedQuestId();

        public string PendingMessage => pendingQuestId == null ? null : CommitmentMessage;

        public bool IsAvailable(string questId)
        {
            var committedQuestId = CommittedQuestId;
            return IsPairQuest(questId) && (committedQuestId == null || committedQuestId == questId);
        }

        public bool BeginCommitment(string questId)
        {
            if (!IsAvailable(questId) || pendingQuestId != null)
            {
                return false;
            }

            pendingQuestId = questId;
            if (dialogue != null)
            {
                confirmationFact = $"mission_pair_{PairId}_confirm_{questId}";
                dialogue.Completed += ResolveDialogueCommitment;
                dialogue.Start(CreateConfirmationDialogue(confirmationFact));
            }

            return true;
        }

        public bool ConfirmCommitment()
        {
            if (pendingQuestId == null)
            {
                return false;
            }

            if (!TryCommit(pendingQuestId))
            {
                pendingQuestId = null;
                return false;
            }

            pendingQuestId = null;
            return true;
        }

        public void CancelCommitment()
        {
            pendingQuestId = null;
            if (dialogue != null)
            {
                dialogue.Completed -= ResolveDialogueCommitment;
                dialogue.Stop();
            }
        }

        public bool TryCommit(string questId)
        {
            if (!IsPairQuest(questId))
            {
                return false;
            }

            var committedQuestId = CommittedQuestId;
            if (committedQuestId != null)
            {
                return committedQuestId == questId;
            }

            var prospectiveState = NarrativeState.FromJson(state.State.ToJson());
            prospectiveState.Set(CommitmentFact(questId), true);
            prospectiveState.Set(MissedFact(OtherQuestId(questId)), true);
            if (saveGame != null && !saveGame.Save(prospectiveState))
            {
                return false;
            }

            state.Set(CommitmentFact(questId), true);
            state.Set(MissedFact(OtherQuestId(questId)), true);
            return true;
        }

        private void ResolveDialogueCommitment()
        {
            dialogue.Completed -= ResolveDialogueCommitment;
            var shouldCommit = !string.IsNullOrEmpty(confirmationFact) && state.Has(confirmationFact);
            state.Set(confirmationFact, false);
            confirmationFact = null;
            if (shouldCommit)
            {
                ConfirmCommitment();
            }
            else
            {
                pendingQuestId = null;
            }
        }

        private DialogueAsset CreateConfirmationDialogue(string fact)
        {
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            asset.id = $"mission_pair_{PairId}_confirmation";
            asset.lines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    speakerId = "Narrator",
                    presentation = DialoguePresentation.Narration,
                    text = CommitmentMessage,
                    textChinese = "这项任务会占用今晚剩下的时间。",
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { text = "Commit", textChinese = "确认投入", grantedFact = fact, nextLineIndex = -1 },
                        new DialogueChoice { text = "Back", textChinese = "返回", nextLineIndex = -1 }
                    }
                }
            };
            return asset;
        }

        private string LoadCommittedQuestId()
        {
            if (state.Has(CommitmentFact(firstQuestId)))
            {
                return firstQuestId;
            }

            return state.Has(CommitmentFact(secondQuestId)) ? secondQuestId : null;
        }

        private bool IsPairQuest(string questId) => questId == firstQuestId || questId == secondQuestId;

        private string OtherQuestId(string questId) => questId == firstQuestId ? secondQuestId : firstQuestId;

        private string PairId => string.CompareOrdinal(firstQuestId, secondQuestId) < 0
            ? $"{firstQuestId}_{secondQuestId}"
            : $"{secondQuestId}_{firstQuestId}";

        private string CommitmentFact(string questId) => $"mission_pair_{PairId}_committed_{questId}";

        private static string MissedFact(string questId) => $"missed_{questId}";
    }
}
