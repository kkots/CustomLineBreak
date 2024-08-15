using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

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
		private ISignatureHelpBroker signatureHelpBroker;
		private ILightBulbBroker smartTagBroker;
		private IAsyncQuickInfoBroker quickInfoBroker;

		[ImportingConstructor]
		internal KeyFilterFactory(
			ICompletionBroker completionBroker,
			ISignatureHelpBroker signatureHelpBroker,
			ILightBulbBroker smartTagBroker,
			IAsyncQuickInfoBroker quickInfoBroker)
		{
			this.completionBroker = completionBroker;
			this.signatureHelpBroker = signatureHelpBroker;
			this.smartTagBroker = smartTagBroker;
			this.quickInfoBroker = quickInfoBroker;
		}

		public void VsTextViewCreated(IVsTextView viewAdapter)
		{
			var view = editorFactory.GetWpfTextView(viewAdapter);
			if (view == null)
				return;

			IClassifier classifier = classifierAggregatorService.GetClassifier(view.TextBuffer);

			AddCommandFilter(viewAdapter, new KeyFilterImpl(
				completionBroker,
				signatureHelpBroker,
				smartTagBroker,
				quickInfoBroker,
				classifier,
				view,
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
		public void SelectionChangedHandler(object sender, EventArgs e) {
		}
	}
}
