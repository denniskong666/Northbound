using System;
using Northbound.Narrative;
using Northbound.World;
using UnityEngine;

namespace Northbound.Core
{
    public sealed class GameFlowController : MonoBehaviour
    {
        private const string CurrentChapterPrefix = "current_chapter_";

        [SerializeField] private ChapterWorldController chapterWorld;
        private NarrativeStateStore narrativeState;
        private SaveGameService saveGame;

        public string CurrentChapterId { get; private set; }
        public event Action<string> ChapterEntered;

        private void Awake()
        {
            if (narrativeState == null && GameBootstrap.Instance != null)
            {
                Initialize(GameBootstrap.Instance.NarrativeState, GameBootstrap.Instance.SaveGame, chapterWorld);
            }
        }

        private void Start()
        {
            if (!IsReady())
            {
                return;
            }

            if (GameBootstrap.Instance != null && !GameBootstrap.Instance.IsSessionActive)
            {
                return;
            }

            RestoreOrEnterPrologue();
        }

        public bool RestoreOrEnterPrologue() => RestoreCurrentChapter() || EnterChapter("prologue");

        public void Initialize(NarrativeStateStore state, SaveGameService save, ChapterWorldController world)
        {
            narrativeState = state ?? throw new ArgumentNullException(nameof(state));
            saveGame = save ?? throw new ArgumentNullException(nameof(save));
            chapterWorld = world ?? throw new ArgumentNullException(nameof(world));
            chapterWorld.BindNarrativeState(narrativeState);
        }

        public bool EnterChapter(string chapterId)
        {
            if (!IsReady() || string.IsNullOrWhiteSpace(chapterId))
            {
                return false;
            }

            var prospective = NarrativeState.FromJson(narrativeState.State.ToJson());
            foreach (var knownChapterId in chapterWorld.ChapterIds)
            {
                if (!string.IsNullOrWhiteSpace(knownChapterId))
                {
                    prospective.Set(ChapterFact(knownChapterId), false);
                }
            }

            prospective.Set(ChapterFact(chapterId), true);
            if (!chapterWorld.CanApply(chapterId, prospective) || !saveGame.Save(prospective))
            {
                return false;
            }

            foreach (var knownChapterId in chapterWorld.ChapterIds)
            {
                if (!string.IsNullOrWhiteSpace(knownChapterId))
                {
                    narrativeState.Set(ChapterFact(knownChapterId), false);
                }
            }

            narrativeState.Set(ChapterFact(chapterId), true);
            CurrentChapterId = chapterId;
            chapterWorld.Apply(chapterId, narrativeState.State);
            RespawnJamie();
            PlayFinaleIntroductionIfNeeded(chapterId);
            ChapterEntered?.Invoke(chapterId);
            return true;
        }

        public bool RestoreCurrentChapter()
        {
            if (!IsReady())
            {
                return false;
            }

            foreach (var chapterId in chapterWorld.ChapterIds)
            {
                if (!string.IsNullOrWhiteSpace(chapterId) && narrativeState.Has(ChapterFact(chapterId)) &&
                    chapterWorld.Apply(chapterId, narrativeState.State))
                {
                    CurrentChapterId = chapterId;
                    RespawnJamie();
                    PlayFinaleIntroductionIfNeeded(chapterId);
                    ChapterEntered?.Invoke(chapterId);
                    return true;
                }
            }

            return false;
        }

        public static string ChapterFact(string chapterId) => CurrentChapterPrefix + chapterId;

        private void PlayFinaleIntroductionIfNeeded(string chapterId)
        {
            if (chapterId == "prologue" && !narrativeState.Has("cinematic_opening_complete"))
            {
                GameBootstrap.Instance?.PlayCinematic("opening");
            }
            else if (chapterId == "finale" && !narrativeState.Has("cinematic_finale_complete"))
            {
                GameBootstrap.Instance?.PlayCinematic("finale");
            }
        }

        private void RespawnJamie()
        {
            var locations = FindFirstObjectByType<LocationTransitionController>();
            if (locations != null && locations.CurrentLocationId != "exterior")
            {
                // Chapter spawn points are authored in Greybridge, outside the
                // isolated interior roots and their movement bounds.
                locations.SetInitial("exterior");
            }

            var spawn = GameObject.Find(chapterWorld.CurrentSpawnPointId);
            var player = GameObject.Find("Jamie");
            if (spawn != null && player != null)
            {
                player.transform.position = spawn.transform.position;
                Physics2D.SyncTransforms();
                Camera.main?.GetComponent<Northbound.Player.FollowCamera>()?.SnapTo(spawn.transform.position);
            }
        }

        private bool IsReady() => narrativeState != null && saveGame != null && chapterWorld != null;
    }
}
