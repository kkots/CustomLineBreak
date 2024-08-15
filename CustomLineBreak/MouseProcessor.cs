using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
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
	internal class MouseProcessor : MouseProcessorBase
	{
		public MouseProcessor(IWpfTextView wpfTextView, System.IServiceProvider serviceProvider) {
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
		 public override void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
			if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed
					&& (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
					&& (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
					&& !TextView.Selection.IsEmpty
					&& TextView.Selection.IsReversed
					&& TextView.Selection.Mode == TextSelectionMode.Stream) {
				TextView.Selection.Select(TextView.Selection.SelectedSpans[0], false);
			}

			// Some nifty code that injects a command into shell
			/*try
            {
                IVsUIShell shell = (IVsUIShell)ServiceProvider.GetService(typeof(SVsUIShell));
                Guid cmdGroup = VSConstants.GUID_VSStandardCommandSet97;
                OLECMDEXECOPT cmdExecOpt = OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER;
                object obj = null;
                ErrorHandler.ThrowOnFailure(shell.PostExecCommand(cmdGroup, (uint)VSConstants.VSStd97CmdID.ShellNavBackward, (uint)cmdExecOpt, ref obj));
                e.Handled = true;
            }
            catch (COMException)
            {
            }*/
        }
	}
}
