using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Northbound.Cinematics;
using Northbound.Core;
using Northbound.Dialogue;
using Northbound.Narrative;
using Northbound.Quests;
using Northbound.Endings;
using UnityEngine;

namespace Northbound.Content
{
    /// <summary>Runtime bridge from approved content IDs to the existing dialogue, quest and cinematic services.</summary>
    public sealed class NarrativeContentDirector : MonoBehaviour
    {
        public const string DialogueRouteCompletionPrefix = "dialogue_route_complete_";
        public const string RooftopDecisionCompletionFact = "dialogue_rooftop_decision_complete";
        private const string CinematicRoutePendingPrefix = "cinematic_route_pending_";
        private static readonly string[] RoutedCinematicIds = { "maya", "noah", "leo", "rooftop" };
        private NarrativeContentManifest manifest;
        private NarrativeContentCatalog catalog;
        private NarrativeStateStore state;
        private QuestRunner questRunner;
        private readonly Dictionary<string, MissionPairController> pairs = new Dictionary<string, MissionPairController>();
        private readonly HashSet<string> cinematicRetryConsumed = new HashSet<string>();
        private string questWaitingForDialogue;
        private string pairWaitingForDialogue;
        private string activeRoutedCinematicId;
        private string scheduledCinematicRetryId;
        private bool chapterTwoRooftopPending;
        private bool rooftopDecisionPending;
        private Coroutine cinematicRetryRoutine;
        private GameFlowController flow;

        public NarrativeContentManifest Manifest => manifest;
        public string ActiveQuestId => questRunner?.ActiveQuestId;
        public string NextObjectiveId => questRunner?.NextObjectiveId;
        public string LastActivatedRouteId { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            flow = FindFirstObjectByType<GameFlowController>();
            if (flow != null)
            {
                flow.ChapterEntered += OnChapterEntered;
                if (!string.IsNullOrWhiteSpace(flow.CurrentChapterId))
                {
                    OnChapterEntered(flow.CurrentChapterId);
                }
            }
        }

        private void OnDestroy()
        {
            if (GameBootstrap.Instance?.Cinematics != null)
            {
                GameBootstrap.Instance.Cinematics.Finished -= OnCinematicFinished;
                GameBootstrap.Instance.Cinematics.Failed -= OnCinematicFailed;
            }
            if (cinematicRetryRoutine != null) StopCoroutine(cinematicRetryRoutine);
            if (flow != null) flow.ChapterEntered -= OnChapterEntered;
            if (questRunner != null) questRunner.QuestCompleted -= OnQuestCompleted;
            if (rooftopDecisionPending && GameBootstrap.Instance?.Dialogue != null)
            {
                GameBootstrap.Instance.Dialogue.Completed -= CompleteRooftopDecision;
            }
        }

        public void EnsureInitialized()
        {
            if (manifest != null)
            {
                return;
            }
            manifest = NarrativeContentManifest.LoadApproved();
            catalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");
            state = GameBootstrap.Instance != null ? GameBootstrap.Instance.NarrativeState : new NarrativeStateStore();
            questRunner = GameBootstrap.Instance?.Minigames?.Quests ?? new QuestRunner(state);
            if (GameBootstrap.Instance?.Cinematics != null)
            {
                GameBootstrap.Instance.Cinematics.Finished += OnCinematicFinished;
                GameBootstrap.Instance.Cinematics.Failed += OnCinematicFailed;
            }
            questRunner.QuestCompleted += OnQuestCompleted;
            questRunner.RestoreActiveQuest(catalog?.quests);
        }

        public bool HasRoute(string routeId) => manifest != null && manifest.FindTrigger(routeId) != null;

        public bool CanActivate(string routeId)
        {
            var route = manifest?.FindTrigger(routeId);
            if (route == null) return false;
            if (route.phase == "automatic") return false;
            if (!IsCurrentChapter(route.chapterId) || !FactsSatisfied(route.prerequisiteFacts)) return false;
            if (route.routeType == "dialogue" && state.Has(DialogueRouteCompletionFact(route.id))) return false;
            if (route.routeType != "quest") return true;
            var quest = manifest.FindQuest(route.targetId);
            return quest != null && CanStartQuest(quest);
        }

        public bool Activate(string routeId)
        {
            var route = manifest?.FindTrigger(routeId);
            if (route == null || catalog == null) return false;
            LastActivatedRouteId = routeId;
            switch (route.routeType)
            {
                case "dialogue": return StartRouteDialogue(route);
                case "cinematic": return GameBootstrap.Instance != null && GameBootstrap.Instance.PlayCinematic(route.targetId);
                case "quest": return StartQuest(route.targetId);
                default: return false;
            }
        }

        public bool StartDialogue(string dialogueId)
        {
            return StartDialogue(dialogueId, 0);
        }

        public bool StartDialogue(string dialogueId, int startLineIndex)
        {
            var dialogue = catalog?.Dialogue(dialogueId);
            if (dialogue == null || GameBootstrap.Instance == null) return false;
            GameBootstrap.Instance.Dialogue.Start(dialogue, startLineIndex);
            return GameBootstrap.Instance.Dialogue.IsRunning;
        }

        public static string DialogueRouteCompletionFact(string routeId) =>
            DialogueRouteCompletionPrefix + (routeId ?? string.Empty);

        public bool HasCompletedDialogueRoute(string routeId) =>
            state != null && state.Has(DialogueRouteCompletionFact(routeId));

        private bool StartRouteDialogue(ContentTrigger route)
        {
            if (route == null || !StartDialogue(route.targetId)) return false;
            GameBootstrap.Instance.Dialogue.Completed += CompleteRouteDialogue;
            return true;

            void CompleteRouteDialogue()
            {
                GameBootstrap.Instance.Dialogue.Completed -= CompleteRouteDialogue;
                state.Set(DialogueRouteCompletionFact(route.id), true);
                PersistOrdinaryProgress();
            }
        }

        public bool PlayEndingDialogue(EndingContext context, Action afterDialogue)
        {
            var id = EndingDialogueMap.DialogueId(context?.DialogueVariantId);
            if (string.IsNullOrWhiteSpace(id) || !StartDialogue(id)) return false;
            GameBootstrap.Instance.Dialogue.Completed += Complete;
            return true;

            void Complete()
            {
                GameBootstrap.Instance.Dialogue.Completed -= Complete;
                afterDialogue?.Invoke();
            }
        }

        private bool StartQuest(string questId)
        {
            var quest = catalog?.Quest(questId);
            var definition = manifest?.FindQuest(questId);
            if (quest == null || definition == null) return false;
            if (!string.IsNullOrEmpty(definition.pairId)) return BeginPairCommitment(definition);
            return StartQuestDialogue(quest, definition);
        }

        public bool CanReportObjective(string questId, string objectiveId)
        {
            return questRunner != null && questRunner.IsCurrentObjective(questId, objectiveId);
        }

        public bool CanCompleteRouteObjective(string routeId)
        {
            var route = manifest?.FindTrigger(routeId);
            var quest = route != null && route.routeType == "quest" ? manifest.FindQuest(route.targetId) : null;
            var asset = quest != null ? catalog?.Quest(quest.id) : null;
            return quest != null && asset != null && quest.completionMode == "physical" &&
                CanReportObjective(quest.id, asset.objectives.FirstOrDefault()?.id);
        }

        public bool CompleteRouteObjective(string routeId)
        {
            var route = manifest?.FindTrigger(routeId);
            var definition = route != null ? manifest.FindQuest(route.targetId) : null;
            var asset = definition != null ? catalog?.Quest(definition.id) : null;
            var objective = asset?.objectives.FirstOrDefault();
            if (definition == null || objective == null || !CanReportObjective(definition.id, objective.id)) return false;
            if (!string.IsNullOrWhiteSpace(definition.minigameId))
                return GameBootstrap.Instance?.Minigames?.BeginActive(definition.minigameId, definition.id, objective.id) == true;
            return CompleteActiveQuestObjective(objective.id, Math.Max(1, objective.requiredAmount));
        }

        public bool CompleteActiveQuestObjective(string objectiveId, int amount = 1)
        {
            var activeId = questRunner?.ActiveQuestId;
            if (string.IsNullOrWhiteSpace(activeId) || !questRunner.Report(objectiveId, amount)) return false;
            return true;
        }

        public void SetFact(string fact)
        {
            state?.Set(fact, true);
            PersistOrdinaryProgress();
        }

        public void SelectCarriedFact(string selectedFact)
        {
            if (string.IsNullOrWhiteSpace(selectedFact)) return;
            foreach (var fact in new[] { "carried_photo", "carried_notebook", "carried_house_key", "carried_old_map" })
            {
                state?.Set(fact, fact == selectedFact);
            }
            PersistOrdinaryProgress();
        }

        private bool BeginPairCommitment(ContentQuest definition)
        {
            if (GameBootstrap.Instance == null || GameBootstrap.Instance.Dialogue == null) return false;
            if (!pairs.TryGetValue(definition.pairId, out var pair))
            {
                var members = manifest.quests.Where(quest => quest != null && quest.pairId == definition.pairId).ToArray();
                if (members.Length != 2) return false;
                pair = new MissionPairController(members[0].id, members[1].id, state, GameBootstrap.Instance.SaveGame, GameBootstrap.Instance.Dialogue);
                pairs.Add(definition.pairId, pair);
            }

            if (!pair.BeginCommitment(definition.id)) return false;
            pairWaitingForDialogue = definition.id;
            GameBootstrap.Instance.Dialogue.Completed += ResumeCommittedPair;
            return true;
        }

        private void ResumeCommittedPair()
        {
            GameBootstrap.Instance.Dialogue.Completed -= ResumeCommittedPair;
            var id = pairWaitingForDialogue;
            pairWaitingForDialogue = null;
            var definition = manifest.FindQuest(id);
            if (definition == null || !pairs.TryGetValue(definition.pairId, out var pair) || pair.CommittedQuestId != id) return;
            StartQuestDialogue(catalog.Quest(id), definition);
        }

        private bool StartQuestDialogue(QuestAsset quest, ContentQuest definition)
        {
            if (!questRunner.StartQuest(quest)) return false;
            var dialogue = catalog?.Dialogue(definition.dialogueId);
            var resumeLineIndex = FindSelectedResponseLine(dialogue, null);
            if (GameBootstrap.Instance == null || !StartDialogue(definition.dialogueId, resumeLineIndex)) return false;
            if (definition.completionMode == "dialogue")
            {
                questWaitingForDialogue = definition.id;
                GameBootstrap.Instance.Dialogue.Completed += CompleteDialogueQuest;
            }
            return true;
        }

        private void CompleteDialogueQuest()
        {
            GameBootstrap.Instance.Dialogue.Completed -= CompleteDialogueQuest;
            var id = questWaitingForDialogue;
            questWaitingForDialogue = null;
            var quest = catalog.Quest(id);
            if (quest == null || quest.objectives == null || quest.objectives.Count == 0) return;
            var definition = manifest.FindQuest(id);
            if (definition == null || definition.completionMode != "dialogue") return;
            CompleteActiveQuestObjective(quest.objectives[0].id, Math.Max(1, quest.objectives[0].requiredAmount));
        }

        private bool CanStartQuest(ContentQuest quest)
        {
            if (questRunner == null || !string.IsNullOrEmpty(questRunner.ActiveQuestId) || state.Has(QuestRunner.CompletionFact(quest.id))) return false;
            if (!IsCurrentChapter(quest.chapterId) || !FactsSatisfied(quest.prerequisiteFacts)) return false;
            foreach (var prerequisite in quest.prerequisiteQuestIds ?? Array.Empty<string>())
            {
                if (state.Has(QuestRunner.CompletionFact(prerequisite))) continue;
                var prerequisiteDefinition = manifest.FindQuest(prerequisite);
                var pairSatisfied = prerequisiteDefinition != null && !string.IsNullOrWhiteSpace(prerequisiteDefinition.pairId) &&
                    manifest.quests.Where(item => item != null && item.pairId == prerequisiteDefinition.pairId)
                        .Any(item => state.Has(QuestRunner.CompletionFact(item.id)));
                if (!pairSatisfied) return false;
            }
            if (string.IsNullOrEmpty(quest.pairId)) return true;
            if (!pairs.TryGetValue(quest.pairId, out var pair))
            {
                var members = manifest.quests.Where(item => item != null && item.pairId == quest.pairId).ToArray();
                if (members.Length != 2) return false;
                pair = new MissionPairController(members[0].id, members[1].id, state, GameBootstrap.Instance?.SaveGame, GameBootstrap.Instance?.Dialogue);
                pairs.Add(quest.pairId, pair);
            }
            return pair.IsAvailable(quest.id);
        }

        private bool IsCurrentChapter(string chapterId)
        {
            var flow = FindFirstObjectByType<GameFlowController>();
            return flow != null && flow.CurrentChapterId == chapterId;
        }

        private bool FactsSatisfied(IEnumerable<string> facts)
        {
            return (facts ?? Array.Empty<string>()).All(fact => !string.IsNullOrWhiteSpace(fact) && state.Has(fact));
        }

        private void OnQuestCompleted(string id)
        {
            if (id == "rooftop_inventory")
            {
                ChapterStoryMarkResolver.ResolveChapterOne(state);
            }

            RecordFriendMission(id);
            if (id == "first_light")
            {
                if (!BeginCinematicRoute("maya")) AdvanceWorldAfter(id);
            }
            else if (id == "static") BeginCinematicRoute("noah");
            else if (id == "last_night_open")
            {
                if (!BeginCinematicRoute("leo") && !BeginCinematicRoute("rooftop")) BeginRooftopDecisionOrAdvance();
            }
            else if (id == "pack_trunk")
            {
                if (!BeginCinematicRoute("rooftop")) BeginRooftopDecisionOrAdvance();
            }
            else if (id == "one_more_table") BeginChapterTwoRooftop();
            else AdvanceWorldAfter(id);
            PersistOrdinaryProgress();
        }

        private void OnCinematicFinished(string cinematicId)
        {
            flow ??= FindFirstObjectByType<GameFlowController>();
            if (flow == null) return;
            if (activeRoutedCinematicId == cinematicId) activeRoutedCinematicId = null;
            cinematicRetryConsumed.Remove(cinematicId);
            if (cinematicId == "opening") flow.EnterChapter("chapter_1");
            else if (cinematicId == "maya")
            {
                CompletePendingCinematic("maya");
                if (flow.CurrentChapterId == "chapter_3_day_3") AdvanceWorldAfter("first_light");
            }
            else if (cinematicId == "noah")
            {
                CompletePendingCinematic("noah");
            }
            else if (cinematicId == "leo")
            {
                CompletePendingCinematic("leo");
                if (flow.CurrentChapterId == "chapter_3_day_2" &&
                    !BeginCinematicRoute("rooftop")) BeginRooftopDecisionOrAdvance();
            }
            else if (cinematicId == "rooftop")
            {
                CompletePendingCinematic("rooftop");
                if (flow.CurrentChapterId is "chapter_3_day_2" or "chapter_4") BeginRooftopDecisionOrAdvance();
            }

            if (GameBootstrap.Instance?.Cinematics?.IsPlaying != true)
                ResumePendingCinematic(flow.CurrentChapterId);
        }

        private void OnCinematicFailed(string error)
        {
            var cinematicId = activeRoutedCinematicId;
            activeRoutedCinematicId = null;
            if (string.IsNullOrWhiteSpace(cinematicId) ||
                !state.Has(CinematicRoutePendingFact(cinematicId))) return;

            ScheduleCinematicRetry(cinematicId, error);
        }

        private bool BeginCinematicRoute(string cinematicId)
        {
            if (string.IsNullOrWhiteSpace(cinematicId)) return false;
            if (state.Has(CinematicCompletionFact(cinematicId))) return false;

            var pendingFact = CinematicRoutePendingFact(cinematicId);
            if (!state.Has(pendingFact))
            {
                state.Set(pendingFact, true);
                PersistOrdinaryProgress();
            }

            cinematicRetryConsumed.Remove(cinematicId);
            flow ??= FindFirstObjectByType<GameFlowController>();
            var chapterId = flow?.CurrentChapterId;
            if (!CanResumeInChapter(cinematicId, chapterId)) return true;
            var firstPending = FirstPendingCinematic(chapterId);
            if (firstPending != cinematicId)
            {
                ResumePendingCinematic(chapterId);
                return true;
            }
            TryPlayPendingCinematic(cinematicId, true);
            return true;
        }

        private bool TryPlayPendingCinematic(string cinematicId, bool allowAutomaticRetry)
        {
            if (GameBootstrap.Instance?.PlayCinematic(cinematicId) == true)
            {
                if (scheduledCinematicRetryId == cinematicId) CancelScheduledCinematicRetry();
                activeRoutedCinematicId = cinematicId;
                return true;
            }

            if (allowAutomaticRetry) ScheduleCinematicRetry(cinematicId, "the player was not ready");
            else Debug.LogWarning($"Cinematic '{cinematicId}' remains pending after its automatic retry.", this);
            return false;
        }

        private void ScheduleCinematicRetry(string cinematicId, string reason)
        {
            if (cinematicRetryConsumed.Contains(cinematicId) || scheduledCinematicRetryId == cinematicId) return;
            cinematicRetryConsumed.Add(cinematicId);
            scheduledCinematicRetryId = cinematicId;
            cinematicRetryRoutine = StartCoroutine(RetryPendingCinematicNextFrame(cinematicId));
            Debug.LogWarning($"Cinematic '{cinematicId}' failed ({reason}); retrying once without advancing the story.", this);
        }

        private IEnumerator RetryPendingCinematicNextFrame(string cinematicId)
        {
            yield return null;
            if (scheduledCinematicRetryId != cinematicId) yield break;
            scheduledCinematicRetryId = null;
            cinematicRetryRoutine = null;

            flow ??= FindFirstObjectByType<GameFlowController>();
            var chapterId = flow?.CurrentChapterId;
            if (!state.Has(CinematicRoutePendingFact(cinematicId)) ||
                state.Has(CinematicCompletionFact(cinematicId)) ||
                !CanResumeInChapter(cinematicId, chapterId)) yield break;

            TryPlayPendingCinematic(cinematicId, false);
        }

        private void OnChapterEntered(string chapterId)
        {
            CancelScheduledCinematicRetry();
            foreach (var cinematicId in RoutedCinematicIds)
            {
                if (state.Has(CinematicRoutePendingFact(cinematicId)) && CanResumeInChapter(cinematicId, chapterId))
                    cinematicRetryConsumed.Remove(cinematicId);
            }
            ResumePendingCinematic(chapterId);
        }

        private void CancelScheduledCinematicRetry()
        {
            if (cinematicRetryRoutine != null) StopCoroutine(cinematicRetryRoutine);
            cinematicRetryRoutine = null;
            scheduledCinematicRetryId = null;
        }

        private void ResumePendingCinematic(string chapterId)
        {
            if (GameBootstrap.Instance?.Cinematics?.IsPlaying == true) return;
            DiscardOutdatedPendingCinematics(chapterId);
            RecoverExpectedCinematic(chapterId);
            foreach (var cinematicId in RoutedCinematicIds)
            {
                if (!state.Has(CinematicRoutePendingFact(cinematicId))) continue;
                if (!CanResumeInChapter(cinematicId, chapterId)) continue;
                if (state.Has(CinematicCompletionFact(cinematicId))) OnCinematicFinished(cinematicId);
                else TryPlayPendingCinematic(cinematicId, true);
                return;
            }

            EnsureCompletedRouteProgress(chapterId);
        }

        private string FirstPendingCinematic(string chapterId)
        {
            return RoutedCinematicIds.FirstOrDefault(cinematicId =>
                state.Has(CinematicRoutePendingFact(cinematicId)) &&
                !state.Has(CinematicCompletionFact(cinematicId)) &&
                CanResumeInChapter(cinematicId, chapterId));
        }

        private void RecoverExpectedCinematic(string chapterId)
        {
            if (chapterId == "chapter_3_day_3")
            {
                if (state.Has(QuestRunner.CompletionFact("first_light")) &&
                    !state.Has(CinematicCompletionFact("maya"))) QueuePendingCinematic("maya");
                return;
            }

            if (chapterId == "chapter_3_day_2")
            {
                if (state.Has(QuestRunner.CompletionFact("static")) &&
                    !state.Has(CinematicCompletionFact("noah")))
                {
                    QueuePendingCinematic("noah");
                    return;
                }

                if (state.Has(QuestRunner.CompletionFact("last_night_open")) &&
                    !state.Has(CinematicCompletionFact("leo")))
                {
                    QueuePendingCinematic("leo");
                    return;
                }
            }

            if (chapterId is ("chapter_3_day_2" or "chapter_4") &&
                (state.Has(QuestRunner.CompletionFact("last_night_open")) ||
                 state.Has(QuestRunner.CompletionFact("pack_trunk"))) &&
                !state.Has(CinematicCompletionFact("rooftop")))
            {
                QueuePendingCinematic("rooftop");
            }
        }

        private void DiscardOutdatedPendingCinematics(string chapterId)
        {
            if (chapterId == "chapter_3_day_2")
            {
                ClearPendingCinematic("maya");
            }
            else if (chapterId == "chapter_4")
            {
                ClearPendingCinematic("maya");
                ClearPendingCinematic("noah");
                ClearPendingCinematic("leo");
            }
            else if (chapterId is "finale" or "prologue" or "chapter_1" or "chapter_2")
            {
                foreach (var cinematicId in RoutedCinematicIds) ClearPendingCinematic(cinematicId);
            }
        }

        private void ClearPendingCinematic(string cinematicId)
        {
            var pendingFact = CinematicRoutePendingFact(cinematicId);
            if (!state.Has(pendingFact)) return;
            state.Set(pendingFact, false);
            cinematicRetryConsumed.Remove(cinematicId);
            if (scheduledCinematicRetryId == cinematicId) CancelScheduledCinematicRetry();
            PersistOrdinaryProgress();
        }

        private static bool CanResumeInChapter(string cinematicId, string chapterId) => cinematicId switch
        {
            "maya" => chapterId == "chapter_3_day_3",
            "noah" or "leo" => chapterId == "chapter_3_day_2",
            "rooftop" => chapterId is "chapter_3_day_2" or "chapter_4",
            _ => false
        };

        private void EnsureCompletedRouteProgress(string chapterId)
        {
            if (chapterId == "chapter_3_day_3" &&
                (state.Has(QuestRunner.CompletionFact("alternator")) ||
                 (state.Has(QuestRunner.CompletionFact("first_light")) && state.Has(CinematicCompletionFact("maya")))))
            {
                AdvanceWorldAfter("alternator");
            }
            else if (chapterId == "chapter_3_day_2" &&
                     (state.Has(QuestRunner.CompletionFact("last_night_open")) ||
                      state.Has(QuestRunner.CompletionFact("pack_trunk"))) &&
                     state.Has(CinematicCompletionFact("rooftop")))
            {
                BeginRooftopDecisionOrAdvance();
            }
        }

        private void QueuePendingCinematic(string cinematicId)
        {
            var pendingFact = CinematicRoutePendingFact(cinematicId);
            if (!state.Has(pendingFact))
            {
                state.Set(pendingFact, true);
                PersistOrdinaryProgress();
            }
        }

        private void CompletePendingCinematic(string cinematicId)
        {
            state.Set(CinematicRoutePendingFact(cinematicId), false);
            cinematicRetryConsumed.Remove(cinematicId);
            if (scheduledCinematicRetryId == cinematicId) CancelScheduledCinematicRetry();
            PersistOrdinaryProgress();
        }

        public static string CinematicRoutePendingFact(string cinematicId) =>
            CinematicRoutePendingPrefix + (cinematicId ?? string.Empty);

        private static string CinematicCompletionFact(string cinematicId) =>
            $"cinematic_{cinematicId ?? string.Empty}_complete";

        private void EnterChapterFour()
        {
            flow ??= FindFirstObjectByType<GameFlowController>();
            if (flow != null && flow.CurrentChapterId != "chapter_4") flow.EnterChapter("chapter_4");
        }

        private void BeginRooftopDecisionOrAdvance()
        {
            if (state.Has(RooftopDecisionCompletionFact))
            {
                EnterChapterFour();
                return;
            }

            if (rooftopDecisionPending || GameBootstrap.Instance?.Dialogue == null)
            {
                return;
            }

            var dialogue = catalog?.Dialogue("rooftop_decision");
            if (dialogue == null || GameBootstrap.Instance == null)
            {
                Debug.LogError("The rooftop cinematic finished, but rooftop_decision is unavailable. The story remains in chapter 3 until the decision is available.", this);
                return;
            }

            var resumeLineIndex = FindSelectedResponseLine(dialogue, "story_mark_ch3_");
            GameBootstrap.Instance.Dialogue.Start(dialogue, resumeLineIndex);
            if (!GameBootstrap.Instance.Dialogue.IsRunning)
            {
                Debug.LogError("The rooftop cinematic finished, but rooftop_decision could not start. The story remains in chapter 3 until the decision is available.", this);
                return;
            }

            rooftopDecisionPending = true;
            GameBootstrap.Instance.Dialogue.Completed += CompleteRooftopDecision;
        }

        private void CompleteRooftopDecision()
        {
            if (GameBootstrap.Instance?.Dialogue != null)
            {
                GameBootstrap.Instance.Dialogue.Completed -= CompleteRooftopDecision;
            }

            rooftopDecisionPending = false;
            if (!HasAnyStoryMark("ch3"))
            {
                Debug.LogError("The rooftop decision ended without recording a chapter-three story mark. Chapter 4 remains locked so the choice cannot be silently discarded.", this);
                return;
            }
            state.Set(RooftopDecisionCompletionFact, true);
            PersistOrdinaryProgress();
            EnterChapterFour();
        }

        private int FindSelectedResponseLine(DialogueAsset dialogue, string factPrefix)
        {
            if (dialogue?.lines == null) return 0;
            foreach (var line in dialogue.lines)
            {
                if (line?.choices == null) continue;
                foreach (var choice in line.choices)
                {
                    if (choice == null || string.IsNullOrWhiteSpace(choice.grantedFact) ||
                        (!string.IsNullOrEmpty(factPrefix) && !choice.grantedFact.StartsWith(factPrefix, StringComparison.Ordinal)) ||
                        !state.Has(choice.grantedFact)) continue;
                    return choice.nextLineIndex >= 0 ? choice.nextLineIndex : 0;
                }
            }

            return 0;
        }

        private bool HasAnyStoryMark(string chapter)
        {
            var prefix = $"story_mark_{chapter}_";
            return state.Has(prefix + "a") || state.Has(prefix + "b") || state.Has(prefix + "c");
        }

        private void BeginChapterTwoRooftop()
        {
            if (chapterTwoRooftopPending || !StartDialogue("chapter_two_rooftop")) return;
            chapterTwoRooftopPending = true;
            GameBootstrap.Instance.Dialogue.Completed += CompleteChapterTwoRooftop;
        }

        private void CompleteChapterTwoRooftop()
        {
            GameBootstrap.Instance.Dialogue.Completed -= CompleteChapterTwoRooftop;
            chapterTwoRooftopPending = false;
            FindFirstObjectByType<GameFlowController>()?.EnterChapter("chapter_3_day_3");
        }

        private void PersistOrdinaryProgress()
        {
            var bootstrap = GameBootstrap.Instance;
            bootstrap?.SaveGame?.Save(NarrativeState.FromJson(state.State.ToJson()));
        }

        private void RecordFriendMission(string questId)
        {
            var friend = questId switch
            {
                "first_light" => "maya",
                "static" => "noah",
                "last_night_open" => "leo",
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(friend)) return;
            state.Add($"bond_{friend}", 1);
            var order = state.GetInt("friend_completion_count") + 1;
            state.Add("friend_completion_count", 1);
            state.Set($"friend_{friend}_completion_order_{order}", true);
        }

        private void AdvanceWorldAfter(string questId)
        {
            var flow = FindFirstObjectByType<GameFlowController>();
            if (flow == null) return;
            var target = questId switch
            {
                "rooftop_inventory" => "chapter_2",
                "alternator" or "first_light" => "chapter_3_day_2",
                "road_test" or "static" => "chapter_3_day_2",
                "before_morning" => "finale",
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(target) && flow.CurrentChapterId != target) flow.EnterChapter(target);
        }
    }
}
