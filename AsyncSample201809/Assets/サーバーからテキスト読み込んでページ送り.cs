using System.Linq;
using System.Threading.Tasks;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class サーバーからテキスト読み込んでページ送り : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private Button _button;

    void Start()
    {
        RunAsync("001").FireAndForget();
    }

    private async Task RunAsync(string storyName)
    {
        var story = await LoadStoryAsync(storyName);
        await テキストページ送りAsync(story);
    }

    private async Task<string[]> LoadStoryAsync(string storyName)
    {
        using (var req = UnityWebRequest.Get($"https://raw.githubusercontent.com/OrangeCube/AsyncSample201809/master/RemoteResources/Story/{storyName}.txt"))
        {
            await req.SendWebRequest();

            return req.downloadHandler.text
                .Split(new[] { "\r\n" }, System.StringSplitOptions.None)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }
    }

    private async Task テキストページ送りAsync(string[] story)
    {
        foreach(var line in story)
        {
            _text.text = line;
            await _button.OnClickAsObservable().First();
        }
        _text.text = "おわり";
    }
}
