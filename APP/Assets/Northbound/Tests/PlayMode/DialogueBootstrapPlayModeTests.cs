using System.Collections;
using Northbound.Core;
using Northbound.Dialogue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Collections.Generic;
using Northbound.UI;
using Northbound.Quests;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Northbound.Tests
{
    public sealed class DialogueBootstrapPlayModeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_PersistsCanvasDialogueViewAndMouseEventSystemAfterSceneLoad()
        {
            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var view = Object.FindFirstObjectByType<DialogueView>();
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();

            Assert.That(view, Is.Not.Null);
            Assert.That(view.GetComponent<Canvas>(), Is.Not.Null);
            Assert.That(view.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(eventSystem.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }

        [UnityTest]
        public IEnumerator DialogueView_TypesBeforeShowingOrderedInlineChoicesAndRemembersKeyboardSelection()
        {
            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Single);
            yield return null;
            yield return null;
            var view = Object.FindFirstObjectByType<DialogueView>();
            var branching = ScriptableObject.CreateInstance<DialogueAsset>();
            branching.id = "ui_branch_test";
            branching.lines.Add(new DialogueLine
            {
                speakerId = "Maya", text = "Which version do you want?", textChinese = "你想要哪一种答案？", choices = new List<DialogueChoice>
                {
                    new DialogueChoice { text = "Tell me the truth.", textChinese = "告诉我真相。", nextLineIndex = 1 },
                    new DialogueChoice { text = "Give me a minute.", textChinese = "给我一点时间。", nextLineIndex = 2 }
                }
            });
            branching.lines.Add(new DialogueLine { speakerId = "Maya", text = "Then listen.", textChinese = "那就听好。" });
            branching.lines.Add(new DialogueLine { speakerId = "Maya", text = "Take the minute.", textChinese = "慢慢想。" });
            GameText.Use(GameLanguage.English);
            GameBootstrap.Instance.Dialogue.Start(branching);
            yield return null;

            Assert.That(view.transform.Find("Panel/Speaker").GetComponent<Text>().text, Is.EqualTo("MAYA"));
            Assert.That(view.IsTyping, Is.True);
            Assert.That(view.transform.Find("Panel/Choice 1").gameObject.activeSelf, Is.False,
                "Web-style choices must wait until the current sentence has finished typing.");

            view.RevealCurrentLine();
            var first = view.transform.Find("Panel/Choice 1").GetComponent<RectTransform>();
            var second = view.transform.Find("Panel/Choice 2").GetComponent<RectTransform>();
            var dialogue = view.transform.Find("Panel/Dialogue Text").GetComponent<RectTransform>();
            Assert.That(first.gameObject.activeSelf, Is.True);
            Assert.That(first.GetComponentInChildren<Text>().text, Does.StartWith("\u25B6"));
            Assert.That(first.GetComponentInChildren<Text>().text, Does.Not.StartWith("1."));
            Assert.That(first.anchoredPosition.y, Is.GreaterThan(second.anchoredPosition.y),
                "Choice one must render above choice two instead of reversing the list.");
            Assert.That(first.GetComponent<Image>().color.a, Is.EqualTo(0f),
                "Normal dialogue choices are inline text, not blue button bars.");
            AssertNoVerticalOverlap(dialogue, first, "Dialogue body and first choice");

            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow));
                InputSystem.Update();
                Assert.That(Keyboard.current, Is.SameAs(keyboard));
                Assert.That(view.SelectedChoiceIndex, Is.EqualTo(1));
                Assert.That(second.GetComponentInChildren<Text>().text, Does.StartWith("\u25B6"));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                InputSystem.Update();
                Assert.That(GameBootstrap.Instance.Dialogue.Current.text, Is.EqualTo("Take the minute."));
                Assert.That(view.IsTyping, Is.True);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }

            view.RevealCurrentLine();
            var continueIndicator = view.transform.Find("Panel/Continue Indicator");
            Assert.That(continueIndicator.gameObject.activeSelf, Is.True);
            Assert.That(continueIndicator.GetComponent<Text>().text, Does.Contain("ENTER"));
            Object.Destroy(branching);
        }

        [UnityTest]
        public IEnumerator DialogueView_UsesChineseStoryTextAndPanelClickOnlyRevealsBeforeAdvancing()
        {
            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Single);
            yield return null;
            yield return null;
            var view = Object.FindFirstObjectByType<DialogueView>();
            var dialogue = ScriptableObject.CreateInstance<DialogueAsset>();
            dialogue.id = "ui_chinese_test";
            dialogue.lines.Add(new DialogueLine
            {
                speakerId = "Maya",
                text = "The old arrow is still under the paint.",
                textChinese = "旧的箭头还藏在颜料下面。"
            });
            dialogue.lines.Add(new DialogueLine
            {
                speakerId = "Jamie",
                text = "I can still see it.",
                textChinese = "我还看得见它。"
            });
            GameText.Use(GameLanguage.SimplifiedChinese);
            GameBootstrap.Instance.Dialogue.Start(dialogue);
            yield return null;

            Assert.That(view.transform.Find("Panel/Speaker").GetComponent<Text>().text, Is.EqualTo("玛雅"));
            Assert.That(view.IsTyping, Is.True);
            view.OnPointerClick(new PointerEventData(EventSystem.current));
            Assert.That(view.transform.Find("Panel/Dialogue Text").GetComponent<Text>().text,
                Is.EqualTo("旧的箭头还藏在颜料下面。"));
            Assert.That(GameBootstrap.Instance.Dialogue.Current.speakerId, Is.EqualTo("Maya"),
                "The first panel click must reveal, not advance.");
            view.OnPointerClick(new PointerEventData(EventSystem.current));
            Assert.That(GameBootstrap.Instance.Dialogue.Current.speakerId, Is.EqualTo("Jamie"));

            GameText.Use(GameLanguage.English);
            Object.Destroy(dialogue);
        }

        [UnityTest]
        public IEnumerator DialogueView_SwitchesBetweenCompactNarrationAndCharacterPanel()
        {
            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var view = Object.FindFirstObjectByType<DialogueView>();
            var dialogue = ScriptableObject.CreateInstance<DialogueAsset>();
            dialogue.id = "ui_presentation_test";
            dialogue.lines.Add(new DialogueLine
            {
                speakerId = "Jamie",
                presentation = DialoguePresentation.Narration,
                text = "The street waits below.",
                textChinese = "街道在楼下等着。"
            });
            dialogue.lines.Add(new DialogueLine
            {
                speakerId = "Maya",
                text = "You made it.",
                textChinese = "你来了。"
            });

            GameText.Use(GameLanguage.SimplifiedChinese);
            GameBootstrap.Instance.Dialogue.Start(dialogue);
            yield return null;
            view.RevealCurrentLine();

            var panel = view.transform.Find("Panel").GetComponent<RectTransform>();
            var speaker = view.transform.Find("Panel/Speaker").gameObject;
            Assert.That(view.IsShowingNarration, Is.True);
            Assert.That(speaker.activeSelf, Is.False);
            Assert.That(panel.rect.height, Is.EqualTo(176f).Within(1f));
            Assert.That(view.transform.Find("Panel/Dialogue Text").GetComponent<Text>().text, Is.EqualTo("街道在楼下等着。"));

            GameBootstrap.Instance.Dialogue.Advance();
            yield return null;
            view.RevealCurrentLine();
            Assert.That(view.IsShowingNarration, Is.False);
            Assert.That(speaker.activeSelf, Is.True);
            Assert.That(speaker.GetComponent<Text>().text, Is.EqualTo("玛雅"));
            Assert.That(panel.rect.height, Is.EqualTo(400f).Within(1f));

            GameText.Use(GameLanguage.English);
            GameBootstrap.Instance.Dialogue.Stop();
            Object.Destroy(dialogue);
        }

        [UnityTest]
        public IEnumerator MissionPairConfirmation_UsesNarrationAndLocalizedCommitChoice()
        {
            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var bootstrap = GameBootstrap.Instance;
            var view = Object.FindFirstObjectByType<DialogueView>();
            const string selectedQuestId = "ui_commitment_route_a";
            var pair = new MissionPairController(selectedQuestId, "ui_commitment_route_b", bootstrap.NarrativeState, null, bootstrap.Dialogue);
            GameText.Use(GameLanguage.SimplifiedChinese);

            Assert.That(pair.BeginCommitment(selectedQuestId), Is.True);
            yield return null;
            view.RevealCurrentLine();

            Assert.That(bootstrap.Dialogue.Current.textChinese, Is.EqualTo("这项任务会占用今晚剩下的时间。"));
            Assert.That(view.IsShowingNarration, Is.True);
            Assert.That(view.transform.Find("Panel/Choice 1").GetComponentInChildren<Text>().text, Does.Contain("确认投入"));
            bootstrap.Dialogue.Choose(0);
            Assert.That(pair.CommittedQuestId, Is.EqualTo(selectedQuestId));

            GameText.Use(GameLanguage.English);
            bootstrap.Dialogue.Stop();
        }

        private static void AssertNoVerticalOverlap(RectTransform upper, RectTransform lower, string label)
        {
            var upperCorners = new Vector3[4];
            var lowerCorners = new Vector3[4];
            upper.GetWorldCorners(upperCorners);
            lower.GetWorldCorners(lowerCorners);
            Assert.That(upperCorners[0].y, Is.GreaterThanOrEqualTo(lowerCorners[1].y), label);
        }
    }
}
