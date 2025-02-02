using System;
using System.Linq;
using HarmonyLib;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BannerKings.Behaviours
{
    public class BKLordPropertyBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSettlementEntered(MobileParty party, Settlement target, Hero hero)
        {
            if (party?.LeaderHero == null || !party.IsLordParty)
            {
                return;
            }

            var lord = party.LeaderHero;
            var kingdom = lord.Clan.Kingdom;
            if (lord == Hero.MainHero || kingdom == null || target.OwnerClan == null ||
                target.OwnerClan.Kingdom != kingdom ||
                FactionManager.GetEnemyKingdoms(kingdom).Any())
            {
                return;
            }

            var caravanCost = BannerKingsConfig.Instance.EconomyModel.GetCaravanPrice(target, lord).ResultNumber;
            if (ShouldHaveCaravan(lord, (int) caravanCost))
            {
                lord.ChangeHeroGold(-(int) caravanCost);
                CaravanPartyComponent.CreateCaravanParty(lord, target);
            }

            if (target.IsTown && !target.Town.Workshops.Any(x => x.Owner == lord))
            {
                var random = target.Town.Workshops.GetRandomElement();
                if (random != null)
                {
                    float workshopCost = BannerKingsConfig.Instance.WorkshopModel.GetBuyingCostForPlayer(random);
                    if (ShouldHaveWorkshop(lord, (int) workshopCost))
                    {

                        if (random.Owner == Hero.MainHero)
                        {
                            InformationManager.ShowInquiry(new InquiryData(new TextObject("{=HGHxECuY}Workshop Acquisition").ToString(),
                                new TextObject("{=Q19XEcNq}The {CLAN} proposes to buy your {WORKSHOP} at {TOWN}. They offer you {GOLD}{GOLD_ICON}")
                                .SetTextVariable("CLAN", lord.Clan.Name)
                                .SetTextVariable("WORKSHOP", random.WorkshopType.Name)
                                .SetTextVariable("TOWN", target.Name)
                                .SetTextVariable("GOLD", (int)workshopCost).ToString(),
                                true, 
                                true,
                                GameTexts.FindText("str_accept").ToString(),
                                GameTexts.FindText("str_reject").ToString(),
                                () => BuyWorkshop(random, lord, kingdom, workshopCost),
                                null), 
                                true);
                        }
                        else
                        {
                            BuyWorkshop(random, lord, kingdom, workshopCost);
                        }
                    }
                }
            }
        }

        private void BuyWorkshop(Workshop wk, Hero buyer, Kingdom kingdom, float cost)
        {
            if (kingdom == Clan.PlayerClan.Kingdom)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=gQYsK1AT}The {CLAN} now own {WORKSHOP} at {TOWN}.")
                        .SetTextVariable("CLAN", buyer.Clan.Name)
                        .SetTextVariable("WORKSHOP", wk.Name)
                        .SetTextVariable("TOWN", wk.Settlement.Name)
                        .ToString()));
            }
            ChangeOwnerOfWorkshopAction.ApplyByTrade(wk, buyer, wk.WorkshopType,
                Campaign.Current.Models.WorkshopModel.GetInitialCapital(1), true,
                (int)cost);
        }

        private bool ShouldHaveCaravan(Hero hero, int cost)
        {
            return hero == hero.Clan.Leader && hero.Clan.Gold >= (int) (cost * 2f) &&
                   hero.OwnedCaravans.Count < (int) (hero.Clan.Tier / 3f);
        }

        private bool ShouldHaveWorkshop(Hero hero, int cost)
        {
            return hero == hero.Clan.Leader && hero.Clan.Gold >= (int) (cost * 2f) &&
                   hero.OwnedCaravans.Count < 1 + hero.Clan.Tier;
        }
    }

    namespace Patches
    {
        [HarmonyPatch(typeof(CaravansCampaignBehavior))]
        internal class CaravansCampaignBehaviorPatches
        {
            [HarmonyPrefix]
            [HarmonyPatch("SpawnCaravan", MethodType.Normal)]
            private static bool SpawnCaravan(Hero hero, bool initialSpawn = false)
            {
                return hero.CurrentSettlement is {IsTown: true};
            }
        }

        [HarmonyPatch(typeof(LordConversationsCampaignBehavior))]
        internal class LordConversationsCampaignBehaviorPatches
        {
            [HarmonyPostfix]
            [HarmonyPatch("SmallCaravanFormingCost", MethodType.Getter)]
            private static void SmallCaravanFormingCost(ref int __result)
            {
                __result = (int) BannerKingsConfig.Instance.EconomyModel
                    .GetCaravanPrice(Settlement.CurrentSettlement, Hero.MainHero).ResultNumber;
            }

            [HarmonyPostfix]
            [HarmonyPatch("LargeCaravanFormingCost", MethodType.Getter)]
            private static void LargeCaravanFormingCost(ref int __result)
            {
                __result = (int) BannerKingsConfig.Instance.EconomyModel
                    .GetCaravanPrice(Settlement.CurrentSettlement, Hero.MainHero, true).ResultNumber;
            }
        }
    }
}