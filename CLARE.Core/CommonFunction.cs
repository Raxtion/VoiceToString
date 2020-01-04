using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows.Forms;
using Windows.UI.Xaml.Controls;
using System.ComponentModel;
using System.Globalization;

namespace CLARE.Core
{
    namespace CommonFunction
    {
        public static partial class CommonFunction
        {
            public static T DeepClone<T>(this T source)
            {
                if (!typeof(T).IsSerializable)
                {
                    throw new ArgumentException("The type must be serializable.", "source");
                }

                if (source != null)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        var formatter = new BinaryFormatter();
                        formatter.Serialize(stream, source);
                        stream.Seek(0, SeekOrigin.Begin);
                        T clonedSource = (T)formatter.Deserialize(stream);
                        return clonedSource;
                    }
                }
                else
                { return default(T); }
            }

            public static void ChangeLanguageMode(Windows.UI.Xaml.Controls.Page source, int nMode)
            {
                Page objUI = source;
                Type typeUI = objUI.GetType();

                if (nMode == 1) Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("zh-TW");
                else if (nMode == 2) Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
                else if (nMode == 3) Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("");
                else { Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(""); }

                ComponentResourceManager resources = new ComponentResourceManager(typeUI);
                /*
                if (objUI.MainMenuStrip != null)
                {
                    foreach (ToolStripMenuItem item in objUI.MainMenuStrip.Items)
                    {
                        resources.ApplyResources(item, item.Name);
                        foreach (ToolStripMenuItem subItem in item.DropDownItems)
                        {
                            resources.ApplyResources(subItem, subItem.Name);
                        }
                    }
                }
                foreach (Control c in objUI.Controls)
                {
                    try
                    {
                        resources.ApplyResources(c, c.Name);
                    }
                    catch (Exception) { }
                    ChangeCulture(c, typeUI);
                }*/

                resources.ApplyResources(objUI, "$this");
            }

            private static void ChangeCulture(Control control, Type typeUI)
            {
                /*
                Control.ControlCollection controls = control.Controls;
                foreach (Control c in controls)
                {
                    ComponentResourceManager resources = new ComponentResourceManager(typeUI);
                    try
                    {
                        resources.ApplyResources(c, c.Name);
                    }
                    catch (Exception) { }
                    ChangeCulture(c, typeUI);
                }*/
            }

        }
    }
}
