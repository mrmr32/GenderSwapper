using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MVR.FileManagementSecure;

namespace JustAnotherUser {
    public class Texture {
        public static readonly int DIFFUSE_TEXTURE = 0,
                                    SPECULAR_TEXTURE = 1,
                                    GLOSS_TEXTURE = 2,
                                    NORMAL_TEXTURE = 3,
                                    DECAL_TEXTURE = 5;

        public string diffuse;
        public string specular;
        public string gloss;
        public string normal;
        public string decal;

        public Texture() {
            this.diffuse = "";
            this.specular = "";
            this.gloss = "";
            this.normal = "";
            this.decal = "";
        }

        public override string ToString() {
            return "diffuse: " + this.diffuse +
                "\nspecular: " + this.specular +
                "\ngloss: " + this.gloss +
                "\nnormal: " + this.normal +
                "\ndecal: " + this.decal;
        }

        public IEnumerable<KeyValuePair<string, int>> GetTextures(bool returnOnlyIfAny = true) {
            if (!returnOnlyIfAny || this.diffuse.Length > 0) yield return new KeyValuePair<string, int>(this.diffuse, DIFFUSE_TEXTURE);
            if (!returnOnlyIfAny || this.specular.Length > 0) yield return new KeyValuePair<string, int>(this.specular, SPECULAR_TEXTURE);
            if (!returnOnlyIfAny || this.gloss.Length > 0) yield return new KeyValuePair<string, int>(this.gloss, GLOSS_TEXTURE);
            if (!returnOnlyIfAny || this.normal.Length > 0) yield return new KeyValuePair<string, int>(this.normal, NORMAL_TEXTURE);
            if (!returnOnlyIfAny || this.decal.Length > 0) yield return new KeyValuePair<string, int>(this.decal, DECAL_TEXTURE);
        }

        
        public void SetTexture(int index, string value) {
            if (index == DIFFUSE_TEXTURE) this.diffuse = value;
            else if (index == SPECULAR_TEXTURE) this.specular = value;
            else if (index == GLOSS_TEXTURE) this.gloss = value;
            else if (index == NORMAL_TEXTURE) this.normal = value;
            else if (index == DECAL_TEXTURE) this.decal = value;
            else throw new IndexOutOfRangeException("Invalid index");
        }
    }

    public class AtomTexture {
        public static readonly int GENITALS_REGION = 3;

        public Texture head;
        public Texture torso;
        public Texture limbs;
        public Texture genitals;

        public bool isValid {
            get {
                if (head == null) return false; // all null

                return true; // TODO check if file exists
            }
        }

        public override string ToString() {
            if (!this.isValid) return "";

            return "- Head -\n" + this.head.ToString() +
                "\n\n- Torso -\n" + this.torso.ToString() +
                "\n\n- Limbs -\n" + this.limbs.ToString() +
                "\n\n- Genitals -\n" + this.genitals.ToString();
        }

        public IEnumerable<KeyValuePair<Texture,int>> GetTextures() {
            yield return new KeyValuePair<Texture, int>(this.head, UVData.FACE_REGION);
            yield return new KeyValuePair<Texture, int>(this.torso, UVData.TORSO_REGION);
            yield return new KeyValuePair<Texture, int>(this.limbs, UVData.LIMBS_REGION);
            yield return new KeyValuePair<Texture, int>(this.genitals, AtomTexture.GENITALS_REGION);
        }

        public Texture GetTexture(int index) {
            foreach (KeyValuePair<Texture, int> e in this.GetTextures()) {
                if (index == e.Value) return e.Key;
            }
            return null;
        }

        public void SetTexture(int index, Texture value) {
            if (index == UVData.FACE_REGION) this.head = value;
            else if (index == UVData.TORSO_REGION) this.torso = value;
            else if (index == UVData.LIMBS_REGION) this.limbs = value;
            else if (index == AtomTexture.GENITALS_REGION) this.genitals = value;
            else throw new IndexOutOfRangeException("Invalid index");
        }
    }

    public class MorphValue {
        public string id { get; private set; }
        public float value { get; private set; }
        public bool isMaleMorph { get; private set; }

        public MorphValue(string id, float value, bool isMaleMorph) {
            this.id = id;
            this.value = value;
            this.isMaleMorph = isMaleMorph;
        }
    }

    public class PresetData {
        public string name { get; private set; }

        /* preset data variables */

        public bool isMale { get; private set; }

        public AtomTexture maleTexture;

        public AtomTexture femaleTexture;

        /**
         * Base model name
         **/
        public string skin { get; private set; }

        // TODO you can set tiles to irises
        public string iris { get; private set; }

        public List<MorphValue> morphs { get; private set; }

        public Dictionary<string, bool> isMaleMorph {
            get {
                Dictionary<string, bool> r = new Dictionary<string, bool>();
                foreach (MorphValue morph in this.morphs) {
                    if (r.ContainsKey(morph.id)) SuperController.LogError("Warning: calling `isMaleMorph` while there's duplicates morphs (" + morph.id + ")");
                    r[morph.id] = morph.isMaleMorph;
                }
                return r;
            }
        }

        public bool isValid {
            get {
                return this.name.Length > 0;
            }
        }

        public AtomTexture texture {
            get {
                return this.isMale ? this.maleTexture : this.femaleTexture;
            }
        }

        public PresetData() {
            this.name = "";
            this.isMale = false;
            this.skin = "";
            this.iris = "";
            this.maleTexture = null; // no texture applied yet
            this.femaleTexture = null;
            this.morphs = new List<MorphValue>();
        }

        /**
         * Gets the JSON with the data of the current object
         **/
        public JSONClass GetJSON(string subScenePrefix) {
            JSONClass r = new JSONClass();
            if (!this.isValid) return r;

            r["name"] = this.name;
            r["isMale"].AsBool = this.isMale;
            if (this.maleTexture != null) r["maleTexture"] = PresetData.GetJSON(this.maleTexture);
            if (this.femaleTexture != null) r["femaleTexture"] = PresetData.GetJSON(this.femaleTexture);
            r["skin"] = this.skin;
            r["iris"] = this.iris;
            JSONArray morphs = new JSONArray();
            foreach (var morph in this.morphs) {
                JSONClass m = new JSONClass();
                m["id"] = morph.id;
                m["value"].AsFloat =  morph.value;
                m["isMaleMorph"].AsBool = morph.isMaleMorph;
                morphs.Add(m);
            }
            r["morphs"] = morphs;

            return r;
        }

        private static JSONClass GetJSON(AtomTexture atomTexture) {
            JSONClass r = new JSONClass();
            
            r["head"] = PresetData.GetJSON(atomTexture.head);
            r["torso"] = PresetData.GetJSON(atomTexture.torso);
            r["limbs"] = PresetData.GetJSON(atomTexture.limbs);
            r["genitals"] = PresetData.GetJSON(atomTexture.genitals);

            return r;
        }

        private static JSONClass GetJSON(Texture texture) {
            JSONClass r = new JSONClass();
            
            r["diffuse"] = texture.diffuse ?? "";
            r["specular"] = texture.specular ?? "";
            r["gloss"] = texture.gloss ?? "";
            r["normal"] = texture.normal ?? "";
            r["decal"] = texture.decal ?? "";

            return r;
        }

        public void RestoreFromJSON(JSONClass jc, string subScenePrefix, bool isMerge, bool setMissingToDefault) {
            this.name = jc["name"].Value;
            this.isMale = jc["isMale"].AsBool;
            // we'll theck if the opposite-gender textures are still valid; if not keep the null as value
            this.maleTexture = (!jc.HasKey("maleTexture") ? null : PresetData.RestoreAtomTextureFromJSON(jc["maleTexture"].AsObject, checkIfFilesExist: !this.isMale));
            this.femaleTexture = (!jc.HasKey("femaleTexture") ? null : PresetData.RestoreAtomTextureFromJSON(jc["femaleTexture"].AsObject, checkIfFilesExist: this.isMale));
            this.skin = jc["skin"].Value;
            this.iris = jc["iris"].Value;
            this.morphs = new List<MorphValue>();
            foreach (JSONClass entry in jc["morphs"].AsArray) this.morphs.Add(new MorphValue(entry["id"], entry["value"].AsFloat, entry["isMaleMorph"].AsBool));
        }

        public static AtomTexture RestoreAtomTextureFromJSON(JSONClass jc, bool checkIfFilesExist = false) {
            AtomTexture r = new AtomTexture();

            r.head = PresetData.RestoreTextureFromJSON(jc["head"].AsObject, checkIfFilesExist);
            r.torso = PresetData.RestoreTextureFromJSON(jc["torso"].AsObject, checkIfFilesExist);
            r.limbs = PresetData.RestoreTextureFromJSON(jc["limbs"].AsObject, checkIfFilesExist);
            r.genitals = PresetData.RestoreTextureFromJSON(jc["genitals"].AsObject, checkIfFilesExist);

            if (r.head == null || r.torso == null || r.limbs == null || r.genitals == null) {
                // file check failed
                SuperController.LogMessage("A texture was saved on GenderSwapper atom, but it wasn't found. Invalidating that texture...");
                return null;
            }
            return r;
        }

        public static Texture RestoreTextureFromJSON(JSONClass jc, bool checkIfFilesExist = false) {
            Texture r = new Texture();

            r.diffuse = jc["diffuse"].Value;
            r.specular = jc["specular"].Value;
            r.gloss = jc["gloss"].Value;
            r.normal = jc["normal"].Value;
            r.decal = jc["decal"].Value;

            if (checkIfFilesExist) {
                bool allOk = true;
                foreach (KeyValuePair<string,int> got in r.GetTextures()) {
                    if (got.Key.Length == 0) continue;

                    if (!FileManagerSecure.FileExists(got.Key)) allOk = false;
                }
                if (!allOk) return null; // send to `RestoreAtomTextureFromJSON` that the file check failed
            }

            return r;
        }

        /**
         * @param geometry: An already added Atom to be replaced
         * @param useOtherSexMorphs: Activate useXMorphsOnY
         * @return Modified atom /!\ WITHOUT MORPHS /!\
         **/
        public void LoadGeometryToAtom(DAZCharacterSelector selector, bool useOtherSexMorphs = false) {
            if (!this.isValid) return;

            selector.SelectCharacterByName(this.skin, true);
            
            // useFemaleMorphsOnMale & useMaleMorphsOnFemale applied at `LoadMorphs`
            /*geometry["useFemaleMorphsOnMale"].AsBool = false;
            geometry["useMaleMorphsOnFemale"].AsBool = false;
            if (useOtherSexMorphs) geometry[this.isMale ? "useFemaleMorphsOnMale" : "useMaleMorphsOnFemale"].AsBool = true;*/

            // TODO hair

            // TODO iris
            /*if (this.iris.Length > 0) {
                if (GetClassById(elements, "irises") == null) {
                    JSONClass irises = new JSONClass();
                    irises["id"] = "irises";

                    elements.Add(irises);
                }
                GetClassById(elements, "irises")["Irises"] = this.iris;
            }*/
        }
            
        public JSONClass LoadTexturesToAtom(JSONClass textures) {
            List<string> appliedTextures = textures.Keys.Where(k => !k.Equals("id")).ToList();
            foreach (string at in appliedTextures) textures.Remove(at); // clear the previous textures

            if (!this.isValid) return textures;

            if (this.texture.head.diffuse.Length > 0) textures["faceDiffuseUrl"] = this.texture.head.diffuse;
            if (this.texture.head.specular.Length > 0) textures["faceSpecularUrl"] = this.texture.head.specular;
            if (this.texture.head.gloss.Length > 0) textures["faceGlossUrl"] = this.texture.head.gloss;
            if (this.texture.head.normal.Length > 0) textures["faceNormalUrl"] = this.texture.head.normal;
            if (this.texture.head.decal.Length > 0) textures["faceDecalUrl"] = this.texture.head.decal;
            if (this.texture.torso.diffuse.Length > 0) textures["torsoDiffuseUrl"] = this.texture.torso.diffuse;
            if (this.texture.torso.specular.Length > 0) textures["torsoSpecularUrl"] = this.texture.torso.specular;
            if (this.texture.torso.gloss.Length > 0) textures["torsoGlossUrl"] = this.texture.torso.gloss;
            if (this.texture.torso.normal.Length > 0) textures["torsoNormalUrl"] = this.texture.torso.normal;
            if (this.texture.torso.decal.Length > 0) textures["torsoDecalUrl"] = this.texture.torso.decal;
            if (this.texture.limbs.diffuse.Length > 0) textures["limbsDiffuseUrl"] = this.texture.limbs.diffuse;
            if (this.texture.limbs.specular.Length > 0) textures["limbsSpecularUrl"] = this.texture.limbs.specular;
            if (this.texture.limbs.gloss.Length > 0) textures["limbsGlossUrl"] = this.texture.limbs.gloss;
            if (this.texture.limbs.normal.Length > 0) textures["limbsNormalUrl"] = this.texture.limbs.normal;
            if (this.texture.limbs.decal.Length > 0) textures["limbsDecalUrl"] = this.texture.limbs.decal;
            if (this.texture.genitals.diffuse.Length > 0) textures["genitalsDiffuseUrl"] = this.texture.genitals.diffuse;
            if (this.texture.genitals.specular.Length > 0) textures["genitalsSpecularUrl"] = this.texture.genitals.specular;
            if (this.texture.genitals.gloss.Length > 0) textures["genitalsGlossUrl"] = this.texture.genitals.gloss;
            if (this.texture.genitals.normal.Length > 0) textures["genitalsNormalUrl"] = this.texture.genitals.normal;
            if (this.texture.genitals.decal.Length > 0) textures["genitalsDecalUrl"] = this.texture.genitals.decal;
            return textures;
        }

        public void RestoreFromAppearancePreset(string name, JSONClass jc) {
            JSONArray elements = jc["storables"].AsArray;

            this.name = name;
            this.isMale = (GetClassById(elements, "MaleAnatomy") != null);
            if (this.isMale) {
                this.maleTexture = RestoreAtomTextureFromAppearancePreset(GetClassById(elements, "textures"));
                this.femaleTexture = null;
            }
            else {
                this.maleTexture = null;
                this.femaleTexture = RestoreAtomTextureFromAppearancePreset(GetClassById(elements, "textures"));
            }
            this.skin = GetClassById(elements, "geometry")["character"].Value;
            this.iris = GetClassById(elements, "irises")["Irises"].Value;
            this.morphs = new List<MorphValue>();
            foreach (JSONNode entry in GetClassById(elements, "geometry")["morphs"].AsArray.Childs) {
                this.morphs.Add(new MorphValue(entry["uid"].Value, entry["value"].AsFloat, this.isMale));
            }
            foreach (JSONNode entry in GetClassById(elements, "geometry")["morphsOtherGender"].AsArray.Childs) {
                this.morphs.Add(new MorphValue(entry["uid"].Value, entry["value"].AsFloat, !this.isMale));
            }
        }


        public static AtomTexture RestoreAtomTextureFromAppearancePreset(JSONClass jc) {
            AtomTexture r = new AtomTexture();

            r.head = new Texture();
            r.torso = new Texture();
            r.limbs = new Texture();
            r.genitals = new Texture();

            // TODO if no value grab the skin default
            if (jc == null) return r; // all textures to default?

            r.head.diffuse = jc["faceDiffuseUrl"].Value;
            r.head.specular = jc["faceSpecularUrl"].Value;
            r.head.gloss = jc["faceGlossUrl"].Value;
            r.head.normal = jc["faceNormalUrl"].Value;
            r.head.decal = jc["faceDecalUrl"].Value;

            r.torso.diffuse = jc["torsoDiffuseUrl"].Value;
            r.torso.specular = jc["torsoSpecularUrl"].Value;
            r.torso.gloss = jc["torsoGlossUrl"].Value;
            r.torso.normal = jc["torsoNormalUrl"].Value;
            r.torso.decal = jc["torsoDecalUrl"].Value;

            r.limbs.diffuse = jc["limbsDiffuseUrl"].Value;
            r.limbs.specular = jc["limbsSpecularUrl"].Value;
            r.limbs.gloss = jc["limbsGlossUrl"].Value;
            r.limbs.normal = jc["limbsNormalUrl"].Value;
            r.limbs.decal = jc["limbsDecalUrl"].Value;

            r.genitals.diffuse = jc["genitalsDiffuseUrl"].Value;
            r.genitals.specular = jc["genitalsSpecularUrl"].Value;
            r.genitals.gloss = jc["genitalsGlossUrl"].Value;
            r.genitals.normal = jc["genitalsNormalUrl"].Value;
            r.genitals.decal = jc["genitalsDecalUrl"].Value;

            return r;
        }

        private static JSONClass GetClassById(JSONArray jc, string id) {
            foreach (JSONNode node in jc.Childs) {
                if (node["id"].Value.Equals(id)) return node.AsObject;
            }

            return null;
        }
    }
}
