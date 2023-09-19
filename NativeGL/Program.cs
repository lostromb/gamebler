using Durandal.Common.Logger;
using Durandal.Common.Utils.NativePlatform;
using NativeGL.Utils;

namespace NativeGL
{
    public class Program
    {
        public static void Main(string[] args)
        {
            NativePlatformUtils.SetGlobalResolver(new NativeLibraryLoader());
            NativePlatformUtils.PrepareNativeLibrary("opus", DebugLogger.Default);
            NativePlatformUtils.PrepareNativeLibrary("speexdsp", DebugLogger.Default);
            new MainWindow().Run();
        }
    }
}
