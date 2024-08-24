using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLineBreak
{
    public class WordBoundary {
		static public IList<Span> breakLineIntoSpans(SnapshotSpan section) {
			IList<Span> spans = new List<Span>();
			ITextSnapshot buf = section.Snapshot;
			int newSpanStart = 0;
			bool insideString = false;
			char stringType = '\0';
			bool lastCharWasBackslash = false;
			int end = section.End;
			for (int i = section.Start; i < end; ++i) {
				char c = buf[i];
				bool lastCharWasBackslashCurrent = lastCharWasBackslash;
				lastCharWasBackslash = false;
				if (char.IsLetterOrDigit(c) || c == '_') {
					newSpanStart = i;
					do {
						++i;
						if (i < end) c = buf[i];
						else break;
					} while (char.IsLetterOrDigit(c) || c == '_');
					spans.Add(new Span(newSpanStart, i - newSpanStart));
					--i;
				} else if (char.IsWhiteSpace(c)) {
					newSpanStart = i;
					do {
						++i;
					} while (i < end && char.IsWhiteSpace(buf[i]));
					spans.Add(new Span(newSpanStart, i - newSpanStart));
					--i;
				} else if (c == '\\') {
					lastCharWasBackslash = true;
					newSpanStart = i;
					do {
						++i;
					} while (i < end && buf[i] == c);
					spans.Add(new Span(newSpanStart, i - newSpanStart));
					--i;
				} else if (c == '<' || c == '>') {
					if (i + 1 < end) {
						char nextC = buf[i + 1];
						if (nextC == '=' || c == '<' && nextC == '>') {
							spans.Add(new Span(i, 2));
							++i;
							continue;
						}
						if (nextC == c) {
							if (i + 2 < end) {
								char nextNextC = buf[i + 2];
								if (nextNextC == '=') {
									spans.Add(new Span(i, 3));
									i += 2;
									continue;
								}
							}
							newSpanStart = i;
							++i;
							do {
								++i;
							} while (i < end && buf[i] == c);
							spans.Add(new Span(newSpanStart, i - newSpanStart));
							--i;
							continue;
						}
					}
					spans.Add(new Span(i, 1));
				} else if (c == '/') {
					if (i + 1 < end) {
						char nextC = buf[i + 1];
						if (nextC == '/' || nextC == '*' || nextC == '=') {
							spans.Add(new Span(i, 2));
							++i;
							continue;
						}
					}
					newSpanStart = i;
					do {
						++i;
					} while (i < end && buf[i] == c);
					spans.Add(new Span(newSpanStart, i - newSpanStart));
					--i;
				} else if (c == '*') {
					if (i + 1 < end) {
						char nextC = buf[i + 1];
						if (nextC == '/' || nextC == '=') {
							spans.Add(new Span(i, 2));
							++i;
							continue;
						}
					}
					newSpanStart = i;
					do {
						++i;
					} while (i < end && buf[i] == c);
					spans.Add(new Span(newSpanStart, i - newSpanStart));
					--i;
				} else if (c == '%'
						|| c == '^'
						|| c == '!'
						|| c == '='
						|| c == '~') {
					if (i + 1 < end) {
						char nextC = buf[i + 1];
						if (nextC == '=') {
							if (c == '=') {
								newSpanStart = i;
								++i;
								do {
									++i;
								} while (i < end && buf[i] == c);
								spans.Add(new Span(newSpanStart, i - newSpanStart));
								--i;
								continue;
							}
							spans.Add(new Span(i, 2));
							++i;
							continue;
						}
					}
					if (c != '=') {
						newSpanStart = i;
						do {
							++i;
						} while (i < end && buf[i] == c);
						spans.Add(new Span(newSpanStart, i - newSpanStart));
						--i;
						continue;
					}
					spans.Add(new Span(i, 1));
				} else if (c == '|'
						|| c == '&'
						|| c == '+') {
					if (i + 1 < end) {
						char nextC = buf[i + 1];
						if (nextC == c || nextC == '=') {
							if (nextC == c) {
								newSpanStart = i;
								++i;
								do {
									++i;
								} while (i < end && buf[i] == c);
								spans.Add(new Span(newSpanStart, i - newSpanStart));
								--i;
								continue;
							}
							spans.Add(new Span(i, 2));
							++i;
							continue;
						}
					}
					spans.Add(new Span(i, 1));
				} else if (c == ':' || c == '#') {
					if (i + 1 < end) {
						char nextC = buf[i + 1];
						if (nextC == c) {
							newSpanStart = i;
							++i;
							do {
								++i;
							} while (i < end && buf[i] == c);
							spans.Add(new Span(newSpanStart, i - newSpanStart));
							--i;
							continue;
						}
					}
					spans.Add(new Span(i, 1));
				} else if (c == '-') {
					if (i + 1 < end) {
						char nextC = buf[i + 1];
						if (nextC == '-' || nextC == '=' || nextC == '>') {
							if (nextC == '-') {
								newSpanStart = i;
								++i;
								do {
									++i;
								} while (i < end && buf[i] == c);
								spans.Add(new Span(newSpanStart, i - newSpanStart));
								--i;
								continue;
							}
							spans.Add(new Span(i, 2));
							++i;
							continue;
						}
					}
					spans.Add(new Span(i, 1));
				} else if (c == '"' || c == '\'') {
					if (insideString) {
						if (c != stringType) {
							newSpanStart = i;
							do {
								++i;
							} while (i < end && buf[i] == c);
							spans.Add(new Span(newSpanStart, i - newSpanStart));
							--i;
							continue;
						}
						if (!lastCharWasBackslashCurrent) {
							insideString = false;
						}
						newSpanStart = i;
						do {
							++i;
						} while (i < end && buf[i] == c);
						spans.Add(new Span(newSpanStart, i - newSpanStart));
						--i;
						continue;
					}
					newSpanStart = i;
					do {
						++i;
					} while (i < end && buf[i] == c);
					spans.Add(new Span(newSpanStart, i - newSpanStart));
					--i;
				} else {
					newSpanStart = i;
					do {
						++i;
					} while (i < end && buf[i] == c);
					spans.Add(new Span(newSpanStart, i - newSpanStart));
					--i;
				}
			}
			return spans;
		}
		static public SnapshotPoint findLeftWordBoundAt(SnapshotPoint cursor) {
			int pos = cursor.Position;
			ITextSnapshotLine line = cursor.GetContainingLine();
			ITextSnapshot buf = cursor.Snapshot;
		    int start = line.Extent.Start;
			if (pos <= start) {
			    return cursor;
			}
			char c = buf[pos];
			bool whitespaceMode = char.IsWhiteSpace(c);
			if (whitespaceMode) {
				while (pos > start) {
					c = buf[pos - 1];
					if (!char.IsWhiteSpace(c)) break;
					--pos;
				}
				return new SnapshotPoint(buf, pos);
			}
			IList<Span> spans = breakLineIntoSpans(line.Extent);
			if (spans.Count == 0) {
				return cursor;
			}
			int positionToSeek = cursor.Position;
			for (int i = 0; i < spans.Count; ++i) {
				Span span = spans[i];
				if (span.Contains(positionToSeek)) {
					int offset = positionToSeek - span.Start;
					if (offset == 0) {
						return cursor;
					}
					return cursor - offset;
				}
			}
			Span lastSpan = spans[spans.Count - 1];
			if (positionToSeek == lastSpan.Start + lastSpan.Length) {
				return cursor - lastSpan.Length;
			}
			return cursor;
		}
		static public SnapshotPoint findRightWordBoundAt(SnapshotPoint cursor) {
			int pos = cursor.Position;
			ITextSnapshot buf = cursor.Snapshot;
			ITextSnapshotLine line = cursor.GetContainingLine();
			int end = line.Extent.End;
			if (pos >= end) {
			    cursor = cursor - 1;
			    --pos;
			}
			if (pos <= line.Extent.Start) {
			    pos = line.Extent.Start;
			    cursor = line.Extent.Start;
			}
			char c = buf[pos];
			bool whitespaceMode = char.IsWhiteSpace(c);
			++pos;
			if (whitespaceMode) {
				while (pos < end) {
					c = buf[pos];
					if (!char.IsWhiteSpace(c)) break;
					++pos;
				}
				return new SnapshotPoint(buf, pos);
			}
			IList<Span> spans = breakLineIntoSpans(line.Extent);
			if (spans.Count == 0) {
				return cursor;
			}
			int positionToSeek = cursor.Position;
			for (int i = 0; i < spans.Count; ++i) {
				Span span = spans[i];
				if (span.Contains(positionToSeek)) {
					int offset = positionToSeek - span.Start;
					return cursor + (span.Length - offset);
				}
			}
			return cursor;
		}
		static public SnapshotSpan wordBoundsAt(SnapshotPoint cursor) {
			return new SnapshotSpan(findLeftWordBoundAt(cursor), findRightWordBoundAt(cursor));
		}
		static public SnapshotPoint findLeftWordBoundary(SnapshotPoint cursor) {
			int pos = cursor.Position;
			if (pos == 0) return cursor;
			ITextSnapshot buf = cursor.Snapshot;
			ITextSnapshotLine line = cursor.GetContainingLine();
			if (cursor == line.Extent.Start) {
				if (buf[pos - 1] == '\n' && pos > 1 && buf[pos - 2] == '\r') {
					return cursor - 2;
				}
				return cursor - 1;
			}
			--pos;
			char c = buf[pos];
			bool whitespaceMode = char.IsWhiteSpace(c);
			if (whitespaceMode) {
				while (pos > 0) {
					c = buf[pos - 1];
					if (!(char.IsWhiteSpace(c) && c != '\n')) break;
					--pos;
				}
				return new SnapshotPoint(buf, pos);
			}
			IList<Span> spans = breakLineIntoSpans(line.Extent);
			if (spans.Count == 0) {
				return cursor - 1;
			}
			int positionToSeek = cursor.Position;
			for (int i = 0; i < spans.Count; ++i) {
				Span span = spans[i];
				if (span.Contains(positionToSeek)) {
					int offset = positionToSeek - span.Start;
					if (offset == 0) {
						if (i == 0) {
							return cursor - 1;
						}
						return cursor - spans[i - 1].Length;
					}
					return cursor - offset;
				}
			}
			Span lastSpan = spans[spans.Count - 1];
			if (positionToSeek == lastSpan.Start + lastSpan.Length) {
				return cursor - lastSpan.Length;
			}
			return cursor - 1;
		}
		static public SnapshotPoint findRightWordBoundary(SnapshotPoint cursor) {
			int pos = cursor.Position;
			ITextSnapshot buf = cursor.Snapshot;
			if (pos >= buf.Length) return cursor;
			ITextSnapshotLine line = cursor.GetContainingLine();
			if (cursor == line.Extent.End) {
				if (pos < buf.Length && buf[pos] == '\r' && pos + 1 < buf.Length && buf[pos + 1] == '\n') {
					return cursor + 2;
				}
				return cursor + 1;
			}
			char c = buf[pos];
			bool whitespaceMode = char.IsWhiteSpace(c);
			++pos;
			if (whitespaceMode) {
				while (pos < buf.Length) {
					c = buf[pos];
					if (!(char.IsWhiteSpace(c) && c != '\r' && c != '\n')) break;
					++pos;
				}
				return new SnapshotPoint(buf, pos);
			}
			IList<Span> spans = breakLineIntoSpans(line.Extent);
			if (spans.Count == 0) {
				return cursor + 1;
			}
			int positionToSeek = cursor.Position;
			for (int i = 0; i < spans.Count; ++i) {
				Span span = spans[i];
				if (span.Contains(positionToSeek)) {
					int offset = positionToSeek - span.Start;
					return cursor + (span.Length - offset);
				}
			}
			return cursor + 1;
		}
    }
}
