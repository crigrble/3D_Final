using UnityEngine;
using UnityEngine.UI;


namespace NekoLegends
{
    public class DemoSceneAndroids : DemoScenes
    {
        [Space]
        [SerializeField] protected DemoCharacterTypeAndroid _Character;
        
        [Space]
        [SerializeField] protected Button AnimationBtn, MaterialBtn;

        private const string _title = "Androids Demo Scene";

        private Camera _mainCamera;

        #region Singleton
        public static new DemoSceneAndroids Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType(typeof(DemoSceneAndroids)) as DemoSceneAndroids;

                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        private static DemoSceneAndroids _instance;
        #endregion


        protected override void OnEnable()
        {
            if (AnimationBtn)
                RegisterButtonAction(AnimationBtn, () => _Character.NextAnim());
            
            if (MaterialBtn)
                RegisterButtonAction(MaterialBtn, () => _Character.NextMaterial());
            base.OnEnable();

        }


        protected override void Start()
        {
            base.Start();
            _mainCamera = Camera.main;
            DescriptionText.SetText(_title);
        }

    }
}
