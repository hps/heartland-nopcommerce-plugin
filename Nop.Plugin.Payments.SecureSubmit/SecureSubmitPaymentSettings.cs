using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.SecureSubmit
{
    public class SecureSubmitPaymentSettings : ISettings
    {
        public TransactMode TransactMode { get; set; }
        public string PublicApiKey { get; set; }
        public string SecretApiKey { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
    }
}
