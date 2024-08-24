using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;

namespace CustomLineBreak
{
	using UIElement = System.Windows.UIElement;

	[Export(typeof(IKeyProcessorProvider))]
    [Order]
    [ContentType("text")]
    [Name("KeyEventProcessing")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
	internal class KeyProcessorProvider : IKeyProcessorProvider
	{
		[Import]
        public SVsServiceProvider ServiceProvider
        {
            get;
            private set;
        }

        public KeyProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            if (!(wpfTextView is UIElement))
                return null;

            return new KeyProcessorImpl(wpfTextView, ServiceProvider);
        }
	}
}
