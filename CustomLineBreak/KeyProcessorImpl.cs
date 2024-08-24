using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace CustomLineBreak
{
	internal class KeyProcessorImpl : KeyProcessor
	{
		public KeyProcessorImpl(IWpfTextView wpfTextView, System.IServiceProvider serviceProvider) {
            TextView = wpfTextView;
            ServiceProvider = serviceProvider;
        }
		
        private IWpfTextView TextView
        {
            get;
            set;
        }
		
        private System.IServiceProvider ServiceProvider
        {
            get;
            set;
        }
        public override void KeyUp(KeyEventArgs e) {
        	bool ctrlHeld = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        	bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        	foreach (MouseProcessor p in MouseProcessor.instances) {
        		if (!ctrlHeld) p.ctrlReleased = true;
        		if (!shiftHeld) p.shiftReleased = true;
        	}
        }
	}
}
