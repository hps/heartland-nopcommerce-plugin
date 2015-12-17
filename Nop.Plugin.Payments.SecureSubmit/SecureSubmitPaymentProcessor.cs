using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;

using Nop.Plugin.Payments.SecureSubmit.Controllers;
using SecureSubmit.Services;
using SecureSubmit.Entities;
using SecureSubmit.Infrastructure;

namespace Nop.Plugin.Payments.SecureSubmit
{
    /// <summary>
    /// SecureSubmit payment processor
    /// </summary>
    public class SecureSubmitPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly SecureSubmitPaymentSettings _secureSubmitPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IEncryptionService _encryptionService;

        #endregion

        #region Ctor

        public SecureSubmitPaymentProcessor(SecureSubmitPaymentSettings secureSubmitPaymentSettings,
            ISettingService settingService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            CurrencySettings currencySettings, IWebHelper webHelper,
            IOrderTotalCalculationService orderTotalCalculationService, IEncryptionService encryptionService)
        {
            this._secureSubmitPaymentSettings = secureSubmitPaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._customerService = customerService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._encryptionService = encryptionService;
        }

        #endregion

        #region Methods



        /// <summary>
        /// HidePaymentsMethods
        /// </summary>
        /// <param name="ShoppingCartItems">A Nop.Core iList of objects that are items in the shopping cart</param>
        /// <returns>Process payment result</returns>1
        public bool HidePaymentMethod(IList<Nop.Core.Domain.Orders.ShoppingCartItem> shoppingCartItems)
        {
            return (false);
        }

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            var token = (string)processPaymentRequest.CustomValues["token_value"];

            var config = new HpsServicesConfig();
            config.SecretApiKey = _secureSubmitPaymentSettings.SecretApiKey;
            config.DeveloperId = "002914";
            config.VersionNumber = "1513";

            var creditService = new HpsCreditService(config);
            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);

            var cardHolder = new HpsCardHolder();
            cardHolder.Address = new HpsAddress();
            cardHolder.Address.Address = customer.BillingAddress.Address1;
            cardHolder.Address.City = customer.BillingAddress.City;
            cardHolder.Address.State = customer.BillingAddress.StateProvince.Abbreviation;
            cardHolder.Address.Zip = customer.BillingAddress.ZipPostalCode.Replace("-", "");
            cardHolder.Address.Country = customer.BillingAddress.Country.ThreeLetterIsoCode;

            HpsAuthorization response = null;

            try
            {
                if (_secureSubmitPaymentSettings.TransactMode == TransactMode.Authorize)
                {
                    // auth
                    response = creditService.Authorize(
                        processPaymentRequest.OrderTotal,
                        _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode,
                        token,
                        cardHolder,
                        false);

                    result.NewPaymentStatus = PaymentStatus.Authorized;
                    result.AuthorizationTransactionCode = response.AuthorizationCode;
                    result.AuthorizationTransactionId = response.TransactionId.ToString();
                }
                else
                {
                    //capture
                    response = creditService.Charge(
                        processPaymentRequest.OrderTotal,
                        _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode,
                        token,
                        cardHolder,
                        false);

                    result.NewPaymentStatus = PaymentStatus.Paid;
                    result.CaptureTransactionId = response.TransactionId.ToString();
                    result.CaptureTransactionResult = response.ResponseText;
                }
            }
            catch (HpsException ex)
            {
                result.AddError(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            // we don't use this
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _secureSubmitPaymentSettings.AdditionalFee, _secureSubmitPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();

            var config = new HpsServicesConfig();
            config.SecretApiKey = _secureSubmitPaymentSettings.SecretApiKey;
            config.DeveloperId = "002914";
            config.VersionNumber = "1513";

            var creditService = new HpsCreditService(config);

            try
            {
                var response = creditService.Capture(Convert.ToInt32(capturePaymentRequest.Order.AuthorizationTransactionId), capturePaymentRequest.Order.OrderTotal);

                result.NewPaymentStatus = PaymentStatus.Paid;
                result.CaptureTransactionId = response.TransactionId.ToString();
                result.CaptureTransactionResult = response.ResponseText;
            }
            catch (HpsException ex)
            {
                result.AddError(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();

            var config = new HpsServicesConfig();
            config.SecretApiKey = _secureSubmitPaymentSettings.SecretApiKey;
            config.DeveloperId = "002914";
            config.VersionNumber = "1513";

            var creditService = new HpsCreditService(config);

            try
            {
                creditService.Refund(
                    refundPaymentRequest.AmountToRefund, 
                    _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode, 
                    refundPaymentRequest.Order.CaptureTransactionId);

                var isOrderFullyRefunded = (refundPaymentRequest.AmountToRefund + refundPaymentRequest.Order.RefundedAmount == refundPaymentRequest.Order.OrderTotal);
                result.NewPaymentStatus = isOrderFullyRefunded ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
            }
            catch (HpsException ex)
            {
                result.AddError(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();

            var config = new HpsServicesConfig();
            config.SecretApiKey = _secureSubmitPaymentSettings.SecretApiKey;
            config.DeveloperId = "002914";
            config.VersionNumber = "1513";

            var creditService = new HpsCreditService(config);

            try
            {
                if (string.IsNullOrEmpty(voidPaymentRequest.Order.CaptureTransactionId))
                {
                    creditService.Void(Convert.ToInt32(voidPaymentRequest.Order.AuthorizationTransactionId));
                }
                else
                {
                    creditService.Void(Convert.ToInt32(voidPaymentRequest.Order.CaptureTransactionId));
                }

                result.NewPaymentStatus = PaymentStatus.Voided;
            }
            catch (HpsException ex)
            {
                result.AddError(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            throw new Exception("not implemented");
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            throw new Exception("not implemented");
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");
            
            //it's not a redirection payment method. So we always return false
            return false;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentSecureSubmit";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.SecureSubmit.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentSecureSubmit";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.SecureSubmit.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentSecureSubmitController);
        }

        public override void Install()
        {
            //settings
            var settings = new SecureSubmitPaymentSettings()
            {
                TransactMode = TransactMode.Authorize,
                PublicApiKey = "123",
                SecretApiKey = "456"
            };

            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Notes", "If you're using this gateway, ensure that your primary store currency is set to USD.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.TransactModeValues", "Transaction mode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.TransactModeValues.Hint", "Choose transaction mode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.PublicApiKey", "Public API Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.PublicApiKey.Hint", "Public API Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.SecretApiKey", "Secret API Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.SecretApiKey.Hint", "Specify your Secret API Key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");

            
            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<SecureSubmitPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Notes");
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.TransactModeValues");
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.TransactModeValues.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.PublicApiKey");
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.PublicApiKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.SecretApiKey");
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.SecretApiKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.SecureSubmit.Fields.AdditionalFeePercentage.Hint");
            
            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.Manual;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Standard;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get
            {
                return false;
            }
        }

        #endregion
    }
}
