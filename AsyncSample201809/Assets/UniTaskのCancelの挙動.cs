using System.Threading.Tasks;
using UniRx;
using UniRx.Async;
using UnityEngine;

public class UniTaskのCancelの挙動 : MonoBehaviour
{
    /// <summary>
    /// 本家タスクとUniTaskを入れ子にした上でタスクをキャンセルさせたときの挙動に問題がないことを確認
    /// </summary>
    void Start()
    {
        RunAsync1().FireAndForget();
        RunAsync2().FireAndForget();
    }

    /// <summary>
    /// <see cref="RunAsyncInternal1"/>はTaskでキャンセル。<see cref="RunAsync1"/>はUniTask
    /// </summary>
    /// <returns></returns>
    private async UniTask RunAsync1()
    {
        var a = RunAsyncInternal1();
        try
        {
            await a;
        }
        finally
        {
            Debug.Log("RunAsync1 status= " + a.Status);
        }
    }

    private async Task RunAsyncInternal1()
    {
        var tcs = new TaskCompletionSource<Unit>();
        tcs.TrySetCanceled();
        try
        {
            await tcs.Task;
        }
        finally
        {
            Debug.Log("RunAsyncInternal1 status= " + tcs.Task.Status);
        }
    }

    /// <summary>
    /// <see cref="RunAsyncInternal2"/>はUniTaskでキャンセル。<see cref="RunAsync1"/>はTask
    /// </summary>
    /// <returns></returns>
    private async Task RunAsync2()
    {
        var a = RunAsyncInternal2();
        try
        {
            await a;
        }
        finally
        {
            Debug.Log("RunAsync2 status= " + a.Status);
        }
    }


    private async UniTask RunAsyncInternal2()
    {
        var tcs = new UniTaskCompletionSource();
        tcs.TrySetCanceled();
        try
        {
            await tcs.Task;
        }
        finally
        {
            Debug.Log("RunAsyncInternal2 status= " + tcs.Task.Status);
        }
    }

}
