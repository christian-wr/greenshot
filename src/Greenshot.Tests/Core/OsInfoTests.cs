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

using System.Runtime.InteropServices;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The architectures in the environment of bug reports: an x64 Greenshot on ARM64 Windows runs emulated, the report must show both
    /// </summary>
    public class OsInfoTests
    {
        [Fact]
        public void ProcessArchitecture_MatchesBits()
        {
            if (OsInfo.Bits == 64)
            {
                Assert.Contains(OsInfo.ProcessArchitecture, new[] { Architecture.X64, Architecture.Arm64 });
            }
            else
            {
                Assert.Equal(Architecture.X86, OsInfo.ProcessArchitecture);
            }
        }

        [Fact]
        public void OsArchitecture_CanRunTheProcess()
        {
            switch (OsInfo.ProcessArchitecture)
            {
                case Architecture.Arm64:
                    Assert.Equal(Architecture.Arm64, OsInfo.OsArchitecture);
                    break;
                case Architecture.X64:
                    // x64 Windows, or emulated on ARM64 Windows
                    Assert.Contains(OsInfo.OsArchitecture, new[] { Architecture.X64, Architecture.Arm64 });
                    break;
            }
        }

        [Fact]
        public void EnvironmentToString_NamesBothArchitectures()
        {
            string environment = EnvironmentInfo.EnvironmentToString(true);

            Assert.Contains($" bit, {OsInfo.ProcessArchitectureName})", environment);
            Assert.Contains($"OS: {OsInfo.Name} {OsInfo.OsArchitectureName} ", environment);
            Assert.Contains(OsInfo.OsArchitectureName, new[] { "x86", "x64", "ARM64" });
        }
    }
}
