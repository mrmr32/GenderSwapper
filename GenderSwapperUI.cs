using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

        private JSONStorableFloat _transformationProgress;
        private UIDynamicSlider _slider;
        private UIDynamicButton _btn;

        public GenderSwapperUI(GenderSwapper script) {
            this._script = script;

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

        public void SetOnSliderChange(UnityAction<float> e) {
            this._slider.slider.onValueChanged.AddListener(e);
        }

        public void SetOnButtonClick(UnityAction e) {
            this._btn.button.onClick.AddListener(e);
        }

        public float GetSliderProgress() {
            return this._slider.slider.value;
        }

        public void SetupUI() {
            this._transformationProgress = new JSONStorableFloat("progress", 0f, 0f, 1f);
            this._script.RegisterFloat(this._transformationProgress);
            this._slider = this._script.CreateSlider(this._transformationProgress);

            this._btn = this._script.CreateButton("Spawn penis (only for female destiny)", true);
            this._btn.height = this._slider.height;

            this._soruce.CreateUI(false);
            this._destiny.CreateUI(true);
        }
    }
}
