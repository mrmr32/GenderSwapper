using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JustAnotherUser {
    class MorphHelper {
        /**
         * @ref https://github.com/ProjectCanyon/morph-merger/blob/master/MorphMerger.cs
         **/
        public static List<DAZMorph> ScanBank(DAZCharacterSelector characterSelector, bool ignoreGender = false) {
            List<DAZMorph> r = new List<DAZMorph>();
            if (ignoreGender || characterSelector.gender.Equals(DAZCharacterSelector.Gender.Male)) {
                r.AddRange(ScanBank(characterSelector.maleMorphBank1));
                r.AddRange(ScanBank(characterSelector.maleMorphBank2));
                r.AddRange(ScanBank(characterSelector.maleMorphBank3));
            }
            if (ignoreGender || characterSelector.gender.Equals(DAZCharacterSelector.Gender.Female)) {
                r.AddRange(ScanBank(characterSelector.femaleMorphBank1));
                r.AddRange(ScanBank(characterSelector.femaleMorphBank2));
                r.AddRange(ScanBank(characterSelector.femaleMorphBank3));
            }
            return r;
        }

        public static bool? IsMorphAMaleMorph(DAZCharacterSelector characterSelector, DAZMorph morph) {
            List<DAZMorph> maleMorphs = new List<DAZMorph>();
            maleMorphs.AddRange(ScanBank(characterSelector.maleMorphBank1));
            maleMorphs.AddRange(ScanBank(characterSelector.maleMorphBank2));
            maleMorphs.AddRange(ScanBank(characterSelector.maleMorphBank3));
            if (maleMorphs.Contains(morph)) return true;

            List<DAZMorph> femaleMorphs = new List<DAZMorph>();
            femaleMorphs.AddRange(ScanBank(characterSelector.femaleMorphBank1));
            femaleMorphs.AddRange(ScanBank(characterSelector.femaleMorphBank2));
            femaleMorphs.AddRange(ScanBank(characterSelector.femaleMorphBank3));
            if (femaleMorphs.Contains(morph)) return false;

            return null;
        }

        private static List<DAZMorph> ScanBank(DAZMorphBank bank) { // TODO only morph (not morph & pose)
            List<DAZMorph> r = new List<DAZMorph>();
            if (bank == null) return r;

            foreach (DAZMorph morph in bank.morphs) {
                if (!morph.visible) continue;

                r.Add(morph);
            }
            return r;
        }

        /**
         * Gets a morph given an id or name
         * @author https://github.com/mrmr32/PICOFacialTrackerVamLink
         **/
        public static DAZMorph FindMorphByID(GenerateDAZMorphsControlUI morphs, GenerateDAZMorphsControlUI otherGenderMorphs, string id, bool? onyCurrentGenderMorphs = null) {
            List<GenerateDAZMorphsControlUI> UIs = new List<GenerateDAZMorphsControlUI> {};
            if (onyCurrentGenderMorphs != false) UIs.Add(morphs); // if onlyCurrentGender, or both (null)
            if (onyCurrentGenderMorphs != true && otherGenderMorphs != null) UIs.Add(otherGenderMorphs); // if onlyOtherGender, or both (null), and the other is specified

            foreach (GenerateDAZMorphsControlUI control in UIs) {
                try {
                    return control.GetMorphByUid(id);
                }
                catch { }

                try {
                    return control.GetMorphByDisplayName(id);
                }
                catch { }
            }
            return null;
        }
        public static DAZMorph FindMorphByID(DAZCharacterSelector cs, string id, bool? onyCurrentGenderMorphs = null) {
            return MorphHelper.FindMorphByID(cs.morphsControlUI, cs.morphsControlUIOtherGender, id, onyCurrentGenderMorphs);
        }

        public static DAZMorph FindMorphByIDGivenGender(DAZCharacterSelector cs, string id, bool isMaleMorph) {
            bool isMale = cs.gender.Equals(DAZCharacterSelector.Gender.Male);
            bool onyCurrentGenderMorphs = (isMale == isMaleMorph);
            return MorphHelper.FindMorphByID(cs.morphsControlUI, cs.morphsControlUIOtherGender, id, onyCurrentGenderMorphs);
        }
        
        public static List<string> GetAllMorphNames(List<DAZMorph> morphs) {
            return morphs.Select(m => m.resolvedDisplayName).Distinct().ToList();
        }

        public static void SetMorphValue(DAZMorph morph, float value) {
            if (morph == null) return;
            morph.SetValue(value);
            morph.SyncJSON();
        }

        public static Dictionary<DAZMorph, float> GetDefaults(List<DAZMorph> morphs) {
            Dictionary<DAZMorph, float> r = new Dictionary<DAZMorph, float>();
            foreach (DAZMorph morph in morphs) {
                r[morph] = GetDefault(morph);
            }
            return r;
        }

        public static float GetDefault(DAZMorph morph) {
            return morph.jsonFloat.defaultVal;
        }

        public static string GetMorphName(string uid) {
            if (uid.Contains("/")) uid = uid.Substring(uid.LastIndexOf("/")+1);
            if (uid.EndsWith(".vmi")) uid = uid.Remove(uid.Length - ".vmi".Length);
            return uid;
        }

        public static float GetMorphValue(DAZMorph morph) {
            return morph.appliedValue;
        }
    }
}
