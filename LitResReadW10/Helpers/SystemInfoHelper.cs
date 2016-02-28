﻿using Autofac;
using LitRes.Services;

namespace LitResReadW10.Helpers
{
    public class SystemInfoHelper
    {
        private static readonly IDeviceInfoService DeviceInfoService = ((App)App.Current).Scope.Resolve<IDeviceInfoService>();

        public static bool IsDesktop()
        {
            var deviceFamily = DeviceInfoService.DeviceFamily;
            return !string.IsNullOrEmpty(deviceFamily) && deviceFamily.Equals("Windows.Desktop");
        }
    }
}
