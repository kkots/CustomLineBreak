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
	public class MouseProcessor : MouseProcessorBase
	{
		static public IList<MouseProcessor> instances = new List<MouseProcessor>();
		
	    private bool dragging = false;
	    SnapshotPoint selBeforeDragStart = new SnapshotPoint();
	    SnapshotPoint selBeforeDragEnd = new SnapshotPoint();
	    SnapshotPoint selBeforeDragAnchor = new SnapshotPoint();
	    public bool ctrlReleased = false;
	    public bool shiftReleased = false;
		public MouseProcessor(IWpfTextView wpfTextView, System.IServiceProvider serviceProvider) {
			instances.Add(this);
            TextView = wpfTextView;
            ServiceProvider = serviceProvider;
        }
		~MouseProcessor() {
			instances.Remove(this);
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
        private SnapshotPoint mapScreenToBuffer(MouseEventArgs e) {
            System.Windows.Point mousePosRel = e.GetPosition(TextView.VisualElement);
		    double curY = (double)mousePosRel.Y + TextView.ViewportTop;
		    ITextViewLine line = TextView.TextViewLines.GetTextViewLineContainingYCoordinate(curY);
		    if (line == null) return new SnapshotPoint();
		    double curX = (double)mousePosRel.X + 4 + TextView.ViewportLeft;
		    SnapshotPoint selectedChar = new SnapshotPoint();
		    if (curX > line.TextRight) {
		        selectedChar = line.Extent.End;
		    } else {
		        var callResult = line.GetBufferPositionFromXCoordinate(curX, true);
		        if (callResult != null) {
		            selectedChar = (SnapshotPoint)callResult;
		        }
		    }
		    return selectedChar;
        }
        public override void PreprocessMouseLeftButtonUp(MouseButtonEventArgs e) {
            dragging = false;
        }
        public override void PreprocessMouseUp(MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left
                    && e.ButtonState == MouseButtonState.Released) {
                dragging = false;
            }
        }
		public override void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            string ContentTypeName = TextView.TextSnapshot.ContentType.TypeName;
            if (ContentTypeName == "BuildOutput") return;
            
            dragging = false;
			if (e.ChangedButton == MouseButton.Left
			        && e.ButtonState == MouseButtonState.Pressed
			        && TextView.Selection.Mode == TextSelectionMode.Stream) {
			    bool ctrlHeld = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || e.ClickCount == 2;
			    if (!ctrlHeld) return;
			    bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
			    SnapshotPoint selectAnchor = TextView.Selection.AnchorPoint.Position;
			    SnapshotPoint pnt = mapScreenToBuffer(e);
			    if (pnt.Snapshot == null) return;
			    SnapshotSpan wordBounds = WordBoundary.wordBoundsAt(pnt);
			    if (wordBounds.Snapshot == null) return;
			    SnapshotPoint selectStart = TextView.Selection.Start.Position;
			    SnapshotPoint selectEnd = TextView.Selection.End.Position;
		        selBeforeDragStart = selectStart;
		        selBeforeDragEnd = selectEnd;
		        selBeforeDragAnchor = selectAnchor;
		        dragging = true;
		        e.Handled = true;
			    if (TextView.Selection.IsEmpty || !shiftHeld) {
			        TextView.Selection.Select(wordBounds, false);
			        TextView.Caret.MoveTo(wordBounds.End);
    		        selBeforeDragStart = wordBounds.Start;
    		        selBeforeDragEnd = wordBounds.End;
    		        selBeforeDragAnchor = wordBounds.Start;
			        return;
			    }
			    if (wordBounds.Start >= selectStart && wordBounds.End <= selectEnd) {
			        if (selectAnchor == selectEnd) {
			            TextView.Selection.Select(new SnapshotSpan(wordBounds.Start, selectEnd - wordBounds.Start), true);
			            TextView.Caret.MoveTo(wordBounds.Start);
			        } else {
			            TextView.Selection.Select(new SnapshotSpan(selectAnchor, wordBounds.End - selectStart), false);
			            TextView.Caret.MoveTo(wordBounds.End);
			        }
			        return;
			    }
			    SnapshotPoint newSelectionActivePoint = new SnapshotPoint();
			    if (wordBounds.Start < selectAnchor || wordBounds.End < selectAnchor) {
			        selectAnchor = selectEnd;
			        newSelectionActivePoint = wordBounds.Start;
			    } else if (wordBounds.Start > selectAnchor || wordBounds.End > selectAnchor) {
			        selectAnchor = selectStart;
			        newSelectionActivePoint = wordBounds.End;
			    }
			    if (newSelectionActivePoint.Snapshot == null) {
			        return;
			    }
			    if (selectAnchor <= newSelectionActivePoint) {
			        TextView.Selection.Select(new SnapshotSpan(selectAnchor, newSelectionActivePoint), false);
			    } else {
			        TextView.Selection.Select(new SnapshotSpan(newSelectionActivePoint, selectAnchor), true);
			    }
		        TextView.Caret.MoveTo(newSelectionActivePoint);
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
        public override void PreprocessMouseMove(MouseEventArgs e) {
            string ContentTypeName = TextView.TextSnapshot.ContentType.TypeName;
            if (ContentTypeName == "BuildOutput") return;
            
            if (!dragging) return;
            if (e.LeftButton == MouseButtonState.Released) {
                dragging = false;
                return;
            }
		    SnapshotPoint pnt = mapScreenToBuffer(e);
		    if (pnt.Snapshot == null) return;
		    bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool ctrlHeld = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
		    if (!ctrlHeld) {
		    	if (!shiftHeld) return;
		        if (pnt > selBeforeDragAnchor) {
		            TextView.Selection.Select(new SnapshotSpan(selBeforeDragAnchor, pnt), false);
		        } else if (pnt < selBeforeDragAnchor) {
		            TextView.Selection.Select(new SnapshotSpan(pnt, selBeforeDragAnchor), true);
		        } else {
		        	TextView.Selection.Clear();
		        }
	        	TextView.Caret.MoveTo(pnt);
	            e.Handled = true;
	            return;
		    }
		    SnapshotSpan wordBounds = WordBoundary.wordBoundsAt(pnt);
		    if (wordBounds.Snapshot == null) return;
		    if (TextView.Selection.IsEmpty) {
		        TextView.Selection.Select(wordBounds, false);
		        if (pnt < selBeforeDragAnchor) {
		            TextView.Caret.MoveTo(wordBounds.Start);
		        } else {
		            TextView.Caret.MoveTo(wordBounds.End);
		        }
		        e.Handled = true;
		        return;
		    }
	        e.Handled = true;
		    if (wordBounds.Start >= selBeforeDragStart && wordBounds.End <= selBeforeDragEnd) {
		        if (selBeforeDragAnchor == selBeforeDragEnd) {
		            TextView.Selection.Select(new SnapshotSpan(wordBounds.Start, selBeforeDragEnd - wordBounds.Start), true);
		            TextView.Caret.MoveTo(wordBounds.Start);
		        } else {
		            TextView.Selection.Select(new SnapshotSpan(selBeforeDragAnchor, wordBounds.End - selBeforeDragStart), false);
		            TextView.Caret.MoveTo(wordBounds.End);
		        }
		        return;
		    }
		    SnapshotPoint selectAnchor = new SnapshotPoint();
		    SnapshotPoint newSelectionActivePoint = new SnapshotPoint();
		    if (wordBounds.Start < selBeforeDragAnchor || wordBounds.End < selBeforeDragAnchor) {
		        selectAnchor = selBeforeDragEnd;
		        newSelectionActivePoint = wordBounds.Start;
		    } else if (wordBounds.Start > selBeforeDragAnchor || wordBounds.End > selBeforeDragAnchor) {
		        selectAnchor = selBeforeDragStart;
		        newSelectionActivePoint = wordBounds.End;
		    }
		    if (newSelectionActivePoint.Snapshot == null) {
		        return;
		    }
		    if (selectAnchor <= newSelectionActivePoint) {
		        TextView.Selection.Select(new SnapshotSpan(selectAnchor, newSelectionActivePoint), false);
		    } else {
		        TextView.Selection.Select(new SnapshotSpan(newSelectionActivePoint, selectAnchor), true);
		    }
	        TextView.Caret.MoveTo(newSelectionActivePoint);
        }
		/*public override void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            return;
			if (dataValid) {
			    dataValid = false;
			    int newSelectionStart = TextView.Selection.Start.Position;
			    int newSelectionEnd = TextView.Selection.End.Position;
			    if (newSelectionStart > newSelectionEnd) {
			        int temp = newSelectionStart;
			        newSelectionStart = newSelectionEnd;
			        newSelectionEnd = temp;
			    }
			    bool madeChanges = false;
			    if (newSelectionStart < selectAnchor.Position) {
			        newSelectionEnd = selectAnchor.Position;
			        madeChanges = true;
			    } else if (newSelectionEnd > selectAnchor.Position) {
			        newSelectionStart = selectAnchor.Position;
			        madeChanges = true;
			    }
			    if (madeChanges) {
			        TextView.Selection.Select(new SnapshotSpan(TextView.TextSnapshot, newSelectionStart, newSelectionEnd - newSelectionStart),
                        TextView.Caret.Position.BufferPosition.Position < (newSelectionEnd + newSelectionStart) / 2);
                    if (selectAnchor.Position == newSelectionStart) {
                        TextView.Caret.MoveTo(new SnapshotPoint(TextView.TextSnapshot, newSelectionEnd), PositionAffinity.Predecessor);
                    } else {
                        TextView.Caret.MoveTo(new SnapshotPoint(TextView.TextSnapshot, newSelectionStart), PositionAffinity.Successor);
                    }
			    }
			}
        }*/
	}
}
