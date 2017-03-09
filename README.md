# ked
kedはWindows上で動くsedライクなCUI Editorを目指しています。上書きはリダイレクトで行ってください。以下は使い方の例です。  

``./ked.exe ./hoge.txt 3d``  
4行目を削除し残りを表示します。  

``./ked.exe ./hoge.txt 0~2p -n``  
奇数行のみを表示します。  

``./ked.exe ./hoge.txt $!s/piyo//``  
最終行以外のpiyoを削除します。  

## 使い方
``./ked.exe option  inputfile script``

* option:-で始まる一文字アルファベット。
* inputfile:入力したいテキストファイル。複数選択可能。
* script:アドレスーコマンド形式。sedと同じものをまず実装しています。

### アドレス
sedと基本同じです。数字で記入します。0始まりです。＄有り。
* 0,4:初めから５行目までのアドレスを指します。
* $:最終行を指します。
* 5!:６行目以外の行を指し示します。
* 0~2:奇数行を指定します。

### コマンド
* d:指定アドレスを削除します。
* p:指定アドレスを表示します。普通-nオプションとともに使用します。
* s/old/new/:指定アドレスのoldをnewに置き換えます。正規表現未対応コマンド。

### オプション
* -e encoding:文字コードをencodingにします。
* -n:すべての表示を切り替えます。
