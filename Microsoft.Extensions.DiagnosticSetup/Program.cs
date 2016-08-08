
namespace Microsoft.Extensions.DiagnosticSetup
{
    class Program
    {
        // If more than one copy of the .NET CLR runtime is loaded into the process, the counter instance names
        // must be disambiguated by applying alternative instace name format. This requires a registry key change.
        // For more info see https://msdn.microsoft.com/en-us/library/dd537616(v=vs.110).aspx
        // The method to construct the instance name is VersioningHelper.MakeVersionSafeName https://msdn.microsoft.com/en-us/library/ms147560(v=vs.110).aspx
        // For more info see http://stackoverflow.com/questions/10196432/getting-the-clr-id
        static void Main(string[] args)
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(
                      @"System\CurrentControlSet\Services\.NETFramework\Performance");
            // Create or overwrite value.
            key.SetValue("ProcessNameFormat", 1, Microsoft.Win32.RegistryValueKind.DWord);
            key.Close();
        }
    }
}
