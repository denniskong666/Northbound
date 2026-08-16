using System.Collections;
using Northbound.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Northbound.Tests
{
    public sealed class InteractionFeedbackTests
    {
        [UnityTest]
        public IEnumerator Show_UpdatesReadableToastAndPlaysProceduralTone()
        {
            var service = InteractionFeedbackService.Create(null);

            service.Show("Missing socket collected", FeedbackKind.Success);
            yield return null;

            Assert.That(service.VisibleMessage, Is.EqualTo("Missing socket collected"));
            Assert.That(service.LastKind, Is.EqualTo(FeedbackKind.Success));
            var label = service.GetComponentInChildren<Text>(true);
            Assert.That(label.text, Is.EqualTo("Missing socket collected"));
            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(34));
            var audio = service.GetComponent<AudioSource>();
            Assert.That(audio.clip, Is.Not.Null);
            Assert.That(audio.clip.samples, Is.GreaterThan(1000));
            Object.Destroy(service.gameObject);
        }
    }
}
