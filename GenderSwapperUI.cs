using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static JSONStorableBool;

namespace JustAnotherUser {
    class GenderSwapperUIColumn {
        private MVRScript _script;
        private string _baseName;

        public JSONStorableString nameLabel { get; private set; }
        private JSONStorableUrl _loadPresetButton;
        public JSONStorableString morphList { get; private set; }
        public JSONStorableString bodyLabel { get; private set; }
        public JSONStorableString irisLabel { get; private set; }
        public JSONStorableString textureList { get; private set; }

        private JSONStorableString.SetStringCallback _loadPresetButtonClikedEvent;
        public JSONStorableString.SetStringCallback loadPresetButtonClikedEvent {
            get {
                return this._loadPresetButtonClikedEvent;
            }
            set {
                this._loadPresetButtonClikedEvent = value;
                if (this._loadPresetButton != null && value != null) this._loadPresetButton.setCallbackFunction = value;
            }
        }

        public GenderSwapperUIColumn(MVRScript script, string baseName) {
            this._script = script;
            this._baseName = baseName;

            this.nameLabel = new JSONStorableString(baseName + "name", "");
            this.morphList = new JSONStorableString(baseName + "morphs", "");
            this.bodyLabel = new JSONStorableString(baseName + "body", "");
            this.irisLabel = new JSONStorableString(baseName + "iris", "");
            this.textureList = new JSONStorableString(baseName + "textures", "");
        }

        public void CreateUI(bool right = false) {
            UIDynamicTextField nameLabelObject = this._script.CreateTextField(this.nameLabel, right);
            nameLabelObject.height = 40f;

            this._loadPresetButton = new JSONStorableUrl(this._baseName + "load", string.Empty, (JSONStorableString.SetStringCallback)((val) => {}), "vap", "Custom/Atom/Person/Appearance");
            //this._loadPresetButton.suggestedPathGroup = "DAZCharacterTexture";
            UIDynamicButton btn = this._script.CreateButton(this._baseName + "load_btn", right);
            this._loadPresetButton.fileBrowseButton = btn.button;

            UIDynamicTextField morphListObject = this._script.CreateTextField(this.morphList, right);
            morphListObject.height = 120f;
            UIDynamicTextField bodyLabelObject = this._script.CreateTextField(this.bodyLabel, right);
            bodyLabelObject.height = 40f;
            UIDynamicTextField irisLabelObject = this._script.CreateTextField(this.irisLabel, right);
            irisLabelObject.height = 40f;
            UIDynamicTextField textureListObject = this._script.CreateTextField(this.textureList, right);
            textureListObject.height = 120f;
        }
    }

    class GenderSwapperUI {
        private GenderSwapper _script;

        private GenderSwapperUIColumn _soruce,
                                      _destiny;

        private AdjustableElementsGenderSwapperUI _adjustableElements;

        private JSONStorableFloat _transformationProgress;
        private UIDynamicSlider _slider;
        private UIDynamicButton _btn;

        public GenderSwapperUI(GenderSwapper script) {
            this._script = script;
            this._adjustableElements = new AdjustableElementsGenderSwapperUI(script);

            this._soruce = new GenderSwapperUIColumn(script, "source_");
            this._destiny = new GenderSwapperUIColumn(script, "destiny_");
        }

        public void SetLoadPresetButtonEvent(JSONStorableString.SetStringCallback ua, bool applyToDestiny) {
            if (applyToDestiny) this._destiny.loadPresetButtonClikedEvent = ua;
            else this._soruce.loadPresetButtonClikedEvent = ua;
        }

        public void SetName(string name, bool applyToDestiny) {
            if (applyToDestiny) this._destiny.nameLabel.val = name;
            else this._soruce.nameLabel.val = name;
        }

        public void SetIris(string name, bool applyToDestiny) {
            name = "Iris: " + name;

            if (applyToDestiny) this._destiny.irisLabel.val = name;
            else this._soruce.irisLabel.val = name;
        }

        public void SetBody(string name, bool applyToDestiny) {
            name = "Body: " + name;

            if (applyToDestiny) this._destiny.bodyLabel.val = name;
            else this._soruce.bodyLabel.val = name;
        }

        public void SetMorphList(string list, bool applyToDestiny) {
            if (applyToDestiny) this._destiny.morphList.val = list;
            else this._soruce.morphList.val = list;
        }

        public void SetTextureList(string list, bool applyToDestiny) {
            if (applyToDestiny) this._destiny.textureList.val = list;
            else this._soruce.textureList.val = list;
        }

        public void SetOnButtonClick(UnityAction e) {
            this._btn.button.onClick.AddListener(e);
        }

        public float GetSliderProgress() {
            return this._slider.slider.value;
        }

        public AdjustableElementsGenderSwapperUI GetSubUI() {
            return this._adjustableElements;
        }

        public void SetupUI() {
            this._transformationProgress = new JSONStorableFloat("progress", 0f, 0f, 1f);
            this._script.RegisterFloat(this._transformationProgress);
            this._slider = this._script.CreateSlider(this._transformationProgress);

            this._btn = this._script.CreateButton("Spawn penis (only for female destiny)", true);
            this._btn.height = this._slider.height;

            this._soruce.CreateUI(false);
            this._destiny.CreateUI(true);

            // TODO move this to a different view
            this._adjustableElements.SetupUI();
        }

        public void UpdateSliders(AdjustableElement globalSlider) {
            this._transformationProgress.val = globalSlider.GetValue();
            this._slider.slider.onValueChanged.RemoveAllListeners();
            this._slider.slider.onValueChanged.AddListener((e) => {
                globalSlider.SetValue(e);
            });

            this._adjustableElements.UpdateSliders(globalSlider);
            globalSlider.SetValue(this.GetSliderProgress()); // update adjustable values
        }
    }

    class AdjustableElementsGenderSwapperUI {
        private GenderSwapper _script;

        private List<UIDynamic> _uiElements;
        private IDictionary<string,JSONStorableParam> _params, _defaultParams;

        public AdjustableElementsGenderSwapperUI(GenderSwapper script) {
            this._script = script;
            this._uiElements = new List<UIDynamic>();
            this._params = new Dictionary<string, JSONStorableParam>();
            this._defaultParams = new Dictionary<string, JSONStorableParam>();
        }

        public void SetupUI() {
            JSONStorableString msg = new JSONStorableString("overrideProgressMsg", "-- Override progress --");
            UIDynamicTextField bodyLabelObject = this._script.CreateTextField(msg);
            bodyLabelObject.height = 80f;

            UIDynamic spacer = this._script.CreateSpacer(true);
            spacer.height = bodyLabelObject.height;
        }

        public void Clear(ICollection<string> keepStorables = null) {
            foreach (UIDynamic uiEelement in this._uiElements) {
                if (uiEelement.GetType() == typeof(UIDynamicSlider)) this._script.RemoveSlider((UIDynamicSlider)uiEelement);
                else if (uiEelement.GetType() == typeof(UIDynamicToggle)) this._script.RemoveToggle((UIDynamicToggle)uiEelement);
            }
            this._uiElements.Clear();

            if (keepStorables == null) keepStorables = new List<string>();
            foreach (JSONStorableParam param in new List<JSONStorableParam>(this._params.Values)) {
                if (keepStorables.Contains(param.name)) continue;

                if (param.GetType() == typeof(JSONStorableBool)) this._script.DeregisterBool((JSONStorableBool)param);
                else if (param.GetType() == typeof(JSONStorableFloat)) this._script.DeregisterFloat((JSONStorableFloat)param);
                this._params.Remove(param.name);
            }
        }

        
        public JSONClass GetJSON() {
            JSONClass r = new JSONClass();
            JSONClass bools = new JSONClass(),
                    floats = new JSONClass();

            foreach (JSONStorableParam param in this._params.Values) {
                if (param.GetType() == typeof(JSONStorableBool) && ((JSONStorableBool)param).val) bools[param.name].AsBool = true;
                else if (param.GetType() == typeof(JSONStorableFloat)) {
                    JSONStorableFloat p = (JSONStorableFloat)param;
                    if (p.defaultVal != p.val) floats[param.name].AsFloat = p.val;
                }
            }

            r["bools"] = bools;
            r["floats"] = floats;

            return r;
        }
        
        public void RestoreFromJSON(JSONClass jc, string subScenePrefix = "", bool isMerge = true, bool setMissingToDefault = true) {
            if (jc == null) return;

            this._defaultParams = new Dictionary<string, JSONStorableParam>();
            foreach (KeyValuePair<string,JSONNode> e in jc["bools"].AsObject) {
                JSONStorableBool overrideProgress = new JSONStorableBool(e.Key, false);
                overrideProgress.val = e.Value.AsBool;
                this._script.RegisterBool(overrideProgress);
                this._params.Add(overrideProgress.name, overrideProgress);
                this._defaultParams.Add(overrideProgress.name, overrideProgress);
            }
            foreach (KeyValuePair<string,JSONNode> e in jc["floats"].AsObject) {
                JSONStorableFloat progress = new JSONStorableFloat(e.Key, 0f, 0f, 1f);
                progress.val = e.Value.AsFloat;
                this._script.RegisterFloat(progress);
                this._params.Add(progress.name, progress);
                this._defaultParams.Add(progress.name, progress);
            }
        }

        public void UpdateSliders(AdjustableElement globalSlider) {
            var keepStorables = new HashSet<string>(this._defaultParams.Keys);
            foreach (AdjustableElement e in globalSlider.GetAll()) {
                keepStorables.Add(GetOverrideStorableId(e));
                keepStorables.Add(GetProgressStorableId(e));
            }

            this.Clear(keepStorables);

            foreach (var subslider in globalSlider.GetSubelements()) {
                this._UpdateSliders(subslider);
            }
        }

        private static string GetProgressStorableId(AdjustableElement slider) {
            return slider.name + "_progress";
        }

        private static string GetOverrideStorableId(AdjustableElement slider) {
            return new String('>', slider.depth == 0 ? 0 : (slider.depth - 1)) + slider.name + "_activated";
        }

        private void _UpdateSliders(AdjustableElement slider) {
            bool progressFound = this._params.ContainsKey(GetProgressStorableId(slider));
            JSONStorableFloat progress = (progressFound ? this._params[GetProgressStorableId(slider)] as JSONStorableFloat : new JSONStorableFloat(GetProgressStorableId(slider), 0f, 0f, 1f));
            JSONStorableBool overrideProgress;

            if (this._params.ContainsKey(GetOverrideStorableId(slider))) overrideProgress = this._params[GetOverrideStorableId(slider)] as JSONStorableBool;
            else {
                overrideProgress = new JSONStorableBool(GetOverrideStorableId(slider), false);
                this._script.RegisterBool(overrideProgress);
                this._params.Add(overrideProgress.name, overrideProgress);
            }
            overrideProgress.setCallbackFunction = (SetBoolCallback)((e) => {
                if (e) slider.SetValue(progress.val); // override (set something different than null)
                else slider.SetValue(null); // back to slave
            });
            UIDynamicToggle toggle = this._script.CreateToggle(overrideProgress);
            this._uiElements.Add(toggle);

            if (!progressFound) {
                this._script.RegisterFloat(progress);
                this._params.Add(progress.name, progress);
            }
            UIDynamicSlider progressSlider = this._script.CreateSlider(progress, true);
            this._uiElements.Add(progressSlider);
            progressSlider.slider.onValueChanged.AddListener((e) => {
                if (!overrideProgress.val) return; // the override must be enabled
                slider.SetValue(e);
            });
            toggle.height = progressSlider.height;


            foreach (var subslider in slider.GetSubelements()) {
                this._UpdateSliders(subslider);
            }

            if (overrideProgress.val) slider.SetValue(progress.val); // update adjustable values
        }
    }
}
