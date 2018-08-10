using UniRx;
using UniRx.Async;
using UnityEngine;

public class UniTaskのCancelの挙動 : MonoBehaviour
{
    void Start()
    {
        RunAsync().FireAndForget();
    }

    private async UniTask RunAsync()
    {
        var a = RunAsyncInternal();
        try
        {
            await a;
        }
        finally
        {
            Debug.Log("RunAsync status= " + a.Status);
        }
    }

    private async UniTask RunAsyncInternal()
    {
        var tcs = new UniTaskCompletionSource();
        tcs.TrySetCanceled();
        try
        {
            await tcs.Task;
        }
        finally
        {
            Debug.Log("RunAsyncInternal status= " + tcs.Task.Status);
        }
    }
}
