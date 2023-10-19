mergeInto(LibraryManager.library, {

  FSStartup: function () {
    	FS.syncfs(true, function (err) {
  		if (err != null) console.log(err);
  	});
  },

  FSSync: function () {
    	FS.syncfs(false, function (err) {
  		if (err != null) console.log(err);
  	});
  },
});