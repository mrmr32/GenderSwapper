using System;
using System.Collections.Generic;
using static MeshVR.PresetManager;

namespace JustAnotherUser {
    public class AdjustableElement {
        private List<AdjustableElement> _subelements;
        private AdjustableElement _superelement;
        private float? _value;
        private float _inheritedValue;

        public string name { get; set; }
        public int depth {
            get {
                return (this._superelement == null) ? 0 : this._superelement.depth+1;
            }
        }

        public AdjustableElement(string name, float? value = null, AdjustableElement superelement = null) {
            this._subelements = new List<AdjustableElement>();
            if (superelement != null) superelement.SetSubelement(this);

            this.name = name;
            this._value = value;
            this._inheritedValue = 0f;
            this._superelement = superelement;
        }

        private void SetSubelement(AdjustableElement element) {
            this._subelements.Add(element);
        }

        virtual public void SetValue(float? value) {
            this._value = value;
            foreach (AdjustableElement element in this._subelements) element.UpdateInheritedValue(this.GetValue()); // expand the value to the sons
        }

        virtual protected void UpdateInheritedValue(float value) {
            if (this.IsValueSetted()) return; // no need to change anything
            this._inheritedValue = value;
            foreach (AdjustableElement element in this._subelements) element.UpdateInheritedValue(value);
        }

        public float GetValue() {
            return (this.IsValueSetted() ? this._value.Value : this._inheritedValue);
        }

        public IEnumerable<AdjustableElement> GetSubelements() {
            return this._subelements;
        }

        public IEnumerable<AdjustableElement> GetAll() {
            yield return this;
            foreach (AdjustableElement e in this.GetSubelements()) {
                foreach (AdjustableElement sub in e.GetAll()) {
                    yield return sub;
                }
            }
        }

        public void Clear() {
            this._subelements.Clear();
        }

        public bool IsLeaf() {
            return this._subelements.Count == 0;
        }

        public bool IsValueSetted() {
            return this._value != null;
        }
    }

    public class RangeAdjustableElement : AdjustableElement {
        private float _min, _max;
        public RangeAdjustableElement(string name, float min = 0f, float max = 1f, float? value = null, AdjustableElement superelement = null) : base(name, value, superelement) {
            this._min = min;
            this._max = max;
        }

        protected float PercentageToValue(float percent) {
            float diff = this._max - this._min;
            return this._min + diff*percent;
        }

        override public void SetValue(float? value) {
            base.SetValue(value == null ? (float?)null : this.PercentageToValue(value.Value));
        }

        override protected void UpdateInheritedValue(float value) {
            base.UpdateInheritedValue(this.PercentageToValue(value));
        }
    }

    public class RangeTriggerElement : RangeAdjustableElement {
        private float _trigger;
        private Action _lowerTrigger,
                        _higherOrEqualTrigger;
        public RangeTriggerElement(string name, float trigger, Action lowerTrigger, Action higherOrEqualTrigger, float min = 0f, float max = 1f, AdjustableElement superelement = null) : base(name, min, max, null, superelement) {
            this._trigger = trigger;
            this._lowerTrigger = lowerTrigger;
            this._higherOrEqualTrigger = higherOrEqualTrigger;
        }

        override public void SetValue(float? value) {
            base.SetValue(value);
            if (this.GetValue() < this._trigger) this._lowerTrigger();
            else this._higherOrEqualTrigger();
        }

        override protected void UpdateInheritedValue(float value) {
            base.UpdateInheritedValue(value);
            if (this.GetValue() < this._trigger) this._lowerTrigger();
            else this._higherOrEqualTrigger();
        }

        public float GetTriggerValue() {
            return this._trigger;
        }
    }

    public class MorphAdjustableElement : RangeAdjustableElement {
        private DAZMorph _morph;
        public MorphAdjustableElement(DAZMorph morph, float min = 0f, float max = 1f, AdjustableElement superelement = null, string customName = null) : base(customName == null ? morph.uid : customName, min, max, null, superelement) {
            this._morph = morph;
        }

        override public void SetValue(float? value) {
            base.SetValue(value);
            MorphHelper.SetMorphValue(this._morph, this.GetValue());
        }

        override protected void UpdateInheritedValue(float value) {
            base.UpdateInheritedValue(value);
            MorphHelper.SetMorphValue(this._morph, this.GetValue());
        }
    }

    public class StorableAdjustableElement : RangeAdjustableElement {
        private JSONStorable _storable;
        private string _id;
        public StorableAdjustableElement(JSONStorable storable, string id, float min = 0f, float max = 1f, AdjustableElement superelement = null, string customName = null) : base(customName == null ? id : customName, min, max, null, superelement) {
            this._storable = storable;
            this._id = id;
        }

        override public void SetValue(float? value) {
            base.SetValue(value);
            this._storable.SetFloatParamValue(this._id, this.GetValue());
        }

        override protected void UpdateInheritedValue(float value) {
            base.UpdateInheritedValue(value);
            this._storable.SetFloatParamValue(this._id, this.GetValue());
        }
    }
}
