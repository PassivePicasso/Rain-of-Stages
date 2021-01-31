#if THUNDERKIT_CONFIGURED
namespace PassivePicasso.RainOfStages.Proxy
{
    internal interface IProxyReference<T> where T : UnityEngine.Object
    {
        T ResolveProxy();
    }
}
#endif
