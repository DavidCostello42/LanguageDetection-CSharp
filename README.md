# LanguageDetection-CSharp
A simple C# library to provide language detection based on GNU Aspell dictionaries (.dic files).

* LanguageDetection is a C# 2010 DLL project.
* ConsoleDetection is a sample command-line tool.

# LanguageDetection (Project)

## Lookup (Class)
Primary class for the LanguageDetection library.

Property | Data Type | Description
--- | --- | ---
DictionaryFile | String | The path to a single-language dictionary file to load (if no DictionaryText is provided).
DictionaryText | String | The text to use for the dictionary. You can specify this instead of having the DictionaryFile read.

Constructor | Description
--- | ---
Lookup() | Standard constructor. DictionaryFile and DictionaryText will default to empty strings. You will need to specify the properties manually.
Lookup(String DictionaryFile) | Takes the DictionaryFile string and loads the specified file content into DictionaryText automatically.
Lookup(String DictionaryFile,String DictionaryText) | Specifies both properties during initialisation. File content will not be loaded in this instance.

Method | Description
--- | ---
Identify(String text) | Main language detection function. Returns a score based on the input value 'text' when tested against the loaded DictionaryText.

## dictionaries (Directory)
A dictionary containing all ASpell Dictionary files of the languages you wish to detect.

# DLL Usage
Store your ASpell dictionaries in your project, import the LanguageDetection.dll library into your project and call Lookup.Identify() method for each dictionary language you wish to test.

# ConsoleDetection (program)
This is a console demo application. Download and compile the application then execute from the command line.

## Example usage
### Steps
1. Load up a command line session
2. Type: idl "The quick brown fox jumps over the lazy dog."
3. Press Enter
### Output
You should see something similar to:
Result: english.dic (9)/in: 3727.3643ms

## Command Line switches
Switch | Description
--- | ---
/? /h /help | Prints the built-in help
/d /dir <dir> | The directory where dictionaries are found. Defaults to the folder idl is located when not specified. (Remember to encase in quotes if your dir name has spaces).
/f <file> | Alternative to text input - specify a text file to read.
/m <mode> | Change output on console to CSV or JSON (specify /m JSON or /m CSV).
/r | Enable full report of matches against each dictionary.
/t <max> | Enable threading and set a maximum number of worker threads.
"<text>" | The text to analyse in quotes.
### Command line examples
* idl /m json "The quick brown fox jumps over the lazy dog."
* idl /r /m csv /t 2 "The quick brown fox jumps over the lazy dog."

## Useful output
Use the /m switch to enable JSON or CSV formatted output.

# Get ASpell Dictionaries
Grab ASpell dictionaries from http://aspell.net/