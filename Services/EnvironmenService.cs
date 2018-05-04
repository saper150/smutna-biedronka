


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

interface IEnvironmentService {
    OSPlatform OSType();
}


class EnvironmentService : IEnvironmentService {
    public OSPlatform OSType() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return OSPlatform.Windows;
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            return OSPlatform.Linux;
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            return OSPlatform.OSX;
        } else {
            throw new Exception("Unknown OS");
        }

    }
}

