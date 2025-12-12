using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NekoLegends
{
    public class DemoMousePointerImage : MonoBehaviour
    {
        [Header("Pointer Image Configuration")]
        [SerializeField] private Image pointerImage;
        [SerializeField] private Vector2 hotspotOffset = Vector2.zero;
        [SerializeField] private bool hideMouseCursor = true;

        private RectTransform pointerRectTransform;
        private RectTransform parentRectTransform;
        private Canvas canvas;

        void Start()
        {
            InitializePointer();
        }

        private void InitializePointer()
        {
            if (pointerImage == null)
            {
                Debug.LogError("DemoMousePointerImage: Pointer image is not assigned!");
                return;
            }

            // Hide system cursor if enabled
            if (hideMouseCursor)
            {
                Cursor.visible = false;
            }

            // Get or add RectTransform
            pointerRectTransform = pointerImage.GetComponent<RectTransform>();
            if (pointerRectTransform == null)
            {
                pointerRectTransform = pointerImage.gameObject.AddComponent<RectTransform>();
            }
            parentRectTransform = pointerRectTransform.parent as RectTransform;

            // Set up UI image properties
            SetupUIImage();
            
            // Set initial position to center
            UpdatePointerPosition(Input.mousePosition);
        }

        private void SetupUIImage()
        {
            // Ensure the image is part of a canvas
            canvas = pointerImage.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("DemoMousePointerImage: Image is not part of a Canvas. Creating one...");
                GameObject canvasObj = new GameObject("PointerCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                // Move the image to the new canvas
                pointerImage.transform.SetParent(canvasObj.transform, false);
            }

            // Configure RectTransform for screen-space positioning
            pointerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            pointerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pointerRectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot
            pointerRectTransform.anchoredPosition = Vector2.zero;

            if (pointerImage != null)
            {
                pointerImage.raycastTarget = false;
            }

            parentRectTransform = pointerRectTransform.parent as RectTransform;
        }

        void Update()
        {
            UpdatePointerPosition(Input.mousePosition);
        }

        private void UpdatePointerPosition(Vector3 mousePosition)
        {
            if (pointerRectTransform == null) return;

            // Use screen space positioning (not world space)
            Vector2 screenPosition = new Vector2(mousePosition.x, mousePosition.y);

            if (parentRectTransform == null)
            {
                parentRectTransform = pointerRectTransform.parent as RectTransform;
            }

            if (parentRectTransform == null)
            {
                pointerRectTransform.anchoredPosition = screenPosition + hotspotOffset;
                return;
            }

            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                eventCamera = canvas.worldCamera;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, screenPosition, eventCamera, out Vector2 localPoint))
            {
                pointerRectTransform.anchoredPosition = localPoint + hotspotOffset;
            }
        }

        void OnDisable()
        {
            // Restore system cursor when script is disabled
            if (hideMouseCursor)
            {
                Cursor.visible = true;
            }
        }

        void OnDestroy()
        {
            // Ensure system cursor is restored when object is destroyed
            if (hideMouseCursor)
            {
                Cursor.visible = true;
            }
        }

        // Public methods for runtime control
        public void ShowSystemCursor()
        {
            Cursor.visible = true;
            if (pointerImage != null)
            {
                pointerImage.enabled = false;
            }
        }

        public void HideSystemCursor()
        {
            Cursor.visible = false;
            if (pointerImage != null)
            {
                pointerImage.enabled = true;
            }
        }

        public void SetHotspotOffset(Vector2 newOffset)
        {
            hotspotOffset = newOffset;
        }

        public void SetPointerImage(Sprite newSprite)
        {
            if (pointerImage != null)
            {
                pointerImage.sprite = newSprite;
            }
        }

        public void SetPointerColor(Color newColor)
        {
            if (pointerImage != null)
            {
                pointerImage.color = newColor;
            }
        }

        public void SetHideMouseCursor(bool hide)
        {
            hideMouseCursor = hide;
            Cursor.visible = !hide;
        }
    }
}