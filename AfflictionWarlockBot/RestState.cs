using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace AfflictionWarlockBot
{
    class RestState : RestStateBase
    {
        const string ConsumeShadows = "Consume Shadows";
        const string HealthFunnel = "Health Funnel";

        LocalPet pet;

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
            : base(botStates, container)
        {
            player.SetTarget(player.Guid);
        }

        public override void Update()
        {
            pet = ObjectManager.Pet;

            if (pet != null && pet.HealthPercent < 60 && pet.CanUse(ConsumeShadows) && !pet.IsCasting && !pet.IsChanneling)
                pet.Cast(ConsumeShadows);

            if (InCombat || (HealthOk && ManaOk))
            {
                if (!player.IsCasting && !player.IsChanneling)
                    player.Stand();

                if (InCombat || PetHealthOk)
                {
                    pet?.FollowPlayer();
                    botStates.Pop();

                    if (!TryRunRestockErrands(12, 28))
                        botStates.Push(new SummonVoidwalkerState(botStates));
                }
                else
                {
                    if (!player.IsChanneling && !player.IsCasting && player.KnowsSpell(HealthFunnel) && player.HealthPercent > 30)
                        player.LuaCall($"CastSpellByName('{HealthFunnel}')");
                }

                return;
            }

            TryEat(80, delayMs: 500);
            TryDrink(60, delayMs: 500);
        }

        bool HealthOk => foodItem == null || player.HealthPercent >= 90 || (player.HealthPercent >= 70 && !player.IsEating);

        bool PetHealthOk => ObjectManager.Pet == null || ObjectManager.Pet.HealthPercent >= 80;

        bool ManaOk => (player.Level < 6 && player.ManaPercent > 50) || player.ManaPercent >= 90 || (player.ManaPercent >= 55 && !player.IsDrinking);

        protected override bool InCombat =>
            ObjectManager.Player.IsInCombat ||
            ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid || u.TargetGuid == ObjectManager.Pet?.Guid);
    }
}
