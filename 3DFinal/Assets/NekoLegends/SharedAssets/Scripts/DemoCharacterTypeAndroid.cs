using System;
using System.Collections;
using UnityEngine;

namespace NekoLegends
{
    public class DemoCharacterTypeAndroid : MonoBehaviour
    {
        [SerializeField] protected bool isStartAnimationOnLoad;
        [SerializeField] protected AnimationClip[] animations;
        [SerializeField] protected SkinnedMeshRenderer skinnedMeshRendererBody;
        [SerializeField] protected Material[] bodyMaterialList;
        [SerializeField] protected GameObject LeftHandItem, RightHandItem;

        public Animator animator { get; protected set; }
        protected int _currentAnimIndex = -1, _bodyMaterialIndex = 0;
        protected float _transitionDuration = 0.25f;
      

        protected virtual void Start()
        {
            animator = GetComponent<Animator>();
            if(isStartAnimationOnLoad)
            {
                PlayAnimationIndex(0);
            }
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

            animator.CrossFade(selectedAnim.name, 0);
            DemoScenes.Instance.DescriptionText.SetText("Animation: " + selectedAnim.name);
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

        public virtual void NextMaterial()
        {
            // Cycle through the bodyMaterialList
            _bodyMaterialIndex++;
            if (_bodyMaterialIndex >= bodyMaterialList.Length) // Use >= to handle empty array edge case
            {
                _bodyMaterialIndex = 0;
            }

            // Ensure we have a valid material to apply
            if (bodyMaterialList.Length == 0 || skinnedMeshRendererBody == null)
            {
                Debug.LogWarning("No materials available in bodyMaterialList or skinnedMeshRendererBody is null.");
                return;
            }

            // Get the current materials array from the renderer
            Material[] bodyMaterials = skinnedMeshRendererBody.materials;

            // Typically, we only want to replace the first material slot (index 0)
            // If your SkinnedMeshRenderer has multiple material slots, adjust accordingly
            bodyMaterials[0] = bodyMaterialList[_bodyMaterialIndex]; // Always set the first slot

            // Apply the updated materials back to the renderer
            skinnedMeshRendererBody.materials = bodyMaterials;

            // Update the description text with the new material name
            DemoScenes.Instance.DescriptionText.SetText("Material: " + bodyMaterialList[_bodyMaterialIndex].name);
        }
    }
}