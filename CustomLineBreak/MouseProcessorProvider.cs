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

	[Export(typeof(IMouseProcessorProvider))]
    [Order]
    [ContentType("text")]
    [Name("MouseNavigation")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
	internal class MouseProcessorProvider : IMouseProcessorProvider
	{
        [Import]
        public SVsServiceProvider ServiceProvider
        {
            get;
            private set;
        }

        public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            if (!(wpfTextView is UIElement))
                return null;

            return new MouseProcessor(wpfTextView, ServiceProvider);
        }
	}
}
