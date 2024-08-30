using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using IServiceProvider = System.IServiceProvider;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Formatting;

namespace CustomLineBreak
{
	internal class KeyFilterImpl : KeyFilter
	{
		private IClassifier classifier;
	    private EnvDTE.DTE dte;

		public KeyFilterImpl(
				ICompletionBroker completionBroker,
				ILightBulbBroker smartTagBroker,
				IClassifier classifier,
				IWpfTextView textView,
				EnvDTE.DTE dte,
				IServiceProvider provider)
			: base(completionBroker, smartTagBroker, textView, provider)
		{
		    this.dte = dte;
			this.classifier = classifier;
		}

		private char GetTypeChar(IntPtr pvaIn)
		{
			return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
		}

		private Boolean canPerform() {
			return !IsInAutomationFunction
				&& !IsCompletionActive;
		}

		private IClassificationType getClassOfChar(SnapshotPoint point) {
			IList<ClassificationSpan> classificationSpans = classifier.GetClassificationSpans(new SnapshotSpan(point, 1));
			if (classificationSpans.Count > 0) {
				return classificationSpans[0].ClassificationType;
			}
			return null;
		}
		private enum BracketType {
			ROUND,
			SQUARE,
			CURLY,
			TRIANGLE
		};

		private void collapseUntilLastBracketOfType(IList<BracketType> bracketStack, BracketType type) {
			for (int i = bracketStack.Count - 1; i >= 0; --i) {
				BracketType currentType = bracketStack[i];
				bracketStack.RemoveAt(i);
				if (currentType == type) {
					break;
				}
			}
		}
		private SnapshotPoint findOuterOpeningParenthesis(SnapshotPoint point) {
			int pos = point.Position;
			if (pos == 0) return new SnapshotPoint();
			ITextSnapshot buf = point.Snapshot;
			--pos;
			ITextSnapshotLine line = point.GetContainingLine();
			int lineNumber = line.LineNumber;
			bool isFirstLine = true;
			// we can ignore the < > template specialization brackets because () cannot be inside them
			IList<BracketType> bracketStack = new List<BracketType>();
			while (lineNumber >= 0) {
				LineCharCollection lineChars = null;
				if (isFirstLine) {
					lineChars = new LineCharCollection(classifier, new SnapshotSpan(line.Extent.Start, point));
				} else {
					lineChars = new LineCharCollection(classifier, line.Extent);
				}
				isFirstLine = false;
				for (int i = lineChars.Count - 1; i >= 0; --i) {
					LineCharElement elem = lineChars[i];
					char c = elem.c;
					if (elem.isComment || char.IsWhiteSpace(elem.c) || elem.isString) continue;
					if (c == ')') {
						bracketStack.Add(BracketType.ROUND);
					} else if (c == ']') {
						bracketStack.Add(BracketType.SQUARE);
					} else if (c == '}') {
						bracketStack.Add(BracketType.CURLY);
					} else if (c == '(') {
						if (bracketStack.Count == 0) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.ROUND);
					} else if (c == '[') {
						if (bracketStack.Count == 0) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.SQUARE);
					} else if (c == '{') {
						if (bracketStack.Count == 0) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.CURLY);
					}
				}
				--lineNumber;
				if (lineNumber >= 0) {
					line = buf.GetLineFromLineNumber(lineNumber);
				}
			}
			return new SnapshotPoint();
		}
		private SnapshotPoint findOpeningParenthesis(SnapshotPoint point, char brace) {
			int pos = point.Position;
			if (pos == 0) return new SnapshotPoint();
			ITextSnapshot buf = point.Snapshot;
			--pos;
			bool enableTriangleBrackets = false;
			if (brace == '}') brace = '{';
			if (brace == ')') brace = '(';
			if (brace == ']') brace = '[';
			if (brace == '>') {
				brace = '<';
				enableTriangleBrackets = true;
			}
			ITextSnapshotLine line = point.GetContainingLine();
			int lineNumber = line.LineNumber;
			bool isFirstLine = true;
			// we can ignore the < > template specialization brackets because () cannot be inside them
			IList<BracketType> bracketStack = new List<BracketType>();
			while (lineNumber >= 0) {
				LineCharCollection lineChars = null;
				if (isFirstLine) {
					lineChars = new LineCharCollection(classifier, new SnapshotSpan(line.Extent.Start, point));
				} else {
					lineChars = new LineCharCollection(classifier, line.Extent);
				}
				isFirstLine = false;
				for (int i = lineChars.Count - 1; i >= 0; --i) {
					LineCharElement elem = lineChars[i];
					char c = elem.c;
					if (elem.isComment || char.IsWhiteSpace(elem.c) || elem.isString) continue;
					if (c == ')') {
						bracketStack.Add(BracketType.ROUND);
					} else if (c == ']') {
						bracketStack.Add(BracketType.SQUARE);
					} else if (c == '}') {
						bracketStack.Add(BracketType.CURLY);
					} else if (enableTriangleBrackets && c == '>') {
						bracketStack.Add(BracketType.TRIANGLE);
					} else if (c == '(') {
						if (bracketStack.Count == 0 && c == brace) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.ROUND);
					} else if (c == '[') {
						if (bracketStack.Count == 0 && c == brace) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.SQUARE);
					} else if (c == '{') {
						if (bracketStack.Count == 0 && c == brace) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.CURLY);
					} else if (enableTriangleBrackets && c == '<') {
						if (bracketStack.Count == 0 && c == brace) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.TRIANGLE);
					}
				}
				--lineNumber;
				if (lineNumber >= 0) {
					line = buf.GetLineFromLineNumber(lineNumber);
				}
			}
			return new SnapshotPoint();
		}
		private SnapshotPoint findClosingParenthesis(SnapshotPoint point, char brace) {
			int pos = point.Position;
			ITextSnapshot buf = point.Snapshot;
			if (pos == buf.Length - 1) return new SnapshotPoint();
			++pos;
			bool enableTriangleBrackets = false;
			if (brace == '{') brace = '}';
			if (brace == '(') brace = ')';
			if (brace == '[') brace = ']';
			if (brace == '<') {
				brace = '>';
				enableTriangleBrackets = true;
			}
			ITextSnapshotLine line = point.GetContainingLine();
			int lineNumber = line.LineNumber;
			bool isFirstLine = true;
			// we can ignore the < > template specialization brackets because () cannot be inside them
			IList<BracketType> bracketStack = new List<BracketType>();
			while (lineNumber < buf.LineCount) {
				LineCharCollection lineChars = null;
				if (isFirstLine) {
					lineChars = new LineCharCollection(classifier, new SnapshotSpan(point + 1, line.Extent.End));
				} else {
					lineChars = new LineCharCollection(classifier, line.Extent);
				}
				isFirstLine = false;
				for (int i = 0; i < lineChars.Count; ++i) {
					LineCharElement elem = lineChars[i];
					if (elem.isComment || char.IsWhiteSpace(elem.c) || elem.isString) continue;
					char c = elem.c;
					if (c == '(') {
						bracketStack.Add(BracketType.ROUND);
					} else if (c == '[') {
						bracketStack.Add(BracketType.SQUARE);
					} else if (c == '{') {
						bracketStack.Add(BracketType.CURLY);
					} else if (enableTriangleBrackets && c == '<') {
						bracketStack.Add(BracketType.TRIANGLE);
					} else if (c == ')') {
						if (bracketStack.Count == 0 && c == brace) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.ROUND);
					} else if (c == ']') {
						if (bracketStack.Count == 0 && c == brace) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.SQUARE);
					} else if (c == '}') {
						if (bracketStack.Count == 0 && c == brace) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.CURLY);
					} else if (enableTriangleBrackets && c == '>') {
						if (bracketStack.Count == 0 && c == brace) return elem.point;
						collapseUntilLastBracketOfType(bracketStack, BracketType.TRIANGLE);
					}
				}
				++lineNumber;
				if (lineNumber < buf.LineCount) {
					line = buf.GetLineFromLineNumber(lineNumber);
				}
			}
			return new SnapshotPoint();
		}

		private SnapshotPoint findOpeningParenthesis(SnapshotPoint point) {
			int pos = point.Position;
			if (pos == 0) return new SnapshotPoint();
			ITextSnapshot buf = point.Snapshot;
			if (pos >= buf.Length) return new SnapshotPoint();
			char brace = buf[pos];
			return findOpeningParenthesis(point, brace);
		}

		private SnapshotPoint findClosingParenthesis(SnapshotPoint point) {
			int pos = point.Position;
			ITextSnapshot buf = point.Snapshot;
			if (pos == buf.Length - 1) return new SnapshotPoint();
			char brace = buf[pos];
			return findClosingParenthesis(point, brace);
		}
        
        private bool isComment(IClassificationType type) {
            return type.IsOfType("comment") || type.IsOfType("XML Doc Comment");
        }
        
		private SnapshotPoint findClosingParenthesisToTheLeftFromHere(SnapshotPoint point) {
			int pos = point.Position;
			if (pos == 0) return new SnapshotPoint();
			--pos;
			ITextSnapshot buf = point.Snapshot;
			ITextSnapshotLine line = point.GetContainingLine();
			int lineNumber = line.LineNumber;

			bool insideMultilineComment = false;
			bool potentiallyInsideMultilineComment = false;

			while (pos >= 0) {
				char c = buf[pos];
				--pos;
				if (c == '\n') {
					pos += 2;
					break;
				}
				if (char.IsWhiteSpace(c)) continue;
				if (insideMultilineComment) {
					if (c == '*') {
						if (pos > 0) {
							char prevC = buf[pos];
							if (prevC == '/') {
								insideMultilineComment = false;
								potentiallyInsideMultilineComment = true;
								--pos;
								continue;
							}
						}
					}
				} else {
					if (c == '/') {
						char prevC = buf[pos];
						if (prevC == '*') {
							IClassificationType type = getClassOfChar(new SnapshotPoint(buf, pos));
							if (type != null && isComment(type)) {
								insideMultilineComment = true;
								--pos;
								continue;
							}
						}
					}
					if (potentiallyInsideMultilineComment) {
						potentiallyInsideMultilineComment = false;
						IClassificationType type = getClassOfChar(new SnapshotPoint(buf, pos));
						insideMultilineComment = type != null && isComment(type);
					}
					if (insideMultilineComment) continue;
					if (c == ')' || c == ']') return new SnapshotPoint(buf, pos + 1);
					return new SnapshotPoint();
				}
			}
			while (pos >= 0 && lineNumber > 0) {
				// things get a little complicated here, because the previous line could contain a // comment
				// and we can only know about it when we get to it
				// and /* // comment */ can still close comments before the line ends
				// and /* /*  */ can contain fake-out multiline comment starts so we're never sure when we
				// have left a multiline comment.
				// So we just classify the whole line and get it to tell us what is a comment and what is not.
				--lineNumber;
				line = buf.GetLineFromLineNumber(lineNumber);
				LineCharCollection lineChars = new LineCharCollection(classifier, line.Extent);
				foreach (LineCharElement elem in lineChars) {
					if (elem.isComment || char.IsWhiteSpace(elem.c)) continue;
					if (elem.c == ')' || elem.c == ']') return elem.point;
					return new SnapshotPoint();
				}
			}
			return new SnapshotPoint();
		}
		private string getNameExtension(string name) {
		    int index = name.LastIndexOf('.');
		    if (index == -1) return string.Empty;
		    return name.Substring(index + 1);
		}
		public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			string ContentTypeName = TextView.TextSnapshot.ContentType.TypeName;
			string nameExt = getNameExtension(dte.ActiveDocument.Name).ToUpper();
			bool isCpp = ContentTypeName == "C/C++"
				|| ContentTypeName == "CSharp"
				|| ContentTypeName == "code++.Java"
				|| ContentTypeName == "JSON"
				|| nameExt == "RON";
			// Enter key
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN && canPerform()) {
				bool handled = handleReturn(isCpp);
				if (handled) return VSConstants.S_OK;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Closing }
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR && isCpp && canPerform()) {
				char typedChar = GetTypeChar(pvaIn);
				if (typedChar == '}') {
					handleTypeClosingBrace(typedChar);
				}
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Ctrl+Left
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.WORDPREV && canPerform()) {
				bool handled = handleCtrlLeft();
				if (handled) return VSConstants.S_OK;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Shift+Ctrl+Left
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.WORDPREV_EXT && canPerform()) {
				bool handled = handleCtrlShiftLeft();
				if (handled) return VSConstants.S_OK;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Ctrl+Right
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.WORDNEXT && canPerform()) {
				bool handled = handleCtrlRight();
				if (handled) return VSConstants.S_OK;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Shift+Ctrl+Right
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.WORDNEXT_EXT && canPerform()) {
				bool handled = handleCtrlShiftRight();
				if (handled) return VSConstants.S_OK;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Ctrl+]
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.GOTOBRACE && isCpp && canPerform()) {
				bool handled = handleCtrlSquareBracket(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut, out int handlingResult);
				if (handled) return handlingResult;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Shift+Ctrl+]
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.GOTOBRACE_EXT && canPerform()) {
				bool handled = handleCtrlShiftSquareBracket2(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut, out int handlingResult);
				if (handled) return handlingResult;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Ctrl+Delete
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.DELETEWORDRIGHT && canPerform()) {
				bool handled = handleCtrlDelete();
				if (handled) return VSConstants.S_OK;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Ctrl+Backspace
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.DELETEWORDLEFT && canPerform()) {
				bool handled = handleCtrlBackspace();
				if (handled) return VSConstants.S_OK;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Tab
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.ECMD_TAB && canPerform()) {
				bool handled = handleTab();
				if (handled) return VSConstants.S_OK;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

            // Shift+Tab
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKTAB && canPerform()) {
				bool handled = handleShiftTab();
				if (handled) return VSConstants.S_OK;
				return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

			return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return NextTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}

		private void ReplaceVirtualSpaces()
		{
			if (Caret.InVirtualSpace)
			{
				TextView.TextBuffer.Insert(Caret.Position.BufferPosition, new string(' ', Caret.Position.VirtualSpaces));
				Caret.MoveTo(CaretLine.End);
			}
		}
		private bool hasBoxSelection() {
			return TextView.Selection.Mode != TextSelectionMode.Stream
					&& !TextView.Selection.IsEmpty
					|| TextView.Selection.Mode == TextSelectionMode.Stream
					&& !TextView.Selection.IsEmpty
					&& TextView.Selection.SelectedSpans.Count != 1;
		}
		private bool noEditAccessOrHasBoxSelection() {
			return !TextView.TextBuffer.CheckEditAccess()
					|| hasBoxSelection();
		}
		private SnapshotPoint getIndentOriginOfOpeningBrace(SnapshotPoint bracePosition) {
			ITextSnapshot buf = bracePosition.Snapshot;
			SnapshotPoint pointToIndentFrom = bracePosition;
			if (buf[bracePosition.Position] == '{') {
				SnapshotPoint parenthesisClose = findClosingParenthesisToTheLeftFromHere(bracePosition);
				if (parenthesisClose.Snapshot != null) {
					SnapshotPoint parenthesisOpen = findOpeningParenthesis(parenthesisClose);
					if (parenthesisOpen.Snapshot != null) {
						pointToIndentFrom = parenthesisOpen;
					}
				}
			}
			return pointToIndentFrom;
		}
		// Returns the position of the opening brace
		SnapshotPoint lineEndsWithUnclosedBrace(LineCharCollection lineChars) {
		    SnapshotPoint bracePosition = new SnapshotPoint();
		    IList<BracketType> bracketStack = new List<BracketType>();
			foreach (LineCharElement lineChar in lineChars) {
				if (lineChar.isComment || lineChar.isString || char.IsWhiteSpace(lineChar.c)) {
					continue;
				}
				char c = lineChar.c;
				if (c == '{') {
					bracePosition = lineChar.point;
					bracketStack.Add(BracketType.CURLY);
				} else if (c == '[') {
					bracePosition = lineChar.point;
					bracketStack.Add(BracketType.SQUARE);
				} else if (c == '(') {
					bracePosition = lineChar.point;
					bracketStack.Add(BracketType.ROUND);
				} else if (c == '}') {
					collapseUntilLastBracketOfType(bracketStack, BracketType.CURLY);
				} else if (c == ']') {
					collapseUntilLastBracketOfType(bracketStack, BracketType.SQUARE);
				} else if (c == ')') {
					collapseUntilLastBracketOfType(bracketStack, BracketType.ROUND);
				}
			}
			if (bracketStack.Count > 0) {
			    return bracePosition;
			} else {
			    return new SnapshotPoint();
			}
		}
		
		// Returns position of the end of previous statement
		SnapshotPoint findPreviousStatementEnd(SnapshotPoint thisStatementSemicolonPos) {
		    IList<BracketType> bracketStack = new List<BracketType>();
		    bool isFirstLine = true;
		    int lineNumber = thisStatementSemicolonPos.GetContainingLineNumber() + 1;
		    ITextSnapshot buf = thisStatementSemicolonPos.Snapshot;
            bool encounteredAnything = false;
		    while (lineNumber > 0) {
		        bool isFirstLineCurrent = isFirstLine;
		        isFirstLine = false;
		        --lineNumber;
		        ITextSnapshotLine line;
		        LineCharCollection lineChars;
		        if (isFirstLineCurrent) {
		            line = thisStatementSemicolonPos.GetContainingLine();
			        lineChars = new LineCharCollection(classifier,
				        new SnapshotSpan(buf,
					        line.Start,
					        thisStatementSemicolonPos.Position - line.Start));
		        } else {
		            line = buf.GetLineFromLineNumber(lineNumber);
		            lineChars = new LineCharCollection(classifier, line.Extent);
                }
                for (int i = lineChars.Count - 1; i >= 0; --i) {
		            LineCharElement lineChar = lineChars[i];
		            char c = lineChar.c;
		            bool isWhitespace = char.IsWhiteSpace(c);
		            if (!lineChar.isComment && !lineChar.isString && !isWhitespace) {
		                if (bracketStack.Count == 0 && (c == ';' || c == ':' && i > 0 && lineChars[i - 1].c != ':')) {
		                    return lineChar.point;
		                }
				        if (c == '{') {
					        if (bracketStack.Count == 0) return lineChar.point;
					        collapseUntilLastBracketOfType(bracketStack, BracketType.CURLY);
				        } else if (c == '[') {
					        if (bracketStack.Count == 0) return lineChar.point;
					        collapseUntilLastBracketOfType(bracketStack, BracketType.SQUARE);
				        } else if (c == '(') {
					        if (bracketStack.Count == 0) return lineChar.point;
					        collapseUntilLastBracketOfType(bracketStack, BracketType.ROUND);
				        } else if (c == '}') {
				            if (isFirstLineCurrent && !encounteredAnything) {
				                bracketStack.Add(BracketType.CURLY);
				            } else {
				                return lineChar.point;
				            }
				        } else if (c == ']') {
					        bracketStack.Add(BracketType.SQUARE);
				        } else if (c == ')') {
					        bracketStack.Add(BracketType.ROUND);
				        }
				        if (c == ':' && i > 0 && lineChars[i - 1].c == ':') --i;
		            }
		            if (!encounteredAnything && !lineChar.isComment && !isWhitespace) encounteredAnything = true;
                }
		    }
		    return new SnapshotPoint();
		}
		private string generateNewIndent() {
			if (ConvertTabsToSpaces || IndentSize % TabSize != 0) {
				return new string(' ', IndentSize);
			} else {
				return new string('\t', IndentSize / TabSize);
			}
		}
		private string getIndentOfLineUntilPoint(ITextSnapshotLine line, SnapshotPoint point) {
			int end = point.Position;
			ITextSnapshot buf = TextView.TextSnapshot;
			for (int i = line.Start.Position; i < end; ++i) {
				char c = buf[i];
				if (char.IsWhiteSpace(c)) {
					// whitespace
					continue;
				}
				end = i;
				break;
			}
			if (end > line.Start.Position) {
				return buf.GetText(new Span(line.Start.Position,
					end - line.Start.Position));
			} else {
				return "";
			}
		}
		private string getIndentOfLineUntilPoint(SnapshotPoint point) {
			ITextSnapshotLine line = point.GetContainingLine();
			return getIndentOfLineUntilPoint(line, point);
		}
		/// <summary>
		/// This function requires a valid opening brace ({, ( or [) position to be provded in -bracePosition-.
        /// First off, it deletes the current selection of the Text View. Then, inserts a newline ("\n"),
        /// at the caret position. Then, calculates the needed indent based on the provided opening brace,
        /// and inserts that.
        /// </summary>
        /// <param name="bracePosition">Position of the opening brace to indent from. Newline will be inserted
        /// at the position of the caret.</param>
		private void insertNewLineWithIndentCalculatedFromUnclosedBrace(SnapshotPoint bracePosition) {
		    SnapshotPoint pointToIndentFrom = getIndentOriginOfOpeningBrace(bracePosition);
			string indent = getIndentOfLineUntilPoint(pointToIndentFrom);
			replaceSelectionWithText("\n" + indent + generateNewIndent());
		}
		private void replaceSelectionWithText(string newText) {
			SnapshotPoint pointEnd;
			if (TextView.Selection.IsEmpty) {
				pointEnd = TextView.Caret.Position.BufferPosition;
			} else if (TextView.Selection.IsReversed) {
				pointEnd = TextView.Selection.AnchorPoint.Position;
			} else {
				pointEnd = TextView.Selection.ActivePoint.Position;
			}
			ITextEdit textEdit = TextView.TextBuffer.CreateEdit();
			if (!TextView.Selection.IsEmpty) {
				textEdit.Delete(TextView.Selection.SelectedSpans[0]);
			}
			textEdit.Insert(pointEnd, newText);
			textEdit.Apply();
			if (!TextView.Selection.IsEmpty) {
				TextView.Selection.Clear();
			}
			TextView.Caret.EnsureVisible();
		}
		private SnapshotPoint findNextStatementStart(SnapshotPoint prevStatementEnd) {
		    bool isFirstLine = true;
		    int lineNumber = prevStatementEnd.GetContainingLineNumber() - 1;
		    ITextSnapshot buf = prevStatementEnd.Snapshot;
		    while (lineNumber < buf.LineCount - 1) {
		        ++lineNumber;
		        ITextSnapshotLine line;
		        LineCharCollection lineChars;
		        if (isFirstLine) {
		            line = prevStatementEnd.GetContainingLine();
			        lineChars = new LineCharCollection(classifier,
				        new SnapshotSpan(buf,
					        prevStatementEnd.Position + 1,
					        line.End - prevStatementEnd.Position - 1));
			        isFirstLine = false;
		        } else {
		            line = buf.GetLineFromLineNumber(lineNumber);
		            lineChars = new LineCharCollection(classifier, line.Extent);
                }
                foreach (LineCharElement lineChar in lineChars) {
		            if (lineChar.isComment || char.IsWhiteSpace(lineChar.c)) continue;
		            return lineChar.point;
                }
		    }
		    return new SnapshotPoint();
		}
		private bool lineStartsWithTripleSlashUpToPoint(ITextSnapshotLine line, SnapshotPoint point) {
		    ITextSnapshot buf = TextView.TextSnapshot;
		    int end = point.Position;
		    for (int i = line.Extent.Start; i < end; ++i) {
		        char c = buf[i];
		        if (char.IsWhiteSpace(c)) continue;
		        if (c == '/' && i < end - 1 && buf[i + 1] == '/' && i < end - 2 && buf[i + 2] == '/') {
		            return true;
		        }
		        return false;
		    }
	        return false;
		}
		private string getWhitespaceAfterTripleSlashUpToPoint(ITextSnapshotLine line, SnapshotPoint point) {
		    ITextSnapshot buf = TextView.TextSnapshot;
		    int end = point.Position;
		    for (int i = line.Extent.Start; i < end; ++i) {
		        char c = buf[i];
		        if (char.IsWhiteSpace(c)) continue;
		        if (c == '/' && i < end - 1 && buf[i + 1] == '/' && i < end - 2 && buf[i + 2] == '/') {
		            i += 3;
		            int whitespaceStart = i;
		            int whitespaceEnd = i;
		            for (int j = i; j < end; ++j) {
		                c = buf[j];
		                if (!char.IsWhiteSpace(c)) {
		                    whitespaceEnd = j;
		                    break;
		                }
		            }
		            if (whitespaceEnd != whitespaceStart) {
		                return buf.GetText(whitespaceStart, whitespaceEnd - whitespaceStart);
		            }
		            return "";
		        }
		        return "";
		    }
	        return "";
		}
		
		private bool handleReturn(bool isCpp) {
			ITextSnapshot buf = null;
			if (noEditAccessOrHasBoxSelection()) {
				return false;
			}
			SnapshotPoint point;
			SnapshotPoint pointEnd;
			if (TextView.Selection.IsEmpty) {
				point = TextView.Caret.Position.BufferPosition;
				pointEnd = point;
			} else if (TextView.Selection.IsReversed) {
				point = TextView.Selection.ActivePoint.Position;
				pointEnd = TextView.Selection.AnchorPoint.Position;
			} else {
				point = TextView.Selection.AnchorPoint.Position;
				pointEnd = TextView.Selection.ActivePoint.Position;
			}
			ITextSnapshotLine pointLine = point.GetContainingLine();
			LineCharCollection lineChars = null;
			SnapshotPoint bracePosition = new SnapshotPoint();
			if (isCpp) {
			    lineChars = new LineCharCollection(classifier,
				    new SnapshotSpan(TextView.TextSnapshot,
					    pointLine.Start,
					    point.Position - pointLine.Start));
			    bracePosition = lineEndsWithUnclosedBrace(lineChars);
			}
			buf = TextView.TextSnapshot;
			if (bracePosition.Snapshot != null) {
				insertNewLineWithIndentCalculatedFromUnclosedBrace(bracePosition);
				return true;
			}
			SnapshotPoint semicolonPos = new SnapshotPoint();
			bool lastIsSemicolon = false;
			if (isCpp) {
			    foreach (LineCharElement lineChar in lineChars) {
			        if (lineChar.isComment || char.IsWhiteSpace(lineChar.c)) continue;
			        lastIsSemicolon = lineChar.c == ';';
			        if (lastIsSemicolon) {
			            semicolonPos = lineChar.point;
			        }
			    }
			}
			if (lastIsSemicolon) {
			    SnapshotPoint prevStatementEnd = findPreviousStatementEnd(semicolonPos);
			    if (prevStatementEnd.Snapshot != null) {
			        if (prevStatementEnd.GetContainingLineNumber() == pointLine.LineNumber) {
			            lineChars = new LineCharCollection(classifier,
				            new SnapshotSpan(buf,
					            pointLine.Start,
					            prevStatementEnd.Position - pointLine.Start + 1));
			        } else {
			            lineChars = new LineCharCollection(classifier, prevStatementEnd.GetContainingLine().Extent);
			        }
			        bracePosition = lineEndsWithUnclosedBrace(lineChars);
			        if (bracePosition.Snapshot != null) {
			            insertNewLineWithIndentCalculatedFromUnclosedBrace(bracePosition);
			            return true;
			        }
			        SnapshotPoint statementStart = findNextStatementStart(prevStatementEnd);
			        if (statementStart.Snapshot != null) {
			            replaceSelectionWithText("\n" + getIndentOfLineUntilPoint(statementStart));
			            return true;
			        }
			    }
			}
			if (isCpp && lineStartsWithTripleSlashUpToPoint(pointLine, point)) {
			    replaceSelectionWithText("\n" + getIndentOfLineUntilPoint(pointLine, pointLine.End) + "///" + getWhitespaceAfterTripleSlashUpToPoint(pointLine, point));
			    return true;
			}
			replaceSelectionWithText("\n" + getIndentOfLineUntilPoint(pointLine, pointLine.End));
			return true;
		}
		private void handleTypeClosingBrace(char typedChar) {
			ITextSnapshot buf = TextView.TextSnapshot;
			if (noEditAccessOrHasBoxSelection()) return;
			
			SnapshotPoint point;
			SnapshotPoint pointEnd;
			if (TextView.Selection.IsEmpty) {
				point = TextView.Caret.Position.BufferPosition;
				pointEnd = point;
			} else if (TextView.Selection.IsReversed) {
				point = TextView.Selection.ActivePoint.Position;
				pointEnd = TextView.Selection.AnchorPoint.Position;
			} else {
				point = TextView.Selection.AnchorPoint.Position;
				pointEnd = TextView.Selection.ActivePoint.Position;
			}
			ITextSnapshotLine pointLine = point.GetContainingLine();
			IClassificationType type = getClassOfChar(point);
			if (type != null && isComment(type)) {
				return;
			}

			if(buf.GetText(pointLine.Extent).Substring(0,
						point - pointLine.Extent.Start
					).TrimStart().Length != 0) {
				return;
			}
			SnapshotPoint parenthesisOpen = findOpeningParenthesis(TextView.Caret.Position.BufferPosition, typedChar);
			if (parenthesisOpen.Snapshot == null) return;
			SnapshotPoint pointToIndentFrom = getIndentOriginOfOpeningBrace(parenthesisOpen);
			ITextSnapshotLine indentOriginLine = pointToIndentFrom.GetContainingLine();

			int whitespaceChars = 0;
			for (int i = indentOriginLine.Extent.Start; i < parenthesisOpen; ++i) {
				char c = buf[i];
				if (char.IsWhiteSpace(c)) {
					++whitespaceChars;
					continue;
				}
				break;
			}
			string indent = "";
			if (whitespaceChars > 0) {
				indent = buf.GetText(new Span(indentOriginLine.Extent.Start, whitespaceChars));
			}

			ITextEdit edit = TextView.TextBuffer.CreateEdit();
			if (!TextView.Selection.IsEmpty) {
				edit.Delete(TextView.Selection.SelectedSpans[0]);
			}
			edit.Delete(new Span(pointLine.Extent.Start,
					point - pointLine.Extent.Start));
			edit.Insert(pointEnd, indent);
			if (!TextView.Selection.IsEmpty) {
				TextView.Selection.Clear();
			}
			edit.Apply();
			TextView.Caret.EnsureVisible();
			return;
		}
		private bool handleCtrlLeft() {
			if (hasBoxSelection()) return false;
			TextView.Selection.Clear();
			SnapshotPoint newCaretPos = WordBoundary.findLeftWordBoundary(TextView.Caret.Position.BufferPosition);
			TextView.Caret.MoveTo(newCaretPos, PositionAffinity.Predecessor, true);
			TextView.Caret.EnsureVisible();
			return true;
		}
		private bool handleCtrlShiftLeft() {
			if (hasBoxSelection()) return false;
			SnapshotPoint newCaretPos = WordBoundary.findLeftWordBoundary(TextView.Caret.Position.BufferPosition);
			SnapshotPoint selectionAnchor = TextView.Selection.AnchorPoint.Position;
			TextView.Caret.MoveTo(newCaretPos, PositionAffinity.Predecessor, true);
			bool isReversed = newCaretPos < selectionAnchor;
			if (isReversed) {
				TextView.Selection.Select(new SnapshotSpan(newCaretPos, selectionAnchor), isReversed);
			} else {
				TextView.Selection.Select(new SnapshotSpan(selectionAnchor, newCaretPos), isReversed);
			}
			TextView.Caret.EnsureVisible();
			return true;
		}
		private bool handleCtrlRight() {
			if (hasBoxSelection()) return false;
			TextView.Selection.Clear();
			SnapshotPoint newCaretPos = WordBoundary.findRightWordBoundary(TextView.Caret.Position.BufferPosition);
			TextView.Caret.MoveTo(newCaretPos, PositionAffinity.Predecessor, true);
			TextView.Caret.EnsureVisible();
			return true;
		}
		private bool handleCtrlShiftRight() {
			if (hasBoxSelection()) return false;
			SnapshotPoint newCaretPos = WordBoundary.findRightWordBoundary(TextView.Caret.Position.BufferPosition);
			SnapshotPoint selectionAnchor = TextView.Selection.AnchorPoint.Position;
			TextView.Caret.MoveTo(newCaretPos, PositionAffinity.Predecessor, true);
			bool isReversed = newCaretPos < selectionAnchor;
			if (isReversed) {
				TextView.Selection.Select(new SnapshotSpan(newCaretPos, selectionAnchor), isReversed);
			} else {
				TextView.Selection.Select(new SnapshotSpan(selectionAnchor, newCaretPos), isReversed);
			}
			TextView.Caret.EnsureVisible();
			return true;
		}
		private bool handleCtrlSquareBracket(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut, out int handlingResult) {
			if (hasBoxSelection()) {
				handlingResult = VSConstants.S_FALSE;
				return false;
			}
			CaretPosition oldCaretPos = TextView.Caret.Position;

			ITextViewLineCollection visibleLines;
			while (!TextView.TryGetTextViewLines(out visibleLines)) {
				System.Threading.Thread.Sleep(1);
			}
			ITextViewLine firstLine = visibleLines.FirstVisibleLine;
			SnapshotPoint firstLineStart = firstLine.Start;
			double viewDelta = firstLine.Top - TextView.ViewportTop;
			double viewportLeft = TextView.ViewportLeft;

			ThreadHelper.ThrowIfNotOnUIThread();
			handlingResult = NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

			if (TextView.Caret.Position == oldCaretPos) {
				SnapshotPoint outerBrace = findOuterOpeningParenthesis(oldCaretPos.BufferPosition);
				if (outerBrace.Snapshot != null) {
					TextView.Caret.MoveTo(outerBrace, PositionAffinity.Successor, true);
					TextView.Caret.EnsureVisible();
				}
				return true;
			}

			CaretPosition newCaretPos = TextView.Caret.Position;
			
			TextView.DisplayTextLineContainingBufferPosition(firstLineStart, viewDelta, ViewRelativePosition.Top);
			TextView.ViewportLeft = viewportLeft;
			handlingResult = NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

			CaretPosition thirdCaretPos = TextView.Caret.Position;

			ITextSnapshot buf = TextView.TextSnapshot;

			CaretPosition openPos;
			CaretPosition closePos;
			if (newCaretPos.VirtualBufferPosition < thirdCaretPos.VirtualBufferPosition) {
				openPos = newCaretPos;
				closePos = thirdCaretPos;
			} else {
				openPos = thirdCaretPos;
				closePos = newCaretPos;
			}

			char brace = '\0';
			SnapshotPoint point = openPos.BufferPosition;
			if (point.Position < buf.Length) {
				brace = buf[point.Position];
				if (brace != '('
						&& brace != '['
						&& brace != '{'
						&& brace != '"'
						&& brace != '/') {
					brace = '\0';
				}
			}
			if (brace == '\0' || brace == '(' || brace == '[' || brace == '{') {
				TextView.DisplayTextLineContainingBufferPosition(firstLineStart, viewDelta, ViewRelativePosition.Top);
				TextView.ViewportLeft = viewportLeft;
				handlingResult = NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				return true;
			}

			if (brace == '"') {
				bool needFlip = false;
				if (oldCaretPos.VirtualBufferPosition <= openPos.VirtualBufferPosition
						|| oldCaretPos.BufferPosition <= openPos.BufferPosition + 1) {
					needFlip = !(thirdCaretPos.VirtualBufferPosition >= closePos.VirtualBufferPosition);
				} else if (oldCaretPos.VirtualBufferPosition >= closePos.VirtualBufferPosition) {
					needFlip = thirdCaretPos != openPos;
				} else if (thirdCaretPos != openPos) {
					needFlip = true;
				}
				if (needFlip) {
					TextView.DisplayTextLineContainingBufferPosition(firstLineStart, viewDelta, ViewRelativePosition.Top);
					TextView.ViewportLeft = viewportLeft;
					handlingResult = NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				}
				return true;
			}

			if (brace == '/') {
				char closingBrace = buf[closePos.BufferPosition];
				int closingBraceSize = 2;
				if (closingBrace == '/') {
					closingBraceSize = 1;
				}
				bool needFlip = false;
				if (oldCaretPos.VirtualBufferPosition <= openPos.VirtualBufferPosition
						|| oldCaretPos.BufferPosition <= openPos.BufferPosition + 2) {
					needFlip = !(thirdCaretPos.VirtualBufferPosition >= closePos.VirtualBufferPosition);
				} else if (oldCaretPos.VirtualBufferPosition >= closePos.VirtualBufferPosition) {
					needFlip = thirdCaretPos != openPos;
				} else if (thirdCaretPos != openPos) {
					needFlip = true;
				}
				if (needFlip) {
					TextView.DisplayTextLineContainingBufferPosition(firstLineStart, viewDelta, ViewRelativePosition.Top);
					TextView.ViewportLeft = viewportLeft;
					handlingResult = NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				}
				if (TextView.Caret.Position.VirtualBufferPosition >= closePos.VirtualBufferPosition
						&& TextView.Caret.Position.BufferPosition != closePos.BufferPosition + closingBraceSize) {
					TextView.Caret.MoveTo(closePos.BufferPosition + closingBraceSize, PositionAffinity.Predecessor, true);
				}
				return true;
			}
			
			return true;
		}
		private bool handleCtrlShiftSquareBracket2(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut, out int handlingResult) {
			if (TextView.Selection.IsEmpty || hasBoxSelection()) {
				handlingResult = VSConstants.S_FALSE;
				return false;
			}
			VirtualSnapshotPoint selectionAnchor = TextView.Selection.AnchorPoint;

			ThreadHelper.ThrowIfNotOnUIThread();
			handlingResult = NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

			VirtualSnapshotPoint selectionHead = TextView.Selection.ActivePoint;

			TextView.Selection.Select(selectionAnchor, selectionHead);
			return true;
		}

		// I gave up trying to handle /* */ bracket pair matching and made handleCtrlShiftSquareBracket2() instead
		// that just calls the built-in implementation and plays around it
		private bool handleCtrlShiftSquareBracket() {
			if (hasBoxSelection()) return false;
			ITextSnapshot buf = TextView.TextSnapshot;
			SnapshotPoint point = TextView.Caret.Position.BufferPosition;
			int pos = point.Position;
			bool foundPoint = false;
			char c = '\0';
			IClassificationType type;
			if (pos >= 0 & pos < buf.Length) {
				c = buf[pos];
				if (c == '('
						|| c == '['
						|| c == '{'
						|| c == '<'
						|| c == ')'
						|| c == ']'
						|| c == '}'
						|| c == '>') {
					foundPoint = true;
				}
				if (pos > 1 && !foundPoint) {
					c = buf[pos - 1];
					if (c == '('
							|| c == '['
							|| c == '{'
							|| c == '<'
							|| c == ')'
							|| c == ']'
							|| c == '}'
							|| c == '>') {
						foundPoint = true;
						point -= 1;
						--pos;
					}
				}
			}
			if (!foundPoint) return false;
			type = getClassOfChar(point);
			if (type != null && isComment(type)) {
				return false;
			}
			c = buf[pos];
			bool isClosing = false;
			SnapshotPoint matchingBrace = new SnapshotPoint();
			if (c == '(' || c == '[' || c == '{' || c == '<') {
				isClosing = true;
				matchingBrace = findClosingParenthesis(point, c);
			} else {
				matchingBrace = findOpeningParenthesis(point, c);
			}
			if (matchingBrace.Snapshot == null) return false;
			
			SnapshotPoint newCaretPos;
			SnapshotPoint selectionAnchor;
			if (TextView.Selection.IsEmpty) {
				if (isClosing) {
					selectionAnchor = point;
					newCaretPos = matchingBrace + 1;
				} else {
					selectionAnchor = point + 1;
					newCaretPos = matchingBrace;
				}
			} else {
				selectionAnchor = TextView.Selection.AnchorPoint.Position;
				if (matchingBrace >= selectionAnchor) {
					newCaretPos = matchingBrace + 1;
				} else {
					newCaretPos = matchingBrace;
				}
			}

			TextView.Caret.MoveTo(newCaretPos, PositionAffinity.Predecessor, true);
			bool isReversed = newCaretPos < selectionAnchor;
			if (isReversed) {
				TextView.Selection.Select(new SnapshotSpan(newCaretPos, selectionAnchor), isReversed);
			} else {
				TextView.Selection.Select(new SnapshotSpan(selectionAnchor, newCaretPos), isReversed);
			}
			TextView.Caret.EnsureVisible();
			return true;
		}
		private bool handleCtrlDelete() {
			if (noEditAccessOrHasBoxSelection()) return false;
			ITextSnapshot buf = TextView.TextSnapshot;
			SnapshotPoint point;
			SnapshotPoint pointEnd;
			ITextEdit edit;
			if (TextView.Selection.IsEmpty) {
				point = TextView.Caret.Position.BufferPosition;
				pointEnd = point;
			} else {
			    edit = TextView.TextBuffer.CreateEdit();
				edit.Delete(TextView.Selection.SelectedSpans[0]);
    			edit.Apply();
				TextView.Selection.Clear();
    			TextView.Caret.EnsureVisible();
    			return true;
			}
			int pos = pointEnd.Position;
			if (pos == buf.Length) return true;
			SnapshotPoint positionAfterTheWord = WordBoundary.findRightWordBoundary(pointEnd);
			edit = TextView.TextBuffer.CreateEdit();
			edit.Delete(new Span(pos, positionAfterTheWord - pointEnd));
			edit.Apply();
			TextView.Caret.EnsureVisible();
			return true;
		}
		private bool handleCtrlBackspace() {
			if (noEditAccessOrHasBoxSelection()) return false;
			ITextSnapshot buf = TextView.TextSnapshot;
			SnapshotPoint point;
			SnapshotPoint pointEnd;
			ITextEdit edit;
			if (TextView.Selection.IsEmpty) {
				point = TextView.Caret.Position.BufferPosition;
				pointEnd = point;
			} else {
			    edit = TextView.TextBuffer.CreateEdit();
				edit.Delete(TextView.Selection.SelectedSpans[0]);
    			edit.Apply();
				TextView.Selection.Clear();
    			TextView.Caret.EnsureVisible();
    			return true;
			}
			int pos = point.Position;
			if (pos == 0) return true;
			SnapshotPoint prevWordStartOrThisWordStart = WordBoundary.findLeftWordBoundary(point);
			edit = TextView.TextBuffer.CreateEdit();
			edit.Delete(new Span(prevWordStartOrThisWordStart.Position, point - prevWordStartOrThisWordStart));
			edit.Apply();
			TextView.Caret.EnsureVisible();
			return true;
		}
		private string calculateIndent() {
			string indent = "";
			if (ConvertTabsToSpaces) {
				indent = new string(' ', IndentSize);
			} else {
				int accumulatedIndent = 0;
				while (accumulatedIndent < IndentSize) {
					if (IndentSize - accumulatedIndent <= TabSize && IndentSize % TabSize == 0) {
						indent += "\t";
						accumulatedIndent += TabSize;
					} else {
						indent += " ";
						++accumulatedIndent;
					}
				}
			}
			return indent;
		}
		private bool handleTab() {
			if (noEditAccessOrHasBoxSelection()) return false;
			string indent;
			ITextEdit edit;
			ITextSnapshot buf = TextView.TextSnapshot;
			if (TextView.Selection.IsEmpty) {
			    indent = calculateIndent();
    			edit = TextView.TextBuffer.CreateEdit();
				edit.Insert(TextView.Caret.Position.BufferPosition, indent);
    			edit.Apply();
    			return true;
			}
			VirtualSnapshotPoint selStart;
			VirtualSnapshotPoint selEnd;
			if (TextView.Selection.IsReversed) {
				selStart = TextView.Selection.ActivePoint;
				selEnd = TextView.Selection.AnchorPoint;
			} else {
				selEnd = TextView.Selection.ActivePoint;
				selStart = TextView.Selection.AnchorPoint;
			}
			int lineStart = selStart.Position.GetContainingLineNumber();
			int lineEnd = selEnd.Position.GetContainingLineNumber();
			if (selEnd.Position.GetContainingLine().Start == selEnd.Position) {
				--lineEnd;
			}
			if (lineEnd < lineStart) return false;
			indent = calculateIndent();
			edit = TextView.TextBuffer.CreateEdit();
			buf = TextView.TextSnapshot;
			int caretLineNum = TextView.Caret.Position.BufferPosition.GetContainingLineNumber();
			for (int i = lineStart; i <= lineEnd; ++i) {
				ITextSnapshotLine line = buf.GetLineFromLineNumber(i);
                int j = line.Extent.Start;
                for (; j < line.Extent.End; ++j) {
                    if (buf[j] != '\t') break;
                }
				edit.Insert(j, indent);
			}
			edit.Apply();
			return true;
		}
		private bool handleShiftTab() {
			if (noEditAccessOrHasBoxSelection()) return false;
			VirtualSnapshotPoint selStart;
			VirtualSnapshotPoint selEnd;
			if (TextView.Selection.IsReversed) {
				selStart = TextView.Selection.ActivePoint;
				selEnd = TextView.Selection.AnchorPoint;
			} else {
				selEnd = TextView.Selection.ActivePoint;
				selStart = TextView.Selection.AnchorPoint;
			}
			int lineStart = selStart.Position.GetContainingLineNumber();
			int lineEnd = selEnd.Position.GetContainingLineNumber();
			if (selEnd.Position.GetContainingLine().Start == selEnd.Position) {
				--lineEnd;
			}
			if (lineEnd < lineStart) return false;
			ITextEdit edit = TextView.TextBuffer.CreateEdit();
			ITextSnapshot buf = TextView.TextSnapshot;
			int caretLineNum = TextView.Caret.Position.BufferPosition.GetContainingLineNumber();
			for (int i = lineStart; i <= lineEnd; ++i) {
				int charsToDelete = 0;
				ITextSnapshotLine line = buf.GetLineFromLineNumber(i);
				int accumulatedSpace = 0;
				int end = line.Extent.End;
				if (line.Extent.Start >= buf.Length) break;
				for (int j = line.Extent.Start; j < end; ++j) {
					char c = buf[j];
					if (char.IsWhiteSpace(c)) {
						++charsToDelete;
						if (c == '\t') {
							accumulatedSpace += TabSize - (accumulatedSpace % TabSize);
						} else if (c == ' ') {
							++accumulatedSpace;
						}
						if (accumulatedSpace >= IndentSize) {
							break;
						}
						continue;
					}
					break;
				}
				if (charsToDelete > 0) {
					edit.Delete(line.Extent.Start, charsToDelete);
				}
				if (accumulatedSpace > IndentSize) {
				    int lineIndentEndPos = -1;
    				for (lineIndentEndPos = line.Extent.Start; lineIndentEndPos < end; ++lineIndentEndPos) {
    					char c = buf[lineIndentEndPos];
    					if (!char.IsWhiteSpace(c)) {
    					    break;
    					}
    				}
    				edit.Insert(lineIndentEndPos, new string(' ', accumulatedSpace - IndentSize));
				}
			}
			edit.Apply();
			return true;
		}
	}
}
