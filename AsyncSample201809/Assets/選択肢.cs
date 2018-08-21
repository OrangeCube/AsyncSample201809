using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class 選択肢 : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private Button _button;

    public async UniTask<int> AwaitSelect(string message, int storyId, CancellationToken ct)
    {
        try
        {
            _text.text = message;

            await _button.OnClickAsync(ct);

            return storyId;
        }
        catch (Exception ex)
        {
            Debug.Log($"{message} is cancel {ex}");
            throw;
        }
    }
}
