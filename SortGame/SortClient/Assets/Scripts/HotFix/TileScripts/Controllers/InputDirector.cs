using UnityEngine;
using UnityEngine.InputSystem;

namespace FishTilePlay
{
    [DefaultExecutionOrder(-1)]
    public class InputDirector : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;

        private InputAction mousePositionAction;

        // 添加键盘按键监听
        private InputAction f1Action;
        private InputAction f2Action;
        private InputAction f3Action;
        private InputAction f4Action;

        private InputAction f6Action;
        private InputAction escapeAction;

        public static InputActionAsset InputActionsAsset { get; private set; }

        public static Vector2 MousePosition { get; private set; }
        public static InputAction ClickAction { get; private set; }

        // 添加键盘按键的静态属性
        public static InputAction F1Action { get; private set; }
        public static InputAction F2Action { get; private set; }
        public static InputAction F3Action { get; private set; }
        public static InputAction F4Action { get; private set; }

        public static InputAction F6Action { get; private set; }
        public static InputAction EscapeAction { get; private set; }
        private void Awake()
        {
            InputActionsAsset = inputActions;

            mousePositionAction = inputActions.FindAction("Point");
            mousePositionAction.Enable();

            ClickAction = inputActions.FindAction("Click");
            ClickAction.Enable();

            // 初始化键盘按键监听
            InitializeKeyboardActions();

            MousePosition = mousePositionAction.ReadValue<Vector2>();
        }

        private void InitializeKeyboardActions()
        {
            // 创建键盘按键的InputAction
            f1Action = new InputAction("F1", InputActionType.Button, "<Keyboard>/f1");
            f2Action = new InputAction("F2", InputActionType.Button, "<Keyboard>/f2");
            f3Action = new InputAction("F3", InputActionType.Button, "<Keyboard>/f3");
            f4Action = new InputAction("F4", InputActionType.Button, "<Keyboard>/f4");
            f6Action = new InputAction("F6", InputActionType.Button, "<Keyboard>/f6");
            // Esc键，同时支持键盘Esc和Android返回键
            escapeAction = new InputAction("Escape", InputActionType.Button, "<Keyboard>/escape");

            // 启用按键监听
            f1Action.Enable();
            f2Action.Enable();
            f3Action.Enable();
            f4Action.Enable();
            f6Action.Enable();
            escapeAction.Enable();

            // 设置静态属性
            F1Action = f1Action;
            F2Action = f2Action;
            F3Action = f3Action;
            F4Action = f4Action;
            F6Action = f6Action;
            EscapeAction = escapeAction;

        }

        private void Update()
        {
            MousePosition = mousePositionAction.ReadValue<Vector2>();
        }

        private void OnDestroy()
        {
            f1Action?.Dispose();
            f2Action?.Dispose();
            f3Action?.Dispose();
            f4Action?.Dispose();
            f6Action?.Dispose();
            escapeAction?.Dispose();
        }
    }
}
