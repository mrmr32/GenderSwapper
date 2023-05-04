using System;
using System.Linq;
using MVR.FileManagementSecure; // WriteAllBytes
using UnityEngine;

namespace JustAnotherUser {
    public interface SkinToDefaultTextures {
        AtomTexture getTextures(string skinName);
    }

    class SkinToDefaultTexturesBase {
        /**
         * From a set of characters, get their base textures
         * @ref DecalMaker for getting the textures from the GPU
         **/
        public static AtomTexture getTextures(DAZCharacter[] characters, string skinName) {
            /*DAZCharacter selected = Enumerable.FirstOrDefault(characters, character => character.displayName == skinName);
            if (selected == null) return null; // character not found
            
            const string MainTex = "_MainTex";
            const string DecalTex = "_DecalTex";
            const string BumpMap = "_BumpMap";
            const string SpecTex = "_SpecTex";
            const string GlossTex = "_GlossTex";

            DAZSkinV2 skin = selected.skin;

            try
            {
                byte[] image = ImageConversion.EncodeToPNG(skin.GPUmaterials[11].GetTexture(MainTex) as Texture2D);
                string filePath = CUAManagerPath.Combine(saveFolder, "test.png");
                FileManagerSecure.WriteAllBytes(filePath, image);
            }
            catch (Exception ex)
            {
                SuperController.LogError(ex.ToString());
            }

            // DAZCharacterTextureControl#StartSyncImage



            AtomTexture textures = new AtomTexture();
            textures.head = new Texture();
            textures.torso = new Texture();
            textures.limbs = new Texture();
            
            for (int i = 0; i < skin.GPUmaterials.Length; i++) {
                try {
                SuperController.LogMessage(i.ToString() + ": " + skin.materialNames[i]);
                if (skin.GPUmaterials[i].HasProperty(MainTex))
                    SuperController.LogMessage((skin.GPUmaterials[i].GetTexture(MainTex) as Texture2D).name + " (main)");
                if (skin.GPUmaterials[i].HasProperty(DecalTex))
                    SuperController.LogMessage((skin.GPUmaterials[i].GetTexture(DecalTex) as Texture2D).name + " (decal)");
                if (skin.GPUmaterials[i].HasProperty(SpecTex))
                    SuperController.LogMessage((skin.GPUmaterials[i].GetTexture(SpecTex) as Texture2D).name + " (specular)");
                if (skin.GPUmaterials[i].HasProperty(GlossTex))
                    SuperController.LogMessage((skin.GPUmaterials[i].GetTexture(GlossTex) as Texture2D).name + " (gloss)");
                if (skin.GPUmaterials[i].HasProperty(BumpMap))
                    SuperController.LogMessage((skin.GPUmaterials[i].GetTexture(BumpMap) as Texture2D).name + " (bump)");
                } catch (Exception ex) {}
            }*/

            return /*textures*/null;
        }
    }
}
