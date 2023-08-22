using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SimpleJSON; // JSONNode
using MVR.FileManagementSecure; // WriteAllBytes
using System.Text.RegularExpressions;

namespace JustAnotherUser {
    public class GenderSwapper : MVRScript, SkinToDefaultTextures {
        private static readonly string VERSION = "0.4";
        private static readonly int UUID_LENGTH = 8;

        public static readonly int DIFFUSE_TEXTURE = 0,
                                    SPECULAR_TEXTURE = 1,
                                    GLOSS_TEXTURE = 2,
                                    NORMAL_TEXTURE = 3,
                                    DECAL_TEXTURE = 5;

        //private DecalMakerHelper _helper;
        private CUAManagerPath _path;

        private DAZCharacterSelector _dazCharacterSelector;
        private JSONStorable _decalmakerStorable;
        private List<string> _decalmakerProgressStorables;
        
        private List<DAZMorph> _morphs;
        private Dictionary<string, float> _defaultMorphs;

        public List<DAZMorph> morphs {
            get { return this._morphs; }
            set {
                this._morphs = value;
                
                var defaultMorphs = MorphHelper.GetDefaults(this._morphs);
                this._defaultMorphs = new Dictionary<string, float>();
                foreach (var e in defaultMorphs) {
                    this._defaultMorphs[e.Key.uid] = e.Value;
                }
            }
        }

        private PresetData _origin;
        private PresetData _destiny;

        private GenderSwapperUI _ui;

        private object _decalIsLoadingSync = new object();
        private bool _decalIsLoading = false;

        private string _baseName;

        public override void Init() {
            // plugin VaM GUI description
            pluginLabelJSON.val = "GenderSwapper v" + VERSION;

            this._path = new CUAManagerPath(this, "Saves\\PluginData\\mrmr32\\GenderSwapper");
            this._path.VerifyPluginDataFolder();
            this._path.Init();

            this._dazCharacterSelector = containingAtom.GetComponentInChildren<DAZCharacterSelector>();
            if (this._dazCharacterSelector == null) {
                SuperController.LogError("You need to aply GenderSwapper on a Person atom");
                return;
            }
            this._decalmakerProgressStorables = new List<string>();
            this.morphs = MorphHelper.ScanBank(this._dazCharacterSelector, ignoreGender: true);

            this._origin = new PresetData();
            this._destiny = new PresetData();

            this._ui = new GenderSwapperUI(this);
            this._ui.SetupUI();
            this._ui.SetOnSliderChange(SetProgress);
            this._ui.SetLoadPresetButtonEvent((val) => {
                try {
                    this._origin.RestoreFromAppearancePreset(GetFileName(val,"vap"), SuperController.singleton.LoadJSON(val).AsObject);
                    this.LoadData();
                } catch (Exception ex) {
                    SuperController.LogError(ex.ToString());
                }
            }, false);
            this._ui.SetLoadPresetButtonEvent((val) => {
                try {
                    this._destiny.RestoreFromAppearancePreset(GetFileName(val, "vap"), SuperController.singleton.LoadJSON(val).AsObject);
                    this.LoadData();
                } catch (Exception ex) {
                    SuperController.LogError(ex.ToString());
                }
            }, true);

            // TODO add scene name instead of random
            Regex rgx = new Regex("[^a-zA-Z0-9]");
            this._baseName = GetRandomName(UUID_LENGTH) + "_" + rgx.Replace(containingAtom.name, "");
        }

        protected void Start() {
            string decalMakerId = this.GetDecalMaker();
            if (decalMakerId == null) {
                SuperController.LogError("GenderSwapper needs DecalMaker in order to work. Please, first add DecalMaker.");
                return;
            }
            this._decalmakerStorable = containingAtom.GetStorableByID(decalMakerId);

            UVData.Load(this);

            StartCoroutine(LoadDataOnceAllFinishes());

            // TODO temporal test
            //this.getTextures(this._dazCharacterSelector.selectedCharacter.displayName);
        }

        private IEnumerator LoadDataOnceAllFinishes() {
            // wait the UVData to finish loading
            yield return new WaitUntil(() => UVData.IsLoaded());

            this.LoadData(retryIfFail: true); // DecalMaker may fail at the beginning
        }

        private string GetDecalMaker() {
            Regex decalName = new Regex(@"plugin#\d+_VAM_Decal_Maker.Decal_Maker");
            foreach (string id in containingAtom.GetStorableIDs()) {
                if (decalName.Match(id).Success) return id;
            }

            return null; // none
        }

        public void SetProgress(float val) {
            if (!this._origin.isValid || !this._destiny.isValid) return; // we need both
            
            Dictionary<string,float> result = LeapMorphs(this.GetMorphs(false), this.GetMorphs(true), this._defaultMorphs, val);
            foreach (var entry in result) {
                MorphHelper.SetMorphValue(MorphHelper.FindMorphByID(this.morphs, entry.Key), entry.Value);
            }

            if (this._decalmakerStorable == null) return;
            lock (this._decalIsLoadingSync) {
                foreach (string storable in this._decalmakerProgressStorables) {
                    this._decalmakerStorable.SetFloatParamValue(storable, 1.0f - val); // as we're assigning the source here, 0% progress is 100% source, and 100% progress 0% source
                }
            }
        }

        /**
         * Loads the `origin` texture into the first and second slots of DecalMaker.
         * The prefix will be 'swpDXXX' for decals, 'swpRXXX' for others.
         * @param decalMaker:   JSONClass that contains decalMaker. This information will be changed
         * @param origin:       Textures to load. This must be of the same sex as the atom.
         **/
        public static void LoadOriginTexturesIntoDecalMaker(JSONClass decalMaker, AtomTexture origin, float progress = 0f) {
            if (!origin.isValid) return;
            int decalVersion = decalMaker["SaveVersion"].AsInt;
            LoadOriginTexturesIntoDecalMaker(decalMaker, decalVersion, "face", origin.head, progress);
            LoadOriginTexturesIntoDecalMaker(decalMaker, decalVersion, "torso", origin.torso, progress);
            LoadOriginTexturesIntoDecalMaker(decalMaker, decalVersion, "limbs", origin.limbs, progress);
            LoadOriginTexturesIntoDecalMaker(decalMaker, decalVersion, "genitals", origin.genitals, progress);
        }

        public static void LoadOriginTexturesIntoDecalMaker(JSONClass decalMaker, int decalVersion, string part, Texture originTextureOfPart, float progress) {
            if (originTextureOfPart.diffuse.Length > 0) LoadOriginTexturesIntoDecalMaker(decalMaker, "_DecalTex", part, originTextureOfPart.diffuse, progress);
            if (originTextureOfPart.specular.Length > 0) LoadOriginTexturesIntoDecalMaker(decalMaker, "_SpecTex", part, originTextureOfPart.specular, progress);
            if (originTextureOfPart.gloss.Length > 0) LoadOriginTexturesIntoDecalMaker(decalMaker, "_GlossTex", part, originTextureOfPart.gloss, progress);
            if (originTextureOfPart.normal.Length > 0) LoadOriginTexturesIntoDecalMaker(decalMaker, "_BumpMap", part, originTextureOfPart.normal, progress);
            if (originTextureOfPart.decal.Length > 0) LoadOriginTexturesIntoDecalMaker(decalMaker, "_DecalTex", part, originTextureOfPart.decal, progress, isDecal:true);
            // TODO add decal as another row
        }

        public static void LoadOriginTexturesIntoDecalMaker(JSONClass decalMaker, string cls, string part, string texturePath, float progress, bool isDecal = false) {
            JSONArray elementsInPart = decalMaker[cls][part].AsArray;

            JSONClass texture = GetClassWithinArray(elementsInPart, isDecal ? "swpD" : "swpR");
            if (texture == null) {
                texture = new JSONClass();
                AddElementToJSONArray(elementsInPart, texture, isDecal ? -1 : 0); // add it on top if not decal
                elementsInPart[0] = texture;
                texture["H"].AsFloat = 0f;
                texture["S"].AsFloat = 0f;
                texture["V"].AsFloat = 1f;
                texture["RandomName"] = "swp" + (isDecal ? "D" : "R") + GetRandomName(3);
                texture["LinkID"] = "*";
            }

            texture["Alpha"].AsFloat = 1.0f - progress; // as we're assigning the source here, 0% progress is 100% source, and 100% progress 0% source
            texture["Path"] = texturePath;
        }
        
        private static JSONClass GetClassWithinArray(JSONArray elements, string startsWith) {
            foreach (JSONClass e in elements) {
                if (e["RandomName"].Value.StartsWith(startsWith)) return e;
            }
            return null;
        }

        private static void AddElementToJSONArray(JSONArray array, JSONNode element, int position = -1) {
            if (position == -1 || position == array.Count) {
                // add it on the last position
                array.Add(element);
                return;
            }
            
            // we need to shift elements
            array.Add(array[array.Count-1]);
            for (int index = array.Count - 1; index > position; index--) array[index] = array[index - 1];
            array[position] = element;
        }

        private void GetDecalMakerStorablesIds(JSONClass decalMaker) {
            List<string> storables = new List<string>();
            // TODO access Decal  to change its value; load all the names to modify on the `LoadDecalMaker`

            string prefix = "Alpha";
            // @ref got from BodyRegionEnum and MatSlotEnum
            string materialSlot = "_DecalTex"; // only decal has alpha value
            foreach (string textureSlot in new String[] { "torso", "face", "limbs", "genitals" }) {
                JSONArray textures = decalMaker[materialSlot][textureSlot].AsArray;
                foreach (JSONClass sliderClass in new JSONClass[]
                             { GetClassWithinArray(textures, "swpD"), GetClassWithinArray(textures, "swpR") }) {
                    if (sliderClass == null) continue;
                    string randomName = sliderClass["RandomName"];
                    string sliderId = string.Format("{0}_{1}{2}{3}", prefix, textureSlot, materialSlot, randomName); // @ref from CreateJSN
                    storables.Add(sliderId);
                }
            }

            this._decalmakerProgressStorables = storables;
        }

        private static string GetFileName(string path, string prefix) {
            var pattern = @"(Preset_)?([^\\/]+)\." + prefix;
            var match = Regex.Match(path, pattern);
            return match.Groups[2].Value;
        }
        
		public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false) {
            JSONClass jc = base.GetJSON(includePhysical, includeAppearance, forceStore);
            if (includePhysical || forceStore) {
                jc["origin"] = this._origin.GetJSON(this.subScenePrefix);
                jc["destiny"] = this._destiny.GetJSON(this.subScenePrefix);
            }
            return jc;
        }

        public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical, bool restoreAppearance, bool setMissingToDefault) {
            base.LateRestoreFromJSON(jc, restorePhysical, restoreAppearance, setMissingToDefault);

            if (!this.physicalLocked && restorePhysical) {
                this._origin = new PresetData();
                this._destiny = new PresetData();

                this._origin.RestoreFromJSON(jc["origin"].AsObject, this.subScenePrefix, this.mergeRestore, setMissingToDefault);
                this._destiny.RestoreFromJSON(jc["destiny"].AsObject, this.subScenePrefix, this.mergeRestore, setMissingToDefault);

                this.LoadData(); // now that we have the origin/target, update the UI
            }
        }

        /**
         * From origin's morphs to target's. If no morph is present at its counterpart, then a value of 0 is assumed.
         * TODO shouldn't we keep two different dictionaries for male-female morphs?
         **/
        public static Dictionary<string, float> LeapMorphs(Dictionary<string, float> origin, Dictionary<string, float> target, Dictionary<string, float> defaults, float targetPercentage) {
            Dictionary<string, float> r = new Dictionary<string, float>();

            if (origin == null) origin = new Dictionary<string, float>();
            if (target == null) target = new Dictionary<string, float>();
            
            foreach (string morph in origin.Keys.Union(target.Keys)) {
                float from = (origin.ContainsKey(morph) ? origin[morph] : (/*defaults.ContainsKey(morph) ? defaults[morph] :*/ 0.0f)),
                    to = (target.ContainsKey(morph) ? target[morph] : (/*defaults.ContainsKey(morph) ? defaults[morph] :*/ 0.0f));

                // 1 to 0 at 0% is 1, at 50% 0.5, at 100% 0
                // 0 to 1 at 0% is 0, at 50% 0.5, at 100% 1
                // 1 to 1 is always 1
                float diff = to - from;
                float current = from + diff*targetPercentage;

                r[morph] = current;
            }

            return r;
        }

        public void SetCharacterJSON(JSONClass geometry) {
            this._dazCharacterSelector.RestoreFromJSON(geometry);
            this.morphs = MorphHelper.ScanBank(this._dazCharacterSelector, ignoreGender: true);
        }

        private void LoadData(bool retryIfFail = false) {
            if (this._destiny.isValid) {
                // load destiny data (eyes, sex...)
                bool useOtherSexMorphs = (this._origin.isValid && this._destiny.isValid) && (this._origin.isMale != this._destiny.isMale);

                JSONClass geometry = this._dazCharacterSelector.GetJSON();
                this._destiny.LoadGeometryToAtom(geometry, useOtherSexMorphs);
                this.SetCharacterJSON(geometry);

                // morphs will be set later, on `SetProgress`
                
                DAZCharacterTextureControl t = this.containingAtom.GetStorableByID("textures") as DAZCharacterTextureControl;
                if (t == null) SuperController.LogError("NPE on LoadData: DAZCharacterTextureControl can't be null");
                else {
                    JSONClass textures = t.GetJSON();
                    this._destiny.LoadTexturesToAtom(textures);
                    t.RestoreFromJSON(textures);
                }

                if (this._decalmakerStorable != null) this._decalmakerStorable.CallAction("Reset To Original Textures");
            }
            
            this.LoadUIData();

            // update morphs %
            this.LoadMorphs(this._ui.GetSliderProgress());

            // load the origin decal texture
            StartCoroutine(this.LoadDecalMaker(this._ui.GetSliderProgress(), retryIfFail));
        }

        /**
         * Loads the morphs into the JSON because for some reason `morphs` don't contains all of them
         */
        private void LoadMorphs(float progress) {
            if (!this._origin.isValid || !this._destiny.isValid) return; // we need both
            
            Dictionary<string,float> result = LeapMorphs(this.GetMorphs(false), this.GetMorphs(true), this._defaultMorphs, progress);
            JSONClass geometry = this._dazCharacterSelector.GetJSON();
            JSONArray morphsJSON = new JSONArray();
            JSONArray morphsOnOtherGenderJSON = new JSONArray();

            Dictionary<string, bool> morphIDToIsMorphYourGender = new Dictionary<string, bool>();
            morphIDToIsMorphYourGender["MVR_G2Female"] = this._destiny.isMale; // MVR_G2Female is a male morph
            foreach (var e in this._origin.isMaleMorph) morphIDToIsMorphYourGender[e.Key] = (e.Value == this._destiny.isMale);
            foreach (var e in this._destiny.isMaleMorph) morphIDToIsMorphYourGender[e.Key] = (e.Value == this._destiny.isMale);
            
            foreach (var entry in result) {
                JSONClass entryJSON = new JSONClass();

                entryJSON["uid"] = entry.Key;
                entryJSON["name"] = MorphHelper.GetMorphName(entry.Key);
                entryJSON["value"].AsFloat = entry.Value;
                
                if (!morphIDToIsMorphYourGender.ContainsKey(entry.Key)) SuperController.LogError("Morph " + entry.Key + " not found in male's batch nor female's");
                JSONArray target = (morphIDToIsMorphYourGender[entry.Key] ? morphsJSON : morphsOnOtherGenderJSON);
                target.Add(entryJSON);
            }
            geometry["morphs"] = morphsJSON;
            geometry["morphsOtherGender"] = morphsOnOtherGenderJSON;
            
            geometry["useFemaleMorphsOnMale"].AsBool = false;
            geometry["useMaleMorphsOnFemale"].AsBool = false;
            if (morphsOnOtherGenderJSON.Count > 0) geometry[this._destiny.isMale ? "useFemaleMorphsOnMale" : "useMaleMorphsOnFemale"].AsBool = true;
            this.SetCharacterJSON(geometry);

            // sometimes (besides explicitally telling the morphs in the JSON) some extra morphs are added; remove those
            foreach (DAZMorph morph in this.morphs) {
                if (MorphHelper.GetMorphValue(morph) != 0 && !result.ContainsKey(morph.uid)) MorphHelper.SetMorphValue(morph, 0);
            }
        }
        
        private IEnumerator LoadDecalMaker(float progress = 0f, bool retryIfFail = false) {
            bool isYours = false;
            lock (this._decalIsLoadingSync) {
                isYours = !this._decalIsLoading;
                if (isYours) this._decalIsLoading = true;
            }
            if (!isYours) yield break; // already loading

            Exception exception = null; // TODO try/catch, saving the exception (if any) here
            if (this._origin.isValid && this._destiny.isValid && this._decalmakerStorable != null) { // we need destiny data to create the origin decal
                JSONClass cls = new JSONClass();
                try {
                    cls = this._decalmakerStorable.GetJSON();
                } catch (Exception ex) {
                    exception = ex;
                }

                if (exception == null) { // continue?
                    AtomTexture texture = (this._destiny.isMale ? this._origin.maleTexture : this._origin.femaleTexture);
                    if (texture == null) {
                        // the texture don't exist in that gender; generate it
                        StartCoroutine(LoadTexture(this._origin, this._decalIsLoadingSync, this._destiny.isMale, this._path.PluginDataFolder, this._baseName));

                        // wait `LoadTexture` to finish
                        yield return new WaitUntil(() => {
                            lock (this._decalIsLoadingSync) {
                                return (this._destiny.isMale ? this._origin.maleTexture : this._origin.femaleTexture) != null;
                            }
                        });

                        texture = (this._destiny.isMale ? this._origin.maleTexture : this._origin.femaleTexture); // get the loaded texture
                    }

                    try {
                        cls["Nipple Cutouts ON"].AsBool = false;
                        
                        // @pre the texture must be the same sex as the atom
                        LoadOriginTexturesIntoDecalMaker(cls, texture, progress);

                        this._decalmakerStorable.CallAction("ClearAll"); //removes any existing loaded decals
                        this._decalmakerStorable.RestoreFromJSON(cls);
                        this._decalmakerStorable.CallAction("PerformLoad"); //load the data from the _savedData variable

                        GetDecalMakerStorablesIds(cls);
                    } catch (Exception ex) {
                        exception = ex;
                    }
                }
            }

            lock (this._decalIsLoadingSync) {
                this._decalIsLoading = false;
            }

            if (exception != null) {
                // failed
                SuperController.LogError(exception.ToString());
                if (retryIfFail) {
                    // try again
                    yield return new WaitForSeconds(0.5f);
                    StartCoroutine(this.LoadDecalMaker(progress, retryIfFail));
                }
            }
        }

        private void LoadUIData() {
            if (this._origin.isValid) {
                this._ui.SetName(this._origin.name + (this._origin.isMale ? " (male)" : " (female)"), false);
                // TODO mark body&iris as invalid
                this._ui.SetBody(this._origin.skin, false);
                this._ui.SetIris(this._origin.iris, false);
                this._ui.SetMorphList(this.MorphListToString(false), false);
                this._ui.SetTextureList(this._origin.texture.ToString(), false); // TODO show also converted texture path
            }

            if (this._destiny.isValid) {
                this._ui.SetName(this._destiny.name + (this._destiny.isMale ? " (male)" : " (female)"), true);
                this._ui.SetBody(this._destiny.skin, true);
                this._ui.SetIris(this._destiny.iris, true);
                this._ui.SetMorphList(this.MorphListToString(true), true);
                this._ui.SetTextureList(this._destiny.texture.ToString(), true);
            }
        }

        private string MorphListToString(bool target) {
            Dictionary<string, float> list = this.GetMorphs(target);

            string r = "";
            float? femaleConversionValue = null;
            foreach (var e in list) {
                if (e.Key.Equals("MVR_G2Female")) femaleConversionValue = e.Value; // we'll place it at the bottom
                else {
                    string name = MorphHelper.FindMorphByID(this.morphs, e.Key)?.resolvedDisplayName;
                    if (name == null) name = e.Key;
                    r += name + ": " + e.Value.ToString("0.00") + "\n";
                }
            }

            if (femaleConversionValue != null) {
                r += "MVR_G2Female: " + femaleConversionValue.Value.ToString("0.00") + "*\n\n"
                    + "*As you’re changing from " + (this._origin.isMale ? "male" : "female") + " to " + (this._destiny.isMale ? "male" : "female") + " this morph needs to be added";
            }

            return r;
        }

        private Dictionary<string, float> GetMorphs(bool target) {
            if (target) return this._destiny?.morphs;

            // the source may need to add the `MVR_G2Female` morph
            if (this._origin == null) return null;
            if (this._destiny.isMale == this._origin.isMale) return this._origin.morphs; // same sex; we don't need to convert anything

            Dictionary<string, float> r = new Dictionary<string, float>(this._origin.morphs);
            r["MVR_G2Female"] = (this._destiny.isMale ? 1f : -1f); // if it's a female converting into a male we need to set it on 1, if it's a male into a female set it to -1
            return r;
        }

        public static string GetRandomName(int lenght) {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string r = "";
            for (int i = 0; i < lenght; i++) r += chars[(int)UnityEngine.Random.Range(0, chars.Length)];
            return r;
        }

        private static IEnumerator LoadTexture(PresetData textures, object changingTexturesLock, bool intoMale, string folder, string baseOutFileName) {
            AtomTexture textureToTransform = textures.texture;
            AtomTexture transformedTexture = new AtomTexture();
            transformedTexture.head = new Texture();
            transformedTexture.torso = new Texture();
            transformedTexture.limbs = new Texture();
            transformedTexture.genitals = new Texture();

            // we'll have to convert as many textures as `AtomTexture` holds
            foreach (KeyValuePair<Texture, int> texture in textureToTransform.GetTextures()) {
                int region = texture.Value;
                if (region == AtomTexture.GENITALS_REGION) {
                    // genitals only need the torso
                    // @pre yielded & converted torso
                    string femaleTorso = (intoMale) ? textureToTransform.torso.diffuse : transformedTexture.torso.diffuse;
                    if (femaleTorso.Length == 0) {
                        SuperController.LogMessage("Diffuse torso not set; couldn't generate genitals.");
                        continue;
                    }

                    Texture2D originalTexture = new Texture2D(1, 1); // it will be changed later by `ImageConversion.LoadImage`

                    try {
                        // get the torso
                        byte[] originalData = FileManagerSecure.ReadAllBytes(femaleTorso);
                        ImageConversion.LoadImage(originalTexture, originalData);

                        // generate the genitals
                        IEnumerable<GenitalsHelper.GenitalsTexture> genitals = (intoMale) ? GenitalsHelper.LoadMaleGenitals(originalTexture) : GenitalsHelper.LoadFemaleGenitals(originalTexture);

                        foreach (GenitalsHelper.GenitalsTexture text in genitals) {
                            int typeNum;
                            if (text.typeOfTexture == "diffuse") typeNum = DIFFUSE_TEXTURE;
                            else if (text.typeOfTexture == "specular") typeNum = SPECULAR_TEXTURE;
                            else if (text.typeOfTexture == "normal") typeNum = NORMAL_TEXTURE;
                            else typeNum = GLOSS_TEXTURE;

                            // get the output path
                            // @ref CUAManager
                            string filePath = CUAManagerPath.Combine(folder, baseOutFileName + "-" + region.ToString() + "_" + typeNum.ToString() + ".png");
                
                            // convert the output image back into bytes
                            byte[] image = ImageConversion.EncodeToPNG(text.texture);
                
                            // write the output image
                            FileManagerSecure.WriteAllBytes(filePath, image);
                
                            if (FileManagerSecure.FileExists(filePath)) {
                                SuperController.LogMessage("Generated " + text.typeOfTexture + " genital texture '" + filePath + "'.");
                                transformedTexture.GetTexture(region).SetTexture(typeNum, filePath);
                            }
                            else {
                                SuperController.LogError("Error while generating " + text.typeOfTexture + " genital texture.");
                            }
                        }
                    } catch (Exception ex) {
                        SuperController.LogError(ex.ToString());
                    }
                }
                else {
                    // head/torso/limbs
                    foreach (KeyValuePair<string, int> tex in texture.Key.GetTextures()) {
                        string texturePath = tex.Key;
                        int typeOfTexture = tex.Value;


                        Texture2D originalTexture = new Texture2D(1, 1); // it will be changed later by `ImageConversion.LoadImage`

                        try {
                            SuperController.LogMessage("Converting image '" + texturePath + "'...");

                            // get the new image
                            byte[] originalData = FileManagerSecure.ReadAllBytes(texturePath);
                            ImageConversion.LoadImage(originalTexture, originalData);
                        } catch (Exception ex) {
                            SuperController.LogError(ex.ToString());
                        }

                        // wait the UVData to finish loading
                        yield return new WaitUntil(() => UVData.IsLoaded());

                        try {
                            // distort the image
                            Texture2D targetTexture = UVData.DeformUVs(originalTexture, !intoMale, region);

                            // get the output path
                            // @ref CUAManager
                            string filePath = CUAManagerPath.Combine(folder, baseOutFileName + "-" + region.ToString() + "_" + typeOfTexture.ToString() + ".png");
                
                            // convert the output image back into bytes
                            byte[] image = ImageConversion.EncodeToPNG(targetTexture);
                
                            // write the output image
                            FileManagerSecure.WriteAllBytes(filePath, image);
                
                            if (FileManagerSecure.FileExists(filePath)) {
                                SuperController.LogMessage("Image '" + texturePath + "' converted as '" + filePath + "'.");
                                transformedTexture.GetTexture(region).SetTexture(typeOfTexture, filePath);
                            }
                            else {
                                SuperController.LogError("Error while converting image '" + texturePath + "'");
                            }
                        } catch (Exception ex) {
                            SuperController.LogError(ex.ToString());
                        }
                    }
                }
            }

            
            
            lock (changingTexturesLock) {
                if (intoMale) textures.maleTexture = transformedTexture;
                else textures.femaleTexture = transformedTexture;
            }
        }

        public AtomTexture getTextures(string skinName) {
            return SkinToDefaultTexturesBase.getTextures(this._dazCharacterSelector.characters, skinName);
        }
    }
}