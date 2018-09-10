using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Microsoft.Diagnostics.EventFlow.Core")]
[assembly: AssemblyTrademark("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f1ef768d-9e8b-42d5-b0e1-c7ef2270c245")]

// Expose internals to Tests
[assembly: InternalsVisibleTo("Microsoft.Diagnostics.EventFlow.Core.Tests")]
[assembly: InternalsVisibleTo("Microsoft.Diagnostics.EventFlow.TestHelpers")]
[assembly: InternalsVisibleTo("Microsoft.Diagnostics.EventFlow.FunctionalTests.HealthReporterBuster")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] 