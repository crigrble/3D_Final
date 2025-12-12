using System;
using System.Collections;
using UnityEngine;


namespace NekoLegends
{
    public class DemoCharacterTypeB : MonoBehaviour
    {
        [SerializeField] protected AnimationClip[] animations;

        [SerializeField] protected SkinnedMeshRenderer skinnedMeshRendererBody, skinnedMeshRendererHair, skinnedMeshRendererHeadFace, skinnedOutfitRenderer;

        [SerializeField] protected Material[] bodyMaterialList, hairMaterialList, outfitMaterialList, bottomsMaterialList, shoesMaterialList;

        [SerializeField] protected GameObject LeftHandItem, RightHandItem;

        public Animator animator { get; protected set; }

        [SerializeField] protected Boolean autoblink;
        [SerializeField] protected Vector2 blinkStartEndTimeRange = new Vector2(3, 6);
        [SerializeField] protected float blinkDuration = 0.15f;
        [SerializeField] protected float blinkClosedDuration = 0.5f;

        [SerializeField] protected int mainClothingMaterialIndex , bottomsClothingMaterialIndex , shoesMaterialIndex;
        protected int _currentEmotionIndex, _hairColorIndex, _outfitStyleIndex, _bottomsStyleIndex, _shoesStyleIndex, _currentAnimIndex = -1;

        protected int _emotionIndexMax = 6;
        protected float _transitionDuration = 0.25f; 


        protected virtual void Start()
        {
            animator = GetComponent<Animator>();
            ResetBlendShapes();
         
            if (autoblink)
            {
                StartCoroutine(AutoBlink());
            }
        }

        private IEnumerator AutoBlink()
        {
            while (autoblink)
            {
                float timeUntilNextBlink = UnityEngine.Random.Range(blinkStartEndTimeRange[0], blinkStartEndTimeRange[1]);

                yield return new WaitForSeconds(timeUntilNextBlink);
                SetBlendShapeWeightFace(6, 100f, blinkDuration);
                SetBlendShapeWeightFace(13, 100f, blinkDuration);

                // Wait briefly while eyes are closed
                yield return new WaitForSeconds(blinkClosedDuration);

                SetBlendShapeWeightFace(6, 0f, blinkDuration);
                SetBlendShapeWeightFace(13, 0f, blinkDuration);
            }
        }

        public void NextEmotion()
        {
            _currentEmotionIndex++;
            if (_currentEmotionIndex > _emotionIndexMax)
                _currentEmotionIndex = 0;

            SetEmotion(_currentEmotionIndex);
        }

        public virtual void SetEmotion(int emoIndex)
        {
            autoblink = false;
            ResetBlendShapes();
            switch (emoIndex)
            {
                case 0:
                    DemoScenes.Instance.SetDescriptionText("Emotion: None");
                    break;
                case 1:
                    DemoScenes.Instance.SetDescriptionText("Emotion: Smile");
                    SetBlendShapeWeightFace(2, 100f);
                    break;
                case 2:
                    DemoScenes.Instance.SetDescriptionText("Emotion: Celebratory");
                    SetBlendShapeWeightFace(3, 100f);
                    break;
                case 3:
                    DemoScenes.Instance.SetDescriptionText("Emotion: Disappointed");
                    SetBlendShapeWeightFace(1, 100f);
                    break;
                case 4:
                    DemoScenes.Instance.SetDescriptionText("Emotion: Concerned");
                    SetBlendShapeWeightFace(4, 100f);
                    break;
                case 5:
                    DemoScenes.Instance.SetDescriptionText("Emotion: Surprised");
                    SetBlendShapeWeightFace(5, 100f);
                    break;
                case 6:
                    DemoScenes.Instance.SetDescriptionText("Emotion: Sigh");
                    SetBlendShapeWeightFace(4, 100f);
                    SetBlendShapeWeightFace(9, 100f);
                    SetBlendShapeWeightFace(13, 100f);
                    break;
            }
        }

        protected void ResetBlendShapes()
        {
            for (int i = 0; i < skinnedMeshRendererHeadFace.sharedMesh.blendShapeCount; i++)
            {
                skinnedMeshRendererHeadFace.SetBlendShapeWeight(i, 0f);
            }

        }


        public virtual void ToggleHairColor()
        {
            _hairColorIndex++;
            if (_hairColorIndex >= hairMaterialList.Length)
                _hairColorIndex = 0;

            Material[] hairMaterials = skinnedMeshRendererHair.materials;

            hairMaterials[0] = hairMaterialList[_hairColorIndex];

            skinnedMeshRendererHair.materials = hairMaterials;

            DemoScenes.Instance.DescriptionText.SetText(hairMaterialList[_hairColorIndex].name);
        }

        public virtual void NextAnim()
        {
            _currentAnimIndex++;
            if (_currentAnimIndex >= animations.Length)
            {
                _currentAnimIndex = 0;
            }
            AnimationClip nextAnim = animations[_currentAnimIndex];
            this.animator.CrossFade(nextAnim.name, _transitionDuration);
            DemoScenes.Instance.DescriptionText.SetText("Animation: " + nextAnim.name);
        }


        public void AddMaterialToRenderer(SkinnedMeshRenderer renderer, Material materialToAdd)
        {
            Material[] currentMaterials = renderer.materials;
            Material[] newMaterials = new Material[currentMaterials.Length + 1];
            currentMaterials.CopyTo(newMaterials, 0);
            newMaterials[currentMaterials.Length] = materialToAdd;
            renderer.materials = newMaterials;
        }

        public void RemoveLastMaterialFromRenderer(SkinnedMeshRenderer renderer)
        {
            if (renderer.materials.Length > 1)
            {
                Material[] materials = renderer.materials;
                Array.Resize(ref materials, materials.Length - 1);
                renderer.materials = materials;
            }
        }


        protected void SetBlendShapeWeightFace(int blendShapeIndex, float targetWeight, float duration=.25f)
        {
            StartCoroutine(AnimateBlendShape(blendShapeIndex, targetWeight, duration));

        }

        private IEnumerator AnimateBlendShape(int blendShapeIndex, float targetWeight, float duration)
        {
            float startTime = Time.time;
            float startWeight = skinnedMeshRendererHeadFace.GetBlendShapeWeight(blendShapeIndex);

            while (Time.time < startTime + duration)
            {
                float elapsed = Time.time - startTime;
                float normalizedTime = elapsed / duration;

                float weight = Mathf.Lerp(startWeight, targetWeight, normalizedTime);
                skinnedMeshRendererHeadFace.SetBlendShapeWeight(blendShapeIndex, weight);

                yield return null;
            }

            skinnedMeshRendererHeadFace.SetBlendShapeWeight(blendShapeIndex, targetWeight);
        }


        public virtual void NextClothing()
        {
            _outfitStyleIndex++;
            if (_outfitStyleIndex > outfitMaterialList.Length - 1)
                _outfitStyleIndex = 0;

            Material[] clothingMaterials = skinnedOutfitRenderer.materials;
            clothingMaterials[mainClothingMaterialIndex] = outfitMaterialList[_outfitStyleIndex];
            skinnedOutfitRenderer.materials = clothingMaterials;
            DemoScenes.Instance.DescriptionText.SetText("Outfit Style: " + skinnedOutfitRenderer.materials[mainClothingMaterialIndex].name);
        }

        public virtual void NextBottoms()
        {
            _bottomsStyleIndex++;
            if (_bottomsStyleIndex > bottomsMaterialList.Length - 1)
                _bottomsStyleIndex = 0;

            Material[] clothingMaterials = skinnedOutfitRenderer.materials;
            clothingMaterials[bottomsClothingMaterialIndex] = bottomsMaterialList[_bottomsStyleIndex];
            skinnedOutfitRenderer.materials = clothingMaterials;
            DemoScenes.Instance.DescriptionText.SetText("Bottoms Style: " + skinnedOutfitRenderer.materials[bottomsClothingMaterialIndex].name);

        }

        public virtual void NextShoes()
        {
            _shoesStyleIndex++;
            if (_shoesStyleIndex > shoesMaterialList.Length - 1)
                _shoesStyleIndex = 0;

            Material[] clothingMaterials = skinnedOutfitRenderer.materials;
            clothingMaterials[shoesMaterialIndex] = shoesMaterialList[_shoesStyleIndex];
            skinnedOutfitRenderer.materials = clothingMaterials;
            DemoScenes.Instance.DescriptionText.SetText("Bottoms Style: " + skinnedOutfitRenderer.materials[shoesMaterialIndex].name);

        }

        

        public virtual void ToggleHeadGear() { }
        public virtual void ToggleFaceGear() { }

        public virtual void ToggleEquip(){}

    }
}
