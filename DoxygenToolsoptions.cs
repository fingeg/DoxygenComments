

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DoxygenComments
{
    public class DoxygenToolsOptionsGeneral : DialogPage
    {
        bool storeInFile = false;

        [Category("General")]
        [DisplayName("Share format")]
        [Description(
            "If the sharing option is set to true, everyone with this plugin will have the same format for this project. So " +
            "you can force a documentation type or for example a specific licence in the header")]
        public bool StoreInFile
        {
            get { return storeInFile; }
            set { storeInFile = value; }
        }
    }

    public abstract class DoxygenToolsOptionsBase : DialogPage
    {

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        public void SetToDefault()
        {
            // Load the default formats from a recource file
            ComponentResourceManager resources = new ComponentResourceManager(typeof(DoxygenToolsOptionsBase));
            Format = resources.GetString(defaultValueKey);
        }

        protected abstract string defaultValueKey { get; }

        public abstract string Format { get; set; }

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

        string defaultFormat = "";

        public override string Format
        {
            get { return defaultFormat; }
            set { defaultFormat = value; }
        }
    }
}
