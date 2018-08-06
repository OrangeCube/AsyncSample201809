using System;
using System.Linq;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class 選択肢 : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private Button _button;

    private IAsyncClickEventHandler _handler;

    private void Awake()
    {
        _handler = _button.GetAsyncClickEventHandler();
    }

    private void OnDestroy()
    {
        _handler?.Dispose();
    }

    public async UniTask<int> AwaitSelect(string message, int storyId, CancellationToken ct)
    {
        try
        {
            _text.text = message;
            await _handler.OnClickAsync().WithCancellation(ct);

            return storyId;
        }
        catch (Exception ex)
        {
            Debug.Log($"{message} is cancel {ex}");
            throw ex;
        }
    }
}
