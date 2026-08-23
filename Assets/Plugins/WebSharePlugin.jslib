mergeInto(LibraryManager.library, {
    // 画像をダウンロード保存し、Xの投稿画面を開く
    SaveImageAndShareX: function (imageStrPtr, tweetTextPtr, gameUrlPtr) {
        var base64Image = UTF8ToString(imageStrPtr);
        var tweetText = UTF8ToString(tweetTextPtr);
        var gameUrl = UTF8ToString(gameUrlPtr);

        // 1. 画像のダウンロード処理
        var link = document.createElement('a');
        link.download = 'dying_message.png';
        link.href = base64Image;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        // 2. X（Twitter）投稿画面の起動
        var shareUrl = "https://twitter.com/intent/tweet?text=" 
            + encodeURIComponent(tweetText) 
            + "&url=" + encodeURIComponent(gameUrl);
        
        window.open(shareUrl, '_blank');
    }
});