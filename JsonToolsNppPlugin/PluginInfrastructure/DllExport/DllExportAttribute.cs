// NPP plugin platform for .Net v0.94.00 by Kasper B. Graversen etc.
using System;
using System.Runtime.InteropServices;

namespace RGiesecke.DllExport
{
    /// <summary>
    /// The fully qualified type name must be <c>RGiesecke.DllExport.DllExportAttribute</c> in order to work with the pre-configured task in <c>UnmanagedExports.Repack.Upgrade.targets</c>.
    /// This implementation could be avoided if we referenced the <c>RGiesecke.DllExport.Metadata</c> assembly, but then it will look like a runtime dependency, and be copied to the build output directory.
    /// <para>
    /// See <seealso href="https://github.com/stevenengland/UnmanagedExports.Repack.Upgrade/blob/master/nuget/build/UnmanagedExports.Repack.Upgrade.targets"/>
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    partial class DllExportAttribute : Attribute
    {
        public DllExportAttribute()
        {
        }

        public DllExportAttribute(string exportName)
            : this(exportName, CallingConvention.StdCall)
        {
        }

        public DllExportAttribute(string exportName, CallingConvention callingConvention)
        {
            ExportName = exportName;
            CallingConvention = callingConvention;
        }

        public CallingConvention CallingConvention { get; set; }

        public string ExportName { get; set; }
    }
}
