using System.Linq;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class サーバーからテキスト読み込んでページ送り : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private Button _button;

    void Start()
    {
        サーバーからテキスト読み込んでページ送りAsync("001").FireAndForget();
    }

    private async Task サーバーからテキスト読み込んでページ送りAsync(string storyName)
    {
        var story = await LoadStoryAsync(storyName);
        await テキストページ送りAsync(story);
    }

    private async Task<string[]> LoadStoryAsync(string storyName)
    {
        var www = new WWW($"https://raw.githubusercontent.com/OrangeCube/AsyncSample201809/master/RemoteResources/Story/{storyName}.txt");

        await www;

        return System.Text.Encoding.UTF8.GetString(www.bytes)
            .Split(new[] { "\r\n" }, System.StringSplitOptions.None)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    private async Task テキストページ送りAsync(string[] story)
    {
        foreach(var line in story)
        {
            _text.text = line;
            await _button.OnClickAsObservable().First().ToTask();
        }
        _text.text = "おわり";
    }
}
