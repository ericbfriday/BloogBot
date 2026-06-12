using BloogBot;
using BloogBot.AI;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace BloodDeathKnightBot
{
    [Export(typeof(IBot))]
    class BloodDeathKnightBot : Bot, IBot
    {
        public string Name => "Blood Death Knight";

        public string FileName => "BloodDeathKnightBot.dll";

        bool AdditionalTargetingCriteria(WoWUnit unit) => true;

        IBotState CreateRestState(Stack<IBotState> botStates, IDependencyContainer container) =>
            new RestState(botStates, container);

        IBotState CreateMoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) =>
            new MoveToTargetState(botStates, container, target);

        IBotState CreatePowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget) =>
            new PowerlevelCombatState(botStates, container, target, powerlevelTarget);

        IBotState CreateCombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot) => new CombatState(botStates, container, target, loot);

        public IDependencyContainer GetDependencyContainer(BotSettings botSettings, Probe probe, IEnumerable<Hotspot> hotspots) =>
            new DependencyContainer(
                AdditionalTargetingCriteria,
                CreateRestState,
                CreateMoveToTargetState,
                CreatePowerlevelCombatState,
                CreateCombatState,
                botSettings,
                probe,
                hotspots);

        public void Test(IDependencyContainer container) { }
    }
}
