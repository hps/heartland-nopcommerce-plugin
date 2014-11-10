using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.SecureSubmit.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        public int TransactModeId { get; set; }
        public bool TransactModeId_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.SecureSubmit.Fields.TransactModeValues")]
        public SelectList TransactModeValues { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecureSubmit.Fields.PublicApiKey")]
        public string PublicApiKey { get; set; }
        public bool PublicApiKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecureSubmit.Fields.SecretApiKey")]
        public string SecretApiKey { get; set; }
        public bool SecretApiKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecureSubmit.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecureSubmit.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }
    }
}