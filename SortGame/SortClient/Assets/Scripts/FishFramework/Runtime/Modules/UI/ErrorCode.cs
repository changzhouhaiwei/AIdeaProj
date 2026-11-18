namespace FishFramework
{
    public static class FindCompErrorCode
    {
        //UIView中
        public const int OK = 0;
        public const int ERROR_CAST_TYPE = 1001; //错误的组件转换类型
        public const int COMP_DEFINE_IS_NULL_OR_EMPTY = 1002; //compDefine为null或""
        public const int NOT_EXIST_THIS_COMPONENT = 1003; //View中不存在此组件定义
        public const int NOT_EXIST_ANY_CHILD_WIDGET = 1004; //View中不存在任何子Widget(不存在此Widget)
        public const int WIDGETS_ID_IS_NULL_OR_EMPTY = 1005; //widgetIds为null或""
        public const int NOT_EXIST_THIS_CHILD_WIDGET = 1006; //View不存在此Widget
    }
}