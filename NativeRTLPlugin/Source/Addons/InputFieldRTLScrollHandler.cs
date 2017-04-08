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

        [SerializeField]
        private RectTransform m_scrollContent;

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
            Debug.Log("localCaretRect yMin: " + localCaretRect.min);
            Debug.Log("localCaretRect yMax: " + localCaretRect.max);
            Debug.Log("m_scrollContent:" + m_scrollContent.rect.min);
            Debug.Log("m_scrollContent:" + m_scrollContent.rect.max);

            if (!m_scrollContent.rect.ContainsInclusive(localCaretRect.min) || !m_scrollContent.rect.ContainsInclusive(localCaretRect.max))
            {
                Debug.Log("SCROLL");
            }
        }
    }
}
