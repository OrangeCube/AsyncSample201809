using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class 選択肢 : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private Button _button;

    public async Task<int> AwaitSelect(string message, int storyId, CancellationToken ct)
    {
        try
        {
            _text.text = message;
            await _button.OnClickAsObservable().First().ToTask(ct);
            return storyId;
        }
        catch (Exception ex)
        {
            Debug.Log($"{message} is cancel {ex}");
            throw ex;
        }
    }
}
