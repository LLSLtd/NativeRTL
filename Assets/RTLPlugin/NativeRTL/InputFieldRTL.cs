using System.Collections.Generic;
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
        private NBidi.NBidi.Paragraph[] m_paragraphs;

        [SerializeField]
        protected Text TextField;

        [Multiline(30)]
        [SerializeField]
        protected string VisualText;

        [SerializeField]
        private int m_logicalCaretPosition;

        private List<int> m_lines;

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

            Debug.Log("Rect dimension changed, updating label");
            UpdateLabel();
        }

        private EditState KeyPressed(Event processingEvent)
        {
            Debug.Log("KeyPressed: " + processingEvent.character);
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

            var settings =
                TextField.GetGenerationSettings(TextField.rectTransform.rect.size);

            settings.horizontalOverflow = HorizontalWrapMode.Wrap;

            CachedTextGenerator.Populate(text, settings);

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

        private void UpdateLabel()
        {
            // Enforce horizontal overflow
            TextField.horizontalOverflow = HorizontalWrapMode.Overflow;

            var c = m_processingEvent.character;

            if (IsValidChar(c))
            {
                AppendChar(c);
            }

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

                    var stringToAppend = LogicalText.Substring(start, lineEndingIdx - start + 1).Replace("\n", "");

                    // stringToAppend = stringToAppend.TrimEnd(' ');

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

            MarkGeometryAsDirty();
            UpdateGeometry();
        }

        private void AppendChar(char c)
        {
            LogicalCaretPosition++;
            LogicalText += c;
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
                //                if (!isFocused)
                //                {
                //                    helper.FillMesh(vbo);
                //                    return;
                //                }

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

                //                if (!hasSelection)
                GenerateCaret(helper, roundingOffset);
                //                else
                //                    GenerateHightlight(helper, roundingOffset);

                helper.FillMesh(vbo);
            }
        }

        /// <summary>
        /// Find the paragraph corresponding to the logical position
        /// </summary>
        /// <param name="logicalCharacterPosition"></param>
        /// <param name="logicalLine"></param>
        /// <param name="lineStartCharLogical"></param>
        /// <returns></returns>
        protected NBidi.NBidi.Paragraph GetParagraph(TextGenerator gen, int logicalCharacterPosition)
        {
            if (m_paragraphs.Length == 0) return null;

            int logicalCharacterLine = DetermineCharacterLine(logicalCharacterPosition, gen);

            if (m_paragraphs.Length <= logicalCharacterLine)
                return null;

            return m_paragraphs[logicalCharacterLine];
        }

        private void GenerateCaret(VertexHelper vbo, Vector2 roundingOffset)
        {
            //            if (!m_CaretVisible)
            //                return;

            if (m_CursorVerts == null)
                CreateCursorVerts();

            var width = m_CaretWidth;

            int adjustedPos = LogicalCaretPosition;

            TextGenerator gen = CachedTextGenerator;

            // TODO: optimize this
            int logicalCharacterLine = DetermineCharacterLine(adjustedPos, gen);
            var lineStartCharLogical = gen.lines[logicalCharacterLine].startCharIdx;

            BidiCharacterType charType = BidiCharacterType.R;

            if (m_paragraphs.Length != 0)
            {
                var adjustedLogicalPosition = Mathf.Max(0, LogicalCaretPosition);
                var paragraph = GetParagraph(gen, adjustedLogicalPosition);
                if (paragraph == null)
                {
                    adjustedPos = lineStartCharLogical;
                }
                else
                {
                    if ((paragraph.embedding_level & 1) != 0)
                    {
                        adjustedPos = lineStartCharLogical;
                    }
                    else
                    {
                        var bidiIndices = paragraph.BidiIndexes;
                        var paragraphCharIdx = Mathf.Max(0, adjustedLogicalPosition - lineStartCharLogical - 1);
                        var visualParagraphCharIdx = bidiIndices.Length > 0 ? bidiIndices[paragraphCharIdx] : 0;

                        charType = paragraph.TextData[paragraphCharIdx]._ct;

                        adjustedPos = Mathf.Max(0, visualParagraphCharIdx + lineStartCharLogical);
                    }
                }
            }

            if (gen == null)
                return;

            if (gen.lineCount == 0)
                return;

            var startPosition = Vector2.zero;

            // Calculate startPosition
            if (adjustedPos < gen.characters.Count)
            {
                UICharInfo cursorChar = gen.characters[adjustedPos];

                startPosition.x = cursorChar.cursorPos.x;
            }
            startPosition.x /= TextField.pixelsPerUnit;

            // TODO: Only clamp when Text uses horizontal word wrap.
            if (startPosition.x > TextField.rectTransform.rect.xMax)
                startPosition.x = TextField.rectTransform.rect.xMax;

            int characterLine = DetermineCharacterLine(adjustedPos, gen);
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