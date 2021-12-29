﻿using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using static Populations.PopulationManager;

namespace Populations.Models
{
    public class ProsperityModel : DefaultSettlementProsperityModel
    {
		private static readonly float STABILITY_FACTOR = 5f;
		public override ExplainedNumber CalculateHearthChange(Village village, bool includeDescriptions = false)
        {
            ExplainedNumber baseResult = base.CalculateHearthChange(village);
            if (PopulationConfig.Instance.PopulationManager != null && PopulationConfig.Instance.PopulationManager.IsSettlementPopulated(village.Settlement))
                new GrowthModel().CalculateHearthGrowth(village, ref baseResult);

            return baseResult;
        }

        public override ExplainedNumber CalculateProsperityChange(Town fortification, bool includeDescriptions = false)
        {
            ExplainedNumber baseResult = base.CalculateProsperityChange(fortification, includeDescriptions);
            if (PopulationConfig.Instance.PopulationManager != null && PopulationConfig.Instance.PopulationManager.IsSettlementPopulated(fortification.Settlement))
            {
				ExplainedNumber explainedNumber = new ExplainedNumber(0f, true);
				PopulationData data = PopulationConfig.Instance.PopulationManager.GetPopData(fortification.Settlement);
                int craftsmen = data.GetTypeCount(PopType.Craftsmen);
                baseResult.Add((float)craftsmen * 0.0005f, new TextObject("Craftsmen output"));
                int slaves = data.GetTypeCount(PopType.Slaves);
                baseResult.Add((float)slaves * -0.0001f, new TextObject("Slave population"));

                if (PopulationConfig.Instance.PopulationManager.PopSurplusExists(fortification.Settlement, PopType.Slaves, true))
                    baseResult.Add((float)slaves * -0.0003f, new TextObject("Slave surplus"));
                
                if (PopulationConfig.Instance.PolicyManager.IsPolicyEnacted(fortification.Settlement, PolicyManager.PolicyType.SELF_INVEST)) 
                {
                    ExplainedNumber income = new TaxModel().CalculateTownTax(fortification);
                    float tax = income.ResultNumber;
                    if (tax > 0)
                        baseResult.Add(tax * 0.0005f, new TextObject("Self-investment policy"));
                }

				float factor = data.Stability - 1f + data.Stability;
				float stabilityImpact = (float)STABILITY_FACTOR * factor;

				int foodLimitForBonus = (int)((float)fortification.FoodStocksUpperLimit() * 0.8f);
				if (fortification.FoodStocks >= foodLimitForBonus)
					explainedNumber.Add(0.5f, new TextObject("Well fed populace"));
				else if (fortification.Settlement.IsStarving)
                {
					float starvation = stabilityImpact;
					if (starvation > 0f)
						starvation *= -0.5f;
					if (stabilityImpact <= 0f && stabilityImpact > -1f)
						starvation = -1f;

					explainedNumber.Add(starvation, FoodShortageText);
				}

				float houseCost = fortification.Prosperity < 1500f ? fortification.Prosperity / 250f - 1f : fortification.Prosperity >= 6000f 
					? 2f - (fortification.Prosperity / 3000f) : 0f;
				explainedNumber.Add(6f - houseCost, HousingCostsText, null);

				if (fortification.IsTown)
				{
					int num3 = fortification.SoldItems.Sum(delegate (Town.SellLog x)
					{
						if (x.Category.Properties != ItemCategory.Property.BonusToProsperity)
						{
							return 0;
						}
						return x.Number;
					});
					if (num3 > 0)
					{
						explainedNumber.Add((float)num3 * 0.1f, ProsperityFromMarketText, null);
					}
				}
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Medicine.PristineStreets, fortification, ref explainedNumber);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Riding.Veterinary, fortification, ref explainedNumber);
				if (PerkHelper.GetPerkValueForTown(DefaultPerks.Engineering.Apprenticeship, fortification))
				{
					float num4 = 0f;
					foreach (Building building in from x in fortification.Buildings
												  where !x.BuildingType.IsDefaultProject && x.CurrentLevel > 0
												  select x)
					{
						num4 += DefaultPerks.Engineering.Apprenticeship.SecondaryBonus;
					}
					if (num4 > 0f && explainedNumber.ResultNumber > 0f)
					{
						explainedNumber.AddFactor(num4, DefaultPerks.Engineering.Apprenticeship.Name);
					}
				}
				if (fortification.BuildingsInProgress.IsEmpty<Building>())
					BuildingHelper.AddDefaultDailyBonus(fortification, BuildingEffectEnum.ProsperityDaily, ref explainedNumber);
				
				foreach (Building building2 in fortification.Buildings)
				{
					float buildingEffectAmount = building2.GetBuildingEffectAmount(BuildingEffectEnum.Prosperity);
					if (!building2.BuildingType.IsDefaultProject && buildingEffectAmount > 0f)
						explainedNumber.Add(buildingEffectAmount, building2.Name, null);
					
					if (building2.BuildingType == DefaultBuildingTypes.SettlementAquaducts || building2.BuildingType == DefaultBuildingTypes.CastleGranary || 
						building2.BuildingType == DefaultBuildingTypes.SettlementGranary)
						PerkHelper.AddPerkBonusForTown(DefaultPerks.Medicine.CleanInfrastructure, fortification, ref explainedNumber);
				}
				
				if (fortification.IsTown && !fortification.CurrentBuilding.IsCurrentlyDefault && fortification.Governor != null && fortification.Governor.GetPerkValue(DefaultPerks.Trade.TrickleDown))
					explainedNumber.Add(DefaultPerks.Trade.TrickleDown.SecondaryBonus, DefaultPerks.Trade.TrickleDown.Name, null);
				
				if (fortification.Settlement.OwnerClan.Kingdom != null)
				{
					if (fortification.Settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.RoadTolls))
						explainedNumber.Add(-0.2f, DefaultPolicies.RoadTolls.Name, null);
					
					if (fortification.Settlement.OwnerClan.Kingdom.RulingClan == fortification.Settlement.OwnerClan && fortification.Settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.ImperialTowns))
						explainedNumber.Add(1f, DefaultPolicies.ImperialTowns.Name, null);
					
					if (fortification.Settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.CrownDuty))
						explainedNumber.Add(-1f, DefaultPolicies.CrownDuty.Name, null);
					
					if (fortification.Settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.WarTax))
						explainedNumber.Add(-1f, DefaultPolicies.WarTax.Name, null);
					
				}
				this.GetSettlementProsperityChangeDueToIssues(fortification.Settlement, ref explainedNumber);
			}
            return baseResult;
        }

		private void GetSettlementProsperityChangeDueToIssues(Settlement settlement, ref ExplainedNumber result)
		{
			Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementProsperity, settlement, ref result);
		}

		private static readonly TextObject LoyaltyText = GameTexts.FindText("str_loyalty", null);
		private static readonly TextObject FoodShortageText = new TextObject("{=qTFKvGSg}Food Shortage", null);
		private static readonly TextObject ProsperityFromMarketText = new TextObject("{=RNT5hMVb}Goods From Market", null);
		private static readonly TextObject SurplusFoodText = GameTexts.FindText("str_surplus_food", null);
		private static readonly TextObject EmpireProsperityBonus = new TextObject("{=3Ditaq1M}Empire Prosperity Bonus", null);
		private static readonly TextObject Governor = new TextObject("{=Fa2nKXxI}Governor", null);
		private static readonly TextObject NewBornText = new TextObject("{=RVas571P}New Born", null);
		private static readonly TextObject RaidedText = new TextObject("{=RVas572P}Raided", null);
		private static readonly TextObject HousingCostsText = new TextObject("{=ByRAgJy4}Housing Costs", null);
	}
}
