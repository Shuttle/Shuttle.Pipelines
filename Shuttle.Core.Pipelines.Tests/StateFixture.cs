using System;
using NUnit.Framework;

namespace Shuttle.Core.Pipelines.Tests;

public class StateFixture
{
    [Test]
    public void Should_be_able_to_clear_state()
    {
        var item = new Item();

        var state = new State();

        state.Add("key", "value-1");
        state.Add(item);

        Assert.That(state.Contains("key"), Is.True);
        Assert.That(state.Get("key"), Is.EqualTo("value-1"));
        Assert.That(state.Get<string>("key"), Is.EqualTo("value-1"));

        Assert.That(state.Contains<Item>(), Is.True);
        Assert.That(state.Get<Item>(), Is.Not.Null);
        Assert.That(state.Get<Item>()!.Id, Is.EqualTo(item.Id));

        state.Clear();

        Assert.That(state.Contains("key"), Is.False);
        Assert.That(state.Get("key"), Is.EqualTo(null));
        Assert.That(state.Get<string>("key"), Is.EqualTo(null));

        Assert.That(state.Contains<Item>(), Is.False);
        Assert.That(state.Get<Item>(), Is.Null);
    }

    [Test]
    public void Should_be_able_to_manage_state_using_a_given_key()
    {
        var state = new State();

        state.Add("key", "value-1");

        Assert.That(state.Contains("key"), Is.True);
        Assert.That(state.Get("key"), Is.EqualTo("value-1"));
        Assert.That(state.Get<string>("key"), Is.EqualTo("value-1"));

        state.Replace("key", "value-2");

        Assert.That(state.Contains("key"), Is.True);
        Assert.That(state.Get("key"), Is.EqualTo("value-2"));

        state.Remove("key");

        Assert.That(state.Contains("key"), Is.False);
        Assert.That(state.Get("key"), Is.EqualTo(null));
        Assert.That(state.Get<string>("key"), Is.EqualTo(null));
    }

    [Test]
    public void Should_be_able_to_manage_state_using_a_type()
    {
        var itemA = new Item();
        var itemB = new Item();

        var state = new State();

        state.Add(itemA);

        Assert.That(state.Contains<Item>(), Is.True);
        Assert.That(state.Get<Item>(), Is.Not.Null);
        Assert.That(state.Get<Item>()!.Id, Is.EqualTo(itemA.Id));

        state.Replace(itemB);

        Assert.That(state.Contains<Item>(), Is.True);
        Assert.That(state.Get<Item>(), Is.Not.Null);
        Assert.That(state.Get<Item>()!.Id, Is.EqualTo(itemB.Id));

        state.Remove<Item>();

        Assert.That(state.Contains<Item>(), Is.False);
        Assert.That(state.Get<Item>(), Is.Null);
    }

    private class Item
    {
        public Guid Id { get; } = Guid.NewGuid();
    }
}