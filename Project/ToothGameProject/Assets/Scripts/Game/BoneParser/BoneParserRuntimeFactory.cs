
namespace GameDll
{
    internal static class BoneParserRuntimeFactory
    {
        public static IBoneParserRuntime Create(BoneParserConfig config)
        {
#if BoneParserLib
            return new BoneParserRuntimeManaged(config);
#else
            return new BoneParserRuntimeNative(config);
#endif
        }
    }
}
