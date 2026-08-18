namespace GameDll
{
    public interface IBoneFrameSource
    {
        string ReadSourceName();
        void Tick();
        BoneFrameData ReadLatestFrameData();
        void Shutdown();
    }
}
