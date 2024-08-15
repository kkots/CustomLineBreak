using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace CustomLineBreak
{
	public class LineCharElement
	{
		public SnapshotPoint point;
		public char c;
		public Boolean isKeyword = false;
		public Boolean isIdentifier = false;
		public Boolean isComment = false;
		public Boolean isOperator = false;
		public Boolean isString = false;
		public Boolean isStringDelimiter = false;
		public IClassificationType classificationType = null;
		public override string ToString() {
			if (classificationType == null) {
				return "'" + c + "'";
			} else {
				return "'" + c + "' " + classificationType;
			}
		}
	}
	public class LineCharCollection : ICollection<LineCharElement>
	{
		public IEnumerator<LineCharElement> GetEnumerator()
		{
			return new LineCharEnumerator(this);
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new LineCharEnumerator(this);
		}
		private List<LineCharElement> collection;
		public LineCharCollection(IClassifier classifier, SnapshotSpan textLine)
		{
			collection = new List<LineCharElement>();
			IList<ClassificationSpan> classificationSpans = classifier.GetClassificationSpans(textLine);
			ClassificationSpan currentSpan = null;
			int currentSpanIndex = -1;
			if (classificationSpans.Count > 0) {
				currentSpan = classificationSpans[0];
				currentSpanIndex = 0;
			}
			int start = textLine.Start.Position;
			int end = textLine.End.Position;
			ITextSnapshot buf = textLine.Snapshot;
			for (int i = start; i < end; ++i) {
				char c = buf[i];
				Boolean insideSpan = false;
				if (currentSpan != null) {
					SnapshotSpan classificationSpan = currentSpan.Span;
					while (true) {
						if (classificationSpan.Start.Position <= i && classificationSpan.End.Position > i) {
							insideSpan = true;
							break;
						}
						if (classificationSpan.Start.Position > i) {
							break;
						}
						++currentSpanIndex;
						if (currentSpanIndex >= classificationSpans.Count) {
							currentSpan = null;
							break;
						}
						currentSpan = classificationSpans[currentSpanIndex];
						classificationSpan = currentSpan.Span;
					}
				}
				LineCharElement newElem = new LineCharElement();
				newElem.c = c;
				newElem.point = (textLine.Start + (i - start));
				if (insideSpan) {
					IClassificationType classificationType = currentSpan.ClassificationType;
					newElem.classificationType = classificationType;
					newElem.isKeyword = classificationType.IsOfType("keyword");
					newElem.isIdentifier = classificationType.IsOfType("identifier");
					newElem.isComment = classificationType.IsOfType("comment");
					newElem.isOperator = classificationType.IsOfType("operator");
					newElem.isString = classificationType.IsOfType("string");
					newElem.isStringDelimiter = classificationType.IsOfType("cppStringDelimiterCharacter");
				}
				collection.Add(newElem);
			}
		}
		public LineCharElement this[int index]
		{
			get { return (LineCharElement)collection[index]; }
			set { collection[index] = value; }
		}
		public bool Contains(LineCharElement item)
		{
			return true;
		}
		public bool Contains(LineCharElement item, EqualityComparer<LineCharElement> comp)
		{
			return true;
		}
		public void Add(LineCharElement item)
		{
			collection.Add(item);
		}
		public void Clear()
		{
			collection.Clear();
		}
		public void CopyTo(LineCharElement[] array, int arrayIndex)
		{
			if (array == null)
			   throw new ArgumentNullException("The array cannot be null.");
			if (arrayIndex < 0)
			   throw new ArgumentOutOfRangeException("The starting array index cannot be negative.");
			if (Count > array.Length - arrayIndex)
			   throw new ArgumentException("The destination array has fewer elements than the collection.");

			for (int i = 0; i < collection.Count; i++) {
				array[i + arrayIndex] = collection[i];
			}
		}
		public int Count
		{
			get
			{
				return collection.Count;
			}
		}
		public bool IsReadOnly
		{
			get { return true; }
		}
		public bool Remove(LineCharElement item)
		{
			return false;
		}
	}
	public class LineCharEnumerator : IEnumerator<LineCharElement> {
		private LineCharCollection collection;
		private int index = -1;
		public LineCharEnumerator(LineCharCollection collection) : base() {
			this.collection = collection;
			index = -1;
		}
		public bool MoveNext()
		{
			//Avoids going beyond the end of the collection.
			if (++index >= collection.Count)
			{
				return false;
			}
			return true;
		}
		public void Reset() { index = -1; }
		void IDisposable.Dispose() { }
		public LineCharElement Current
		{
			get { return collection[index]; }
		}
		object IEnumerator.Current
		{
			get { return collection[index]; }
		}
	}
}
