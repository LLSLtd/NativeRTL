﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBidi;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    /// <summary>
    ///     Editable text input field.
    /// </summary>
    [AddComponentMenu("UI/Input Field RTL")]
    public class InputFieldRTL
        : Selectable,
            IUpdateSelectedHandler,
            IBeginDragHandler,
            IDragHandler,
            IEndDragHandler,
            IPointerClickHandler,
            ISubmitHandler,
            ICanvasElement
    {
        /// <summary>
        ///     Magic num
        /// </summary>
        private readonly float m_leftOffsetX = 4f;

        private readonly Event m_processingEvent = new Event();

        protected CanvasRenderer CachedCaretRenderer;

        [Multiline(30)]
        [SerializeField]
        protected string LogicalText;

        private RectTransform m_caretRectTrans;
        private readonly float m_CaretWidth = 1.0f;

        protected UIVertex[] m_CursorVerts = null;
        private Mesh m_mesh;

        [SerializeField]
        private List<int> m_cumulativeWrappedNewLines = new List<int>();

        [SerializeField]
        private NBidi.NBidi.Paragraph[] m_paragraphs;

        [SerializeField]
        protected Text TextField;

        [Multiline(30)]
        [SerializeField]
        protected string VisualText;

        private bool m_isLastActionWasClick = false;

        [SerializeField]
        private int m_logicalCaretPosition;

        private List<int> m_lines;
        private int m_textGenToLogicalLineNum;

        protected Mesh CaretMesh
        {
            get { return m_mesh ?? (m_mesh = new Mesh()); }
        }

        protected TextGenerator CachedTextGenerator
        {
            get { return TextField.cachedTextGenerator; }
        }

        public int LogicalCaretPosition
        {
            get { return m_logicalCaretPosition; }
            set { m_logicalCaretPosition = value; }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void Rebuild(CanvasUpdate executing)
        {
            switch (executing)
            {
                case CanvasUpdate.LatePreRender:
                    UpdateGeometry();

                    m_isLastActionWasClick = false;
                    break;
            }
        }

        public void LayoutComplete()
        {
        }

        public void GraphicUpdateComplete()
        {
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_mesh != null)
                DestroyImmediate(m_mesh);
            m_mesh = null;
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        public void OnPointerClick(PointerEventData eventData)
        {
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            m_isLastActionWasClick = true;

            EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            Vector2 localMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(TextField.rectTransform,
                eventData.position, eventData.pressEventCamera, out localMousePos);

            int lineNum;

            // this is the text generator position (including the zero-width char)
            var textGeneratorCaretPosition = GetCharacterIndexFromPosition(localMousePos, out lineNum);

            Debug.Log("Character index after press: " + textGeneratorCaretPosition);

            PopulateCachedTextGenerator(VisualText);
            LogicalCaretPosition = TextGenPosToLogicalPos(CachedTextGenerator, textGeneratorCaretPosition);

            UpdateLabel();
            eventData.Use();
        }

        protected int LogicalPosToTextGenPos(TextGenerator textGenerator, int logicalPos)
        {
            int res = 0;
            int lineNum;
            int lineEnd;
            int startCharIdx;

            #region Fake \n handling

            int nonWrappedLine = m_lines.Count - 1;

            // find the right line
            //
            // m_lines contains the line endings without \n's
            for (int i = 0; i < m_lines.Count; i++)
            {
                if (logicalPos > m_lines[i]) continue;
                nonWrappedLine = i;
                break;
            }

            // find the actual logical position WITH the line wraps
            logicalPos += m_cumulativeWrappedNewLines[nonWrappedLine];

            #endregion

            var paragraph = GetParagraph(textGenerator,
                logicalPos, out lineNum, out lineEnd, out startCharIdx);

            Debug.Log("[LOGICAL TO VISUAL] lineNum: " + lineNum +", tg line num: " + m_textGenToLogicalLineNum);

            bool isLineCorrectionNeeded = lineNum != m_textGenToLogicalLineNum;

            if (isLineCorrectionNeeded && m_isLastActionWasClick)
            {
                return CachedTextGenerator.lines[lineNum - 1].startCharIdx;
            }

            int bidiCorrection = 0;

            if (paragraph == null && lineNum == 0)
                return 0;

            if (paragraph == null)
                return startCharIdx;

            if (logicalPos == lineEnd)
            {
                res = startCharIdx;
            }
            else if (logicalPos == startCharIdx)
            {
                res = lineEnd;
            }
            else
            {
                int reverseIdx = paragraph.BidiIndexes.ToList().IndexOf(logicalPos - startCharIdx);
                var charData = paragraph.TextData[reverseIdx];
                var bidiCharacterType = charData._ct;

                int prevCharIdx = paragraph.BidiIndexes.ToList().IndexOf(logicalPos - startCharIdx - 1);
                var prevCharData = paragraph.TextData[prevCharIdx];
                var prevCharacterBidiType = prevCharData._ct;

                if (bidiCharacterType == BidiCharacterType.L && prevCharacterBidiType == BidiCharacterType.R)
                {
                    bidiCorrection = prevCharIdx;

                    // we just need to put the caret on the prev. ch. index
                    reverseIdx = 0;
                }
                else if (bidiCharacterType == BidiCharacterType.R && prevCharacterBidiType == BidiCharacterType.L)
                {
                    bidiCorrection = 1;
                }
                else if (bidiCharacterType == BidiCharacterType.R)
                {
                    bidiCorrection = 1;
                }

                res = reverseIdx + startCharIdx;
            }

            Debug.Log("TG Pos: " + (res + bidiCorrection));

            return res + bidiCorrection;
        }

        private NBidi.NBidi.Paragraph GetParagraph(TextGenerator textGenerator, int logicalPos, out int lineNum, out int lineEnd,
            out int startCharIdx)
        {
            lineNum = DetermineCharacterLine(logicalPos, textGenerator);
            lineEnd = GetLineEndPosition(textGenerator, lineNum);
            startCharIdx = textGenerator.lines[lineNum].startCharIdx;
            var paragraph = GetParagraph(textGenerator, logicalPos);
            return paragraph;
        }

        protected int TextGenPosToLogicalPos(TextGenerator textGenerator, int textGeneratorPos)
        {
            int lineNum;
            int lineEnd;
            int startCharIdx;
            var paragraph = GetParagraph(textGenerator, textGeneratorPos, out lineNum, out lineEnd, out startCharIdx);

            m_textGenToLogicalLineNum = lineNum;

            Debug.Log("lineNum: " + lineNum);

            int res = startCharIdx;

            if (paragraph == null) return res;

            if (textGeneratorPos == lineEnd)
            {
                res = startCharIdx;
            }
            else if (textGeneratorPos == startCharIdx)
            {
                res = lineEnd;
            }
            else
            {
                var bidiCorrection = 0;
                var lateCorrection = 0;
                var charData = paragraph.TextData[textGeneratorPos - startCharIdx];
                var bidiCharacterType = charData._ct;

                var prevCharData = paragraph.TextData[textGeneratorPos - startCharIdx - 1];

                if (bidiCharacterType == BidiCharacterType.L && prevCharData._ct == BidiCharacterType.R)
                {
                    bidiCorrection = -1;
                }
                else if (bidiCharacterType == BidiCharacterType.R)
                {
                    bidiCorrection = 0;
                    lateCorrection = 1;
                }

                res = paragraph.BidiIndexes[textGeneratorPos + bidiCorrection - startCharIdx] + lateCorrection +
                      startCharIdx;
            }

            var textGenPosToLogicalPos = res - m_cumulativeWrappedNewLines[lineNum];

            Debug.Log("textGenPosToLogicalPos: " + textGenPosToLogicalPos);

            return textGenPosToLogicalPos;
        }

        public void OnSubmit(BaseEventData eventData)
        {
        }

        public void OnUpdateSelected(BaseEventData eventData)
        {
            var consumedEvent = false;

            while (Event.PopEvent(m_processingEvent))
            {
                if (m_processingEvent.rawType != EventType.KeyDown)
                    continue;

                var editState = KeyPressed(m_processingEvent);

                if (editState == EditState.Continue)
                    consumedEvent = true;
                else
                    break;
            }

            if (consumedEvent)
                UpdateLabel();

            eventData.Use();
        }

        protected override void Start()
        {
            base.Start();

            LogicalCaretPosition = 0;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            // Debug.Log("Rect dimension changed, updating label");

            UpdateLabel();
        }

        private EditState KeyPressed(Event processingEvent)
        {
            // Debug.Log("KeyPressed: " + processingEvent.character);

            switch (processingEvent.keyCode)
            {
                case KeyCode.RightArrow:
                    LogicalCaretPosition = Mathf.Max(0, --LogicalCaretPosition);
                    break;

                case KeyCode.LeftArrow:
                    LogicalCaretPosition = Mathf.Max(0, ++LogicalCaretPosition);
                    break;
            }

            return EditState.Continue;
        }

        private bool IsValidChar(char c)
        {
            // Delete key on mac
            if (c == 127)
                return false;
            // Accept newline and tab
            if (c == '\t' || c == '\n')
                return true;

            return TextField.font.HasCharacter(c);
        }

        private List<int> CalculateLineEndings(string text)
        {
            var lineEndings = new List<int>();

            PopulateCachedTextGenerator(text);

            var linesCount = CachedTextGenerator.lines.Count;
            for (var index = 0; index < linesCount; index++)
            {
                if (index == linesCount - 1)
                    lineEndings.Add(text.Length - 1);
                else
                    lineEndings.Add(CachedTextGenerator.lines[index + 1].startCharIdx - 1);
            }

            return lineEndings;
        }

        private void PopulateCachedTextGenerator(string text)
        {
            var settings =
                TextField.GetGenerationSettings(TextField.rectTransform.rect.size);

            settings.horizontalOverflow = HorizontalWrapMode.Wrap;

            CachedTextGenerator.Populate(text, settings);
        }

        private void UpdateLabel()
        {
            // Enforce horizontal overflow
            TextField.horizontalOverflow = HorizontalWrapMode.Overflow;

            var c = m_processingEvent.character;

            if (IsValidChar(c))
            {
                AppendChar(c);
            }

            int totalNewLines = 0;
            m_cumulativeWrappedNewLines.Clear();
            m_cumulativeWrappedNewLines.Add(0);

            m_lines = CalculateLineEndings(LogicalText);
            var logicalWrapperSb = new StringBuilder();

            if (m_lines.Count == 1)
            {
                logicalWrapperSb.Append(LogicalText);
            }
            else
            {
                var start = 0;
                for (var index = 0; index < m_lines.Count; index++)
                {
                    var lineEndingIdx = m_lines[index];

                    var logicalTextSubstr = LogicalText.Substring(start, lineEndingIdx - start + 1);
                    bool newLineExisted = logicalTextSubstr.Any(e => e == '\n');

                    if (index > 0)
                        m_cumulativeWrappedNewLines.Add(totalNewLines);

                    if (!newLineExisted)
                    {
                        totalNewLines++;
                    }

                    var stringToAppend = logicalTextSubstr.Replace("\n", "");

                    if (index == m_lines.Count - 1)
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

            m_paragraphs = NBidi.NBidi.SplitStringToParagraphs(wrappedText);

            var logicalToVisual = NBidi.NBidi.LogicalToVisual(wrappedText);
            VisualText = logicalToVisual;
            TextField.text = logicalToVisual;

            PopulateCachedTextGenerator(logicalToVisual);

            MarkGeometryAsDirty();
            UpdateGeometry();
        }

        private string ReplaceCharAtIdx(string theString, char newChar, int idx)
        {
            StringBuilder sb = new StringBuilder(theString);
            sb[idx] = newChar;
            theString = sb.ToString();

            return theString;
        }

        BidiCharacterType GetCharacterType(int logicalPos)
        {
            Debug.Log("Determining char type for " + logicalPos);

            var lineNum = DetermineCharacterLine(logicalPos, CachedTextGenerator);
            var uiLineInfo = CachedTextGenerator.lines[lineNum];
            var paragraph = GetParagraph(CachedTextGenerator, logicalPos);

            if (paragraph == null)
            {
                return BidiCharacterType.R;
            }

            return paragraph.TextData[paragraph.BidiIndexes[Mathf.Max(0, logicalPos - 1 - uiLineInfo.startCharIdx)]]._ct;
        }

        private void AppendChar(char c)
        {
            var insertionIdx = Mathf.Min(LogicalCaretPosition, LogicalText.Length);
            LogicalText = LogicalText.Insert(insertionIdx, c.ToString());
            LogicalCaretPosition++;
        }

        protected void UpdateGeometry()
        {
            // No need to draw a cursor on mobile as its handled by the devices keyboard.
            if (CachedCaretRenderer == null && TextField != null)
            {
                var go = new GameObject(transform.name + " Input Caret");
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(TextField.transform.parent);
                go.transform.SetAsFirstSibling();
                go.layer = gameObject.layer;

                m_caretRectTrans = go.AddComponent<RectTransform>();
                CachedCaretRenderer = go.AddComponent<CanvasRenderer>();
                CachedCaretRenderer.SetMaterial(Graphic.defaultGraphicMaterial, Texture2D.whiteTexture);

                // Needed as if any layout is present we want the caret to always be the same as the text area.
                go.AddComponent<LayoutElement>().ignoreLayout = true;

                AssignPositioningIfNeeded();
            }

            if (CachedCaretRenderer == null)
                return;

            OnFillVBO(CaretMesh);
            CachedCaretRenderer.SetMesh(CaretMesh);
        }

        private void OnFillVBO(Mesh vbo)
        {
            using (var helper = new VertexHelper())
            {
                var inputRect = TextField.rectTransform.rect;
                var extents = inputRect.size;

                // get the text alignment anchor point for the text in local space
                var textAnchorPivot = Text.GetTextAnchorPivot(TextField.alignment);
                var refPoint = Vector2.zero;

                refPoint.x = Mathf.Lerp(inputRect.xMin, inputRect.xMax, textAnchorPivot.x);
                refPoint.y = Mathf.Lerp(inputRect.yMin, inputRect.yMax, textAnchorPivot.y);

                // Adjust the anchor point in screen space
                var roundedRefPoint = TextField.PixelAdjustPoint(refPoint);

                // Determine fraction of pixel to offset text mesh.
                // This is the rounding in screen space, plus the fraction of a pixel the text anchor pivot is from the corner of the text mesh.
                var roundingOffset = roundedRefPoint - refPoint + Vector2.Scale(extents, textAnchorPivot);
                roundingOffset.x = roundingOffset.x - Mathf.Floor(0.5f + roundingOffset.x);
                roundingOffset.y = roundingOffset.y - Mathf.Floor(0.5f + roundingOffset.y);

                GenerateCaret(helper, roundingOffset);

                helper.FillMesh(vbo);
            }
        }

        /// <summary>
        /// Find the paragraph corresponding to the logical position
        /// </summary>
        /// <param name="textGenPos"></param>
        /// <param name="logicalLine"></param>
        /// <param name="lineStartCharLogical"></param>
        /// <returns></returns>
        protected NBidi.NBidi.Paragraph GetParagraph(TextGenerator gen, int textGenPos)
        {
            if (m_paragraphs.Length == 0) return null;

            int lineNum = DetermineCharacterLine(textGenPos, gen);

            if (m_paragraphs.Length <= lineNum)
                return null;

            return m_paragraphs[lineNum];
        }

        private void GenerateCaret(VertexHelper vbo, Vector2 roundingOffset)
        {
            if (m_CursorVerts == null)
                CreateCursorVerts();

            var width = m_CaretWidth;

            PopulateCachedTextGenerator(VisualText);

            TextGenerator gen = CachedTextGenerator;

            var startPosition = Vector2.zero;

            int adjustedTextGenPos = LogicalPosToTextGenPos(CachedTextGenerator, LogicalCaretPosition);

            // Calculate startPosition
            if (adjustedTextGenPos < gen.characters.Count)
            {
                UICharInfo cursorChar = gen.characters[adjustedTextGenPos];
                startPosition.x = cursorChar.cursorPos.x;
            }

            startPosition.x /= TextField.pixelsPerUnit;

            // TODO: Only clamp when Text uses horizontal word wrap.
            if (startPosition.x > TextField.rectTransform.rect.xMax)
                startPosition.x = TextField.rectTransform.rect.xMax;

            int characterLine = DetermineCharacterLine(adjustedTextGenPos, gen);
            startPosition.y = gen.lines[characterLine].topY / TextField.pixelsPerUnit;
            float height = gen.lines[characterLine].height / TextField.pixelsPerUnit;

            for (var i = 0; i < m_CursorVerts.Length; i++)
                m_CursorVerts[i].color = Color.black;

            m_CursorVerts[0].position = new Vector3(startPosition.x, startPosition.y - height, 0.0f);
            m_CursorVerts[1].position = new Vector3(startPosition.x + width, startPosition.y - height, 0.0f);
            m_CursorVerts[2].position = new Vector3(startPosition.x + width, startPosition.y, 0.0f);
            m_CursorVerts[3].position = new Vector3(startPosition.x, startPosition.y, 0.0f);

            if (roundingOffset != Vector2.zero)
                for (var i = 0; i < m_CursorVerts.Length; i++)
                {
                    var uiv = m_CursorVerts[i];
                    uiv.position.x += roundingOffset.x;
                    uiv.position.y += roundingOffset.y;
                }

            vbo.AddUIVertexQuad(m_CursorVerts);

            var screenHeight = Screen.height;
            // Removed multiple display support until it supports none native resolutions(case 741751)
            //int displayIndex = m_TextComponent.canvas.targetDisplay;
            //if (Screen.fullScreen && displayIndex < Display.displays.Length)
            //    screenHeight = Display.displays[displayIndex].renderingHeight;

            startPosition.y = screenHeight - startPosition.y;
            //  Input.compositionCursorPos = startPosition;
        }

        /// <summary>
        /// TODO: adjust to RTL (??)
        /// </summary>
        /// <param name="charPos"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        private int DetermineCharacterLine(int charPos, TextGenerator generator)
        {
            for (int i = 0; i < generator.lineCount - 1; ++i)
            {
                if (generator.lines[i + 1].startCharIdx > charPos)
                    return i;
            }

            return generator.lineCount - 1;
        }

        private int LineUpCharacterPosition(int originalPos, bool goToFirstChar)
        {
            if (originalPos >= CachedTextGenerator.characters.Count)
                return 0;

            UICharInfo originChar = CachedTextGenerator.characters[originalPos];
            int originLine = DetermineCharacterLine(originalPos, CachedTextGenerator);

            // We are on the first line return first character
            if (originLine <= 0)
                return goToFirstChar ? 0 : originalPos;

            int endCharIdx = CachedTextGenerator.lines[originLine].startCharIdx - 1;

            for (int i = CachedTextGenerator.lines[originLine - 1].startCharIdx; i < endCharIdx; ++i)
            {
                if (CachedTextGenerator.characters[i].cursorPos.x >= originChar.cursorPos.x)
                    return i;
            }
            return endCharIdx;
        }

        private void CreateCursorVerts()
        {
            m_CursorVerts = new UIVertex[4];

            for (int i = 0; i < m_CursorVerts.Length; i++)
            {
                m_CursorVerts[i] = UIVertex.simpleVert;
                m_CursorVerts[i].uv0 = Vector2.zero;
            }
        }

        private void AssignPositioningIfNeeded()
        {
            if (TextField != null && m_caretRectTrans != null &&
                (m_caretRectTrans.localPosition != TextField.rectTransform.localPosition ||
                 m_caretRectTrans.localRotation != TextField.rectTransform.localRotation ||
                 m_caretRectTrans.localScale != TextField.rectTransform.localScale ||
                 m_caretRectTrans.anchorMin != TextField.rectTransform.anchorMin ||
                 m_caretRectTrans.anchorMax != TextField.rectTransform.anchorMax ||
                 m_caretRectTrans.anchoredPosition != TextField.rectTransform.anchoredPosition ||
                 m_caretRectTrans.sizeDelta != TextField.rectTransform.sizeDelta ||
                 m_caretRectTrans.pivot != TextField.rectTransform.pivot))
            {
                m_caretRectTrans.localPosition = TextField.rectTransform.localPosition;
                m_caretRectTrans.localRotation = TextField.rectTransform.localRotation;
                m_caretRectTrans.localScale = TextField.rectTransform.localScale;
                m_caretRectTrans.anchorMin = TextField.rectTransform.anchorMin;
                m_caretRectTrans.anchorMax = TextField.rectTransform.anchorMax;
                m_caretRectTrans.anchoredPosition = TextField.rectTransform.anchoredPosition;
                m_caretRectTrans.sizeDelta = TextField.rectTransform.sizeDelta;
                m_caretRectTrans.pivot = TextField.rectTransform.pivot;
            }
        }

        private int GetUnclampedCharacterLineFromPosition(Vector2 pos, TextGenerator generator)
        {
            //            if (!multiLine)
            //                return 0;

            // transform y to local scale
            float y = pos.y * TextField.pixelsPerUnit;
            float lastBottomY = 0.0f;

            for (int i = 0; i < generator.lineCount; ++i)
            {
                float topY = generator.lines[i].topY;
                float bottomY = topY - generator.lines[i].height;

                // pos is somewhere in the leading above this line
                if (y > topY)
                {
                    // determine which line we're closer to
                    float leading = topY - lastBottomY;
                    if (y > topY - 0.5f * leading)
                        return i - 1;
                    else
                        return i;
                }

                if (y > bottomY)
                    return i;

                lastBottomY = bottomY;
            }

            // Position is after last line.
            return generator.lineCount;
        }
        private static int GetLineEndPosition(TextGenerator gen, int line)
        {
            line = Mathf.Max(line, 0);
            if (line + 1 < gen.lines.Count)
                return gen.lines[line + 1].startCharIdx - 1;
            return gen.characterCountVisible;
        }

        protected int GetCharacterIndexFromPosition(Vector2 pos, out int lineNum)
        {
            lineNum = -1;

            TextGenerator gen = CachedTextGenerator;

            if (gen.lineCount == 0)
                return 0;

            int line = GetUnclampedCharacterLineFromPosition(pos, gen);

            lineNum = line;

            if (line < 0)
                return 0;
            if (line >= gen.lineCount)
                return gen.characterCountVisible;

            int startCharIndex = gen.lines[line].startCharIdx;
            int endCharIndex = GetLineEndPosition(gen, line);

            for (int i = startCharIndex; i < endCharIndex; i++)
            {
                if (i >= gen.characterCountVisible)
                    break;

                UICharInfo charInfo = gen.characters[i];
                Vector2 charPos = charInfo.cursorPos / TextField.pixelsPerUnit;

                float distToCharStart = pos.x - charPos.x;
                float distToCharEnd = charPos.x + (charInfo.charWidth / TextField.pixelsPerUnit) - pos.x;
                if (distToCharStart < distToCharEnd)
                    return i;
            }

            return endCharIndex;
        }

        private void MarkGeometryAsDirty()
        {
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        protected enum EditState
        {
            Continue,
            Finish
        }
    }
}