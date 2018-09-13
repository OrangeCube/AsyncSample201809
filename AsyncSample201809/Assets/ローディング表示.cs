using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static TaskExtensions;

public class ローディング表示 : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private RawImage _image;

    [SerializeField]
    private Button _button;

    [SerializeField]
    private RectTransform _選択肢Container;

    [SerializeField]
    private GameObject _選択肢Prefab;

    [SerializeField]
    private LoadingPanel _loadinPanel;

    private IAsyncClickEventHandler _clickHandler;

    void Start()
    {
        _clickHandler = _button.GetAsyncClickEventHandler();
        RunAsync("003").FireAndForget();
    }

    void OnDestroy() => _clickHandler?.Dispose();

    private async UniTask RunAsync(string storyName)
    {
        var story = await _loadinPanel.LoadingOn(LoadStoryAsync(storyName));
        await ページ送りAsync(story);
    }

    private readonly struct StoryContent
    {
        public int Id { get; }
        public Texture2D Image { get; }
        public string Text { get; }
        public SelectionContentModel[] SelectionContents { get; }

        public StoryContent(int id, Texture2D image, string text, SelectionContentModel[] selectionContents)
            => (Id, Image, Text, SelectionContents) = (id, image, text, selectionContents);
    }

    private async UniTask<StoryContent[]> LoadStoryAsync(string storyName)
    {
        var story = await storyName.LoadStoryTextAsync();

        var contents = story
            .Select(async x =>
            {
                var content = x.Split(',');
                var selectionContents = content.ParseSelectionContentModels();

                var storyId = int.Parse(content[0]);
                Texture2D image = null;
                try
                {
                    image = await content[1].LoadImageAsync();
                }
                catch (ResourceLoadException ex)
                {
                    // 画像読み込み時の例外処理
                    // 今回はログを出しつつ処理を継続させる
                    Debug.LogWarning(ex);
                }

                return new StoryContent(storyId, image, content[2], selectionContents.ToArray());
            });

        return await UniTask.WhenAll(contents);
    }

    private async UniTask ページ送りAsync(StoryContent[] story)
    {
        var content = story.First();
        var contents = story.ToDictionary(x => x.Id);

        while (true)
        {
            _text.text = content.Text;
            _image.texture = content.Image;

            var nextContentId = 0;
            if (content.SelectionContents.Any())
            {
                using (var cts = new CancellationTokenSource())
                {
                    var 選択肢待ち = content.SelectionContents.Await選択肢(_選択肢Prefab, _選択肢Container, cts.Token, _選択肢プール);
                    nextContentId = await 選択肢待ち;
                    cts.Cancel();
                }

                Release選択肢();
            }
            else
            {
                await _clickHandler.OnClickAsync();

                nextContentId = content.Id + 1;
            }

            if (!contents.TryGetValue(nextContentId, out content))
                break;
        }

        _text.text = "おわり";
    }

    private void Release選択肢()
    {
        foreach (Transform c in _選択肢Container)
        {
            if (!c.gameObject.activeSelf)
                continue;
            c.gameObject.SetActive(false);
            _選択肢プール.Push(c.GetComponent<選択肢>());
        }
    }

    private Stack<選択肢> _選択肢プール = new Stack<選択肢>();
}
