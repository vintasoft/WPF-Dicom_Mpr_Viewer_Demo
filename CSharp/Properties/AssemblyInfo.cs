using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

#if REMOVE_DICOM_PLUGIN
#error DicomMprViewerDemo project cannot be used without VintaSoft DICOM .NET Plug-in.
#endif

[assembly: AssemblyTitle("VintaSoft WPF DICOM MPR Viewer Demo")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("VintaSoft Ltd.")]
[assembly: AssemblyProduct("VintaSoft Imaging .NET SDK")]
[assembly: AssemblyCopyright("Copyright VintaSoft Ltd. 2024")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
)]

[assembly: AssemblyVersion("12.4.9.1")]
[assembly: AssemblyFileVersion("12.4.9.1")]
