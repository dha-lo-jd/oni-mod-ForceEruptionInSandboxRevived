using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ForceEruptionInSandboxRevived;

// 1. 間欠泉・火山が選択された時の詳細パネル（SideScreen）にボタンを追加するパッチ
[HarmonyPatch(typeof(Geyser), "OnSpawn")]
public static class Geyser_OnSpawn_Patch
{
    public static void Postfix(Geyser __instance)
    {
        // すでにコンポーネントが追加されていないかチェック
        if (__instance.GetComponent<ForceEruptButtonManager>() == null)
        {
            __instance.gameObject.AddComponent<ForceEruptButtonManager>();
        }
    }
}

// サンドボックス状態の変更を検知するフック
[HarmonyPatch(typeof(Game), "set_SandboxModeActive")] // プロパティのセッターをフック
public static class Game_SandboxModeActive_Patch
{
    public static void Postfix()
    {
        // サンドボックスの状態が変わった瞬間、現在プレイヤーが詳細画面（DetailsScreen）を開いているか確認
        if (DetailsScreen.Instance != null && DetailsScreen.Instance.target != null)
        {
            GameObject currentTarget = DetailsScreen.Instance.target;

            // 選択中のオブジェクトが、あなたの管理コンポーネント（MyGeyserManager）を持っているか確認
            var manager = currentTarget.GetComponent<ForceEruptButtonManager>();
            if (manager != null)
            {
                // 状態が変わったのでコンポーネントの着脱を実行
                manager.RefreshButtonComponent();
                
                if (DetailsScreen.Instance != null && DetailsScreen.Instance.CompareTargetWith(manager.gameObject))
                {
                    DetailsScreen.Instance.Refresh(manager.gameObject);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(DebugHandler), "set_enabled")] // プロパティのセッターをフック
public static class DebugHandler_Enabled_Patch
{
    public static void Postfix()
    {
        Game_SandboxModeActive_Patch.Postfix();
    }
}

[HarmonyPatch(typeof(DetailsScreen), nameof(DetailsScreen.Refresh))]
public static class DetailsScreen_Refresh_Patch
{
    // 画面が開く（または更新される）直前にターゲットのコンポーネントを操作する
    public static void Prefix(DetailsScreen __instance, GameObject go)
    {
        if (go == null) return;

        // 選択されたオブジェクト（間欠泉など）が管理クラスを持っているか確認
        var manager = go.GetComponent<ForceEruptButtonManager>();
        if (manager != null)
        {
            // 画面が組み立てられる「直前」に、コンポーネントを追加または削除する
            manager.RefreshButtonComponent();
        }
    }
}

// 2. 間欠泉本体、あるいは監視用にアタッチする管理用コンポーネント
public class ForceEruptButtonManager : KMonoBehaviour
{
    protected override void OnSpawn()
    {
        base.OnSpawn();
        // スポーン時の初期チェック
        RefreshButtonComponent();
    }
    
    // ボタンの「枠ごと消去」を制御するコアロジック
    public void RefreshButtonComponent()
    {
        var buttonControl = this.GetComponent<ForceEruptButton>();

        if (ForceEruptButton.IsSandbox())
        {
            // サンドボックスON：ボタンが必要なので、なければ追加する
            if (buttonControl is null)
            {
                this.gameObject.AddComponent<ForceEruptButton>();
            }
        }
        else
        {
            // サンドボックスOFF：枠ごと完全に消したいので、コンポーネントを即座に破棄する
            if (buttonControl is not null)
            {
                DestroyImmediate(buttonControl);
            }
        }
    }
}

public class ForceEruptButton : KMonoBehaviour, ISidescreenButtonControl
{
    // ターゲットとなるGeyserコンポーネント
    private Geyser geyser;

    protected override void OnPrefabInit()
    {
        base.OnPrefabInit();
        geyser = GetComponent<Geyser>();
    }

    // 現在間欠泉が「噴火中」かどうかを判定
    private bool IsCurrentlyErupting()
    {
        if (geyser == null) return false;
        var smInstance = geyser.GetSMI<Geyser.StatesInstance>();

        if (smInstance == null) return false;

        // ステートマシンが erupting（噴火中）の階層にいるかをチェック
        var currentState = smInstance.GetCurrentState();
        StateMachine.BaseState[] eruptStates =
        {
            smInstance.sm.erupt.erupting,
            smInstance.sm.erupt.overpressure
        };
        return eruptStates.Contains(currentState);
    }

    // ボタンに表示されるテキスト
    public string SidescreenButtonText => IsSandbox() ? IsCurrentlyErupting() ? "強制休眠" : "強制噴火" : "";

    // ボタンのツールチップ
    public string SidescreenButtonTooltip => IsCurrentlyErupting()
        ? "時間を次の休眠まで強制的に進めます"
        : "時間を次の噴火まで強制的に進めます";

    // ★最重要：サンドボックスまたはデバッグモードが有効な時だけボタンを表示する
    public void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
    {
        return;
    }

    public bool SidescreenEnabled()
    {
        return true;
    }

    public bool SidescreenButtonInteractable()
    {
        return IsSandbox();
    }

    public static bool IsSandbox()
    {
        bool isSandbox = Game.Instance != null && Game.Instance.SandboxModeActive;
        bool isDebug = DebugHandler.enabled;

        return isSandbox || isDebug;
    }

    // ボタンがクリックされた時の処理
    public void OnSidescreenButtonPressed()
    {
        if (geyser == null) return;

        // 1. 状態管理（SMI）を取得
        var smInstance = geyser.GetSMI<Geyser.StatesInstance>();
        if (smInstance == null) return;

        if (IsCurrentlyErupting())
        {
            smInstance.master.ShiftTimeTo(Geyser.TimeShiftStep.DormantState);
            smInstance.master.ShiftTimeTo(Geyser.TimeShiftStep.NextIteration);
        }
        else
        {
            smInstance.master.ShiftTimeTo(Geyser.TimeShiftStep.ActiveState);
            smInstance.master.ShiftTimeTo(Geyser.TimeShiftStep.NextIteration);
        }

        DetailsScreen.Instance.Refresh(geyser.gameObject);
    }

    public int ButtonSideScreenSortOrder() => 20;
    public int HorizontalGroupID() => -1;
}