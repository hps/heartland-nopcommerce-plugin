using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.SecureSubmit.Models;
using Nop.Plugin.Payments.SecureSubmit.Validators;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Services;

namespace Nop.Plugin.Payments.SecureSubmit.Controllers
{
    public class PaymentSecureSubmitController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        public PaymentSecureSubmitController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            ILocalizationService localizationService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._localizationService = localizationService;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var secureSubmitPaymentSettings = _settingService.LoadSetting<SecureSubmitPaymentSettings>(storeScope);

            var model = new ConfigurationModel();

            model.TransactModeId = Convert.ToInt32(secureSubmitPaymentSettings.TransactMode);
            model.PublicApiKey = secureSubmitPaymentSettings.PublicApiKey;
            model.SecretApiKey = secureSubmitPaymentSettings.SecretApiKey;
            model.AdditionalFee = secureSubmitPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = secureSubmitPaymentSettings.AdditionalFeePercentage;
            model.TransactModeValues = secureSubmitPaymentSettings.TransactMode.ToSelectList();

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.TransactModeId_OverrideForStore = _settingService.SettingExists(secureSubmitPaymentSettings, x => x.TransactMode, storeScope);
                model.PublicApiKey_OverrideForStore = _settingService.SettingExists(secureSubmitPaymentSettings, x => x.PublicApiKey, storeScope);
                model.SecretApiKey_OverrideForStore = _settingService.SettingExists(secureSubmitPaymentSettings, x => x.SecretApiKey, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(secureSubmitPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(secureSubmitPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.SecureSubmit/Views/PaymentSecureSubmit/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var secureSubmitPaymentSettings = _settingService.LoadSetting<SecureSubmitPaymentSettings>(storeScope);

            //save settings
            secureSubmitPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;
            secureSubmitPaymentSettings.PublicApiKey = model.PublicApiKey;
            secureSubmitPaymentSettings.SecretApiKey = model.SecretApiKey;
            secureSubmitPaymentSettings.AdditionalFee = model.AdditionalFee;
            secureSubmitPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            if (model.TransactModeId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(secureSubmitPaymentSettings, x => x.TransactMode, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(secureSubmitPaymentSettings, x => x.TransactMode, storeScope);

            if (model.PublicApiKey_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(secureSubmitPaymentSettings, x => x.PublicApiKey, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(secureSubmitPaymentSettings, x => x.PublicApiKey, storeScope);

            if (model.SecretApiKey_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(secureSubmitPaymentSettings, x => x.SecretApiKey, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(secureSubmitPaymentSettings, x => x.SecretApiKey, storeScope);

            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(secureSubmitPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(secureSubmitPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(secureSubmitPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(secureSubmitPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            _settingService.ClearCache();

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var secureSubmitPaymentSettings = _settingService.LoadSetting<SecureSubmitPaymentSettings>(storeScope);
            
            for (int i = 0; i < 15; i++)
            {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem()
                {
                    Text = year,
                    Value = year,
                });
            }

            for (int i = 1; i <= 12; i++)
            {
                string text = (i < 10) ? "0" + i.ToString() : i.ToString();
                model.ExpireMonths.Add(new SelectListItem()
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }

            model.PublicApiKey = secureSubmitPaymentSettings.PublicApiKey.Trim();

            //set postback values
            var form = this.Request.Form;
            model.SecureSubmitToken = form["token_value"];

            return View("~/Plugins/Payments.SecureSubmit/Views/PaymentSecureSubmit/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            return new List<string>();
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CustomValues.Add("token_value", form["token_value"]);
            return paymentInfo;
        }
    }
}