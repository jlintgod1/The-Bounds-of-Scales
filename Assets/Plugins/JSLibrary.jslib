mergeInto(LibraryManager.library, {

    FlushFileWrites: function() {
        FS.syncfs(false, function (err) {
            if (err != null)
            {
                console.log('Error: syncfs failed! (' + err + ')');
            }
            else
            {
                console.log('Called syncfs!');
            }
            
         });        
    },
    InitializeJavascript: function() {
        addEventListener("beforeunload", (event) => { 
            SendMessage('GameManager', 'OnApplicationQuit');
        });
    },
});