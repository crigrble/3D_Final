using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NekoLegends
{
    public class DemoCharacterTypeMob : MonoBehaviour
    {
        [SerializeField] protected bool isStartAnimationOnLoad;
        [SerializeField] protected AnimationClip[] animations;
        [SerializeField] protected SkinnedMeshRenderer skinnedMeshRendererBody;
        [SerializeField] protected LODGroup lodGroup;
        [SerializeField] protected Material[] bodyMaterialList;
        [SerializeField] protected GameObject LeftHandItem, RightHandItem;

        public Animator animator { get; protected set; }
        protected int _currentAnimIndex = -1, _bodyMaterialIndex = 0;
        [SerializeField] protected float _transitionDuration = 0.25f;

        [SerializeField] private Slider mouthBlendShapeSlider;
        protected SkinnedMeshRenderer[] bodyRenderers;

        protected virtual void Start()
        {
            animator = GetComponent<Animator>();

            // Collect body renderers from SkinnedMeshRendererBody and/or LODGroup
            var rendererList = new List<SkinnedMeshRenderer>();

            if (skinnedMeshRendererBody != null)
            {
                rendererList.Add(skinnedMeshRendererBody);
            }

            if (lodGroup != null)
            {
                var lods = lodGroup.GetLODs();
                var rendererSet = new HashSet<SkinnedMeshRenderer>();
                foreach (var lod in lods)
                {
                    foreach (var renderer in lod.renderers)
                    {
                        if (renderer is SkinnedMeshRenderer smr)
                        {
                            rendererSet.Add(smr);
                        }
                    }
                }
                rendererList.AddRange(rendererSet);
            }

            bodyRenderers = rendererList.ToArray();

            if (bodyRenderers.Length == 0)
            {
                Debug.LogError("Neither SkinnedMeshRendererBody nor LODGroup assigned. No body renderers available.");
            }

            if (isStartAnimationOnLoad)
            {
                PlayAnimationIndex(0);
            }

            if (mouthBlendShapeSlider)
                BindBlendShapeSlider(mouthBlendShapeSlider, 0, 50f);
        }

        /// <summary>
        /// Binds a UI Slider to control a blend shape weight on all body SkinnedMeshRenderers.
        /// Default blendShapeIndex = 0 (mouth), defaultValue = 50.
        /// </summary>
        /// <param name="slider">UI Slider component to drive the blend shape.</param>
        /// <param name="blendShapeIndex">Index of the blend shape to control.</param>
        /// <param name="defaultValue">Initial value for the slider (0-100 range).</param>
        public void BindBlendShapeSlider(Slider slider, int blendShapeIndex = 0, float defaultValue = 50f)
        {
            if (slider == null || bodyRenderers.Length == 0)
            {
                Debug.LogWarning("BindBlendShapeSlider: Slider is null or no body renderers available.");
                return;
            }
            // Configure slider range
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = defaultValue;

            // Apply initial blend shape weight to all renderers
            foreach (var smr in bodyRenderers)
            {
                smr.SetBlendShapeWeight(blendShapeIndex, slider.value);
            }

            // Subscribe to value changes
            slider.onValueChanged.AddListener(value =>
            {
                foreach (var smr in bodyRenderers)
                {
                    smr.SetBlendShapeWeight(blendShapeIndex, value);
                }
            });
        }

        public virtual void PlayAnimationIndex(int _currentAnimIndex)
        {
            if (_currentAnimIndex < 0 || _currentAnimIndex >= animations.Length)
            {
                Debug.LogError("Invalid animation index: " + _currentAnimIndex);
                return;
            }

            AnimationClip selectedAnim = animations[_currentAnimIndex];
            if (selectedAnim == null)
            {
                Debug.LogError("Selected animation is null at index: " + _currentAnimIndex);
                return;
            }

            animator.CrossFade(selectedAnim.name, _transitionDuration);
            DemoScenes.Instance.DescriptionText.SetText("Animation: " + selectedAnim.name);
        }

        public virtual void NextAnim()
        {
            _currentAnimIndex++;
            if (_currentAnimIndex >= animations.Length)
                _currentAnimIndex = 0;

            AnimationClip nextAnim = animations[_currentAnimIndex];
            this.animator.CrossFade(nextAnim.name, _transitionDuration);
            DemoScenes.Instance.DescriptionText.SetText("Animation: " + nextAnim.name);
        }

        public virtual void NextMaterial()
        {
            _bodyMaterialIndex++;
            if (_bodyMaterialIndex >= bodyMaterialList.Length)
                _bodyMaterialIndex = 0;

            if (bodyMaterialList.Length == 0 || bodyRenderers.Length == 0)
            {
                Debug.LogWarning("No materials available or no body renderers.");
                return;
            }

            Material selectedMat = bodyMaterialList[_bodyMaterialIndex];
            foreach (var smr in bodyRenderers)
            {
                Material[] bodyMaterials = smr.materials;
                if (bodyMaterials.Length > 0)
                {
                    bodyMaterials[0] = selectedMat;
                    smr.materials = bodyMaterials;
                }
            }
            DemoScenes.Instance.DescriptionText.SetText("Material: " + selectedMat.name);
        }
    }
}