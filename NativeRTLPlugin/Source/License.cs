using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Text;

using Babel.Licensing;

namespace NativeRTLPlugin
{
    [LicenseRestrictionProvider(typeof(FileLicenseProvider))]
    class License
    {
        public void CheckTrialLicense()
        {
            try
            {
                Babel.Licensing.BabelFileLicenseProvider.LicenseFile = Application.dataPath + "/NativeRTL.licenses";

                BabelLicenseManager.RegisterLicenseProvider(typeof(License), new BabelFileLicenseProvider());
                ILicense license = BabelLicenseManager.Validate(typeof(License), this);

                TrialRestriction trialRestriction = license.Restrictions.OfType<TrialRestriction>().FirstOrDefault();

                if (trialRestriction != null && trialRestriction.TimeLeft.HasValue)
                {
                    Debug.Log("[NativeRTLPlugin]: You have " + trialRestriction.TimeLeft.Value + " left to evaluate NativeRTL");
                }

                if (trialRestriction != null && trialRestriction.ExpireDate.HasValue)
                {
                    Debug.Log("[NativeRTLPlugin]: You have " + trialRestriction.ExpireDate.Value + " left to evaluate NativeRTL");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }
    }
}
