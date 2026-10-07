using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace StarterProject.Tests
{
    public sealed class GameStateTests
    {
        [Test]
        public void MenuGameplayAndPauseFollowTheContractAndNotifyOnce()
        {
            var state = new GameStateController();
            var changes = new List<(GameState, GameState)>();
            state.StateChanged += (previous, current) =>
            {
                Assert.That(state.Current, Is.EqualTo(current));
                changes.Add((previous, current));
            };
            var path = new[] { GameState.Boot, GameState.Loading, GameState.Menu, GameState.Loading,
                GameState.Gameplay, GameState.Pause, GameState.Gameplay, GameState.Loading, GameState.Menu };
            for (var i = 1; i < path.Length; i++)
            {
                Assert.That(state.CanTransitionTo(path[i]), Is.True);
                Assert.That(state.Current, Is.EqualTo(path[i - 1]), "Querying must not mutate state.");
                Assert.That(state.TryTransitionTo(path[i]), Is.True);
                Assert.That(changes[i - 1], Is.EqualTo((path[i - 1], path[i])));
            }
            Assert.That(changes.Count, Is.EqualTo(path.Length - 1));
        }

        [TestCase(GameState.Menu)]
        [TestCase(GameState.Gameplay)]
        public void StartupAlreadyInDestinationCanPublishItWithoutLoading(GameState destination)
        {
            var state = new GameStateController();
            Assert.That(state.TryTransitionTo(destination), Is.True);
            Assert.That(state.Current, Is.EqualTo(destination));
        }

        [Test]
        public void RejectedTransitionsDoNotMutateOrNotify()
        {
            var state = new GameStateController();
            Assert.That(state.TryTransitionTo(GameState.Menu), Is.True);
            var notifications = 0;
            state.StateChanged += (_, _) => notifications++;
            foreach (var invalid in new[] { GameState.Boot, GameState.Menu, GameState.Gameplay,
                GameState.Pause, (GameState)(-1), (GameState)999 })
            {
                Assert.That(state.CanTransitionTo(invalid), Is.False);
                Assert.That(state.TryTransitionTo(invalid), Is.False);
                Assert.That(state.Current, Is.EqualTo(GameState.Menu));
            }
            Assert.That(notifications, Is.Zero);
        }

        [TestCase(GameState.Boot)]
        [TestCase(GameState.Menu)]
        [TestCase(GameState.Loading)]
        [TestCase(GameState.Gameplay)]
        [TestCase(GameState.Pause)]
        public void FailureIsReachableAndTerminal(GameState source)
        {
            var state = new GameStateController();
            if (source == GameState.Pause)
            {
                state.TryTransitionTo(GameState.Gameplay);
                state.TryTransitionTo(GameState.Pause);
            }
            else if (source != GameState.Boot) state.TryTransitionTo(source);
            Assert.That(state.Current, Is.EqualTo(source));
            Assert.That(state.TryTransitionTo(GameState.Failed), Is.True);
            foreach (GameState destination in Enum.GetValues(typeof(GameState)))
                Assert.That(state.TryTransitionTo(destination), Is.False);
            Assert.That(state.Current, Is.EqualTo(GameState.Failed));
        }

        [Test]
        public void ObserverCannotReenterAnOrdinaryTransition()
        {
            var state = new GameStateController();
            var reentered = true;
            state.StateChanged += (_, _) => reentered = state.TryTransitionTo(GameState.Menu);
            Assert.That(state.TryTransitionTo(GameState.Loading), Is.True);
            Assert.That(reentered, Is.False);
            Assert.That(state.Current, Is.EqualTo(GameState.Loading));
            Assert.That(state.TryTransitionTo(GameState.Menu), Is.True);
        }

        [Test]
        public void FailureDuringNotificationStillBlocksFurtherTransitions()
        {
            var state = new GameStateController();
            state.StateChanged += (_, current) =>
            {
                if (current == GameState.Loading)
                    Assert.That(state.TryTransitionTo(GameState.Failed), Is.True);
            };
            state.TryTransitionTo(GameState.Loading);
            Assert.That(state.Current, Is.EqualTo(GameState.Failed));
            Assert.That(state.TryTransitionTo(GameState.Menu), Is.False);
        }
    }
}