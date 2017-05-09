// Guids.cs
// MUST match guids.h

using System;

namespace LDC.Generator.Kamelon.VSExtension
{
    internal static class GuidList
    {
        public const string guidKamelonLibraryGeneratorPkgString = "19c00731-bc8c-4b1b-90f3-0bc976fafe71";
        public const string guidKamelonLibraryGeneratorCmdSetString = "9ec25f50-101d-4ed9-b1c7-063a3c1e62bf";

        public static readonly Guid guidKamelonLibraryGeneratorCmdSet = new Guid(guidKamelonLibraryGeneratorCmdSetString);
    }
}