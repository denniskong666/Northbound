using Northbound.Dialogue;
using NUnit.Framework;

namespace Northbound.Tests
{
    public sealed class DialoguePresentationCatalogTests
    {
        [Test]
        public void ExplicitNarration_HidesCharacterPresentation()
        {
            var line = new DialogueLine
            {
                speakerId = "Jamie",
                presentation = DialoguePresentation.Narration,
                text = "The street is quiet."
            };

            Assert.That(DialoguePresentationCatalog.IsNarration("new_dialogue", 0, line), Is.True);
        }

        [Test]
        public void LegacyNarrationCatalog_CoversStoryCallbackWithoutChangingCharacterLines()
        {
            Assert.That(DialoguePresentationCatalog.IsNarration(
                "one_more_table_dialogue", 3, new DialogueLine { speakerId = "Jamie" }), Is.True);
            Assert.That(DialoguePresentationCatalog.IsNarration(
                "one_more_table_dialogue", 0, new DialogueLine { speakerId = "Jamie" }), Is.False);
        }

        [Test]
        public void ThirdPersonJamieChoicePrompt_IsNarrationInsteadOfNamedSpeech()
        {
            Assert.That(DialoguePresentationCatalog.IsNarration(
                "optional_leo_diner", 2, new DialogueLine
                {
                    speakerId = "Jamie",
                    text = "How should Jamie answer Leo at the diner?"
                }), Is.True);
        }

        [Test]
        public void NarratorSpeaker_IsNarrationEvenWithoutAnExplicitPresentationField()
        {
            Assert.That(DialoguePresentationCatalog.IsNarration(
                "new_dialogue", 0, new DialogueLine { speakerId = "Narrator" }), Is.True);
        }
    }
}
