using Northbound.Core;
using Northbound.Interaction;
using UnityEngine;
using Northbound.UI;

namespace Northbound.Content
{
    /// <summary>Physical inspect/service interaction that reports one authored quest objective.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class NarrativeObjectiveTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private string questId;
        [SerializeField] private string objectiveId;
        [SerializeField] private string minigameId;
        [SerializeField] private string grantedFact;
        [SerializeField] private string dialogueRouteId;
        [SerializeField] private string prompt = "Continue task";
        private NarrativeContentDirector director;
        private bool waitingForDialogue;

        public string Prompt => prompt;
        public string QuestId => questId;
        public string ObjectiveId => objectiveId;
        public bool CanInteract => director != null && director.CanReportObjective(questId, objectiveId);

        public void Configure(string quest, string objective, string interactionPrompt, NarrativeContentDirector content, string minigame = "",
            string fact = "", string dialogueRoute = "")
        {
            questId = quest;
            objectiveId = objective;
            prompt = interactionPrompt;
            director = content;
            minigameId = minigame ?? string.Empty;
            grantedFact = fact ?? string.Empty;
            dialogueRouteId = dialogueRoute ?? string.Empty;
        }

        private void OnDestroy()
        {
            if (waitingForDialogue && GameBootstrap.Instance?.Dialogue != null)
                GameBootstrap.Instance.Dialogue.Completed -= CompleteAfterDialogue;
        }

        public void Interact(GameObject actor)
        {
            var feedback = GameBootstrap.Instance?.Feedback;
            if (!CanInteract)
            {
                feedback?.Show(GameText.T("Finish the current objective first.", "请先完成当前目标。"), FeedbackKind.Guidance);
                return;
            }
            if (!string.IsNullOrWhiteSpace(minigameId))
            {
                if (GameBootstrap.Instance?.Minigames?.BeginActive(minigameId, questId, objectiveId) != true)
                    feedback?.Show(GameText.T("That activity is not ready yet.", "这个活动还没有准备好。"), FeedbackKind.Error);
                return;
            }
            if (!string.IsNullOrWhiteSpace(dialogueRouteId))
            {
                if (director.HasCompletedDialogueRoute(dialogueRouteId))
                {
                    CompleteObjective();
                    return;
                }
                if (GameBootstrap.Instance?.Dialogue == null || !director.Activate(dialogueRouteId))
                {
                    feedback?.Show(GameText.T("That conversation is not ready yet.", "这段对话还没有准备好。"), FeedbackKind.Error);
                    return;
                }
                waitingForDialogue = true;
                GameBootstrap.Instance.Dialogue.Completed += CompleteAfterDialogue;
                return;
            }
            CompleteObjective();
        }

        private void CompleteAfterDialogue()
        {
            if (GameBootstrap.Instance?.Dialogue != null)
                GameBootstrap.Instance.Dialogue.Completed -= CompleteAfterDialogue;
            waitingForDialogue = false;
            CompleteObjective();
        }

        private void CompleteObjective()
        {
            if (!director.CompleteActiveQuestObjective(objectiveId))
            {
                GameBootstrap.Instance?.Feedback?.Show(GameText.T("Finish the current objective first.", "请先完成当前目标。"), FeedbackKind.Guidance);
                return;
            }
            if (!string.IsNullOrWhiteSpace(grantedFact)) director.SelectCarriedFact(grantedFact);
            GetComponent<ObjectivePropFeedback>()?.PlaySuccessAndHide();
            GameBootstrap.Instance?.Feedback?.Show(GameText.Completion(prompt), FeedbackKind.Success);
        }
    }
}
