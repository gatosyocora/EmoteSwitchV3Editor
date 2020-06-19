EmoteSwitchV3Editor.csについて

〇使い方
HowToUse.txtを参照

〇利用規約・免責事項
本スクリプトはgatosyocoraが作成しました。
本スクリプトはZlibライセンスで運用されます。(LICENSE_Editor.txt参照)
本スクリプトのEditor拡張の使用およびそれにより作成されたものによる
如何なる問題に対してgatosyocoraは責任を負いかねます。
また, 本スクリプトはEmoteSwitchV3本体を支援するために作成されました。
双方の利用規約等に矛盾が発生した場合またはこちらに記載されていないことに関しては
EmoteSwitchV3本体の利用規約を優先することとします。

〇ライセンス表記
(c) 2019 gatosyocora

〇更新履歴
v1.4	・EmoteSwitchV3を設定する部分を外部公開APIとして整備(SetEmoteSwitchV3)
		・EmoteSwitchV3Editorで作成されるAnimationファイルの保存先を設定できるように
		・初期状態がActiveなオブジェクトが一瞬消えてしまう現象に対応
		・EmoteSwitchV3設定時にテスト用のPrefabを追加するように
v1.3	・LocalSystemに対応
		・JointやIKFollowerを使ったシステムが壊れることを回避
			- コンポーネントを非アクティブにならないところに移動させる
v1.2	・Unity2018のPrefabシステムにあわせて変更
			- SetParent前にUnpackPrefabの実施
		・Undo機能を追加
