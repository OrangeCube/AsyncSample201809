using System;
using System.Threading.Tasks;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class TaskとUniTaskの動作コンテキスト : MonoBehaviour
{
    [SerializeField]
    private Text _text1;

    [SerializeField]
    private Text _text2;

    [SerializeField]
    private Text _text3;

    [SerializeField]
    private Text _text4;

    [SerializeField]
    private Button _button;

    async UniTask Start()
    {
        await _button.OnClickAsObservable().ToUniTask(useFirstValue:true);

        var startFrame = Time.frameCount;
        UniTaskのみAsync(startFrame).FireAndForget();
        Taskのみで同期コンテキストで待つAsync(startFrame).FireAndForget();
        TaskとUniTaskを交互に待つAsync(startFrame).FireAndForget();

        await _button.OnClickAsObservable().ToUniTask(useFirstValue: true);
        startFrame = Time.frameCount;
        Taskのみで同期コンテキストを外して待つAsync(startFrame).FireAndForget();
    }

    /// <summary>
    /// <see cref="UniTask"/>のみで1ms待つタスクを100回分実行
    /// </summary>
    private async UniTask UniTaskのみAsync(int startFrame)
    {
        for (var i = 0; i < 50; i++)
        {
            await UniTask.Delay(1);
            await UniTask.Delay(1);
        }

        _text1.text = $"UniTaskのみだと{Time.frameCount - startFrame}Frameかかる";
    }

    /// <summary>
    /// <see cref="Task"/>のみで1ms待つタスクを100回同期コンテキスト上で実行
    /// </summary>
    private async Task Taskのみで同期コンテキストで待つAsync(int startFrame)
    {
        for (var i = 0; i < 50; i++)
        {
            await Task.Delay(1);
            await Task.Delay(1);
        }
        _text2.text = $"Taskのみだと{Time.frameCount - startFrame}Frameかかる";
    }

    /// <summary>
    /// <see cref="Task"/>と<see cref="UniTask"/>で交互に1ms待つタスクを100回分実行
    /// </summary>
    /// <remarks>
    /// <see cref="Task"/>と<see cref="UniTask"/>を交互にawaitした場合、同フレーム中の2か所に完了を待つ処理が分散するため1フレームで両方完了するような挙動となる。
    /// * メインスレッドで<see cref="Task"/>をawaitした場合は<see cref="UnitySynchronizationContext"/>でタスクの完了を待つ。
    /// * <see cref="UniTask"/>は<see cref="IPlayerLoopItem"/>を実装する非同期処理は<see cref="PlayerLoopHelper"/>で登録されているプレイヤーループの中でタスクの完了を待つ。
    /// </remarks>
    private async UniTask TaskとUniTaskを交互に待つAsync(int startFrame)
    {
        for (var i = 0; i < 50; i++)
        {
            await Task.Delay(1);
            await UniTask.Delay(1);
        }
        _text3.text = $"TaskとUniTaskを交互に待つと{Time.frameCount - startFrame}Frameかかる";
    }

    /// <summary>
    /// <see cref="Task"/>のみで1ms待つタスクを101回実行
    /// </summary>
    /// <remarks>
    /// <see cref="Taskのみで同期コンテキストを外して待つAsyncInternal"/>の最初の1回目で同期コンテキストに戻らない（別スレッド実行に移行）ようにしてある
    /// <see cref="Taskのみで同期コンテキストを外して待つAsyncInternal"/>をawaitしているところで同期コンテキストに戻ってくるのでテキストへの代入時はメインスレッドで行われている
    /// </remarks>
    private async Task Taskのみで同期コンテキストを外して待つAsync(int startFrame)
    {
        await Taskのみで同期コンテキストを外して待つAsyncInternal();
        _text4.text = $"Taskのみで同期コンテキストを外して待つと{Time.frameCount - startFrame}Frameかかる";
    }

    private static async Task Taskのみで同期コンテキストを外して待つAsyncInternal()
    {
        await Task.Delay(1).ConfigureAwait(false);

        for (var i = 0; i < 50; i++)
        {
            await Task.Delay(1);
            await Task.Delay(1);
        }
    }
}
