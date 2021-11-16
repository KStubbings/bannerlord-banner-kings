﻿using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace Populations
{
    public static class PolicyManager
    {
        private static readonly Dictionary<Settlement, List<PolicyElement>> POLICIES = new Dictionary<Settlement, List<PolicyElement>>();
        private static readonly Dictionary<Settlement, TaxType> TAXES = new Dictionary<Settlement, TaxType>();
        public static List<PolicyElement> GetSettlementPolicies(Settlement settlement)
        {
            List<PolicyElement> policies = new List<PolicyElement>();
            if (!POLICIES.ContainsKey(settlement))
            {
                policies.Add(new PolicyElement("Allow slaves to be exported", "Slave caravans will be formed when slave population is big", true, PolicyType.EXPORT_SLAVES));
                policies.Add(new PolicyElement("Accelerate population growth", "Population will grow faster at the cost of influence", false, PolicyType.POP_GROWTH));
                policies.Add(new PolicyElement("Allow settlement to self-invest", "Income generated by the settlement will be reverted back into it's growth", false, PolicyType.SELF_INVEST));
                policies.Add(new PolicyElement("Conscript the lowmen", "Extensive recruitment will draft serfs into the militia, costing gold and reducing the productive workforce", false, PolicyType.CONSCRIPTION));
                policies.Add(new PolicyElement("Subsidize the militia", "Improve militia quality by subsidizing their equipment and trainning", false, PolicyType.SUBSIDIZE_MILITIA));
                policies.Add(new PolicyElement("Exempt nobles from taxes", "Exempt nobles from taxes, making them vouch in your favor", false, PolicyType.EXEMPTION));
                POLICIES.Add(settlement, policies);
            }
            else policies = POLICIES[settlement];

            return policies;
        }

        public static TaxType GetSettlementTax(Settlement settlement)
        {
            if (TAXES.ContainsKey(settlement))
                return TAXES[settlement];
            else
            {
                TAXES.Add(settlement, TaxType.STANDARD);
                return TaxType.STANDARD;
            }
        }

        public static bool IsPolicyEnacted(Settlement settlement, PolicyType policy)
        {
            PolicyElement element = null;
            if (POLICIES.ContainsKey(settlement))
                element = POLICIES[settlement].Find(x => x.type == policy);
            return element != null ? element.isChecked : false;
        }

        public static void UpdatePolicy(Settlement settlement, PolicyType policy, bool value)
        {
            PolicyElement element = POLICIES[settlement].Find(x => x.type == policy);
            if (element != null)
                element.isChecked = value;
        }

        public class PolicyElement
        {
            public string description, hint;
            public bool isChecked;
            public PolicyType type;

            public PolicyElement(string description, string hint, bool isChecked, PolicyType type)
            {
                this.description = description;
                this.hint = hint;
                this.isChecked = isChecked;
                this.type = type;
            }
        }

        public enum PolicyType
        {
            EXPORT_SLAVES,
            POP_GROWTH,
            SELF_INVEST,
            CONSCRIPTION,
            EXEMPTION,
            SUBSIDIZE_MILITIA
        }

        public enum TaxType
        {
            HIGH,
            STANDARD,
            LOW
        }
    }
}
