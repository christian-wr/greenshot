/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.User32;
using Greenshot.Base.Interfaces.Plugin;
using System.Linq;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Description of EnvironmentInfo.
    /// </summary>
    public static class EnvironmentInfo
    {
        private static bool? _isWindows;

        public static bool IsWindows
        {
            get
            {
                if (_isWindows.HasValue)
                {
                    return _isWindows.Value;
                }

                _isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
                return _isWindows.Value;
            }
        }

        public static string GetGreenshotVersion(bool shortVersion = false) => GetVersion(Assembly.GetExecutingAssembly(), shortVersion);

        /// <summary>
        /// The version of an assembly as the build made it: the informational version (e.g. 1.4.123 - g1a2b3c4), the short one is
        /// the file version (1.4.123). The assembly version alone only has major and minor (1.4.0.0).
        /// </summary>
        public static string GetVersion(Assembly executingAssembly, bool shortVersion = false)
        {
            // Use assembly version
            string greenshotVersion = executingAssembly.GetName().Version.ToString();

            // Use AssemblyFileVersion if available
            var assemblyFileVersionAttribute = executingAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (!string.IsNullOrEmpty(assemblyFileVersionAttribute?.Version))
            {
                var assemblyFileVersion = new Version(assemblyFileVersionAttribute.Version);
                greenshotVersion = assemblyFileVersion.ToString(2);
                try
                {
                    greenshotVersion = assemblyFileVersion.ToString(3);
                }
                catch (Exception)
                {
                    // Ignore
                }
            }

            if (!shortVersion)
            {
                // Use AssemblyInformationalVersion if available
                var informationalVersionAttribute = executingAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (!string.IsNullOrEmpty(informationalVersionAttribute?.InformationalVersion))
                {
                    greenshotVersion = informationalVersionAttribute.InformationalVersion;
                }
            }

            return greenshotVersion.Replace("+", " - ");
        }

        public static string EnvironmentToString(bool newline)
        {
            StringBuilder environment = new();
            environment.Append("Software version: " + GetGreenshotVersion() + EditionInfo.Suffix);
            if (GreenshotEnvironment.IsPortable)
            {
                environment.Append(" Portable");
            }

            environment.Append(" (" + OsInfo.Bits + " bit, " + OsInfo.ProcessArchitectureName + ")");

            if (newline)
            {
                environment.AppendLine();
            }
            else
            {
                environment.Append(", ");
            }

            environment.Append(".NET runtime version: " + Environment.Version);

            if (newline)
            {
                environment.AppendLine();
            }
            else
            {
                environment.Append(", ");
            }

            environment.Append("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));

            if (IsWindows)
            {
                if (newline)
                {
                    environment.AppendLine();
                }
                else
                {
                    environment.Append(", ");
                }

                environment.Append($"OS: {OsInfo.Name}");
                environment.Append($" {OsInfo.OsArchitectureName}");
                environment.Append($" {OsInfo.VersionString}");
                if (newline)
                {
                    environment.AppendLine();
                }
                else
                {
                    environment.Append(", ");
                }

                // Get some important information for fixing GDI related Problems
                environment.AppendFormat("GDI object count: {0}", User32Api.GetGuiResourcesGdiCount());
                if (newline)
                {
                    environment.AppendLine();
                }
                else
                {
                    environment.Append(", ");
                }

                environment.AppendFormat("User object count: {0}", User32Api.GetGuiResourcesUserCount());
            }
            else
            {
                if (newline)
                {
                    environment.AppendLine();
                }
                else
                {
                    environment.Append(", ");
                }

                environment.AppendFormat("OS: {0}", Environment.OSVersion.Platform);
            }

            if (newline)
            {
                environment.AppendLine();
            }
            else
            {
                environment.Append(", ");
            }
            // TODO: Is this needed?
            // environment.AppendFormat("Surface count: {0}", Surface.Count);

            environment.Append("Plugin list: ");
            environment.Append(string.Join(",", SimpleServiceProvider.Current.GetAllInstances<IGreenshotPlugin>().Select(plugin => plugin.Name)));

            if (newline)
            {
                environment.AppendLine();
            }
            else
            {
                environment.Append(", ");
            }

            return environment.ToString();
        }

        public static string ExceptionToString(Exception ex)
        {
            if (ex == null)
                return "null\r\n";

            StringBuilder report = new();

            report.AppendLine("Exception: " + ex.GetType());
            report.AppendLine("Message: " + ex.Message);
            if (ex.Data.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("Additional Information:");
                foreach (object key in ex.Data.Keys)
                {
                    object data = ex.Data[key];
                    if (data != null)
                    {
                        report.AppendLine(key + " : " + data);
                    }
                }
            }

            if (ex is ExternalException externalException)
            {
                // e.g. COMException
                report.AppendLine().AppendLine("ErrorCode: 0x" + externalException.ErrorCode.ToString("X"));
            }

            report.AppendLine().AppendLine("Stack:").AppendLine(ex.StackTrace);

            if (ex is ReflectionTypeLoadException reflectionTypeLoadException)
            {
                report.AppendLine().AppendLine("LoaderExceptions: ");
                foreach (Exception cbE in reflectionTypeLoadException.LoaderExceptions)
                {
                    report.AppendLine(cbE.Message);
                }
            }

            if (ex.InnerException != null)
            {
                report.AppendLine("--- InnerException: ---");
                report.AppendLine(ExceptionToString(ex.InnerException));
            }

            return report.ToString();
        }

        public static string BuildReport(Exception exception)
        {
            StringBuilder exceptionText = new();
            exceptionText.AppendLine(EnvironmentToString(true));
            exceptionText.AppendLine(ExceptionToString(exception));
            exceptionText.AppendLine("Configuration dump:");

            return exceptionText.ToString();
        }

        /// <summary>
        /// Returns the directory where the application is located
        /// </summary>
        public static string GetApplicationFolder()
            => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }

    /// <summary>
    /// Provides information about the host operating system, Greenshot needs Windows 10 1809 or later.
    /// </summary>
    public static class OsInfo
    {
        private const ushort IMAGE_FILE_MACHINE_I386 = 0x014C;
        private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
        private const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;

        private static readonly Lazy<Architecture> _osArchitecture = new(GetOsArchitecture);

        /// <summary>
        /// Determines if the current application is 32 or 64-bit.
        /// </summary>
        public static int Bits => IntPtr.Size * 8;

        /// <summary>
        /// The processor architecture Greenshot runs as: X64, Arm64 (the ARM64 build, see Greenshot.csproj) or X86 (Greenshot Light on 32-bit Windows).
        /// </summary>
        public static Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

        /// <summary>
        /// The processor architecture of Windows. Unlike RuntimeInformation.OSArchitecture it is Arm64 also for the x64 build,
        /// which ARM64 Windows runs emulated.
        /// </summary>
        public static Architecture OsArchitecture => _osArchitecture.Value;

        /// <summary>
        /// ProcessArchitecture as Windows names it, e.g. x64 or ARM64
        /// </summary>
        public static string ProcessArchitectureName => ToName(ProcessArchitecture);

        /// <summary>
        /// OsArchitecture as Windows names it, e.g. x64 or ARM64
        /// </summary>
        public static string OsArchitectureName => ToName(OsArchitecture);

        /// <summary>
        /// Gets the name of the operating system running on this computer.
        /// </summary>
        public static string Name => Environment.OSVersion.Version.Build < 22000 ? "Windows 10" : "Windows 11";

        /// <summary>
        /// Gets the version string of the operating system running on this computer.
        /// </summary>
        public static string VersionString => $"build {Environment.OSVersion.Version.Build}";

        private static string ToName(Architecture architecture) => architecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm => "ARM",
            Architecture.Arm64 => "ARM64",
            _ => architecture.ToString()
        };

        private static Architecture GetOsArchitecture()
        {
            try
            {
                // Also tells an emulated process the architecture of the machine
                if (IsWow64Process2(GetCurrentProcess(), out _, out ushort nativeMachine))
                {
                    switch (nativeMachine)
                    {
                        case IMAGE_FILE_MACHINE_I386:
                            return Architecture.X86;
                        case IMAGE_FILE_MACHINE_AMD64:
                            return Architecture.X64;
                        case IMAGE_FILE_MACHINE_ARM64:
                            return Architecture.Arm64;
                    }
                }
            }
            catch (EntryPointNotFoundException)
            {
                // Windows 10 before 1709
            }
            return RuntimeInformation.OSArchitecture;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();
    }
}
