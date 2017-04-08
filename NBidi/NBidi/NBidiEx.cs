using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace NBidi { 
    class NBidiEx : NBidi
    {
        /// <summary>
        /// Implementation of the BIDI algorithm, as described in http://www.unicode.org/reports/tr9/tr9-17.html
        /// </summary>
        /// <param name="logicalString">The original logical-ordered string.</param>
        /// <param name="indexes">Implies where the original characters are.</param>
        /// <param name="lengths">Implies how many characters each original character occupies.</param>
        /// <param name="charData">Internal character data</param>
        /// <returns>The visual representation of the string.</returns>
        public static string LogicalToVisual(string logicalString, out int[] indexes, out int[] lengths, out CharData[] charData)
        {
            //Section 3:
            //1. seperate text into paragraphs
            //2. resulate each paragraph to its embeding levels of text
            //2.1 find the first character of type L, AL, or R.
            //3. reorder text elements

            //Section 3.3: Resolving Embedding Levels:
            //(1) determining the paragraph level.
            //(2) determining explicit embedding levels and directions.
            //(3) resolving weak types.
            //(4) resolving neutral types.
            //(5) resolving implicit embedding levels.

            ArrayList arrIndexes = new ArrayList();
            ArrayList arrLengths = new ArrayList();
            ArrayList arrCharData = new ArrayList();

            Paragraph[] pars = SplitStringToParagraphs(logicalString);
            StringBuilder sb = new StringBuilder();
            foreach (Paragraph p in pars)
            {
                sb.Append(p.BidiText);
                arrIndexes.AddRange(p.BidiIndexes);
                arrLengths.AddRange(p.BidiIndexLengths);
                arrCharData.AddRange(p.TextData);
            }

            indexes = (int[])arrIndexes.ToArray(typeof(int));
            lengths = (int[])arrLengths.ToArray(typeof(int));
            charData = (CharData[])arrCharData.ToArray(typeof(CharData));

            return sb.ToString();
        }
    }
}