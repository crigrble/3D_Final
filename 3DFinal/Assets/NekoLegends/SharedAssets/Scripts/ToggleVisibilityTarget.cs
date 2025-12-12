using System.Collections.Generic;
using UnityEngine;

namespace NekoLegends
{
    public class ToggleVisibilityTarget : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> objects = new List<GameObject>();

        private int currentIndex = -1;

        private void Awake()
        {
            if (objects.Count > 0)
            {
                SetActiveIndex(0);
            }
        }

        public void ShowNext()
        {
            if (objects.Count == 0) return;

            int nextIndex = (currentIndex + 1) % objects.Count;
            SetActiveIndex(nextIndex);
        }

        public void ShowPrev()
        {
            if (objects.Count == 0) return;

            int prevIndex = (currentIndex - 1 + objects.Count) % objects.Count;
            SetActiveIndex(prevIndex);
        }

        public void SetActiveIndex(int index)
        {
            if (objects.Count == 0) return;

            index = Mathf.Clamp(index, 0, objects.Count - 1);

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(i == index);
                }
            }

            currentIndex = index;
        }

        //these are but button onClick() linkages
        public void ToggleAll() => ToggleAllInternal(null);
        public void EnableAll() => ToggleAllInternal(true);
        public void DisableAll() => ToggleAllInternal(false);

        private void ToggleAllInternal(bool? state)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;

                if (state.HasValue)
                    obj.SetActive(state.Value);
                else
                    obj.SetActive(!obj.activeSelf);
            }
        }

        public int CurrentIndex => currentIndex;
    }
}
