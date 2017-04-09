using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NativeRTL
{
    [RequireComponent(typeof(InputFieldRTLAdapter))]
    public class InputFieldRTLScrollHandler : MonoBehaviour
    {
        private InputFieldRTLAdapter m_inputFieldRtlAdapter;

        [SerializeField]
        [Obfuscation(Exclude = true)]
        private ScrollRect m_scrollRect;

        private bool m_destroyed = false;

        public void Awake()
        {
            m_inputFieldRtlAdapter = GetComponent<InputFieldRTLAdapter>();

            // subscribe to change event
            m_inputFieldRtlAdapter.InputFieldRtl.onCaretPositionChangedEvent.AddListener(OnCaretPosChanged);
        }

        public void OnDestroy()
        {
            m_destroyed = true;
        }

        private void OnCaretPosChanged()
        {
            if (m_destroyed)
                return;

            var localCaretRect = m_inputFieldRtlAdapter.InputFieldRtl.CaretCursorInfo;

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
                var pointToNormalized = Rect.PointToNormalized(m_scrollRect.content.rect, localCaretRect.min);
                m_scrollRect.verticalNormalizedPosition = pointToNormalized.y;
            }
            else if (!scrollViewTransformRect.ContainsInclusive(localCaretPosMax))
            {
                // caret is outside of visible view, calculate the correct scroll amount
                var pointToNormalized = Rect.PointToNormalized(m_scrollRect.content.rect, localCaretRect.max);
                m_scrollRect.verticalNormalizedPosition = pointToNormalized.y;
            }
        }
    }
}
