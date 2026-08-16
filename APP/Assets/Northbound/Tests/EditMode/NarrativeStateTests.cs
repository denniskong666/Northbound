using NUnit.Framework;
using Northbound.Narrative;

public sealed class NarrativeStateTests
{
    [Test]
    public void FactsAndCounters_RoundTrip()
    {
        var state = new NarrativeState();
        state.Set("attended_maya_exhibition", true);
        state.Add("bond_maya", 2);

        var loaded = NarrativeState.FromJson(state.ToJson());

        Assert.That(loaded.Has("attended_maya_exhibition"), Is.True);
        Assert.That(loaded.GetInt("bond_maya"), Is.EqualTo(2));
    }

    [Test]
    public void NewState_ReturnsDefaultValuesForAbsentIds()
    {
        var state = new NarrativeState();

        Assert.That(state.Has("helped_noah"), Is.False);
        Assert.That(state.GetInt("bond_noah"), Is.EqualTo(0));
    }

    [Test]
    public void SetRepeatedTrueThenFalse_RemovesTheFact()
    {
        var state = new NarrativeState();

        state.Set("jamie_uncertain", true);
        state.Set("jamie_uncertain", true);
        state.Set("jamie_uncertain", false);

        Assert.That(state.Has("jamie_uncertain"), Is.False);
    }

    [Test]
    public void FromJson_CorruptOrEmptyJsonReturnsNewState()
    {
        Assert.That(NarrativeState.FromJson("{ not-json").Has("attended_maya_exhibition"), Is.False);
        Assert.That(NarrativeState.FromJson(string.Empty).GetInt("bond_maya"), Is.EqualTo(0));
    }

    [Test]
    public void Store_EmitsOnlyForMutationsAndReset()
    {
        var store = new NarrativeStateStore(new NarrativeState());
        var changeCount = 0;
        store.Changed += () => changeCount++;

        store.Set("helped_noah", true);
        store.Set("helped_noah", true);
        store.Add("bond_noah", 3);
        store.Reset();

        Assert.That(changeCount, Is.EqualTo(3));
        Assert.That(store.Has("helped_noah"), Is.False);
        Assert.That(store.GetInt("bond_noah"), Is.EqualTo(0));
    }

    [Test]
    public void Store_SetWithInvalidIdDoesNotChangeOrNotify()
    {
        var store = new NarrativeStateStore(new NarrativeState());
        var changeCount = 0;
        store.Changed += () => changeCount++;

        store.Set(null, true);
        store.Set(string.Empty, true);

        Assert.That(changeCount, Is.EqualTo(0));
        Assert.That(store.Has("helped_noah"), Is.False);
    }
}
