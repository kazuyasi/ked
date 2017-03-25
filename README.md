# ked
kedはWindows上で動くsedライクなCUI Editorを目指しています。以下は使い方の例です。  

``./ked.exe ./hoge.txt 3d``  
4行目を削除し残りを表示します。  

``./ked.exe ./hoge.txt 0~2p -n``  
奇数行のみを表示します。  

``./ked.exe ./hoge.txt $!s/piyo/``  
最終行以外のpiyoを削除します。  

## ライセンス  
MITライセンスにて提供しています。ライブラリとして下記を使用しています。  
* sprache(Nicholas Blumhardt氏制作：https://github.com/sprache/Sprache) @MITライセンス 
* ReadJEnc(hnx8氏制作：http://hp.vector.co.jp/authors/VA055804/) @MITライセンス
 
 
## 使い方
``./ked.exe option  inputfile script``

* option:-で始まる一文字アルファベット。
* inputfile:入力したいテキストファイル。複数選択可能。
* script:アドレスーコマンド形式。

### アドレス
sedと基本同じです。数字で記入します。0始まりです。＄有り。
* 0,4:初めから５行目までのアドレスを指します。
* $:最終行を指します。
* 5!:６行目以外の行を指し示します。
* 0~2:奇数行を指定します。
* /hoge/:hogeを含む行を指定します。正規表現対応（/以外）。

### コマンド  
コマンドは任意デリミタに対応しています。指定場所のcharがデリミタになります。
* d:指定アドレスを削除します。
* p:指定アドレスを表示します。普通-nオプションとともに使用します。
* s/old/new:指定アドレスのoldをnewに置き換えます。正規表現未対応コマンド。
* r/old/new:指定アドレスのoldをnewに置き換えます。正規表現対応コマンド。
* i text:指定アドレスの前に１行textを追加します。
* a text:指定アドレスの後に１行textを追加します。
* c text:指定アドレスを１行まるごとtextに置き換えます。

### オプション
* -e encoding:文字コードをencodingにします。
* -n:表示を抑制します。
* -r:先頭に行数を表示します。
* -i:ファイルを上書きします。バックアップを同時に作成します。
* -I:sコマンドで大文字小文字を区別しなくなります。
* -h int:int行だけ先頭を表示します。
* -t int:int行だけ末尾を表示します。
