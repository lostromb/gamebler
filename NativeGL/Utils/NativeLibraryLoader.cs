using Durandal.API;
using Durandal.Common.Logger;
using Durandal.Common.Utils;
using Durandal.Common.Utils.NativePlatform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace NativeGL.Utils
{
    /// <summary>
    /// Default implementation of INativeLibraryResolver which works for most platforms.
    /// Looks for library files for the current platform in
    /// /{currentDir}/runtimes/{runtime}/native, and (usually) copies the matching library into
    /// {currentDir} so it will be picked up by the platform's library resolver.
    /// </summary>
    public class NativeLibraryLoader : INativeLibraryResolver
    {
        private static readonly Lazy<OSAndArchitecture> CachedPlatformInfo = new Lazy<OSAndArchitecture>(
            GetCurrentPlatformInternal, LazyThreadSafetyMode.PublicationOnly);

        private readonly IDictionary<string, NativeLibraryStatus> _loadedLibraries = new Dictionary<string, NativeLibraryStatus>();
        private readonly object _libraryLoadMutex = new object();
        private readonly DirectoryInfo _workingDir;

        /// <summary>
        /// Constructs a new <see cref="NativeLibraryResolverImpl"/> with a specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the program. If null, <see cref="Environment.CurrentDirectory"/> will be used.</param>
        public NativeLibraryLoader(DirectoryInfo workingDirectory = null)
        {
            _workingDir = workingDirectory ?? new DirectoryInfo(Environment.CurrentDirectory);
        }

        /// <inheritdoc />
        public OSAndArchitecture GetCurrentPlatform()
        {
            return CachedPlatformInfo.Value;
        }

        /// <inheritdoc />
        public NativeLibraryStatus PrepareNativeLibrary(string libraryName, ILogger logger)
        {
            logger.Log(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Preparing native library \"{0}\"", libraryName);

            OSAndArchitecture platform = CachedPlatformInfo.Value;
            string normalizedLibraryName = NormalizeLibraryName(libraryName, platform);
            lock (_libraryLoadMutex)
            {
                NativeLibraryStatus prevStatus;
                if (_loadedLibraries.TryGetValue(normalizedLibraryName, out prevStatus))
                {
                    logger.Log(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Native library \"{0}\" has already been prepared; nothing to do", libraryName);
                    return prevStatus;
                }

                // Clean up any loose local binaries first
                DeleteLocalLibraryIfPresent(normalizedLibraryName, logger);

                // See if the library is actually provided by the system already
                NativeLibraryStatus builtInLibStatus = ProbeLibrary(normalizedLibraryName);
                if (builtInLibStatus == NativeLibraryStatus.Available)
                {
                    logger.Log(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Native library \"{0}\" resolved to a system-provided binary; skipping local binary resolution", libraryName);
                    _loadedLibraries[normalizedLibraryName] = builtInLibStatus;
                    return builtInLibStatus;
                }

                // Search the most applicable /runtimes source directory for a matching library file
                string baseDirectory = Path.Combine(_workingDir.FullName, "runtimes");
                List<string> possibleLibraryNames = PermuteLibraryNames(libraryName, platform);
                List<string> possibleDirectoryNames = PermuteArchitectureSpecificDirectoryNames(platform);
                foreach (string possibleDirectory in possibleDirectoryNames)
                {
                    DirectoryInfo probeDir = new DirectoryInfo(Path.Combine(baseDirectory, possibleDirectory, "native"));
                    if (!probeDir.Exists)
                    {
                        continue;
                    }

                    foreach (string possibleSourceLibrary in possibleLibraryNames)
                    {
                        FileInfo sourceLibraryFile = new FileInfo(Path.Combine(probeDir.FullName, possibleSourceLibrary));
                        if (!sourceLibraryFile.Exists)
                        {
                            continue;
                        }

                        // Do platform-specific work to make this library discoverable by the platform's default library lookup
                        // Apparently in legacy .NetFx (and Mono), Linux .so libraries would not be picked up from the current
                        // executable directory. This seems to have changed in .Net core so that .so files are discovered
                        // the same way as .dlls. "lib" is also prepended to Linux lib search paths automatically.
                        if (platform.OS == PlatformOperatingSystem.Windows ||
                            platform.OS == PlatformOperatingSystem.Linux)
                        {
                            FileInfo desiredBinplacePath = new FileInfo(Path.Combine(_workingDir.FullName, normalizedLibraryName));

                            try
                            {
                                logger.Log($"Resolved native library \"{libraryName}\" to {sourceLibraryFile.FullName}");
                                sourceLibraryFile.CopyTo(desiredBinplacePath.FullName);
                                _loadedLibraries[normalizedLibraryName] = NativeLibraryStatus.Available;
                                return NativeLibraryStatus.Available;
                            }
                            catch (Exception e)
                            {
                                logger.Log(e, LogLevel.Err);
                                logger.Log($"Could not prepare native library \"{libraryName}\" (is the existing library file locked or in use?)", LogLevel.Err);
                                _loadedLibraries[normalizedLibraryName] = NativeLibraryStatus.Unknown;
                                return NativeLibraryStatus.Unknown;
                            }
                        }
                        else
                        {
                            throw new PlatformNotSupportedException($"Don't know yet how to load libraries for {platform.OS}");
                        }
                    }
                }

                logger.Log(LogLevel.Wrn, DataPrivacyClassification.SystemMetadata,
                    "Failed to resolve native library \"{0}\".", libraryName);
                _loadedLibraries[normalizedLibraryName] = NativeLibraryStatus.Unavailable;
                return NativeLibraryStatus.Unavailable;
            }
        }

        private void DeleteLocalLibraryIfPresent(string normalizedLibraryName, ILogger logger)
        {
            FileInfo existingLocalLibPath = new FileInfo(Path.Combine(_workingDir.FullName, normalizedLibraryName));

            if (existingLocalLibPath.Exists)
            {
                try
                {
                    logger.Log($"Clobbering existing file {existingLocalLibPath.FullName}", LogLevel.Wrn);
                    existingLocalLibPath.Delete();
                }
                catch (Exception)
                {
                    logger.Log($"Failed to clean up \"{existingLocalLibPath.FullName}\" (is it locked or in use?)", LogLevel.Wrn);
                }
            }
        }

        /// <summary>
        /// Determines the current operating system and processor architecture that this program is running in.
        /// </summary>
        /// <returns></returns>
        private static OSAndArchitecture GetCurrentPlatformInternal()
        {
#if NETCOREAPP
            return NativeLibraryExtensions.ParseRuntimeId(RuntimeInformation.RuntimeIdentifier);
#else
            // Figure out our OS
            PlatformOperatingSystem os = PlatformOperatingSystem.Unknown;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = PlatformOperatingSystem.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                os = PlatformOperatingSystem.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                os = PlatformOperatingSystem.MacOS;
            }

            // Figure out our architecture
            PlatformArchitecture arch = PlatformArchitecture.Unknown;
            if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
            {
                arch = PlatformArchitecture.I386;
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                arch = PlatformArchitecture.X64;
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
            {
                arch = PlatformArchitecture.ArmV7;
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                arch = PlatformArchitecture.Arm64;
            }

            return new OSAndArchitecture(os, arch);
#endif
        }

        private static List<string> PermuteArchitectureSpecificDirectoryNames(OSAndArchitecture platformInfo)
        {
            string mostSpecificRid = $"{platformInfo.OS.GetRuntimeIdString()}-{platformInfo.Architecture.GetRuntimeIdString()}";

            IReadOnlyList<string> inheritedRids = NativeLibraryExtensions.GetInheritedRuntimeIds(mostSpecificRid);
            List<string> returnVal = new List<string>(inheritedRids.Count + 1);
            returnVal.Add(mostSpecificRid);

            // handle legacy windows IDs that might come up somewhere
            if (platformInfo.OS == PlatformOperatingSystem.Windows)
            {
                returnVal.Add($"win10-{platformInfo.Architecture.GetRuntimeIdString()}");
                returnVal.Add($"win81-{platformInfo.Architecture.GetRuntimeIdString()}");
                returnVal.Add($"win8-{platformInfo.Architecture.GetRuntimeIdString()}");
                returnVal.Add($"win7-{platformInfo.Architecture.GetRuntimeIdString()}");
                returnVal.Add($"win10");
                returnVal.Add($"win81");
                returnVal.Add($"win8");
                returnVal.Add($"win7");
            }

            returnVal.AddRange(inheritedRids);
            return returnVal;
        }

        private static string LibraryNameWithoutExtension(string libraryName)
        {
            if (!libraryName.Contains('.'))
            {
                return libraryName;
            }

            string libNameLowercase = libraryName.ToLowerInvariant();
            if (libNameLowercase.EndsWith(".dll") ||
                libNameLowercase.EndsWith(".so") ||
                libNameLowercase.EndsWith(".dylib"))
            {
                return libraryName.Substring(0, libraryName.LastIndexOf('.'));
            }

            return libraryName;
        }

        private static string NormalizeLibraryName(string requestedName, OSAndArchitecture platformInfo)
        {
            string nameWithoutExtension = LibraryNameWithoutExtension(requestedName);

            if (platformInfo.OS == PlatformOperatingSystem.Windows)
            {
                return nameWithoutExtension + ".dll";
            }
            else if (platformInfo.OS == PlatformOperatingSystem.Linux ||
                platformInfo.OS == PlatformOperatingSystem.Android ||
                platformInfo.OS == PlatformOperatingSystem.Linux_Bionic ||
                platformInfo.OS == PlatformOperatingSystem.Linux_Musl ||
                platformInfo.OS == PlatformOperatingSystem.Unix)
            {
                return nameWithoutExtension + ".so";
            }
            else if (platformInfo.OS == PlatformOperatingSystem.iOS ||
                platformInfo.OS == PlatformOperatingSystem.iOS_Simulator ||
                platformInfo.OS == PlatformOperatingSystem.MacOS ||
                platformInfo.OS == PlatformOperatingSystem.MacCatalyst)
            {
                return nameWithoutExtension + ".dylib";
            }
            else
            {
                return requestedName;
            }
        }

        private static List<string> PermuteLibraryNames(string requestedName, OSAndArchitecture platformInfo)
        {
            List<string> returnVal = new List<string>(16);
            string nameWithoutExtension = LibraryNameWithoutExtension(requestedName);

            if (platformInfo.OS == PlatformOperatingSystem.Windows)
            {
                returnVal.Add($"{nameWithoutExtension}.dll");
                returnVal.Add($"lib{nameWithoutExtension}.dll");
                if (platformInfo.Architecture == PlatformArchitecture.I386)
                {
                    returnVal.Add($"{nameWithoutExtension}_x86.dll");
                    returnVal.Add($"lib{nameWithoutExtension}_x86.dll");
                    returnVal.Add($"{nameWithoutExtension}x86.dll");
                    returnVal.Add($"lib{nameWithoutExtension}x86.dll");
                    returnVal.Add($"{nameWithoutExtension}32.dll");
                    returnVal.Add($"lib{nameWithoutExtension}32.dll");
                    returnVal.Add($"{nameWithoutExtension}_32.dll");
                    returnVal.Add($"lib{nameWithoutExtension}_32.dll");
                    returnVal.Add($"{nameWithoutExtension}-32.dll");
                    returnVal.Add($"lib{nameWithoutExtension}-32.dll");
                }

                if (platformInfo.Architecture == PlatformArchitecture.X64)
                {
                    returnVal.Add($"{nameWithoutExtension}_x64.dll");
                    returnVal.Add($"lib{nameWithoutExtension}_x64.dll");
                    returnVal.Add($"{nameWithoutExtension}x64.dll");
                    returnVal.Add($"lib{nameWithoutExtension}x64.dll");
                    returnVal.Add($"{nameWithoutExtension}64.dll");
                    returnVal.Add($"lib{nameWithoutExtension}64.dll");
                    returnVal.Add($"{nameWithoutExtension}_64.dll");
                    returnVal.Add($"lib{nameWithoutExtension}_64.dll");
                    returnVal.Add($"{nameWithoutExtension}-64.dll");
                    returnVal.Add($"lib{nameWithoutExtension}-64.dll");
                }
            }
            else if (platformInfo.OS == PlatformOperatingSystem.Linux)
            {
                returnVal.Add($"{nameWithoutExtension}.so");
                returnVal.Add($"lib{nameWithoutExtension}.so");
            }
            else if (platformInfo.OS == PlatformOperatingSystem.MacOS)
            {
                returnVal.Add($"{nameWithoutExtension}.dylib");
                returnVal.Add($"lib{nameWithoutExtension}.dylib");
            }
            else
            {
                returnVal.Add(requestedName);
            }

            return returnVal;
        }

        private NativeLibraryStatus ProbeLibrary(string libName)
        {
            libName.AssertNonNullOrEmpty(nameof(libName));
            OSAndArchitecture platformInfo = GetCurrentPlatform();
            if (platformInfo.OS == PlatformOperatingSystem.Windows)
            {
                IntPtr dllHandle = IntPtr.Zero;
                try
                {
                    dllHandle = LoadLibraryEx(libName, hFile: IntPtr.Zero, dwFlags: LOAD_LIBRARY_AS_DATAFILE);
                    return dllHandle == IntPtr.Zero ? NativeLibraryStatus.Unavailable : NativeLibraryStatus.Available;
                }
                finally
                {
                    if (dllHandle != IntPtr.Zero)
                    {
                        FreeLibrary(dllHandle);
                    }
                }
            }
            else if (platformInfo.OS == PlatformOperatingSystem.Linux)
            {
                IntPtr soHandle = IntPtr.Zero;
                try
                {
                    soHandle = dlopen(libName, RTLD_NOW);
                    return soHandle == IntPtr.Zero ? NativeLibraryStatus.Unavailable : NativeLibraryStatus.Available;
                }
                finally
                {
                    if (soHandle != IntPtr.Zero)
                    {
                        dlclose(soHandle);
                    }
                }
            }
            else
            {
                return NativeLibraryStatus.Unknown;
            }
        }

        // ---- Windows ----

        private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

        private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;


        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        // ---- Linux ----

        private const int RTLD_NOW = 2;

        [DllImport("libdl.so")]
        private static extern IntPtr dlopen(string fileName, int flags);

        [DllImport("libdl.so")]
        private static extern int dlclose(IntPtr handle);

        [DllImport("libdl.so")]
        private static extern IntPtr dlerror();
    }
}
