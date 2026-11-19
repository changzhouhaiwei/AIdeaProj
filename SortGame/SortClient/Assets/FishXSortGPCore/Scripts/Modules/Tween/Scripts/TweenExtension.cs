namespace XSortGameCore
{
    public static class TweenExtension
    {
        public static void KillActive(this TweenClip tc)
        {
            if (tc != null && tc.IsActive) tc.Kill();
        }
        public static void KillActive(this TweenClip[] arr)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] != null && arr[i].IsActive) arr[i].Kill();
        }
        public static bool ExistsAndActive(this TweenClip tc) => tc != null && tc.IsActive;
    }
}
