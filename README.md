# Force Eruption in Sandbox Revived

Oxygen Not Included向けのModです。サンドボックスモードまたはデバッグモード中、間欠泉の詳細画面から噴火と休眠を切り替えるボタンを追加します。

## 導入

このリポジトリはソースコードです。ビルド済みファイルを使う場合は、GitHub Releasesに配布物が公開されているか確認してください。

## ビルド

### 必要なもの

- Oxygen Not Included（ビルド対象のゲームバージョン）
- .NET SDK
- ゲームの `OxygenNotIncluded_Data\Managed` フォルダに含まれる参照DLL

### 手順

1. リポジトリをクローンします。
2. `Local.Build.props.example` を `Local.Build.props` にコピーします。
3. `Local.Build.props` の `GameManagedPath` をゲームの `OxygenNotIncluded_Data\Managed` フォルダに変更します。
4. Modフォルダへの自動コピーを使う場合は、`ModInstallPath` をローカルModフォルダに変更します。自動コピーが不要なら空欄にします。
5. ソリューションをビルドします。

```powershell
dotnet build .\ForceEruptionInSandboxRevived.sln
```

`ModInstallPath` を指定した場合、ビルド後にModのDLL、必要なPLib DLL、および `mod_info.yaml` が指定先へコピーされます。ゲーム本体のDLLはこのリポジトリには含めません。

## ライセンス

このプロジェクトはMIT Licenseの下で公開されています。詳細は [LICENSE](LICENSE) を参照してください。
