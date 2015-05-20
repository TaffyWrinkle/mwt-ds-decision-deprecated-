﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDecisionService
{
    internal static class DecisionServiceConstants
    {
        internal static readonly string RedirectionBlobLocation = "http://decisionservicestorage.blob.core.windows.net/app-locations/{0}";

        internal static readonly string ApplicationBlobLocationContainerName = "app-locations";
        internal static readonly string SettingsContainerName = "mwt-settings-{0}";
        internal static readonly string LatestSettingsBlobName = "settings";

        internal static readonly int RetryCount = 3;
        internal static readonly TimeSpan RetryMinBackoff = TimeSpan.FromMilliseconds(500);
        internal static readonly TimeSpan RetryMaxBackoff = TimeSpan.FromMilliseconds(4000);
        internal static readonly TimeSpan RetryDeltaBackoff = TimeSpan.FromMilliseconds(1000);

        internal static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(5);
        internal static readonly TimeSpan PollCancelWait = TimeSpan.FromSeconds(2);
    }
}
