using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

namespace com.deltamango.LanguageDetection
{
    public class Lookup
    {
        /// <summary>
        /// The Dictionary file containing the words for the target language.
        /// </summary>
        public String DictionaryFile { get; set; }

        /// <summary>
        /// The text content of the Dictionary file
        /// </summary>
        public String DictionaryText { get; set; }

        
        /// <summary>
        /// Default constructor. Creates an empty instance.
        /// </summary>
        public Lookup()
        {
            
        }

        /// <summary>
        /// Loads the given fileName into memory and prepares the dictionary text for use.
        /// </summary>
        /// <param name="fileName">File name of a dictionary file.</param>
        public Lookup(String fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            if (File.Exists(fi.FullName))
            {
                DictionaryFile = fileName;
                DictionaryText = File.ReadAllText(fi.FullName);
            }
        }

        /// <summary>
        /// Assigns the file and dictionary properties to their respective internal values.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dictionaryText"></param>
        public Lookup(String fileName, String dictionaryText)
        {
            DictionaryFile = fileName;
            DictionaryText = dictionaryText;
        }


        /// <summary>
        /// Processes the sample text. Gives the total score for the given dictionary.
        /// </summary>
        /// <param name="text">Sample text to assess against the given dictionary.</param>
        /// <returns>The score for the given dictionary.</returns>
        public int Identify(String text) 
        {
            int score = 0;
            var words = text.Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in words)
            {
                var theWord = CleanUpWord(word);
                if (DictionaryText.Contains(theWord))
                {
                    score += 1;
                }
            }
            return score;
        }
        
        /// <summary>
        /// Removes punctuation from the word to search in the dictionary for.
        /// </summary>
        /// <param name="word">Word to clean.</param>
        /// <returns>The resulting cleaned word.</returns>
        private string CleanUpWord(string word)
        {
            StringBuilder cleanWord = new StringBuilder();
            foreach (char c in word)
            {
                if (Char.IsPunctuation(c)) { }
                else { cleanWord.Append(c); }
            }
            return cleanWord.ToString();
        }
    }
}
