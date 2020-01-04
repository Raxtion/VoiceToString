using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Storage;

// 組件的一般資訊是由下列的屬性集控制。
// 變更這些屬性的值即可修改組件的相關
// 資訊。
[assembly: AssemblyTitle("VoiceToString_Sharp")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("VoiceToString_Sharp")]
[assembly: AssemblyCopyright("Copyright ©  2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 將 ComVisible 設為 false 可對 COM 元件隱藏
// 組件中的類型。若必須從 COM 存取此組件中的類型，
// 的類型，請在該類型上將 ComVisible 屬性設定為 true。
[assembly: ComVisible(false)]

// 下列 GUID 為專案公開 (Expose) 至 COM 時所要使用的 typelib ID
[assembly: Guid("8d3ae920-549a-4db3-8513-ef0ed2af4d2c")]

// 組件的版本資訊由下列四個值所組成: 
//
//      主要版本
//      次要版本
//      組建編號
//      修訂編號
//
// 您可以指定所有的值，也可以使用 '*' 將組建和修訂編號
// 設為預設，如下所示:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]


namespace AssemblyInfo
{
    public class AssemblyInfo
    {
        //__version=1.0.0.1
        public string Assembly_Version = "1.0.0.2";
        public string File_Version_Str = "1.0.0.2";

        //IniFile assembly
        //public string Assembly_Product_DataDir = "C:\\Product_Data\\";
        public string Assembly_Product_DataDir = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Product_Data\\";
        public string Assembly_ErrorMessageDir = "Error Message\\";
        public string Assembly_ErrorMessageFileName = "VoiceToString_ErrorMessage.ini";
        public string Assembly_ErrorMessageEngFileName = "VoiceToString_ErrorMessageEng.ini";
        public string Assembly_MachineFileName = "VoiceToString.sis";
        public string Assembly_DefaultProductFileName = "Default.ini";
        public string Assembly_LogDir = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Log\\";

        //Exe assembly
        public string Assembly_Copyright = "Copyright Clare-Tech (C) 2020";
        public string Assembly_FileDescription = "VoiceToString Power by C#";
        public string Assembly_OriginalFilename = "VoiceToString.exe";

        public string Assembly_ProductName = "VoiceToString";
    }
}


















































































