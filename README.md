# CustomLineBreak

Visual Studio 2022 Community Edition extension for working with C/C++ that overrides:

- Enter key behavior. Performs a smart indent in a hardcoded style. Prevents the editor from creating virtual space. Copies whitespace from the previous line without converting spaces to tabs or vice versa.
- Typing a closing }. Aligns the typed } on the same indent as the opening {.
- Ctrl+Left. Goes to the start or end of previous word, whichever is closer. Instead of always to the start of previous word.
- Ctrl+Shift+Left. Extends selection to the start or end of previous word, whichever is closer. Instead of always to the start of previous word.
- Ctrl+Right. Goes to the start or end of next word, whichever is closer. Instead of always to the start of next word.
- Ctrl+Shift+Right. Extends selection to the start or end of next word, whichever is closer. Instead of always to the start of next word.
- Ctrl+]. When inside a "" or /\* \*/ block, if the cursor is not on the opening " or /\*,  makes sure to always navigate to the opening " or /\*. When the cursor is not on a brace, navigates to the outer enclosing content's opening (, [ or {.
- Ctrl+Shift+]. Extends selection to the closing/opening brace without moving the text selection anchor (base or starting point of the selection).
- Ctrl+Delete. Deletes until the start or end of next word. Instead of always deleting until the start of the next word and the word after that or whatever it did in the default behavior (I forgot).
- Ctrl+Backspace. Deletes until the start or end of previous word. Instead of always deleting until the start of previous word.
- Tab. Indents the current selection if the selection is not empty (something is selected), even if the selection starts and ends on the same line. Indents even those lines that are empty.
- Shift+Tab. Unindents the current selected lines. Unindents even those lines that are empty.
- Ctrl+Shift+Left mouse click. Fixes an issue when of the word is right next to an operator, no whitespace inbetween, if the caret was at the start of that word and the word was selected prior to clicking on the operator, it would deselect the word and select only the operator.

Works only with C/C++ files.  
This is created out of hatred for VS's built-in behavior that cannot be configured and out of desperation that they will never listen to the feedback and fix any of it.

## Installation

Launch the VSIX file.

## Credits

Based on TabSanity. This is literally a modified copy of TabSanity, check it out: <https://github.com/jednano/tabsanity-vs>
