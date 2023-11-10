mergeInto(LibraryManager.library, {

    FlushFileWrites: function() {
        FS.syncfs(false, function (err) {
            console.log('Error: syncfs failed!');
         });        
    },
});