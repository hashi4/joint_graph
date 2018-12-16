# joint_graph

## 概要
ジョイントと剛体との接続グラフをGraphvizのdot形式でテキスト出力します。

## 動作環境
* PmxEditor 0.2.5.4 64bit
* Windows 10 64bit
    * 単に私の環境です
* GraphViz 2.38.0
    * https://www.graphviz.org/ より入手可能です
    * グラフ構造を可視化するツールです

## ビルド方法
* C#コンパイラのパスを確認
    * Windows 10には素で4.xコンパイラが入っています。ビルドのバッチファイルはこれを呼ぶように書きました
        * 手元環境では以下にあり、バージョンは4.7.3056.0でした
        * `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
    * Windows 7でも PmxEditorを動作させるために追加インストールする .NET Frameworkにコンパイラも含まれるので、上記ディレクトリと似た所にあるのではないでしょうか
    * [.NET Downloads](https://www.microsoft.com/net/download/windows)や[Microsoft Build Tools 2015](https://www.microsoft.com/ja-JP/download/details.aspx?id=48159)等でも入手可能です
* build.batを編集
    * 4行目、コンパイラの設定
        * コンパイラパスを必要に応じて変えてください
        * `@set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
    * 7行目、PmxEditorのライブラリパス設定
        * PmxEditorのLibディレクトリ名を記述してください
        * C:\直下にPmxEditorをインストールした場合は以下になります
        * `@set PMXE_LIBPATH=C:\PmxEditor_0254\Lib`
* ビルド実行、インストール
    * build.batを実行するとdllが出来上がりますので、PmxEditorのプラグイン置き場へ置いてください

## 使い方
物理も素晴らしい、つみだんごさん作「[霧島改二](https://www.nicovideo.jp/watch/sm25332477)」をお借りして説明します。  
利用方法は以下二種類です
* 剛体やジョイントを指定してから本プラグインを実行する方法
* 指定せず本プラグインを実行する方法

機能的には大差なく、剛体/ジョイントを選択した場合は、選択したものと繋がっている範囲のみを出力します。

### プラグイン実行
* 剛体を選択  
選択しない場合は無視します  
![剛体選択](https://user-images.githubusercontent.com/16065740/50050850-f9669200-0149-11e9-982c-8ddd09f18205.png)  
* ジョイントを選択  
選択しない場合は無視します  
![ジョイント選択](https://user-images.githubusercontent.com/16065740/50050854-fff50980-0149-11e9-9190-7cb21c5723df.png)  
* プラグイン実行(直接実行の場合)  
![プラグイン実行](https://user-images.githubusercontent.com/16065740/50051134-2fa71000-0150-11e9-9562-5326c97ef3cf.png)  
「名前を付けて保存」のダイアログが出ますので、ファイル名を指定し、「保存」を押下します。  
プラグインは指定された名称でテキストファイルを作成し、dot形式でグラフ情報を保存します。

### 図に変換
Graphvizはsvgやpdfのようなベクトル形式にも、pngやjpgのようなラスター形式にも対応しています。  
詳細は上記のGraphvizサイトを参照してください。

"kirishima.dot"にプラグイン出力したとして、例を示します。

* PNG形式へ変換する例
    * `dot -Tpng kirishima.dot -o kirishima.png`

* SVG形式へサイズを指定して変換する例
    * `dot -Tsvg -Gsize=10 kirishima.dot -o kirishima.svg`

## 図の説明
* 四角: ジョイント
* 楕円: 剛体
    * 実線: ボーン追従
    * 破線: 物理演算
    * 点線: 物理+ボーン位置合わせ
    * []内: 関連ボーン番号と名称

## 例
PNGへ変換した例は以下です。 

![霧島袖](https://user-images.githubusercontent.com/16065740/50050750-ae4b7f80-0147-11e9-9b63-7aa1b7f73d4a.png)

以上、何かのお役に立てば