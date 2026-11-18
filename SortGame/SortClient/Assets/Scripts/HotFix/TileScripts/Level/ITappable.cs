namespace FishTilePlay
{
    public interface ITappable
    {
        public void OnObjectClicked(out bool isValid);

        public void OnObjectReleased();

        public void PlayLongPresssAnim();

        public void RecoverLongPressAnim(bool bResetTran = true);
    }
}
