using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DoxygenComments
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(DoxygenToolsOptionsFunction), "Doxygen", "Function", 0, 0, true)]
    [ProvideOptionPage(typeof(DoxygenToolsOptionsHeader), "Doxygen", "Header", 0, 0, true)]
    [ProvideOptionPage(typeof(DoxygenToolsOptionsGeneral), "Doxygen", "General", 0, 0, true)]
    [Guid(PackageGuidString)]
    public sealed class DoxygenCommentsPackage : AsyncPackage
    {
        /// <summary>
        /// DoxygenCommentsPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "90a3ad63-a5d5-4d82-a133-3299feece828";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        public string HeaderFormat
        {
            get
            {
                DoxygenToolsOptionsHeader page = (DoxygenToolsOptionsHeader) GetDialogPage(typeof(DoxygenToolsOptionsHeader));
                return page.Format;
            }
            set
            {
                DoxygenToolsOptionsHeader page = (DoxygenToolsOptionsHeader)GetDialogPage(typeof(DoxygenToolsOptionsHeader));
                page.Format = value;
            }
        }

        public string FunctionFormat
        {
            get
            {
                DoxygenToolsOptionsFunction page = (DoxygenToolsOptionsFunction)GetDialogPage(typeof(DoxygenToolsOptionsFunction));
                return page.Format;
            }
            set
            {
                DoxygenToolsOptionsFunction page = (DoxygenToolsOptionsFunction)GetDialogPage(typeof(DoxygenToolsOptionsFunction));
                page.Format = value;
            }
        }

        public string DefaultFormat
        {
            get
            {
                DoxygenToolsOptionsDefault page = (DoxygenToolsOptionsDefault)GetDialogPage(typeof(DoxygenToolsOptionsDefault));
                return page.Format;
            }
            set
            {
                DoxygenToolsOptionsDefault page = (DoxygenToolsOptionsDefault)GetDialogPage(typeof(DoxygenToolsOptionsDefault));
                page.Format = value;
            }
        }

        #endregion
    }
}
