

using EnvDTE;
using EnvDTE80;
using Microsoft.SqlServer.Server;
using Microsoft.VisualBasic.Logging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DoxygenComments
{

    public abstract class DoxygenToolsOptionsBase : DialogPage
    {
        SettingsHelper settings = new SettingsHelper();

        public void SetToDefault()
        {
            // Load the default formats from a recource file
            ComponentResourceManager resources = new ComponentResourceManager(typeof(DoxygenToolsOptionsBase));
            Format = resources.GetString(defaultValueKey);
            SaveSettingsToStorage();
        }

        protected abstract string defaultValueKey { get; }
        protected abstract string registryKey { get; }
        public abstract string[] additionalKeys { get; }

        public abstract string Format { get; set; }


        public override void SaveSettingsToStorage()
        {
            settings.SetFormat(registryKey, Format);
        }

        public override void LoadSettingsFromStorage()
        {
            Format = settings.GetFormat(registryKey, defaultValueKey);
        }

        [Browsable(false)]
        protected override IWin32Window Window
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                DTE dte = (DTE)GetService(typeof(SDTE));
                DoxygenToolsOptionsControl page = new DoxygenToolsOptionsControl(this, dte);
                return page;
            }
        }
    }

    [Guid("DD41AF05-A916-426F-8E39-4EA8155D6DDD")]
    public class DoxygenToolsOptionsHeader : DoxygenToolsOptionsBase
    {
        protected override string defaultValueKey => "header";
        protected override string registryKey => SettingsHelper.HeaderPage;
        public override string[] additionalKeys => new string[] { };

        string headerFormat = "";

        public override string Format
        {
            get { return headerFormat; }
            set { headerFormat = value; }
        }
    }

    [Guid("94E06251-750A-401B-AA80-5803EADC25F3")]
    public class DoxygenToolsOptionsFunction : DoxygenToolsOptionsBase
    {
        protected override string defaultValueKey => "functions";
        protected override string registryKey => SettingsHelper.FunctionPage;
        public override string[] additionalKeys => new string[] { "$FUNCTION_NAME" , "$FUNCTION_TYPE" };

        string functionFormat = "";

        public override string Format
        {
            get { return functionFormat; }
            set { functionFormat = value; }
        }
    }

    [Guid("862F6AB3-AA53-40BB-BBF9-B2CEAF34E6AC")]
    public class DoxygenToolsOptionsDefault : DoxygenToolsOptionsBase
    {
        protected override string defaultValueKey => "default";
        protected override string registryKey => SettingsHelper.DefaultPage;
        public override string[] additionalKeys => new string[] { };

        string defaultFormat = "";

        public override string Format
        {
            get { return defaultFormat; }
            set { defaultFormat = value; }
        }
    }

    class SettingsHelper
    {
        private const string RegistryPath = @"ApplicationPrivateSettings\DoxygenComments\";
        public const string DefaultPage = "DoxygenToolsOptionsDefault";
        public const string FunctionPage = "DoxygenToolsOptionsFunction";
        public const string HeaderPage = "DoxygenToolsOptionsHeader";
        private WritableSettingsStore settings;
        private ComponentResourceManager resourceManager;

        public SettingsHelper()
        {
            settings = GetSettings();
            resourceManager = new ComponentResourceManager(typeof(DoxygenToolsOptionsBase));
        }

        public string DefaultFormat
        {
            get
            {
                return GetFormat(DefaultPage, "default");
            }
            set
            {
                SetFormat(DefaultPage, value);
            }
        }

        public string HeaderFormat
        {
            get
            {
                return GetFormat(HeaderPage, "header");
            }
            set
            {
                SetFormat(HeaderPage, value);
            }
        }

        public string FunctionFormat
        {
            get
            {
                return GetFormat(FunctionPage, "functions");
            }
            set
            {
                SetFormat(FunctionPage, value);
            }
        }

        public string GetFormat(string registryKey, string resourceKey)
        {
            string format = "";
            try
            {
                format = settings.GetString(RegistryPath + registryKey, "Format").Replace("0*System.String*", "");
            } catch {
                SetFormat(registryKey, resourceManager.GetString(resourceKey));
            }

            if (format == null || format.ToLower() == "test" || format.Length == 0)
            {
                return resourceManager.GetString(resourceKey);
            }
            return format;
        }

        public void SetFormat(string registryKey, string format)
        {
            try
            {
                settings.SetString(RegistryPath + registryKey, "Format", format);
            } catch
            {
                settings.CreateCollection(RegistryPath + registryKey);
            }
        }

        private WritableSettingsStore GetSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            return settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }
    }

}
