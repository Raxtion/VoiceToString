using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;
using AssemblyInfo;
using Windows.Storage;
using Windows.UI.Popups;

using CLARE.Core;
using VoiceToString_CSharp;

namespace CLARE.Data
{
    public class IniFile
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        internal static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder value, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern int GetPrivateProfileSection(string section, IntPtr keyValue, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern Boolean WritePrivateProfileString(string section, string key, string value, string filePath);



        #region private variable
        private MainPage g_MainPage;
        private static int capacity = 512;
        #endregion

        #region private function
        private static bool WriteValue(string section, string key, string value, string filePath)
        {
            bool result = WritePrivateProfileString(section, key, value, filePath);
            return result;
        }
        private static string ReadValue(string section, string key, string filePath, string defaultValue)
        {
            StringBuilder value = new StringBuilder(capacity);
            GetPrivateProfileString(section, key, defaultValue, value, value.Capacity, filePath);
            return value.ToString();
        }
        private static bool DeleteSection(string section, string filepath)
        {
            bool result = WritePrivateProfileString(section, null, null, filepath);
            return result;
        }
        private static bool DeleteKey(string section, string key, string filepath)
        {
            bool result = WritePrivateProfileString(section, key, null, filepath);
            return result;
        }
        private static Dictionary<string, string> ReadKeyValuePairs(string section, string filePath)
        {
            while (true)
            {
                Char c = '0';
                IntPtr returnedString = Marshal.AllocCoTaskMem(capacity * Marshal.SizeOf(structure: c));
                int size = GetPrivateProfileSection(section, returnedString, capacity, filePath);

                if (size == 0)
                {
                    Marshal.FreeCoTaskMem(returnedString);
                    return null;
                }

                if (size < capacity - 2)
                {
                    Dictionary<string, string> dicResult = new Dictionary<string, string>();
                    string result = Marshal.PtrToStringAuto(returnedString, size - 1);
                    Marshal.FreeCoTaskMem(returnedString);
                    string[] arystrTemp = result.Split('\0');

                    for (int nX = 0; nX < arystrTemp.Length; nX++)
                    {
                        string[] strKeyValue = arystrTemp[nX].Split('=');
                        dicResult.Add(strKeyValue[0], strKeyValue[1]);
                    }

                    return dicResult;
                }

                Marshal.FreeCoTaskMem(returnedString);
                capacity = capacity * 2;
            }
        }
        private void DDXFile_Bool(bool bRead, string filePath, string Section, string key, ref bool value, bool bDefault)
        {
            if (bRead)
            {
                string strDefault = "";
                if (bDefault) strDefault = "1";
                else strDefault = "0";
                if (ReadValue(Section, key, filePath, strDefault) == "1") value = true;
                else value = false;
            }
            else
            {
                if (value == true) WriteValue(Section, key, "1", filePath);
                else WriteValue(Section, key, "0", filePath);
            }
        }
        private void DDXFile_Int(bool bRead, string filePath, string Section, string key, ref int value, int bDefault)
        {
            string strDefault = Convert.ToString(bDefault);
            if (bRead) value = Convert.ToInt32(ReadValue(Section, key, filePath, strDefault));
            else WriteValue(Section, key, Convert.ToString(value), filePath);
        }
        private void DDXFile_Float(bool bRead, string filePath, string Section, string key, ref double value, double bDefault)
        {
            string strDefault = Convert.ToString(bDefault);
            if (bRead) value = Convert.ToDouble(ReadValue(Section, key, filePath, strDefault));
            else WriteValue(Section, key, Convert.ToString(value), filePath);
        }
        private void DDXFile_String(bool bRead, string filePath, string Section, string key, ref string value, string bDefault)
        {
            string strDefault = Convert.ToString(bDefault);
            if (bRead) value = ReadValue(Section, key, filePath, strDefault);
            else WriteValue(Section, key, (string)value, filePath);
        }
        #endregion


        #region public variable
        //Assambly
        private AssemblyInfo.AssemblyInfo assembly = new AssemblyInfo.AssemblyInfo();

        //Software
        public readonly string Version = "";
        public readonly string Product_DataDir = "";
        public readonly string ErrorMessageDir = "";
        public readonly string ErrorMessageFileName = "";
        public readonly string ErrorMessageEngFileName = "";
        public readonly string MachineFileName = "";
        public readonly string DefaultProductFileName = "";
        public readonly string LogDir = "";
        public readonly string Assembly_Copyright = "";
        public readonly string Assembly_FileDescription = "";
        public readonly string Assembly_OriginalFilename = "";
        public readonly string Assembly_ProductName = "";

        //System
        public int m_nErrorCode = 0;
        public int m_nLanguageMode = 0;    //0=Chinese 1=English 2=Simple
        public int m_nPriviledge = 0;
        public string m_strCTPassword = "";
        public string m_strENGPassword = "";
        public string m_strLastFileName = "";
        public string m_strAccountPE = "";
        public string m_strAccountEE = "";
        public string m_strCAFullPath = "";
        public string m_strLogInENGAccount = "";

        

        #endregion

        #region public function
        public void MachineFile(bool bRead)
        {
            string strfilepath = Product_DataDir + MachineFileName;
            DDXFile_Int(bRead, strfilepath, "System", "m_nLanguageMode",ref m_nLanguageMode, 0);
            DDXFile_String(bRead, strfilepath, "System", "m_strCTPassword",ref m_strCTPassword, "CLARE");
            DDXFile_String(bRead, strfilepath, "System", "m_strENGPassword",ref m_strENGPassword, "123");
            DDXFile_String(bRead, strfilepath, "System", "m_strLastFileName",ref m_strLastFileName, "Default.ini");
            DDXFile_String(bRead, strfilepath, "System", "m_strAccountPE", ref m_strAccountPE, "Account=Password=[1,2,3,4,5]@@clare=123=[1,2,3,4,5]@@BAC=123=[1,2,3,4,5]@@CXZ=555=[1,2,3,4,5]");
            DDXFile_String(bRead, strfilepath, "System", "m_strAccountEE", ref m_strAccountEE, "Account=Password=[1,2,3,4,5]@@clare=123=[1,2,3,4,5]@@BAC=123=[1,2,3,4,5]@@CXZ=555=[1,2,3,4,5]");
            DDXFile_String(bRead, strfilepath, "System", "m_strCAFullPath", ref m_strCAFullPath, "R:\\ClareHashInfo");
        }
        public void ProductFile(string strFileName, bool bRead)
        {
            //throw new System.NotImplementedException();
            // TODO: 於此處插入傳回陳述式
        }
        public string GetErrorString(int nErrorCode, int nMode)
        {
            throw new System.NotImplementedException();
            // TODO: 於此處插入傳回陳述式
        }
        public string GetVersionCode(string strInp, int nAddOrCut)
        {
            string strRet = "";
            try
            {
                string[] arystrVersion = strInp.Split('.');
                arystrVersion[3] = System.Convert.ToString(Convert.ToInt32(arystrVersion[3]) + nAddOrCut);
                strRet = String.Join(".", arystrVersion);
            }
            catch (Exception) { }
            return strRet;
        }
        public int AddLog(string strInp)
        {
            int nAddLogResult = 0;

            if (System.IO.Directory.Exists(LogDir) != true) System.IO.Directory.CreateDirectory(LogDir);

            DateTime dtDate = DateTime.Now;
            string strLogTime = dtDate.Hour.ToString("[00:") + dtDate.Minute.ToString("00:") + dtDate.Second.ToString("00]  ");

            System.Text.Encoding u8 = System.Text.Encoding.UTF8;
            Byte[] arrydate = u8.GetBytes(strLogTime + strInp + "\r\n");

            string fileName = LogDir + dtDate.Year.ToString("0000_") + dtDate.Month.ToString("00_") + dtDate.Day.ToString("00") + ".txt";
            if (System.IO.File.Exists(fileName) != true)
            {
                new FileStream(fileName, FileMode.Create).Close();
            }
            System.IO.FileStream f = new FileStream(fileName, FileMode.Append);
            try
            {
                for (int i = 0; i < arrydate.Length; i++)
                {
                    f.WriteByte(arrydate[i]);
                }
                nAddLogResult = 0;
            }
            catch (Exception e)
	        {
                Byte[] arryerrdate = u8.GetBytes(e.Message + "\r\n");
                for (int i = 0; i < arryerrdate.Length; i++)
                {
                    f.WriteByte(arryerrdate[i]);
                }
                nAddLogResult = 1;
            }
            finally
            {
                f.Close();
            }
            return nAddLogResult;
        }
        public int AddChangeLog(string strInp)
        {
            int nAddLogResult = 0;

            if (System.IO.Directory.Exists(LogDir + "ParameterChangeLog\\") != true) System.IO.Directory.CreateDirectory(LogDir + "ParameterChangeLog\\");

            DateTime dtDate = DateTime.Now;
            string strLogTime = dtDate.Hour.ToString("[00:") + dtDate.Minute.ToString("00:") + dtDate.Second.ToString("00]  ");

            System.Text.Encoding u8 = System.Text.Encoding.UTF8;
            Byte[] arrydate = u8.GetBytes(strLogTime + m_strLogInENGAccount + "\t: " + strInp + "\r\n");

            string fileName = LogDir + "ParameterChangeLog\\" + dtDate.Year.ToString("0000_") + dtDate.Month.ToString("00_") + dtDate.Day.ToString("00") + ".txt";
            if (System.IO.File.Exists(fileName) != true)
            {
                new FileStream(fileName, FileMode.Create).Close();
            }
            System.IO.FileStream f = new FileStream(fileName, FileMode.Append);
            try
            {
                for (int i = 0; i < arrydate.Length; i++)
                {
                    f.WriteByte(arrydate[i]);
                }
                nAddLogResult = 0;
            }
            catch (Exception e)
            {
                Byte[] arryerrdate = u8.GetBytes(e.Message + "\r\n");
                for (int i = 0; i < arryerrdate.Length; i++)
                {
                    f.WriteByte(arryerrdate[i]);
                }
                nAddLogResult = 1;
            }
            finally
            {
                f.Close();
            }
            return nAddLogResult;
        }
        public void ProcessVersion()
        {
            //Get Version File Path
            string strPath = this.GetType().Assembly.Location;              //Get .exe Location "D:\\DeskTopForCode\\Project\\ControlTemplate_working\\x64\\Debug\\ControlTemplate.exe"
            string[] arystrPath = strPath.Split('\\');
            int nIsReleaseMode = Array.IndexOf(arystrPath, "Release");
            if (nIsReleaseMode != -1) return;
            Array.Resize<string>(ref arystrPath, arystrPath.Length - 3);    //arystrPath = "D:","DeskTopForCode","Project","ControlTemplate_working"
            Array.Resize<string>(ref arystrPath, arystrPath.Length + 1);
            arystrPath[arystrPath.Length - 1] = "Properties";
            string strPathNew = String.Join("\\", arystrPath);              //strPathNew = "D:\\DeskTopForCode\\Project\\ControlTemplate_working"
            string strVersionFile = strPathNew + "\\AssemblyInfo.cs";

            if (System.IO.File.Exists(strVersionFile))
            {
                //Read Version File
                System.IO.StreamReader filereader = new System.IO.StreamReader(strVersionFile);
                string[] arystrVersionFileDataRow = new string[1000];
                int nIndex = 0;
                try
                {
                    while (filereader.Peek() >= 0)
                    {
                        arystrVersionFileDataRow[nIndex] = filereader.ReadLine();
                        nIndex++;
                    }
                }
                finally { filereader.Close(); }

                Array.Resize<string>(ref arystrVersionFileDataRow, nIndex + 2);

                int nLoc = 0;
                foreach (string strRow in arystrVersionFileDataRow)
                {
                    if (strRow.Contains("//__version=")) break;
                    nLoc++;
                }

                string strVersionCode = "";
                if (nIndex > 0) strVersionCode = (arystrVersionFileDataRow[nLoc].Split('='))[1];

                string strWriteToAssembly = GetVersionCode(strVersionCode, +2);
                string strLogOutToAssembly = GetVersionCode(strVersionCode, +1);

                if (strVersionCode != "")
                {
                    //Check This VERSION
                    string strAssembly_Version = (arystrVersionFileDataRow[nLoc + 1].Split(' '))[2];
                    string strForwardVERSION = GetVersionCode(assembly.Assembly_Version, +1);

                    if (strForwardVERSION != strAssembly_Version)
                    {
                        //Write AssemblyInfo
                        arystrVersionFileDataRow[nLoc] = "        //__version=" + strLogOutToAssembly;
                        arystrVersionFileDataRow[nLoc + 1] = "        public string Assembly_Version = \"" + strWriteToAssembly + "\";";
                        arystrVersionFileDataRow[nLoc + 2] = "        public string File_Version_Str = \"" + strWriteToAssembly + "\";";

                        System.IO.StreamWriter filewriter = new System.IO.StreamWriter(strVersionFile);
                        string strOutput = String.Join("\r\n", arystrVersionFileDataRow);
                        try
                        {
                            filewriter.Write(strOutput);
                        }
                        finally { filewriter.Close(); }
                    }
                }
            }
        }
        #endregion


        public IniFile(ref MainPage page)
        {
            g_MainPage = page;

            Version = assembly.Assembly_Version;
            Product_DataDir = assembly.Assembly_Product_DataDir;
            ErrorMessageDir = assembly.Assembly_ErrorMessageDir;
            ErrorMessageFileName = assembly.Assembly_ErrorMessageFileName;
            ErrorMessageEngFileName = assembly.Assembly_ErrorMessageEngFileName;
            MachineFileName = assembly.Assembly_MachineFileName;
            DefaultProductFileName = assembly.Assembly_Product_DataDir + assembly.Assembly_DefaultProductFileName;
            LogDir = assembly.Assembly_LogDir;

            Assembly_Copyright = assembly.Assembly_Copyright;
            Assembly_FileDescription = assembly.Assembly_FileDescription;
            Assembly_OriginalFilename = assembly.Assembly_OriginalFilename;
            Assembly_ProductName = assembly.Assembly_ProductName;

            /*
            if (!System.IO.Directory.Exists(Product_DataDir)) System.IO.Directory.CreateDirectory(Product_DataDir);
            if (!System.IO.Directory.Exists(Product_DataDir + ErrorMessageDir)) System.IO.Directory.CreateDirectory(Product_DataDir + ErrorMessageDir);
            if (!System.IO.File.Exists(Product_DataDir + MachineFileName)) System.IO.File.Create(Product_DataDir + MachineFileName);
            if (!System.IO.File.Exists(Product_DataDir + ErrorMessageDir + ErrorMessageFileName)) System.IO.File.Create(Product_DataDir + ErrorMessageDir + ErrorMessageFileName);
            if (!System.IO.File.Exists(Product_DataDir + ErrorMessageDir + ErrorMessageEngFileName)) System.IO.File.Create(Product_DataDir + ErrorMessageDir + ErrorMessageEngFileName);
            if (!System.IO.Directory.Exists(LogDir)) System.IO.Directory.CreateDirectory(LogDir);
            */
        }
    }
}
