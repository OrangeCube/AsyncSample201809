using System.Threading;
using UniRx;
using UnityEngine;

public abstract class TypedMonoBehaviour : MonoBehaviour
{
    protected CancellationToken CancelOnDestroy => _cancelOnDestroyCts.Token;

    private CancellationTokenSource _cancelOnDestroyCts = new CancellationTokenSource();

    protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

    private void OnDestroy()
    {
        OnDestroyInternal();
        _cancelOnDestroyCts.Cancel();
        _cancelOnDestroyCts.Dispose();
        Disposables.Dispose();
    }

    protected void OnDestroyInternal() { }
}
