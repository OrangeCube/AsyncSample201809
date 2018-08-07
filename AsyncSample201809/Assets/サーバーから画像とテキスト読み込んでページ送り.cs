using System.Linq;
using System.Threading.Tasks;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class サーバーから画像とテキスト読み込んでページ送り : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private RawImage _image;

    [SerializeField]
    private Button _button;

    void Start()
    {
        RunAsync("002").FireAndForget();
    }

    private async Task RunAsync(string storyName)
    {
        var story = await LoadStoryAsync(storyName);
        await ページ送りAsync(story);
    }

    private readonly struct StoryContent
    {
        public Texture2D Image { get; }
        public string Text { get; }

        public StoryContent(Texture2D image, string text)
            => (Image, Text) = (image, text);
    }

    private async Task<StoryContent[]> LoadStoryAsync(string storyName)
    {
        var www = new WWW($"https://raw.githubusercontent.com/OrangeCube/AsyncSample201809/master/RemoteResources/Story/{storyName}.txt");

        await www;

        const string BOM = "\uFEFF";
        var contents = System.Text.Encoding.UTF8.GetString(www.bytes)
            .Split(new[] { "\r\n", BOM }, System.StringSplitOptions.None)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(async x =>
            {
                var content = x.Split(',');
                return new StoryContent(await LoadImageAsync(content[0]), content[1]);
            });

        return await Task.WhenAll(contents);
    }

    private async Task<Texture2D> LoadImageAsync(string imageName)
    {
        var url = $"https://raw.githubusercontent.com/OrangeCube/AsyncSample201809/master/RemoteResources/Images/{imageName}.png";
        Debug.Log(url);

        var www = new WWW(url);

        await www;

        return www.texture;
    }

    private async Task ページ送りAsync(StoryContent[] story)
    {
        foreach(var content in story)
        {
            _text.text = content.Text;
            _image.texture = content.Image;
            await _button.OnClickAsObservable().First();
        }
        _text.text = "おわり";
    }
}
