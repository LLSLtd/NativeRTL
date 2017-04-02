using System.Reflection;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public class InputFieldRTLAdapter : InputField
    {
        private InputFieldRTL m_inputFieldRTL;

        private InputFieldRTL InputFieldRtl => m_inputFieldRTL ?? InitInputField();

        private InputFieldRTL InitInputField()
        {
            m_inputFieldRTL = gameObject.AddComponent<InputFieldRTL>();
            m_inputFieldRTL.textComponent = base.textComponent;

            return m_inputFieldRTL;
        }

        internal bool m_isChangingTextValue = false;

        // public new Text textComponent => InputFieldRtl.textComponent;

        public override void OnDrag(PointerEventData eventData)
        {
            InputFieldRtl.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            InputFieldRtl.OnEndDrag(eventData);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            InputFieldRtl.OnPointerClick(eventData);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            InputFieldRtl.OnPointerDown(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            InputFieldRtl.OnSubmit(eventData);
        }

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            InputFieldRtl.OnUpdateSelected(eventData);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            InputFieldRtl.OnSelect(eventData);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            InputFieldRtl.OnDeselect(eventData);
        }

        #region Overrides of InputField

        protected override void OnValidate()
        {
        }

        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }

        protected override void LateUpdate()
        {
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            InputFieldRtl.OnBeginDrag(eventData);
        }

        public override void Rebuild(CanvasUpdate update)
        {
            InputFieldRtl.Rebuild(update);
        }

        public override void LayoutComplete()
        {
            InputFieldRtl.LayoutComplete();
        }

        public override void GraphicUpdateComplete()
        {
            InputFieldRtl.GraphicUpdateComplete();
        }

        #endregion
    }
}