using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace CustomLineBreak
{
	[Export(typeof(IVsTextViewCreationListener))]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	internal class KeyFilterFactory : IVsTextViewCreationListener
	{
		[Import(typeof(IVsEditorAdaptersFactoryService))]
		private IVsEditorAdaptersFactoryService editorFactory;

		[Import]
		private SVsServiceProvider serviceProvider;

		[Import]
		private IClassifierAggregatorService classifierAggregatorService;
		
		private ICompletionBroker completionBroker;
		private ILightBulbBroker smartTagBroker;

		[ImportingConstructor]
		internal KeyFilterFactory(
			ICompletionBroker completionBroker,
			ILightBulbBroker smartTagBroker)
		{
			this.completionBroker = completionBroker;
			this.smartTagBroker = smartTagBroker;
		}

		public void VsTextViewCreated(IVsTextView viewAdapter)
		{
			var view = editorFactory.GetWpfTextView(viewAdapter);
			if (view == null)
				return;

			IClassifier classifier = classifierAggregatorService.GetClassifier(view.TextBuffer);
            
			ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.DTE dte = serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            
			AddCommandFilter(viewAdapter, new KeyFilterImpl(
				completionBroker,
				smartTagBroker,
				classifier,
				view,
				dte,
				serviceProvider));
		}

		private static void AddCommandFilter(IVsTextView viewAdapter, KeyFilter commandFilter)
		{
			if (commandFilter.Added) return;
			//get the view adapter from the editor factory
			var hr = viewAdapter.AddCommandFilter(commandFilter, out IOleCommandTarget next);

			if (hr != VSConstants.S_OK) return;
			commandFilter.Added = true;
			//you'll need the next target for Exec and QueryStatus
			if (next != null)
				commandFilter.NextTarget = next;
		}
	}
}
