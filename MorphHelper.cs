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
            if (characterSelector.gender.Equals(DAZCharacterSelector.Gender.Male) || ignoreGender) {
                r.AddRange(ScanBank(characterSelector.maleMorphBank1));
                r.AddRange(ScanBank(characterSelector.maleMorphBank2));
                r.AddRange(ScanBank(characterSelector.maleMorphBank3));
            }
            if (characterSelector.gender.Equals(DAZCharacterSelector.Gender.Female) || ignoreGender) {
                r.AddRange(ScanBank(characterSelector.femaleMorphBank1));
                r.AddRange(ScanBank(characterSelector.femaleMorphBank2));
                r.AddRange(ScanBank(characterSelector.femaleMorphBank3));
            }
            return r;
        }

        public static List<DAZMorph> ScanBank(DAZMorphBank bank) { // TODO only morph (not morph & pose)
            List<DAZMorph> r = new List<DAZMorph>();
            if (bank == null) return r;

            foreach (DAZMorph morph in bank.morphs) {
                if (!morph.visible) continue;

                r.Add(morph);
            }
            return r;
        }

        public static DAZMorph FindMorphByID(List<DAZMorph> morphs, string id) {
            return morphs.FirstOrDefault(m => m.uid.Equals(id));
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
