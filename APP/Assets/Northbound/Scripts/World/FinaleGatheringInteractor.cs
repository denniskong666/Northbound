using Northbound.Content;
using Northbound.Core;
using Northbound.Endings;
using Northbound.Interaction;
using Northbound.Narrative;
using Northbound.UI;
using UnityEngine;

namespace Northbound.World
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FinaleGatheringInteractor : MonoBehaviour, IInteractable
    {
        public const string ReviewedFact = "finale_routes_reviewed";

        private NarrativeContentDirector director;
        private GameFlowController flow;
        private NarrativeStateStore state;
        private GameObject castRoot;

        public string Prompt => "Review the available routes";
        public bool CanInteract => IsFinaleVisible && !state.Has(ReviewedFact);
        public bool IsFinaleVisible => state != null && flow != null &&
            flow.CurrentChapterId == "finale" && state.Has("cinematic_finale_complete") && !state.Has("ending_selected");

        public void Configure(NarrativeContentDirector content, GameFlowController gameFlow, NarrativeStateStore narrativeState, GameObject characters)
        {
            Unsubscribe();
            director = content;
            flow = gameFlow;
            state = narrativeState;
            castRoot = characters;
            if (flow != null) flow.ChapterEntered += OnChapterEntered;
            if (state != null) state.Changed += Refresh;
            Refresh();
        }

        public void Interact(GameObject actor)
        {
            if (!CanInteract)
            {
                return;
            }

            director?.SetFact(ReviewedFact);
            GameBootstrap.Instance?.Feedback?.Show(RouteSummary(), FeedbackKind.Guidance);
            Refresh();
        }

        private string RouteSummary()
        {
            var northbound = EndingResolver.IsDirectionAvailable(EndingDirection.Northbound, state?.State);
            var home = EndingResolver.IsDirectionAvailable(EndingDirection.HomeChosen, state?.State);
            if (!northbound)
            {
                return GameText.T(
                    "Three routes remain. Your earlier choices closed the northbound road: southwest toward home, east to the unmarked road, or northeast to wait.",
                    "还剩三条路。此前的选择关闭了北上的公路：向西南回家、向东走无名之路，或向东北等待天亮。");
            }
            if (!home)
            {
                return GameText.T(
                    "Three routes remain. Your earlier choices closed the road home: southeast northbound, east to the unmarked road, or northeast to wait.",
                    "还剩三条路。此前的选择关闭了回家的方向：向东南北上、向东走无名之路，或向东北等待天亮。");
            }
            return GameText.T(
                "Four routes remain: southeast northbound, southwest toward home, east to the unmarked road, or northeast to wait.",
                "还剩四条路：向东南北上、向西南回家、向东走无名之路，或向东北等待天亮。");
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnChapterEntered(string _) => Refresh();

        private void Refresh()
        {
            if (castRoot != null)
            {
                castRoot.SetActive(IsFinaleVisible);
            }
        }

        private void Unsubscribe()
        {
            if (flow != null) flow.ChapterEntered -= OnChapterEntered;
            if (state != null) state.Changed -= Refresh;
        }
    }
}
