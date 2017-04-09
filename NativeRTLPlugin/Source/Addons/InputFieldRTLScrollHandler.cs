using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NativeRTL
{
    [RequireComponent(typeof(InputFieldRTLAdapter))]
    class InputFieldRTLScrollHandler : MonoBehaviour
    {
        private InputFieldRTLAdapter m_inputFieldRtlAdapter;

        [SerializeField]
        private ScrollRect m_scrollRect;

        void Awake()
        {
            m_inputFieldRtlAdapter = GetComponent<InputFieldRTLAdapter>();

            // subscribe to change event
            m_inputFieldRtlAdapter.InputFieldRtl.onCaretPositionChangedEvent.AddListener(OnCaretPosChanged);
        }

        private void OnCaretPosChanged()
        {
            // calculate the caret positon in normalized coordinates
            var localCaretRect = m_inputFieldRtlAdapter.InputFieldRtl.CaretCursorInfo;
            //Debug.Log("localCaretRect yMin: " + localCaretRect.min);
            //Debug.Log("localCaretRect yMax: " + localCaretRect.max);
            //Debug.Log("m_scrollContentTransform:" + m_scrollContentTransform.rect.min);
            //Debug.Log("m_scrollContentTransform:" + m_scrollContentTransform.rect.max);

            var scrollContentTransfrom = m_scrollRect.content;
            var scrollViewTransform = m_scrollRect.viewport;

            var scrollViewTransformRect = scrollViewTransform.rect;

            var worldCaretPosMin = scrollContentTransfrom.TransformPoint(new Vector2(localCaretRect.min.x, localCaretRect.min.y));
            Vector2 localCaretPosMin = scrollViewTransform.InverseTransformPoint(worldCaretPosMin);

            var worldCaretPosMax = scrollContentTransfrom.TransformPoint(new Vector2(localCaretRect.max.x, localCaretRect.max.y));
            Vector2 localCaretPosMax = scrollViewTransform.InverseTransformPoint(worldCaretPosMax);

            if (!scrollViewTransformRect.ContainsInclusive(localCaretPosMin))
            {
                // caret is outside of visible view, calculate the correct scroll amount
                var pointToNormalized = Rect.PointToNormalized(m_scrollRect.viewport.rect, localCaretPosMin);
                Debug.Log("**************: " + pointToNormalized);
                m_scrollRect.verticalNormalizedPosition = pointToNormalized.y;
            }
            else if (!scrollViewTransformRect.ContainsInclusive(localCaretPosMax))
            {
                // caret is outside of visible view, calculate the correct scroll amount
                var pointToNormalized = Rect.PointToNormalized(m_scrollRect.viewport.rect, localCaretPosMax);
                Debug.Log("**************: " + pointToNormalized);
                m_scrollRect.verticalNormalizedPosition = pointToNormalized.y;
            }
        }
    }
}
