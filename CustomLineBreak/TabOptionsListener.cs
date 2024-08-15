using System;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;

namespace CustomLineBreak
{
	internal class TabOptionsListener : IDisposable
	{
		protected IWpfTextView TextView;
		protected readonly IEditorOptions Options;
		protected bool ConvertTabsToSpaces;
		protected int IndentSize;
		protected int TabSize;

		public TabOptionsListener(IWpfTextView textView)
		{
			TextView = textView;
			Options = textView.Options;

			Options.OptionChanged += OnTextViewOptionChanged;
			TextView.Closed += TextViewOnClosed;

			OnConvertTabsToSpacesOptionChanged();
			OnIndentSizeOptionChanged();
			OnTabSizeOptionChanged();
		}

		private void OnTextViewOptionChanged(object sender, EditorOptionChangedEventArgs e)
		{
			switch (e.OptionId)
			{
				case DefaultOptions.ConvertTabsToSpacesOptionName:
					OnConvertTabsToSpacesOptionChanged();
					break;

				case DefaultOptions.IndentSizeOptionName:
					OnIndentSizeOptionChanged();
					break;

				case DefaultOptions.TabSizeOptionName:
					OnTabSizeOptionChanged();
					break;
			}
		}

		protected virtual void OnConvertTabsToSpacesOptionChanged()
		{
			ConvertTabsToSpaces = Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
		}

		private void OnIndentSizeOptionChanged()
		{
			IndentSize = Options.GetOptionValue(DefaultOptions.IndentSizeOptionId);
		}

		private void OnTabSizeOptionChanged()
		{
			TabSize = Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
		}

		private void TextViewOnClosed(object sender, EventArgs eventArgs)
		{
			Dispose();
		}

		public virtual void Dispose()
		{
			TextView.Closed -= TextViewOnClosed;
			Options.OptionChanged -= OnTextViewOptionChanged;
		}
	}
}
