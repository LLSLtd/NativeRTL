using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NativeRTL
{
    public class TextRTL : Text
    {
        #region Overrides of Text

        /// <summary>
        /// Sets / gets the logical text
        /// </summary>
        public override string text
        {
            get
            {
                return !gameObject.GetComponentInParent<InputFieldRTLAdapter>() ? LogicalToVisual(base.text) : base.text;
            }
            set { base.text = value; }
        }

        #endregion

        private List<int> CalculateLineEndings(string text)
        {
            var lineEndings = new List<int>();

            PopulateCachedTextGenerator(text, HorizontalWrapMode.Wrap);

            var lineCount = cachedTextGenerator.lineCount;

            for (var index = 0; index < lineCount; index++)
                if (index == lineCount - 1)
                    lineEndings.Add(text.Length - 1);
                else
                    lineEndings.Add(cachedTextGenerator.lines[index + 1].startCharIdx - 1);

            return lineEndings;
        }

        private void PopulateCachedTextGenerator(string logicalText, HorizontalWrapMode settingsHorizontalOverflow)
        {
            var settings = GetGenerationSettings(rectTransform.rect.size);

            settings.generateOutOfBounds = true;
            // settings.updateBounds = true;
            settings.horizontalOverflow = settingsHorizontalOverflow;

            cachedTextGenerator.Populate(logicalText, settings);
        }

        public override void OnRebuildRequested()
        {
            if (!gameObject.GetComponentInParent<InputFieldRTLAdapter>())
                horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        protected string LogicalToVisual(string logicalText)
        {
            var lineEndings = CalculateLineEndings(logicalText);
            var logicalWrapperSb = new StringBuilder();

            if (lineEndings.Count == 1)
            {
                logicalWrapperSb.Append(logicalText);
            }
            else
            {
                var start = 0;
                for (var index = 0; index < lineEndings.Count; index++)
                {
                    var lineEndingIdx = lineEndings[index];

                    var logicalTextSubstr = logicalText.Substring(start, lineEndingIdx - start + 1);

                    var stringToAppend = logicalTextSubstr.Replace("\n", "");

                    if (index == lineEndings.Count - 1)
                    {
                        logicalWrapperSb.Append(stringToAppend);
                        break;
                    }

                    logicalWrapperSb.Append(stringToAppend);
                    logicalWrapperSb.Append('\n');

                    start = lineEndingIdx + 1;
                }
            }

            var wrappedText = logicalWrapperSb.ToString();

            return NBidi.NBidi.LogicalToVisual(wrappedText);
        }
    }
}