# ked
kedはWindows上で動くsedライクなEditorを目指しています。

## 使い方
./ked.exe option  inputfile script

* option:-で始まる一文字アルファベット。
* inputfile:入力したいテキストファイル。複数選択可能。
* script:アドレスーコマンド形式。sedと同じものをまず実装しています。

### アドレス
sedと基本同じです。数字で記入します。0始まりです。＄有り。
0,4:初めから５行目までのアドレスを指します。
$:最終行を指します。
5!:６行目以外の行を指し示します。
0~2:奇数行を指定します。
